namespace GitGet.Core.Models;

public class Release
{
    public long Id { get; set; }
    public string TagName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty; // Release notes (Markdown)
    public bool Prerelease { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime PublishedAt { get; set; }
    public string HtmlUrl { get; set; } = string.Empty;
    public List<ReleaseAsset> Assets { get; set; } = new();
}