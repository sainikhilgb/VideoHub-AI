using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using VideoHub.Api.Application.Captions;
using VideoHub.Api.Application.CurrentUser;
using VideoHub.Api.Infrastructure.Abstractions;
using VideoHub.Api.Infrastructure.Authentication;
using VideoHub.Api.Domain.Entities;

namespace VideoHub.Api.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/jobs")]
public sealed class JobsController : ControllerBase
{
    private readonly ICaptionService captionService;
    private readonly IRepository<Job> jobRepository;
    private readonly IRepository<CaptionFile> captionFileRepository;
    private readonly IRepository<Project> projectRepository;
    private readonly ICurrentUserService currentUserService;
    private readonly IConfiguration configuration;
    private readonly ILogger<JobsController> logger;

    public JobsController(
        ICaptionService captionService,
        IRepository<Job> jobRepository,
        IRepository<CaptionFile> captionFileRepository,
        IRepository<Project> projectRepository,
        ICurrentUserService currentUserService,
        IConfiguration configuration,
        ILogger<JobsController> logger)
    {
        this.captionService = captionService;
        this.jobRepository = jobRepository;
        this.captionFileRepository = captionFileRepository;
        this.projectRepository = projectRepository;
        this.currentUserService = currentUserService;
        this.configuration = configuration;
        this.logger = logger;
    }

    /// <summary>
    /// Receives the final processing summary from the Python AI service after all languages have settled.
    /// Persists transcript data and sets the parent job's terminal status.
    /// </summary>
    [HttpPost("{jobId:guid}/callback")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ProcessCallback(
        Guid jobId,
        [FromBody] AiProcessCallbackDto dto,
        [FromQuery] string? secret,
        [FromServices] IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        var expectedSecret = AiCallbackSecretResolver.ResolveSecret(configuration, environment);
        if (string.IsNullOrEmpty(expectedSecret))
        {
            logger.LogError("Callback secret is not configured in the host environment.");
            return StatusCode(StatusCodes.Status500InternalServerError, "Callback secret is not configured.");
        }

        if (string.IsNullOrEmpty(secret))
        {
            if (Request.Headers.TryGetValue("X-Callback-Secret", out var headerSecret))
            {
                secret = headerSecret;
            }
        }

        if (secret != expectedSecret)
        {
            logger.LogWarning("Unauthorized job callback attempt: JobId={JobId}", jobId);
            return Unauthorized("Invalid callback credentials.");
        }

        if (!string.IsNullOrEmpty(dto.TranscriptBlobUrl))
        {
            var configuredOrigin = configuration["BlobStorage:SupabaseUrl"];
            if (string.IsNullOrEmpty(configuredOrigin))
            {
                logger.LogError("Supabase URL is not configured.");
                return StatusCode(StatusCodes.Status500InternalServerError, "Blob storage URL is not configured.");
            }

            if (!Uri.TryCreate(configuredOrigin, UriKind.Absolute, out var originUri) ||
                !Uri.TryCreate(dto.TranscriptBlobUrl, UriKind.Absolute, out var uri) ||
                uri.Scheme != Uri.UriSchemeHttps ||
                !string.Equals(originUri.Host, uri.Host, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("Invalid or mismatched transcript blob URL: {Url}", dto.TranscriptBlobUrl);
                return BadRequest("Invalid transcript blob URL or origin mismatch.");
            }

            var path = uri.AbsolutePath;
            var prefix = "/storage/v1/object/authenticated/";
            var publicPrefix = "/storage/v1/object/public/";
            var actualPrefix = path.Contains(prefix) ? prefix : (path.Contains(publicPrefix) ? publicPrefix : null);

            if (actualPrefix == null)
            {
                logger.LogWarning("Invalid storage URL structure: {Url}", dto.TranscriptBlobUrl);
                return BadRequest("Invalid transcript blob URL structure.");
            }

            var relativePath = path.Substring(path.IndexOf(actualPrefix) + actualPrefix.Length);
            var parts = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 1)
            {
                logger.LogWarning("Missing bucket segment in URL: {Url}", dto.TranscriptBlobUrl);
                return BadRequest("Invalid transcript blob URL bucket.");
            }

            var bucket = parts[0];
            var configuredBucket = configuration["BlobStorage:BucketName"] ?? "media";
            if (!string.Equals(bucket, configuredBucket, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("Bucket name mismatch: {Bucket} (expected {ConfiguredBucket})", bucket, configuredBucket);
                return BadRequest("Invalid transcript blob URL bucket.");
            }
        }

        logger.LogInformation("Job Callback Received: JobId={JobId} DetectedLanguage={Lang}", jobId, dto.DetectedLanguage);

        await captionService.FinalizeJobAsync(jobId, dto.DetectedLanguage, dto.TranscriptBlobUrl, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Returns the current status of a job and all of its caption files for frontend polling.
    /// </summary>
    [HttpGet("{jobId:guid}")]
    [ProducesResponseType(typeof(JobStatusResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetJobStatus(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await jobRepository.GetByIdAsync(jobId, cancellationToken);
        if (job is null) return NotFound();

        var project = await projectRepository.GetByIdAsync(job.ProjectId, cancellationToken);
        if (project is null || project.UserId != currentUserService.UserId)
            return StatusCode(StatusCodes.Status403Forbidden, "You do not have permission to access this job.");

        var allCaptionFiles = (await captionFileRepository.ListAsync(cancellationToken))
            .Where(cf => cf.JobId == jobId)
            .GroupBy(cf => cf.Language)
            .Select(group => new LanguageStatusDto
            {
                LanguageCode = group.Key,
                Captions = group.Select(cf => new CaptionFileStatusDto
                {
                    CaptionFileId = cf.Id,
                    Format = cf.Format,
                    Status = cf.Status,
                    BlobUrl = cf.BlobUrl,
                    ErrorMessage = cf.ErrorMessage
                }).ToList()
            })
            .ToList();

        return Ok(new JobStatusResponseDto
        {
            JobId = job.Id,
            Status = job.Status,
            StatusMessage = job.StatusMessage,
            StartedAt = job.StartedAt,
            CompletedAt = job.CompletedAt,
            Languages = allCaptionFiles
        });
    }
}

// Response DTOs for the polling endpoint
public sealed class JobStatusResponseDto
{
    public Guid JobId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? StatusMessage { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public List<LanguageStatusDto> Languages { get; set; } = [];
}

public sealed class LanguageStatusDto
{
    public string LanguageCode { get; set; } = string.Empty;
    public List<CaptionFileStatusDto> Captions { get; set; } = [];
}

public sealed class CaptionFileStatusDto
{
    public Guid CaptionFileId { get; set; }
    public string Format { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? BlobUrl { get; set; }
    public string? ErrorMessage { get; set; }
}
