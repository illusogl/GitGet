# GitGet 项目文件说明

> 本文档详细说明 GitGet 项目中每一个代码文件的作用，包括技术解释和通俗解释，以及对应的测试文件说明。

---

## 一、项目结构总览

```
d:\GitGet
├── GitGet.sln                          # 解决方案文件（Visual Studio 项目入口）
├── GitGet.Core/                        # 核心类库（所有业务逻辑和数据层）
│   ├── Models/                         # 数据模型
│   │   ├── Repository.cs               # 仓库模型
│   │   ├── Release.cs                  # 发布版本模型
│   │   ├── ReleaseAsset.cs             # 发布资源文件模型
│   │   └── GitHubUser.cs              # GitHub 用户模型
│   ├── Interfaces/                     # 接口定义（契约）
│   │   ├── IGitHubApiClient.cs         # GitHub API 接口
│   │   ├── ILocalDataStore.cs          # 本地数据存储接口
│   │   ├── ISecureDataStore.cs         # 安全加密存储接口
│   │   └── ICacheService.cs            # 缓存服务接口
│   ├── Services/                       # 服务实现
│   │   ├── GitHubApiClient.cs          # GitHub API 调用实现
│   │   └── CacheService.cs             # 缓存服务实现
│   └── Data/                           # 数据层实现
│       ├── LocalDataStore.cs           # 本地 SQLite 数据库实现
│       └── SecureDataStore.cs          # 加密令牌存储实现
├── GitGet.Desktop/                     # 桌面应用程序（Avalonia UI，当前为占位）
│   └── Program.cs                      # 程序入口
└── GitGet.Core.Tests/                  # 单元测试项目
    ├── Models/
    │   └── RepositoryTests.cs          # 仓库模型测试
    └── Services/
        ├── GitHubApiClientTests.cs     # GitHub API 客户端测试
        ├── LocalDataStoreTests.cs      # 本地数据存储测试
        ├── SecureDataStoreTests.cs     # 安全存储测试
        └── CacheServiceTests.cs        # 缓存服务测试
```

---

## 二、解决方案文件

### GitGet.sln

- **技术解释**：Visual Studio 解决方案文件，定义了整个项目中包含哪几个子项目（Core、Desktop、Tests），以及它们之间的依赖关系。
- **通俗解释**：就像一个项目的"总目录"，告诉 Visual Studio 这个软件由哪几个部分组成，每个部分是什么类型（类库、应用程序、测试项目）。
- **依赖关系**：
  - `GitGet.Desktop` → 引用 `GitGet.Core`
  - `GitGet.Core.Tests` → 引用 `GitGet.Core`

---

## 三、数据模型（Models）

### Repository.cs

- **文件路径**：`GitGet.Core/Models/Repository.cs`
- **技术解释**：定义了一个 GitHub 仓库的数据结构，包含仓库的所有关键属性，如仓库名、所有者、描述、编程语言、Star 数等。使用 C# 的 `class` 实现，所有属性都有 `get` 和 `set` 访问器。
- **通俗解释**：这个文件定义了"一个 GitHub 仓库长什么样"。就像一张"仓库信息表"的模板，每个仓库都有：名字（name）、所属用户（owner）、描述（description）、用什么语言写的（language）、有多少人点赞（stars）等。
- **关键属性**：
  - `Id`（long）：GitHub 上的唯一编号
  - `FullName`（string）：完整名称，格式如 `"dotnet/runtime"`
  - `Name`（string）：仓库简称，如 `"runtime"`
  - `Owner`（string）：所有者用户名，如 `"dotnet"`
  - `Description`（string?）：项目描述
  - `Language`（string?）：主要编程语言
  - `Stars`（int）：Star 数量
  - `Forks`（int）：Fork 数量
  - `OpenIssues`（int）：未解决的问题数
  - `Topics`（List\<string\>）：标签列表
  - `License`（string?）：开源协议
  - `Homepage`（string?）：项目主页网址
  - `DefaultBranch`（string）：默认分支名称
  - `CreatedAt` / `UpdatedAt`（DateTime）：创建/更新时间

### Release.cs

