using System.Text.Json;
using FluentAssertions;
using GitGet.Core.Interfaces;
using GitGet.Core.Models;
using GitGet.Core.Services;
using Moq;

namespace GitGet.Core.Tests.Services;

public class GitHubApiClientTests
{
    private readonly Mock<INodeScriptRunner> _scriptRunnerMock;
    private readonly IGitHubApiClient _client;

    public GitHubApiClientTests()
    {
        _scriptRunnerMock = new Mock<INodeScriptRunner>();
        _client = new GitHubApiClient(_scriptRunnerMock.Object);
    }

    [Fact]
    public async Task SearchRepositoriesAsync_ReturnsResults()
    {
        var jsonResponse = JsonSerializer.Serialize(new
        {
            total_count = 2,
            items = new[]
            {
                new
                {
                    id = 1,
                    full_name = "owner1/repo1",
                    name = "repo1",
                    owner = new { login = "owner1" },
                    description = "First repo",
                    language = "C#",
                    stargazers_count = 100,
                    forks_count = 50,
                    open_issues_count = 5,
                    updated_at = "2025-01-01T00:00:00Z",
                    created_at = "2024-01-01T00:00:00Z",
                    topics = new[] { "dotnet" },
                    default_branch = "main"
                },
                new
                {
                    id = 2,
                    full_name = "owner2/repo2",
                    name = "repo2",
                    owner = new { login = "owner2" },
                    description = "Second repo",
                    language = "Python",
                    stargazers_count = 200,
                    forks_count = 75,
                    open_issues_count = 3,
                    updated_at = "2025-02-01T00:00:00Z",
                    created_at = "2024-02-01T00:00:00Z",
                    topics = new[] { "python", "data" },
                    default_branch = "main"
                }
            }
        });

        _scriptRunnerMock
            .Setup(s => s.RunScriptAsync(
                It.Is<string[]>(args => args[1] == "/search/repositories"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(jsonResponse);

        var results = await _client.SearchRepositoriesAsync("test");

        results.Should().HaveCount(2);
        results[0].FullName.Should().Be("owner1/repo1");
        results[0].Stars.Should().Be(100);
    }

    [Fact]
    public async Task SearchRepositoriesAsync_WithLanguage_FiltersCorrectly()
    {
        var jsonResponse = JsonSerializer.Serialize(new
        {
            total_count = 1,
            items = new[]
            {
                new
                {
                    id = 1,
                    full_name = "owner/csharp-repo",
                    name = "csharp-repo",
                    owner = new { login = "owner" },
                    description = "A C# project",
                    language = "C#",
                    stargazers_count = 500,
                    forks_count = 100,
                    open_issues_count = 2,
                    updated_at = "2025-01-01T00:00:00Z",
                    created_at = "2024-01-01T00:00:00Z",
                    topics = Array.Empty<string>(),
                    default_branch = "main"
                }
            }
        });

        _scriptRunnerMock
            .Setup(s => s.RunScriptAsync(
                It.Is<string[]>(args => args[1] == "/search/repositories"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(jsonResponse);

        var results = await _client.SearchRepositoriesAsync("test", language: "C#");
        results.Should().HaveCount(1);
        results[0].Language.Should().Be("C#");
    }

    [Fact]
    public async Task SearchRepositoriesAsync_EmptyResponse_ReturnsEmptyList()
    {
        var jsonResponse = JsonSerializer.Serialize(new
        {
            total_count = 0,
            items = Array.Empty<object>()
        });

        _scriptRunnerMock
            .Setup(s => s.RunScriptAsync(
                It.Is<string[]>(args => args[1] == "/search/repositories"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(jsonResponse);

        var results = await _client.SearchRepositoriesAsync("nonexistent");
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRepositoryAsync_ReturnsRepository()
    {
        var jsonResponse = JsonSerializer.Serialize(new
        {
            id = 42,
            full_name = "dotnet/runtime",
            name = "runtime",
            owner = new { login = "dotnet" },
            description = ".NET is a cross-platform runtime",
            language = "C#",
            stargazers_count = 15000,
            forks_count = 5000,
            open_issues_count = 200,
            license = new { spdx_id = "MIT" },
            homepage = "https://dot.net",
            updated_at = "2025-06-01T00:00:00Z",
            created_at = "2019-01-01T00:00:00Z",
            topics = new[] { "dotnet", "runtime" },
            default_branch = "main"
        });

        _scriptRunnerMock
            .Setup(s => s.RunScriptAsync(
                It.Is<string[]>(args => args[1] == "/repos/dotnet/runtime"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(jsonResponse);

        var repo = await _client.GetRepositoryAsync("dotnet", "runtime");
        repo.Should().NotBeNull();
        repo!.FullName.Should().Be("dotnet/runtime");
        repo.Stars.Should().Be(15000);
        repo.License.Should().Be("MIT");
    }

    [Fact]
    public async Task GetRepositoryAsync_NotFound_ReturnsNull()
    {
        _scriptRunnerMock
            .Setup(s => s.RunScriptAsync(
                It.Is<string[]>(args => args[1] == "/repos/nonexistent/repo"),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("404"));

        var repo = await _client.GetRepositoryAsync("nonexistent", "repo");
        repo.Should().BeNull();
    }

    [Fact]
    public async Task GetReleasesAsync_ReturnsReleases()
    {
        var jsonResponse = JsonSerializer.Serialize(new[]
        {
            new
            {
                id = 100,
                tag_name = "v2.0.0",
                name = "Version 2.0.0",
                body = "Major release",
                prerelease = false,
                created_at = "2025-06-01T00:00:00Z",
                published_at = "2025-06-02T00:00:00Z",
                html_url = "https://github.com/owner/repo/releases/v2.0.0",
                assets = new[]
                {
                    new
                    {
                        id = 1001,
                        name = "app-v2.0.0-win-x64.exe",
                        size = 52428800,
                        content_type = "application/octet-stream",
                        browser_download_url = "https://github.com/owner/repo/releases/download/v2.0.0/app-v2.0.0-win-x64.exe",
                        created_at = "2025-06-02T00:00:00Z",
                        updated_at = "2025-06-02T00:00:00Z",
                        download_count = 1500
                    }
                }
            }
        });

        _scriptRunnerMock
            .Setup(s => s.RunScriptAsync(
                It.Is<string[]>(args => args[1] == "/repos/owner/repo/releases"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(jsonResponse);

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
        _scriptRunnerMock
            .Setup(s => s.RunScriptAsync(
                It.Is<string[]>(args => args[1] == "/repos/owner/repo/releases"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("[]");

        var releases = await _client.GetReleasesAsync("owner", "repo");
        releases.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStarredReposAsync_ReturnsStarredRepos()
    {
        var jsonResponse = JsonSerializer.Serialize(new[]
        {
            new
            {
                id = 10,
                full_name = "starred-owner/starred-repo",
                name = "starred-repo",
                owner = new { login = "starred-owner" },
                description = "A popular repo",
                language = "Rust",
                stargazers_count = 5000,
                forks_count = 1000,
                open_issues_count = 50,
                updated_at = "2025-05-01T00:00:00Z",
                created_at = "2023-01-01T00:00:00Z",
                topics = new[] { "systems" },
                default_branch = "main"
            }
        });

        _scriptRunnerMock
            .Setup(s => s.RunScriptAsync(
                It.Is<string[]>(args => args[1] == "/users/testuser/starred"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(jsonResponse);

        var starred = await _client.GetStarredReposAsync("testuser");
        starred.Should().HaveCount(1);
        starred[0].FullName.Should().Be("starred-owner/starred-repo");
        starred[0].Language.Should().Be("Rust");
    }

    [Fact]
    public async Task GetUserAsync_ReturnsUser()
    {
        var jsonResponse = JsonSerializer.Serialize(new
        {
            id = 999,
            login = "testuser",
            name = "Test User",
            email = "test@example.com",
            avatar_url = "https://avatars.githubusercontent.com/u/999?v=4",
            html_url = "https://github.com/testuser",
            bio = "A developer",
            public_repos = 42,
            followers = 100,
            following = 50,
            created_at = "2020-01-01T00:00:00Z"
        });

        _scriptRunnerMock
            .Setup(s => s.RunScriptAsync(
                It.Is<string[]>(args => args[1] == "/user"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(jsonResponse);

        var user = await _client.GetUserAsync();
        user.Should().NotBeNull();
        user!.Login.Should().Be("testuser");
        user.Name.Should().Be("Test User");
    }

    [Fact]
    public async Task GetUserAsync_Unauthenticated_ReturnsNull()
    {
        _scriptRunnerMock
            .Setup(s => s.RunScriptAsync(
                It.Is<string[]>(args => args[1] == "/user"),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("401"));

        var user = await _client.GetUserAsync();
        user.Should().BeNull();
    }

    [Fact]
    public void RemainingRateLimit_ReturnsDefault_WhenNoRequestMade()
    {
        _client.RemainingRateLimit.Should().Be(60);
    }
}