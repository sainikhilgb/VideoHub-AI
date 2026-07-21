using Hangfire;
using Microsoft.Extensions.Logging;
using VideoHub.Api.Application.Captions;
using VideoHub.Api.Domain.Entities;
using VideoHub.Api.Domain.Jobs;
using VideoHub.Api.Domain.Media;
using VideoHub.Api.Infrastructure.Abstractions;
using VideoHub.Api.Infrastructure.Authentication;
using VideoHub.Api.Infrastructure.Persistence;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;

namespace VideoHub.Api.Application.BackgroundJobs;

public sealed class BackgroundJobService : IBackgroundJobService
{
    private readonly IBackgroundJobClient backgroundJobClient;
    private readonly IRecurringJobManager recurringJobManager;
    private readonly IRepository<Job> jobRepository;
    private readonly IRepository<MediaFile> mediaFileRepository;
    private readonly IRepository<Project> projectRepository;
    private readonly ICaptionService captionService;
    private readonly IUnitOfWork unitOfWork;
    private readonly AppDbContext dbContext;
    private readonly IBlobStorage blobStorage;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly IConfiguration configuration;
    private readonly IHostEnvironment environment;
    private readonly ILogger<BackgroundJobService> logger;

    public BackgroundJobService(
        IBackgroundJobClient backgroundJobClient,
        IRecurringJobManager recurringJobManager,
        IRepository<Job> jobRepository,
        IRepository<MediaFile> mediaFileRepository,
        IRepository<Project> projectRepository,
        ICaptionService captionService,
        IUnitOfWork unitOfWork,
        AppDbContext dbContext,
        IBlobStorage blobStorage,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<BackgroundJobService> logger)
    {
        this.backgroundJobClient = backgroundJobClient;
        this.recurringJobManager = recurringJobManager;
        this.jobRepository = jobRepository;
        this.mediaFileRepository = mediaFileRepository;
        this.projectRepository = projectRepository;
        this.captionService = captionService;
        this.unitOfWork = unitOfWork;
        this.dbContext = dbContext;
        this.blobStorage = blobStorage;
        this.httpClientFactory = httpClientFactory;
        this.configuration = configuration;
        this.environment = environment;
        this.logger = logger;
    }

    public string QueueContinuationHelloWorld()
    {
        logger.LogInformation("Job Queued: Continuation job chain");

        var parentJobId = backgroundJobClient.Enqueue(() => ExecuteHelloWorldAsync("Fire-and-Forget", CancellationToken.None));
        return backgroundJobClient.ContinueJobWith(parentJobId, () => ExecuteHelloWorldAsync("Continuation", CancellationToken.None));
    }

    public string QueueHelloWorld()
    {
        logger.LogInformation("Job Queued: Fire-and-forget job");
        return backgroundJobClient.Enqueue(() => ExecuteHelloWorldAsync("Fire-and-Forget", CancellationToken.None));
    }

    public string QueueMediaProcessingJob(Guid jobId, Guid mediaFileId, string? correlationId = null)
    {
        logger.LogInformation("Job Queued: Media processing JobId={JobId} MediaId={MediaId}", jobId, mediaFileId);
        return backgroundJobClient.Enqueue(() => ExecuteMediaProcessingAsync(jobId, mediaFileId, correlationId, CancellationToken.None));
    }

    public string RegisterRecurringHelloWorld()
    {
        const string recurringJobId = "hello-world-recurring";
        logger.LogInformation("Job Queued: Recurring job {RecurringJobId}", recurringJobId);

        recurringJobManager.AddOrUpdate(
            recurringJobId,
            () => ExecuteHelloWorldAsync("Recurring", CancellationToken.None),
            Cron.Minutely);

        return recurringJobId;
    }

    public string ScheduleHelloWorld(TimeSpan delay)
    {
        logger.LogInformation("Job Queued: Delayed job with delay {Delay}", delay);
        return backgroundJobClient.Schedule(() => ExecuteHelloWorldAsync("Delayed", CancellationToken.None), delay);
    }

