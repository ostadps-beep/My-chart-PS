using MyChart.Core.Plugins.Manifest;
using MyChart.Generator.Engine.Operations;
using Xunit;

namespace MyChart.Generator.Tests;

/// <summary>PG3.06 VERIFY — missing, cycle, has-dependents, duplicates, stale catalog.</summary>
public class DependencyValidateTests
{
    [Fact]
    public void MissingTarget_E010()
    {
        var g = new DependencyGraph();
        g.AddNode("ToolA");
        g.AddEdge("ToolA", "Icon.Missing");
        var missing = g.MissingTargets();
        Assert.Single(missing);
        Assert.Equal("Icon.Missing", missing[0].To);
    }

    [Fact]
    public void Cycle_E011()
    {
        var g = new DependencyGraph();
        g.AddEdge("A", "B");
        g.AddEdge("B", "C");
        g.AddEdge("C", "A");
        var cycle = g.FindCycle();
        Assert.NotNull(cycle);
        Assert.Contains("A", cycle!);
    }

    [Fact]
    public void HasDependents_E012()
    {
        var g = new DependencyGraph();
        g.AddEdge("ToolA", "IconX");
        g.AddEdge("ToolB", "IconX");
        var deps = g.HasDependents("IconX");
        Assert.Equal(new[] { "ToolA", "ToolB" }, deps);
    }

    [Fact]
    public void Validate_DuplicateHotkey_Reported()
    {
        var a = Rec("A", hotkey: "T");
        var b = Rec("B", hotkey: "T");
        var report = Validate.Run(new[] { a, b });
        Assert.Contains(report.Errors, e => e.Code == ErrorCodes.HotkeyConflict);
        Assert.False(report.Ok);
    }

    [Fact]
    public void Validate_MissingDefinition_Reported()
    {
        var c = new Validate.ComponentRecord
        {
            ComponentId = "X",
            Kind = "DataTool",
            RootPath = "/tmp",
            ManifestPath = "/tmp/X/Manifest.json",
            Manifest = new ComponentManifest { ComponentId = "X", Kind = "DataTool" },
            DefinitionPath = "/tmp/X/Definition.json" // does not exist
        };
        var report = Validate.Run(new[] { c });
        Assert.Contains(report.MissingFiles, f => f.Contains("Definition.json"));
    }

    [Fact]
    public void Validate_CatalogStale()
    {
        var report = Validate.Run(
            Array.Empty<Validate.ComponentRecord>(),
            expectedCatalogHash: "aaa",
            actualCatalogHash: "bbb");
        Assert.True(report.CatalogStale);
        Assert.False(report.Ok);
    }

    private static Validate.ComponentRecord Rec(string id, string? hotkey = null)
        => new()
        {
            ComponentId = id,
            Kind = "DataTool",
            RootPath = "/tmp",
            ManifestPath = "/tmp/" + id + "/Manifest.json",
            Manifest = new ComponentManifest
            {
                ComponentId = id,
                Kind = "DataTool",
                ToolId = "Tool." + id,
                TypeId = "drawing." + id,
                Hotkey = hotkey,
                IconKey = "Icon.Default"
            },
            DefinitionPath = null
        };
}
