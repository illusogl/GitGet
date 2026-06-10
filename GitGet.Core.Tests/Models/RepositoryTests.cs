using FluentAssertions;
using GitGet.Core.Models;

namespace GitGet.Core.Tests.Models;

public class RepositoryTests
{
    [Fact]
    public void Repository_DefaultValues_AreSet()
    {
        var repo = new Repository();

        repo.Id.Should().Be(0);
        repo.FullName.Should().BeEmpty();
        repo.Stars.Should().Be(0);
        repo.Topics.Should().NotBeNull().And.BeEmpty();
        repo.DefaultBranch.Should().Be("main");
    }

    [Fact]
    public void Repository_CanSetAndGetProperties()
    {
        var repo = new Repository
        {
            Id = 12345,
            FullName = "test-owner/test-repo",
            Name = "test-repo",
            Owner = "test-owner",
            Description = "A test repository",
            Language = "C#",
            Stars = 1000,
            Forks = 500,
            Topics = new List<string> { "dotnet", "csharp" }
        };

        repo.Id.Should().Be(12345);
        repo.FullName.Should().Be("test-owner/test-repo");
        repo.Stars.Should().Be(1000);
        repo.Topics.Should().Contain("dotnet");
    }
}