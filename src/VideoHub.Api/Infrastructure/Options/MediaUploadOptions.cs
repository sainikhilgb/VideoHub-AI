namespace VideoHub.Api.Infrastructure.Options;

public sealed class MediaUploadOptions
{
    public const long VideoMaxBytes = 2L * 1024 * 1024 * 1024;
    public const long AudioMaxBytes = 500L * 1024 * 1024;
}
