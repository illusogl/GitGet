# GitGet.Core API 参考文档

> **版本**：v1.0  
> **编制日期**：2026年6月17日  
> **技术栈**：C# / .NET 9 / SQLite / Node.js  
> **目标平台**：Windows  

---

## 一、接口层（Interfaces）

### 1. IGitHubApiClient — GitHub API 客户端

**文件**：`GitGet.Core/Interfaces/IGitHubApiClient.cs`

#### SearchRepositoriesAsync

```csharp
Task<List<Repository>> SearchRepositoriesAsync(
    string query,
    string? language = null,
    string sort = "stars",
    int page = 1,
    int perPage = 20,
    CancellationToken ct = default
)
```

搜索 GitHub 仓库。

| 参数 | 类型 | 说明 | 默认值 |
|------|------|------|--------|
| `query` | string | 搜索关键词（必填） | — |
| `language` | string? | 编程语言过滤（如 "C#"），null 表示不过滤 | null |
| `sort` | string | 排序字段：`"stars"` / `"forks"` / `"updated"` | "stars" |
| `page` | int | 页码（从 1 开始） | 1 |
| `perPage` | int | 每页数量（最大 100） | 20 |
| `ct` | CancellationToken | 取消令牌 | default |

**返回值**：`List<Repository>` — 匹配的仓库列表，无结果时为空列表。

---

#### GetRepositoryAsync

```csharp
Task<Repository?> GetRepositoryAsync(
    string owner,
    string repo,
    CancellationToken ct = default
)
```

获取单个仓库的详细信息。

| 参数 | 类型 | 说明 |
|------|------|------|
| `owner` | string | 仓库所有者用户名 |
| `repo` | string | 仓库名称 |
| `ct` | CancellationToken | 取消令牌 |

**返回值**：`Repository?` — 仓库对象，不存在时返回 null。

---

#### GetReleasesAsync

```csharp
Task<List<Release>> GetReleasesAsync(
    string owner,
    string repo,
    int page = 1,
    int perPage = 30,
    CancellationToken ct = default
)
```

获取仓库的 Release 版本列表。

| 参数 | 类型 | 说明 | 默认值 |
|------|------|------|--------|
| `owner` | string | 仓库所有者 | — |
| `repo` | string | 仓库名 | — |
| `page` | int | 页码 | 1 |
| `perPage` | int | 每页数量 | 30 |
| `ct` | CancellationToken | 取消令牌 | default |

**返回值**：`List<Release>` — Release 列表。每个 Release 包含 `Assets` 数组。

---

#### GetStarredReposAsync

```csharp
Task<List<Repository>> GetStarredReposAsync(
    string username,
    int page = 1,
    int perPage = 50,
    CancellationToken ct = default
)
```

获取用户 Star 过的仓库列表（用于个性化推荐）。

| 参数 | 类型 | 说明 |
|------|------|------|
| `username` | string | 用户名 |
| `page` | int | 页码 |
| `perPage` | int | 每页数量 |
| `ct` | CancellationToken | 取消令牌 |

---

#### GetUserAsync

```csharp
Task<GitHubUser?> GetUserAsync(CancellationToken ct = default)
```

获取当前认证用户的个人信息。需要有效的访问 Token。

**返回值**：`GitHubUser?` — 用户信息，未认证时返回 null。

---

#### 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `RemainingRateLimit` | int | 当前小时剩余的 API 调用次数。匿名：60；已认证：5000 |
| `RateLimitResetAt` | DateTime? | 速率限制重置时间（UTC） |

---

### 2. ILocalDataStore — 本地数据存储

**文件**：`GitGet.Core/Interfaces/ILocalDataStore.cs`

基于 SQLite 的键值存储接口。所有数据以 JSON 格式存储在 `data` 列中。

#### SaveAsync\<T\>

```csharp
Task SaveAsync<T>(string key, T data, CancellationToken ct = default)
    where T : class
```

保存任意类型的数据。

| 参数 | 类型 | 说明 |
|------|------|------|
| `key` | string | 唯一键（如 `"download_{taskId}"`） |
| `data` | T | 要保存的对象（自动 JSON 序列化） |
| `ct` | CancellationToken | 取消令牌 |

---

#### GetAsync\<T\>

```csharp
Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    where T : class
```

根据键读取数据。

| 参数 | 类型 | 说明 |
|------|------|------|
| `key` | string | 键名 |
| `ct` | CancellationToken | 取消令牌 |

**返回值**：`T?` — 反序列化后的对象，键不存在时返回 null。

