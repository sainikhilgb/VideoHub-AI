using VideoHub.Api.Infrastructure.Abstractions;

namespace VideoHub.Api.Infrastructure.Storage;

public sealed class LocalBlobStorage : IBlobStorage
{
    private readonly string rootPath;

    public LocalBlobStorage(IConfiguration configuration)
    {
        rootPath = configuration["BlobStorage:LocalPath"] ?? Path.Combine(AppContext.BaseDirectory, "blobs");
        Directory.CreateDirectory(rootPath);
    }

    public async Task DeleteAsync(string blobName, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(rootPath, blobName);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        await Task.CompletedTask;
    }

    public async Task<string> UploadAsync(Stream content, string blobName, string contentType, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(rootPath, blobName);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? rootPath);

        await using var fileStream = File.Create(fullPath);
        await content.CopyToAsync(fileStream, cancellationToken);
        return fullPath;
    }
}
