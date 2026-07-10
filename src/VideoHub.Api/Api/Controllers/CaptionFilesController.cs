using Microsoft.AspNetCore.Mvc;
using VideoHub.Api.Application.Captions;
using VideoHub.Api.Application.Exceptions;

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
}
