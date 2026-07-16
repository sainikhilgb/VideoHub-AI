using Microsoft.AspNetCore.Mvc;
using VideoHub.Api.Application.Captions;
using VideoHub.Api.Application.Exceptions;
using VideoHub.Api.Infrastructure.Abstractions;
using VideoHub.Api.Domain.Entities;
using VideoHub.Api.Application.CurrentUser;

namespace VideoHub.Api.Api.Controllers;

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
        CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(projectId, cancellationToken);
        if (project is null) return NotFound($"Project '{projectId}' was not found.");

        if (project.UserId != currentUserService.UserId)
            return StatusCode(StatusCodes.Status403Forbidden, "You do not have permission to access this project.");

        var captions = await captionService.GetCaptionsByProjectIdAsync(projectId, cancellationToken);
        return Ok(captions);
    }
}
