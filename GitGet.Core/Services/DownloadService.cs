using System.Collections.Concurrent;
using System.Diagnostics;
using GitGet.Core.Interfaces;
using GitGet.Core.Models;

namespace GitGet.Core.Services;

public class DownloadService : IDownloadService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalDataStore _dataStore;
    private readonly IGitGetSettings _settings;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeDownloads = new();
    private readonly SemaphoreSlim _concurrencyLimit;

    public DownloadService(HttpClient httpClient, ILocalDataStore dataStore, IGitGetSettings settings)
    {
        _httpClient = httpClient;
        _dataStore = dataStore;
        _settings = settings;
        _concurrencyLimit = new SemaphoreSlim(settings.MaxConcurrentDownloads);
    }

    public string GetDownloadPath() => _settings.DownloadPath;
    public int GetMaxConcurrentDownloads() => _settings.MaxConcurrentDownloads;

    public async Task<DownloadTask> StartDownloadAsync(
        string url, string fileName, string repoFullName,
        string destinationDir, IProgress<DownloadProgress>? progress = null,
        CancellationToken ct = default)
    {
        var task = new DownloadTask
        {
            DownloadUrl = url,
            FileName = fileName,
            RepoFullName = repoFullName,
            LocalPath = Path.Combine(destinationDir, fileName),
            Status = DownloadTask.TaskStatus.Queued.ToString(),
            CreatedAt = DateTime.UtcNow
        };

        // Generate unique file name to avoid conflicts
        if (File.Exists(task.LocalPath))
        {
            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            var ext = Path.GetExtension(fileName);
            task.LocalPath = Path.Combine(destinationDir,
                $"{nameWithoutExt}_{DateTime.UtcNow:yyyyMMddHHmmss}_{task.TaskId[..6]}{ext}");
        }

        // Save to database and register task ID immediately
        await SaveTaskAsync(task, ct);

        // Start download in background
        var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _activeDownloads[task.TaskId] = cts;

        _ = Task.Run(() => ExecuteDownloadAsync(task, progress, cts.Token), ct);

        return task;
    }

    private async Task ExecuteDownloadAsync(
        DownloadTask task,
        IProgress<DownloadProgress>? progress,
        CancellationToken ct)
    {
        try
        {
            await _concurrencyLimit.WaitAsync(ct);

            task.Status = DownloadTask.TaskStatus.Downloading.ToString();
            await SaveTaskAsync(task, ct);

            using var request = new HttpRequestMessage(HttpMethod.Get, task.DownloadUrl);

            // Support resume if previously started
            if (task.ReceivedBytes > 0)
            {
                request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(
                    task.ReceivedBytes, null);
            }

            using var response = await _httpClient.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead, ct);

            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? 0;
            if (task.ReceivedBytes > 0 && totalBytes > 0)
                totalBytes += task.ReceivedBytes;
            else if (totalBytes == 0)
                totalBytes = task.TotalBytes;

            task.TotalBytes = totalBytes;

            var fileMode = task.ReceivedBytes > 0 ? FileMode.Append : FileMode.Create;
            await using var fileStream = new FileStream(task.LocalPath!, fileMode,
                FileAccess.Write, FileShare.None, 8192, useAsync: true);
            await using var stream = await response.Content.ReadAsStreamAsync(ct);

            var buffer = new byte[8192];
            int bytesRead;
            var stopwatch = Stopwatch.StartNew();
            long lastReportedBytes = task.ReceivedBytes;
            long bytesSinceLastReport = 0;

            while ((bytesRead = await stream.ReadAsync(buffer, ct)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                task.ReceivedBytes += bytesRead;
                bytesSinceLastReport += bytesRead;

                // Report progress every 256KB or every 500ms
                if (bytesSinceLastReport >= 262144 ||
                    stopwatch.ElapsedMilliseconds >= 500)
                {
                    var elapsed = stopwatch.Elapsed.TotalSeconds;
                    var speed = elapsed > 0
                        ? (task.ReceivedBytes - lastReportedBytes) / elapsed
                        : 0;
                    var remaining = speed > 0
                        ? TimeSpan.FromSeconds((totalBytes - task.ReceivedBytes) / speed)
                        : TimeSpan.Zero;

                    progress?.Report(new DownloadProgress(
                        task.TaskId, task.FileName, totalBytes, task.ReceivedBytes,
                        DownloadTask.TaskStatus.Downloading, speed, remaining));

                    await SaveTaskAsync(task, ct);

                    stopwatch.Restart();
                    lastReportedBytes = task.ReceivedBytes;
                    bytesSinceLastReport = 0;
                }
            }

            task.Status = DownloadTask.TaskStatus.Completed.ToString();
            task.CompletedAt = DateTime.UtcNow;

            progress?.Report(new DownloadProgress(
                task.TaskId, task.FileName, totalBytes, task.ReceivedBytes,
                DownloadTask.TaskStatus.Completed, 0, TimeSpan.Zero));

            await SaveTaskAsync(task, ct);
        }
        catch (OperationCanceledException)
        {
            // Cancelled - keep current status (Paused or Cancelled set by caller)
            if (task.Status != DownloadTask.TaskStatus.Paused.ToString() &&
                task.Status != DownloadTask.TaskStatus.Cancelled.ToString())
            {
                task.Status = DownloadTask.TaskStatus.Cancelled.ToString();
            }
            await SaveTaskAsync(task, CancellationToken.None);
        }
        catch (Exception ex)
        {
            task.Status = DownloadTask.TaskStatus.Failed.ToString();
            task.ErrorMessage = ex.Message;
            task.RetryCount++;
            await SaveTaskAsync(task, CancellationToken.None);
        }
        finally
        {
            _activeDownloads.TryRemove(task.TaskId, out _);
            _concurrencyLimit.Release();
        }
    }

    public async Task PauseDownloadAsync(string taskId, CancellationToken ct = default)
    {
        var task = await GetTaskAsync(taskId, ct);
        if (task == null) return;

        if (_activeDownloads.TryGetValue(taskId, out var cts))
        {
            task.Status = DownloadTask.TaskStatus.Paused.ToString();
            await SaveTaskAsync(task, ct);
            cts.Cancel();
        }
    }

    public async Task ResumeDownloadAsync(string taskId, CancellationToken ct = default)
    {
        var task = await GetTaskAsync(taskId, ct);
        if (task == null) return;
        if (task.Status != DownloadTask.TaskStatus.Paused.ToString()) return;

        var destDir = Path.GetDirectoryName(task.LocalPath)!;
        var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _activeDownloads[taskId] = cts;

        _ = Task.Run(() => ExecuteDownloadAsync(task, null, cts.Token), ct);
    }

    public async Task CancelDownloadAsync(string taskId, CancellationToken ct = default)
    {
        var task = await GetTaskAsync(taskId, ct);
        if (task == null) return;

        if (_activeDownloads.TryGetValue(taskId, out var cts))
        {
            task.Status = DownloadTask.TaskStatus.Cancelled.ToString();
            await SaveTaskAsync(task, ct);
            cts.Cancel();
        }
        else
        {
            task.Status = DownloadTask.TaskStatus.Cancelled.ToString();
            await SaveTaskAsync(task, ct);
        }
    }

    public async Task<List<DownloadTask>> GetTasksAsync(CancellationToken ct = default)
    {
        var taskIds = await _dataStore.GetAsync<List<string>>("__download_tasks__", ct);
        if (taskIds == null) return new();

        var tasks = new List<DownloadTask>();
        foreach (var id in taskIds)
        {
            var task = await _dataStore.GetAsync<DownloadTask>($"download_{id}", ct);
            if (task != null) tasks.Add(task);
        }
        return tasks;
    }

    public async Task<DownloadTask?> GetTaskAsync(string taskId, CancellationToken ct = default)
    {
        return await _dataStore.GetAsync<DownloadTask>($"download_{taskId}", ct);
    }

    private async Task SaveTaskAsync(DownloadTask task, CancellationToken ct)
    {
        await _dataStore.SaveAsync($"download_{task.TaskId}", task, ct);

        // Maintain list of all download task IDs (add immediately on first save)
        var taskIds = await _dataStore.GetAsync<List<string>>("__download_tasks__", ct) ?? new();
        if (!taskIds.Contains(task.TaskId))
        {
            taskIds.Add(task.TaskId);
            await _dataStore.SaveAsync("__download_tasks__", taskIds, ct);
        }
    }
}