---

#### DeleteAsync

```csharp
Task DeleteAsync(string key, CancellationToken ct = default)
```

删除指定键的数据。

---

#### QueryAsync\<T\>

```csharp
Task<List<T>> QueryAsync<T>(
    string tableName,
    string? whereClause = null,
    Dictionary<string, object>? parameters = null,
    CancellationToken ct = default
) where T : class, new()
```

按 SQL WHERE 条件查询数据（支持 `json_extract` 函数）。

| 参数 | 类型 | 说明 |
|------|------|------|
| `tableName` | string | 表名（`"cache_repos"` / `"cache_releases"` / `"download_tasks"`） |
| `whereClause` | string? | SQL WHERE 子句（如 `"stars > @min"`） |
| `parameters` | Dictionary\<string, object\>? | 参数化查询的参数集合 |
| `ct` | CancellationToken | 取消令牌 |

---

#### InitializeAsync

```csharp
Task InitializeAsync(CancellationToken ct = default)
```

初始化数据库，创建必要的表（`cache_repos`, `cache_releases`, `download_tasks`）。

---

### 3. ISecureDataStore — 加密存储

**文件**：`GitGet.Core/Interfaces/ISecureDataStore.cs`

使用 AES-256-GCM 加密的 Token 存储接口。数据存储在 `%APPDATA%/GitGet/token.enc`。

#### SaveTokenAsync

```csharp
Task SaveTokenAsync(string key, string token, CancellationToken ct = default)
```

加密并保存 Token。

| 参数 | 类型 | 说明 |
|------|------|------|
| `key` | string | 服务标识（如 `"github"`） |
| `token` | string | 明文 Token |

---

#### GetTokenAsync

```csharp
Task<string?> GetTokenAsync(string key, CancellationToken ct = default)
```

读取并解密 Token。

**返回值**：`string?` — 解密后的明文 Token，未存储时返回 null。

---

#### ClearTokenAsync

```csharp
Task ClearTokenAsync(string key, CancellationToken ct = default)
```

删除已存储的 Token。

---

### 4. ICacheService — 缓存服务

**文件**：`GitGet.Core/Interfaces/ICacheService.cs`

基于 `ILocalDataStore` 实现的通用缓存层。使用 Cache-Aside 模式。

#### GetOrSetAsync\<T\>

```csharp
Task<T?> GetOrSetAsync<T>(
    string key,
    Func<Task<T>> factory,
    TimeSpan? expiration = null,
    CancellationToken ct = default
) where T : class
```

从缓存读取；缓存未命中时调用 `factory` 获取数据并存入缓存。

| 参数 | 类型 | 说明 |
|------|------|------|
| `key` | string | 缓存键 |
| `factory` | Func\<Task\<T\>\> | 缓存未命中时的数据工厂函数 |
| `expiration` | TimeSpan? | 缓存有效期（null 表示永久有效） |
| `ct` | CancellationToken | 取消令牌 |

**返回值**：`T?` — 缓存数据或工厂函数返回的数据。

---

#### InvalidateAsync

```csharp
Task InvalidateAsync(string key, CancellationToken ct = default)
```

使指定键的缓存失效（删除）。

---

#### ClearAllAsync

```csharp
Task ClearAllAsync(CancellationToken ct = default)
```

清空所有缓存条目。

---

#### GetCacheSizeAsync

```csharp
Task<long> GetCacheSizeAsync(CancellationToken ct = default)
```

获取当前缓存的总大小（估计值，字节）。

---

### 5. IDownloadService — 下载管理

**文件**：`GitGet.Core/Interfaces/IDownloadService.cs`

下载管理器接口。支持并发控制（默认 3 个）、断点续传、暂停/恢复/取消。

#### StartDownloadAsync

```csharp
Task<DownloadTask> StartDownloadAsync(
    string url,
    string fileName,
    string repoFullName,
    string destinationDir,
    IProgress<DownloadProgress>? progress = null,
    CancellationToken ct = default
)
```

创建下载任务并在后台启动下载（立即返回任务对象）。

| 参数 | 类型 | 说明 |
|------|------|------|
| `url` | string | 文件下载 URL |
| `fileName` | string | 保存的文件名 |
| `repoFullName` | string | 仓库全名（如 "owner/repo"） |
| `destinationDir` | string | 保存目录 |
| `progress` | IProgress\<DownloadProgress\>? | 进度报告回调（可选） |
| `ct` | CancellationToken | 取消令牌 |

