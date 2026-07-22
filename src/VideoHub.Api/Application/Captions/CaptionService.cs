using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VideoHub.Api.Application.Exceptions;
using VideoHub.Api.Domain.Entities;
using VideoHub.Api.Domain.Jobs;
using VideoHub.Api.Domain.Media;
using VideoHub.Api.Infrastructure.Abstractions;
using VideoHub.Api.Infrastructure.Authentication;
using VideoHub.Api.Infrastructure.Persistence;

namespace VideoHub.Api.Application.Captions;

public sealed class CaptionService : ICaptionService
{
    private readonly IRepository<CaptionFile> captionFileRepository;
    private readonly IRepository<Job> jobRepository;
    private readonly IRepository<Transcript> transcriptRepository;
    private readonly IBlobStorage blobStorage;
    private readonly IUnitOfWork unitOfWork;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor;
    private readonly IConfiguration configuration;
    private readonly IHostEnvironment environment;
    private readonly ILogger<CaptionService> logger;
    private readonly AppDbContext dbContext;

    public CaptionService(
        IRepository<CaptionFile> captionFileRepository,
        IRepository<Job> jobRepository,
        IRepository<Transcript> transcriptRepository,
        IBlobStorage blobStorage,
        IUnitOfWork unitOfWork,
        IHttpClientFactory httpClientFactory,
        Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<CaptionService> logger,
        AppDbContext dbContext)
    {
        this.captionFileRepository = captionFileRepository;
        this.jobRepository = jobRepository;
        this.transcriptRepository = transcriptRepository;
        this.blobStorage = blobStorage;
        this.unitOfWork = unitOfWork;
        this.httpClientFactory = httpClientFactory;
        this.httpContextAccessor = httpContextAccessor;
        this.configuration = configuration;
        this.environment = environment;
        this.logger = logger;
        this.dbContext = dbContext;
    }

    public async Task DispatchCaptionGenerationAsync(
        Guid jobId,
        Guid projectId,
        Guid mediaFileId,
        Guid userId,
        string storagePath,
        string mediaType,
        string bucket,
        string originalLanguage,
        IReadOnlyList<string> targetLanguages,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        var callbackSecret = AiCallbackSecretResolver.ResolveSecret(configuration, environment)
            ?? throw new InvalidOperationException("Configuration 'AiService:CallbackSecret' is missing or blank.");

        logger.LogInformation("Caption Dispatch Started: JobId={JobId} Languages={Languages}", jobId, string.Join(",", targetLanguages));

        var languageTargets = new List<LanguageTarget>();

        foreach (var lang in targetLanguages)
        {
            var folderPath = $"{userId}/{projectId}/captions/{lang}/";

            var captionFileIds = new Dictionary<string, Guid>();
            foreach (var format in new[] { "srt", "vtt" })
            {
                var captionFile = new CaptionFile
                {
                    Id = Guid.NewGuid(),
                    JobId = jobId,
                    Language = lang,
                    Format = format,
                    Status = CaptionFileStatuses.Queued
                };
                await captionFileRepository.AddAsync(captionFile, cancellationToken);
                captionFileIds[format] = captionFile.Id;
            }

            languageTargets.Add(new LanguageTarget
            {
                LanguageCode = lang,
                CaptionFileIds = captionFileIds,
                FolderPath = folderPath
            });
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Caption Dispatch: CaptionFile rows created and folders ensured. Dispatching to Python.");

        var job = await jobRepository.GetByIdAsync(jobId, cancellationToken);
        if (job is null) throw new NotFoundException($"Job '{jobId}' was not found.");

        job.Status = JobStatuses.Processing;
        job.StatusMessage = "Dispatched to AI service";
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Resolve CorrelationId and RequestId from context if not passed
        string? requestId = null;
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            if (string.IsNullOrEmpty(correlationId) && httpContext.Items.TryGetValue(VideoHub.Api.Infrastructure.Middleware.CorrelationIdMiddleware.HeaderName, out var corrObj))
            {
                correlationId = corrObj?.ToString();
            }
            requestId = httpContext.TraceIdentifier;
        }

        if (string.IsNullOrEmpty(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
        }
        if (string.IsNullOrEmpty(requestId))
        {
            requestId = Guid.NewGuid().ToString();
        }

        var request = new AiProcessRequest
        {
            JobId = jobId,
            ProjectId = projectId,
            MediaId = mediaFileId,
            MediaType = mediaType,
            Bucket = bucket,
            StoragePath = storagePath,
            OriginalLanguage = originalLanguage,
            CallbackUrl = $"/api/v1/jobs/{jobId}/callback?secret={callbackSecret}",
            Languages = languageTargets,
            CorrelationId = correlationId,
            RequestId = requestId
        };

        var httpClient = httpClientFactory.CreateClient("AiService");
        
        // Propagate X-Correlation-ID header on the HTTP request
        httpClient.DefaultRequestHeaders.Remove("X-Correlation-ID");
        httpClient.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);

        var response = await httpClient.PostAsJsonAsync("process", request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Caption Dispatch Failed: Python returned {StatusCode} Body={Body}", response.StatusCode, body);
            throw new ServiceUnavailableException($"AI service returned {(int)response.StatusCode}.");
        }

        logger.LogInformation("Caption Dispatch Completed: JobId={JobId} — Python accepted request (202).", jobId);
    }

