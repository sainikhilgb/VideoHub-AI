using Microsoft.AspNetCore.Http;

namespace VideoHub.Api.Application.DTOs;

public sealed class UploadMediaRequestDto
{
    public IFormFile? File { get; init; }
}