**返回值**：`DownloadTask` — 下载任务对象（含 TaskId 供后续操作引用）。

---

#### PauseDownloadAsync

```csharp
Task PauseDownloadAsync(string taskId, CancellationToken ct = default)
```

暂停下载（记录已下载字节数到 SQLite，下次恢复时使用 Range 请求续传）。

---

#### ResumeDownloadAsync

```csharp
Task ResumeDownloadAsync(string taskId, CancellationToken ct = default)
```

恢复之前暂停的下载。

---

#### CancelDownloadAsync

```csharp
Task CancelDownloadAsync(string taskId, CancellationToken ct = default)
```

取消下载并删除临时文件。

---

#### GetTasksAsync

```csharp
Task<List<DownloadTask>> GetTasksAsync(CancellationToken ct = default)
```

获取所有下载任务列表。

---

#### GetTaskAsync

```csharp
Task<DownloadTask?> GetTaskAsync(string taskId, CancellationToken ct = default)
```

根据 TaskId 获取单个下载任务。

**返回值**：`DownloadTask?` — 任务对象，不存在时返回 null。

---

### 6. IAssetMatcherService — 安装包匹配

**文件**：`GitGet.Core/Interfaces/IAssetMatcherService.cs`

仅 Windows 平台。基于文件名的关键词评分机制，自动匹配最适合的安装包。

> 详细算法说明参见《AssetMatcherService 算法说明.md》

#### FindMatchingAsset

```csharp
ReleaseAsset? FindMatchingAsset(IEnumerable<ReleaseAsset> assets)
```

从 Asset 列表中找出最适合当前系统的安装包。

| 参数 | 类型 | 说明 |
|------|------|------|
| `assets` | IEnumerable\<ReleaseAsset\> | 候选 Asset 列表 |

**返回值**：`ReleaseAsset?` — 最佳匹配项，无匹配时返回 null。

#### GetRecommendedAssets

```csharp
Dictionary<long, int> GetRecommendedAssets(IEnumerable<ReleaseAsset> assets)
```

返回所有 Asset 的推荐分数。

**返回值**：`Dictionary<long, int>` — 键为 Asset ID，值为分值（越高越好）。

---

### 7. INodeScriptRunner — Node.js 脚本运行器

**文件**：`GitGet.Core/Interfaces/INodeScriptRunner.cs`

C# 与 Node.js 之间的桥接接口。允许在测试中用 Mock 替换真实进程调用。

#### RunScriptAsync

```csharp
Task<string> RunScriptAsync(string[] arguments, CancellationToken ct = default)
```

执行 Node.js 脚本并返回标准输出的 JSON 字符串。

| 参数 | 类型 | 说明 |
|------|------|------|
| `arguments` | string[] | 传递给脚本的命令行参数 |
| `ct` | CancellationToken | 取消令牌 |

**返回值**：`string` — 脚本 stdout 输出（JSON 格式）。  
**异常**：`TimeoutException`（30 秒超时） / `InvalidOperationException`（非零退出码）。

---

## 二、数据模型（Models）

### Repository — 仓库模型

**文件**：`GitGet.Core/Models/Repository.cs`

| 属性 | 类型 | 说明 |
|------|------|------|
| `Id` | long | 仓库唯一 ID |
| `FullName` | string | 完整名称（"owner/repo"） |
| `Name` | string | 仓库简称 |
| `Owner` | string | 所有者用户名 |
| `Description` | string? | 项目描述 |
| `Language` | string? | 主编程语言 |
| `Stars` | int | Star 数量 |
| `Forks` | int | Fork 数量 |
| `OpenIssues` | int | 未解决的 Issue 数 |
| `Topics` | List\<string\> | 主题标签列表 |
| `License` | string? | 开源协议 SPDX 标识 |
| `Homepage` | string? | 项目主页 URL |
| `DefaultBranch` | string | 默认分支名（默认 "main"） |
| `CreatedAt` | DateTime | 创建时间（UTC） |
| `UpdatedAt` | DateTime | 最后更新时间（UTC） |

---

### Release — 发布版本模型

**文件**：`GitGet.Core/Models/Release.cs`

| 属性 | 类型 | 说明 |
|------|------|------|
| `Id` | long | Release ID |
| `TagName` | string | 标签名（"v2.0.0"） |
| `Name` | string? | 发布名称 |
| `Body` | string? | Release Notes（Markdown） |
| `Prerelease` | bool | 是否为预发布版本 |
| `CreatedAt` | DateTime | 创建时间 |
| `PublishedAt` | DateTime | 发布时间 |
| `HtmlUrl` | string? | 发布页面 URL |
| `Assets` | List\<ReleaseAsset\> | 安装包列表 |

