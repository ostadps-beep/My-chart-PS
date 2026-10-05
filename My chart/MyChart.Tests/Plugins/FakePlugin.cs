using MyChart.Core.Contracts.Plugins;
using MyChart.Core.Models.Drawing;
using MyChart.Core.Models.Geometry;
using MyChart.Core.Models.Plugins;
using MyChart.Core.Models.Rendering;

namespace MyChart.Tests.Plugins;

/// <summary>
/// PG4.01 FakePlugin — one-click tool; paint draws a line; hit tests near anchors.
/// ComponentId is intentionally generic so chart code never names a concrete product tool.
/// </summary>
public sealed class FakePlugin : IChartPlugin
{
    public const string ToolId = "Tool.Fake";
    public const string TypeId = "drawing.Fake";

    public string ComponentId => "FakeComponent";

    public void Register(IPluginHost host)
    {
        host.Tools.Register(
            new ToolDescriptor(
                ComponentId, ToolId, "Fake", "Draw", "Icon.Fake", null, TypeId, 1,
                Array.Empty<ParameterDescriptor>()),
            _ => new FakeTool());

        host.DrawingTypes.Register(
            new DrawingTypeDescriptor(TypeId, 1, 1),
            new FakePainter(),
            new FakeHitTester(),
            migrator: null);

        host.Icons.Register(new IconDescriptor("Icon.Fake", "M0 0 L16 16"));
    }
}

internal sealed class FakeTool : IDrawingTool
{
    public string ToolId => FakePlugin.ToolId;
    public DrawingObject? Preview { get; private set; }

    public void Activate() { }
    public void Deactivate() => Preview = null;

    public ToolResult OnPointer(PointerEvent e)
    {
        if (e.Kind != PointerKind.Down) return new ToolResult.None();

        var obj = new DrawingObject(
            Guid.NewGuid().ToString("N"),
            FakePlugin.TypeId,
            1,
            new DrawingAnchor[] { new(DateTimeOffset.UnixEpoch, 1.10) },
            new DrawingStyle(RgbaColor.FromRgb(0, 120, 215), 2),
            Locked: false,
            Hidden: false,
            Extra: null);
        return new ToolResult.Commit(obj);
    }

    public ToolResult OnKey(KeyEvent e) => new ToolResult.None();
}

internal sealed class FakePainter : IDrawingObjectPainter
{
    public void Paint(DrawContext ctx, DrawingObject obj)
    {
        if (obj.Anchors.Count == 0) return;
        var a = obj.Anchors[0];
        var x = ctx.Map.X(ctx.Map.IndexOfTime(a.TimeUtc));
        var y = ctx.Map.Y(a.Price);
        ctx.Render.DrawLine(x, y, x + 10, y, obj.Style.Color, obj.Style.Thickness, null);
    }
}

internal sealed class FakeHitTester : IDrawingHitTester
{
    public HitResult HitTest(DrawingObject obj, PointD p, IChartMapper map, double toleranceDip)
    {
        if (obj.Anchors.Count == 0) return new HitResult(HitKind.None);
        var a = obj.Anchors[0];
        var x = map.X(map.IndexOfTime(a.TimeUtc));
        var y = map.Y(a.Price);
        var dx = p.X - x;
        var dy = p.Y - y;
        if (dx * dx + dy * dy <= toleranceDip * toleranceDip)
            return new HitResult(HitKind.Body);
        return new HitResult(HitKind.None);
    }
}

internal sealed class IdentityMapper : IChartMapper
{
    public RectD PlotRect => new(0, 0, 800, 600);
    public double X(double u) => u;
    public double U(double x) => x;
    public double Y(double price) => price * 100;
    public double Price(double y) => y / 100;
    public double IndexOfTime(DateTimeOffset t) => 0;
    public DateTimeOffset TimeAtIndex(double u) => DateTimeOffset.UnixEpoch;
    public int SnapIndex(double u) => (int)Math.Round(u);
}
