using FluentAssertions;
using GitGet.Core.Interfaces;
using GitGet.Core.Data;

namespace GitGet.Core.Tests.Services;

public class SecureDataStoreTests : IDisposable
{
    private readonly string _testDir;
    private readonly ISecureDataStore _store;

    public SecureDataStoreTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"giget_secure_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
        _store = new SecureDataStore(_testDir);
    }

    [Fact]
    public async Task SaveAndGetTokenAsync_Roundtrip_Succeeds()
    {
        // Arrange
        const string token = "gho_test_token_abc123";

        // Act
        await _store.SaveTokenAsync("github_token", token);
        var loaded = await _store.GetTokenAsync("github_token");

        // Assert
        loaded.Should().Be(token);
    }

    [Fact]
    public async Task GetTokenAsync_NoToken_ReturnsNull()
    {
        // Act
        var result = await _store.GetTokenAsync("nonexistent_token");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ClearTokenAsync_RemovesToken()
    {
        // Arrange
        await _store.SaveTokenAsync("test_token", "some_value");

        // Act
        await _store.ClearTokenAsync("test_token");
        var result = await _store.GetTokenAsync("test_token");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task TokenIsEncrypted_NotPlainText()
    {
        // Arrange
        const string token = "gho_secret_token_xyz";

        // Act
        await _store.SaveTokenAsync("secret", token);

        // Assert - file should exist but not contain plain text
        var filePath = Path.Combine(_testDir, "secret.enc");
        File.Exists(filePath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(filePath);
        content.Should().NotContain(token);
    }

    [Fact]
    public async Task MultipleTokens_CanBeStoredIndependently()
    {
        // Arrange
        await _store.SaveTokenAsync("token1", "value1");
        await _store.SaveTokenAsync("token2", "value2");

        // Act
        var loaded1 = await _store.GetTokenAsync("token1");
        var loaded2 = await _store.GetTokenAsync("token2");

        // Assert
        loaded1.Should().Be("value1");
        loaded2.Should().Be("value2");
    }

    [Fact]
    public async Task SaveTokenAsync_OverwritesExisting()
    {
        // Arrange
        await _store.SaveTokenAsync("overwrite_test", "old_value");

        // Act
        await _store.SaveTokenAsync("overwrite_test", "new_value");
        var result = await _store.GetTokenAsync("overwrite_test");

        // Assert
        result.Should().Be("new_value");
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
    }
}