- **文件路径**：`GitGet.Core/Models/Release.cs`
- **技术解释**：定义 GitHub Release（发布版本）的数据结构，包含版本标签、发布名称、发布说明、是否为预发布版本、发布时间以及关联的资源文件列表。
- **通俗解释**：这个文件定义了"一个 GitHub 发布的版本长什么样"。类似于软件更新页面上的"版本 2.0.0"，包含：版本标签（如 v2.0.0）、发布说明（更新了啥）、是不是测试版（pre-release）、什么时候发布的、以及这个版本有哪些安装包文件（assets）。
- **关键属性**：
  - `Id`（long）：发布编号
  - `TagName`（string）：标签名，如 `"v2.0.0"`
  - `Name`（string?）：发布名称
  - `Body`（string?）：发布说明（Markdown 格式）
  - `Prerelease`（bool）：是否为预发布版本
  - `CreatedAt` / `PublishedAt`（DateTime）：创建/发布时间
  - `HtmlUrl`（string?）：发布页面的网址
  - `Assets`（List\<ReleaseAsset\>）：该版本包含的资源文件列表

### ReleaseAsset.cs

- **文件路径**：`GitGet.Core/Models/ReleaseAsset.cs`
- **技术解释**：定义 GitHub Release 中单个资源（安装包）的数据结构，包含文件名、文件大小、下载链接、下载次数等。
- **通俗解释**：这个文件定义了"一个安装包长什么样"。比如你下载软件时看到的 `GitGet-Setup-2.0.0.exe` 就是一个 Asset，它有：文件名（name）、大小（size，单位字节）、下载链接（browser_download_url）、被下载了多少次（download_count）。
- **关键属性**：
  - `Id`（long）：资源编号
  - `Name`（string）：文件名，如 `"GitGet-2.0.0-win-x64.exe"`
  - `Size`（long）：文件大小（字节）
  - `ContentType`（string?）：MIME 类型，如 `"application/octet-stream"`
  - `BrowserDownloadUrl`（string?）：浏览器直接下载的链接
  - `CreatedAt` / `UpdatedAt`（DateTime）：创建/更新时间
  - `DownloadCount`（int）：下载次数

### GitHubUser.cs

- **文件路径**：`GitGet.Core/Models/GitHubUser.cs`
- **技术解释**：定义 GitHub 用户的数据结构，包含用户名、昵称、邮箱、头像、粉丝数等。
- **通俗解释**：这个文件定义了"一个 GitHub 用户长什么样"。包括：用户名（login，如 "glll0"）、昵称（name）、邮箱、头像、个人简介、有多少公开仓库、有多少粉丝等。
- **关键属性**：
  - `Id`（long）：用户编号
  - `Login`（string）：用户名
  - `Name`（string?）：显示名称
  - `Email`（string?）：邮箱
  - `AvatarUrl`（string?）：头像图片链接
  - `HtmlUrl`（string?）：个人主页链接
  - `Bio`（string?）：个人简介
  - `PublicRepos`（int）：公开仓库数
  - `Followers` / `Following`（int）：粉丝/关注数
  - `CreatedAt`（DateTime）：注册时间

---

## 四、接口定义（Interfaces）

### IGitHubApiClient.cs

- **文件路径**：`GitGet.Core/Interfaces/IGitHubApiClient.cs`
- **技术解释**：定义了所有 GitHub REST API 调用的接口契约，包括搜索仓库、获取仓库详情、获取发布版本列表、获取用户 Star 的仓库、获取当前登录用户信息等。
- **通俗解释**：这个接口就像一份"外卖菜单"，告诉使用者"我们能做这些事"：搜索 GitHub 上的仓库、查看某个仓库的详细信息、查看某个仓库有哪些发布版本、查看某个用户 Star 过的仓库、查看当前登录用户的信息。具体怎么做（怎么调用 GitHub API）在实现类中完成。
- **方法列表**：
  - `SearchRepositoriesAsync`：搜索仓库（支持关键词、语言、分页、排序）
  - `GetRepositoryAsync`：获取单个仓库详情
  - `GetReleasesAsync`：获取某个仓库的所有发布版本
  - `GetStarredReposAsync`：获取用户 Star 的仓库列表
  - `GetUserAsync`：获取当前认证用户的信息
  - `RemainingRateLimit`（属性）：查询当前剩余的 API 调用次数

### ILocalDataStore.cs

- **文件路径**：`GitGet.Core/Interfaces/ILocalDataStore.cs`
- **技术解释**：定义了本地数据存储的接口，提供通用的键值存储能力，支持泛型序列化、条件查询、删除和批量操作。
- **通俗解释**：这个接口定义了"本地数据库能做什么"：存数据（SaveAsync）、取数据（GetAsync）、删数据（DeleteAsync）、初始化建表（InitializeAsync）、按条件筛选（QueryAsync）。所有数据都存在电脑本地的 SQLite 数据库中。
- **方法列表**：
  - `InitializeAsync`：初始化数据库，创建必要的表
  - `SaveAsync<T>`：保存任意类型的数据
  - `GetAsync<T>`：根据 key 获取数据
  - `DeleteAsync`：删除指定 key 的数据
  - `QueryAsync<T>`：按条件查询数据（支持 SQL WHERE 子句）

