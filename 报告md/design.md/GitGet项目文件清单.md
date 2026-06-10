# GitGet 项目文件清单

> 本文档列出 GitGet——GitHub Release 应用商店系统 开发过程中需要编写的所有源文件、配置文件和资源文件。
>
> 每项条目均标注了：使用语言、功能描述和开发状态。
>
> **全栈语言：C#**（前端 UI + 后端逻辑 + 测试统一使用 C#）

---

## 约定

| 图标 | 含义 |
|------|------|
| 📦 | 依赖/库文件（无需手动编写） |
| ⚙️ | 配置文件 |
| 🚀 | 入口文件 |
| 🧩 | 模块文件 |
| 🧪 | 测试文件 |
| 📄 | 文档文件 |
| 🎨 | 样式/资源文件 |
| 🔧 | 工具脚本 |

---

## 1. 解决方案根目录

| # | 文件路径 | 语言 | 说明 | 状态 |
|---|----------|------|------|------|
| 1 | `GitGet.sln` | — | ⚙️ .NET 解决方案文件，包含所有项目引用 | ⚪ 待创建 |
| 2 | `Directory.Build.props` | XML | ⚙️ 全局 MSBuild 属性（目标框架、Nullable、ImplicitUsings） | ⚪ 待创建 |
| 3 | `Directory.Packages.props` | XML | ⚙️ 集中式 NuGet 包版本管理（Central Package Management） | ⚪ 待创建 |
| 4 | `.gitignore` | — | ⚙️ Git 忽略规则（bin/obj/Dist/等） | ⚪ 待创建 |
| 5 | `.editorconfig` | — | ⚙️ 代码风格统一配置 | ⚪ 待创建 |
| 6 | `README.md` | Markdown | 📄 项目自述文件（安装指南/使用说明） | ⚪ 待创建 |
| 7 | `LICENSE` | — | 📄 开源许可证文件 | ⚪ 待创建 |

---

## 2. 桌面启动项目（GitGet.Desktop）

**语言：C# + XAML（Avalonia UI）**
（桌面应用入口，包含 View 和 ViewModel）

### 2.1 项目配置文件

| # | 文件路径 | 语言 | 说明 | 状态 |
|---|----------|------|------|------|
| 8 | `src/GitGet.Desktop/GitGet.Desktop.csproj` | XML | ⚙️ 项目文件（TargetFramework: net9.0, 使用 Avalonia） | ⚪ 待创建 |
| 9 | `src/GitGet.Desktop/Program.cs` | C# | 🚀 应用入口（Avalonia AppBuilder 启动） | ⚪ 待创建 |
| 10 | `src/GitGet.Desktop/App.axaml` | XAML | 🚀 应用级资源（主题/样式/字体） | ⚪ 待创建 |
| 11 | `src/GitGet.Desktop/App.axaml.cs` | C# | 🚀 应用代码后置（DI 容器注册/启动配置） | ⚪ 待创建 |

### 2.2 ViewModel 层（src/GitGet.Desktop/ViewModels/）

| # | 文件路径 | 语言 | 说明 | 状态 |
|---|----------|------|------|------|
| 12 | `src/GitGet.Desktop/ViewModels/MainWindowViewModel.cs` | C# | 🚀 主窗口 ViewModel（导航/全局状态/侧边栏） | ⚪ 待创建 |
| 13 | `src/GitGet.Desktop/ViewModels/HomeViewModel.cs` | C# | 🧩 🏠 首页 ViewModel（推荐项目/分类切换） | ⚪ 待创建 |
| 14 | `src/GitGet.Desktop/ViewModels/SearchViewModel.cs` | C# | 🧩 🔍 搜索页 ViewModel（搜索/筛选/分页） | ⚪ 待创建 |
| 15 | `src/GitGet.Desktop/ViewModels/DetailViewModel.cs` | C# | 🧩 📋 项目详情 ViewModel（仓库/Release/Asset/下载） | ⚪ 待创建 |
| 16 | `src/GitGet.Desktop/ViewModels/SettingsViewModel.cs` | C# | 🧩 ⚙️ 设置页 ViewModel（下载路径/主题/语言） | ⚪ 待创建 |
| 17 | `src/GitGet.Desktop/ViewModels/UserProfileViewModel.cs` | C# | 🧩 👤 个人中心 ViewModel（用户信息/Star 列表/下载历史） | ⚪ 待创建 |
| 18 | `src/GitGet.Desktop/ViewModels/DownloadPanelViewModel.cs` | C# | 🧩 📥 下载管理 ViewModel（下载列表/进度/操作） | ⚪ 待创建 |

