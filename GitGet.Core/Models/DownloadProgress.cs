namespace GitGet.Core.Models;

public record DownloadProgress(
    string TaskId,
    string FileName,
    long TotalBytes,
    long ReceivedBytes,
    DownloadTask.TaskStatus Status,
    double SpeedBytesPerSecond = 0,
    TimeSpan EstimatedTimeRemaining = default
)
{
    public double Percent => TotalBytes > 0
        ? Math.Round((double)ReceivedBytes / TotalBytes * 100, 1)
        : 0;
}