# 前端页面与缺失后端函数对照表

> **状态**：✅ 已实现 | ⬜ 需要实现

---

## 各页面对后端函数的依赖

### Home.razor — 首页热门推荐

| 功能 | 后端依赖 | 状态 |
|------|---------|------|
| 热门项目列表 | `GetTrendingReposAsync(timeRange)` | ⬜ |
| 按时间筛选（本周/本月/今年/全历史） | `timeRange` 参数：`daily` / `weekly` / `monthly` / `yearly` / `all` | ⬜ |
| 个性化推荐（已登录） | `IRecommendationService.GetRecommendedReposAsync(username)` | ⬜ |
| 点击跳详情 | `AppState.SelectedRepository` → 导航 | ✅ |
| 加载骨架屏 | 前端状态 | ✅ |

### Search.razor — 搜索

| 功能 | 后端依赖 | 状态 |
|------|---------|------|
| 关键词搜索 | `IGitHubApiClient.SearchRepositoriesAsync()` | ✅ |
| 语言筛选 | `language` 参数 | ✅ |
| 排序切换 | `sort` 参数 | ✅ |
| 分页加载 | `page` 参数 | ✅ |

### Detail.razor — 详情/下载

| 功能 | 后端依赖 | 状态 |
|------|---------|------|
| 仓库详情 | `GetRepositoryAsync()` | ✅ |
| Release 列表 | `GetReleasesAsync()` | ✅ |
| 匹配 Win 安装包 | `AssetMatcherService.FindMatchingAsset()` | ✅ |
| 下载文件 | `DownloadService.StartDownloadAsync()` | ✅ |

### Downloads.razor — 下载管理

| 功能 | 后端依赖 | 状态 |
|------|---------|------|
| 下载列表 | `GetTasksAsync()` | ✅ |
| 暂停/继续/取消 | `PauseDownloadAsync / ResumeDownloadAsync / CancelDownloadAsync` | ✅ |
| 进度显示 | `ProgressPercent` 属性 | ✅ |

### Settings.razor — 设置

| 功能 | 后端依赖 | 状态 |
|------|---------|------|
| 下载路径设置 | `IGitGetSettings.DownloadPath` | ⬜ |
| 最大并发数 | `IGitGetSettings.MaxConcurrentDownloads` | ⬜ |
| 清除缓存 | `ICacheService.ClearAllAsync()` | ✅ |
| 缓存大小 | `ICacheService.GetCacheSizeAsync()` | ✅ |

---

## 缺失的后端函数

### 1. ITrendingService — GitHub 热门排行（P0）

**需要新建**：`GitGet.Core/Interfaces/ITrendingService.cs`
**实现类**：`GitGet.Core/Services/TrendingService.cs`

```csharp
public interface ITrendingService
{
    Task<List<Repository>> GetTrendingReposAsync(
        string timeRange = "weekly",   // daily / weekly / monthly / yearly / all
        string? language = null,
        int page = 1,
        int perPage = 20,
        CancellationToken ct = default);
}
```

**与 GitHub API 的映射**：

| timeRange | Search API 查询 | 说明 |
|-----------|----------------------|------|
| `daily` | `q=created:>=今日日期&sort=stars` | 当天创建的热门 |
| `weekly` | `q=created:>=7天前&sort=stars` | 本周创建的热门 |
| `monthly` | `q=created:>=30天前&sort=stars` | 本月创建的热门 |
| `yearly` | `q=created:>=365天前&sort=stars` | 今年创建的热门 |
| `all` | `q=stars:>=10000&sort=stars` | 历史最热门 |

GitHub 没有原生 Trending API，用搜索 API + `created` 限定符实现。

**前端 Home.razor 中的调用**：

```razor
@inject ITrendingService TrendingService
<select @bind="_timeRange">
    <option value="daily">今天</option>
    <option value="weekly">本周</option>
    <option value="monthly">本月</option>
    <option value="all">历史</option>
</select>

@code {
    private string _timeRange = "weekly";
    protected override async Task OnInitializedAsync()
    {
        _trending = await TrendingService.GetTrendingReposAsync(_timeRange);
    }
}
```

---

### 2. IRecommendationService — 个性化推荐（P1）

**需要新建**：`GitGet.Core/Interfaces/IRecommendationService.cs`
**实现类**：`GitGet.Core/Services/RecommendationService.cs`

```csharp
public interface IRecommendationService
{
    Task<List<Repository>> GetRecommendedReposAsync(
        string? username = null,
        int count = 20,
        CancellationToken ct = default);
}
```

**算法**（已登录用户）：
1. 获取用户 Star 列表 → `GetStarredReposAsync(username)`
2. 统计 Top 3 语言 → 搜索 API 按语言 + 按 Star 排序
3. 合并去重 → 返回 Top 20
4. 缓存 1 小时

**降级**（未登录用户）：
→ 调用 `ITrendingService.GetTrendingReposAsync("weekly")`

---

### 3. IGitGetSettings — 设置持久化（P0）

**需要新建**：`GitGet.Core/Interfaces/IGitGetSettings.cs`
**实现类**：`GitGet.Core/Services/GitGetSettings.cs`

```csharp
public interface IGitGetSettings
{
    string DownloadPath { get; set; }
    int MaxConcurrentDownloads { get; set; }
    string Theme { get; set; }       // dark / light / system
    string Language { get; set; }    // zh / en

    Task LoadAsync(CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
}
```

**存储**：`%APPDATA%/GitGet/settings.json`

---

## 实现优先级

| 优先级 | 接口 | 理由 |
|--------|------|------|
| 🔴 P0 | `ITrendingService` | 首页当前是占位状态 |
| 🔴 P0 | `IGitGetSettings` | 设置页无法保存 |
| 🟡 P1 | `IRecommendationService` | 增强体验 |

*编制：2026年6月17日*