### 2.3 View 层（src/GitGet.Desktop/Views/）

| # | 文件路径 | 语言 | 说明 | 状态 |
|---|----------|------|------|------|
| 19 | `src/GitGet.Desktop/Views/MainWindow.xaml` | XAML | 🚀 主窗口布局（侧边栏+内容区+底部下载栏） | ⚪ 待创建 |
| 20 | `src/GitGet.Desktop/Views/MainWindow.xaml.cs` | C# | 🚀 主窗口代码后置（窗口事件/托盘处理） | ⚪ 待创建 |
| 21 | `src/GitGet.Desktop/Views/HomeView.xaml` | XAML | 🧩 首页视图（推荐项目卡片网格） | ⚪ 待创建 |
| 22 | `src/GitGet.Desktop/Views/HomeView.xaml.cs` | C# | 🧩 首页代码后置 | ⚪ 待创建 |
| 23 | `src/GitGet.Desktop/Views/SearchView.xaml` | XAML | 🧩 搜索视图（搜索框+筛选+结果列表） | ⚪ 待创建 |
| 24 | `src/GitGet.Desktop/Views/SearchView.xaml.cs` | C# | 🧩 搜索代码后置 | ⚪ 待创建 |
| 25 | `src/GitGet.Desktop/Views/DetailView.xaml` | XAML | 🧩 项目详情视图（仓库信息+Release 列表+Asset 标签） | ⚪ 待创建 |
| 26 | `src/GitGet.Desktop/Views/DetailView.xaml.cs` | C# | 🧩 详情代码后置 | ⚪ 待创建 |
| 27 | `src/GitGet.Desktop/Views/SettingsView.xaml` | XAML | 🧩 设置视图（配置项表单） | ⚪ 待创建 |
| 28 | `src/GitGet.Desktop/Views/SettingsView.xaml.cs` | C# | 🧩 设置代码后置 | ⚪ 待创建 |
| 29 | `src/GitGet.Desktop/Views/UserProfileView.xaml` | XAML | 🧩 个人中心视图（用户头像/信息/Star 列表） | ⚪ 待创建 |
| 30 | `src/GitGet.Desktop/Views/UserProfileView.xaml.cs` | C# | 🧩 个人中心代码后置 | ⚪ 待创建 |
| 31 | `src/GitGet.Desktop/Views/DownloadPanelView.xaml` | XAML | 🧩 下载面板视图（底部下载栏/进度条） | ⚪ 待创建 |
| 32 | `src/GitGet.Desktop/Views/DownloadPanelView.xaml.cs` | C# | 🧩 下载面板代码后置 | ⚪ 待创建 |

### 2.4 自定义控件（src/GitGet.Desktop/Controls/）

