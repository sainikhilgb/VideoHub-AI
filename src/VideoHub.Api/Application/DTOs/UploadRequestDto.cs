namespace VideoHub.Api.Application.DTOs;

public sealed record UploadRequestDto(
    string FileName,
    string ContentType,
    long FileSizeBytes);
