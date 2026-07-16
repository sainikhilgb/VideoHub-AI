using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using VideoHub.Api.Infrastructure.Abstractions;
using VideoHub.Api.Infrastructure.Options;
using System.IO;

namespace VideoHub.Api.Infrastructure.Storage;

public sealed class LocalBlobStorage : IBlobStorage
{
    private readonly string rootPath;
    private readonly IConfiguration configuration;

    public LocalBlobStorage(IOptions<BlobStorageOptions> options, IConfiguration configuration)
    {
        var storageOptions = options.Value;
        rootPath = string.IsNullOrWhiteSpace(storageOptions.LocalPath)
            ? Path.Combine(AppContext.BaseDirectory, "blobs")
            : storageOptions.LocalPath;
        Directory.CreateDirectory(rootPath);
        this.configuration = configuration;
    }

    private string GetSafeFullPath(string candidatePath)
    {
        var canonicalRoot = Path.GetFullPath(rootPath);
        string candidateFullPath = Path.IsPathRooted(candidatePath)
            ? Path.GetFullPath(candidatePath)
            : Path.GetFullPath(Path.Combine(rootPath, candidatePath));

        var isContained = candidateFullPath == canonicalRoot ||
                           candidateFullPath.StartsWith(canonicalRoot + Path.DirectorySeparatorChar);

        if (!isContained)
        {
            throw new UnauthorizedAccessException("Path traversal attempt detected.");
        }

        return candidateFullPath;
    }

    public async Task DeleteAsync(string blobName, CancellationToken cancellationToken = default)
    {
        var fullPath = GetSafeFullPath(blobName);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        await Task.CompletedTask;
    }

    public async Task<string> UploadAsync(Stream content, string blobName, string contentType, CancellationToken cancellationToken = default)
    {
        var fullPath = GetSafeFullPath(blobName);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? rootPath);

        await using var fileStream = File.Create(fullPath);
        await content.CopyToAsync(fileStream, cancellationToken);
        return fullPath;
    }

    public Task<string> EnsureFolderExistsAsync(string folderPath, CancellationToken cancellationToken = default)
    {
        var fullPath = GetSafeFullPath(folderPath);
        Directory.CreateDirectory(fullPath);
        return Task.FromResult(fullPath);
    }

    public Task<string?> GetSignedUrlAsync(
        string blobPath,
        TimeSpan expiry,
        CancellationToken cancellationToken = default)
    {
        string fullPath;
        try
        {
            fullPath = GetSafeFullPath(blobPath);
        }
        catch (UnauthorizedAccessException)
        {
            return Task.FromResult<string?>(null);
        }

        if (!File.Exists(fullPath))
        {
            return Task.FromResult<string?>(null);
        }

        var relativePath = Path.GetRelativePath(rootPath, fullPath);
        var safePath = relativePath.Replace('\\', '/').TrimStart('/');

        var origin = configuration["BlobStorage:PublicOrigin"] ?? configuration["Api:BaseUrl"];
        if (string.IsNullOrEmpty(origin))
        {
            return Task.FromResult<string?>($"/blobs/{safePath}");
        }

        return Task.FromResult<string?>($"{origin.TrimEnd('/')}/blobs/{safePath}");
    }
}
