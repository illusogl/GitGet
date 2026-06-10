using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GitGet.Core.Interfaces;
using GitGet.Core.Models;

namespace GitGet.Core.Services;

public class GitHubApiClient : IGitHubApiClient
{
    private readonly HttpClient _httpClient;
    private int _remainingRateLimit = 60; // Default unauthenticated limit
    private DateTime? _rateLimitResetAt;

    public int RemainingRateLimit => _remainingRateLimit;
    public DateTime? RateLimitResetAt => _rateLimitResetAt;

    public GitHubApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<Repository>> SearchRepositoriesAsync(
        string query, string? language = null, string sort = "stars",
        int page = 1, int perPage = 20, CancellationToken ct = default)
    {
        var q = language != null ? $"{query}+language:{Uri.EscapeDataString(language)}" : query;
        var url = $"search/repositories?q={Uri.EscapeDataString(q)}&sort={sort}&order=desc&page={page}&per_page={perPage}";

        var response = await SendRequestAsync(url, ct);
        if (!response.IsSuccessStatusCode)
            return new List<Repository>();

        var json = await response.Content.ReadFromJsonAsync<SearchResult>(cancellationToken: ct);
        return json?.Items?.Select(MapRepository).ToList() ?? new List<Repository>();
    }

    public async Task<Repository?> GetRepositoryAsync(string owner, string repo, CancellationToken ct = default)
    {
        var url = $"repos/{owner}/{repo}";
        var response = await SendRequestAsync(url, ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        return MapRepository(json);
    }

    public async Task<List<Release>> GetReleasesAsync(
        string owner, string repo, int page = 1, int perPage = 30, CancellationToken ct = default)
    {
        var url = $"repos/{owner}/{repo}/releases?page={page}&per_page={perPage}";
        var response = await SendRequestAsync(url, ct);

        if (!response.IsSuccessStatusCode)
            return new List<Release>();

        var json = await response.Content.ReadFromJsonAsync<List<JsonElement>>(cancellationToken: ct);
        return json?.Select(MapRelease).ToList() ?? new List<Release>();
    }

    public async Task<List<Repository>> GetStarredReposAsync(
        string username, int page = 1, int perPage = 50, CancellationToken ct = default)
    {
        var url = $"users/{username}/starred?page={page}&per_page={perPage}";
        var response = await SendRequestAsync(url, ct);

        if (!response.IsSuccessStatusCode)
            return new List<Repository>();

        var json = await response.Content.ReadFromJsonAsync<List<JsonElement>>(cancellationToken: ct);
        return json?.Select(MapRepository).ToList() ?? new List<Repository>();
    }

    public async Task<GitHubUser?> GetUserAsync(CancellationToken ct = default)
    {
        var response = await SendRequestAsync("user", ct);

        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        return MapUser(json);
    }

    private async Task<HttpResponseMessage> SendRequestAsync(string relativeUrl, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, relativeUrl);
        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

        UpdateRateLimit(response);

        return response;
    }

    private void UpdateRateLimit(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("X-RateLimit-Remaining", out var remainingValues) &&
            int.TryParse(remainingValues.FirstOrDefault(), out var remaining))
        {
            _remainingRateLimit = remaining;
        }

