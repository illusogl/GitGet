# AssetMatcherService 安装包匹配算法说明

> **适用范围**：GitGet 项目  
> **实现文件**：`GitGet.Core/Services/AssetMatcherService.cs`  
> **目标平台**：Windows（仅发行）  
> **当前版本**：v1.0

---

## 一、算法概述

GitHub Release 中的 Asset（安装包）文件名通常包含操作系统、架构、文件类型等信息，例如：

- `app-v2.0.0-win-x64.exe` — Windows 64 位可执行文件
- `app-v2.0.0-macos-arm64.dmg` — macOS Apple Silicon 镜像
- `app-v2.0.0-linux-amd64.deb` — Linux x64 Debian 包
- `source-code.zip` — 源代码压缩包
- `release.7z` — 无平台标识的压缩包

**目标**：从一堆 Asset 文件名中，自动选出最适合当前 Windows 系统的安装包。

**核心思路**：基于**关键词评分机制**，为每个 Asset 计算一个分数，取最高分作为推荐。如果没有匹配项，返回 `null`。

---

## 二、评分权重定义

| 权重项 | 分值 | 触发条件 |
|--------|------|---------|
| **架构匹配** `ArchitectureMatchWeight` | +100 | 文件名包含当前 CPU 架构关键词（如 x64 系统匹配 "x64"/"x86_64"/"amd64"） |
| **Windows 关键词** `WindowsKeywordWeight` | +80 | 文件名包含 "win" 或 "windows" |
| **扩展名匹配** `ExtensionMatchWeight` | +60 | Windows 特有扩展名匹配（.exe +15, .msi +10） |
| **非 Windows 惩罚** | -40 | 文件名包含 macOS/Linux 关键词 |
| **非可执行文件惩罚** | -60 | 文件名以 .sha256 / .md5 / .asc / .sig / .md / .txt 结尾 |
| **源码包惩罚** | -30 | 文件名包含 "source" 或 "src" |
| **架构不匹配惩罚** | -30 | 文件名包含非当前 CPU 架构关键词（如 x64 系统匹配到 "arm64"） |
| **32 位架构**（非精确匹配） | +20 | x64 系统上匹配到 "x86"（不含 "x64"），可运行但有轻微惩罚 |

---

## 三、两阶段评分流程

### 阶段一：基础评分（`CalculateWindowsScore`）

对每个 Asset 独立计算基础分数：

```
score = Windows关键词加分 + 架构匹配分 + 扩展名加分 - 惩罚分
```

**计算步骤**：

1. **空名称检查**：如果文件名为空或纯空格 → 返回 -100（直接淘汰）

2. **Windows 关键词检查**：
   ```
   if 包含 "win" 或 "windows" → score += 80
   ```

3. **非 Windows 惩罚**：
   ```
   if 包含 "mac"/"darwin"/"osx"/"apple"/"linux"/"ubuntu"/"debian"/"fedora"
       → score -= 40
   ```
   例如 `app-macos.dmg` 在 Windows 系统上会获得 -40 分。

4. **架构匹配**（检测当前 CPU 为 x64/ARM64/x86）：
   ```
   x64 系统：匹配 "x64"/"x86_64"/"amd64" → +100
            匹配 "arm64" → -30
            匹配 "x86"（不含 "x64"）→ +20
   
   ARM64 系统：匹配 "arm64"/"aarch64" → +100
              匹配 "x64"/"x86_64" → -30
   
   x86 系统：匹配 "x86"（不含 "x64"/"x86_64"）→ +100
   ```

5. **扩展名匹配**（仅 Windows）：
   ```
   .exe → +75 (60+15)
   .msi → +70 (60+10)
   .zip → +60  [仅当包含 "win" 或 "windows" 时]
   .7z  → +50  [仅当包含 "win" 或 "windows" 时]
   ```
   > ⚠️ 注意：通用压缩包（.zip/.7z）只有同时包含 Windows 关键词时才加分，避免把源码包误判为安装包。

6. **非可执行文件惩罚**：
   ```
   .sha256 / .md5 / .asc / .sig / .md / .txt → -60
   ```
   这些都是校验文件或文档，不可能是安装包。

7. **源码包惩罚**：
   ```
   含 "source" 或 "src" → -30
   ```
   例如 `source-code.zip`、`project-src.tar.gz` 会获得 -30 分。

### 阶段二：回退机制（`GetRecommendedAssets` 中）

**触发条件**：当**所有 Asset 都不包含任何操作系统关键词**时（即都不含 win/mac/linux/darwin/ubuntu 等），触发回退机制。

**回退逻辑**：
- 将所有**有效的压缩包/安装包文件**赋予基础分 1（视为中性）
- **仍然排除**源码包（含 "source"/"src"）和校验文件（.sha256 等）
- 有效的文件类型：`.zip` / `.7z` / `.rar` / `.exe` / `.msi` / `.tar.gz` / `.tgz` / `.tar.xz`

