using MyChart.Core.Plugins.Manifest;
using MyChart.Generator.Engine.Operations;
using Xunit;

namespace MyChart.Generator.Tests;

/// <summary>PG3.07 VERIFY — AT1 scope, AT2 add then remove, E002, E023.</summary>
public class ToolOperationsTests
{
    private static (PathScope scope, string repo, string user, ToolOperations tools) Setup()
    {
        var repo = Path.Combine(Path.GetTempPath(), "ops_repo_" + Guid.NewGuid().ToString("N"));
        var user = Path.Combine(Path.GetTempPath(), "ops_user_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(repo);
        Directory.CreateDirectory(user);
        var plugins = Path.Combine(repo, "MyChart.Plugins");
        Directory.CreateDirectory(plugins);
        Directory.CreateDirectory(Path.Combine(plugins, "Components"));
        File.WriteAllText(Path.Combine(plugins, "PluginCatalog.g.cs"), "// seed\n");
        File.WriteAllText(Path.Combine(plugins, "IconCatalog.g.cs"), "// seed\n");

        var scope = new PathScope(repo, user);
        var tools = new ToolOperations(scope);
        return (scope, repo, user, tools);
    }

    [Fact]
    public void AddThenRemove_LeavesTreeByteIdentical()
    {
        // UserRoot ops skip git (PG3.04); verifies add then remove restores empty component tree.
        var (_, repo, user, tools) = Setup();
        try
        {
            var before = Snapshot(user);

            var add = tools.Add(new ToolAddRequest
            {
                ComponentId = "TrendLine",
                Name = "Trend Line",
                Kind = ToolKind.DataTool,
                Target = ComponentTarget.User
            });
            Assert.True(add.Ok, add.ErrorCode + " " + add.ErrorMessage);

            var folder = Path.Combine(user, "TrendLine");
            Assert.True(Directory.Exists(folder));
            Assert.True(File.Exists(Path.Combine(folder, "Manifest.json")));
            Assert.True(File.Exists(Path.Combine(folder, "Definition.json")));

            var remove = tools.Remove("TrendLine", ComponentTarget.User);
            Assert.True(remove.Ok, remove.ErrorCode + " " + remove.ErrorMessage);
            Assert.False(Directory.Exists(folder));

            Assert.Equal(before, Snapshot(user));
        }
        finally
        {
            Directory.Delete(repo, true);
            Directory.Delete(user, true);
        }
    }

    [Fact]
    public void AddTwice_SecondFailsE002()
    {
        var (_, repo, user, tools) = Setup();
        try
        {
            var req = new ToolAddRequest
            {
                ComponentId = "TrendLine",
                Name = "Trend Line",
                Target = ComponentTarget.User
            };
            Assert.True(tools.Add(req).Ok);
            var second = tools.Add(req);
            Assert.False(second.Ok);
            Assert.Equal(ErrorCodes.DuplicateId, second.ErrorCode);
        }
        finally
        {
            Directory.Delete(repo, true);
            Directory.Delete(user, true);
        }
    }

    [Fact]
    public void CodeToolInUserRoot_E023()
    {
        var (_, repo, user, tools) = Setup();
        try
        {
            var result = tools.Add(new ToolAddRequest
            {
                ComponentId = "MyCode",
                Name = "My Code",
                Kind = ToolKind.CodeTool,
                Target = ComponentTarget.User
            });
            Assert.False(result.Ok);
            Assert.Equal(ErrorCodes.CodeToolInUserRoot, result.ErrorCode);
        }
        finally
        {
            Directory.Delete(repo, true);
            Directory.Delete(user, true);
        }
    }

    [Fact]
    public void DryRun_Add_WritesNothing()
    {
        var (_, repo, user, tools) = Setup();
        try
        {
            var result = tools.Add(new ToolAddRequest
            {
                ComponentId = "TrendLine",
                Name = "Trend Line",
                Target = ComponentTarget.User,
                DryRun = true
            });
            Assert.True(result.Ok);
            Assert.True(result.DryRun);
            Assert.False(Directory.Exists(Path.Combine(user, "TrendLine")));
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
        return string.Join("|",
            Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                .OrderBy(p => p, StringComparer.Ordinal)
                .Select(p => p[root.Length..] + "=" + ManifestIO.Sha256Normalized(File.ReadAllText(p))));
    }
}
