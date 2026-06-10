# GitGet 依赖与运行环境说明

## 你需要安装的

你只需要安装 **.NET 9 SDK**：

| 系统 | 下载地址 | 说明 |
|------|---------|------|
| Windows | https://dotnet.microsoft.com/download/dotnet/9.0 | 下载 dotnet-sdk-9.0.x-win-x64.exe |
| macOS | https://dotnet.microsoft.com/download/dotnet/9.0 | 下载 dotnet-sdk-9.0.x-osx-x64.pkg |
| Linux | https://dotnet.microsoft.com/download/dotnet/9.0 | 按系统选择对应的包管理器安装 |

安装完成后，打开终端验证：

```bash
dotnet --version
# 应该输出 9.0.x
```

## 你不需要安装的 ❌

### SQLite ❌ 不需要安装

**SQLite 已经通过 NuGet 包自动引用了。**

在 `GitGet.Core.csproj` 中：

```xml
<PackageReference Include="Microsoft.Data.Sqlite" Version="10.0.8" />
```

当你运行 `dotnet build` 或 `dotnet test` 时，.NET 会自动：
1. 从 NuGet 仓库下载 SQLite（`Microsoft.Data.Sqlite` 和 `SQLitePCLRaw`）
2. 把 SQLite 嵌入到编译后的程序里
3. 运行时自动创建数据库文件

**整个过程完全自动，零配置。**

### Node.js ❌ 不需要安装

本项目是 .NET 桌面应用，不是 Web 项目，不需要 Node.js。

### 数据库管理工具 ❌ 不需要安装

你不需要安装 SQLiteStudio、DB Browser for SQLite 等工具。程序会自动管理数据库。

---

## SQLite 在项目中的使用场景

### 场景一：缓存搜索结果（已实现 ✅）

```
用户搜索 GitHub  →  GitHubApiClient 请求 GitHub API  →  返回结果
                                                ↓
                                          存入 SQLite（缓存）
                                                ↓
              用户再次搜同样的内容  ←  直接从 SQLite 读取，不用联网
```

**涉及的文件：**
- `GitGet.Core/Data/LocalDataStore.cs` — SQLite 增删改查
- `GitGet.Core/Services/CacheService.cs` — 缓存逻辑

### 场景二：下载任务管理（后续阶段实现）

```
用户点击下载  →  下载信息存入 SQLite（url、进度、状态）
                                       ↓
用户关闭程序再打开  →  从 SQLite 读取，恢复下载进度
```

### 场景三：用户设置存储（后续阶段实现）

```
用户设置偏好（语言筛选、每页数量等）  →  存入 SQLite
                                          ↓
每次打开软件  →  从 SQLite 读取，恢复用户设置
```

---

## SQLite 的特点

| 特性 | 说明 |
|------|------|
| 服务器 | 不需要。不像 MySQL 需要安装数据库服务器，SQLite 是嵌入式数据库 |
| 文件 | 所有数据存在一个 `.db` 文件中 |
| 大小 | 编译后约 1MB，极其轻量 |
| 依赖 | 零依赖。不依赖任何外部服务或系统组件 |
| 并发 | 支持多线程读，写锁机制保证数据安全 |

---

## 一句话总结

> **你只需要安装 .NET 9 SDK，其他一切由代码自动处理。编译时自动下载 SQLite，运行时自动创建数据库文件。什么都不要管。**