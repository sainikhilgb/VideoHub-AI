namespace VideoHub.Api.Application.Commands;

public sealed record SubmitUploadCommand(
    string FileName,
    string ContentType,
    long FileSizeBytes);
