using MyChart.Core.Contracts.Plugins;
using MyChart.Core.Contracts.Rendering;
using MyChart.Core.Contracts.Services;
using MyChart.Core.Contracts.UI;
using MyChart.Core.Models.Drawing;
using MyChart.Core.Models.Geometry;
using MyChart.Core.Models.Market;
using MyChart.Core.Models.Rendering;
using MyChart.PluginHost.DataTools;
using MyChart.PluginHost.Loading;
using MyChart.Tests.Support;
using Xunit;
using MyChart.Core.Services;

namespace MyChart.Tests.Plugins;

public class DataToolRuntimeTests
{
    private sealed class FakeToolContext : IToolContext
    {
        public required IChartMapper Map { get; init; }
        public IChartSettings Settings => null!;
        public IThemeService Theme => null!;
        public SymbolInfo Symbol => new("EURUSD", "EURUSD", SymbolGroup.Forex, 5);
        public PointD Snap(PointD p) => p;
        public string? PromptResult { get; set; }
        public void PromptText(Action<string?> onDone) => onDone(PromptResult);
        public void Invalidate() { }
    }

    private sealed class RecordingRender : IRenderContext
    {
        public int Width => 800;
        public int Height => 500;
        public double DpiScale => 1;
        public List<string> Calls { get; } = new();
        public void Clear(RgbaColor color) { }
        public void FillRect(int x, int y, int w, int h, RgbaColor color) => Calls.Add($"FillRect");
        public void StrokeRect(int x, int y, int w, int h, int thickness, RgbaColor color) => Calls.Add($"StrokeRect");
        public void DrawLine(double x1, double y1, double x2, double y2, RgbaColor color, double width, double[]? dash)
            => Calls.Add($"DrawLine");
        public void DrawText(string text, double x, double y, TextStyle style, RgbaColor color, TextAlign align) { }
        public void DrawPath(IReadOnlyList<PointD> points, RgbaColor color, double width, bool closed, bool fill) { }
        public SizeD MeasureText(string text, TextStyle style) => new(10, 10);
        public void PushClip(RectD rect) { }
        public void PopClip() { }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void ClickWorkflow_N_CommitsObject(int n)
    {
        var map = ViewChartMapper.Standard();
        var tool = new DataToolFactory("T", "drawing.t", n);
        tool.Bind(new FakeToolContext { Map = map });
        tool.Activate();

        ToolResult? last = null;
        for (int i = 0; i < n; i++)
            last = tool.OnPointer(new PointerEvent(PointerKind.Down, "Left", 100 + i * 10, 200, ""));

        Assert.IsType<ToolResult.Commit>(last);
        var commit = (ToolResult.Commit)last!;
        Assert.Equal(n, commit.Object.Anchors.Count);
        Assert.Equal("drawing.t", commit.Object.TypeId);
    }

    [Fact]
    public void Escape_CancelsWithoutCommit()
    {
        var map = ViewChartMapper.Standard();
        var tool = new DataToolFactory("T", "drawing.t", 2);
        tool.Bind(new FakeToolContext { Map = map });
        tool.Activate();
        tool.OnPointer(new PointerEvent(PointerKind.Down, "Left", 100, 200, ""));
        var result = tool.OnKey(new KeyEvent("Escape", ""));
        Assert.IsType<ToolResult.Cancel>(result);
        Assert.Null(tool.Preview);
    }

    [Fact]
    public void ClickThenText_Empty_Cancels()
    {
        var map = ViewChartMapper.Standard();
        var tool = new DataToolFactory("Text", "drawing.text", 1, "ClickThenText");
        var ctx = new FakeToolContext { Map = map, PromptResult = null };
        tool.Bind(ctx);
        tool.Activate();
        var result = tool.OnPointer(new PointerEvent(PointerKind.Down, "Left", 100, 200, ""));
        Assert.IsType<ToolResult.Cancel>(result);
    }

