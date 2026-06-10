using System.Net;
using System.Text.Json;
using FluentAssertions;
using GitGet.Core.Interfaces;
using GitGet.Core.Models;
using GitGet.Core.Services;
using Moq;
using Moq.Protected;

namespace GitGet.Core.Tests.Services;

public class GitHubApiClientTests
{
    private readonly Mock<HttpMessageHandler> _handlerMock;
    private readonly HttpClient _httpClient;
    private readonly IGitHubApiClient _client;

    public GitHubApiClientTests()
    {
        _handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        _httpClient = new HttpClient(_handlerMock.Object)
        {
            BaseAddress = new Uri("https://api.github.com/")
        };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "GitGet-Test");
        _client = new GitHubApiClient(_httpClient);
    }

    [Fact]
    public async Task SearchRepositoriesAsync_ReturnsResults()
    {
        var jsonResponse = """
        {
            "total_count": 2,
            "items": [
                {
                    "id": 1,
                    "full_name": "owner1/repo1",
                    "name": "repo1",
                    "owner": { "login": "owner1" },
                    "description": "First repo",
                    "language": "C#",
                    "stargazers_count": 100,
                    "forks_count": 50,
                    "open_issues_count": 5,
                    "updated_at": "2025-01-01T00:00:00Z",
                    "created_at": "2024-01-01T00:00:00Z",
                    "topics": ["dotnet"],
                    "default_branch": "main"
                },
                {
                    "id": 2,
                    "full_name": "owner2/repo2",
                    "name": "repo2",
                    "owner": { "login": "owner2" },
                    "description": "Second repo",
                    "language": "Python",
                    "stargazers_count": 200,
                    "forks_count": 75,
                    "open_issues_count": 3,
                    "updated_at": "2025-02-01T00:00:00Z",
                    "created_at": "2024-02-01T00:00:00Z",
                    "topics": ["python", "data"],
                    "default_branch": "main"
                }
            ]
        }
        """;

        SetupMockResponse("/search/repositories", jsonResponse);

        var results = await _client.SearchRepositoriesAsync("test");

        results.Should().HaveCount(2);
        results[0].FullName.Should().Be("owner1/repo1");
        results[0].Stars.Should().Be(100);
        results[1].FullName.Should().Be("owner2/repo2");
    }

    [Fact]
    public async Task SearchRepositoriesAsync_WithLanguage_FiltersCorrectly()
    {
        var jsonResponse = """
        {
            "total_count": 1,
            "items": [
                {
                    "id": 1,
                    "full_name": "owner/csharp-repo",
                    "name": "csharp-repo",
                    "owner": { "login": "owner" },
                    "description": "A C# project",
                    "language": "C#",
                    "stargazers_count": 500,
                    "forks_count": 100,
                    "open_issues_count": 2,
                    "updated_at": "2025-01-01T00:00:00Z",
                    "created_at": "2024-01-01T00:00:00Z",
                    "topics": [],
                    "default_branch": "main"
                }
            ]
        }
        """;

        SetupMockResponse("/search/repositories", jsonResponse);

        var results = await _client.SearchRepositoriesAsync("test", language: "C#");

        results.Should().HaveCount(1);
        results[0].Language.Should().Be("C#");
    }

    [Fact]
    public async Task SearchRepositoriesAsync_EmptyResponse_ReturnsEmptyList()
    {
        var jsonResponse = """{"total_count": 0, "items": []}""";
        SetupMockResponse("/search/repositories", jsonResponse);

        var results = await _client.SearchRepositoriesAsync("nonexistent");

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRepositoryAsync_ReturnsRepository()
    {
        var jsonResponse = """
        {
            "id": 42,
            "full_name": "dotnet/runtime",
            "name": "runtime",
            "owner": { "login": "dotnet" },
            "description": ".NET is a cross-platform runtime",
            "language": "C#",
            "stargazers_count": 15000,
            "forks_count": 5000,
            "open_issues_count": 200,
            "license": { "spdx_id": "MIT" },
            "homepage": "https://dot.net",
            "updated_at": "2025-06-01T00:00:00Z",
            "created_at": "2019-01-01T00:00:00Z",
            "topics": ["dotnet", "runtime", "cross-platform"],
            "default_branch": "main"
        }
        """;

        SetupMockResponse("/repos/dotnet/runtime", jsonResponse);

        var repo = await _client.GetRepositoryAsync("dotnet", "runtime");

        repo.Should().NotBeNull();
        repo!.FullName.Should().Be("dotnet/runtime");
        repo.Stars.Should().Be(15000);
        repo.License.Should().Be("MIT");
    }

    [Fact]
    public async Task GetRepositoryAsync_NotFound_ReturnsNull()
    {
        SetupMockResponse("/repos/nonexistent/repo", """{"message": "Not Found"}""", HttpStatusCode.NotFound);

        var repo = await _client.GetRepositoryAsync("nonexistent", "repo");

        repo.Should().BeNull();
    }

    [Fact]
    public async Task GetReleasesAsync_ReturnsReleases()
    {
        var jsonResponse = """
        [
            {
                "id": 100,
                "tag_name": "v2.0.0",
                "name": "Version 2.0.0",
                "body": "Major release with breaking changes",
                "prerelease": false,
                "created_at": "2025-06-01T00:00:00Z",
                "published_at": "2025-06-02T00:00:00Z",
                "html_url": "https://github.com/owner/repo/releases/v2.0.0",
                "assets": [
                    {
                        "id": 1001,
                        "name": "app-v2.0.0-win-x64.exe",
                        "size": 52428800,
                        "content_type": "application/octet-stream",
                        "browser_download_url": "https://github.com/owner/repo/releases/download/v2.0.0/app-v2.0.0-win-x64.exe",
                        "created_at": "2025-06-02T00:00:00Z",
                        "updated_at": "2025-06-02T00:00:00Z",
                        "download_count": 1500
                    }
                ]
            }
        ]
        """;

        SetupMockResponse("/repos/owner/repo/releases", jsonResponse);

        var releases = await _client.GetReleasesAsync("owner", "repo");

        releases.Should().HaveCount(1);
        releases[0].TagName.Should().Be("v2.0.0");
        releases[0].Prerelease.Should().BeFalse();
        releases[0].Assets.Should().HaveCount(1);
        releases[0].Assets[0].Name.Should().Be("app-v2.0.0-win-x64.exe");
    }

    [Fact]
    public async Task GetReleasesAsync_ReturnsEmpty_WhenNoReleases()
    {
        SetupMockResponse("/repos/owner/repo/releases", "[]");

        var releases = await _client.GetReleasesAsync("owner", "repo");

        releases.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStarredReposAsync_ReturnsStarredRepos()
    {
        var jsonResponse = """
        [
            {
                "id": 10,
                "full_name": "starred-owner/starred-repo",
                "name": "starred-repo",
                "owner": { "login": "starred-owner" },
                "description": "A popular repo",
                "language": "Rust",
                "stargazers_count": 5000,
                "forks_count": 1000,
                "open_issues_count": 50,
                "updated_at": "2025-05-01T00:00:00Z",
                "created_at": "2023-01-01T00:00:00Z",
                "topics": ["systems"],
                "default_branch": "main"
            }
        ]
        """;

        SetupMockResponse("/users/testuser/starred", jsonResponse);

        var starred = await _client.GetStarredReposAsync("testuser");

        starred.Should().HaveCount(1);
        starred[0].FullName.Should().Be("starred-owner/starred-repo");
        starred[0].Language.Should().Be("Rust");
    }

    [Fact]
    public async Task GetUserAsync_ReturnsUser()
    {
        var jsonResponse = """
        {
            "id": 999,
            "login": "testuser",
            "name": "Test User",
            "email": "test@example.com",
            "avatar_url": "https://avatars.githubusercontent.com/u/999?v=4",
            "html_url": "https://github.com/testuser",
            "bio": "A developer",
            "public_repos": 42,
            "followers": 100,
            "following": 50,
            "created_at": "2020-01-01T00:00:00Z"
        }
        """;

        SetupMockResponse("/user", jsonResponse);

        var user = await _client.GetUserAsync();

        user.Should().NotBeNull();
        user!.Login.Should().Be("testuser");
        user.Name.Should().Be("Test User");
    }

    [Fact]
    public async Task GetUserAsync_Unauthenticated_ReturnsNull()
    {
        SetupMockResponse("/user", """{"message": "Requires authentication"}""", HttpStatusCode.Unauthorized);

        var user = await _client.GetUserAsync();

        user.Should().BeNull();
    }

    [Fact]
    public void RemainingRateLimit_ReturnsDefault_WhenNoRequestMade()
    {
        _client.RemainingRateLimit.Should().Be(60);
    }

    [Fact]
    public async Task RateLimit_UpdatedAfterRequest()
    {
        var jsonResponse = """{"total_count": 0, "items": []}""";
        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse),
            Headers = { { "X-RateLimit-Remaining", new[] { "42" } } }
        };

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        await _client.SearchRepositoriesAsync("test");

        _client.RemainingRateLimit.Should().Be(42);
    }

    private void SetupMockResponse(string urlPath, string jsonContent, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var responseMessage = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(jsonContent),
            Headers = { { "X-RateLimit-Remaining", new[] { "59" } } }
        };

        // Use Func-based matching instead of expression tree to avoid CS0854
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri != null &&
                    req.RequestUri.ToString().Contains(urlPath)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);
    }
}