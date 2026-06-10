# GitGet 四阶段开发实施计划

> **基于现有设计文档整理的精简开发计划**
>
> **截止日期**：2026年6月20日（剩余11天）
>
> **技术栈**：C# + .NET 9 + Avalonia UI + SQLite
>
> **解决方案结构**：
> ```
> GitGet.sln
> ├── GitGet.Desktop      (Avalonia UI 桌面应用 - 入口)
> ├── GitGet.Core         (.NET 类库 - 业务逻辑+数据访问)
> └── GitGet.Core.Tests   (xUnit 单元测试)
> ```

---

## 阶段一：完成后端整体框架

> **目标**：搭建完整的后端基础设施，确保所有 Service 接口定义和基础实现可用，DI 容器配置正确，测试框架就绪。
>
> **预计工期**：2.5 天

### 1.1 项目脚手架搭建

| 任务 | 文件/产出 | 说明 |
|------|-----------|------|
| 创建 GitGet.sln + 三个项目 | 解决方案骨架 | `dotnet new sln` → 添加三个项目 |
| 安装 NuGet 依赖 | packages.config / csproj | Avalonia, CommunityToolkit.Mvvm, Microsoft.Data.Sqlite, Serilog, xUnit, Moq, FluentAssertions |
| 配置 DI 容器 | `Program.cs` / `App.axaml.cs` | Microsoft.Extensions.DependencyInjection 注册所有服务 |

### 1.2 数据模型层（Models）

| 任务 | 类名 | 说明 |
|------|------|------|
| 仓库模型 | `Repository` | Id, FullName, Description, Language, Stars, Topics... |
| Release 模型 | `Release` | Id, TagName, Name, Prerelease, Assets... |
| Asset 模型 | `ReleaseAsset` | Id, Name, Size, DownloadUrl, ContentType |
| 用户模型 | `GitHubUser` | Id, Login, AvatarUrl, Name, Email |
| 下载任务模型 | `DownloadTask` | TaskId, RepoFullName, FileName, Status, Progress... |
| 操作系统枚举 | `OSPlatform` | Windows, macOS, Linux (使用 RuntimeInformation 检测) |

### 1.3 接口定义（核心抽象层）

| 接口 | 主要方法 | 用途 |
|------|----------|------|
| `IGitHubApiClient` | `SearchRepositoriesAsync`, `GetRepositoryAsync`, `GetReleasesAsync`, `GetStarredReposAsync`, `GetUserAsync` | 封装所有 GitHub REST/GraphQL API 调用 |
| `ILocalDataStore` | `SaveAsync<T>`, `GetAsync<T>`, `DeleteAsync`, `QueryAsync<T>` | SQLite 数据持久化 |
| `ISecureDataStore` | `SaveTokenAsync`, `GetTokenAsync`, `ClearTokenAsync` | 加密存储 OAuth Token |
| `ICacheService` | `GetOrSetAsync<T>`, `InvalidateAsync`, `ClearAllAsync` | 本地缓存管理 |
| `IDownloadService` | `StartDownloadAsync`, `PauseDownload`, `ResumeDownload`, `CancelDownload`, `GetTasksAsync` | 下载任务管理 |
| `IAssetMatcherService` | `FindMatchingAsset`, `GetRecommendedAssets` | 按操作系统匹配 Asset 文件 |
| `ITokenService` | `LoginAsync`, `RefreshTokenAsync`, `LogoutAsync`, `IsLoggedIn`, `CurrentUser` | OAuth 认证管理 |
| `IRecommendationService` | `GetRecommendedReposAsync` | 个性化推荐引擎 |

### 1.4 Service 基础实现

#### 1.4.1 GitHubApiClient

```
GitHubApiClient : IGitHubApiClient
├── 构造函数: HttpClient (通过 IHttpClientFactory 注入)
├── 速率限制管理: 检查剩余配额，自动退避 (使用 RetryAfter 头)
├── 认证: 构造 Bearer Token 头
├── SearchRepositoriesAsync  →  GET /search/repositories
├── GetRepositoryAsync       →  GET /repos/{owner}/{repo}
├── GetReleasesAsync         →  GET /repos/{owner}/{repo}/releases
├── GetStarredReposAsync     →  GET /users/{user}/starred (分页处理)
└── GetUserAsync             →  GET /user
```

#### 1.4.2 LocalDataStore

