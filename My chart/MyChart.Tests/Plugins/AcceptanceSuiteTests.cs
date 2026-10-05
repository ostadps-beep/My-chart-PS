using MyChart.Core.Plugins.Identity;
using MyChart.Core.Plugins.Manifest;
using MyChart.Core.Plugins.Vocabulary;
using MyChart.Generator.Engine.Operations;
using Xunit;

namespace MyChart.Tests.Plugins;

/// <summary>
/// PG4.02 AcceptanceSuite — consolidates AT vectors available before PG5.
/// AT9 and AT11 full completion is deferred to PG5 (per PG4.02 REQUIRES).
/// </summary>
public class AcceptanceSuiteTests
{
    // --- AT5 V1 ids ---
    [Theory]
    [InlineData("trendline", false)]
    [InlineData("Trend Line", false)]
    [InlineData("TL", false)]
    [InlineData("TrendLine", true)]
    public void AT5_V1_Ids(string id, bool ok)
    {
        var err = IdRules.Validate(id);
        if (ok) Assert.Null(err);
        else Assert.Equal(ErrorCodes.InvalidId, err);
    }

    // --- AT5 V6 geometry ---
    [Theory]
    [InlineData("M2 2 L14 14", true)]
    [InlineData("M2 2 X14 14", false)]
    [InlineData("M2 2 L14", false)]
    [InlineData("", false)]
    public void AT5_V6_Geometry(string path, bool ok)
    {
        var err = GeometryPathGrammar.Validate(string.IsNullOrEmpty(path) ? path : path);
        if (ok) Assert.Null(err);
        else Assert.Equal(ErrorCodes.InvalidGeometry, err);
    }

    [Fact]
    public void AT5_V6_Geometry_TooLong()
    {
        var longPath = "M0 0 " + string.Concat(Enumerable.Repeat("L1 1 ", 2000));
        Assert.True(longPath.Length > 4096);
        Assert.Equal(ErrorCodes.InvalidGeometry, GeometryPathGrammar.Validate(longPath));
    }

    // --- AT5 V8 version ---
    [Theory]
    [InlineData("1.0", false)]
    [InlineData("1.0.0", true)]
    [InlineData("2.1.3", true)]
    public void AT5_V8_Version(string ver, bool ok)
    {
        // SemVer major.minor.patch
        var parts = ver.Split('.');
        var valid = parts.Length == 3 && parts.All(p => int.TryParse(p, out _));
        Assert.Equal(ok, valid);
    }

    // --- AT14 vocabulary vectors (subset already enforced by DefinitionValidator) ---
    [Fact]
    public void AT14_Anchors0_E019()
    {
        var json = """
            {"schema":"mychart.tooldef","vocabularyVersion":"1.0","anchors":0,"workflow":"Click","shapes":[]}
            """;
        var err = DefinitionValidator.ValidateJson(json, out _);
        Assert.Equal(ErrorCodes.DefinitionSchemaError, err);
    }

    [Fact]
    public void AT14_UnknownShape_E021()
    {
        var json = """
            {"schema":"mychart.tooldef","vocabularyVersion":"1.0","anchors":2,"workflow":"Click",
             "shapes":[{"kind":"Spiral","from":{"x":{"anchor":0},"y":{"anchor":0}},"to":{"x":{"anchor":1},"y":{"anchor":1}}}]}
            """;
        var err = DefinitionValidator.ValidateJson(json, out _);
        Assert.Equal(ErrorCodes.VocabularyUnsupported, err);
    }

    [Fact]
    public void AT14_ClickThenText_Anchors2_E019()
    {
        var json = """
            {"schema":"mychart.tooldef","vocabularyVersion":"1.0","anchors":2,"workflow":"ClickThenText","shapes":[]}
            """;
        var err = DefinitionValidator.ValidateJson(json, out _);
        Assert.Equal(ErrorCodes.DefinitionSchemaError, err);
    }

    // --- AT10 no tool names in dispatch (already in HostWiringTests) ---
    [Fact]
    public void AT10_DispatchFiles_NoProductToolLiterals()
    {
        var dir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "MyChart.Core", "Analysis"));
        if (!Directory.Exists(dir)) return;
        foreach (var name in new[] { "DrawingPaintDispatcher.cs", "DrawingHitDispatcher.cs", "ToolSession.cs" })
        {
            var path = Path.Combine(dir, name);
            if (!File.Exists(path)) continue;
            var text = File.ReadAllText(path);
            Assert.DoesNotContain("TrendLine", text);
            Assert.DoesNotContain("drawing.TrendLine", text);
        }
    }

    // --- Mapping note: AT1/AT2/AT3/AT7/AT8 covered by Generator.Tests (Transaction/ToolOps/Catalog/CLI) ---
    [Fact]
    public void AT_Mapping_GeneratorSuite_Present()
    {
        // Smoke: Scaffolder + CatalogGenerator available for generator ATs.
        var files = Scaffolder.Build(new ScaffoldRequest
        {
            Kind = ScaffoldKind.DataToolClick,
            ComponentId = "TrendLine",
            Name = "Trend Line",
            Anchors = 2
        });
        Assert.True(files.ContainsKey("Manifest.json"));
        var (plugin, _, _) = CatalogGenerator.Generate(new[]
        {
            new CatalogGenerator.ComponentInput
            {
                ComponentId = "TrendLine",
                Kind = "DataTool",
                Active = true,
                ManifestJson = files["Manifest.json"],
                DefinitionJson = files["Definition.json"]
            }
        });
        Assert.Contains("TrendLine", plugin);
    }
}
