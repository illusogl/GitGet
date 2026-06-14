using System.Runtime.InteropServices;
using GitGet.Core.Interfaces;
using GitGet.Core.Models;

namespace GitGet.Core.Services;

public class AssetMatcherService : IAssetMatcherService
{
    private const int ArchitectureMatchWeight = 100;
    private const int WindowsKeywordWeight = 80;
    private const int ExtensionMatchWeight = 60;

    public ReleaseAsset? FindMatchingAsset(IEnumerable<ReleaseAsset> assets)
    {
        var assetList = assets.ToList();
        if (!assetList.Any()) return null;

        var scored = GetRecommendedAssets(assetList);
        var best = scored.OrderByDescending(kv => kv.Value).FirstOrDefault();
        if (best.Value <= 0) return null;

        return assetList.FirstOrDefault(a => a.Id == best.Key);
    }

    public Dictionary<long, int> GetRecommendedAssets(IEnumerable<ReleaseAsset> assets)
    {
        var assetList = assets.ToList();
        var scores = new Dictionary<long, int>();
        bool anyHasOSKeyword = false;

        foreach (var asset in assetList)
        {
            int score = CalculateWindowsScore(asset.Name);
            scores[asset.Id] = score;

            var lower = asset.Name.ToLowerInvariant();
            if (lower.Contains("win") || lower.Contains("windows") ||
                lower.Contains("mac") || lower.Contains("darwin") ||
                lower.Contains("linux") || lower.Contains("ubuntu"))
                anyHasOSKeyword = true;
        }

        // Fallback: if no asset has any OS keyword, treat archives as neutral
        // but still exclude source/checksum files
        if (!anyHasOSKeyword && assetList.Any())
        {
            foreach (var asset in assetList)
            {
                var lower = asset.Name.ToLowerInvariant();

                // Skip source archives and checksum files even in fallback
                if (lower.Contains("source") || lower.Contains("src"))
                    continue;
                if (lower.EndsWith(".sha256") || lower.EndsWith(".md5") ||
                    lower.EndsWith(".asc") || lower.EndsWith(".sig") ||
                    lower.EndsWith(".md") || lower.EndsWith(".txt"))
                    continue;

                if (lower.EndsWith(".zip") || lower.EndsWith(".7z") ||
                    lower.EndsWith(".rar") || lower.EndsWith(".exe") ||
                    lower.EndsWith(".msi") || lower.EndsWith(".tar.gz") ||
                    lower.EndsWith(".tgz") || lower.EndsWith(".tar.xz"))
                {
                    scores[asset.Id] = 1;
                }
            }
        }

        return scores;
    }

    private int CalculateWindowsScore(string assetName)
    {
        if (string.IsNullOrWhiteSpace(assetName))
            return -100;

        int score = 0;
        var lower = assetName.ToLowerInvariant();

        // Windows keyword bonus
        if (lower.Contains("win") || lower.Contains("windows"))
            score += WindowsKeywordWeight;

        // Penalize non-Windows packages
        if (lower.Contains("mac") || lower.Contains("darwin") ||
            lower.Contains("osx") || lower.Contains("apple") ||
            lower.Contains("linux") || lower.Contains("ubuntu") ||
            lower.Contains("debian") || lower.Contains("fedora"))
            score -= 40;

        // Architecture matching
        var currentArch = RuntimeInformation.ProcessArchitecture;
        score += currentArch switch
        {
            Architecture.X64 => lower.Contains("x64") || lower.Contains("x86_64") || lower.Contains("amd64")
                ? ArchitectureMatchWeight
                : lower.Contains("arm64") || lower.Contains("aarch64") ? -30
                : lower.Contains("x86") && !lower.Contains("x64") && !lower.Contains("x86_64") ? 20 : 0,
            Architecture.Arm64 => lower.Contains("arm64") || lower.Contains("aarch64")
                ? ArchitectureMatchWeight
                : lower.Contains("x64") || lower.Contains("x86_64") || lower.Contains("amd64") ? -30 : 0,
            Architecture.X86 => lower.Contains("x86") && !lower.Contains("x64") && !lower.Contains("x86_64")
                ? ArchitectureMatchWeight
                : 0,
            _ => 0
        };

        // Extension matching (Windows only) — only boost if OS keyword matched
        if (lower.EndsWith(".exe")) score += ExtensionMatchWeight + 15;
        else if (lower.EndsWith(".msi")) score += ExtensionMatchWeight + 10;
        else if (lower.EndsWith(".zip") && (lower.Contains("win") || lower.Contains("windows")))
            score += ExtensionMatchWeight;
        else if (lower.EndsWith(".7z") && (lower.Contains("win") || lower.Contains("windows")))
            score += ExtensionMatchWeight - 10;

        // Penalize non-executable files
        if (lower.EndsWith(".sha256") || lower.EndsWith(".md5") ||
            lower.EndsWith(".asc") || lower.EndsWith(".sig") ||
            lower.EndsWith(".md") || lower.EndsWith(".txt"))
            score -= 60;

        // Source archives
        if (lower.Contains("source") || lower.Contains("src"))
            score -= 30;

        return score;
    }
}