```
LocalDataStore : ILocalDataStore
├── SQLite 数据库初始化 (表结构创建)
├── 表: cache_repos, cache_releases, download_tasks
├── 索引: 按 full_name, tag_name, task_id
└── 基于 Microsoft.Data.Sqlite
```

#### 1.4.3 SecureDataStore

```
SecureDataStore : ISecureDataStore
├── Token 使用 AES-256-GCM 加密
├── 存储位置: %APPDATA%/GitGet/token.enc
└── 数据保护: Windows 下使用 DPAPI 辅助保护密钥
```

#### 1.4.4 CacheService

```
CacheService : ICacheService
├── 包装 LocalDataStore
├── 缓存策略: 仓库缓存 1h, Release 30min, 搜索 10min, Star 列表 6h
├── 自动清理过期条目
└── 最大条目限制 (仓库 500, Release 300, 搜索 200)
```

### 1.5 测试就绪

| 任务 | 说明 |
|------|------|
| xUnit 测试项目配置 | 引用 GitGet.Core，安装 Moq + FluentAssertions |
| Mock HttpMessageHandler | 自定义 DelegatingHandler 模拟 GitHub API 响应 |
| 预设测试数据 | JSON 格式的模拟 API 响应文件 |
| 第一个测试用例 | `GitHubApiClient_SearchRepositories_ReturnsResults` |

### 1.6 阶段一验收标准

- [ ] 解决方案编译通过，无错误
- [ ] DI 容器配置完成，所有服务可正确解析
- [ ] IGitHubApiClient 的模拟测试通过
- [ ] SQLite 本地数据库可正常初始化和读写
- [ ] Token 加密存储可正常工作

---

## 阶段二：完成下载相关功能并通过相关测试

> **目标**：实现 OAuth 登录、项目详情浏览、Asset 智能匹配、带进度条的下载管理，并且通过单元测试和集成测试验证。
>
> **依赖**：阶段一的后端框架已完成
>
> **预计工期**：3.5 天

### 2.1 TokenService（OAuth 登录）

```
TokenService : ITokenService
├── LoginAsync()
│   ├── 1. 启动本地 HTTP 侦听器 (http://localhost:{port}/callback)
│   ├── 2. 打开系统浏览器跳转到 GitHub OAuth 授权页面
│   ├── 3. 接收回调获取授权码
│   ├── 4. POST 授权码 → 换取 Access Token
│   ├── 5. 调用 /user 获取用户信息
│   └── 6. 加密存储 Token (SecureDataStore)
├── RefreshTokenAsync()  →  重新执行 OAuth 流程
├── LogoutAsync()        →  清除 Token
├── IsLoggedIn           →  检查 Token 是否存在且未过期
└── CurrentUser          →  当前登录用户信息
```

### 2.2 AssetMatcherService

```
AssetMatcherService : IAssetMatcherService
├── FindMatchingAsset(assets, currentOS) → 最优匹配
├── GetRecommendedAssets(assets) → 标记推荐列表
├── 匹配规则:
│   ├── Windows: .exe, .msi, .zip (含 win/windows)
│   ├── macOS: .dmg, .pkg, .app (含 mac/darwin/apple)
│   └── Linux: .deb, .rpm, .AppImage, .tar.gz (含 linux/ubuntu)
└── 评分权重: 架构匹配 (x64/arm64) > 平台匹配 > 安装包类型
```

### 2.3 DownloadService（下载管理器）

```
DownloadService : IDownloadService
├── 数据结构: ConcurrentDictionary<string, DownloadTask>
├── 下载队列: SemaphoreSlim 控制并发 (默认 3，最大 10)
│
├── StartDownloadAsync(url, destPath, progress, ct)
│   ├── 1. 创建 DownloadTask (GUID TaskId)
│   ├── 2. 检查目标磁盘空间 (DriveInfo.AvailableFreeSpace)
│   ├── 3. 检查文件冲突 → 生成唯一文件名
│   ├── 4. POST 任务 → 后台队列
│   └── 5. 流式下载:
│       ├── HttpCompletionOption.ResponseHeadersRead
│       ├── Content.Headers.ContentLength → totalBytes
│       ├── 循环: ReadAsync(buffer) → WriteAsync(fileStream)
│       ├── 每 256KB → 报告 IProgress<DownloadProgress>
│       └── 异常处理: 网络中断 → 自动重试 (最多 3 次)
│
├── PauseDownload(taskId)
│   ├── 取消 CancellationTokenSource
│   ├── 记录已下载字节数到 SQLite
│   └── 标记状态为 Paused
│
├── ResumeDownload(taskId)
│   ├── 读取已下载字节数
│   ├── Range: bytes={downloaded}- 请求
│   └── 继续流式写入
│
├── CancelDownload(taskId)
│   ├── 取消 CancellationTokenSource
│   ├── 删除临时文件
│   └── 更新状态为 Cancelled
│
└── GetTasksAsync() → 从 SQLite 读取所有任务
```