---

### ReleaseAsset — 安装包模型

**文件**：`GitGet.Core/Models/ReleaseAsset.cs`

| 属性 | 类型 | 说明 |
|------|------|------|
| `Id` | long | Asset ID |
| `Name` | string | 文件名（"app-v2.0.0-win-x64.exe"） |
| `Size` | long | 文件大小（字节） |
| `ContentType` | string? | MIME 类型 |
| `DownloadUrl` | string? | 浏览器直接下载链接（`browser_download_url`） |
| `CreatedAt` | DateTime | 创建时间 |
| `UpdatedAt` | DateTime | 更新时间 |
| `DownloadCount` | int | 下载次数 |

---

### GitHubUser — 用户模型

**文件**：`GitGet.Core/Models/GitHubUser.cs`

| 属性 | 类型 | 说明 |
|------|------|------|
| `Id` | long | 用户 ID |
| `Login` | string | 用户名 |
| `Name` | string? | 显示名称 |
| `Email` | string? | 邮箱 |
| `AvatarUrl` | string? | 头像图片 URL |
| `HtmlUrl` | string? | 个人主页 URL |
| `Bio` | string? | 个人简介 |
| `PublicRepos` | int | 公开仓库数 |
| `Followers` | int | 粉丝数 |
| `Following` | int | 关注数 |
| `CreatedAt` | DateTime | 注册时间 |

---

### DownloadTask — 下载任务模型

**文件**：`GitGet.Core/Models/DownloadTask.cs`

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `TaskId` | string | `Guid.NewGuid()` | 任务唯一标识 |
| `RepoFullName` | string | `""` | 仓库全名 |
| `FileName` | string | `""` | 文件名 |
| `DownloadUrl` | string | `""` | 下载链接 |
| `LocalPath` | string? | null | 本地保存路径 |
| `TotalBytes` | long | 0 | 文件总大小 |
| `ReceivedBytes` | long | 0 | 已下载字节 |
| `Status` | string | `"Queued"` | 状态（见枚举 `TaskStatus`） |
| `CreatedAt` | DateTime | `UtcNow` | 创建时间 |
| `CompletedAt` | DateTime? | null | 完成时间 |
| `ErrorMessage` | string? | null | 错误信息 |
| `RetryCount` | int | 0 | 重试次数 |
| `ProgressPercent` | double | 计算值 | 进度百分比（0.0-100.0） |

**`TaskStatus` 枚举**：
| 状态 | 说明 |
|------|------|
| `Queued` | 排队中 |
| `Downloading` | 下载中 |
| `Paused` | 已暂停 |
| `Completed` | 已完成 |
| `Failed` | 失败 |
| `Cancelled` | 已取消 |

---

### DownloadProgress — 下载进度报告模型

**文件**：`GitGet.Core/Models/DownloadProgress.cs`

一个 `record` 类型，通过 `IProgress<DownloadProgress>` 回调报告。

| 属性 | 类型 | 说明 |
|------|------|------|
| `TaskId` | string | 任务 ID |
| `FileName` | string | 文件名 |
| `TotalBytes` | long | 文件总大小 |
| `ReceivedBytes` | long | 已下载字节 |
| `Status` | TaskStatus | 当前状态 |
| `SpeedBytesPerSecond` | double | 下载速度（字节/秒） |
| `EstimatedTimeRemaining` | TimeSpan | 预估剩余时间 |
| `Percent` | double | 进度百分比（计算属性） |

---

## 三、服务实现（Services）

### GitHubApiClient

**文件**：`GitGet.Core/Services/GitHubApiClient.cs`  
**实现**：`IGitHubApiClient`  
**构造函数**：`GitHubApiClient(INodeScriptRunner scriptRunner)`

通过 Node.js 中转调用 GitHub REST API。支持分页、排序、速率限制追踪。

关键特性：
- 通过 `INodeScriptRunner` 调用 `scripts/github-api.js` 执行 HTTP 请求
- 自动解析 `__rateLimit` 元数据（来自响应头的 `X-RateLimit-*`）
- 所有方法在异常时返回空列表/null（而非抛出异常）

---

### LocalDataStore

**文件**：`GitGet.Core/Data/LocalDataStore.cs`  
**实现**：`ILocalDataStore`  
**构造函数**：`LocalDataStore(SqliteConnection connection)`

