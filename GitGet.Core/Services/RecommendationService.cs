using GitGet.Core.Interfaces;
using GitGet.Core.Models;

namespace GitGet.Core.Services;

public class RecommendationService : IRecommendationService
{
    private readonly IGitHubApiClient _gitHubClient;
    private readonly ITrendingService _trendingService;

    public RecommendationService(IGitHubApiClient gitHubClient, ITrendingService trendingService)
    {
        _gitHubClient = gitHubClient;
        _trendingService = trendingService;
    }

    public async Task<List<Repository>> GetRecommendedReposAsync(
        string? username = null,
        int count = 20,
        CancellationToken ct = default)
    {
        // Fallback: no username → return global trending
        if (string.IsNullOrWhiteSpace(username))
        {
            return await _trendingService.GetTrendingReposAsync("weekly", perPage: count, ct: ct);
        }

        // Personalized: analyze user's star history
        var stars = await _gitHubClient.GetStarredReposAsync(username, 1, 100, ct);
        if (stars.Count == 0)
        {
            return await _trendingService.GetTrendingReposAsync("weekly", perPage: count, ct: ct);
        }

        // Count language frequency from starred repos
        var langCounts = new Dictionary<string, int>();
        foreach (var repo in stars)
        {
            if (!string.IsNullOrWhiteSpace(repo.Language))
            {
                langCounts.TryGetValue(repo.Language, out var n);
                langCounts[repo.Language] = n + 1;
            }
        }

        // Get top 3 languages
        var topLanguages = langCounts
            .OrderByDescending(kv => kv.Value)
            .Take(3)
            .Select(kv => kv.Key)
            .ToList();

        // Search each top language individually, merge & deduplicate
        var results = new List<Repository>();
        var seen = new HashSet<long>();

        int perQuery = topLanguages.Count > 0
            ? Math.Max(7, count / topLanguages.Count)
            : count;

        foreach (var lang in topLanguages)
        {
            var langResults = await _gitHubClient.SearchRepositoriesAsync(
                $"language:{lang}", language: null, sort: "stars", page: 1,
                perPage: perQuery, ct: ct);

            if (langResults == null || langResults.Count == 0) continue;

            foreach (var repo in langResults)
            {
                if (seen.Add(repo.Id))
                    results.Add(repo);
            }
        }

        return results
            .OrderByDescending(r => r.Stars)
            .Take(count)
            .ToList();
    }
}