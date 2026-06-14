using GitGet.Core.Models;

namespace GitGet.Core.Interfaces;

public interface IDownloadService
{
    Task<DownloadTask> StartDownloadAsync(string url, string fileName, string repoFullName,
        string destinationDir, IProgress<DownloadProgress>? progress = null,
        CancellationToken ct = default);

    Task PauseDownloadAsync(string taskId, CancellationToken ct = default);
    Task ResumeDownloadAsync(string taskId, CancellationToken ct = default);
    Task CancelDownloadAsync(string taskId, CancellationToken ct = default);
    Task<List<DownloadTask>> GetTasksAsync(CancellationToken ct = default);
    Task<DownloadTask?> GetTaskAsync(string taskId, CancellationToken ct = default);
}