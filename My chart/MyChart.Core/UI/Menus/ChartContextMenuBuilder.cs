using MyChart.Core.Models.Plugins;

namespace MyChart.Core.UI.Menus;

/// <summary>
/// T6.02 CHART MENU inventory (INVENTORY_V1_1). Exactly this list; no extra tool names in core.
/// Drawing tools come only from Slot.ChartContextMenu contributions.
/// </summary>
public static class ChartContextMenuBuilder
{
    public const string MenuId = "chart.context";

    /// <summary>
    /// Build the chart context menu. <paramref name="drawingContributions"/> are registry items
    /// for Slot.ChartContextMenu (Order, ItemId, CommandRef); Cursor is always first in Drawing.
    /// </summary>
    public static ContextMenuModel Build(
        IReadOnlyList<SlotContribution>? drawingContributions = null,
        ChartMenuState? state = null)
    {
        state ??= ChartMenuState.Defaults;

        var navigation = new ContextMenuSection("navigation", "Navigation", new[]
        {
            Cmd("nav.goToLatest", "Go To Latest", "GoToLatest"),
            Cmd("nav.goToDate", "Go To Date...", "GoToDate"),
            Cmd("nav.resetView", "Reset View", "ResetView"),
        });

        var crosshair = new ContextMenuSection("crosshair", "Crosshair", new[]
        {
            Toggle("xh.analysis", "Analysis", "Crosshair.Analysis", state.CrosshairAnalysis),
            Toggle("xh.dataInspector", "Data Inspector", "Crosshair.DataInspector", state.CrosshairDataInspector),
            Toggle("xh.magnet", "Magnet", "Crosshair.Magnet", state.CrosshairMagnet),
        });

        var drawingItems = new List<ContextMenuItem>
        {
            Cmd("draw.cursor", "Cursor", "Tool.Cursor"),
        };
        if (drawingContributions is not null)
        {
            foreach (var c in drawingContributions
                         .Where(x => x.Slot == Slot.ChartContextMenu)
                         .OrderBy(x => x.Order)
                         .ThenBy(x => x.ItemId, StringComparer.Ordinal))
            {
                drawingItems.Add(new ContextMenuItem(
                    c.ItemId,
                    Label: string.Empty,
                    MenuActionKind.Command,
                    Target: c.CommandRef,
                    IsEnabled: true));
            }
        }

        var drawing = new ContextMenuSection("drawing", "Drawing", drawingItems);

        var indicator = new ContextMenuSection("indicator", "Indicator", new[]
        {
            Cmd("ind.add", "Add...", "Indicator.Add"),
            Cmd("ind.manage", "Manage...", "Indicator.Manage"),
        });

        var chartOptions = new ContextMenuSection("chartOptions", "Chart Options", new[]
        {
            Set("opt.chart.candles", "Candles", "ChartType", "Candles", state.ChartType == "Candles"),
            Set("opt.chart.hollow", "Hollow Candles", "ChartType", "HollowCandles", state.ChartType == "HollowCandles"),
            Set("opt.chart.ohlc", "OHLC", "ChartType", "OHLC", state.ChartType == "OHLC"),
            Set("opt.scale.auto", "Scale Auto", "ScaleFit", "Auto", state.ScaleFit == "Auto"),
            Set("opt.scale.manual", "Scale Manual", "ScaleFit", "Manual", state.ScaleFit == "Manual"),
            Set("opt.scale.log", "Scale Log", "ScaleTransform", "Log", state.ScaleTransform == "Log"),
            Set("opt.scale.pct", "Scale Percentage", "ScaleTransform", "Percentage", state.ScaleTransform == "Percentage"),
            Set("opt.axis.right", "Price Axis Right", "PriceAxis", "Right", state.PriceAxis == "Right"),
            Set("opt.axis.left", "Price Axis Left", "PriceAxis", "Left", state.PriceAxis == "Left"),
            Toggle("opt.grid.major", "Grid Major", "Grid.Major", state.GridMajor),
            Toggle("opt.grid.minor", "Grid Minor", "Grid.Minor", state.GridMinor),
            Toggle("opt.navigator", "Navigator", "Navigator.Visible", state.NavigatorVisible),
            Toggle("opt.hud", "HUD", "Hud.Visible", state.HudVisible),
        });

        var reset = new ContextMenuSection("reset", "Reset", new[]
        {
            Cmd("reset.view", "Reset View", "ResetView"),
            Cmd("reset.scaleAuto", "Reset Scale to Auto", "ResetScaleAuto"),
        });

        return new ContextMenuModel(MenuId, new[]
        {
            navigation, crosshair, drawing, indicator, chartOptions, reset
        });
    }

    private static ContextMenuItem Cmd(string id, string label, string target) =>
        new(id, label, MenuActionKind.Command, target);

    private static ContextMenuItem Toggle(string id, string label, string target, bool isChecked) =>
        new(id, label, MenuActionKind.Toggle, target, IsToggle: true, IsChecked: isChecked);

    private static ContextMenuItem Set(string id, string label, string target, string value, bool isChecked) =>
        new(id, label, MenuActionKind.SetState, target, Value: value, IsChecked: isChecked);
}

/// <summary>Current chart flags used to mark toggles/sets as checked.</summary>
public sealed class ChartMenuState
{
    public static ChartMenuState Defaults { get; } = new();

    public bool CrosshairAnalysis { get; init; }
    public bool CrosshairDataInspector { get; init; }
    public bool CrosshairMagnet { get; init; }
    public string ChartType { get; init; } = "Candles";
    public string ScaleFit { get; init; } = "Auto";
    public string ScaleTransform { get; init; } = "Linear";
    public string PriceAxis { get; init; } = "Right";
    public bool GridMajor { get; init; } = true;
    public bool GridMinor { get; init; }
    public bool NavigatorVisible { get; init; } = true;
    public bool HudVisible { get; init; } = true;
}