### ISecureDataStore.cs

- **文件路径**：`GitGet.Core/Interfaces/ISecureDataStore.cs`
- **技术解释**：定义了安全存储的接口，专门用于存储用户的敏感信息（如 GitHub Token），确保数据加密后再写入磁盘。
- **通俗解释**：这个接口定义了"怎么安全地存密码"：存 Token（SaveTokenAsync）、取 Token（GetTokenAsync）、删 Token（ClearTokenAsync）。Token 存的时候会被加密，取的时候会自动解密。
- **方法列表**：
  - `SaveTokenAsync`：保存加密后的 Token
  - `GetTokenAsync`：获取并解密 Token
  - `ClearTokenAsync`：删除已存储的 Token

### ICacheService.cs

- **文件路径**：`GitGet.Core/Interfaces/ICacheService.cs`
- **技术解释**：定义了缓存服务的接口，提供通用的缓存读写能力，减少重复的 API 调用，提升性能。
- **通俗解释**：这个接口定义了"缓存系统能做什么"：取缓存，如果没有就自动请求并存入缓存（GetOrSetAsync）、手动删除某条缓存（InvalidateAsync）、清空所有缓存（ClearAllAsync）、查看缓存大小（GetCacheSizeAsync）。
- **方法列表**：
  - `GetOrSetAsync<T>`：取缓存，如果没有则通过 factory 函数获取并缓存
  - `InvalidateAsync`：使某条缓存失效
  - `ClearAllAsync`：清空所有缓存
  - `GetCacheSizeAsync`：获取缓存总大小

---

## 五、服务实现（Services）

### GitHubApiClient.cs

- **文件路径**：`GitGet.Core/Services/GitHubApiClient.cs`
- **技术解释**：实现 `IGitHubApiClient` 接口，使用 `HttpClient` 调用 GitHub REST API v3。支持认证（Bearer Token）、分页、排序、错误处理、速率限制追踪。
- **通俗解释**：这个文件是"GitHub 的连接器"，负责真正去 GitHub 服务器上拿数据。比如用户搜索"dotnet"，它就调用 GitHub 的搜索 API，把结果拿回来并转换成 C# 的对象。它还能追踪"今天还剩多少次 API 调用机会"。
- **实现细节**：
  - 使用 GitHub REST API v3（`api.github.com`）
  - 支持 Bearer Token 认证（通过 `Authorization` 请求头）
  - 自动解析 `Link` 响应头中的分页信息
  - 追踪 `X-RateLimit-Remaining` 响应头记录剩余调用次数
  - 返回数据时自动反序列化 JSON 为 C# 模型
  - 对搜索 API 支持 `q`（关键词）、`language`（语言）、`per_page`（每页数）、`sort`（排序方式）等参数

### CacheService.cs

- **文件路径**：`GitGet.Core/Services/CacheService.cs`
- **技术解释**：实现 `ICacheService` 接口，基于 `ILocalDataStore` 实现键值缓存。支持缓存的自动读写、手动失效和清空。通过一个特殊的 `__cache_keys__` 键来追踪所有缓存条目。
- **通俗解释**：这个文件是"缓存管理员"。比如你第一次搜"dotnet"，它去 GitHub 拿数据并记在小本子上。第二次再搜同样的内容，它直接从本子上读取，不用再联网，速度更快。它还能记录缓存了多少条数据、清空所有缓存等。
- **实现细节**：
  - 使用通用的 `GetOrSetAsync` 模式：先查缓存 → 命中则返回 → 未命中则调用 factory 函数获取 → 存入缓存后返回
  - 使用 `__cache_keys__` 特殊键记录所有缓存条目，方便批量操作
  - 支持可选的时间过期参数（`expiration` 参数预留）

---

## 六、数据层实现（Data）

### LocalDataStore.cs

- **文件路径**：`GitGet.Core/Data/LocalDataStore.cs`
- **技术解释**：实现 `ILocalDataStore` 接口，使用 `Microsoft.Data.Sqlite` 作为底层数据库引擎。数据以 JSON 格式存储在 SQLite 的 `data` 列中，支持任意类型的序列化。
- **通俗解释**：这个文件是"本地数据库管理员"，负责在电脑本地创建一个 SQLite 数据库文件，把数据存进去。比如缓存搜索结果、下载记录等。它用 JSON 格式来存数据，这样任何类型的数据（仓库信息、版本信息等）都能存进去。
- **实现细节**：
  - 在构造函数中接收 `SqliteConnection` 并保持连接
  - 创建三个表：`cache_repos`（仓库缓存）、`cache_releases`（版本缓存）、`download_tasks`（下载任务）
  - 每个表有 `key`（主键）和 `data`（JSON 文本）两列
  - 使用 `System.Text.Json` 序列化/反序列化对象
  - `QueryAsync` 支持自定义 WHERE 子句和参数，利用 SQLite 的 `json_extract` 函数进行 JSON 字段查询
  - 支持 `:memory:` 模式（内存数据库，用于测试）

