namespace VideoHub.Api.Application.DTOs;

public sealed record UploadMediaResponseDto(
    Guid MediaId,
    Guid ProjectId,
    string UploadStatus,
    string StoragePath);