| # | 文件路径 | 语言 | 说明 | 状态 |
|---|----------|------|------|------|
| 33 | `src/GitGet.Desktop/Controls/ProjectCard.xaml` | XAML | 🧩 项目卡片控件（图标/名称/Star/语言标签） | ⚪ 待创建 |
| 34 | `src/GitGet.Desktop/Controls/ProjectCard.xaml.cs` | C# | 🧩 项目卡片代码后置 | ⚪ 待创建 |
| 35 | `src/GitGet.Desktop/Controls/SearchBar.xaml` | XAML | 🧩 搜索栏控件（输入框+筛选下拉） | ⚪ 待创建 |
| 36 | `src/GitGet.Desktop/Controls/SearchBar.xaml.cs` | C# | 🧩 搜索栏代码后置 | ⚪ 待创建 |
| 37 | `src/GitGet.Desktop/Controls/ReleaseList.xaml` | XAML | 🧩 Release 列表控件（版本/标签/时间） | ⚪ 待创建 |
| 38 | `src/GitGet.Desktop/Controls/ReleaseList.xaml.cs` | C# | 🧩 Release 列表代码后置 | ⚪ 待创建 |
| 39 | `src/GitGet.Desktop/Controls/AssetBadge.xaml` | XAML | 🧩 Asset 标签控件（文件名/大小/推荐标记） | ⚪ 待创建 |
| 40 | `src/GitGet.Desktop/Controls/AssetBadge.xaml.cs` | C# | 🧩 Asset 标签代码后置 | ⚪ 待创建 |
| 41 | `src/GitGet.Desktop/Controls/ProgressBar.xaml` | XAML | 🧩 进度条控件（百分比/速度/剩余时间） | ⚪ 待创建 |
| 42 | `src/GitGet.Desktop/Controls/ProgressBar.xaml.cs` | C# | 🧩 进度条代码后置 | ⚪ 待创建 |
| 43 | `src/GitGet.Desktop/Controls/Sidebar.xaml` | XAML | 🧩 侧边导航栏控件（首页/搜索/下载/个人中心） | ⚪ 待创建 |
| 44 | `src/GitGet.Desktop/Controls/Sidebar.xaml.cs` | C# | 🧩 侧边栏代码后置 | ⚪ 待创建 |
| 45 | `src/GitGet.Desktop/Controls/UserAvatar.xaml` | XAML | 🧩 用户头像控件（头像/登录状态/下拉菜单） | ⚪ 待创建 |
| 46 | `src/GitGet.Desktop/Controls/UserAvatar.xaml.cs` | C# | 🧩 用户头像代码后置 | ⚪ 待创建 |
| 47 | `src/GitGet.Desktop/Controls/DownloadItem.xaml` | XAML | 🧩 下载项控件（单条任务状态/进度/操作按钮） | ⚪ 待创建 |
| 48 | `src/GitGet.Desktop/Controls/DownloadItem.xaml.cs` | C# | 🧩 下载项代码后置 | ⚪ 待创建 |

### 2.5 样式/资源（src/GitGet.Desktop/Styles/）

| # | 文件路径 | 语言 | 说明 | 状态 |
|---|----------|------|------|------|
| 49 | `src/GitGet.Desktop/Styles/GlobalStyles.xaml` | XAML | 🎨 全局样式（Avalonia 主题/颜色/字体/基础布局） | ⚪ 待创建 |
| 50 | `src/GitGet.Desktop/Styles/LightTheme.xaml` | XAML | 🎨 亮色主题配色方案 | ⚪ 待创建 |
| 51 | `src/GitGet.Desktop/Styles/DarkTheme.xaml` | XAML | 🎨 暗色主题配色方案 | ⚪ 待创建 |

---

## 3. 业务逻辑与数据访问层（GitGet.Core）

**语言：C# + .NET 9 类库**
（所有业务逻辑、数据模型、数据访问代码）

### 3.1 项目配置文件

| # | 文件路径 | 语言 | 说明 | 状态 |
|---|----------|------|------|------|
| 52 | `src/GitGet.Core/GitGet.Core.csproj` | XML | ⚙️ 类库项目文件（net9.0） | ⚪ 待创建 |

### 3.2 业务服务（src/GitGet.Core/Services/）

