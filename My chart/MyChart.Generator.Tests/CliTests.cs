using System.Text;
using System.Text.Json;
using MyChart.Generator.Cli;
using Xunit;

namespace MyChart.Generator.Tests;

/// <summary>PG3.08 VERIFY — every verb with --json returns valid JSON; exit codes match.</summary>
public class CliTests
{
    private static (int exit, string stdout, string stderr) Invoke(params string[] args)
    {
        var so = new StringWriter();
        var se = new StringWriter();
        var exit = CliRunner.Run(args, so, se);
        return (exit, so.ToString(), se.ToString());
    }

    [Fact]
    public void Help_Returns0()
    {
        var (exit, _, stderr) = Invoke("--help");
        Assert.Equal(0, exit);
        Assert.Contains("tool add", stderr);
    }

    [Fact]
    public void ToolAdd_Json_ValidAndDuplicateE002()
    {
        var user = Path.Combine(Path.GetTempPath(), "cli_user_" + Guid.NewGuid().ToString("N"));
        var repo = Path.Combine(Path.GetTempPath(), "cli_repo_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(user);
        Directory.CreateDirectory(repo);
        Directory.CreateDirectory(Path.Combine(repo, "MyChart.Plugins", "Components"));
        try
        {
            var args1 = new[]
            {
                "tool", "add", "--id", "TrendLine", "--name", "Trend Line",
                "--target", "user", "--json",
                "--repo", repo, "--user-root", user
            };
            var (exit1, out1, _) = Invoke(args1);
            Assert.Equal(0, exit1);
            using (var doc = JsonDocument.Parse(out1.Trim()))
            {
                Assert.True(doc.RootElement.GetProperty("ok").GetBoolean());
                Assert.Equal("tool add", doc.RootElement.GetProperty("operation").GetString());
            }

            var (exit2, out2, _) = Invoke(args1);
            Assert.NotEqual(0, exit2);
            using (var doc = JsonDocument.Parse(out2.Trim()))
            {
                Assert.False(doc.RootElement.GetProperty("ok").GetBoolean());
                var errors = doc.RootElement.GetProperty("errors");
                Assert.True(errors.GetArrayLength() >= 1);
                Assert.Equal("E002", errors[0].GetProperty("code").GetString());
            }
        }
        finally
        {
            Directory.Delete(user, true);
            Directory.Delete(repo, true);
        }
    }

    [Fact]
    public void ToolRemove_WithoutYes_IsDryRun()
    {
        var user = Path.Combine(Path.GetTempPath(), "cli_rm_" + Guid.NewGuid().ToString("N"));
        var repo = Path.Combine(Path.GetTempPath(), "cli_rmr_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(user);
        Directory.CreateDirectory(repo);
        Directory.CreateDirectory(Path.Combine(repo, "MyChart.Plugins", "Components"));
        try
        {
            Invoke("tool", "add", "--id", "TrendLine", "--target", "user", "--repo", repo, "--user-root", user);
            Assert.True(Directory.Exists(Path.Combine(user, "TrendLine")));

            var (exit, stdout, _) = Invoke(
                "tool", "remove", "--id", "TrendLine", "--target", "user",
                "--json", "--repo", repo, "--user-root", user);
            Assert.Equal(0, exit);
            using var doc = JsonDocument.Parse(stdout.Trim());
            // without --yes, dry-run: folder still exists
            Assert.True(Directory.Exists(Path.Combine(user, "TrendLine")));
        }
        finally
        {
            Directory.Delete(user, true);
            Directory.Delete(repo, true);
        }
    }
}
