# GitHub REST API 使用指南报告

> **编制日期**：2026年6月14日  
> **文档版本**：v1.0  
> **API 版本**：2026-03-10（最新）  
> **适用范围**：GitGet 项目开发参考

---

## 一、API 概述

### 1.1 基本信息

| 项目 | 内容 |
|------|------|
| 基础 URL | `https://api.github.com` |
| API 版本 | `2026-03-10`（最新） |
| 请求格式 | JSON |
| 认证方式 | Bearer Token / Personal Access Token / GitHub App Token |
| 速率限制（未认证） | 每小时 60 次 |
| 速率限制（已认证） | 每小时 5,000 次 |
| 推荐 Accept 头 | `application/vnd.github+json` |

### 1.2 使用工具

GitHub 官方推荐三种方式调用 REST API：

| 工具 | 适用场景 | 优点 |
|------|---------|------|
| **GitHub CLI** | 命令行交互 | 自动处理认证，最简单 |
| **JavaScript (Octokit.js)** | Web/Node.js 应用 | 封装完善的官方 SDK |
| **curl** | 命令行调试 | 最直接、最灵活 |

**GitGet 项目使用 C# 的 `HttpClient` 来直接调用 GitHub REST API**，与 curl 方式原理一致。

---

## 二、认证方式

### 2.1 三种认证方式对比

| 认证方式 | 有效期 | 适用场景 | GitGet 是否使用 |
|---------|--------|---------|----------------|
| **Personal Access Token (PAT)** | 可自定义（最长1年） | 个人脚本、桌面应用 | ✅ 推荐使用 |
| **GitHub App Token** | 60 分钟 | CI/CD 工作流 | ❌ 不适用 |
| **GITHUB_TOKEN** | 单个工作流运行期间 | GitHub Actions | ❌ 不适用 |
| **OAuth App** | 用户授权 | 需要用户登录的第三方应用 | ✅ 未来使用 |

### 2.2 认证请求头

无论使用哪种方式，请求头格式统一为：

```http
Authorization: Bearer YOUR-TOKEN
Accept: application/vnd.github+json
```

> ⚠️ **重要**：如果不提供 Token，API 仍可调用（匿名），但速率限制仅为每小时 60 次。**GitGet 项目目前已实现无 Token 的匿名调用，后续将支持 PAT 登录。**

### 2.3 Token 获取方式（Personal Access Token）

1. 登录 GitHub → **Settings** → **Developer settings** → **Personal access tokens** → **Fine-grained tokens**
2. 点击 **Generate new token**
3. 设置权限（仅勾选需要的）：
   - `Read access to metadata`（必须）
   - `Read access to code`（查看仓库内容）
   - `Read access to issues`（查看 Issue，可选）
4. 生成后**立即保存**（只显示一次）

### 2.4 GitGet 项目的认证实现

```
GitGet.Core/Services/GitHubApiClient.cs  →  构造 Authorization 请求头
GitGet.Core/Data/SecureDataStore.cs      →  加密存储 Token
GitGet.Core/Services/TokenService.cs      →  （未来）OAuth 登录流程
```

---

## 三、GitGet 项目涉及的关键 API 端点

### 3.1 搜索仓库

```
GET /search/repositories
```

**查询参数：**

| 参数 | 类型 | 说明 | GitGet 使用场景 |
|------|------|------|---------------|
| `q` | string | 搜索关键词（必填） | 用户搜索框输入 |
| `sort` | string | 排序方式：`stars` / `forks` / `updated` | 搜索结果排序 |
| `order` | string | `desc` / `asc` | 默认 desc |
| `per_page` | integer | 每页数量（最大100） | 分页加载 |
| `page` | integer | 页码 | 加载更多 |

**搜索语法示例：**

```
q=dotnet                     # 搜索关键词 "dotnet"
q=language:C#                # 按语言过滤
q=stars:>10000               # Star 超过 1 万的仓库
q=topic:editor               # 按 Topic 过滤
q=dotnet+language:C#         # 组合搜索
```

**GitGet 实际调用路径：**
- 首页热门推荐 → `q=stars:>10000&sort=stars&order=desc&per_page=20`
- 用户搜索 → `q={keyword}&sort=stars&order=desc&per_page=30`
- 按语言筛选 → `q={keyword}+language:{lang}&sort=stars`

**示例 curl：**
```bash
curl -H "Accept: application/vnd.github+json" \
     -H "Authorization: Bearer YOUR-TOKEN" \
     "https://api.github.com/search/repositories?q=dotnet+language:C%23&sort=stars&order=desc"
```

### 3.2 获取仓库详情

```
GET /repos/{owner}/{repo}
```

