using Microsoft.AspNetCore.Mvc;
using VideoHub.Api.Application.Captions;
using VideoHub.Api.Infrastructure.Abstractions;
using VideoHub.Api.Domain.Entities;

namespace VideoHub.Api.Api.Controllers;

[ApiController]
[Route("api/v1/jobs")]
public sealed class JobsController : ControllerBase
{
    private readonly ICaptionService captionService;
    private readonly IRepository<Job> jobRepository;
    private readonly IRepository<CaptionFile> captionFileRepository;
    private readonly ILogger<JobsController> logger;

    public JobsController(
        ICaptionService captionService,
        IRepository<Job> jobRepository,
        IRepository<CaptionFile> captionFileRepository,
        ILogger<JobsController> logger)
    {
        this.captionService = captionService;
        this.jobRepository = jobRepository;
        this.captionFileRepository = captionFileRepository;
        this.logger = logger;
    }

    /// <summary>
    /// Receives the final processing summary from the Python AI service after all languages have settled.
    /// Persists transcript data and sets the parent job's terminal status.
    /// </summary>
    [HttpPost("{jobId:guid}/callback")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ProcessCallback(
        Guid jobId,
        [FromBody] AiProcessCallbackDto dto,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Job Callback Received: JobId={JobId} DetectedLanguage={Lang}", jobId, dto.DetectedLanguage);

        await captionService.FinalizeJobAsync(jobId, dto.DetectedLanguage, dto.Segments, cancellationToken);

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