### SecureDataStore.cs

- **文件路径**：`GitGet.Core/Data/SecureDataStore.cs`
- **技术解释**：实现 `ISecureDataStore` 接口，使用 `AesGcm`（AES-GCM 认证加密算法）对用户的 GitHub Token 进行加密，然后写入本地 `tokens.json` 文件。
- **通俗解释**：这个文件是"密码保险箱"。用户登录 GitHub 时输入的 Token（相当于密码），不能明文保存在电脑上，否则被别人看到就危险了。这个文件会使用 AES 加密算法把 Token 加密成一堆乱码再存起来，取出来的时候再解密。AES-GCM 是目前最安全的加密方式之一。
- **实现细节**：
  - 使用 .NET 的 `AesGcm` 类实现认证加密
  - 数据存储在用户目录下的 `GitGet\tokens.json` 文件中
  - 支持多 Token 存储（通过 key 区分不同服务）
  - 加密流程：生成随机 nonce → 使用密钥加密 → 返回 nonce + 密文（Base64 编码）
  - 解密流程：解码 Base64 → 提取 nonce → 使用密钥解密
  - Windows 上使用 `ProtectedData`（DPAPI）保护加密密钥本身

---

## 七、桌面应用（Desktop）

### Program.cs

- **文件路径**：`GitGet.Desktop/Program.cs`
- **技术解释**：当前为占位代码，提供了一个 `Main` 方法入口点，使得 `GitGet.Desktop` 项目可以编译为可执行文件。后续将在第三阶段替换为 Avalonia UI 的 `AppBuilder`。
- **通俗解释**：这个文件是"程序启动入口"。当你双击打开 GitGet 桌面应用时，系统会从这个文件开始执行代码。目前它只是一个"占位符"，只打印一行文字，真正的界面（窗口、按钮、列表等）将在后续开发中添加。

---

## 八、测试项目（Tests）

### RepositoryTests.cs

- **文件路径**：`GitGet.Core.Tests/Models/RepositoryTests.cs`
- **测试项**：2 个测试
  - `Repository_DefaultValues_AreSet`：验证新建的 `Repository` 对象，其数值类型的属性默认值为 0，引用类型的属性默认值为 `null`
  - `Repository_CanSetAndGetProperties`：验证设置属性值后，读取出来的值与设置的值一致
- **通俗说明**：这两个测试确保"仓库模型"这个类的定义是正确的。好比检查"一张空白的仓库信息表"，确保所有字段的默认值是对的，填了数据之后也能正确读取。

### GitHubApiClientTests.cs

- **文件路径**：`GitGet.Core.Tests/Services/GitHubApiClientTests.cs`
- **测试项**：10 个测试
  - `SearchRepositoriesAsync_ReturnsResults`：验证搜索 API 返回正确数量的结果
  - `SearchRepositoriesAsync_WithLanguage_FiltersCorrectly`：验证按语言过滤功能
  - `SearchRepositoriesAsync_EmptyResponse_ReturnsEmptyList`：验证搜索无结果时返回空列表
  - `GetRepositoryAsync_ReturnsRepository`：验证获取单个仓库详情
  - `GetRepositoryAsync_NotFound_ReturnsNull`：验证仓库不存在时返回 null
  - `GetReleasesAsync_ReturnsReleases`：验证获取发布版本列表
  - `GetReleasesAsync_ReturnsEmpty_WhenNoReleases`：验证无发布版本时返回空列表
  - `GetStarredReposAsync_ReturnsStarredRepos`：验证获取用户 Star 的仓库
  - `GetUserAsync_ReturnsUser`：验证获取当前用户信息
  - `GetUserAsync_Unauthenticated_ReturnsNull`：验证未认证时返回 null
  - `RemainingRateLimit_ReturnsDefault_WhenNoRequestMade`：验证默认速率限制值
  - `RateLimit_UpdatedAfterRequest`：验证请求后速率限制被更新