**示例**：
| 文件名 | 阶段一得分 | 回退得分 | 最终得分 |
|--------|-----------|---------|---------|
| `release.7z` | 0（无 Windows 关键词）| 1 | **1** |
| `source-code.zip` | -30（源码惩罚）| 0（排除）| -30 |
| `README.md` | -60（非可执行）| 0（排除）| -60 |
| `checksum.sha256` | -60（非可执行）| 0（排除）| -60 |

> 最终 `release.7z` 以 1 分胜出，成为推荐安装包。

---

## 四、选择逻辑（`FindMatchingAsset`）

```
1. 对所有 Asset 调用 GetRecommendedAssets() 获取评分
2. 按分值降序排列
3. 取最高分
4. if 最高分 ≤ 0 → return null（无匹配）
5. else → return 最高分对应的 Asset
```

---

## 五、完整评分示例

### 示例 1：标准多平台 Release

当前系统：**Windows x64**

| Asset 文件名 | OS加分 | 架构加分 | 扩展名加分 | 惩罚 | 总分 | 排名 |
|-------------|--------|---------|-----------|------|------|------|
| `app-v2.0.0-win-x64.exe` | +80 | +100 | +75 | 0 | **255** | 🥇 |
| `app-v2.0.0-win-x64.msi` | +80 | +100 | +70 | 0 | 250 | 🥈 |
| `app-v2.0.0-win-x86.exe` | +80 | +20 | +75 | 0 | 175 | 🥉 |
| `app-v2.0.0-macos.dmg` | -40 | 0 | 0 | 0 | -40 | 4 |
| `app-v2.0.0-linux-amd64.deb` | -40 | +100 | 0 | 0 | 60 | (淘汰，非 Windows)不推荐 |

**结果**：选中 `app-v2.0.0-win-x64.exe`（255 分）

### 示例 2：无平台标识的 Release（回退机制）

当前系统：**Windows x64**

| Asset 文件名 | 阶段一得分 | 回退得分 | 最终得分 | 排名 |
|-------------|-----------|---------|---------|------|
| `release.7z` | 0 | 1 | **1** | 🥇 |
| `source-code.zip` | -30 | 0（排除）| -30 | 不推荐 |
| `SHA256SUMS` | 0 | 0（非压缩包）| 0 | 不推荐 |

**结果**：选中 `release.7z`（1 分，唯一有效文件）

### 示例 3：全是源码/文档（无匹配）

| Asset 文件名 | 得分 | 排名 |
|-------------|------|------|
| `source-code.zip` | -30 | 0 |
| `README.md` | -60 | 0 |

**结果**：10 ❌ `return null`（没有有效安装包）

---

## 六、关键代码位置

| 方法 | 位置 | 说明 |
|------|------|------|
| `FindMatchingAsset()` | `AssetMatcherService.cs:16` | 主入口，返回最佳匹配 Asset 或 null |
| `GetRecommendedAssets()` | `AssetMatcherService.cs:28` | 评分入口，含回退逻辑 |
| `CalculateWindowsScore()` | `AssetMatcherService.cs:63` | 基础评分计算 |
| `ScoreByOS()` | 已合并到 `CalculateWindowsScore` | OS 关键词匹配 |
| `ScoreByArchitecture()` | 已合并到 `CalculateWindowsScore` | CPU 架构匹配 |
| `ScoreByExtension()` | 已合并到 `CalculateWindowsScore` | 文件扩展名匹配 |

---

## 七、为什么使用评分机制而非规则匹配？

| 方式 | 优点 | 缺点 |
|------|------|------|
| 规则匹配（if-else 链） | 简单直接 | 无法处理边缘情况（如 `app-x64.exe`），新增规则越来越复杂 |
| **评分机制** ✅ | 灵活、可组合、易扩展 | 需要设计合理的权重体系 |

**评分机制的优势**：
- 多个维度独立评分后相加，逻辑清晰
- 新增匹配维度只需添加新规则并赋权重
- 权重可调优（根据实际使用数据优化分值分配）
- 回退机制保证了即使没有完美匹配也能选出一个相对合适的文件

---

## 八、扩展方向（未来改进）

1. **文件大小加权**：同等评分下优先选择较小的文件
2. **下载次数加权**：GitHub API 返回的 `download_count` 可作为热门度参考
3. **用户偏好学习**：记录用户手动选择的 Asset 类型，调整权重
4. **签名验证**：检测 .asc/.sig 配套的 .exe 文件，自动标记"已验证签名"
5. **多语言适配**：支持中文文件名识别（如 "Windows版"、"苹果版"）

---

*编制日期：2026年6月14日*
*关联文件：`GitGet.Core/Services/AssetMatcherService.cs`*