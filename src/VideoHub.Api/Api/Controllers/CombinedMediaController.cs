using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using VideoHub.Api.Domain.Entities;
using VideoHub.Api.Application.BackgroundJobs;
using VideoHub.Api.Infrastructure.Abstractions;
using VideoHub.Api.Infrastructure.Authentication;
using VideoHub.Api.Infrastructure.Persistence;
using VideoHub.Api.Application.CurrentUser;
using System.Threading;
using VideoHub.Api.Infrastructure.BackgroundJobs;

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
    private readonly IBackgroundJobService backgroundJobService;
    private readonly ILogger<CombinedMediaController> logger;

    public CombinedMediaController(
        AppDbContext dbContext,
        IRepository<Project> projectRepository,
        ICurrentUserService currentUserService,
        IBlobStorage blobStorage,
        IBackgroundJobService backgroundJobService,
        ILogger<CombinedMediaController> logger)
    {
        this.dbContext = dbContext;
        this.projectRepository = projectRepository;
        this.currentUserService = currentUserService;
        this.blobStorage = blobStorage;
        this.backgroundJobService = backgroundJobService;
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
        if (!string.Equals(dto.MuxType, "SoftMux", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Invalid muxType. Only 'SoftMux' is supported.");
        }

        dto = dto with { MuxType = "SoftMux" };

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
            existing.BlobUrl = null;
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

        try
        {
            await dbContext.CombinedMediaFiles.AddAsync(combined, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505")
        {
            dbContext.Entry(combined).State = EntityState.Detached;
            logger.LogWarning("Concurrent insert detected for combined media MediaFileId={MediaFileId} Language={Language} MuxType={MuxType}. Re-queuing existing entry.", dto.MediaFileId, captionFile.Language, dto.MuxType);
            var concurrentExisting = await dbContext.CombinedMediaFiles
                .FirstOrDefaultAsync(cm => cm.MediaFileId == dto.MediaFileId && cm.Language == captionFile.Language && cm.MuxType == dto.MuxType, cancellationToken);
            if (concurrentExisting != null)
            {
                concurrentExisting.Status = "Queued";
                concurrentExisting.Error = null;
                concurrentExisting.BlobUrl = null;
                concurrentExisting.CreatedAt = DateTimeOffset.UtcNow;
                await dbContext.SaveChangesAsync(cancellationToken);

                jobService.QueueCombinedMediaJob(concurrentExisting.Id);
                return Accepted(new CombinedMediaResponseDto(concurrentExisting.Id, concurrentExisting.ProjectId, concurrentExisting.MediaFileId, concurrentExisting.Language, concurrentExisting.MuxType, concurrentExisting.Status, null, null, concurrentExisting.CreatedAt));
            }
            throw;
        }

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
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] CombinedMediaCallbackDto dto,
        [FromServices] IConfiguration configuration,
        [FromServices] IHostEnvironment environment,
        [FromServices] IHubContext<ProjectHub> hubContext,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Combined Media Status Callback: Id={Id} Status={Status}", id, dto.Status);

        var callbackSecret = AiCallbackSecretResolver.ResolveSecret(configuration, environment);
        if (string.IsNullOrWhiteSpace(callbackSecret))
        {
            logger.LogError("AiService:CallbackSecret is not configured. Callback rejected.");
            return StatusCode(StatusCodes.Status500InternalServerError, "Server authorization config is missing.");
        }

        if (!Request.Headers.TryGetValue("Authorization", out var authHeader) || 
            !authHeader.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Unauthorized("Missing or invalid authorization header scheme.");
        }

        var incomingSecret = authHeader.ToString().Substring("Bearer ".Length).Trim();
        if (incomingSecret != callbackSecret)
        {
            logger.LogWarning("Unauthorized callback attempt with secret mismatch for Id={Id}.", id);
            return Unauthorized("Invalid callback secret.");
        }

        var combined = await dbContext.CombinedMediaFiles.FirstOrDefaultAsync(cm => cm.Id == id, cancellationToken);
        if (combined is null) return NotFound($"Combined media export '{id}' was not found.");

        // Validate status transition (only permit from Queued or Processing, and only target Processing, Completed, or Failed)
        if (combined.Status == "Completed" || combined.Status == "Failed")
        {
            logger.LogWarning("Callback rejected: Combined media status '{CurrentStatus}' is already final for Id={Id}.", combined.Status, id);
            return BadRequest($"Combined media '{id}' has already reached a final status '{combined.Status}'.");
        }

        var targetStatus = dto.Status;
        if (targetStatus != "Processing" && targetStatus != "Completed" && targetStatus != "Failed")
        {
            return BadRequest($"Invalid status transition target '{targetStatus}'.");
        }

        // Restrict BlobUrl to storage-owned paths containing the configured bucket
        if (targetStatus == "Completed")
        {
            if (string.IsNullOrEmpty(dto.BlobUrl))
            {
                return BadRequest("BlobUrl is required for status Completed.");
            }

            if (!Uri.TryCreate(dto.BlobUrl, UriKind.Absolute, out var uri))
            {
                return BadRequest("Invalid blob URL format.");
            }

            var path = uri.AbsolutePath;
            var prefix = "/storage/v1/object/authenticated/";
            var publicPrefix = "/storage/v1/object/public/";
            var actualPrefix = path.Contains(prefix) ? prefix : (path.Contains(publicPrefix) ? publicPrefix : null);

            if (actualPrefix == null)
            {
                return BadRequest("Invalid storage URL prefix structure.");
            }

            var relativePath = path.Substring(path.IndexOf(actualPrefix) + actualPrefix.Length);
            var parts = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 1)
            {
                return BadRequest("Missing bucket segment in storage URL.");
            }

            var bucket = parts[0];
            var configuredBucket = configuration["BlobStorage:BucketName"] ?? "media";
            if (!string.Equals(bucket, configuredBucket, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("Callback rejected: Bucket name mismatch: {Bucket} (expected {ConfiguredBucket})", bucket, configuredBucket);
                return BadRequest("Invalid storage URL bucket.");
            }
        }

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

        await hubContext.Clients.Group($"project_{combined.ProjectId}")
            .SendAsync("ReceiveCombinedMediaUpdate", new
            {
                Id = combined.Id,
                ProjectId = combined.ProjectId,
                MediaFileId = combined.MediaFileId,
                Language = combined.Language,
                MuxType = combined.MuxType,
                Status = combined.Status,
                BlobUrl = combined.BlobUrl,
                Error = combined.Error,
                CreatedAt = combined.CreatedAt
            }, cancellationToken);

        return NoContent();
    }
}
