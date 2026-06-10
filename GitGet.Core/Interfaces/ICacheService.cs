namespace GitGet.Core.Interfaces;

public interface ICacheService
{
    Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken ct = default) where T : class;
    Task InvalidateAsync(string key, CancellationToken ct = default);
    Task ClearAllAsync(CancellationToken ct = default);
    Task<long> GetCacheSizeAsync(CancellationToken ct = default);
}