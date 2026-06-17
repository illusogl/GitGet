using FluentAssertions;
using GitGet.Core.Interfaces;
using GitGet.Core.Models;
using GitGet.Core.Services;
using Moq;

namespace GitGet.Core.Tests.Services;

public class RecommendationServiceTests
{
    private readonly Mock<IGitHubApiClient> _apiClientMock;
    private readonly Mock<ITrendingService> _trendingMock;
    private readonly IRecommendationService _service;

    public RecommendationServiceTests()
    {
        _apiClientMock = new Mock<IGitHubApiClient>();
        _trendingMock = new Mock<ITrendingService>();
        _service = new RecommendationService(_apiClientMock.Object, _trendingMock.Object);
    }

    [Fact]
    public async Task GetRecommendedReposAsync_NoUsername_ReturnsTrending()
    {
        // Arrange
        var trendingList = new List<Repository>
        {
            new() { Id = 1, FullName = "trending/repo", Stars = 1000 }
        };
        _trendingMock
            .Setup(x => x.GetTrendingReposAsync("weekly", null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(trendingList);

        // Act
        var result = await _service.GetRecommendedReposAsync(null);

        // Assert
        result.Should().HaveCount(1);
        result[0].FullName.Should().Be("trending/repo");
    }

    [Fact]
    public async Task GetRecommendedReposAsync_WithUsername_ReturnsPersonalized()
    {
        // Arrange
        var starredRepos = new List<Repository>
        {
            new() { Id = 1, FullName = "dotnet/runtime", Language = "C#", Stars = 100, Topics = new() { "dotnet" } },
            new() { Id = 2, FullName = "microsoft/vscode", Language = "TypeScript", Stars = 200, Topics = new() { "editor" } },
            new() { Id = 3, FullName = "dotnet/aspnetcore", Language = "C#", Stars = 150, Topics = new() { "dotnet", "web" } },
            new() { Id = 4, FullName = "golang/go", Language = "Go", Stars = 300, Topics = new() { "systems" } },
            new() { Id = 5, FullName = "rust-lang/rust", Language = "Rust", Stars = 400, Topics = new() { "systems" } },
        };

        _apiClientMock
            .Setup(x => x.GetStarredReposAsync("testuser", 1, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(starredRepos);

        var searchResults = new List<Repository>
        {
            new() { Id = 10, FullName = "awesome/csharp-stuff", Language = "C#", Stars = 5000 },
            new() { Id = 11, FullName = "awesome/typescript-utils", Language = "TypeScript", Stars = 4000 },
            new() { Id = 12, FullName = "cool/go-project", Language = "Go", Stars = 3000 },
        };

        _apiClientMock
            .Setup(x => x.SearchRepositoriesAsync(
                It.IsAny<string>(), It.IsAny<string?>(), "stars", It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults);

        // Act
        var result = await _service.GetRecommendedReposAsync("testuser");

        // Assert: should return personalized results
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetRecommendedReposAsync_EmptyStars_ReturnsTrending()
    {
        // Arrange
        _apiClientMock
            .Setup(x => x.GetStarredReposAsync("newuser", 1, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Repository>());

        var trendingList = new List<Repository>
        {
            new() { Id = 1, FullName = "trending/repo", Stars = 1000 }
        };
        _trendingMock
            .Setup(x => x.GetTrendingReposAsync("weekly", null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(trendingList);

        // Act
        var result = await _service.GetRecommendedReposAsync("newuser");

        // Assert: fallback to trending when no stars
        result.Should().HaveCount(1);
        result[0].FullName.Should().Be("trending/repo");
    }
}