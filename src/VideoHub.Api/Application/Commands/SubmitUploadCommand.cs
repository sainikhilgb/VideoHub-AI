namespace VideoHub.Api.Application.Commands;

public sealed record SubmitUploadCommand(
    Guid ProjectId,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes,
    string Extension,
    Stream Content);
