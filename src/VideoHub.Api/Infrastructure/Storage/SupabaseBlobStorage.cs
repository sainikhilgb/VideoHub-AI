using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using VideoHub.Api.Infrastructure.Abstractions;
using VideoHub.Api.Infrastructure.Options;

namespace VideoHub.Api.Infrastructure.Storage;

public sealed class SupabaseBlobStorage : IBlobStorage
{
    private readonly HttpClient httpClient;
    private readonly BlobStorageOptions options;

    public SupabaseBlobStorage(HttpClient httpClient, IOptions<BlobStorageOptions> options)
    {
        this.httpClient = httpClient;
        this.options = options.Value;

        if (string.IsNullOrWhiteSpace(this.options.SupabaseUrl) ||
            string.IsNullOrWhiteSpace(this.options.SupabaseKey) ||
            string.IsNullOrWhiteSpace(this.options.BucketName))
        {
            throw new InvalidOperationException(
                "Supabase storage is selected, but BlobStorage:SupabaseUrl, BlobStorage:SupabaseKey, or BlobStorage:BucketName is missing.");
        }

        this.httpClient.BaseAddress = new Uri(this.options.SupabaseUrl.TrimEnd('/') + "/");
        this.httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", this.options.SupabaseKey);
        this.httpClient.DefaultRequestHeaders.Add("apikey", this.options.SupabaseKey);
    }

    public async Task DeleteAsync(string blobName, CancellationToken cancellationToken = default)
    {
        var objectPath = BuildObjectPath(blobName);
        using var response = await httpClient.DeleteAsync($"storage/v1/object/{objectPath}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<string> UploadAsync(
        Stream content,
        string blobName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var objectPath = BuildObjectPath(blobName);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"storage/v1/object/{objectPath}");
        request.Headers.Add("x-upsert", "true");

        using var streamContent = new StreamContent(content);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        request.Content = streamContent;

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return $"{options.SupabaseUrl!.TrimEnd('/')}/storage/v1/object/public/{objectPath}";
    }

    private string BuildObjectPath(string blobName)
    {
        var encodedSegments = blobName
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.EscapeDataString);

        return $"{Uri.EscapeDataString(options.BucketName!)}/{string.Join('/', encodedSegments)}";
    }

    public async Task<string> EnsureFolderExistsAsync(string folderPath, CancellationToken cancellationToken = default)
    {
        var keepFilePath = folderPath.TrimEnd('/') + "/.keep";
        using var emptyStream = new MemoryStream();
        await UploadAsync(emptyStream, keepFilePath, "application/octet-stream", cancellationToken);
        return folderPath;
    }

    public async Task<string?> GetSignedUrlAsync(
        string blobPath,
        TimeSpan expiry,
        CancellationToken cancellationToken = default)
    {
        try
        {
            string relativePath = blobPath;
            string bucket = options.BucketName!;

            var prefix = "/storage/v1/object/authenticated/";
            var publicPrefix = "/storage/v1/object/public/";
            var actualPrefix = blobPath.Contains(prefix) ? prefix : (blobPath.Contains(publicPrefix) ? publicPrefix : null);

            if (actualPrefix != null)
            {
                var index = blobPath.IndexOf(actualPrefix);
                var pathAfterPrefix = blobPath.Substring(index + actualPrefix.Length);
                var parts = pathAfterPrefix.Split('/', 2);
                if (parts.Length == 2)
                {
                    bucket = parts[0];
                    relativePath = parts[1];
                }
            }
            else if (Uri.TryCreate(blobPath, UriKind.Absolute, out var uri))
            {
                var pathAndQuery = uri.AbsolutePath;
                var resolvedPrefix = pathAndQuery.Contains(prefix) ? prefix : (pathAndQuery.Contains(publicPrefix) ? publicPrefix : null);
                if (resolvedPrefix != null)
                {
                    var index = pathAndQuery.IndexOf(resolvedPrefix);
                    var pathAfterPrefix = pathAndQuery.Substring(index + resolvedPrefix.Length);
                    var parts = pathAfterPrefix.Split('/', 2);
                    if (parts.Length == 2)
                    {
                        bucket = parts[0];
                        relativePath = parts[1];
                    }
                }
            }

            var encodedSegments = relativePath
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(Uri.EscapeDataString);
            var objectPath = string.Join('/', encodedSegments);

            using var response = await httpClient.PostAsJsonAsync(
                $"storage/v1/object/sign/{Uri.EscapeDataString(bucket)}/{objectPath}",
                new { expiresIn = (int)expiry.TotalSeconds },
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<SupabaseSignedUrlResponse>(cancellationToken);
                if (result != null && !string.IsNullOrEmpty(result.SignedURL))
                {
                    var signedPath = result.SignedURL;
                    if (signedPath.StartsWith("/object/sign/"))
                    {
                        signedPath = "/storage/v1" + signedPath;
                    }

                    if (signedPath.StartsWith("/"))
                    {
                        return $"{options.SupabaseUrl!.TrimEnd('/')}{signedPath}";
                    }
                    return signedPath;
                }
            }
        }
        catch
        {
            // Fail-closed, bubble up or return null
        }

        return null;
    }

    private sealed class SupabaseSignedUrlResponse
    {
        [JsonPropertyName("signedURL")]
        public string SignedURL { get; set; } = string.Empty;
    }
}
