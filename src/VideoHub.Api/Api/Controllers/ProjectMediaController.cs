using Microsoft.AspNetCore.Mvc;
using VideoHub.Api.Application.Commands;
using VideoHub.Api.Application.DTOs;
using VideoHub.Api.Application.Uploads;

namespace VideoHub.Api.Api.Controllers;

[ApiController]
[Route("api/v1/projects/{projectId:guid}/media")]
public sealed class ProjectMediaController : ControllerBase
{
    private readonly IMediaUploadService mediaUploadService;

    public ProjectMediaController(IMediaUploadService mediaUploadService)
    {
        this.mediaUploadService = mediaUploadService;
    }

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
}