- **测试方式**：使用 Moq 框架模拟 `HttpMessageHandler`，不真正连接 GitHub 服务器
- **通俗说明**：验证"GitHub 连接器"是否正常工作。通过模拟 GitHub 服务器的响应，测试各种情况：正常返回、空结果、找不到、未授权等，确保程序在各种网络情况下表现正确。

### LocalDataStoreTests.cs

- **文件路径**：`GitGet.Core.Tests/Services/LocalDataStoreTests.cs`
- **测试项**：5 个测试
  - `InitializeAsync_CreatesTables`：验证初始化时创建了正确的数据库表
  - `SaveAndGetAsync_Roundtrip_Succeeds`：验证保存后能正确读取
  - `GetAsync_NonExistentKey_ReturnsNull`：验证不存在的 key 返回 null
  - `DeleteAsync_RemovesEntry`：验证删除后数据不复存在
  - `QueryAsync_WithWhereClause_FiltersCorrectly`：验证带条件的查询能正确过滤
- **测试方式**：使用 SQLite `:memory:` 模式，每个测试独立运行，测试完后自动销毁
- **通俗说明**：验证"本地数据库管理员"是否靠谱。检查数据库能不能正确创建表、存数据、取数据、删数据、按条件筛选。每个测试都独立使用一个内存数据库，互不干扰。

### SecureDataStoreTests.cs

- **文件路径**：`GitGet.Core.Tests/Services/SecureDataStoreTests.cs`
- **测试项**：6 个测试
  - `SaveAndGetTokenAsync_Roundtrip_Succeeds`：验证 Token 保存后能正确读取
  - `GetTokenAsync_NoToken_ReturnsNull`：验证未存储 Token 时返回 null
  - `SaveTokenAsync_OverwritesExisting`：验证覆盖存储
  - `ClearTokenAsync_RemovesToken`：验证删除 Token
  - `MultipleTokens_CanBeStoredIndependently`：验证多个 Token 独立存储
  - `TokenIsEncrypted_NotPlainText`：验证存储的不是明文（确实是加密的）
- **测试方式**：使用临时目录存储加密文件，测试完后清理
- **通俗说明**：验证"密码保险箱"是否安全。检查加密存取的流程是否完整：能不能存进去、取出来、覆盖、删除，以及最重要的是——存进去的确实是加密后的乱码，不是明文，确保安全性。

### CacheServiceTests.cs

- **文件路径**：`GitGet.Core.Tests/Services/CacheServiceTests.cs`
- **测试项**：4 个测试
  - `GetOrSetAsync_FactoryCalled_WhenCacheMiss`：验证缓存未命中时调用了 factory 函数
  - `GetOrSetAsync_ReturnsCached_WhenCacheHit`：验证缓存命中时不再调用 factory 函数
  - `InvalidateAsync_RemovesFromCache`：验证失效操作后重新调用 factory 函数
  - `GetCacheSizeAsync_ReturnsZero_WhenEmpty`：验证空缓存时大小为 0
- **测试方式**：使用 SQLite `:memory:` 数据库，真实的 `LocalDataStore` 实例
- **通俗说明**：验证"缓存管理员"是否聪明。检查缓存逻辑：第一次请求应该去拿数据（调 factory），第二次请求相同的内容应该直接从缓存读取（不调 factory），手动删除缓存后应该重新去拿数据，空缓存时返回大小为 0。

---

## 九、测试数据统计

| 测试类 | 测试数量 | 测试内容 |
|--------|---------|---------|
| RepositoryTests | 2 | 模型默认值、属性读写 |
| GitHubApiClientTests | 10 | 搜索、详情、版本、Star、用户、速率限制 |
| LocalDataStoreTests | 5 | 初始化、增删改查、条件查询 |
| SecureDataStoreTests | 6 | 加密存取、覆盖、删除、多服务、密文验证 |
| CacheServiceTests | 4 | 缓存命中/未命中、失效、大小 |
| **合计** | **29** | **全部通过 ✅** |

---

## 十、关键技术决策说明

| 决策 | 选择 | 原因 |
|------|------|------|
| 数据库引擎 | SQLite | 无需安装数据库服务器，文件级存储，适合桌面应用 |
| 缓存策略 | 读写时缓存（Cache-Aside） | 实现简单，适合读多写少的场景 |
| 加密算法 | AES-GCM | 提供认证加密，防止篡改，.NET 原生支持 |
| JSON 存储 | System.Text.Json | 高性能，零依赖，.NET 原生支持 |
| 测试框架 | xUnit + Moq + FluentAssertions | 行业标准组合，功能全面，社区活跃 |
| 桌面 UI | Avalonia（规划中） | 跨平台，支持 Windows/Linux/macOS |