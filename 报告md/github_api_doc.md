# GitHub REST API 快速入门

了解如何开始使用 GitHub REST API。

## 简介

本文介绍如何快速地通过 GitHub、`curl` 或 JavaScript 开始使用 GitHub CLI REST API。 有关更详细的指南，请参阅 [REST API 入门](/zh/rest/guides/getting-started-with-the-rest-api)。

<div class="ghd-tool cli">

## 在命令行中使用 GitHub CLI

GitHub CLI 是从命令行使用 GitHub REST API 的最简单方式。

1. 在 macOS、Windows 或 Linux 上安装 GitHub CLI。 有关安装说明的详细信息，请参阅 GitHub CLI 存储库中的[安装](https://github.com/cli/cli?ref_product=cli\&ref_type=engagement\&ref_style=text#installation)。

2. 若要向 GitHub 进行身份验证，请从终端运行以下命令。

   ```shell
   gh auth login
   ```

3. 选择要进行身份验证的位置：

   * 如果通过 GitHub 访问 GitHub.com，请选择“GitHub.com”\*\*\*\*。
   * 如果通过其他域访问 GitHub，请选择“其他”，然后输入主机名（例如 \*\*\*\*）`octocorp.ghe.com`。

4. 按照屏幕上的其余提示操作。

   选择 HTTPS 作为 Git 操作的首选协议时，GitHub CLI 将自动存储 Git 凭据，并对询问是否要使用 GitHub 凭据向 Git 进行身份验证的提示回答“是”。 此操作非常有用，因为这允许直接使用 `git push`、`git pull` 等 Git 命令，无需设置单独的凭据管理器或使用 SSH。

5. 使用 GitHub CLI `api` 子命令并跟随相应路径发起请求。 使用 `--method` 或 `-X` 标志指定方法。 有关详细信息，请参阅 [GitHub CLI `api` 文档](https://cli.github.com/manual/gh_api)。

   此示例向使用方法 `GET` 和路径 `/octocat` 的“Get Octocat”终结点发出请求。 有关此终结点的完整参考文档，请参阅 [元数据的 REST API 端点](/zh/rest/meta/meta#get-octocat)。

   ```shell copy
   gh api /octocat --method GET
   ```

## 在 GitHub CLI 中使用 GitHub Actions

还可以在 GitHub CLI 工作流中使用 GitHub Actions。 有关详细信息，请参阅“[在工作流中使用 GitHub CLI](/zh/actions/using-workflows/using-github-cli-in-workflows)”。

### 使用访问令牌进行身份验证

不要使用 `gh auth login` 命令，而是将访问令牌作为名为 `GH_TOKEN` 的环境变量进行传递。 GitHub 建议使用内置 `GITHUB_TOKEN`，而不是创建令牌。 如果无法执行此操作，请将令牌存储为机密，并将以下示例中的 `GITHUB_TOKEN` 替换为机密的名称。 有关 `GITHUB_TOKEN` 的详细信息，请参阅 [在工作流中使用 GITHUB\_TOKEN 进行身份验证](/zh/actions/security-guides/automatic-token-authentication)。 有关机密的详细信息，请参阅 [在 GitHub Actions 中使用机密](/zh/actions/security-guides/encrypted-secrets)。

以下示例工作流使用[列出仓库问题](/zh/rest/issues/issues#list-repository-issues)接口，并请求`octocat/Spoon-Knife` 仓库中的问题列表。

```yaml copy
on:
  workflow_dispatch:
jobs:
  use_api:
    runs-on: ubuntu-latest
    permissions:
      issues: read
    steps:
      - env:
          GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        run: |
          gh api https://api.github.com/repos/octocat/Spoon-Knife/issues
```

### 使用 GitHub App 进行身份验证

如果使用 GitHub App 进行身份验证，则可以在工作流中创建安装访问令牌：

1. 将 GitHub App 的 ID 存储为配置变量。 在下面的示例中，将 `APP_ID` 替换为配置变量的名称。 可以在应用的设置页面上或通过 API 找到应用 ID。 有关详细信息，请参阅“[GitHub Apps 的 REST API 终结点](/zh/rest/apps/apps#get-an-app)”。 有关配置变量的详细信息，请参阅 [在变量中存储信息](/zh/actions/learn-github-actions/variables#defining-configuration-variables-for-multiple-workflows)。
2. 为应用生成私钥。 将所生成文件的内容作为机密进行存储。 （存储文件的全部内容，包括 `-----BEGIN RSA PRIVATE KEY-----` 和 `-----END RSA PRIVATE KEY-----`。）在以下示例中，将 `APP_PEM` 替换为机密的名称。 有关详细信息，请参阅“[管理GitHub应用的私钥](/zh/apps/creating-github-apps/authenticating-with-a-github-app/managing-private-keys-for-github-apps)”。 有关机密的详细信息，请参阅 [在 GitHub Actions 中使用机密](/zh/actions/security-guides/encrypted-secrets)。
3. 添加用于生成令牌的步骤，并使用该令牌而不是 `GITHUB_TOKEN`。 请注意，此令牌会在 60 分钟后过期。 例如：

   ```yaml copy
   on:
     workflow_dispatch:
   jobs:
     track_pr:
       runs-on: ubuntu-latest
       steps:
         - name: Generate token
           id: generate-token
           uses: actions/create-github-app-token@v2
           with:
             app-id: ${{ vars.APP_ID }}
             private-key: ${{ secrets.APP_PEM }}
         - name: Use API
           env:
             GH_TOKEN: ${{ steps.generate-token.outputs.token }}
           run: |
             gh api https://api.github.com/repos/octocat/Spoon-Knife/issues
   ```

</div>

<div class="ghd-tool javascript">

## 使用 Octokit.js

可以在 JavaScript 脚本中使用 Octokit.js 与 GitHub REST API 进行交互。 有关详细信息，请参阅[使用 REST API 和 JavaScript 编写脚本](/zh/rest/guides/scripting-with-the-rest-api-and-javascript)。

1. 创建访问令牌。 例如，创建 personal access token 或 GitHub App 用户访问令牌。 你将使用此令牌对请求进行身份验证，因此应向其授予访问该终结点所需的任何范围或权限。 有关详细信息，请参阅 [对 REST API 进行身份验证](/zh/rest/overview/authenticating-to-the-rest-api) 或 [Identing and authorizing users for GitHub Apps](/zh/developers/apps/building-github-apps/identifying-and-authorizing-users-for-github-apps)。

   > \[!WARNING]
   > 将访问令牌看做是密码。
   >
   > 若要确保令牌安全，可以将令牌存储为机密，并通过 GitHub Actions 运行脚本。 有关详细信息，请参阅[在 GitHub Actions 中使用 Octokit.js](#using-octokitjs-in-github-actions)部分。

   还可以将令牌存储为 Codespaces 机密，并在 Codespaces 中运行脚本。 有关详细信息，请参阅[管理 Codespaces 的加密机密](/zh/codespaces/managing-your-codespaces/managing-encrypted-secrets-for-your-codespaces)。

   > 如果无法使用这些选项，请考虑使用其他 CLI 服务安全地存储令牌。

2. 安装 `octokit`。 例如，`npm install octokit`。 有关安装或加载 `octokit` 的其他方式，请参阅 [Octokit.js 自述文件](https://github.com/octokit/octokit.js/#readme)。

3. 在脚本中导入 `octokit`。 例如，`import { Octokit } from "octokit";`。 有关导入 `octokit` 的其他方式，请参阅 [Octokit.js 自述文件](https://github.com/octokit/octokit.js/#readme)。

4. 使用令牌创建 `Octokit` 的实例。
   将 `YOUR-TOKEN` 替换为令牌。

   ```javascript copy
   const octokit = new Octokit({ 
     auth: 'YOUR-TOKEN'
   });
   ```

5. 使用 `octokit.request` 执行请求。 将 HTTP 方法和路径作为第一个参数发送。 将对象中的任何路径、查询和正文参数指定为第二个参数。 有关参数的详细信息，请参阅 [REST API 入门](/zh/rest/guides/getting-started-with-the-rest-api#using-parameters)。

   例如，在以下请求中，HTTP 方法为 `GET`，路径为 `/repos/{owner}/{repo}/issues`，参数为 `owner: "octocat"` 和 `repo: "Spoon-Knife"`。

   ```javascript copy
   await octokit.request("GET /repos/{owner}/{repo}/issues", {
     owner: "octocat",
     repo: "Spoon-Knife",
   });
   ```

## 在 GitHub Actions 中使用 Octokit.js

还可以在 GitHub Actions 工作流中执行 JavaScript 脚本。 有关详细信息，请参阅“[GitHub Actions 的工作流语法](/zh/actions/using-workflows/workflow-syntax-for-github-actions#jobsjob_idstepsrun)”。

### 使用访问令牌进行身份验证

GitHub 建议使用内置 `GITHUB_TOKEN`，而不是创建令牌。 如果无法执行此操作，请将令牌存储为机密，并将以下示例中的 `GITHUB_TOKEN` 替换为机密的名称。 有关 `GITHUB_TOKEN` 的详细信息，请参阅 [在工作流中使用 GITHUB\_TOKEN 进行身份验证](/zh/actions/security-guides/automatic-token-authentication)。 有关机密的详细信息，请参阅 [在 GitHub Actions 中使用机密](/zh/actions/security-guides/encrypted-secrets)。

以下示例工作流：

1. 检出存储库内容
2. 设置 Node.js
3. 安装 `octokit`
4. 将 `GITHUB_TOKEN` 的值存储为名为 `TOKEN` 的环境变量，并运行 `.github/actions-scripts/use-the-api.mjs`（它可以将该环境变量作为 `process.env.TOKEN` 进行访问）。

```yaml
on:
  workflow_dispatch:
jobs:
  use_api_via_script:
    runs-on: ubuntu-latest
    permissions:
      issues: read
    steps:
      - name: Check out repo content
        uses: actions/checkout@v6

      - name: Setup Node
        uses: actions/setup-node@v4
        with:
          node-version: '16.17.0'
          cache: npm

      - name: Install dependencies
        run: npm install octokit

      - name: Run script
        run: |
          node .github/actions-scripts/use-the-api.mjs
        env:
          TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

以下是包含文件路径 `.github/actions-scripts/use-the-api.mjs` 的示例 JavaScript 脚本。

```javascript
import { Octokit } from "octokit"

const octokit = new Octokit({ 
  auth: process.env.TOKEN
});

try {
  const result = await octokit.request("GET /repos/{owner}/{repo}/issues", {
      owner: "octocat",
      repo: "Spoon-Knife",
    });

  const titleAndAuthor = result.data.map(issue => {title: issue.title, authorID: issue.user.id})

  console.log(titleAndAuthor)

} catch (error) {
  console.log(`Error! Status: ${error.status}. Message: ${error.response.data.message}`)
}
```

### 使用 GitHub App 进行身份验证

如果使用 GitHub App 进行身份验证，则可以在工作流中创建安装访问令牌：

1. 将 GitHub App 的 ID 存储为配置变量。 在下面的示例中，将 `APP_ID` 替换为配置变量的名称。 您可以在应用的设置页面上或通过应用 API 找到应用 ID。 有关详细信息，请参阅“[GitHub Apps 的 REST API 终结点](/zh/rest/apps/apps#get-an-app)”。 有关配置变量的详细信息，请参阅 [在变量中存储信息](/zh/actions/learn-github-actions/variables#defining-configuration-variables-for-multiple-workflows)。
2. 为应用生成私钥。 将所生成文件的内容作为机密进行存储。 （存储文件的全部内容，包括 `-----BEGIN RSA PRIVATE KEY-----` 和 `-----END RSA PRIVATE KEY-----`。）在以下示例中，将 `APP_PEM` 替换为机密的名称。 有关详细信息，请参阅“[管理GitHub应用的私钥](/zh/apps/creating-github-apps/authenticating-with-a-github-app/managing-private-keys-for-github-apps)”。 有关机密的详细信息，请参阅 [在 GitHub Actions 中使用机密](/zh/actions/security-guides/encrypted-secrets)。
3. 添加用于生成令牌的步骤，并使用该令牌而不是 `GITHUB_TOKEN`。 请注意，此令牌会在 60 分钟后过期。 例如：

   ```yaml
   on:
     workflow_dispatch:
   jobs:
     use_api_via_script:
       runs-on: ubuntu-latest
       steps:
         - name: Check out repo content
           uses: actions/checkout@v6

         - name: Setup Node
           uses: actions/setup-node@v4
           with:
             node-version: '16.17.0'
             cache: npm

         - name: Install dependencies
           run: npm install octokit

         - name: Generate token
           id: generate-token
           uses: actions/create-github-app-token@v2
           with:
             app-id: ${{ vars.APP_ID }}
             private-key: ${{ secrets.APP_PEM }}

         - name: Run script
           run: |
             node .github/actions-scripts/use-the-api.mjs
           env:
             TOKEN: ${{ steps.generate-token.outputs.token }}

   ```

</div>

<div class="ghd-tool curl">

## 使用命令行中的 `curl`

> \[!NOTE]
> 如果要从命令行发出 API 请求，GitHub 建议使用 GitHub CLI，可以简化身份验证和请求。 有关通过 GitHub CLI 开始使用 REST API 的详细信息，请参阅本文的 GitHub CLI 版本。

1. 如果计算机上尚未安装 `curl`，请安装。 若要检查是否安装了 `curl`，请在命令行中执行 `curl --version`。 如果输出是有关 `curl` 版本的信息，则表示已安装 `curl`。 如果收到类似于 `command not found: curl` 的消息，则需要下载并安装 `curl`。 有关详细信息，请参阅 [curl 项目下载页面](https://curl.se/download.html)。

2. 创建访问令牌。 例如，创建 personal access token 或 GitHub App 用户访问令牌。 使用此令牌对请求进行身份验证，因此应向其授予访问终结点所需的任何范围或权限。 有关详细信息，请参阅“[对 REST API 进行身份验证](/zh/rest/overview/authenticating-to-the-rest-api)”。

   > \[!WARNING]
   > 将访问令牌看做是密码。
   >
   > 若要确保令牌安全，可以将令牌存储为 Codespaces 机密，并通过 Codespaces 使用命令行。 有关详细信息，请参阅[管理 Codespaces 的加密机密](/zh/codespaces/managing-your-codespaces/managing-encrypted-secrets-for-your-codespaces)。

   > 还可以使用 GitHub CLI，而不是 `curl`。 GitHub CLI 会为你处理身份验证。 有关详细信息，请参阅此页面的 GitHub CLI 版本。
   >
   > 如果无法使用这些选项，请考虑使用其他 CLI 服务安全地存储令牌。

3. 使用 `curl` 命令发出请求。 通过 `Authorization` 标头传递你的令牌。
   将 `YOUR-TOKEN` 替换为令牌。

   ```shell copy
   curl --request GET \
   --url "https://api.github.com/repos/octocat/Spoon-Knife/issues" \
   --header "Accept: application/vnd.github+json" \
   --header "Authorization: Bearer YOUR-TOKEN"
   ```

   > \[!NOTE]
   > 在大多数情况下，可以使用 `Authorization: Bearer` 或 `Authorization: token` 传递令牌。 但是，如果要传递 JSON Web 令牌 (JWT)，则必须使用 `Authorization: Bearer`。

## 在 GitHub Actions 中使用 `curl` 命令

还可以在 GitHub Actions 工作流中使用 `curl` 命令。

### 使用访问令牌进行身份验证

GitHub 建议使用内置 `GITHUB_TOKEN`，而不是创建令牌。 如果无法执行此操作，请将令牌存储为机密，并将以下示例中的 `GITHUB_TOKEN` 替换为机密的名称。 有关 `GITHUB_TOKEN` 的详细信息，请参阅 [在工作流中使用 GITHUB\_TOKEN 进行身份验证](/zh/actions/security-guides/automatic-token-authentication)。 有关机密的详细信息，请参阅 [在 GitHub Actions 中使用机密](/zh/actions/security-guides/encrypted-secrets)。

```yaml copy
on:
  workflow_dispatch:
jobs:
  use_api:
    runs-on: ubuntu-latest
    permissions:
      issues: read
    steps:
      - env:
          GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        run: |
          curl --request GET \
          --url "https://api.github.com/repos/octocat/Spoon-Knife/issues" \
          --header "Accept: application/vnd.github+json" \
          --header "Authorization: Bearer $GH_TOKEN"
```

### 使用 GitHub App 进行身份验证

如果使用 GitHub App 进行身份验证，则可以在工作流中创建安装访问令牌：

1. 将 GitHub App 的 ID 存储为配置变量。 在下面的示例中，将 `APP_ID` 替换为配置变量的名称。 您可以在应用的设置页面上或通过应用 API 找到应用 ID。 有关详细信息，请参阅“[GitHub Apps 的 REST API 终结点](/zh/rest/apps/apps#get-an-app)”。 有关配置变量的详细信息，请参阅 [在变量中存储信息](/zh/actions/learn-github-actions/variables#defining-configuration-variables-for-multiple-workflows)。
2. 为应用生成私钥。 将所生成文件的内容作为机密进行存储。 （存储文件的全部内容，包括 `-----BEGIN RSA PRIVATE KEY-----` 和 `-----END RSA PRIVATE KEY-----`。）在以下示例中，将 `APP_PEM` 替换为机密的名称。 有关详细信息，请参阅“[管理GitHub应用的私钥](/zh/apps/creating-github-apps/authenticating-with-a-github-app/managing-private-keys-for-github-apps)”。 有关存储机密的详细信息，请参阅“[在 GitHub Actions 中使用机密](/zh/actions/security-guides/encrypted-secrets)”。
3. 添加用于生成令牌的步骤，并使用该令牌而不是 `GITHUB_TOKEN`。 请注意，此令牌会在 60 分钟后过期。 例如：

   ```yaml copy
   on:
     workflow_dispatch:
   jobs:
     use_api:
       runs-on: ubuntu-latest
       steps:
         - name: Generate token
           id: generate-token
           uses: actions/create-github-app-token@v2
           with:
             app-id: ${{ vars.APP_ID }}
             private-key: ${{ secrets.APP_PEM }}

         - name: Use API
           env:
             GH_TOKEN: ${{ steps.generate-token.outputs.token }}
           run: |
             curl --request GET \
             --url "https://api.github.com/repos/octocat/Spoon-Knife/issues" \
             --header "Accept: application/vnd.github+json" \
             --header "Authorization: Bearer $GH_TOKEN"

   ```

</div>

## 后续步骤

有关更详细的指南，请参阅 [REST API 入门](/zh/rest/guides/getting-started-with-the-rest-api)。