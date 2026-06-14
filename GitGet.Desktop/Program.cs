using System.Diagnostics;
using System.Text.Json;
using GitGet.Core.Models;
using GitGet.Core.Services;

namespace GitGet.Desktop;

public static class Program
{
    private const string RepoOwner = "illusogl";
    private const string RepoName = "recite-word-helper";
    private const string TagFilter = "1.0";
    private const int MaxRetries = 5;

    // Mirror for faster download in China
    private const string DownloadMirror = "https://ghproxy.com/";

    private static readonly HttpClient HttpClient = new()
    {
        BaseAddress = new Uri("https://api.github.com/"),
        DefaultRequestHeaders = { { "User-Agent", "GitGet-Dev/1.0" } },
        Timeout = TimeSpan.FromSeconds(30)
    };

    public static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("═══════════════════════════════════════════");
        Console.WriteLine("  GitGet — GitHub Release 快速下载工具");
        Console.WriteLine("═══════════════════════════════════════════");
        Console.WriteLine();

        try
        {
            // 1. Fetch releases
            Console.Write($"[1/4] 获取 Release: {RepoOwner}/{RepoName} ... ");
            var releases = await RetryAsync(() => FetchReleasesAsync(RepoOwner, RepoName));
            if (releases.Count == 0)
            {
                Console.WriteLine("未找到 Release");
                return;
            }

            var targetRelease = releases.FirstOrDefault(r =>
                string.Equals(r.TagName, TagFilter, StringComparison.OrdinalIgnoreCase))
                ?? releases.First();
            Console.WriteLine($"OK (标签: {targetRelease.TagName}, {targetRelease.Assets.Count} 个文件)");

            // 2. Match asset
            Console.Write("[2/4] 匹配 Windows 安装包 ... ");
            var matcher = new AssetMatcherService();
            var bestAsset = matcher.FindMatchingAsset(targetRelease.Assets);

            if (bestAsset == null && targetRelease.Assets.Count == 1)
            {
                bestAsset = targetRelease.Assets[0];
                Console.WriteLine($"唯一文件: {bestAsset.Name}");
            }
            else if (bestAsset == null)
            {
                Console.WriteLine("无匹配文件");
                foreach (var a in targetRelease.Assets)
                    Console.WriteLine($"  {a.Name} ({FormatSize(a.Size)})");
                WaitAndExit(); return;
            }
            else
            {
                Console.WriteLine($"OK ({bestAsset.Name}, {FormatSize(bestAsset.Size)})");
            }

            // 3. Path
            var downloadDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            Directory.CreateDirectory(downloadDir);
            var destPath = Path.Combine(downloadDir, bestAsset.Name);

            Console.WriteLine($"[3/4] 目标: {destPath}");
            Console.WriteLine($"[4/4] 开始下载 ({FormatSize(bestAsset.Size)})...");
            Console.WriteLine();

            // 4. Download — try multiple URLs (direct + mirror)
            bool success = false;
            var urls = new[]
            {
                bestAsset.DownloadUrl!,
                DownloadMirror + bestAsset.DownloadUrl!,
            };

            foreach (var url in urls)
            {
                Console.WriteLine($"  URL: {url[..Math.Min(url.Length, 70)]}...");
                success = await DownloadWithProgressAsync(url, destPath, bestAsset.Size);
                if (success) break;
                Console.WriteLine();
            }

            if (success)
            {
                Console.WriteLine();
                Console.WriteLine("═══════════════════════════════════════════");
                Console.WriteLine("  ✅ 下载完成！");
                Console.WriteLine($"  📁 {destPath}");
                Console.WriteLine("═══════════════════════════════════════════");
                try { Process.Start("explorer.exe", $"/select,\"{destPath}\""); } catch { }
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("═══════════════════════════════════════════");
                Console.WriteLine("  ❌ 所有下载方式均失败");
                Console.WriteLine("  请检查网络连接或使用 VPN");
                Console.WriteLine("═══════════════════════════════════════════");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine($"  ❌ 错误: {ex.Message}");
        }

        WaitAndExit();
    }

    // ============ Core logic ============

