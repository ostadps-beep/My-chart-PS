using MyChart.Core.Models.Market;

namespace MyChart.Core.UI.Toolbar;

/// <summary>T6.03 — one control on the top toolbar.</summary>
public enum TopToolbarItemKind
{
    Symbol,
    Timeframe,
    ChartType,
    Indicators,
    Templates,
    Layout,
    Settings
}

/// <summary>How the control opens content (always one popup; apply on second click).</summary>
public enum ToolbarPopupKind
{
    SearchList,
    QuickButtonsWithDropdown,
    ChoiceList,
    Dialog,
    ActionList,
    Window
}

public sealed record ToolbarChoice(string Id, string Label, string Value, bool IsQuick = false);

public sealed record TopToolbarItem(
    TopToolbarItemKind Kind,
    string Id,
    string Label,
    ToolbarPopupKind Popup,
    IReadOnlyList<ToolbarChoice> Choices,
    bool IsEnabled = true,
    string? SelectedValue = null);

/// <summary>
/// T6.03 TopToolbar model — height 32 DIP; order fixed; each item one popup, apply on second interaction.
/// Settings disabled until T6.08.
/// </summary>
public sealed class TopToolbarModel
{
    public const double HeightDip = 32;

    public IReadOnlyList<TopToolbarItem> Items { get; }

    public TopToolbarModel(IReadOnlyList<TopToolbarItem> items)
    {
        Items = items;
    }

    public static TopToolbarModel CreateDefault(
        string? selectedSymbol = null,
        Timeframe selectedTf = Timeframe.M15,
        string chartType = "Candles",
        int layoutPanels = 1,
        bool settingsAvailable = false)
    {
        var tfChoices = new[]
        {
            new ToolbarChoice("tf.m1", "M1", "M1", IsQuick: true),
            new ToolbarChoice("tf.m5", "M5", "M5", IsQuick: true),
            new ToolbarChoice("tf.m15", "M15", "M15", IsQuick: true),
            new ToolbarChoice("tf.h1", "H1", "H1", IsQuick: true),
            new ToolbarChoice("tf.h4", "H4", "H4", IsQuick: true),
            new ToolbarChoice("tf.d1", "D1", "D1", IsQuick: true),
            new ToolbarChoice("tf.m30", "M30", "M30", IsQuick: false),
            new ToolbarChoice("tf.w1", "W1", "W1", IsQuick: false),
            new ToolbarChoice("tf.mn1", "MN1", "MN1", IsQuick: false),
        };

        var chartChoices = new[]
        {
            new ToolbarChoice("ct.candles", "Candles", "Candles"),
            new ToolbarChoice("ct.hollow", "Hollow Candles", "HollowCandles"),
            new ToolbarChoice("ct.ohlc", "OHLC", "OHLC"),
        };

        var layoutChoices = new[]
        {
            new ToolbarChoice("ly.1", "1", "1"),
            new ToolbarChoice("ly.2", "2", "2"),
            new ToolbarChoice("ly.4", "4", "4"),
            new ToolbarChoice("ly.6", "6", "6"),
            new ToolbarChoice("ly.8", "8", "8"),
        };

        var templateChoices = new[]
        {
            new ToolbarChoice("tpl.save", "Save Template...", "Save"),
            new ToolbarChoice("tpl.load", "Load Template...", "Load"),
        };

        var items = new[]
        {
            new TopToolbarItem(
                TopToolbarItemKind.Symbol, "tb.symbol", "Symbol",
                ToolbarPopupKind.SearchList,
                Array.Empty<ToolbarChoice>(),
                SelectedValue: selectedSymbol),

            new TopToolbarItem(
                TopToolbarItemKind.Timeframe, "tb.timeframe", "Timeframe",
                ToolbarPopupKind.QuickButtonsWithDropdown,
                tfChoices,
                SelectedValue: selectedTf.ToString()),

            new TopToolbarItem(
                TopToolbarItemKind.ChartType, "tb.chartType", "Chart Type",
                ToolbarPopupKind.ChoiceList,
                chartChoices,
                SelectedValue: chartType),

            new TopToolbarItem(
                TopToolbarItemKind.Indicators, "tb.indicators", "Indicators",
                ToolbarPopupKind.Dialog,
                Array.Empty<ToolbarChoice>()),

            new TopToolbarItem(
                TopToolbarItemKind.Templates, "tb.templates", "Templates",
                ToolbarPopupKind.ActionList,
                templateChoices),

            new TopToolbarItem(
                TopToolbarItemKind.Layout, "tb.layout", "Layout",
                ToolbarPopupKind.ChoiceList,
                layoutChoices,
                SelectedValue: layoutPanels.ToString()),

            new TopToolbarItem(
                TopToolbarItemKind.Settings, "tb.settings", "Settings",
                ToolbarPopupKind.Window,
                Array.Empty<ToolbarChoice>(),
                IsEnabled: settingsAvailable),
        };

        return new TopToolbarModel(items);
    }
}

/// <summary>
/// T6.03 interaction rule: first click opens one popup; second click applies.
/// </summary>
public sealed class TopToolbarController
{
    private TopToolbarItemKind? _openPopup;
    private readonly List<string> _applied = new();

    public TopToolbarItemKind? OpenPopup => _openPopup;
    public IReadOnlyList<string> AppliedActions => _applied;

    /// <summary>Click on a toolbar button. Opens its popup (closes any other).</summary>
    public bool Open(TopToolbarItem item)
    {
        if (!item.IsEnabled)
            return false;
        _openPopup = item.Kind;
        return true;
    }

    /// <summary>Second interaction: apply a choice/value while popup is open.</summary>
    public bool Apply(TopToolbarItem item, string value)
    {
        if (!item.IsEnabled)
            return false;
        if (_openPopup != item.Kind)
            return false;

        _applied.Add($"{item.Kind}:{value}");
        _openPopup = null;
        return true;
    }

    public void Close() => _openPopup = null;
}
