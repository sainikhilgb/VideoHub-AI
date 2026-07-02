using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using VideoHub.Api.Infrastructure.Abstractions;

namespace VideoHub.Api.Infrastructure.Caching;

public sealed class RedisCacheService : ICacheService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IDistributedCache distributedCache;

    public RedisCacheService(IDistributedCache distributedCache)
    {
        this.distributedCache = distributedCache;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var cached = await distributedCache.GetStringAsync(key, cancellationToken);
        return cached is null ? default : JsonSerializer.Deserialize<T>(cached, SerializerOptions);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        distributedCache.RemoveAsync(key, cancellationToken);

    public Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpirationRelativeToNow = null, CancellationToken cancellationToken = default)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow
        };

        var payload = JsonSerializer.Serialize(value, SerializerOptions);
        return distributedCache.SetStringAsync(key, payload, options, cancellationToken);
    }
}
