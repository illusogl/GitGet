namespace GitGet.Core.Interfaces;

public interface ILocalDataStore
{
    Task SaveAsync<T>(string key, T data, CancellationToken ct = default) where T : class;
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class;
    Task DeleteAsync(string key, CancellationToken ct = default);
    Task<List<T>> QueryAsync<T>(string tableName, string? whereClause = null, Dictionary<string, object>? parameters = null, CancellationToken ct = default) where T : class, new();
    Task InitializeAsync(CancellationToken ct = default);
}