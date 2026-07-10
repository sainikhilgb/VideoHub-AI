using Microsoft.AspNetCore.Mvc;
using VideoHub.Api.Application.BackgroundJobs;
using VideoHub.Api.Application.Commands;
using VideoHub.Api.Application.DTOs;
using VideoHub.Api.Application.Uploads;
using VideoHub.Api.Domain.Entities;
using VideoHub.Api.Domain.Jobs;
using VideoHub.Api.Infrastructure.Abstractions;

namespace VideoHub.Api.Api.Controllers;

[ApiController]
[Route("api/v1/projects/{projectId:guid}/media")]
public sealed class ProjectMediaController : ControllerBase
{
    private readonly IMediaUploadService mediaUploadService;
    private readonly IBackgroundJobService backgroundJobService;
    private readonly IRepository<Project> projectRepository;
    private readonly IRepository<MediaFile> mediaFileRepository;
    private readonly IRepository<Job> jobRepository;
    private readonly IUnitOfWork unitOfWork;

    public ProjectMediaController(
        IMediaUploadService mediaUploadService,
        IBackgroundJobService backgroundJobService,
        IRepository<Project> projectRepository,
        IRepository<MediaFile> mediaFileRepository,
        IRepository<Job> jobRepository,
        IUnitOfWork unitOfWork)
    {
        this.mediaUploadService = mediaUploadService;
        this.backgroundJobService = backgroundJobService;
        this.projectRepository = projectRepository;
        this.mediaFileRepository = mediaFileRepository;
        this.jobRepository = jobRepository;
        this.unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Uploads a media file for the project (Metadata saved, file uploaded, no AI job queued).
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(UploadMediaResponseDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadAsync(
        [FromRoute] Guid projectId,
        [FromForm] UploadMediaRequestDto request,
        CancellationToken cancellationToken)
    {
        var file = request.File!;

        await using var stream = file.OpenReadStream();
        var command = new SubmitUploadCommand(
            projectId,
            file.FileName,
            file.ContentType,
            file.Length,
            Path.GetExtension(file.FileName),
            stream);

        var response = await mediaUploadService.UploadAsync(command, cancellationToken);
        return Accepted(response);
    }

    /// <summary>
    /// Explicitly triggers the caption generation process for an uploaded media file.
    /// </summary>
    [HttpPost("{mediaId:guid}/captions")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateCaptionsAsync(
        [FromRoute] Guid projectId,
        [FromRoute] Guid mediaId,
        [FromBody] GenerateCaptionsRequestDto request,
        CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(projectId, cancellationToken);
        if (project is null) return NotFound($"Project '{projectId}' was not found.");

        var mediaFile = await mediaFileRepository.GetByIdAsync(mediaId, cancellationToken);
        if (mediaFile is null || mediaFile.ProjectId != projectId)
            return NotFound($"Media file '{mediaId}' was not found under project '{projectId}'.");

        var targetLanguages = request.TargetLanguages ?? new List<string>();
        if (targetLanguages.Count == 0)
        {
            // Default to project original language if none specified
            targetLanguages.Add(project.OriginalLanguage);
        }

        var processingJob = new Job
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Type = JobTypes.MediaProcessing,
            Status = JobStatuses.Queued,
            TargetLanguages = string.Join(",", targetLanguages)
        };

        await jobRepository.AddAsync(processingJob, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        string? correlationId = null;
        if (HttpContext.Items.TryGetValue(VideoHub.Api.Infrastructure.Middleware.CorrelationIdMiddleware.HeaderName, out var corrObj))
        {
            correlationId = corrObj?.ToString();
        }

        var hangfireJobId = backgroundJobService.QueueMediaProcessingJob(processingJob.Id, mediaFile.Id, correlationId);

        return Accepted(new { JobId = processingJob.Id, HangfireJobId = hangfireJobId });
    }
}
