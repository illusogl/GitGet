using GitGet.Core.Models;

namespace GitGet.Core.Interfaces;

public interface IRecommendationService
{
    /// <summary>
    /// 根据用户 Star 记录生成个性化推荐。
    /// 降级策略：未登录（username=null）时返回全局 Trending。
    /// </summary>
    Task<List<Repository>> GetRecommendedReposAsync(
        string? username = null,
        int count = 20,
        CancellationToken ct = default);
}