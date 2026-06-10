namespace GitGet.Core.Models;

public class Repository
{
    public long Id { get; set; }
    public string FullName { get; set; } = string.Empty; // owner/repo
    public string Name { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public int Stars { get; set; }
    public int Forks { get; set; }
    public int OpenIssues { get; set; }
    public string License { get; set; } = string.Empty;
    public string Homepage { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> Topics { get; set; } = new();
    public string DefaultBranch { get; set; } = "main";
}