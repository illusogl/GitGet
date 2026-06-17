using FluentAssertions;
using GitGet.Core.Interfaces;
using GitGet.Core.Services;

namespace GitGet.Core.Tests.Services;

public class GitGetSettingsTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _tempFile;

    public GitGetSettingsTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "GitGet_Test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _tempFile = Path.Combine(_tempDir, "settings.json");
    }

    [Fact]
    public void DefaultValues_AreSet()
    {
        var settings = new GitGetSettings(_tempFile);
        settings.DownloadPath.Should().NotBeNullOrWhiteSpace();
        settings.MaxConcurrentDownloads.Should().Be(3);
    }

    [Fact]
    public async Task SaveAndLoad_Roundtrip_Succeeds()
    {
        var testPath = _tempDir; // Use real existing directory
        var settings = new GitGetSettings(_tempFile);
        settings.DownloadPath = testPath;
        settings.MaxConcurrentDownloads = 5;
        await settings.SaveAsync();

        var loaded = new GitGetSettings(_tempFile);
        await loaded.LoadAsync();

        loaded.DownloadPath.Should().Be(testPath);
        loaded.MaxConcurrentDownloads.Should().Be(5);
    }

    [Fact]
    public async Task Load_NonexistentFile_KeepsDefaults()
    {
        var settings = new GitGetSettings(_tempFile);
        await settings.LoadAsync();

        settings.DownloadPath.Should().NotBeNullOrWhiteSpace();
        settings.MaxConcurrentDownloads.Should().Be(3);
    }

    [Fact]
    public void MaxConcurrentDownloads_CanBeSet()
    {
        var settings = new GitGetSettings(_tempFile);
        settings.MaxConcurrentDownloads = 8;
        settings.MaxConcurrentDownloads.Should().Be(8);
    }

    [Fact]
    public void DownloadPath_CanBeSet()
    {
        var settings = new GitGetSettings(_tempFile);
        settings.DownloadPath = _tempDir;
        settings.DownloadPath.Should().Be(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }
}