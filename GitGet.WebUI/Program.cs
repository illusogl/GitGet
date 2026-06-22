using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using GitGet.Core.Data;
using GitGet.Core.Interfaces;
using GitGet.Core.Services;
using GitGet.WebUI.Services;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Photino.Blazor;

namespace GitGet.WebUI;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        Log.Info("========================================");
        Log.Info("GitGet 启动开始");
        Log.Info($"参数: {string.Join(" ", args)}");
        Log.Info($"BaseDirectory: {AppContext.BaseDirectory}");
        Log.Info($"CurrentDirectory: {Directory.GetCurrentDirectory()}");
        Log.Info($"Framework: {RuntimeInformation.FrameworkDescription}");
        Log.Info($"OS: {RuntimeInformation.OSDescription}");
        Log.Info($"Architecture: {RuntimeInformation.ProcessArchitecture}");

        // 检查关键文件
        var nativeDll = Path.Combine(AppContext.BaseDirectory, "Photino.Native.dll");
        var webView2Loader = Path.Combine(AppContext.BaseDirectory, "WebView2Loader.dll");
        Log.Info($"Photino.Native.dll 存在: {File.Exists(nativeDll)}");
        Log.Info($"WebView2Loader.dll 存在: {File.Exists(webView2Loader)}");

        try
        {
            Log.Info("创建 PhotinoBlazorAppBuilder...");
            var appBuilder = PhotinoBlazorAppBuilder.CreateDefault(args);
            Log.Info("PhotinoBlazorAppBuilder 创建成功");

            Log.Info("注册服务...");
            appBuilder.Services
                .AddSingleton<LocalizationService>()
                .AddSingleton<AppState>()
                .AddSingleton<ILocalDataStore>(sp =>
                {
                    var dbPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "GitGet", "gitget.db");
                    Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
                    var connection = new SqliteConnection($"Data Source={dbPath}");
                    var store = new LocalDataStore(connection);
                    store.InitializeAsync().GetAwaiter().GetResult();
                    return store;
                })
                .AddSingleton<ISecureDataStore>(sp =>
                {
                    var path = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "GitGet", "tokens");
                    return new SecureDataStore(path);
                })
                .AddSingleton<ICacheService>(sp => new CacheService(sp.GetRequiredService<ILocalDataStore>()))
                .AddSingleton<IAssetMatcherService, AssetMatcherService>()
                .AddSingleton<INodeScriptRunner>(sp =>
                {
                    var scriptPath = Path.Combine(AppContext.BaseDirectory, "github-api.js");
                    Log.Info($"NodeScriptRunner 脚本路径: {scriptPath} (存在: {File.Exists(scriptPath)})");
                    return new NodeScriptRunner(scriptPath);
                })
                .AddSingleton<IGitHubApiClient>(sp => new GitHubApiClient(sp.GetRequiredService<INodeScriptRunner>()))
                .AddSingleton<ITrendingService, TrendingService>()
                .AddSingleton<IGitGetSettings, GitGetSettings>()
                .AddSingleton<IDownloadService>(sp =>
                {
                    var client = new HttpClient
                    {
                        Timeout = TimeSpan.FromMinutes(10)
                    };
                    client.DefaultRequestHeaders.Add("User-Agent", "GitGet-WebUI/1.0");
                    return new DownloadService(
                        client,
                        sp.GetRequiredService<ILocalDataStore>(),
                        sp.GetRequiredService<IGitGetSettings>());
                });
            Log.Info("服务注册完成");

            appBuilder.RootComponents.Add<App>("app");
            Log.Info("RootComponent 添加完成");

            Log.Info("调用 appBuilder.Build()...");
            var app = appBuilder.Build();
            Log.Info("appBuilder.Build() 完成");

            // Load persisted settings and apply defaults
            var settings = app.Services.GetRequiredService<IGitGetSettings>();
            settings.LoadAsync().GetAwaiter().GetResult();
            Log.Info($"设置已加载 — 下载路径: {settings.DownloadPath}, 并发数: {settings.MaxConcurrentDownloads}, 主题: {settings.Theme}, 语言: {settings.Language}");

            var appState = app.Services.GetRequiredService<AppState>();
            appState.IsDarkMode = settings.Theme == "dark";

            var localization = app.Services.GetRequiredService<LocalizationService>();
            localization.SetLanguage(settings.Language);

            if (app.MainWindow == null)
            {
                Log.Fatal("MainWindow 为 null，窗口创建失败");
                Log.Fatal("可能原因: Photino.Native 初始化失败或 WebView2 未安装");
                WaitExit();
                return;
            }
            Log.Info("MainWindow 已创建");

            app.MainWindow.SetTitle("GitGet");
            app.MainWindow.SetWidth(1280);
            app.MainWindow.SetHeight(800);

            // Handle external link clicks from WebView
            app.MainWindow.RegisterWebMessageReceivedHandler((sender, message) =>
            {
                try
                {
                    using var doc = JsonDocument.Parse(message);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("type", out var type) && type.GetString() == "openExternal"
                        && root.TryGetProperty("url", out var urlProp))
                    {
                        var url = urlProp.GetString();
                        if (!string.IsNullOrEmpty(url))
                        {
                            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                        }
                    }
                }
                catch { }
            });
            try
            {
                app.MainWindow.Center();
                Log.Info("窗口居中完成");
            }
            catch (Exception ex)
            {
                Log.Info($"Center() 警告: {ex.Message}");
            }

            Log.Info("调用 app.Run()...");
            app.Run();
            Log.Info("app.Run() 已返回，程序正常退出");
        }
        catch (Exception ex)
        {
            Log.Fatal($"启动异常: {ex.GetType().Name}");
            Log.Fatal($"错误信息: {ex.Message}");
            if (ex.InnerException != null)
            {
                Log.Fatal($"内部异常: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }
            Log.Fatal("堆栈跟踪:");
            Log.Fatal(ex.StackTrace ?? "(无)");
            Log.Fatal("");
            Log.Fatal("常见原因及解决方案:");
            Log.Fatal("1. 未安装 Microsoft Edge WebView2 Runtime");
            Log.Fatal("   下载: https://developer.microsoft.com/microsoft-edge/webview2/");
            Log.Fatal("2. 缺少 Photino.Native.dll 或 WebView2Loader.dll");
            Log.Fatal("   尝试运行: dotnet restore --force");
            Log.Fatal("3. 杀毒软件拦截了窗口创建");
            Log.Fatal("4. 系统缺少 VC++ 2015-2022 Redistributable");
            Log.Fatal("");
            Log.Fatal($"日志文件位置: {Log.LogPath}");
            WaitExit();
        }
    }

    static void WaitExit()
    {
        Console.WriteLine("\n按任意键退出...");
        try { Console.ReadKey(true); } catch { }
    }
}