| # | 文件路径 | 语言 | 说明 | 状态 |
|---|----------|------|------|------|
| 53 | `src/GitGet.Core/Services/IGitHubApiClient.cs` | C# | 🧩 GitHub API 客户端接口定义 | ⚪ 待创建 |
| 54 | `src/GitGet.Core/Services/GitHubApiClient.cs` | C# | 🧩 GitHub API 客户端实现（HttpClient 封装/认证/重试/限流） | ⚪ 待创建 |
| 55 | `src/GitGet.Core/Services/IDownloadService.cs` | C# | 🧩 下载管理器接口定义 | ⚪ 待创建 |
| 56 | `src/GitGet.Core/Services/DownloadService.cs` | C# | 🧩 下载管理器实现（任务生命周期/流式下载/断点续传/并发控制） | ⚪ 待创建 |
| 57 | `src/GitGet.Core/Services/IRecommendService.cs` | C# | 🧩 推荐引擎接口定义 | ⚪ 待创建 |
| 58 | `src/GitGet.Core/Services/RecommendService.cs` | C# | 🧩 推荐引擎实现（兴趣模型构建/并行查询/排序） | ⚪ 待创建 |
| 59 | `src/GitGet.Core/Services/IAssetMatcherService.cs` | C# | 🧩 Asset 匹配器接口定义 | ⚪ 待创建 |
| 60 | `src/GitGet.Core/Services/AssetMatcherService.cs` | C# | 🧩 Asset 匹配器实现（正则规则/多 OS 支持/优先级评分） | ⚪ 待创建 |
| 61 | `src/GitGet.Core/Services/ICacheService.cs` | C# | 🧩 缓存管理器接口定义 | ⚪ 待创建 |
| 62 | `src/GitGet.Core/Services/CacheService.cs` | C# | 🧩 缓存管理器实现（SQLite 读写/TTL 管理/清理） | ⚪ 待创建 |
| 63 | `src/GitGet.Core/Services/ITokenService.cs` | C# | 🧩 Token 管理器接口定义 | ⚪ 待创建 |
| 64 | `src/GitGet.Core/Services/TokenService.cs` | C# | 🧩 Token 管理器实现（OAuth 授权码交换/安全存储/刷新） | ⚪ 待创建 |

### 3.3 数据模型（src/GitGet.Core/Models/）

| # | 文件路径 | 语言 | 说明 | 状态 |
|---|----------|------|------|------|
| 65 | `src/GitGet.Core/Models/Repo.cs` | C# | 🧩 仓库数据模型（record struct，对应 GitHub API response） | ⚪ 待创建 |
| 66 | `src/GitGet.Core/Models/Release.cs` | C# | 🧩 Release 数据模型 | ⚪ 待创建 |
| 67 | `src/GitGet.Core/Models/Asset.cs` | C# | 🧩 Asset 数据模型 | ⚪ 待创建 |
| 68 | `src/GitGet.Core/Models/User.cs` | C# | 🧩 GitHub 用户数据模型 | ⚪ 待创建 |
| 69 | `src/GitGet.Core/Models/DownloadTask.cs` | C# | 🧩 下载任务数据模型（含状态枚举） | ⚪ 待创建 |
| 70 | `src/GitGet.Core/Models/SearchResult.cs` | C# | 🧩 搜索结果模型 | ⚪ 待创建 |
| 71 | `src/GitGet.Core/Models/TokenInfo.cs` | C# | 🧩 OAuth Token 信息模型 | ⚪ 待创建 |

### 3.4 数据访问层（src/GitGet.Core/Data/）

| # | 文件路径 | 语言 | 说明 | 状态 |
|---|----------|------|------|------|
| 72 | `src/GitGet.Core/Data/ILocalDataStore.cs` | C# | 🧩 SQLite 数据存储接口（CRUD/事务） | ⚪ 待创建 |
| 73 | `src/GitGet.Core/Data/LocalDataStore.cs` | C# | 🧩 SQLite 数据存储实现（Microsoft.Data.Sqlite） | ⚪ 待创建 |
| 74 | `src/GitGet.Core/Data/ISecureDataStore.cs` | C# | 🧩 安全存储接口 | ⚪ 待创建 |
| 75 | `src/GitGet.Core/Data/SecureDataStore.cs` | C# | 🧩 安全存储实现（AES-256-GCM 加密文件存储） | ⚪ 待创建 |
| 76 | `src/GitGet.Core/Data/AppDbContext.cs` | C# | 🧩 EF Core DbContext（可选，如使用 EF Core 访问 SQLite） | ⚪ 待创建 |

