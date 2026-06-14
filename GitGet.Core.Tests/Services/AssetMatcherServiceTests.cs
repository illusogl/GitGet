using FluentAssertions;
using GitGet.Core.Interfaces;
using GitGet.Core.Models;
using GitGet.Core.Services;

namespace GitGet.Core.Tests.Services;

public class AssetMatcherServiceTests
{
    private readonly IAssetMatcherService _matcher = new AssetMatcherService();

    [Fact]
    public void FindMatchingAsset_Windows_ReturnsExe()
    {
        var assets = new List<ReleaseAsset>
        {
            new() { Id = 1, Name = "app-v1.0-win-x64.exe", Size = 5_000_000 },
            new() { Id = 2, Name = "app-v1.0-macos.dmg", Size = 6_000_000 },
            new() { Id = 3, Name = "app-v1.0-linux.tar.gz", Size = 4_000_000 },
        };

        var result = _matcher.FindMatchingAsset(assets);
        result.Should().NotBeNull();
        result!.Name.Should().Contain(".exe");
    }

    [Fact]
    public void FindMatchingAsset_NoMatch_ReturnsNull()
    {
        var assets = new List<ReleaseAsset>
        {
            new() { Id = 1, Name = "source-code.zip", Size = 1_000_000 },
            new() { Id = 2, Name = "README.md", Size = 500 },
        };

        var result = _matcher.FindMatchingAsset(assets);
        result.Should().BeNull();
    }

    [Fact]
    public void FindMatchingAsset_PrefersArchitectureMatch()
    {
        var assets = new List<ReleaseAsset>
        {
            new() { Id = 1, Name = "app-v1.0-win-x86.exe", Size = 5_000_000 },
            new() { Id = 2, Name = "app-v1.0-win-x64.msi", Size = 5_200_000 },
            new() { Id = 3, Name = "app-v1.0-win-arm64.exe", Size = 5_100_000 },
        };

        var result = _matcher.FindMatchingAsset(assets);
        result.Should().NotBeNull();

        // x64 architecture preferred on x64 systems
        if (System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture ==
            System.Runtime.InteropServices.Architecture.X64)
        {
            result!.Name.Should().Contain("x64");
        }
    }

    [Fact]
    public void GetRecommendedAssets_ReturnsAllWithScores()
    {
        var assets = new List<ReleaseAsset>
        {
            new() { Id = 1, Name = "app-v1.0-win-x64.exe", Size = 5_000_000 },
            new() { Id = 2, Name = "app-v1.0-macos.dmg", Size = 6_000_000 },
            new() { Id = 3, Name = "source-code.zip", Size = 1_000_000 },
        };

        var scores = _matcher.GetRecommendedAssets(assets);
        scores.Should().NotBeNull();
        scores.Count.Should().Be(3);
    }

    [Fact]
    public void FindMatchingAsset_EmptyList_ReturnsNull()
    {
        var assets = new List<ReleaseAsset>();
        var result = _matcher.FindMatchingAsset(assets);
        result.Should().BeNull();
    }
}