    public async Task UpdateCaptionFileStatusAsync(
        Guid captionFileId,
        string status,
        string? blobUrl,
        string? errorMessage,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Caption Status Update Started: CaptionFileId={CaptionFileId} Status={Status}", captionFileId, status);

        var captionFile = await captionFileRepository.GetByIdAsync(captionFileId, cancellationToken)
            ?? throw new NotFoundException($"CaptionFile '{captionFileId}' was not found.");

        captionFile.Status = status;
        captionFile.BlobUrl = blobUrl;
        captionFile.ErrorMessage = errorMessage;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Caption Status Update Completed: CaptionFileId={CaptionFileId} Status={Status}", captionFileId, status);
    }

    public async Task FinalizeJobAsync(
        Guid jobId,
        string detectedLanguage,
        string transcriptBlobUrl,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Job Finalization Started: JobId={JobId}", jobId);

        var job = await jobRepository.GetByIdAsync(jobId, cancellationToken)
            ?? throw new NotFoundException($"Job '{jobId}' was not found.");

        // Evaluate final job status from caption file statuses first to check if we should save transcript
        var allCaptionFiles = (await captionFileRepository.ListAsync(cancellationToken))
            .Where(cf => cf.JobId == jobId)
            .ToList();

        var anyCompleted = allCaptionFiles.Any(cf => cf.Status == CaptionFileStatuses.Completed);
        var anyFailed = allCaptionFiles.Any(cf => cf.Status == CaptionFileStatuses.Failed);

        if (!string.IsNullOrEmpty(transcriptBlobUrl))
        {
            try
            {
                // Check if a transcript already exists for this project, language, and version
                var allTranscripts = await transcriptRepository.ListAsync(cancellationToken);
                var existingTranscript = allTranscripts.FirstOrDefault(t => 
                    t.ProjectId == job.ProjectId && 
                    t.Language == detectedLanguage && 
                    t.Version == 1);

                if (existingTranscript != null)
                {
                    existingTranscript.BlobUrl = transcriptBlobUrl;
                    existingTranscript.Status = "Completed";
                    transcriptRepository.Update(existingTranscript);
                }
                else
                {
                    // Persist transcript metadata
                    var transcript = new Transcript
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = job.ProjectId,
                        Language = detectedLanguage,
                        Status = "Completed",
                        Version = 1,
                        BlobUrl = transcriptBlobUrl
                    };
                    await transcriptRepository.AddAsync(transcript, cancellationToken);
                }
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505")
            {
                logger.LogWarning("Concurrent insert detected for transcript: ProjectId={ProjectId}, Language={Language}. Retrying as update.", job.ProjectId, detectedLanguage);
                
                // Detach the entity to avoid EF tracking conflicts
                var trackedTranscript = dbContext.ChangeTracker.Entries<Transcript>().FirstOrDefault();
                if (trackedTranscript != null)
                {
                    trackedTranscript.State = EntityState.Detached;
                }

                // Query database directly and update
                var allTranscripts = await transcriptRepository.ListAsync(cancellationToken);
                var existingTranscript = allTranscripts.FirstOrDefault(t => 
                    t.ProjectId == job.ProjectId && 
                    t.Language == detectedLanguage && 
                    t.Version == 1);

                if (existingTranscript != null)
                {
                    existingTranscript.BlobUrl = transcriptBlobUrl;
                    existingTranscript.Status = "Completed";
                    transcriptRepository.Update(existingTranscript);
                    await unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }
        }

        job.Status = (anyCompleted, anyFailed) switch
        {
            (true, false) => JobStatuses.Completed,
            (false, true) => JobStatuses.Failed,
            _ => JobStatuses.PartiallyCompleted
        };
        job.CompletedAt = DateTimeOffset.UtcNow;
        job.StatusMessage = anyCompleted ? null : "Job failed globally on AI service.";

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Job Finalization Completed: JobId={JobId} FinalStatus={Status}", jobId, job.Status);
    }

    public async Task<IEnumerable<ProjectCaptionResponseDto>> GetCaptionsByProjectIdAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var allJobs = await jobRepository.ListAsync(cancellationToken);
        var latestJob = allJobs
            .Where(j => j.ProjectId == projectId)
            .OrderByDescending(j => j.StartedAt ?? DateTimeOffset.MinValue)
            .FirstOrDefault();

        if (latestJob is null)
        {
            return Array.Empty<ProjectCaptionResponseDto>();
        }

        var allCaptionFiles = await captionFileRepository.ListAsync(cancellationToken);
        var projectCaptions = allCaptionFiles
            .Where(cf => cf.JobId.HasValue && cf.JobId.Value == latestJob.Id)
            .Select(cf => new ProjectCaptionResponseDto
            {
                Id = cf.Id,
                JobId = cf.JobId,
                Format = cf.Format,
                Language = cf.Language,
                Status = cf.Status,
                BlobUrl = cf.BlobUrl
            })
            .ToList();

        return projectCaptions;
    }
}
