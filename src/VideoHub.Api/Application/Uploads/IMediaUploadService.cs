using VideoHub.Api.Application.Commands;
using VideoHub.Api.Application.DTOs;

namespace VideoHub.Api.Application.Uploads;

public interface IMediaUploadService
{
    Task<UploadMediaResponseDto> UploadAsync(SubmitUploadCommand command, CancellationToken cancellationToken = default);
}
