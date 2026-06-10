using GitGet.Core.Interfaces;

namespace GitGet.Core.Services;

public class CacheService : ICacheService
{
    private readonly ILocalDataStore _dataStore;

    public CacheService(ILocalDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken ct = default) where T : class
    {
        // Try to get from cache
        var cached = await _dataStore.GetAsync<T>(key, ct);
        if (cached != null)
            return cached;

        // Cache miss - call factory
        var result = await factory();

        // Store in cache
        if (result != null)
            await _dataStore.SaveAsync(key, result, ct);

        return result;
    }

    public async Task InvalidateAsync(string key, CancellationToken ct = default)
    {
        await _dataStore.DeleteAsync(key, ct);
    }

    public async Task ClearAllAsync(CancellationToken ct = default)
    {
        // Track cache entry count
        var cacheKeys = await _dataStore.GetAsync<List<string>>("__cache_keys__", ct);
        if (cacheKeys != null)
        {
            foreach (var key in cacheKeys)
            {
                await _dataStore.DeleteAsync(key, ct);
            }
            await _dataStore.DeleteAsync("__cache_keys__", ct);
        }
    }

    public async Task<long> GetCacheSizeAsync(CancellationToken ct = default)
    {
        var cacheKeys = await _dataStore.GetAsync<List<string>>("__cache_keys__", ct);
        return cacheKeys?.Count * 256L ?? 0; // Estimated 256 bytes per entry
    }
}