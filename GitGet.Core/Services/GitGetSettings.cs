using System.Text.Json;
using GitGet.Core.Interfaces;

namespace GitGet.Core.Services;

public class GitGetSettings : IGitGetSettings
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public string DownloadPath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
    public int MaxConcurrentDownloads { get; set; } = 3;
    public string Theme { get; set; } = "light";
    public string Language { get; set; } = "zh";

    public GitGetSettings()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GitGet", "settings.json"))
    {
    }

    public GitGetSettings(string filePath)
    {
        _filePath = filePath;
    }

    public async Task LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_filePath))
            return;

        var json = await File.ReadAllTextAsync(_filePath, ct);
        var data = JsonSerializer.Deserialize<SettingsData>(json);
        if (data == null) return;

        if (!string.IsNullOrWhiteSpace(data.DownloadPath) && Directory.Exists(data.DownloadPath))
            DownloadPath = data.DownloadPath;
        if (data.MaxConcurrentDownloads is >= 1 and <= 10)
            MaxConcurrentDownloads = data.MaxConcurrentDownloads;
        if (data.Theme is "light" or "dark")
            Theme = data.Theme;
        if (data.Language is "zh" or "en")
            Language = data.Language;
    }

    public async Task SaveAsync(CancellationToken ct = default)
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var data = new SettingsData
        {
            DownloadPath = DownloadPath,
            MaxConcurrentDownloads = MaxConcurrentDownloads,
            Theme = Theme,
            Language = Language,
        };
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        await File.WriteAllTextAsync(_filePath, json, ct);
    }

    private class SettingsData
    {
        public string DownloadPath { get; set; } = "";
        public int MaxConcurrentDownloads { get; set; } = 3;
        public string Theme { get; set; } = "light";
        public string Language { get; set; } = "zh";
    }
}