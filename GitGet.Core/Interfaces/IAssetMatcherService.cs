using GitGet.Core.Models;

namespace GitGet.Core.Interfaces;

public interface IAssetMatcherService
{
    /// <summary>
    /// Find the best matching asset for the current operating system.
    /// </summary>
    ReleaseAsset? FindMatchingAsset(IEnumerable<ReleaseAsset> assets);

    /// <summary>
    /// Mark which assets are recommended for the current OS.
    /// Returns a dictionary mapping asset ID to a recommendation score (higher = better match).
    /// </summary>
    Dictionary<long, int> GetRecommendedAssets(IEnumerable<ReleaseAsset> assets);
}