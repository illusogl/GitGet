using FluentAssertions;
using GitGet.Core.Data;
using GitGet.Core.Interfaces;
using GitGet.Core.Models;
using Microsoft.Data.Sqlite;

namespace GitGet.Core.Tests.Services;

public class LocalDataStoreTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ILocalDataStore _store;

    public LocalDataStoreTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _store = new LocalDataStore(_connection);
    }

    [Fact]
    public async Task InitializeAsync_CreatesTables()
    {
        // Act
        await _store.InitializeAsync();

        // Assert - verify tables exist via raw query
        var tablesCommand = _connection.CreateCommand();
        tablesCommand.CommandText =
            "SELECT name FROM sqlite_master WHERE type = 'table' AND name IN ('cache_repos', 'cache_releases', 'download_tasks')";
        var reader = await tablesCommand.ExecuteReaderAsync();
        var tableNames = new List<string>();
        while (await reader.ReadAsync())
        {
            tableNames.Add(reader.GetString(0));
        }

        tableNames.Should().Contain("cache_repos");
        tableNames.Should().Contain("cache_releases");
        tableNames.Should().Contain("download_tasks");
    }

    [Fact]
    public async Task SaveAndGetAsync_Roundtrip_Succeeds()
    {
        // Arrange
        await _store.InitializeAsync();
        var repo = new Repository
        {
            Id = 123,
            FullName = "test/test",
            Name = "test",
            Owner = "test",
            Stars = 100,
            Language = "C#"
        };

        // Act
        await _store.SaveAsync("repo:test/test", repo);
        var loaded = await _store.GetAsync<Repository>("repo:test/test");

        // Assert
        loaded.Should().NotBeNull();
        loaded!.FullName.Should().Be("test/test");
        loaded.Stars.Should().Be(100);
        loaded.Language.Should().Be("C#");
    }

    [Fact]
    public async Task GetAsync_NonExistentKey_ReturnsNull()
    {
        // Arrange
        await _store.InitializeAsync();

        // Act
        var result = await _store.GetAsync<Repository>("nonexistent_key");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_RemovesEntry()
    {
        // Arrange
        await _store.InitializeAsync();
        await _store.SaveAsync("test_key", "test_value");

        // Act
        await _store.DeleteAsync("test_key");
        var result = await _store.GetAsync<string>("test_key");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task QueryAsync_WithWhereClause_FiltersCorrectly()
    {
        // Arrange
        await _store.InitializeAsync();
        var repo1 = new Repository { Id = 1, FullName = "a/repo1", Name = "repo1", Owner = "a", Stars = 100, Language = "C#" };
        var repo2 = new Repository { Id = 2, FullName = "b/repo2", Name = "repo2", Owner = "b", Stars = 200, Language = "Python" };

        await _store.SaveAsync("repo:1", repo1);
        await _store.SaveAsync("repo:2", repo2);

        // Act
        var results = await _store.QueryAsync<Repository>(
            "cache_repos",
            "json_extract(data, '$.Stars') > @minStars",
            new Dictionary<string, object> { { "@minStars", 150 } });

        // Assert
        results.Should().HaveCount(1);
        results[0].FullName.Should().Be("b/repo2");
    }

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
    }
}