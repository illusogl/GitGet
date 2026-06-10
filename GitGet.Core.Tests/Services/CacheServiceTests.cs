using FluentAssertions;
using GitGet.Core.Data;
using GitGet.Core.Interfaces;
using GitGet.Core.Models;
using GitGet.Core.Services;
using Microsoft.Data.Sqlite;

namespace GitGet.Core.Tests.Services;

public class CacheServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ILocalDataStore _dataStore;
    private readonly ICacheService _cache;

    public CacheServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _dataStore = new LocalDataStore(_connection);
        _cache = new CacheService(_dataStore);
    }

    [Fact]
    public async Task GetOrSetAsync_FactoryCalled_WhenCacheMiss()
    {
        await _dataStore.InitializeAsync();
        bool factoryCalled = false;

        var result = await _cache.GetOrSetAsync("test_key", async () =>
        {
            factoryCalled = true;
            return new Repository { Id = 1, FullName = "test/repo" };
        });

        factoryCalled.Should().BeTrue();
        result.Should().NotBeNull();
        result!.FullName.Should().Be("test/repo");
    }

    [Fact]
    public async Task GetOrSetAsync_ReturnsCached_WhenCacheHit()
    {
        await _dataStore.InitializeAsync();
        var repo = new Repository { Id = 1, FullName = "cached/repo", Stars = 500 };

        await _cache.GetOrSetAsync("cache_hit", () => Task.FromResult(repo));

        bool factoryCalled = false;
        var result = await _cache.GetOrSetAsync("cache_hit", async () =>
        {
            factoryCalled = true;
            return new Repository();
        });

        factoryCalled.Should().BeFalse();
        result.Should().NotBeNull();
        result!.Stars.Should().Be(500);
    }

    [Fact]
    public async Task InvalidateAsync_RemovesFromCache()
    {
        await _dataStore.InitializeAsync();
        await _cache.GetOrSetAsync("to_invalidate",
            () => Task.FromResult(new Repository { Id = 1, FullName = "to-go" }));

        await _cache.InvalidateAsync("to_invalidate");

        bool factoryCalled = false;
        await _cache.GetOrSetAsync("to_invalidate", async () =>
        {
            factoryCalled = true;
            return new Repository { Id = 2, FullName = "fresh" };
        });

        factoryCalled.Should().BeTrue();
    }

    [Fact]
    public async Task GetCacheSizeAsync_ReturnsZero_WhenEmpty()
    {
        var size = await _cache.GetCacheSizeAsync();
        size.Should().Be(0);
    }

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
    }
}