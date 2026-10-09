using System.Reflection;
using Xunit;

namespace MyChart.Tests.Smoke;

/// <summary>
/// T7.04 TestingSweep — coverage check that required suite areas exist as test types.
/// Full execution is: dotnet test MyChart.Tests + MyChart.Generator.Tests (owner runs both).
/// Rule: TestsFollowCorrespondingStages; this node only asserts presence and documents the map.
/// </summary>
public class TestingSweepCoverageTests
{
    private static IEnumerable<Type> TestTypes()
        => typeof(TestingSweepCoverageTests).Assembly
            .GetTypes()
            .Where(t => t.IsClass && t.Name.EndsWith("Tests", StringComparison.Ordinal));

    private static void AssertSuite(string suiteName, params string[] typeNameFragments)
    {
        var names = TestTypes().Select(t => t.FullName ?? t.Name).ToList();
        foreach (var frag in typeNameFragments)
        {
            Assert.True(
                names.Any(n => n!.Contains(frag, StringComparison.Ordinal)),
                $"T7.04 {suiteName}: missing test type containing '{frag}'. Found: {string.Join(", ", names.Where(n => n!.Contains(frag.Split('.')[0], StringComparison.Ordinal)).Take(5))}");
        }
    }

    [Fact]
    public void DataTests_Present()
        => AssertSuite("DataTests",
            "TimeBucketsTests",
            "DataValidatorTests",
            "TickFilterTests",
            "CsvProviderTests",
            "SymbolRegistryTests",
            "HistoryMergeTests",
            "MemoryCacheTests",
            "MarketDataBusTests");

    [Fact]
    public void CandleTests_Present()
        => AssertSuite("CandleTests",
            "CandleCalculationTests",
            "AggregationEngineTests",
            "CandleGeometryTests");

    [Fact]
    public void ViewportTests_Present()
        => AssertSuite("ViewportTests",
            "TimeIndexMapperTests",
            "CoordinateConverterTests",
            "ZoomPanTests",
            "PriceScaleEngineTests",
            "NiceTicksTests",
            "LatestViewControllerTests");

    [Fact]
    public void RendererTests_Present()
        => AssertSuite("RendererTests",
            "CandleRendererTests",
            "BackgroundGridAxisTests",
            "CrosshairRendererTests",
            "CurrentPriceRendererTests",
            "HudRendererTests",
            "IndicatorRendererTests",
            "RenderPipelineTests",
            "ThemeTokensTests");

    [Fact]
    public void DrawingTests_Present()
        => AssertSuite("DrawingTests",
            "DrawingGeometryTests",
            "ShapeGeometryTests",
            "CommandBusTests");

    [Fact]
    public void PluginAndExpression_Present()
        => AssertSuite("PluginTests",
            "ExpressionEvaluatorTests",
            "DefinitionValidatorTests",
            "AcceptanceSuiteTests",
            "PluginHostLoadTests",
            "FirstPartyComponentTests");

    [Fact]
    public void Assembly_Has_AtLeast_Fifty_TestClasses()
    {
        var count = TestTypes().Count();
        Assert.True(count >= 50, $"Expected >= 50 test classes, found {count}");
    }
}
