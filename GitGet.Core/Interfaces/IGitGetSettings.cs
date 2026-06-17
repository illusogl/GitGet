namespace GitGet.Core.Interfaces;

public interface IGitGetSettings
{
    string DownloadPath { get; set; }
    int MaxConcurrentDownloads { get; set; }

    Task LoadAsync(CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
}