### 2.4 下载进度数据结构

```csharp
public class DownloadProgress
{
    public string TaskId { get; set; }
    public string FileName { get; set; }
    public long TotalBytes { get; set; }      // 文件总大小
    public long ReceivedBytes { get; set; }   // 已接收
    public double Percent => TotalBytes > 0
        ? Math.Round((double)ReceivedBytes / TotalBytes * 100, 1)
        : 0;
    public double SpeedBytesPerSecond { get; set; }  // 速度
    public TimeSpan EstimatedTimeRemaining { get; set; } // 剩余时间
    public DownloadTaskStatus Status { get; set; }  // 状态枚举
}
```

### 2.5 单元测试

| 测试用例 | 测试内容 | 所属模块 |
|----------|----------|----------|
| `TokenService_Login_Success` | 模拟 OAuth 完整流程，验证 Token 存储 | TokenService |
| `AssetMatcher_Windows_ExeSelected` | Windows 下 .exe 被推荐 | AssetMatcher |
| `AssetMatcher_macOS_DmgSelected` | macOS 下 .dmg 被推荐 | AssetMatcher |
| `AssetMatcher_NoMatch_ReturnsNull` | 无匹配时返回 null | AssetMatcher |
| `DownloadService_CreateTask_Valid` | 创建下载任务，检查状态 | DownloadService |
| `DownloadService_Progress_Reports` | 模拟流式读取，验证进度百分比 | DownloadService |
| `DownloadService_PauseResume_Continuity` | 暂停后继续，验证 Range 请求头 | DownloadService |
| `DownloadService_ConcurrentLimit` | 验证并发下载数不超过配置 | DownloadService |
| `DownloadService_DiskFull_Handling` | 磁盘空间不足时的错误处理 | DownloadService |

### 2.6 阶段二验收标准

- [ ] OAuth 登录完整流程可用（含错误处理）
- [ ] Asset 匹配在 Windows/macOS/Linux 上均能正确推荐
- [ ] 下载任务可正常创建、暂停、继续、取消
- [ ] 进度条每秒至少更新 1 次
- [ ] 所有下载相关单元测试通过
- [ ] 模拟 API 断网后自动重试逻辑验证通过

---

## 阶段三：完成前端框架并通过相关测试

> **目标**：使用 Avalonia UI + MVVM 完成全部界面，对接后端 Service，实现完整的用户操作流程，并通过 UI 测试。
>
> **依赖**：阶段二（需要后端服务可调用）
>
> **预计工期**：3 天

### 3.1 主窗口框架

```
MainWindow.axaml
├── TitleBar (自定义窗口标题栏)
│   ├── 应用图标 + 标题 "GitGet"
│   └── 窗口控制 (最小化/最大化/关闭)
│
├── Sidebar (左侧导航栏)
│   ├── 用户头像区 (登录/未登录状态)
│   ├── 导航项:
│   │   ├── 🏠 首页推荐
│   │   ├── 🔍 项目搜索
│   │   ├── ⬇️ 下载管理
│   │   └── ⚙️ 设置
│   └── 登录/退出按钮
│
└── ContentArea (右侧内容区: ContentControl 绑定当前视图)
```

### 3.2 视图与 ViewModel

#### 3.2.1 HomeView / HomeViewModel

```
HomeView.axaml
├── Header: "🔥 热门推荐" / "👤 为您推荐"
├── 项目卡片列表 (ItemsRepeater / ListBox)
│   └── ProjectCard (自定义控件)
│       ├── 仓库全名 (owner/repo)
│       ├── 描述 (截断 2 行)
│       ├── ⭐ Star 数
│       ├── 主语言标签 (颜色标识)
│       └── 最近更新时间 (相对时间 "3 天前")

HomeViewModel
├── [ObservableProperty] Repositories → ObservableCollection<Repository>
├── [ObservableProperty] IsLoading
├── [ObservableProperty] RecommendationLabel
├── [RelayCommand] LoadRecommendationsAsync()
└── [RelayCommand] NavigateToDetail(Repository)
```

#### 3.2.2 SearchView / SearchViewModel

