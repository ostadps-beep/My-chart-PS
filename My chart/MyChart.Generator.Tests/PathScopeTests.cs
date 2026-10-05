using MyChart.Core.Plugins.Manifest;
using MyChart.Generator.Engine.Operations;
using Xunit;

namespace MyChart.Generator.Tests;

/// <summary>PG3.01 PathScope VERIFY.</summary>
public class PathScopeTests
{
    private static PathScope CreateScope(out string repo, out string user)
    {
        repo = Path.Combine(Path.GetTempPath(), "mychart_repo_" + Guid.NewGuid().ToString("N"));
        user = Path.Combine(Path.GetTempPath(), "mychart_user_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(repo);
        Directory.CreateDirectory(user);
        Directory.CreateDirectory(Path.Combine(repo, "MyChart.Plugins"));
        return new PathScope(repo, user);
    }

    [Fact]
    public void AllowsPathsUnderRepoAndUser()
    {
        var scope = CreateScope(out var repo, out var user);
        try
        {
            Assert.Null(scope.Validate(Path.Combine(repo, "MyChart.Plugins", "x.json")));
            Assert.Null(scope.Validate(Path.Combine(user, "TrendLine", "Manifest.json")));
            Assert.Null(scope.Validate(scope.PluginCatalogPath));
            Assert.Null(scope.Validate(scope.IconCatalogPath));
        }
        finally
        {
            Directory.Delete(repo, true);
            Directory.Delete(user, true);
        }
    }

    [Fact]
    public void RejectsParentSegmentAndOutsideRoots()
    {
        var scope = CreateScope(out var repo, out var user);
        try
        {
            Assert.Equal(ErrorCodes.PathOutOfScope, scope.Validate(Path.Combine(repo, "..", "outside.txt")));
            Assert.Equal(ErrorCodes.PathOutOfScope, scope.Validate(Path.Combine(Path.GetTempPath(), "other_file.txt")));
            Assert.Equal(ErrorCodes.PathOutOfScope, scope.Validate(""));
        }
        finally
        {
            Directory.Delete(repo, true);
            Directory.Delete(user, true);
        }
    }
}
