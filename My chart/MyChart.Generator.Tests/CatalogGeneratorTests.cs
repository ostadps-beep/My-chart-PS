using MyChart.Generator.Engine.Operations;
using Xunit;

namespace MyChart.Generator.Tests;

/// <summary>PG3.05 VERIFY — AT3 determinism; inactive excluded; header hash present.</summary>
public class CatalogGeneratorTests
{
    private static CatalogGenerator.ComponentInput Tool(string id, bool active = true)
    {
        var files = Scaffolder.Build(new ScaffoldRequest
        {
            Kind = ScaffoldKind.DataToolClick,
            ComponentId = id,
            Name = id,
            Anchors = 2
        });
        var manifest = files["Manifest.json"];
        if (!active)
        {
            // flip active in JSON for test
            manifest = manifest.Replace("\"active\": true", "\"active\": false");
        }
        return new CatalogGenerator.ComponentInput
        {
            ComponentId = id,
            Kind = "DataTool",
            Active = active,
            ManifestJson = manifest,
            DefinitionJson = files["Definition.json"]
        };
    }

    private static CatalogGenerator.ComponentInput Icon(string id, string key)
    {
        var files = Scaffolder.Build(new ScaffoldRequest
        {
            Kind = ScaffoldKind.IconVector,
            ComponentId = id,
            Name = id,
            IconKey = key
        });
        return new CatalogGenerator.ComponentInput
        {
            ComponentId = id,
            Kind = "Icon",
            Active = true,
            ManifestJson = files["Manifest.json"],
            IconKey = key,
            GeometryPathData = files["Geometry.txt"].Trim()
        };
    }

    [Fact]
    public void Generate_IsDeterministic_OrdinalSort()
    {
        var comps = new[] { Tool("Zulu"), Tool("Alpha"), Icon("IconB", "Icon.B"), Icon("IconA", "Icon.A") };
        var a = CatalogGenerator.Generate(comps);
        var b = CatalogGenerator.Generate(comps.Reverse());

        Assert.Equal(a.PluginCatalogCs, b.PluginCatalogCs);
        Assert.Equal(a.IconCatalogCs, b.IconCatalogCs);
        Assert.Equal(a.ManifestHash, b.ManifestHash);

        // ordinal: Alpha before Zulu in plugin catalog
        var alphaPos = a.PluginCatalogCs.IndexOf("Alpha", StringComparison.Ordinal);
        var zuluPos = a.PluginCatalogCs.IndexOf("Zulu", StringComparison.Ordinal);
        Assert.True(alphaPos >= 0 && zuluPos > alphaPos);

        // icons sorted by IconKey: Icon.A before Icon.B
        var posA = a.IconCatalogCs.IndexOf("Icon.A", StringComparison.Ordinal);
        var posB = a.IconCatalogCs.IndexOf("Icon.B", StringComparison.Ordinal);
        Assert.True(posA >= 0 && posB > posA);
    }

    [Fact]
    public void Inactive_ExcludedFromCatalog()
    {
        var comps = new[] { Tool("Live", active: true), Tool("Dead", active: false) };
        var (plugin, _, _) = CatalogGenerator.Generate(comps);

        Assert.Contains("Live", plugin);
        Assert.DoesNotContain("Dead", plugin);
    }

    [Fact]
    public void Header_ContainsManifestHash()
    {
        var (plugin, icon, hash) = CatalogGenerator.Generate(new[] { Tool("One") });
        Assert.Contains($"manifest-hash: {hash}", plugin);
        Assert.Contains($"manifest-hash: {hash}", icon);
        Assert.Contains($"ManifestHash = \"{hash}\"", plugin);
        Assert.DoesNotContain("\r", plugin);
        Assert.EndsWith("\n", plugin);
    }
}