```
SearchView.axaml
├── SearchBar (顶部搜索栏 + 搜索按钮)
├── 筛选选项 (语言下拉框, 排序方式)
├── 搜索结果列表 (与 HomeView 共用 ProjectCard)
└── 分页加载 (IncrementalLoading / 滚动触发)

SearchViewModel
├── [ObservableProperty] SearchQuery
├── [ObservableProperty] SelectedLanguage
├── [ObservableProperty] SelectedSort (stars / updated)
├── [ObservableProperty] SearchResults → ObservableCollection<Repository>
├── [ObservableProperty] IsSearching
├── [ObservableProperty] TotalCount
├── [RelayCommand] SearchAsync()
├── [RelayCommand] LoadNextPageAsync()
└── [RelayCommand] NavigateToDetail(Repository)
```

#### 3.2.3 DetailView / DetailViewModel

```
DetailView.axaml
├── 仓库信息头部:
│   ├── 仓库名 + 描述
│   ├── ⭐ ⑂ 👁 统计
│   ├── 许可证 + 主页链接
│   └── 主语言 + Topics 标签
│
├── Release 列表 (Expander 可展开)
│   └── 每个 Release:
│       ├── 版本号 + 发布日期
│       ├── 预发布标记 (prerelease)
│       ├── Release Notes (Markdown 渲染)
│       └── Asset 列表:
│           ├── Asset 文件名 + 大小
│           ├── "推荐下载" 徽标 (AssetMatcher 匹配)
│           └── ⬇️ 下载按钮

DetailViewModel
├── [ObservableProperty] Repository (选中项目)
├── [ObservableProperty] Releases → ObservableCollection<Release>
├── [ObservableProperty] IsLoading
├── [ObservableProperty] RecommendedAssets → 标记匹配
├── [RelayCommand] LoadDetailAsync(string repoFullName)
├── [RelayCommand] DownloadAsset(ReleaseAsset)
└── 初始化: 传入 repoFullName 参数
```

#### 3.2.4 DownloadPanelView / DownloadPanelViewModel

```
DownloadPanelView.axaml
├── 下载任务列表 (实时更新)
│   └── DownloadItem (自定义控件)
│       ├── 文件名 + 仓库名
│       ├── ProgressBar (进度条)
│       ├── 百分比 + 速度 + 剩余时间
│       └── 控制按钮: 暂停/继续/取消/打开文件夹

DownloadPanelViewModel
├── [ObservableProperty] DownloadTasks → ObservableCollection<DownloadTask>
├── 订阅 IDownloadService 事件:
│   ├── OnTaskCreated → 添加任务
│   ├── OnProgressChanged → 更新对应任务进度
│   └── OnTaskCompleted → 标记完成 / 显示通知
├── [RelayCommand] Pause(string taskId)
├── [RelayCommand] Resume(string taskId)
├── [RelayCommand] Cancel(string taskId)
└── [RelayCommand] OpenFolder(string path)
```

#### 3.2.5 SettingsView / SettingsViewModel

```
SettingsView.axaml
├── 下载设置:
│   ├── 默认下载路径 (文件夹选择对话框)
│   └── 最大并发下载数 (Slider 1-10)
├── 外观设置:
│   ├── 主题切换 (亮色/暗色/跟随系统)
│   └── 语言选择 (中文/English)
├── 缓存管理:
│   ├── 当前缓存大小 (显示)
│   └── 清除缓存按钮
└── 关于信息:
    └── 版本号 + GitHub 链接

SettingsViewModel
├── 设置持久化: JSON 配置文件 (%APPDATA%/GitGet/settings.json)
├── [ObservableProperty] DownloadPath
├── [ObservableProperty] MaxConcurrentDownloads
├── [ObservableProperty] SelectedTheme
├── [ObservableProperty] SelectedLanguage
├── [RelayCommand] SaveSettingsAsync()
├── [RelayCommand] ClearCacheAsync()
└── [RelayCommand] SelectDownloadPathAsync()
```

#### 3.2.6 UserProfileView / UserProfileViewModel

```
UserProfileView.axaml
├── 用户信息区: 头像 + 用户名 + 邮箱
├── Star 列表 (分页)
├── 下载历史
└── 退出登录按钮

UserProfileViewModel
├── [ObservableProperty] CurrentUser
├── [ObservableProperty] StarredRepos → ObservableCollection<Repository>
├── [ObservableProperty] DownloadHistory → ObservableCollection<DownloadTask>
├── [RelayCommand] LoadProfileAsync()
├── [RelayCommand] LoadStarredAsync()
├── [RelayCommand] LoadHistoryAsync()
└── [RelayCommand] LogoutAsync()
```

