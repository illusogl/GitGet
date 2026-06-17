using GitGet.Core.Interfaces;
using GitGet.Core.Models;

namespace GitGet.Core.Services;

public class TrendingService : ITrendingService
{
    private readonly IGitHubApiClient _gitHubClient;

    public TrendingService(IGitHubApiClient gitHubClient)
    {
        _gitHubClient = gitHubClient;
    }

    public Task<List<Repository>> GetTrendingReposAsync(
        string timeRange = "all",
        string? language = null,
        int page = 1,
        int perPage = 20,
        CancellationToken ct = default)
    {
        var query = BuildTrendingQuery(timeRange);
        return _gitHubClient.SearchRepositoriesAsync(query, language, "stars", page, perPage, ct);
    }

    private static string BuildTrendingQuery(string timeRange)
    {
        return timeRange.ToLowerInvariant() switch
        {
            "daily" => $"created:>={DateTime.UtcNow.AddDays(-1):yyyy-MM-dd}",
            "monthly" => $"created:>={DateTime.UtcNow.AddMonths(-1):yyyy-MM-dd}",
            "yearly" => $"created:>={DateTime.UtcNow.AddYears(-1):yyyy-MM-dd}",
            _ => "stars:>=10000" // default: all-time most starred
        };
    }
}