    public async Task ExecuteHelloWorldAsync(string jobType, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Job Started: {JobType}", jobType);

        try
        {
            await Task.Delay(250, cancellationToken);
            logger.LogInformation("Hello from Hangfire. JobType={JobType}", jobType);
            logger.LogInformation("Job Completed: {JobType}", jobType);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Job Failed: {JobType}", jobType);
            throw;
        }
    }

    public Task ExecuteMediaProcessingAsync(Guid jobId, Guid mediaFileId, CancellationToken cancellationToken)
    {
        return ExecuteMediaProcessingAsync(jobId, mediaFileId, null, cancellationToken);
    }

    public async Task ExecuteMediaProcessingAsync(Guid jobId, Guid mediaFileId, string? correlationId = null, CancellationToken cancellationToken = default)
    {
        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        using (Serilog.Context.LogContext.PushProperty("JobId", jobId.ToString()))
        using (Serilog.Context.LogContext.PushProperty("MediaId", mediaFileId.ToString()))
        {
            logger.LogInformation("Job Started: Media processing JobId={JobId} MediaId={MediaId}", jobId, mediaFileId);

            var job = await jobRepository.GetByIdAsync(jobId, cancellationToken);
            if (job is null)
            {
                logger.LogWarning("Job Failed: Media processing references missing JobId={JobId} MediaId={MediaId}", jobId, mediaFileId);
                return;
            }

            var mediaFile = await mediaFileRepository.GetByIdAsync(mediaFileId, cancellationToken);
            if (mediaFile is null)
            {
                logger.LogWarning("Job Failed: Media processing references missing JobId={JobId} MediaId={MediaId}", jobId, mediaFileId);
                job.Status = JobStatuses.Failed;
                job.StatusMessage = $"Media file not found: {mediaFileId}";
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return;
            }

            var project = await projectRepository.GetByIdAsync(job.ProjectId, cancellationToken);
            if (project is null)
            {
                logger.LogWarning("Job Failed: Project not found ProjectId={ProjectId} JobId={JobId}", job.ProjectId, jobId);
                job.Status = JobStatuses.Failed;
                job.StatusMessage = $"Project not found: {job.ProjectId}";
                mediaFile.Status = MediaFileStatuses.Failed;
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return;
            }

            using (Serilog.Context.LogContext.PushProperty("ProjectId", project.Id.ToString()))
            {
                try
                {
                    mediaFile.Status = MediaFileStatuses.Processing;
                    job.StartedAt = DateTimeOffset.UtcNow;
                    await unitOfWork.SaveChangesAsync(cancellationToken);

                    // Resolve target languages from the job configuration, falling back to original language if none specified.
                    var targetLanguages = !string.IsNullOrWhiteSpace(job.TargetLanguages)
                        ? job.TargetLanguages.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(lang => lang.Trim()).ToList()
                        : new List<string> { project.OriginalLanguage };

                    await captionService.DispatchCaptionGenerationAsync(
                        jobId,
                        project.Id,
                        mediaFile.Id,
                        project.UserId,
                        mediaFile.StoragePath,
                        mediaFile.Type,
                        mediaFile.Bucket,
                        project.OriginalLanguage,
                        targetLanguages,
                        correlationId,
                        cancellationToken);

                    logger.LogInformation("Job Dispatched: Media processing JobId={JobId} MediaId={MediaId}", jobId, mediaFileId);
                }
                catch (Exception exception)
                {
                    job.Status = JobStatuses.Failed;
                    job.Attempts += 1;
                    job.StatusMessage = exception.Message != null && exception.Message.Length > 500
                        ? exception.Message.Substring(0, 500)
                        : exception.Message;
                    mediaFile.Status = MediaFileStatuses.Failed;
                    await unitOfWork.SaveChangesAsync(cancellationToken);

                    logger.LogError(exception, "Job Failed: Media processing JobId={JobId} MediaId={MediaId}", jobId, mediaFileId);
                    throw;
                }
            }
        }
    }

    public string QueueCombinedMediaJob(Guid combinedMediaId)
    {
        logger.LogInformation("Job Queued: Combined media processing CombinedMediaId={CombinedMediaId}", combinedMediaId);
        return backgroundJobClient.Enqueue(() => ExecuteCombinedMediaJobAsync(combinedMediaId, CancellationToken.None));
    }

