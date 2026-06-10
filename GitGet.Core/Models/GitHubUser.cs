namespace GitGet.Core.Models;

public class GitHubUser
{
    public long Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public string HtmlUrl { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public int PublicRepos { get; set; }
    public int Followers { get; set; }
    public int Following { get; set; }
    public DateTime CreatedAt { get; set; }
}