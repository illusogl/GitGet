using System.Text.Json;
using GitGet.Core.Interfaces;
using Microsoft.Data.Sqlite;

namespace GitGet.Core.Data;

public class LocalDataStore : ILocalDataStore
{
    private readonly SqliteConnection _connection;
    private bool _initialized;

    public LocalDataStore(SqliteConnection connection)
    {
        _connection = connection;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (_initialized) return;

        await _connection.OpenAsync(ct);

        var createTables = """
            CREATE TABLE IF NOT EXISTS cache_repos (
                key TEXT PRIMARY KEY,
                data TEXT NOT NULL,
                created_at TEXT NOT NULL DEFAULT (datetime('now')),
                expires_at TEXT
            );

            CREATE TABLE IF NOT EXISTS cache_releases (
                key TEXT PRIMARY KEY,
                data TEXT NOT NULL,
                created_at TEXT NOT NULL DEFAULT (datetime('now')),
                expires_at TEXT
            );

            CREATE TABLE IF NOT EXISTS download_tasks (
                task_id TEXT PRIMARY KEY,
                data TEXT NOT NULL,
                created_at TEXT NOT NULL DEFAULT (datetime('now'))
            );

            CREATE INDEX IF NOT EXISTS idx_cache_repos_expires ON cache_repos(expires_at);
            CREATE INDEX IF NOT EXISTS idx_cache_releases_expires ON cache_releases(expires_at);
            """;

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = createTables;
        await cmd.ExecuteNonQueryAsync(ct);

        _initialized = true;
    }

    public async Task SaveAsync<T>(string key, T data, CancellationToken ct = default) where T : class
    {
        await InitializeAsync(ct);

        var json = JsonSerializer.Serialize(data);
        var tableName = GetTableNameForKey(key);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = $"""
            INSERT OR REPLACE INTO {tableName} (key, data, created_at, expires_at)
            VALUES (@key, @data, datetime('now'), datetime('now', '+1 hour'))
            """;
        cmd.Parameters.AddWithValue("@key", key);
        cmd.Parameters.AddWithValue("@data", json);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        await InitializeAsync(ct);

        var tableName = GetTableNameForKey(key);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = $"""
            SELECT data FROM {tableName}
            WHERE key = @key AND (expires_at IS NULL OR expires_at > datetime('now'))
            """;
        cmd.Parameters.AddWithValue("@key", key);

        var result = await cmd.ExecuteScalarAsync(ct) as string;
        if (result == null) return null;

        return JsonSerializer.Deserialize<T>(result);
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        await InitializeAsync(ct);

        var tableName = GetTableNameForKey(key);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = $"DELETE FROM {tableName} WHERE key = @key";
        cmd.Parameters.AddWithValue("@key", key);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<List<T>> QueryAsync<T>(string tableName, string? whereClause = null,
        Dictionary<string, object>? parameters = null, CancellationToken ct = default) where T : class, new()
    {
        await InitializeAsync(ct);

        var safeTableName = SanitizeTableName(tableName);
        var where = whereClause != null ? $"WHERE {whereClause}" : "";

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = $"SELECT * FROM {safeTableName} {where}";

        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                cmd.Parameters.AddWithValue(param.Key, param.Value);
            }
        }

        var results = new List<T>();
        using var reader = await cmd.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            var data = reader.GetString(reader.GetOrdinal("data"));
            var item = JsonSerializer.Deserialize<T>(data);
            if (item != null)
                results.Add(item);
        }

        return results;
    }

    private static string GetTableNameForKey(string key)
    {
        if (key.StartsWith("repo:") || key.StartsWith("search:"))
            return "cache_repos";
        if (key.StartsWith("release:") || key.StartsWith("starred:"))
            return "cache_releases";
        if (key.StartsWith("task:"))
            return "download_tasks";
        return "cache_repos"; // default
    }

    public async Task ClearTableAsync(string tableName, CancellationToken ct = default)
    {
        await InitializeAsync(ct);
        var safeTableName = SanitizeTableName(tableName);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = $"DELETE FROM {safeTableName}";
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<long> GetTableRowCountAsync(string tableName, CancellationToken ct = default)
    {
        await InitializeAsync(ct);
        var safeTableName = SanitizeTableName(tableName);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM {safeTableName}";
        var result = await cmd.ExecuteScalarAsync(ct);
        return result is long count ? count : 0;
    }

    private static string SanitizeTableName(string tableName)
    {
        // Only allow known table names to prevent SQL injection
        var allowedTables = new HashSet<string>
        {
            "cache_repos", "cache_releases", "download_tasks",
            "sqlite_master", "sqlite_sequence"
        };
        return allowedTables.Contains(tableName) ? tableName : "cache_repos";
    }
}