### 3.3 导航系统

```
NavigationService (单例, 注入 DI 容器)
├── 当前视图 → ContentControl 绑定
├── INavigationService 接口:
│   ├── NavigateTo<TView>() → 切换主视图
│   ├── NavigateToDetail(string repoFullName) → 传参跳转
│   └── GoBack()
└── 视图注册: 使用 DI 容器自动获取 View + ViewModel
```

### 3.4 UI 测试（Avalonia Headless）

| 测试用例 | 说明 |
|----------|------|
| `HomeView_LoadsRecommendations` | 首页加载时自动调用 RecommendService |
| `SearchView_Input_TriggersSearch` | 输入关键词后点击搜索触发 API 调用 |
| `DetailView_ShowsReleaseList` | 项目详情页正确展示 Release 和 Asset |
| `DownloadPanel_ProgressUpdates` | 下载面板实时更新进度条 |
| `Settings_SavesAndLoads` | 设置保存后重新打开保持不变 |
| `Navigation_BetweenViews` | 导航在首页/搜索/下载间切换正常 |
| `OAuth_LoginButton_Click` | 点击登录按钮打开浏览器 |

### 3.5 阶段三验收标准

- [ ] 所有 6 个页面可正常导航和渲染
- [ ] 搜索功能可用，搜索结果分页加载
- [ ] 项目详情页面展示 Release 列表和 Asset
- [ ] 下载管理面板显示任务并实时更新进度
- [ ] 设置保存后持久化，重启后生效
- [ ] UI 测试通过
- [ ] 主题切换生效

---

## 阶段四：完成推荐功能

> **目标**：实现个性化推荐引擎，利用用户的 Star 数据计算兴趣标签权重，推荐相关热门项目。同时实现推荐降级策略（未登录→Trending）。
>
> **依赖**：阶段一（API 客户端 + 缓存）和阶段三（前端展示）
>
> **预计工期**：2 天

### 4.1 推荐引擎逻辑

```
RecommendationService : IRecommendationService
├── GetRecommendedReposAsync()
│   ├── 已登录用户:
│   │   ├── 1. 从缓存/API 获取用户的 Star 仓库列表
│   │   ├── 2. 统计各语言出现次数 → 语言权重分布
│   │   │    e.g. { "C#": 0.35, "Python": 0.25, "JavaScript": 0.20, "Rust": 0.20 }
│   │   ├── 3. 统计各 Topics 出现次数 → 话题权重
│   │   ├── 4. 取 Top 3 语言作为搜索关键词
│   │   ├── 5. 调用 GitHub Search API:
│   │   │    GET /search/repositories?q=language:C#&sort=stars&order=desc
│   │   ├── 6. 合并结果 (多语言查询), 去重
│   │   ├── 7. 按 Star 数排序 → 取 Top 20
│   │   ├── 8. 缓存结果 (1小时有效期)
│   │   └── 9. 返回推荐列表
│   │
│   └── 未登录用户:
│       ├── 1. 调用 GitHub Search API: 按 Star 数降序
│       │    GET /search/repositories?q=stars:>10000&sort=stars&order=desc
│       └── 2. 取 Top 20 (全局热门)
│
└── RefreshInterestModelAsync()
    ├── 强制刷新用户兴趣模型
    ├── 由登录事件和定时任务 (每6小时) 触发
    └── 结果存入 SQLite (user_interest_weights 表)
```

### 4.2 兴趣模型数据结构

```csharp
public class UserInterestModel
{
    public Dictionary<string, double> LanguageWeights { get; set; }
    // e.g. { "C#": 0.35, "Python": 0.25, "JavaScript": 0.20 }

    public Dictionary<string, double> TopicWeights { get; set; }
    // e.g. { "editor": 0.15, "dotnet": 0.12, "web": 0.10 }

    public DateTime LastUpdated { get; set; }
    public int TotalStarredRepos { get; set; }
}
```

### 4.3 SQLite 推荐表