    private static async Task<List<Release>> FetchReleasesAsync(string owner, string repo)
    {
        var url = $"/repos/{owner}/{repo}/releases?per_page=10";
        var response = await HttpClient.GetStringAsync(url);
        var raw = JsonDocument.Parse(response).RootElement;

        var results = new List<Release>();
        foreach (var item in raw.EnumerateArray())
        {
            results.Add(new Release
            {
                Id = item.TryGetProperty("id", out var id) ? id.GetInt64() : 0,
                TagName = GetStr(item, "tag_name"),
                Name = GetStr(item, "name"),
                Body = GetStr(item, "body"),
                Prerelease = item.TryGetProperty("prerelease", out var pre) && pre.GetBoolean(),
                PublishedAt = TryDate(item, "published_at"),
                HtmlUrl = GetStr(item, "html_url"),
                Assets = item.TryGetProperty("assets", out var assets)
                    ? assets.EnumerateArray().Select(MapAsset).ToList() : new()
            });
        }
        return results;
    }

    private static ReleaseAsset MapAsset(JsonElement item) => new()
    {
        Id = item.TryGetProperty("id", out var i) ? i.GetInt64() : 0,
        Name = GetStr(item, "name"),
        Size = item.TryGetProperty("size", out var s) ? s.GetInt64() : 0,
        ContentType = GetStr(item, "content_type"),
        DownloadUrl = GetStr(item, "browser_download_url"),
        DownloadCount = item.TryGetProperty("download_count", out var dc) ? dc.GetInt32() : 0
    };

    private static async Task<bool> DownloadWithProgressAsync(
        string url, string destPath, long totalSize)
    {
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                if (attempt > 1)
                {
                    Console.WriteLine($"  ── 第 {attempt} 次重试 ──");
                    if (File.Exists(destPath)) File.Delete(destPath);
                }

                using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
                client.DefaultRequestHeaders.Add("User-Agent", "GitGet-Dev/1.0");

                using var response = await client.GetAsync(
                    url, HttpCompletionOption.ResponseHeadersRead);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"  HTTP {(int)response.StatusCode}");
                    await Task.Delay(2000 * attempt);
                    continue;
                }

                var contentLength = response.Content.Headers.ContentLength ?? totalSize;
                await using var stream = await response.Content.ReadAsStreamAsync();
                await using var fileStream = new FileStream(destPath, FileMode.Create,
                    FileAccess.Write, FileShare.None, 8192, useAsync: true);

                var buffer = new byte[8192];
                long totalRead = 0;
                var sw = Stopwatch.StartNew();
                int bytesRead;

                while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                    totalRead += bytesRead;

                    // Print progress every 256KB or 1 second
                    if (totalRead % (256 * 1024) < bytesRead ||
                        sw.ElapsedMilliseconds >= 1000)
                    {
                        PrintProgressLine(totalRead, contentLength, sw.Elapsed.TotalSeconds);
                        sw.Restart();
                    }
                }

                // Final
                PrintProgressLine(totalRead, totalRead, 1);
                Console.WriteLine();
                Console.Write("  OK ");

                return totalRead > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine($"  × {ex.Message}");
                if (attempt < MaxRetries)
                    await Task.Delay(3000 * attempt);
            }
        }

        return false;
    }

    private static void PrintProgressLine(long received, long total, double elapsedSeconds)
    {
        var pct = total > 0 ? (double)received / total * 100 : 0;
        int barW = 40;
        int filled = total > 0 ? (int)(received * barW / total) : 0;
        var bar = new string('█', filled) + new string('░', barW - filled);

        var speed = elapsedSeconds > 0
            ? received / elapsedSeconds / 1024.0 / 1024.0
            : 0;

        var eta = speed > 0 && total > received
            ? TimeSpan.FromSeconds((total - received) / (speed * 1024 * 1024))
            : TimeSpan.Zero;

        Console.Write($"\r  {bar} {pct,5:F1}%  {FormatSize(received)}/{FormatSize(total)}");
        if (speed > 0.01)
            Console.Write($"  {speed:F1} MB/s  {eta:hh\\:mm\\:ss}");
        Console.Write("  ");
    }

    // ============ Helpers ============

    private static async Task<T> RetryAsync<T>(Func<Task<T>> action, int maxRetries = 3)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try { return await action(); }
            catch when (i < maxRetries - 1) { await Task.Delay(1000 * (i + 1)); }
        }
        return await action();
    }

    private static string GetStr(JsonElement el, string name)
        => el.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString() ?? "" : "";

    private static DateTime TryDate(JsonElement el, string name)
        => el.TryGetProperty(name, out var prop)
        && prop.ValueKind == JsonValueKind.String
        && DateTime.TryParse(prop.GetString(), out var dt)
            ? dt : DateTime.MinValue;

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / (1024.0 * 1024.0):F1} MB"
    };

    private static void WaitAndExit()
    {
        if (!Console.IsInputRedirected) { Console.WriteLine(); Console.WriteLine("按任意键退出..."); Console.ReadKey(); }
    }
}