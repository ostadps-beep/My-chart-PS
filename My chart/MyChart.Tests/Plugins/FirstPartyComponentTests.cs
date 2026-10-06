using System.Text.Json.Nodes;
using MyChart.Core.Plugins.Identity;
using MyChart.Core.Plugins.Vocabulary;
using MyChart.Plugins;
using MyChart.Tests.Support;
using Xunit;

namespace MyChart.Tests.Plugins;

/// <summary>T4.09 / PG5 — first-party definitions and scenes.</summary>
public class FirstPartyComponentTests
{
    private static string ComponentsRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "MyChart.Plugins", "Components"));

    [Fact]
    public void SixDataTools_DefinitionsValidate()
    {
        foreach (var id in PluginCatalog.DataToolIds)
        {
            var path = Path.Combine(ComponentsRoot, id, "Definition.json");
            Assert.True(File.Exists(path), $"Missing {path}");
            var json = File.ReadAllText(path);
            var err = DefinitionValidator.ValidateJson(json, out var def);
            Assert.Null(err);
            Assert.NotNull(def);
        }
    }

    [Fact]
    public void Icons_GeometryValid()
    {
        Assert.True(IconCatalog.All.Count >= 8);
        foreach (var icon in IconCatalog.All)
            Assert.True(GeometryPathGrammar.IsValid(icon.PathData), icon.Key);
    }

    [Fact]
    public void TrendLine_Scene_Deterministic()
    {
        var a = ComponentScenes.RenderTrendLine(write: false);
        var b = ComponentScenes.RenderTrendLine(write: false);
        Assert.NotEmpty(a);
        Assert.True(a.AsSpan().SequenceEqual(b));
    }

    [Fact]
    public void TrendLine_Definition_HasLineShape()
    {
        var path = Path.Combine(ComponentsRoot, "TrendLine", "Definition.json");
        var root = JsonNode.Parse(File.ReadAllText(path)) as JsonObject;
        Assert.NotNull(root);
        Assert.Equal(2, root!["anchors"]!.GetValue<int>());
        var shapes = root["shapes"]!.AsArray();
        Assert.Equal("Line", shapes[0]!["kind"]!.GetValue<string>());
    }
}