### 3.5 工具函数（src/GitGet.Core/Utilities/）

| # | 文件路径 | 语言 | 说明 | 状态 |
|---|----------|------|------|------|
| 77 | `src/GitGet.Core/Utilities/PlatformHelper.cs` | C# | 🔧 平台检测工具（OS 类型/架构检测/架构字符串格式化） | ⚪ 待创建 |
| 78 | `src/GitGet.Core/Utilities/RetryHelper.cs` | C# | 🔧 指数退避重试工具（Polly 或自实现） | ⚪ 待创建 |
| 79 | `src/GitGet.Core/Utilities/FileSizeFormatter.cs` | C# | 🔧 文件大小格式化（字节 → KB/MB/GB 可读字符串） | ⚪ 待创建 |
| 80 | `src/GitGet.Core/Utilities/SpeedFormatter.cs` | C# | 🔧 下载速度格式化（Bytes/s → KB/s/MB/s） | ⚪ 待创建 |
| 81 | `src/GitGet.Core/Utilities/LoggerConfig.cs` | C# | 🔧 Serilog 日志配置（文件输出/控制台输出/级别控制） | ⚪ 待创建 |

### 3.6 自定义异常（src/GitGet.Core/Exceptions/）

| # | 文件路径 | 语言 | 说明 | 状态 |
|---|----------|------|------|------|
| 82 | `src/GitGet.Core/Exceptions/GitGetException.cs` | C# | 🧩 基础异常基类 | ⚪ 待创建 |
| 83 | `src/GitGet.Core/Exceptions/RateLimitExceededException.cs` | C# | 🧩 API 速率限制异常 | ⚪ 待创建 |
| 84 | `src/GitGet.Core/Exceptions/NetworkException.cs` | C# | 🧩 网络异常 | ⚪ 待创建 |
| 85 | `src/GitGet.Core/Exceptions/TokenException.cs` | C# | 🧩 Token 相关异常（过期/无效） | ⚪ 待创建 |
| 86 | `src/GitGet.Core/Exceptions/DownloadException.cs` | C# | 🧩 下载异常（中断/文件损坏） | ⚪ 待创建 |

---

## 4. 测试项目（GitGet.Core.Tests）

**语言：C# + xUnit**

| # | 文件路径 | 语言 | 说明 | 状态 |
|---|----------|------|------|------|
| 87 | `src/GitGet.Core.Tests/GitGet.Core.Tests.csproj` | XML | ⚙️ 测试项目文件（net9.0, xUnit） | ⚪ 待创建 |
| 88 | `src/GitGet.Core.Tests/Services/GitHubApiClientTests.cs` | C# | 🧪 GitHub API 客户端单元测试（Mock HTTP 消息处理器） | ⚪ 待创建 |
| 89 | `src/GitGet.Core.Tests/Services/DownloadServiceTests.cs` | C# | 🧪 下载管理器单元测试（任务生命周期/状态转换/断点续传） | ⚪ 待创建 |
| 90 | `src/GitGet.Core.Tests/Services/RecommendServiceTests.cs` | C# | 🧪 推荐引擎单元测试（兴趣模型构建/排序/去重） | ⚪ 待创建 |
| 91 | `src/GitGet.Core.Tests/Services/AssetMatcherServiceTests.cs` | C# | 🧪 Asset 匹配器单元测试（各平台规则验证/优先级） | ⚪ 待创建 |
| 92 | `src/GitGet.Core.Tests/Services/CacheServiceTests.cs` | C# | 🧪 缓存管理器单元测试（CRUD/TTL 过期/LRU 淘汰） | ⚪ 待创建 |
| 93 | `src/GitGet.Core.Tests/Services/TokenServiceTests.cs` | C# | 🧪 Token 管理器单元测试（加密/解密/刷新） | ⚪ 待创建 |
| 94 | `src/GitGet.Core.Tests/Data/LocalDataStoreTests.cs` | C# | 🧪 SQLite 存储层单元测试（表创建/CRUD/SQL 语句） | ⚪ 待创建 |
| 95 | `src/GitGet.Core.Tests/Data/SecureDataStoreTests.cs` | C# | 🧪 安全存储单元测试（加密/解密正确性/文件读写） | ⚪ 待创建 |