```sql
CREATE TABLE IF NOT EXISTS user_interest_model (
    id INTEGER PRIMARY KEY,
    language_weights TEXT NOT NULL,  -- JSON: {"C#":0.35,"Python":0.25}
    topic_weights TEXT NOT NULL,     -- JSON: {"editor":0.15,"dotnet":0.12}
    last_updated TEXT NOT NULL,      -- ISO 8601
    total_starred INTEGER DEFAULT 0
);

CREATE TABLE IF NOT EXISTS recommended_cache (
    id INTEGER PRIMARY KEY,
    recommendation_type TEXT NOT NULL,  -- "personalized" or "trending"
    repos_json TEXT NOT NULL,           -- JSON array of Repository
    created_at TEXT NOT NULL,
    expires_at TEXT NOT NULL
);
```

### 4.4 推荐引擎单元测试

| 测试用例 | 说明 |
|----------|------|
| `Recommendation_LoggedIn_ReturnsPersonalized` | 已登录用户返回基于 Star 的推荐 |
| `Recommendation_NotLoggedIn_ReturnsTrending` | 未登录用户返回全局 Trending |
| `Recommendation_StarLanguage_Top3Used` | 仅使用 Top 3 语言搜索以控制 API 调用数 |
| `Recommendation_Results_Cached` | 推荐结果正确缓存 |
| `Recommendation_CacheExpired_Refetches` | 缓存过期后自动重新请求 |
| `Recommendation_NoStars_UsesTrending` | 新用户无 Star → 降级为 Trending |
| `InterestModel_CalculatesCorrectly` | 兴趣权重计算正确性验证 |
| `Recommendation_EmptyResult_HandlesGracefully` | API 返回空结果时的降级处理 |

### 4.5 推荐页面 UI 集成

| 任务 | 说明 |
|------|------|
| 首页已登录推荐标签 | 显示 "👤 为您推荐" + 项目卡片列表 |
| 首页未登录推荐标签 | 显示 "🔥 热门项目" + 项目卡片列表 |
| 刷新按钮 | 手动刷新推荐结果 |
| 加载状态 | 加载中显示 Skeleton 骨架屏 |
| 空状态提示 | 无推荐结果时显示友好提示 |

### 4.6 阶段四验收标准

- [ ] 已登录用户首页显示个性化推荐（基于 Star 语言分类）
- [ ] 未登录用户首页显示全局 Trending
- [ ] 兴趣模型在登录后正确计算
- [ ] 推荐结果有缓存机制，过期后自动刷新
- [ ] 空 Star 用户降级处理正常
- [ ] 推荐相关单元测试全部通过

---

## 总体时间线

```
日期         阶段                        里程碑
6月9日       阶段一：后端整体框架  ████████░░  2.5天
6月11日      阶段二：下载功能+测试  ██████████░░░░  3.5天
6月14日      阶段三：前端框架+测试  ██████████░░░░  3天
6月17日      阶段四：推荐功能       █████░░░░░░░░  2天
6月17-20日   缓冲/修复/发布        ████████░░
```

| 阶段 | 起止日期 | 工时 |
|------|----------|------|
| 阶段一：后端整体框架 | 6月9日 - 6月11日 | ~20h |
| 阶段二：下载功能 + 测试 | 6月11日 - 6月14日 | ~28h |
| 阶段三：前端框架 + 测试 | 6月14日 - 6月17日 | ~24h |
| 阶段四：推荐功能 | 6月17日 - 6月18日 | ~16h |
| 缓冲/修复/发布 | 6月18日 - 6月20日 | ~16h |

---

## 模块依赖关系（明确阶段间的依赖）

```
阶段一 ────────→ 阶段二 ────────→ 阶段三 ────────→ 阶段四
  │                  │                  │
  ├─ DI 容器         ├─ OAuth 登录       ├─ 所有 View + VM
  ├─ 数据模型        ├─ Asset 匹配       ├─ 导航系统
  ├─ 接口定义        ├─ 下载管理器       ├─ 首页 UI
  ├─ GitHubApiClient ├─ Token 加密       ├─ 搜索 UI
  ├─ LocalDataStore  ├─ 下载单元测试      ├─ 详情 UI
  ├─ SecureDataStore │                  ├─ 下载面板 UI
  └─ CacheService    │                  └─ UI 测试
                      │
                      └──────── 依赖阶段一的 GitHubApiClient
                                依赖阶段一的 CacheService
                                依赖阶段一的 LocalDataStore

阶段四 ──── 依赖阶段一的 GitHubApiClient + CacheService
         ──── 依赖阶段三的 HomeView/HomeViewModel (展示入口)
```

---

*编制日期：2026年6月9日*
*技术栈：C# + .NET 9 + Avalonia UI + SQLite*
*测试框架：xUnit + Moq + FluentAssertions*