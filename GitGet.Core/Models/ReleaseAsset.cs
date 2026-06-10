namespace GitGet.Core.Models;

public class ReleaseAsset
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public long Size { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int DownloadCount { get; set; }
}