---

## 5. 构建/部署/CI 文件

| # | 文件路径 | 语言 | 说明 | 状态 |
|---|----------|------|------|------|
| 96 | `.github/workflows/ci.yml` | YAML | ⚙️ CI 流程（dotnet restore + build + test） | ⚪ 待创建 |
| 97 | `.github/workflows/release.yml` | YAML | ⚙️ 发布流程（dotnet publish + 各平台安装包构建） | ⚪ 待创建 |

---

## 6. 文档文件

| # | 文件路径 | 语言 | 说明 | 状态 |
|---|----------|------|------|------|
| 98 | `README.md` | Markdown | 📄 项目介绍、安装指南、开发指南 | ⚪ 待创建 |
| 99 | `CONTRIBUTING.md` | Markdown | 📄 贡献指南（代码规范/PR 流程/分支策略） | ⚪ 待创建 |
| 100 | `CHANGELOG.md` | Markdown | 📄 版本变更日志 | ⚪ 待创建 |

---

## 7. 资源/静态文件

| # | 文件路径 | 语言 | 说明 | 状态 |
|---|----------|------|------|------|
| 101 | `assets/logo.svg` | SVG | 🎨 应用 Logo | ⚪ 待创建 |
| 102 | `assets/logo.ico` | — | 🎨 Windows 图标 | ⚪ 待创建 |
| 103 | `assets/logo.png` | PNG | 🎨 macOS/Linux 图标 | ⚪ 待创建 |
| 104 | `assets/empty-state.svg` | SVG | 🎨 空状态插图 | ⚪ 待创建 |
| 105 | `assets/error-state.svg` | SVG | 🎨 错误状态插图 | ⚪ 待创建 |
| 106 | `assets/loading.svg` | SVG | 🎨 加载动画占位图 | ⚪ 待创建 |

---

## 8. 统计汇总

| 类别 | 文件数量 | 语言分布 |
|------|----------|----------|
| ⚙️ 配置文件 | 12 个 | XML / YAML / Markdown |
| 🚀 入口文件 | 4 个 | C# / XAML |
| 🧩 模块文件（业务逻辑） | 34 个 | **C#** |
| 🧩 模块文件（UI 层） | 40 个 | **C# / XAML** |
| 🧪 测试文件 | 9 个 | **C#** |
| 🎨 资源文件 | 6 个 | SVG / PNG / ICO |
| 📄 文档 | 10 个 + 已有设计文档 | Markdown |
| **总计** | **约 106 个文件** | **C# 占 80%+** |

### 各层语言占比

```
C#（业务逻辑层）       ████████████████████  30%
C#（ViewModel 层）     ████████████████  20%
C#（测试层）            ████████  10%
XAML（Avalonia UI 视图） ██████████████████  25%
XML（项目配置）         ████  5%
Markdown / YAML / 资源  ████  5%
```

> **核心结论：项目 85% 的代码使用 C# 编写，XAML 用于 UI 描述（约 25%），所有后端逻辑、UI 数据绑定、测试均使用 C# 单一语言完成。仅需掌握 C# / .NET 即可完成全栈开发。**

---

*文档版本：v2.0*
*编制日期：2026年5月28日*
*技术栈：C# + .NET 9 + Avalonia UI + SQLite*
*撰写人：GitGet 开发团队*
