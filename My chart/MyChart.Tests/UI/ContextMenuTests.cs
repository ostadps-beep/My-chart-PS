using MyChart.Core.Models.Plugins;
using MyChart.Core.UI.Menus;
using Xunit;

namespace MyChart.Tests.UI;

/// <summary>T6.02 ContextMenus — inventory and dispatch (GATE=VISUAL pending owner UI proof).</summary>
public class ContextMenuTests
{
    [Fact]
    public void ChartMenu_SectionOrder_Is_Exact()
    {
        var menu = ChartContextMenuBuilder.Build();
        Assert.Equal(ChartContextMenuBuilder.MenuId, menu.MenuId);
        Assert.Equal(
            new[] { "navigation", "crosshair", "drawing", "indicator", "chartOptions", "reset" },
            menu.Sections.Select(s => s.Id).ToArray());
    }

    [Fact]
    public void ChartMenu_Navigation_Items()
    {
        var nav = ChartContextMenuBuilder.Build().Sections.Single(s => s.Id == "navigation");
        Assert.Equal(new[] { "Go To Latest", "Go To Date...", "Reset View" }, nav.Items.Select(i => i.Label).ToArray());
        Assert.All(nav.Items, i => Assert.Equal(MenuActionKind.Command, i.Kind));
    }

    [Fact]
    public void ChartMenu_Crosshair_Toggles()
    {
        var xh = ChartContextMenuBuilder.Build(state: new ChartMenuState
        {
            CrosshairMagnet = true
        }).Sections.Single(s => s.Id == "crosshair");

        Assert.Equal(new[] { "Analysis", "Data Inspector", "Magnet" }, xh.Items.Select(i => i.Label).ToArray());
        Assert.All(xh.Items, i => Assert.True(i.IsToggle));
        Assert.True(xh.Items.Single(i => i.Id == "xh.magnet").IsChecked);
    }

    [Fact]
    public void ChartMenu_Drawing_Starts_With_Cursor_Then_Contributions()
    {
        var contrib = new[]
        {
            new SlotContribution(Slot.ChartContextMenu, "tool.trend", "Icon.TrendLine", 100, "Tools", "Tool.TrendLine"),
            new SlotContribution(Slot.ChartContextMenu, "tool.rect", "Icon.Rectangle", 110, "Tools", "Tool.Rectangle"),
            new SlotContribution(Slot.LeftToolbar, "ignore", "Icon.X", 1, "X", "X"),
        };

        var drawing = ChartContextMenuBuilder.Build(contrib).Sections.Single(s => s.Id == "drawing");
        Assert.Equal("draw.cursor", drawing.Items[0].Id);
        Assert.Equal("Cursor", drawing.Items[0].Label);
        Assert.Equal("tool.trend", drawing.Items[1].Id);
        Assert.Equal(string.Empty, drawing.Items[1].Label);
        Assert.Equal("tool.rect", drawing.Items[2].Id);
        Assert.Equal(3, drawing.Items.Count);
    }

    [Fact]
    public void ChartMenu_Indicator_And_Reset()
    {
        var menu = ChartContextMenuBuilder.Build();
        var ind = menu.Sections.Single(s => s.Id == "indicator");
        Assert.Equal(new[] { "Add...", "Manage..." }, ind.Items.Select(i => i.Label).ToArray());

        var reset = menu.Sections.Single(s => s.Id == "reset");
        Assert.Equal(new[] { "Reset View", "Reset Scale to Auto" }, reset.Items.Select(i => i.Label).ToArray());
    }

    [Fact]
    public void ChartMenu_ChartOptions_Inventory()
    {
        var opt = ChartContextMenuBuilder.Build().Sections.Single(s => s.Id == "chartOptions");
        var labels = opt.Items.Select(i => i.Label).ToArray();
        Assert.Contains("Candles", labels);
        Assert.Contains("Hollow Candles", labels);
        Assert.Contains("OHLC", labels);
        Assert.Contains("Scale Auto", labels);
        Assert.Contains("Scale Manual", labels);
        Assert.Contains("Scale Log", labels);
        Assert.Contains("Scale Percentage", labels);
        Assert.Contains("Price Axis Right", labels);
        Assert.Contains("Price Axis Left", labels);
        Assert.Contains("Grid Major", labels);
        Assert.Contains("Grid Minor", labels);
        Assert.Contains("Navigator", labels);
        Assert.Contains("HUD", labels);
    }

    [Fact]
    public void DrawingMenu_Exact_Six_Items()
    {
        var menu = DrawingContextMenuBuilder.Build(isLocked: false, isHidden: false);
        Assert.Equal(DrawingContextMenuBuilder.MenuId, menu.MenuId);
        var labels = menu.AllItems().Select(i => i.Label).ToArray();
        Assert.Equal(new[] { "Edit", "Lock", "Clone", "Hide", "Delete", "Object Settings" }, labels);

        var locked = DrawingContextMenuBuilder.Build(isLocked: true, isHidden: true);
        var labels2 = locked.AllItems().Select(i => i.Label).ToArray();
        Assert.Equal(new[] { "Edit", "Unlock", "Clone", "Show", "Delete", "Object Settings" }, labels2);
    }

    [Fact]
    public void Invoker_Dispatches_Command_Toggle_SetState()
    {
        var commands = new List<string>();
        var toggles = new List<(string, bool)>();
        var sets = new List<(string, string?)>();

        var invoker = new ContextMenuInvoker(
            c => commands.Add(c),
            (k, v) => toggles.Add((k, v)),
            (k, v) => sets.Add((k, v)));

        invoker.Invoke(new ContextMenuItem("a", "Go", MenuActionKind.Command, "GoToLatest"));
        invoker.Invoke(new ContextMenuItem("b", "Magnet", MenuActionKind.Toggle, "Crosshair.Magnet", IsToggle: true, IsChecked: false));
        invoker.Invoke(new ContextMenuItem("c", "Candles", MenuActionKind.SetState, "ChartType", Value: "Candles"));

        Assert.Equal(new[] { "GoToLatest" }, commands);
        Assert.Equal(("Crosshair.Magnet", true), toggles[0]);
        Assert.Equal(("ChartType", "Candles"), sets[0]);
    }

    [Fact]
    public void Invoker_Skips_Disabled()
    {
        var n = 0;
        var invoker = new ContextMenuInvoker(_ => n++, (_, _) => n++, (_, _) => n++);
        invoker.Invoke(new ContextMenuItem("x", "X", MenuActionKind.Command, "X", IsEnabled: false));
        Assert.Equal(0, n);
    }

    [Fact]
    public void Every_Item_Maps_To_Command_Or_State()
    {
        var menu = ChartContextMenuBuilder.Build(new[]
        {
            new SlotContribution(Slot.ChartContextMenu, "t1", "Icon.T", 50, "G", "Tool.T1")
        });
        foreach (var item in menu.AllItems())
        {
            Assert.False(string.IsNullOrWhiteSpace(item.Target));
            Assert.True(item.Kind is MenuActionKind.Command or MenuActionKind.Toggle or MenuActionKind.SetState);
        }

        foreach (var item in DrawingContextMenuBuilder.Build().AllItems())
        {
            Assert.Equal(MenuActionKind.Command, item.Kind);
            Assert.StartsWith("Drawing.", item.Target);
        }
    }
}
