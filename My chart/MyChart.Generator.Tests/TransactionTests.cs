using MyChart.Core.Plugins.Manifest;
using MyChart.Generator.Engine.Operations;
using Xunit;

namespace MyChart.Generator.Tests;

/// <summary>PG3.04 VERIFY — dry-run, rollback, E014, scope.</summary>
public class TransactionTests
{
    private static (PathScope scope, string repo, string user) TempRoots()
    {
        var repo = Path.Combine(Path.GetTempPath(), "gen_repo_" + Guid.NewGuid().ToString("N"));
        var user = Path.Combine(Path.GetTempPath(), "gen_user_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(repo);
        Directory.CreateDirectory(user);
        Directory.CreateDirectory(Path.Combine(repo, "MyChart.Plugins"));
        return (new PathScope(repo, user), repo, user);
    }

    [Fact]
    public void DryRun_WritesNothing()
    {
        var (scope, repo, user) = TempRoots();
        try
        {
            var path = Path.Combine(user, "TrendLine", "Manifest.json");
            var plan = new FilePlan();
            plan.AddCreate(path, "{ }\n");

            var tx = new Transaction(scope, git: null);
            var result = tx.Execute(plan, dryRun: true, requireGitForRepoPaths: false);

            Assert.True(result.Ok);
            Assert.True(result.DryRun);
            Assert.False(File.Exists(path));
            Assert.Empty(Directory.EnumerateFiles(user, "*", SearchOption.AllDirectories));
        }
        finally
        {
            Directory.Delete(repo, true);
            Directory.Delete(user, true);
        }
    }

    [Fact]
    public void Commit_CreatesFiles()
    {
        var (scope, repo, user) = TempRoots();
        try
        {
            var dir = Path.Combine(user, "TrendLine");
            var path = Path.Combine(dir, "Manifest.json");
            var plan = new FilePlan();
            plan.AddCreate(path, "{\"ok\":true}\n");

            var tx = new Transaction(scope);
            var result = tx.Execute(plan, dryRun: false, requireGitForRepoPaths: false);

            Assert.True(result.Ok, result.ErrorMessage);
            Assert.True(File.Exists(path));
            Assert.Equal("{\"ok\":true}\n", File.ReadAllText(path));
            Assert.False(File.Exists(path + ".tmp"));
        }
        finally
        {
            Directory.Delete(repo, true);
            Directory.Delete(user, true);
        }
    }

    [Fact]
    public void FailureInjection_AtThirdFile_RollsBack()
    {
        var (scope, repo, user) = TempRoots();
        try
        {
            var plan = new FilePlan();
            for (int i = 1; i <= 6; i++)
            {
                plan.AddCreate(
                    Path.Combine(user, "Comp", $"f{i}.txt"),
                    $"content-{i}\n");
            }

            var snapshotBefore = Snapshot(user);
            var tx = new Transaction(scope);
            var result = tx.Execute(
                plan,
                dryRun: false,
                requireGitForRepoPaths: false,
                onBeforeWrite: (index, entry) =>
                {
                    if (index == 2) // third file (0-based)
                        throw new InvalidOperationException("injected failure at file 3 of 6");
                });

            Assert.False(result.Ok);
            Assert.Equal(3, result.ExitCode);
            Assert.Equal(snapshotBefore, Snapshot(user));
            // no leftover temps
            Assert.Empty(Directory.EnumerateFiles(user, "*.tmp", SearchOption.AllDirectories));
        }
        finally
        {
            Directory.Delete(repo, true);
            Directory.Delete(user, true);
        }
    }

    [Fact]
    public void CreateExistingUserFile_ReturnsE014()
    {
        var (scope, repo, user) = TempRoots();
        try
        {
            var path = Path.Combine(user, "Existing", "Definition.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, "old\n");

            var plan = new FilePlan();
            plan.AddCreate(path, "new\n");

            var tx = new Transaction(scope);
            var result = tx.Execute(plan, requireGitForRepoPaths: false);

            Assert.False(result.Ok);
            Assert.Equal(ErrorCodes.UserFileWouldBeOverwritten, result.ErrorCode);
            Assert.Equal("old\n", File.ReadAllText(path));
        }
        finally
        {
            Directory.Delete(repo, true);
            Directory.Delete(user, true);
        }
    }

    [Fact]
    public void PathOutOfScope_ReturnsE013()
    {
        var (scope, repo, user) = TempRoots();
        try
        {
            var plan = new FilePlan();
            plan.AddCreate(Path.Combine(Path.GetTempPath(), "outside_gen.txt"), "x\n");

            var tx = new Transaction(scope);
            var result = tx.Execute(plan, requireGitForRepoPaths: false);

            Assert.False(result.Ok);
            Assert.Equal(ErrorCodes.PathOutOfScope, result.ErrorCode);
            Assert.Equal(4, result.ExitCode);
        }
        finally
        {
            Directory.Delete(repo, true);
            Directory.Delete(user, true);
        }
    }

    private static string Snapshot(string root)
    {
        if (!Directory.Exists(root)) return "";
        var files = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .OrderBy(p => p, StringComparer.Ordinal)
            .Select(p => p[root.Length..] + "=" + ManifestIO.Sha256Normalized(File.ReadAllText(p)));
        return string.Join("|", files);
    }
}
