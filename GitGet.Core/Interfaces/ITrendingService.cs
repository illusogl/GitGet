using GitGet.Core.Models;

namespace GitGet.Core.Interfaces;

public interface ITrendingService
{
    /// <summary>
    /// 获取 GitHub 热门仓库排行
    /// </summary>
    /// <param name="timeRange">时间范围：daily / weekly / monthly / yearly / all</param>
    /// <param name="language">语言过滤（null = 全部语言）</param>
    /// <param name="page">页码</param>
    /// <param name="perPage">每页数量</param>
    Task<List<Repository>> GetTrendingReposAsync(
        string timeRange = "weekly",
        string? language = null,
        int page = 1,
        int perPage = 20,
        CancellationToken ct = default);
}