    public async Task ExecuteCombinedMediaJobAsync(Guid combinedMediaId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Job Started: Combined media processing CombinedMediaId={CombinedMediaId}", combinedMediaId);

        var combined = await dbContext.CombinedMediaFiles.FirstOrDefaultAsync(cm => cm.Id == combinedMediaId, cancellationToken);
        if (combined is null)
        {
            logger.LogWarning("Job Cancelled: Combined media record not found CombinedMediaId={CombinedMediaId}", combinedMediaId);
            return;
        }

        try
        {
            combined.Status = JobStatuses.Processing;
            await dbContext.SaveChangesAsync(cancellationToken);

            var mediaFile = await mediaFileRepository.GetByIdAsync(combined.MediaFileId, cancellationToken);
            if (mediaFile is null)
            {
                throw new Exception($"Media file '{combined.MediaFileId}' not found.");
            }

            var project = await projectRepository.GetByIdAsync(combined.ProjectId, cancellationToken);
            if (project is null)
            {
                throw new Exception($"Project '{combined.ProjectId}' not found.");
            }

            // Resolve project's job IDs to map caption files precisely to this project
            var projectJobIds = await dbContext.Jobs
                .Where(j => j.ProjectId == combined.ProjectId)
                .Select(j => j.Id)
                .ToListAsync(cancellationToken);

            // Find completed caption SRT format for the target language belonging to this project
            var captionFile = await dbContext.CaptionFiles
                .FirstOrDefaultAsync(cf => cf.JobId.HasValue && projectJobIds.Contains(cf.JobId.Value) && cf.Language == combined.Language && cf.Format == "srt" && cf.Status == "Completed", cancellationToken);

            if (captionFile is null)
            {
                throw new Exception($"No completed SRT captions found for project '{combined.ProjectId}' in language '{combined.Language}'.");
            }

            // Generate signed URLs so the Python service can download both files securely via standard HTTP GET requests
            var videoUrl = await blobStorage.GetSignedUrlAsync(mediaFile.StoragePath, TimeSpan.FromHours(2), cancellationToken);
            var subtitleUrl = await blobStorage.GetSignedUrlAsync(captionFile.BlobUrl!, TimeSpan.FromHours(2), cancellationToken);

            if (string.IsNullOrEmpty(videoUrl) || string.IsNullOrEmpty(subtitleUrl))
            {
                throw new Exception("Failed to generate signed URLs for media or subtitle track.");
            }

            var callbackSecret = AiCallbackSecretResolver.ResolveSecret(configuration, environment)
                ?? throw new InvalidOperationException("Configuration 'AiService:CallbackSecret' is missing or blank.");

            var callbackUrl = $"/api/v1/combined-media/{combinedMediaId}/status";

            // Target storage folder
            var outputFolder = $"{project.UserId}/{project.Id}/combined/{combined.Language}/";
            var outputName = $"{combined.MuxType.ToLower()}_combined.mp4";

            var payload = new
            {
                combinedMediaId = combined.Id,
                videoUrl = videoUrl,
                subtitleUrl = subtitleUrl,
                muxType = combined.MuxType,
                language = combined.Language,
                bucket = mediaFile.Bucket,
                outputFolder = outputFolder,
                outputName = outputName,
                callbackUrl = callbackUrl,
                callbackSecret = callbackSecret
            };

            var httpClient = httpClientFactory.CreateClient("AiService");
            var response = await httpClient.PostAsJsonAsync("process-combine", payload, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new Exception($"AI service returned {response.StatusCode}: {body}");
            }

            logger.LogInformation("Job Dispatched: Combined media processing CombinedMediaId={CombinedMediaId}", combinedMediaId);
        }
        catch (Exception exception)
        {
            combined.Status = JobStatuses.Failed;
            combined.Error = exception.Message;
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogError(exception, "Job Failed: Combined media processing CombinedMediaId={CombinedMediaId}", combinedMediaId);
            throw;
        }
    }
}
