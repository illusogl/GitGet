namespace GitGet.Core.Models;

public class DownloadTask
{
    public enum TaskStatus
    {
        Queued,
        Downloading,
        Paused,
        Completed,
        Failed,
        Cancelled
    }

    public string TaskId { get; set; } = Guid.NewGuid().ToString("N");
    public string RepoFullName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public string? LocalPath { get; set; }
    public long TotalBytes { get; set; }
    public long ReceivedBytes { get; set; }
    public string Status { get; set; } = TaskStatus.Queued.ToString();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }

    public double ProgressPercent =>
        TotalBytes > 0 ? Math.Round((double)ReceivedBytes / TotalBytes * 100, 1) : 0;
}