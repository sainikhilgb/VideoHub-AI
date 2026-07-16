using Microsoft.Extensions.Options;
using VideoHub.Api.Infrastructure.Abstractions;
using VideoHub.Api.Infrastructure.Options;

namespace VideoHub.Api.Infrastructure.Storage;

public sealed class LocalBlobStorage : IBlobStorage
{
    private readonly string rootPath;

    public LocalBlobStorage(IOptions<BlobStorageOptions> options)
    {
        var storageOptions = options.Value;
        rootPath = string.IsNullOrWhiteSpace(storageOptions.LocalPath)
            ? Path.Combine(AppContext.BaseDirectory, "blobs")
            : storageOptions.LocalPath;
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

    public Task<string> EnsureFolderExistsAsync(string folderPath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(rootPath, folderPath);
        Directory.CreateDirectory(fullPath);
        return Task.FromResult(fullPath);
    }

    public Task<string?> GetSignedUrlAsync(
        string blobPath,
        TimeSpan expiry,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<string?>(blobPath);
    }
}