基于 Microsoft.Data.Sqlite 的本地持久化存储。

关键特性：
- 数据库表：`cache_repos`, `cache_releases`, `download_tasks`
- 每表结构：`key TEXT PRIMARY KEY, data TEXT`
- 支持 `:memory:` 模式用于单元测试
- 使用 `System.Text.Json` 序列化/反序列化

---

### SecureDataStore

**文件**：`GitGet.Core/Data/SecureDataStore.cs`  
**实现**：`ISecureDataStore`  
**构造函数**：`SecureDataStore()`

AES-256-GCM 加密的 Token 存储。Windows 下使用 DPAPI 辅助保护密钥。

关键特性：
- 加密文件：`%APPDATA%/GitGet/tokens.json`
- 加密算法：`AesGcm`（认证加密，防篡改）
- 支持多服务 Token 独立存储（通过 key 区分）

---

### CacheService

**文件**：`GitGet.Core/Services/CacheService.cs`  
**实现**：`ICacheService`  
**构造函数**：`CacheService(ILocalDataStore dataStore)`

通用键值缓存层。使用 Cache-Aside 模式。

---

### DownloadService

**文件**：`GitGet.Core/Services/DownloadService.cs`  
**实现**：`IDownloadService`  
**构造函数**：`DownloadService(HttpClient httpClient, ILocalDataStore dataStore)`

下载管理器。

关键特性：
- 并发下载限制：`SemaphoreSlim(3)`（默认 3 个）
- 流式下载：`HttpCompletionOption.ResponseHeadersRead`
- 断点续传：暂停后恢复使用 `Range: bytes={downloaded}-` 请求头
- 文件冲突处理：自动生成带时间戳的唯一文件名
- 进度报告：每 256KB 或每 500ms 报告一次
- 状态持久化：所有任务存入 SQLite

---

### AssetMatcherService

**文件**：`GitGet.Core/Services/AssetMatcherService.cs`  
**实现**：`IAssetMatcherService`  
**构造函数**：`AssetMatcherService()`

仅 Windows 平台的安装包匹配服务。使用关键词评分机制。

> 详细算法参见《AssetMatcherService 算法说明.md》

评分规则：
- Windows 关键词（win/windows）：+80
- 架构匹配（x64/x86/arm64）：+100
- 扩展名匹配（.exe/.msi）：+60~+75
- 非 Windows 惩罚：-40
- 源码包惩罚：-30
- 非可执行文件惩罚：-60

回退机制：当所有 Asset 均无 OS 关键词时，取有效压缩包（排除源码/校验文件）。

---

### NodeScriptRunner

**文件**：`GitGet.Core/Services/NodeScriptRunner.cs`  
**实现**：`INodeScriptRunner`  
**构造函数**：`NodeScriptRunner(string scriptPath, string? nodePath = null)`

启动 Node.js 进程并捕获标准输出。

关键特性：
- 内部使用 `Process.Start("node", "...")` 启动脚本
- 30 秒超时自动终止
- 非零退出码抛出 `InvalidOperationException`
- 参数转义处理（双引号/空格安全）
- 额外方法：`RunAsync<T>(arguments, ct)` — 直接反序列化 JSON 为指定类型

---

## 四、模块依赖关系图

```
GitGet.Desktop/Program.cs
    ├── Services/GitHubApiClient     → INodeScriptRunner
    ├── Services/AssetMatcherService
    └── Services/DownloadService     → ILocalDataStore

GitGet.Core.Services
    ├── GitHubApiClient              → INodeScriptRunner
    │   └── NodeScriptRunner         → Process("node", "scripts/github-api.js")
    ├── DownloadService              → ILocalDataStore + HttpClient
    ├── CacheService                 → ILocalDataStore
    ├── AssetMatcherService          (无依赖)
    └── Data/
        ├── LocalDataStore           → SqliteConnection
        └── SecureDataStore          → System.Security (AesGcm)
```

---

## 五、跨模块协作示例

### 搜索并下载流程

```
用户输入关键词 "recite-word"
    ↓
GitHubApiClient.SearchRepositoriesAsync("recite-word")
    ↓ 通过 Node.js 中转 → GitHub REST API
返回 List<Repository>
    ↓
用户点击某个仓库
    ↓
GitHubApiClient.GetReleasesAsync(owner, repo)
    ↓
返回 List<Release>（含 Assets 数组）
    ↓
AssetMatcherService.FindMatchingAsset(assets)
    ↓ 评分机制
返回最佳 ReleaseAsset
    ↓
DownloadService.StartDownloadAsync(asset.DownloadUrl, ...)
    ↓ 流式下载 + 进度回调
IProgress<DownloadProgress>.Report(progress)
    ↓
UI 更新进度条
DownloadTask.Status = Completed
```

