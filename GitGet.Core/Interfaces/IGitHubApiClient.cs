using GitGet.Core.Models;

namespace GitGet.Core.Interfaces;

public interface IGitHubApiClient
{
    Task<List<Repository>> SearchRepositoriesAsync(string query, string? language = null, string sort = "stars", int page = 1, int perPage = 20, CancellationToken ct = default);
    Task<Repository?> GetRepositoryAsync(string owner, string repo, CancellationToken ct = default);
    Task<List<Release>> GetReleasesAsync(string owner, string repo, int page = 1, int perPage = 30, CancellationToken ct = default);
    Task<List<Repository>> GetStarredReposAsync(string username, int page = 1, int perPage = 50, CancellationToken ct = default);
    Task<GitHubUser?> GetUserAsync(CancellationToken ct = default);
    Task<string?> GetReadmeContentAsync(string owner, string repo, CancellationToken ct = default);
    int RemainingRateLimit { get; }
    DateTime? RateLimitResetAt { get; }
}