    [Fact]
    public void ClickThenText_WithText_Commits()
    {
        var map = ViewChartMapper.Standard();
        var tool = new DataToolFactory("Text", "drawing.text", 1, "ClickThenText");
        var ctx = new FakeToolContext { Map = map, PromptResult = "hello" };
        tool.Bind(ctx);
        tool.Activate();
        var result = tool.OnPointer(new PointerEvent(PointerKind.Down, "Left", 100, 200, ""));
        Assert.IsType<ToolResult.Commit>(result);
    }

    [Fact]
    public void Painter_DrawsLine()
    {
        var map = ViewChartMapper.Standard();
        var render = new RecordingRender();
        var painter = new DataToolPainter("Line");
        var t0 = ViewChartMapper.FirstOpen;
        var obj = new DrawingObject(
            "id", "drawing.line", 1,
            new[]
            {
                new DrawingAnchor(t0, 1.1000),
                new DrawingAnchor(t0.AddMinutes(30), 1.1050)
            },
            new DrawingStyle(RgbaColor.FromRgb(33, 150, 243), 1),
            false, false, null);

        painter.Paint(new DrawContext
        {
            Render = render,
            Map = map,
            Theme = ThemeService.Dark,
            Symbol = new SymbolInfo("EURUSD", "EURUSD", SymbolGroup.Forex, 5),
            DpiScale = 1,
            State = DrawState.Normal
        }, obj);

        Assert.Contains("DrawLine", render.Calls);
    }

    [Fact]
    public void UserComponentLoader_GoodAndBroken()
    {
        var root = Path.Combine(Path.GetTempPath(), "mychart-user-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        // good DataTool
        var good = Path.Combine(root, "GoodLine");
        Directory.CreateDirectory(good);
        File.WriteAllText(Path.Combine(good, "Manifest.json"),
            """{"componentId":"GoodLine","kind":"DataTool","toolId":"GoodLine","typeId":"drawing.good","iconKey":"Icon.X"}""");
        File.WriteAllText(Path.Combine(good, "Definition.json"),
            """{"schema":"mychart.tooldef","vocabularyVersion":"1.0","anchors":2,"workflow":"Click","shapes":[{"kind":"Line","from":{"x":{"anchor":0},"y":{"anchor":0}},"to":{"x":{"anchor":1},"y":{"anchor":1}}}]}""");

        // CodeTool -> E023
        var code = Path.Combine(root, "Codey");
        Directory.CreateDirectory(code);
        File.WriteAllText(Path.Combine(code, "Manifest.json"),
            """{"componentId":"Codey","kind":"CodeTool"}""");

        // broken definition
        var bad = Path.Combine(root, "Broken");
        Directory.CreateDirectory(bad);
        File.WriteAllText(Path.Combine(bad, "Manifest.json"),
            """{"componentId":"Broken","kind":"DataTool"}""");
        File.WriteAllText(Path.Combine(bad, "Definition.json"),
            """{"schema":"mychart.tooldef","vocabularyVersion":"1.0","anchors":0,"shapes":[]}""");

        // duplicate against repo
        var dup = Path.Combine(root, "RepoDup");
        Directory.CreateDirectory(dup);
        File.WriteAllText(Path.Combine(dup, "Manifest.json"),
            """{"componentId":"RepoDup","kind":"DataTool"}""");
        File.WriteAllText(Path.Combine(dup, "Definition.json"),
            """{"schema":"mychart.tooldef","vocabularyVersion":"1.0","anchors":1,"shapes":[{"kind":"Line","from":{"x":{"anchor":0},"y":{"anchor":0}},"to":{"x":{"anchor":0},"y":{"anchor":0}}}]}""");

        var report = new PluginLoadReport();
        var loader = new UserComponentLoader(root, repoComponentIds: new[] { "RepoDup" });
        var sets = loader.Load(report).ToList();

        Assert.Single(sets);
        Assert.Equal("GoodLine", sets[0].ComponentId);
        Assert.Contains(report.Failed, f => f.ComponentId == "Codey" && f.Code == "E023");
        Assert.Contains(report.Failed, f => f.ComponentId == "Broken");
        Assert.Contains(report.Failed, f => f.ComponentId == "RepoDup" && f.Code == "E002");

        try { Directory.Delete(root, true); } catch { /* ignore */ }
    }
}
