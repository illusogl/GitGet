using FluentAssertions;
using GitGet.Core.Data;
using GitGet.Core.Interfaces;
using GitGet.Core.Models;
using GitGet.Core.Services;
using Microsoft.Data.Sqlite;

namespace GitGet.Core.Tests.Services;

public class DownloadServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ILocalDataStore _dataStore;
    private readonly IDownloadService _downloadService;
    private readonly string _tempDir;

    public DownloadServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _dataStore = new LocalDataStore(_connection);
        _tempDir = Path.Combine(Path.GetTempPath(), "GitGet_Test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    private IDownloadService CreateDownloadService(TaskCompletionSource<HttpResponseMessage> tcs)
    {
        var handler = new DelayedHandler(tcs);
        var httpClient = new HttpClient(handler);
        return new DownloadService(httpClient, _dataStore);
    }

    [Fact]
    public async Task StartDownloadAsync_CreatesTask()
    {
        await _dataStore.InitializeAsync();
        var tcs = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);

        // Complete immediately with empty body so test doesn't hang
        tcs.SetResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(Array.Empty<byte>())
        });

        var svc = CreateDownloadService(tcs);

        var task = await svc.StartDownloadAsync(
            "https://example.com/test.exe",
            "test.exe", "test/repo", _tempDir);

        task.Should().NotBeNull();
        task.FileName.Should().Be("test.exe");
        task.RepoFullName.Should().Be("test/repo");

        // Status: Queued (initial) or later (background runs quickly)
        // Just verify it was created with valid data
    }

    [Fact]
    public async Task GetTasksAsync_ReturnsAllTasks()
    {
        await _dataStore.InitializeAsync();
        var tcs = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        tcs.SetResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(Array.Empty<byte>())
        });
        var svc = CreateDownloadService(tcs);

        await svc.StartDownloadAsync("https://example.com/a.exe", "a.exe", "test/a", _tempDir);
        await svc.StartDownloadAsync("https://example.com/b.exe", "b.exe", "test/b", _tempDir);

        var tasks = await svc.GetTasksAsync();
        tasks.Count.Should().Be(2);
    }

    [Fact]
    public async Task GetTaskAsync_ReturnsNull_WhenNotFound()
    {
        await _dataStore.InitializeAsync();
        var svc = CreateDownloadService(new TaskCompletionSource<HttpResponseMessage>());

        var task = await svc.GetTaskAsync("nonexistent");
        task.Should().BeNull();
    }

    [Fact]
    public async Task CancelDownloadAsync_UpdatesStatus()
    {
        await _dataStore.InitializeAsync();

        // Use a TCS that never completes to keep download "in progress"
        var tcs = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var svc = CreateDownloadService(tcs);

        var task = await svc.StartDownloadAsync(
            "https://example.com/test.exe", "test.exe", "test/repo", _tempDir);

        // Give background task a moment to start waiting on semaphore
        await Task.Delay(100);

        await svc.CancelDownloadAsync(task.TaskId);

        // Now complete the TCS so background task can exit cleanly
        tcs.TrySetCanceled();

        // Give background task time to process cancellation
        await Task.Delay(100);

        var cancelled = await svc.GetTaskAsync(task.TaskId);
        cancelled.Should().NotBeNull();
        cancelled!.Status.Should().Be("Cancelled");
    }

    [Fact]
    public void DownloadTask_ProgressPercent_CalculatesCorrectly()
    {
        var task = new DownloadTask { TotalBytes = 1000, ReceivedBytes = 500 };
        task.ProgressPercent.Should().Be(50.0);
    }

    [Fact]
    public void DownloadTask_ProgressPercent_ZeroWhenUnknown()
    {
        var task = new DownloadTask { TotalBytes = 0, ReceivedBytes = 500 };
        task.ProgressPercent.Should().Be(0);
    }

    [Fact]
    public void DownloadProgress_Percent_CalculatesCorrectly()
    {
        var progress = new DownloadProgress(
            "task1", "test.exe", 10000, 7500,
            DownloadTask.TaskStatus.Downloading);
        progress.Percent.Should().Be(75.0);
    }

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();

        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    /// <summary>
    /// HttpMessageHandler controlled by TaskCompletionSource for test synchronization.
    /// </summary>
    private class DelayedHandler : HttpMessageHandler
    {
        private readonly TaskCompletionSource<HttpResponseMessage> _tcs;

        public DelayedHandler(TaskCompletionSource<HttpResponseMessage> tcs)
        {
            _tcs = tcs;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
        {
            ct.Register(() => _tcs.TrySetCanceled(ct));
            return _tcs.Task;
        }
    }
}