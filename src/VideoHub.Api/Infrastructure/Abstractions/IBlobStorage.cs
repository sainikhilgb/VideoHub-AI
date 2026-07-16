namespace VideoHub.Api.Infrastructure.Abstractions;

public interface IBlobStorage
{
    Task<string> UploadAsync(
        Stream content,
        string blobName,
        string contentType,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(string blobName, CancellationToken cancellationToken = default);

    Task<string?> GetSignedUrlAsync(
        string blobPath,
        TimeSpan expiry,
        CancellationToken cancellationToken = default);
}