        if (response.Headers.TryGetValues("X-RateLimit-Reset", out var resetValues) &&
            long.TryParse(resetValues.FirstOrDefault(), out var resetUnix))
        {
            _rateLimitResetAt = DateTimeOffset.FromUnixTimeSeconds(resetUnix).UtcDateTime;
        }
    }

    private static Repository MapRepository(JsonElement item)
    {
        return new Repository
        {
            Id = item.GetProperty("id").GetInt64(),
            FullName = GetStringOrDefault(item, "full_name"),
            Name = GetStringOrDefault(item, "name"),
            Owner = item.TryGetProperty("owner", out var owner)
                ? GetStringOrDefault(owner, "login")
                : string.Empty,
            Description = GetStringOrDefault(item, "description"),
            Language = GetStringOrDefault(item, "language"),
            Stars = item.TryGetProperty("stargazers_count", out var stars) ? stars.GetInt32() : 0,
            Forks = item.TryGetProperty("forks_count", out var forks) ? forks.GetInt32() : 0,
            OpenIssues = item.TryGetProperty("open_issues_count", out var issues) ? issues.GetInt32() : 0,
            License = item.TryGetProperty("license", out var license) && license.ValueKind == JsonValueKind.Object
                ? GetStringOrDefault(license, "spdx_id")
                : string.Empty,
            Homepage = GetStringOrDefault(item, "homepage"),
            UpdatedAt = TryParseDateTime(item, "updated_at"),
            CreatedAt = TryParseDateTime(item, "created_at"),
            Topics = item.TryGetProperty("topics", out var topics)
                ? topics.EnumerateArray().Select(t => t.GetString() ?? string.Empty).Where(t => !string.IsNullOrEmpty(t)).ToList()
                : new List<string>(),
            DefaultBranch = GetStringOrDefault(item, "default_branch", "main")
        };
    }

    private static Release MapRelease(JsonElement item)
    {
        return new Release
        {
            Id = item.TryGetProperty("id", out var id) ? id.GetInt64() : 0,
            TagName = GetStringOrDefault(item, "tag_name"),
            Name = GetStringOrDefault(item, "name"),
            Body = GetStringOrDefault(item, "body"),
            Prerelease = item.TryGetProperty("prerelease", out var pre) && pre.GetBoolean(),
            CreatedAt = TryParseDateTime(item, "created_at"),
            PublishedAt = TryParseDateTime(item, "published_at"),
            HtmlUrl = GetStringOrDefault(item, "html_url"),
            Assets = item.TryGetProperty("assets", out var assets)
                ? assets.EnumerateArray().Select(MapAsset).ToList()
                : new List<ReleaseAsset>()
        };
    }

    private static ReleaseAsset MapAsset(JsonElement item)
    {
        return new ReleaseAsset
        {
            Id = item.TryGetProperty("id", out var id) ? id.GetInt64() : 0,
            Name = GetStringOrDefault(item, "name"),
            Size = item.TryGetProperty("size", out var size) ? size.GetInt64() : 0,
            ContentType = GetStringOrDefault(item, "content_type"),
            DownloadUrl = GetStringOrDefault(item, "browser_download_url"),
            CreatedAt = TryParseDateTime(item, "created_at"),
            UpdatedAt = TryParseDateTime(item, "updated_at"),
            DownloadCount = item.TryGetProperty("download_count", out var dc) ? dc.GetInt32() : 0
        };
    }

    private static GitHubUser MapUser(JsonElement item)
    {
        return new GitHubUser
        {
            Id = item.GetProperty("id").GetInt64(),
            Login = GetStringOrDefault(item, "login"),
            Name = GetStringOrDefault(item, "name"),
            Email = GetStringOrDefault(item, "email"),
            AvatarUrl = GetStringOrDefault(item, "avatar_url"),
            HtmlUrl = GetStringOrDefault(item, "html_url"),
            Bio = GetStringOrDefault(item, "bio"),
            PublicRepos = item.TryGetProperty("public_repos", out var repos) ? repos.GetInt32() : 0,
            Followers = item.TryGetProperty("followers", out var followers) ? followers.GetInt32() : 0,
            Following = item.TryGetProperty("following", out var following) ? following.GetInt32() : 0,
            CreatedAt = TryParseDateTime(item, "created_at")
        };
    }

    private static string GetStringOrDefault(JsonElement element, string propertyName, string defaultValue = "")
    {
        return element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString() ?? defaultValue
            : defaultValue;
    }

    private static DateTime TryParseDateTime(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            var str = prop.GetString();
            if (!string.IsNullOrEmpty(str) && DateTime.TryParse(str, out var dt))
                return dt;
        }
        return DateTime.MinValue;
    }

    private class SearchResult
    {
        [JsonPropertyName("total_count")]
        public int TotalCount { get; set; }

        [JsonPropertyName("items")]
        public List<JsonElement>? Items { get; set; }
    }
}