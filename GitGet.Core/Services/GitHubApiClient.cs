using System.Text.Json;
using System.Text.Json.Serialization;
using GitGet.Core.Interfaces;
using GitGet.Core.Models;

namespace GitGet.Core.Services;

public class GitHubApiClient : IGitHubApiClient
{
    private readonly INodeScriptRunner _scriptRunner;
    private string _accessToken = "";

    public int RemainingRateLimit { get; private set; } = 60;
    public DateTime? RateLimitResetAt { get; private set; }

    public GitHubApiClient(INodeScriptRunner scriptRunner)
    {
        _scriptRunner = scriptRunner;
    }

    public void SetAccessToken(string? token)
    {
        _accessToken = token ?? "";
    }

    public async Task<List<Repository>> SearchRepositoriesAsync(
        string query, string? language = null, string sort = "stars",
        int page = 1, int perPage = 20, CancellationToken ct = default)
    {
        var q = language != null ? $"{query}+language:{language}" : query;
        var args = new[] { "GET", "/search/repositories",
            JsonSerializer.Serialize(new { q, sort, order = "desc", page, per_page = perPage }), _accessToken };

        try
        {
            var json = await _scriptRunner.RunScriptAsync(args, ct);
            var searchResult = JsonSerializer.Deserialize<SearchResult>(json);
            UpdateRateLimit(json);

            var items = searchResult?.Items ?? new();
            return items.Select(item => MapRepository(item)).ToList();
        }
        catch
        {
            return new List<Repository>();
        }
    }

    public async Task<Repository?> GetRepositoryAsync(string owner, string repo, CancellationToken ct = default)
    {
        var args = new[] { "GET", $"/repos/{owner}/{repo}", "{}", _accessToken };

        try
        {
            var json = await _scriptRunner.RunScriptAsync(args, ct);
            UpdateRateLimit(json);
            var element = JsonDocument.Parse(json).RootElement;
            return MapRepository(element);
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<Release>> GetReleasesAsync(
        string owner, string repo, int page = 1, int perPage = 30, CancellationToken ct = default)
    {
        var args = new[] { "GET", $"/repos/{owner}/{repo}/releases",
            JsonSerializer.Serialize(new { page, per_page = perPage }), _accessToken };

        try
        {
            var json = await _scriptRunner.RunScriptAsync(args, ct);
            UpdateRateLimit(json);
            var raw = JsonDocument.Parse(json).RootElement;

            var releases = new List<Release>();
            foreach (var item in raw.EnumerateArray())
            {
                releases.Add(MapRelease(item));
            }
            return releases;
        }
        catch
        {
            return new List<Release>();
        }
    }

    public async Task<List<Repository>> GetStarredReposAsync(
        string username, int page = 1, int perPage = 50, CancellationToken ct = default)
    {
        var args = new[] { "GET", $"/users/{username}/starred",
            JsonSerializer.Serialize(new { page, per_page = perPage }), _accessToken };

        try
        {
            var json = await _scriptRunner.RunScriptAsync(args, ct);
            UpdateRateLimit(json);
            var raw = JsonDocument.Parse(json).RootElement;

            var repos = new List<Repository>();
            foreach (var item in raw.EnumerateArray())
            {
                repos.Add(MapRepository(item));
            }
            return repos;
        }
        catch
        {
            return new List<Repository>();
        }
    }

    public async Task<string?> GetReadmeContentAsync(string owner, string repo, CancellationToken ct = default)
    {
        var args = new[] { "GET", $"/repos/{owner}/{repo}/readme", "{}", _accessToken };

        try
        {
            var json = await _scriptRunner.RunScriptAsync(args, ct);
            UpdateRateLimit(json);
            var element = JsonDocument.Parse(json).RootElement;

            if (element.TryGetProperty("content", out var contentProp)
                && element.TryGetProperty("encoding", out var encodingProp))
            {
                var encoding = encodingProp.GetString() ?? "";
                var content = contentProp.GetString() ?? "";

                if (encoding == "base64" && !string.IsNullOrEmpty(content))
                {
                    // Remove newlines from base64 string before decoding
                    var cleaned = content.Replace("\n", "").Replace("\r", "");
                    var bytes = Convert.FromBase64String(cleaned);
                    return System.Text.Encoding.UTF8.GetString(bytes);
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<GitHubUser?> GetUserAsync(CancellationToken ct = default)
    {
        var args = new[] { "GET", "/user", "{}", _accessToken };

        try
        {
            var json = await _scriptRunner.RunScriptAsync(args, ct);
            UpdateRateLimit(json);
            var element = JsonDocument.Parse(json).RootElement;
            return MapUser(element);
        }
        catch
        {
            return null;
        }
    }

    private void UpdateRateLimit(string rawJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("__rateLimit", out var rateLimit))
            {
                if (rateLimit.TryGetProperty("limit", out var limitProp) && limitProp.TryGetInt32(out var limit))
                {
                    // Store for reference (main field is remaining)
                }
                if (rateLimit.TryGetProperty("remaining", out var remainingProp) && remainingProp.TryGetInt32(out var remaining))
                {
                    RemainingRateLimit = remaining;
                }
                if (rateLimit.TryGetProperty("reset", out var resetProp) && resetProp.TryGetInt32(out var resetUnix))
                {
                    RateLimitResetAt = DateTimeOffset.FromUnixTimeSeconds(resetUnix).UtcDateTime;
                }
            }
        }
        catch
        {
            // Ignore parse errors in metadata
        }
    }

    private static Repository MapRepository(JsonElement item)
    {
        return new Repository
        {
            Id = item.TryGetProperty("id", out var id) ? id.GetInt64() : 0,
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
                : new(),
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
                : new()
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
            Id = item.TryGetProperty("id", out var id) ? id.GetInt64() : 0,
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
        public List<JsonElement> Items { get; set; } = new();
    }
}