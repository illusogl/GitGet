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
        var tables = new[] { "cache_repos", "cache_releases" };
        foreach (var table in tables)
        {
            await _dataStore.ClearTableAsync(table, ct);
        }
    }

    public async Task<long> GetCacheSizeAsync(CancellationToken ct = default)
    {
        var tables = new[] { "cache_repos", "cache_releases" };
        long total = 0;
        foreach (var table in tables)
        {
            total += await _dataStore.GetTableRowCountAsync(table, ct);
        }
        return total;
    }
}