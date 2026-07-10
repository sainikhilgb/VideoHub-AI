using System.Net.Http.Headers;
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
}
