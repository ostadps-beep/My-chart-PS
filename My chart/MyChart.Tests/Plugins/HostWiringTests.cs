using MyChart.Core.Analysis;
using MyChart.Core.Contracts.Plugins;
using MyChart.Core.Contracts.Services;
using MyChart.Core.Contracts.UI;
using MyChart.Core.Models.Geometry;
using MyChart.Core.Models.Market;
using MyChart.Core.Models.Rendering;
using MyChart.PluginHost.Loading;
using Xunit;

namespace MyChart.Tests.Plugins;

/// <summary>
/// PG4.01 VERIFY: FakePlugin activate → pointer commit → paint → hit → undo;
/// chart code contains no concrete product tool name (TrendLine etc.).
/// </summary>
public class HostWiringTests
{
    private sealed class Source : IComponentSource
    {
        private readonly IChartPlugin _plugin;
        public Source(IChartPlugin plugin) => _plugin = plugin;
        public IEnumerable<ComponentSet> Load(PluginLoadReport report)
        {
            yield return new ComponentSet { ComponentId = _plugin.ComponentId, Plugin = _plugin };
        }
    }

    private static SymbolInfo EurUsd() => new("EURUSD", "test", SymbolGroup.Forex, 5);

    private sealed class StubToolContext : IToolContext
    {
        public IChartMapper Map { get; } = new IdentityMapper();
        public IChartSettings Settings => null!;
        public IThemeService Theme => null!;
        public SymbolInfo Symbol { get; } = EurUsd();
        public PointD Snap(PointD p) => p;
        public void PromptText(Action<string?> onDone) => onDone(null);
        public void Invalidate() { }
    }

    [Fact]
    public void FakePlugin_Activate_Commit_Paint_Hit_Undo()
    {
        var composition = new ChartComposition();
        composition.Load(new Source(new FakePlugin()));

        Assert.Contains("FakeComponent", composition.LoadReport.Loaded);

        var session = new ToolSession(
            composition.Commands,
            composition.Drawings,
            composition.ResolveToolFactory);

        Assert.True(session.Activate(FakePlugin.ToolId, new StubToolContext()));

        var result = session.OnPointer(new PointerEvent(PointerKind.Down, "Left", 10, 20, ""));
        Assert.IsType<ToolResult.Commit>(result);
        Assert.Single(composition.Drawings.Items);

        var render = new RecordingRenderContext();
        var map = new IdentityMapper();
        var drawCtx = new DrawContext
        {
            Render = render,
            Map = map,
            Theme = new ThemeTokens(),
            Symbol = EurUsd()
        };

        DrawingPaintDispatcher.PaintAll(
            composition.Drawings,
            drawCtx,
            composition.ResolvePainter);

        Assert.True(render.DrawLineCount >= 1);

        var (hitObj, hit) = DrawingHitDispatcher.HitTest(
            composition.Drawings,
            new PointD(0, 110),
            map,
            toleranceDip: 8,
            composition.ResolveHitTester);

        Assert.NotNull(hitObj);
        Assert.Equal(HitKind.Body, hit.Kind);

        composition.Commands.Undo();
        Assert.Empty(composition.Drawings.Items);
    }

    [Fact]
    public void ChartAnalysis_HasNoConcreteProductToolName()
    {
        var dir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "MyChart.Core", "Analysis"));
        if (!Directory.Exists(dir))
            return;

        foreach (var file in Directory.EnumerateFiles(dir, "*.cs"))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("TrendLine", text);
            Assert.DoesNotContain("Fibonacci", text);
            Assert.DoesNotContain("HorizontalLine", text);
        }
    }
}