---

---

## 六、Node.js 中转脚本

### github-api.js — GitHub REST API 桥接脚本

**文件**：`scripts/github-api.js`  
**运行时**：Node.js（无需额外依赖，仅使用内置 `https`/`http` 模块）

#### 命令行用法

```bash
node scripts/github-api.js <method> <endpoint> [params_json] [token]
```

| 参数 | 位置 | 类型 | 说明 | 默认值 |
|------|------|------|------|--------|
| `method` | 1 | string | HTTP 方法（GET/POST 等） | `"GET"` |
| `endpoint` | 2 | string | API 路径（如 `/search/repositories`） | **必填** |
| `params_json` | 3 | string | JSON 格式的查询参数 | `"{}"` |
| `token` | 4 | string | GitHub Personal Access Token（可选） | `""` |

**输出**：
- **stdout**：单行 JSON（API 响应体 + 元数据）
- **stderr**：错误信息的 JSON（仅失败时）
- **退出码**：成功 0，失败 1

#### 使用示例

```bash
# 搜索仓库（匿名，无需 Token）
node scripts/github-api.js GET "/search/repositories" '{"q":"dotnet","sort":"stars","per_page":5}'

# 获取 Release 列表
node scripts/github-api.js GET "/repos/illusogl/recite-word-helper/releases" '{"per_page":10}'

# 带 Token 的认证请求
node scripts/github-api.js GET "/user" "{}" "ghp_xxxxxxxxxxxx"
```

#### 功能说明

| 功能 | 实现细节 |
|------|---------|
| **参数构造** | 将 `params_json` 对象的键值对转义后拼接到 URL 查询字符串 |
| **请求头** | 自动设置 `Accept: application/vnd.github+json` 和 `User-Agent: GitGet/1.0` |
| **Token 认证** | 如果提供第 4 个参数，添加 `Authorization: Bearer {token}` 请求头 |
| **速率限制** | 从响应头 `X-RateLimit-Limit` / `X-RateLimit-Remaining` / `X-RateLimit-Reset` 解析，注入到 JSON 结果的 `__rateLimit` 字段 |
| **分页支持** | 从响应头 `Link` 解析分页链接，注入到 JSON 结果的 `__pagination` 字段 |
| **超时控制** | 单次请求超时 30 秒，超时后强制关闭连接并返回错误 |
| **错误处理** | 异常被捕获并通过 stderr 输出 JSON 格式的错误信息，进程以退出码 1 退出 |
| **非 JSON 响应** | 如果响应体无法解析为 JSON，原样放入 `__raw` 字段并附加 `__status` 和 `__rateLimit` |
| **零依赖** | 仅使用 Node.js 内置的 `https` 模块，不依赖 npm 包 |

#### C# 调用方式

在 `GitGet.Core` 中通过 `NodeScriptRunner` 调用此脚本：

```csharp
// C# 侧调用示例
var runner = new NodeScriptRunner("scripts/github-api.js");
string json = await runner.RunScriptAsync(new[]
{
    "GET",
    "/search/repositories",
    JsonSerializer.Serialize(new { q = "dotnet", sort = "stars", per_page = 20 }),
    "" // 无 Token
});

// 解析结果
var result = JsonSerializer.Deserialize<SearchResult>(json);
```

#### 响应 JSON 结构

成功响应的 JSON 根对象中会附加两个元数据字段：

```json
{
    "total_count": 42,
    "items": [...],
    "__rateLimit": {
        "limit": 60,
        "remaining": 57,
        "reset": 1718668800
    },
    "__pagination": "<https://api.github.com/search/repositories?page=2>; rel=\"next\""
}
```

| 元数据字段 | 类型 | 说明 |
|-----------|------|------|
| `__rateLimit.limit` | int | 每小时总限制次数 |
| `__rateLimit.remaining` | int | 当前剩余次数 |
| `__rateLimit.reset` | int | 重置时间的 Unix 时间戳 |
| `__pagination` | string? | Link 响应头原始值（含 next/last 链接） |

C# 的 `GitHubApiClient.UpdateRateLimit()` 负责在调用后提取这些字段。

---

*编制日期：2026年6月17日*
*关联项目：GitGet — GitHub Release 应用商店*
