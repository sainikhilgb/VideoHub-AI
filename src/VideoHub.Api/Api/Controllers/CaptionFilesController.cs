using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VideoHub.Api.Application.Captions;
using VideoHub.Api.Application.Exceptions;
using VideoHub.Api.Infrastructure.Abstractions;
using VideoHub.Api.Domain.Entities;
using VideoHub.Api.Application.CurrentUser;
using System.Threading;
using System.Linq;
using System.Collections.Generic;

namespace VideoHub.Api.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/caption-files")]
public sealed class CaptionFilesController : ControllerBase
{
    private readonly ICaptionService captionService;
    private readonly ILogger<CaptionFilesController> logger;

    public CaptionFilesController(ICaptionService captionService, ILogger<CaptionFilesController> logger)
    {
        this.captionService = captionService;
        this.logger = logger;
    }

    /// <summary>
    /// Receives per-language status updates from the Python AI service as each caption format completes.
    /// </summary>
    [HttpPost("{captionFileId:guid}/status")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        Guid captionFileId,
        [FromBody] CaptionStatusCallbackDto dto,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Caption Status Callback Received: CaptionFileId={CaptionFileId} Status={Status}", captionFileId, dto.Status);

        await captionService.UpdateCaptionFileStatusAsync(
            captionFileId,
            dto.Status,
            dto.BlobUrl,
            dto.Error,
            cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Retrieves all caption files associated with the specified project.
    /// </summary>
    [HttpGet("project/{projectId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<ProjectCaptionResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCaptionFilesByProject(
        Guid projectId,
        [FromServices] IRepository<Project> projectRepository,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] IBlobStorage blobStorage,
        CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(projectId, cancellationToken);
        if (project is null) return NotFound($"Project '{projectId}' was not found.");

        if (project.UserId != currentUserService.UserId)
            return StatusCode(StatusCodes.Status403Forbidden, "You do not have permission to access this project.");

        var captions = await captionService.GetCaptionsByProjectIdAsync(projectId, cancellationToken);

        using var semaphore = new SemaphoreSlim(4);
        var tasks = captions.Select(async c =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var url = c.BlobUrl;
                if (!string.IsNullOrEmpty(url) && url.Contains("/storage/v1/object/"))
                {
                    url = await blobStorage.GetSignedUrlAsync(url, TimeSpan.FromHours(1), cancellationToken);
                }
                c.BlobUrl = url;
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        return Ok(captions);
    }
}
