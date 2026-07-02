namespace VideoHub.Api.Infrastructure.Options;

public sealed class BlobStorageOptions
{
    public const string SectionName = "BlobStorage";

    public string Provider { get; set; } = "Local";

    public string LocalPath { get; set; } = "blobs";

    public string? SupabaseUrl { get; set; }

    public string? SupabaseKey { get; set; }

    public string? BucketName { get; set; }
}
