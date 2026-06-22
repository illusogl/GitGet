namespace GitGet.WebUI.Services;

public class LocalizationService
{
    private string _lang = "zh";
    public string Lang => _lang;

    public event Action? OnLanguageChanged;

    public void SetLanguage(string lang)
    {
        if (_lang != lang)
        {
            _lang = lang;
            OnLanguageChanged?.Invoke();
        }
    }

    public string T(string key) => _lang switch
    {
        "en" => _en.GetValueOrDefault(key, key),
        _ => _zh.GetValueOrDefault(key, key)
    };

    private static readonly Dictionary<string, string> _zh = new()
    {
        ["nav.home"] = "首页",
        ["nav.search"] = "搜索",
        ["nav.downloads"] = "下载",
        ["nav.login"] = "登录",
        ["nav.settings"] = "设置",

        ["home.title"] = "热门项目",
        ["home.subtitle"] = "发现 GitHub 上最受欢迎的开源项目",
        ["home.today"] = "今天",
        ["home.month"] = "本月",
        ["home.year"] = "今年",
        ["home.all"] = "历史",
        ["home.loading"] = "正在加载...",
        ["home.empty_title"] = "暂无数据",
        ["home.empty_desc"] = "请检查网络连接或稍后重试",

        ["search.title"] = "搜索项目",
        ["search.subtitle"] = "在 GitHub 上搜索你感兴趣的开源项目",
        ["search.placeholder"] = "输入关键词，如: vscode, react, dotnet...",
        ["search.btn"] = "搜索",
        ["search.searching"] = "正在搜索...",
        ["search.no_results"] = "未找到结果",
        ["search.no_results_desc"] = "尝试更换关键词或检查网络连接",
        ["search.result_count"] = "找到约 {0} 个结果",

        ["detail.back"] = "返回",
        ["detail.not_selected"] = "未选择项目",
        ["detail.not_selected_desc"] = "请先从首页或搜索页选择一个项目",
        ["detail.back_home"] = "返回首页",
        ["detail.issues"] = "issues",
        ["detail.readme_tab"] = "README",
        ["detail.releases_tab"] = "Release 版本",
        ["detail.loading_readme"] = "正在加载 README...",
        ["detail.no_readme"] = "此项目没有 README.md 文件",
        ["detail.load_failed"] = "加载 README 失败: {0}",
        ["detail.retry"] = "重试",
        ["detail.loading_releases"] = "正在加载 Release 列表...",
        ["detail.no_releases"] = "暂无 Release",
        ["detail.no_releases_desc"] = "该项目还没有发布任何版本",
        ["detail.prerelease"] = "预发布",
        ["detail.download_assets"] = "下载资源 ({0})",
        ["detail.no_assets"] = "此版本没有上传资源文件",
        ["detail.recommended"] = "推荐",
        ["detail.download"] = "下载",

        ["downloads.title"] = "下载管理",
        ["downloads.subtitle"] = "查看和管理你的下载任务",
        ["downloads.empty"] = "暂无下载任务",
        ["downloads.empty_desc"] = "从项目详情页点击下载按钮开始",
        ["downloads.active"] = "进行中",
        ["downloads.no_active"] = "暂无进行中的下载",
        ["downloads.completed"] = "已完成",
        ["downloads.no_completed"] = "暂无已完成的下载",
        ["downloads.pause"] = "暂停",
        ["downloads.resume"] = "继续",
        ["downloads.cancel"] = "取消",
        ["downloads.open"] = "打开",

        ["settings.title"] = "设置",
        ["settings.subtitle"] = "配置 GitGet 的下载和外观选项",
        ["settings.download_section"] = "下载设置",
        ["settings.download_path"] = "默认下载路径",
        ["settings.download_path_hint"] = "所有下载文件将保存到此目录",
        ["settings.select_folder"] = "选择",
        ["settings.max_concurrent"] = "最大并发下载数",
        ["settings.max_concurrent_hint"] = "同时下载的最大任务数（1-10）",
        ["settings.appearance"] = "外观设置",
        ["settings.theme"] = "主题",
        ["settings.theme_light"] = "浅色",
        ["settings.theme_dark"] = "深色",
        ["settings.language"] = "语言",
        ["settings.lang_zh"] = "简体中文",
        ["settings.lang_en"] = "English",
        ["settings.cache_section"] = "缓存管理",
        ["settings.clear_cache"] = "清除缓存",
        ["settings.cache_desc"] = "清除本地缓存的仓库和 Release 数据",
        ["settings.about_section"] = "关于",
        ["settings.about_desc"] = "GitHub Release 应用商店客户端",
        ["settings.tech_stack"] = "技术栈：C# + .NET 9 + Blazor + Photino",

        ["login.title"] = "登录 GitGet",
        ["login.subtitle"] = "连接你的 GitHub 账户以获得个性化体验",
        ["login.wip"] = "GitHub OAuth 登录功能正在开发中...",
        ["login.features_desc"] = "登录后你可以：",
        ["login.feature_star"] = "星标同步",
        ["login.feature_rec"] = "个性化推荐",
        ["login.feature_cloud"] = "下载历史云同步",
        ["login.offline"] = "当前为离线模式，无需登录也可浏览和下载",

        ["card.days_ago"] = "天前",

        ["status.queued"] = "排队中",
        ["status.downloading"] = "下载中",
        ["status.paused"] = "已暂停",
        ["status.completed"] = "已完成",
        ["status.failed"] = "失败",
        ["status.cancelled"] = "已取消",
        ["status.unknown"] = "未知",
    };