**示例：** `GET /repos/dotnet/runtime`

**返回的关键字段（GitGet 使用）：**

| 字段 | 说明 |
|------|------|
| `id` | 仓库 ID |
| `full_name` | 完整名称 "owner/repo" |
| `description` | 项目描述 |
| `stargazers_count` | Star 数量 |
| `forks_count` | Fork 数量 |
| `language` | 主编程语言 |
| `topics` | Topic 标签列表 |
| `license.name` | 开源协议 |
| `html_url` | 项目主页 |
| `created_at` / `updated_at` | 创建/更新时间 |

### 3.3 获取仓库的 Releases

```
GET /repos/{owner}/{repo}/releases
```

**查询参数：**

| 参数 | 类型 | 说明 |
|------|------|------|
| `per_page` | integer | 每页数量 |
| `page` | integer | 页码 |

**返回的关键字段：**

| 字段 | 说明 | GitGet 使用 |
|------|------|------------|
| `id` | Release ID | 唯一标识 |
| `tag_name` | 版本标签 "v2.0.0" | 显示版本号 |
| `name` | 发布名称 | 显示标题 |
| `body` | Release Notes（Markdown） | 展示更新日志 |
| `prerelease` | 是否为预发布 | 标记测试版 |
| `published_at` | 发布时间 | 排序、显示 |
| `assets` | 安装包列表（数组） | 下载匹配 |

### 3.4 获取 Release 的 Asset（安装包）

Asset 是 Releases 响应中的嵌套数组，**无需额外 API 调用**。

**Asset 对象字段：**

| 字段 | 说明 | GitGet 使用 |
|------|------|------------|
| `id` | Asset ID | 唯一标识 |
| `name` | 文件名 "app-2.0.0-win-x64.exe" | **智能匹配操作系统** |
| `size` | 文件大小（字节） | 显示下载大小 |
| `content_type` | MIME 类型 "application/octet-stream" | 文件类型判断 |
| `browser_download_url` | 浏览器直接下载链接 | **实际下载地址** |
| `download_count` | 下载次数 | 显示热度 |

### 3.5 获取用户 Star 的仓库

```
GET /users/{username}/starred
```

**用途：** 个性化推荐引擎的数据来源

> ⚠️ **注意**：此端点需要认证。未认证用户将使用全局 Trending 作为降级策略。

### 3.6 资源下载（通过 Asset URL）

GitHub 的 Release Asset 可以直接通过 `browser_download_url` 下载，**不需要额外的 API 调用**。

**GitGet 下载流程：**
1. 调用 `GET /repos/{owner}/{repo}/releases` 获取 Release 列表
2. 遍历 `assets` 数组，用 `AssetMatcherService` 匹配当前操作系统
3. 使用 `HttpClient` 直接下载 `browser_download_url`（流式下载 + 进度报告）
4. 支持 `Range: bytes={offset}-` 断点续传

---

## 四、速率限制（Rate Limiting）

### 4.1 限制对比

| 认证状态 | 每小时限制 | 适用场景 |
|---------|-----------|---------|
| 未认证（匿名） | **60 次** | 主页浏览、测试 |
| 已认证（PAT） | **5,000 次** | 正常使用 |
| GitHub App | 5,000 - 15,000 次 | 企业级应用 |

### 4.2 速率限制响应头

每次 API 响应都会包含以下头部：

```
X-RateLimit-Limit: 60          # 总限制
X-RateLimit-Remaining: 57      # 剩余次数
X-RateLimit-Reset: 1620000000  # 重置时间（Unix 时间戳）
```

### 4.3 GitGet 的速率限制处理策略

已实现：
- ✅ 每次请求后解析 `X-RateLimit-Remaining` 响应头
- ✅ 当剩余次数低于阈值时（< 10 次），前端提示用户登录

计划实现：
- ⬜ 登录后速率从 60 → 5,000 次/小时
- ⬜ 全局限流：1 秒内最多 1 个请求（GitHub 推荐）
- ⬜ 收到 429 Too Many Requests 时，读取 `Retry-After` 头并等待

---

## 五、分页（Pagination）

### 5.1 GitHub 的分页方式

GitHub API 使用 **基于页码的分页**：

```
GET /search/repositories?q=dotnet&page=1&per_page=30  ← 第 1 页
GET /search/repositories?q=dotnet&page=2&per_page=30  ← 第 2 页
```

### 5.2 分页响应头

```
Link: <https://api.github.com/search/repositories?q=dotnet&page=2>; rel="next",
      <https://api.github.com/search/repositories?q=dotnet&page=34>; rel="last"
```

### 5.3 GitGet 的分页实现

