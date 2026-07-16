using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VideoHub.Api.Domain.Entities;
using VideoHub.Api.Application.BackgroundJobs;
using VideoHub.Api.Infrastructure.Abstractions;
using VideoHub.Api.Infrastructure.Persistence;
using VideoHub.Api.Application.CurrentUser;
using System.Threading;

namespace VideoHub.Api.Api.Controllers;

public record CombineRequestDto(Guid MediaFileId, Guid CaptionFileId, string MuxType);
public record CombinedMediaResponseDto(Guid Id, Guid ProjectId, Guid MediaFileId, string Language, string MuxType, string Status, string? Url, string? Error, DateTimeOffset CreatedAt);
public record CombinedMediaCallbackDto(string Status, string? BlobUrl, string? Error);

[Authorize]
[ApiController]
[Route("api/v1")]
public sealed class CombinedMediaController : ControllerBase
{
    private readonly AppDbContext dbContext;
    private readonly IRepository<Project> projectRepository;
    private readonly ICurrentUserService currentUserService;
    private readonly IBlobStorage blobStorage;
    private readonly ILogger<CombinedMediaController> logger;

    public CombinedMediaController(
        AppDbContext dbContext,
        IRepository<Project> projectRepository,
        ICurrentUserService currentUserService,
        IBlobStorage blobStorage,
        ILogger<CombinedMediaController> logger)
    {
        this.dbContext = dbContext;
        this.projectRepository = projectRepository;
        this.currentUserService = currentUserService;
        this.blobStorage = blobStorage;
        this.logger = logger;
    }

    [HttpPost("projects/{projectId:guid}/combined-media/combine")]
    [ProducesResponseType(typeof(CombinedMediaResponseDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CombineMedia(
        Guid projectId,
        [FromBody] CombineRequestDto dto,
        [FromServices] IBackgroundJobService jobService,
        CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(projectId, cancellationToken);
        if (project is null) return NotFound($"Project '{projectId}' was not found.");

        if (project.UserId != currentUserService.UserId)
            return StatusCode(StatusCodes.Status403Forbidden, "You do not have permission to access this project.");

        var mediaFile = await dbContext.MediaFiles.FirstOrDefaultAsync(mf => mf.Id == dto.MediaFileId && mf.ProjectId == projectId, cancellationToken);
        if (mediaFile is null) return NotFound($"Media file '{dto.MediaFileId}' not found in this project.");

        var captionFile = await dbContext.CaptionFiles.FirstOrDefaultAsync(cf => cf.Id == dto.CaptionFileId, cancellationToken);
        if (captionFile is null) return NotFound($"Caption file '{dto.CaptionFileId}' not found.");

        // Check if a similar combined file is already processing or completed. If so, re-queue to update with new subtitles.
        var existing = await dbContext.CombinedMediaFiles
            .FirstOrDefaultAsync(cm => cm.MediaFileId == dto.MediaFileId && cm.Language == captionFile.Language && cm.MuxType == dto.MuxType, cancellationToken);

        if (existing != null)
        {
            existing.Status = "Queued";
            existing.Error = null;
            existing.CreatedAt = DateTimeOffset.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);
            jobService.QueueCombinedMediaJob(existing.Id);

            return Accepted(new CombinedMediaResponseDto(existing.Id, existing.ProjectId, existing.MediaFileId, existing.Language, existing.MuxType, existing.Status, null, null, existing.CreatedAt));
        }

        var combined = new CombinedMedia
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            MediaFileId = dto.MediaFileId,
            Language = captionFile.Language,
            MuxType = dto.MuxType,
            Status = "Queued",
            CreatedAt = DateTimeOffset.UtcNow
        };

        await dbContext.CombinedMediaFiles.AddAsync(combined, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        jobService.QueueCombinedMediaJob(combined.Id);

        return Accepted(new CombinedMediaResponseDto(combined.Id, combined.ProjectId, combined.MediaFileId, combined.Language, combined.MuxType, combined.Status, null, null, combined.CreatedAt));
    }

    [HttpGet("projects/{projectId:guid}/combined-media")]
    [ProducesResponseType(typeof(IEnumerable<CombinedMediaResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCombinedMediaFiles(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(projectId, cancellationToken);
        if (project is null) return NotFound($"Project '{projectId}' was not found.");

        if (project.UserId != currentUserService.UserId)
            return StatusCode(StatusCodes.Status403Forbidden, "You do not have permission to access this project.");

        var combinedList = await dbContext.CombinedMediaFiles
            .Where(cm => cm.ProjectId == projectId)
            .ToListAsync(cancellationToken);

        using var semaphore = new SemaphoreSlim(4);
        var tasks = combinedList.Select(async cm =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var url = cm.BlobUrl;
                if (!string.IsNullOrEmpty(url) && url.Contains("/storage/v1/object/"))
                {
                    url = await blobStorage.GetSignedUrlAsync(url, TimeSpan.FromHours(1), cancellationToken);
                }
                return new { Combined = cm, Url = url };
            }
            finally
            {
                semaphore.Release();
            }
        });

        var results = await Task.WhenAll(tasks);

        var dtos = results.Select(r => new CombinedMediaResponseDto(
            r.Combined.Id,
            r.Combined.ProjectId,
            r.Combined.MediaFileId,
            r.Combined.Language,
            r.Combined.MuxType,
            r.Combined.Status,
            r.Url,
            r.Combined.Error,
            r.Combined.CreatedAt));

        return Ok(dtos);
    }

    [HttpPost("combined-media/{id:guid}/status")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] CombinedMediaCallbackDto dto,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Combined Media Status Callback: Id={Id} Status={Status}", id, dto.Status);

        var combined = await dbContext.CombinedMediaFiles.FirstOrDefaultAsync(cm => cm.Id == id, cancellationToken);
        if (combined is null) return NotFound($"Combined media export '{id}' was not found.");

        combined.Status = dto.Status;
        if (!string.IsNullOrEmpty(dto.BlobUrl))
        {
            combined.BlobUrl = dto.BlobUrl;
        }
        if (!string.IsNullOrEmpty(dto.Error))
        {
            combined.Error = dto.Error;
        }
        
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
