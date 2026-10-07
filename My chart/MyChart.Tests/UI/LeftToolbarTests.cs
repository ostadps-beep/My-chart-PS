using MyChart.Core.Models.Plugins;
using MyChart.Core.UI.Toolbar;
using Xunit;

namespace MyChart.Tests.UI;

/// <summary>T6.04 LeftToolbarAndDrawingTools — inventory, sizes, slot order, controller (GATE=VISUAL pending).</summary>
public class LeftToolbarTests
{
    [Fact]
    public void Width_Is_36_Dip_Buttons_32()
    {
        Assert.Equal(36, LeftToolbarModel.WidthDip);
        Assert.Equal(32, LeftToolbarModel.ButtonSizeDip);
    }

    [Fact]
    public void Core_Always_Has_Cursor_And_Crosshair()
    {
        var tb = LeftToolbarModel.FromContributions(Array.Empty<SlotContribution>());
        Assert.Equal(2, tb.Items.Count);
        Assert.Equal("core:cursor", tb.Items[0].ItemId);
        Assert.Equal(0, tb.Items[0].Order);
        Assert.Equal(LeftToolbarItemKind.Cursor, tb.Items[0].Kind);
        Assert.Equal("core:crosshair", tb.Items[1].ItemId);
        Assert.Equal(10, tb.Items[1].Order);
        Assert.Equal(LeftToolbarItemKind.Crosshair, tb.Items[1].Kind);
    }

    [Fact]
    public void Default_Order_Matches_Spec()
    {
        var tb = LeftToolbarModel.CreateDefault();
        var ids = tb.Items.Select(i => i.ItemId).ToArray();
        Assert.Equal(
            new[]
            {
                "core:cursor",
                "core:crosshair",
                "tool:TrendLine",
                "tool:Rectangle",
                "tool:Arrow",
                "tool:Text",
                "tool:Fibonacci",
                "tool:Measure"
            },
            ids);

        Assert.Equal(new[] { 0, 10, 100, 110, 120, 130, 140, 150 },
            tb.Items.Select(i => i.Order).ToArray());
    }

    [Fact]
    public void Sorted_By_Order_Then_ItemId()
    {
        var contribs = new[]
        {
            new SlotContribution(Slot.LeftToolbar, "tool:B", "Icon.B", 100, "tools", "tool.B"),
            new SlotContribution(Slot.LeftToolbar, "tool:A", "Icon.A", 100, "tools", "tool.A"),
            new SlotContribution(Slot.LeftToolbar, "tool:Z", "Icon.Z", 50, "tools", "tool.Z"),
        };
        var tb = LeftToolbarModel.FromContributions(contribs);
        var toolIds = tb.Items.Where(i => i.Kind == LeftToolbarItemKind.DrawingTool)
            .Select(i => i.ItemId).ToArray();
        Assert.Equal(new[] { "tool:Z", "tool:A", "tool:B" }, toolIds);
    }

    [Fact]
    public void No_Hardcoded_Tool_Name_In_UI_Items_Use_IconKey_Only()
    {
        var tb = LeftToolbarModel.CreateDefault();
        foreach (var item in tb.Items)
        {
            Assert.False(string.IsNullOrWhiteSpace(item.IconKey));
            Assert.False(string.IsNullOrWhiteSpace(item.CommandRef));
            Assert.DoesNotContain(" ", item.ItemId);
        }
    }

    [Fact]
    public void Controller_Select_Tool()
    {
        var ctrl = new LeftToolbarController();
        var tool = LeftToolbarModel.CreateDefault().Items.Single(i => i.ItemId == "tool:TrendLine");
        Assert.True(ctrl.Select(tool));
        Assert.Equal("tool:TrendLine", ctrl.SelectedItemId);
    }

    [Fact]
    public void Controller_Crosshair_Toggle()
    {
        var ctrl = new LeftToolbarController();
        var ch = LeftToolbarModel.CreateDefault().Items.Single(i => i.Kind == LeftToolbarItemKind.Crosshair);
        Assert.False(ctrl.CrosshairActive);
        Assert.True(ctrl.Select(ch));
        Assert.True(ctrl.CrosshairActive);
        Assert.True(ctrl.Select(ch));
        Assert.False(ctrl.CrosshairActive);
    }

    [Fact]
    public void Controller_Crosshair_Flyout()
    {
        var ctrl = new LeftToolbarController();
        Assert.True(ctrl.OpenCrosshairFlyout());
        Assert.True(ctrl.FlyoutOpen);
        Assert.True(ctrl.ApplyFlyout(CrosshairFlyoutOption.Magnet));
        Assert.False(ctrl.FlyoutOpen);
        Assert.Equal(CrosshairFlyoutOption.Magnet, ctrl.FlyoutSelection);
        Assert.Contains(ctrl.Events, e => e == "crosshair:flyout:Magnet");
    }

    [Fact]
    public void Controller_Esc_Returns_To_Cursor()
    {
        var ctrl = new LeftToolbarController();
        var tool = LeftToolbarModel.CreateDefault().Items.Single(i => i.ItemId == "tool:Rectangle");
        ctrl.Select(tool);
        Assert.Equal("tool:Rectangle", ctrl.SelectedItemId);
        ctrl.Cancel();
        Assert.Equal("core:cursor", ctrl.SelectedItemId);
    }

    [Fact]
    public void Controller_Commit_Returns_To_Cursor()
    {
        var ctrl = new LeftToolbarController();
        var tool = LeftToolbarModel.CreateDefault().Items.Single(i => i.ItemId == "tool:Arrow");
        ctrl.Select(tool);
        ctrl.OnToolCommitted();
        Assert.Equal("core:cursor", ctrl.SelectedItemId);
        Assert.Contains(ctrl.Events, e => e == "commit->cursor");
    }

    [Fact]
    public void Default_Selected_Is_Cursor()
    {
        var tb = LeftToolbarModel.CreateDefault();
        Assert.True(tb.Items.Single(i => i.ItemId == "core:cursor").IsSelected);
        Assert.All(tb.Items.Where(i => i.ItemId != "core:cursor"), i => Assert.False(i.IsSelected));
    }
}