    private static readonly Dictionary<string, string> _en = new()
    {
        ["nav.home"] = "Home",
        ["nav.search"] = "Search",
        ["nav.downloads"] = "Downloads",
        ["nav.login"] = "Login",
        ["nav.settings"] = "Settings",

        ["home.title"] = "Trending",
        ["home.subtitle"] = "Discover the most popular open-source projects on GitHub",
        ["home.today"] = "Today",
        ["home.month"] = "Month",
        ["home.year"] = "Year",
        ["home.all"] = "All Time",
        ["home.loading"] = "Loading...",
        ["home.empty_title"] = "No Data",
        ["home.empty_desc"] = "Check your network and try again",

        ["search.title"] = "Search",
        ["search.subtitle"] = "Search for open-source projects on GitHub",
        ["search.placeholder"] = "Enter keywords, e.g. vscode, react, dotnet...",
        ["search.btn"] = "Search",
        ["search.searching"] = "Searching...",
        ["search.no_results"] = "No Results",
        ["search.no_results_desc"] = "Try different keywords or check your network",
        ["search.result_count"] = "About {0} results",

        ["detail.back"] = "Back",
        ["detail.not_selected"] = "No Project Selected",
        ["detail.not_selected_desc"] = "Please select a project from the home or search page",
        ["detail.back_home"] = "Back to Home",
        ["detail.issues"] = "issues",
        ["detail.readme_tab"] = "README",
        ["detail.releases_tab"] = "Release Versions",
        ["detail.loading_readme"] = "Loading README...",
        ["detail.no_readme"] = "This project has no README.md",
        ["detail.load_failed"] = "Failed to load README: {0}",
        ["detail.retry"] = "Retry",
        ["detail.loading_releases"] = "Loading Releases...",
        ["detail.no_releases"] = "No Releases",
        ["detail.no_releases_desc"] = "This project has not published any releases yet",
        ["detail.prerelease"] = "Pre-release",
        ["detail.download_assets"] = "Assets ({0})",
        ["detail.no_assets"] = "This release has no asset files",
        ["detail.recommended"] = "Recommended",
        ["detail.download"] = "Download",

        ["downloads.title"] = "Downloads",
        ["downloads.subtitle"] = "View and manage your download tasks",
        ["downloads.empty"] = "No Downloads",
        ["downloads.empty_desc"] = "Click the download button on a project detail page to start",
        ["downloads.active"] = "Active",
        ["downloads.no_active"] = "No active downloads",
        ["downloads.completed"] = "Completed",
        ["downloads.no_completed"] = "No completed downloads",
        ["downloads.pause"] = "Pause",
        ["downloads.resume"] = "Resume",
        ["downloads.cancel"] = "Cancel",
        ["downloads.open"] = "Open",

        ["settings.title"] = "Settings",
        ["settings.subtitle"] = "Configure GitGet download and appearance options",
        ["settings.download_section"] = "Download",
        ["settings.download_path"] = "Default Download Path",
        ["settings.download_path_hint"] = "All downloaded files will be saved to this directory",
        ["settings.select_folder"] = "Browse",
        ["settings.max_concurrent"] = "Max Concurrent Downloads",
        ["settings.max_concurrent_hint"] = "Maximum number of simultaneous downloads (1-10)",
        ["settings.appearance"] = "Appearance",
        ["settings.theme"] = "Theme",
        ["settings.theme_light"] = "Light",
        ["settings.theme_dark"] = "Dark",
        ["settings.language"] = "Language",
        ["settings.lang_zh"] = "简体中文",
        ["settings.lang_en"] = "English",
        ["settings.cache_section"] = "Cache Management",
        ["settings.clear_cache"] = "Clear Cache",
        ["settings.cache_desc"] = "Clear locally cached repository and release data",
        ["settings.about_section"] = "About",
        ["settings.about_desc"] = "GitHub Release App Store Client",
        ["settings.tech_stack"] = "Tech Stack: C# + .NET 9 + Blazor + Photino",

        ["login.title"] = "Login to GitGet",
        ["login.subtitle"] = "Connect your GitHub account for a personalized experience",
        ["login.wip"] = "GitHub OAuth login is under development...",
        ["login.features_desc"] = "After login you can:",
        ["login.feature_star"] = "Star sync",
        ["login.feature_rec"] = "Personalized recommendations",
        ["login.feature_cloud"] = "Download history cloud sync",
        ["login.offline"] = "Currently in offline mode, browsing and downloading is available without login",

        ["card.days_ago"] = "days ago",

        ["status.queued"] = "Queued",
        ["status.downloading"] = "Downloading",
        ["status.paused"] = "Paused",
        ["status.completed"] = "Completed",
        ["status.failed"] = "Failed",
        ["status.cancelled"] = "Cancelled",
        ["status.unknown"] = "Unknown",
    };
}