using FluentAssertions;
using GitGet.Core.Interfaces;
using GitGet.Core.Models;
using GitGet.Core.Services;
using Moq;

namespace GitGet.Core.Tests.Services;

public class TrendingServiceTests
{
    private readonly Mock<IGitHubApiClient> _apiClientMock;
    private readonly ITrendingService _service;

    public TrendingServiceTests()
    {
        _apiClientMock = new Mock<IGitHubApiClient>();
        _service = new TrendingService(_apiClientMock.Object);
    }

    [Fact]
    public async Task GetTrendingReposAsync_Monthly_CallsSearchWithCorrectDateRange()
    {
        // Arrange
        _apiClientMock
            .Setup(x => x.SearchRepositoriesAsync(
                It.IsAny<string>(), null, "stars", 1, 20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Repository>());

        // Act
        var result = await _service.GetTrendingReposAsync("monthly");

        // Assert
        _apiClientMock.Verify(x => x.SearchRepositoriesAsync(
            It.Is<string>(q => q.Contains("created:")), null, "stars", 1, 20,
            It.IsAny<CancellationToken>()), Times.Once);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTrendingReposAsync_All_UsesStarThreshold()
    {
        // Arrange
        _apiClientMock
            .Setup(x => x.SearchRepositoriesAsync(
                It.IsAny<string>(), null, "stars", 1, 20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Repository>());

        // Act
        var result = await _service.GetTrendingReposAsync("all");

        // Assert
        _apiClientMock.Verify(x => x.SearchRepositoriesAsync(
            It.Is<string>(q => q.Contains("stars:>")), null, "stars", 1, 20,
            It.IsAny<CancellationToken>()), Times.Once);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTrendingReposAsync_InvalidTimeRange_DefaultsToAll()
    {
        // Arrange
        _apiClientMock
            .Setup(x => x.SearchRepositoriesAsync(
                It.IsAny<string>(), null, "stars", 1, 20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Repository>());

        // Act
        var result = await _service.GetTrendingReposAsync("invalid_range");

        // Assert: should use 'all' as fallback
        _apiClientMock.Verify(x => x.SearchRepositoriesAsync(
            It.Is<string>(q => q.Contains("stars:>")), null, "stars", 1, 20,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTrendingReposAsync_ReturnsResults()
    {
        // Arrange
        var repos = new List<Repository>
        {
            new() { Id = 1, FullName = "trending/repo1", Stars = 500 },
            new() { Id = 2, FullName = "trending/repo2", Stars = 300 },
        };
        _apiClientMock
            .Setup(x => x.SearchRepositoriesAsync(
                It.IsAny<string>(), null, "stars", 1, 20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(repos);

        // Act
        var result = await _service.GetTrendingReposAsync("all");

        // Assert
        result.Should().HaveCount(2);
        result[0].FullName.Should().Be("trending/repo1");
        result[0].Stars.Should().Be(500);
    }

    [Fact]
    public async Task GetTrendingReposAsync_WithLanguage_FiltersCorrectly()
    {
        // Arrange
        _apiClientMock
            .Setup(x => x.SearchRepositoriesAsync(
                It.IsAny<string>(), "C#", "stars", 1, 20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Repository>());

        // Act
        var result = await _service.GetTrendingReposAsync("all", language: "C#");

        // Assert
        _apiClientMock.Verify(x => x.SearchRepositoriesAsync(
            It.IsAny<string>(), "C#", "stars", 1, 20,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTrendingReposAsync_Daily_FiltersToLast24Hours()
    {
        // Arrange
        _apiClientMock
            .Setup(x => x.SearchRepositoriesAsync(
                It.IsAny<string>(), null, "stars", 1, 20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Repository>());

        // Act
        var result = await _service.GetTrendingReposAsync("daily");

        // Assert
        _apiClientMock.Verify(x => x.SearchRepositoriesAsync(
            It.Is<string>(q => q.Contains("created:>=20")), null, "stars", 1, 20,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}