已实现：
- ✅ 解析 `Link` 响应头中的 `next` / `last` 链接
- ✅ 支持 `page` / `per_page` 参数
- ✅ 搜索结果分页加载（滚动触发加载更多）

---

## 六、错误处理

### 6.1 常见 HTTP 状态码

| 状态码 | 含义 | GitGet 处理策略 |
|--------|------|----------------|
| 200 | 成功 | 正常解析 JSON |
| 304 | 未修改（缓存有效） | 使用本地缓存 |
| 401 | 未授权 | 提示 Token 无效或已过期 |
| 403 | 禁止访问 / 速率限制 | 提示用户等待 |
| 404 | 资源不存在 | 返回 null / 空列表 |
| 422 | 请求参数错误 | 记录日志 |
| 429 | 请求过多 | 读取 Retry-After，等待后重试 |
| 500 | 服务器错误 | 重试 3 次，仍失败则报错 |

### 6.2 GitGet 的错误处理实现

已实现：
- ✅ `GitHubApiClient` 中对 401 返回 `null`
- ✅ 对 404 返回 `null`（仓库不存在）
- ✅ `DownloadService` 中网络中断自动重试（最多 3 次）

---

## 七、GitHub API 与 GitGet 模块映射

| GitGet 功能 | API 端点 | 实现文件 | 状态 |
|------------|---------|---------|------|
| 搜索仓库 | `GET /search/repositories` | `GitHubApiClient.cs` | ✅ 已实现 |
| 仓库详情 | `GET /repos/{owner}/{repo}` | `GitHubApiClient.cs` | ✅ 已实现 |
| Release 列表 | `GET /repos/{owner}/{repo}/releases` | `GitHubApiClient.cs` | ✅ 已实现 |
| Star 列表 | `GET /users/{user}/starred` | `GitHubApiClient.cs` | ✅ 已实现 |
| 用户信息 | `GET /user` | `GitHubApiClient.cs` | ✅ 已实现 |
| Asset 匹配 | 本地算法 | `AssetMatcherService.cs` | ⬜ 计划实现 |
| 文件下载 | HTTP 流式下载 | `DownloadService.cs` | ⬜ 计划实现 |
| OAuth 登录 | OAuth App + Token | `TokenService.cs` | ⬜ 计划实现 |
| 个性化推荐 | 基于 Star 分析 | `RecommendationService.cs` | ⬜ 计划实现 |

---

## 八、开发注意事项

### 8.1 API 版本管理

GitHub REST API 已于 2022 年 11 月开始版本管理。请求中**必须**指定 API 版本：

```
Accept: application/vnd.github+json
X-GitHub-Api-Version: 2026-03-10
```

> GitGet 项目中，`GitHubApiClient.cs` 已设置默认使用最新 API 版本。

### 8.2 最佳实践

1. **始终设置 User-Agent 头**（GitHub 要求）：
   ```
   User-Agent: GitGet/1.0
   ```

2. **限制并发请求**：单账号每秒不超过 1 个请求

3. **优先使用条件请求**：传递 `If-None-Match` 头可以减少不必要的 API 调用：
   ```
   If-None-Match: "abc123..."
   ```

4. **缓存策略**：
   - 仓库搜索结果：缓存 10 分钟
   - Release 信息：缓存 30 分钟
   - Star 列表：缓存 6 小时

5. **敏感 Token 保护**：
   - Token 使用 AES-256-GCM 加密存储
   - 代码中不硬编码 Token
   - `.gitignore` 排除 `token.enc`、`tokens.json`

### 8.3 当前项目使用情况

GitGet 项目目前使用**匿名调用**方式（无 Token），速率限制为每小时 60 次。这在进行开发和测试时需要注意：
- 连续请求不建议超过 50 次
- 如需更高限制，应使用 Personal Access Token

---

## 九、关键参考链接

| 资源 | URL |
|------|-----|
| GitHub REST API 文档 | https://docs.github.com/zh/rest |
| 快速入门 | https://docs.github.com/zh/rest/quickstart |
| 搜索 API | https://docs.github.com/zh/rest/search/search |
| Release API | https://docs.github.com/zh/rest/releases/releases |
| 速率限制 | https://docs.github.com/zh/rest/using-the-rest-api/rate-limits-for-the-rest-api |
| 分页 | https://docs.github.com/zh/rest/using-the-rest-api/using-pagination-in-the-rest-api |
| 认证 | https://docs.github.com/zh/rest/authentication/authenticating-to-the-rest-api |
| Octokit.js | https://github.com/octokit/octokit.js |
| GitHub CLI | https://cli.github.com |

---

*本文档基于 GitHub 官方 REST API 文档（2026-03-10 版本）编制，供 GitGet 项目开发参考。*