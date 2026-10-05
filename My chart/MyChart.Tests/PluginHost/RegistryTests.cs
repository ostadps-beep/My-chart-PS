using MyChart.Core.Contracts.Plugins;
using MyChart.Core.Models.Drawing;
using MyChart.Core.Models.Geometry;
using MyChart.Core.Models.Plugins;
using MyChart.PluginHost.Registries;
using Xunit;

namespace MyChart.Tests.PluginHost;

public class RegistryTests
{
    private static ToolDescriptor Tool(string id, string componentId = "CompA") =>
        new(componentId, id, id, "Draw", "Icon.X", null, "type." + id, 2, Array.Empty<ParameterDescriptor>());

    [Fact]
    public void ToolRegistry_RegisterLookup()
    {
        var reg = new ToolRegistry();
        reg.Register(Tool("TrendLine"), _ => null!);
        Assert.True(reg.TryGet("TrendLine", out var d, out _));
        Assert.Equal("TrendLine", d.ToolId);
    }

    [Fact]
    public void ToolRegistry_Duplicate_Throws()
    {
        var reg = new ToolRegistry();
        reg.Register(Tool("TrendLine"), _ => null!);
        Assert.Throws<DuplicateRegistrationException>(() => reg.Register(Tool("TrendLine"), _ => null!));
    }

    [Fact]
    public void IconRegistry_Duplicate_Throws()
    {
        var reg = new IconRegistry();
        reg.Register(new IconDescriptor("Icon.A", "M0 0 L1 1"));
        Assert.Throws<DuplicateRegistrationException>(() =>
            reg.Register(new IconDescriptor("Icon.A", "M0 0 L2 2")));
    }

    [Fact]
    public void ContributionRegistry_SortedBySlotOrderItemId()
    {
        var reg = new ContributionRegistry();
        reg.Add(new SlotContribution(Slot.LeftToolbar, "b", "Icon.B", 10, "g", "tool:B"));
        reg.Add(new SlotContribution(Slot.LeftToolbar, "a", "Icon.A", 10, "g", "tool:A"));
        reg.Add(new SlotContribution(Slot.LeftToolbar, "c", "Icon.C", 5, "g", "tool:C"));
        var sorted = reg.Sorted();
        Assert.Equal("c", sorted[0].ItemId);
        Assert.Equal("a", sorted[1].ItemId);
        Assert.Equal("b", sorted[2].ItemId);
    }

    [Fact]
    public void DrawingTypeRegistry_IsKnown_VersionCheck()
    {
        var reg = new DrawingTypeRegistry();
        reg.Register(
            new DrawingTypeDescriptor("drawing.trend", 2, 2),
            new NopPainter(),
            new NopHit());
        Assert.True(reg.IsKnown("drawing.trend", 1));
        Assert.True(reg.IsKnown("drawing.trend", 2));
        Assert.False(reg.IsKnown("drawing.trend", 3));
        Assert.False(reg.IsKnown("missing", 1));
    }

    private sealed class NopPainter : IDrawingObjectPainter
    {
        public void Paint(DrawContext ctx, DrawingObject obj) { }
    }

    private sealed class NopHit : IDrawingHitTester
    {
        public HitResult HitTest(DrawingObject obj, PointD p, IChartMapper map, double toleranceDip)
            => new(HitKind.None);
    }
}
