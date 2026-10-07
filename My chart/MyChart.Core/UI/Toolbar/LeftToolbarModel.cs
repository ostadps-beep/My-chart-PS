using MyChart.Core.Models.Plugins;

namespace MyChart.Core.UI.Toolbar;

/// <summary>Kind of an item that can appear on the left toolbar.</summary>
public enum LeftToolbarItemKind
{
    Cursor,
    Crosshair,
    DrawingTool
}

/// <summary>One button on the left toolbar (slot-driven; no hardcoded tool names in UI).</summary>
public sealed record LeftToolbarItem(
    LeftToolbarItemKind Kind,
    string ItemId,
    string IconKey,
    int Order,
    string Group,
    string CommandRef,
    bool IsSelected = false);

/// <summary>
/// T6.04 LeftToolbar model — width 36 DIP, buttons 32x32.
/// Items come from LeftToolbar SlotContributions sorted by (Order, ItemId).
/// Core always contributes Cursor (0) and Crosshair (10).
/// First-party tools: TrendLine 100, Rectangle 110, Arrow 120, Text 130, Fibonacci 140, Measure 150.
/// UI contains no tool name and no icon geometry — only IconKey + CommandRef from contributions.
/// </summary>
public sealed class LeftToolbarModel
{
    public const double WidthDip = 36;
    public const double ButtonSizeDip = 32;

    public IReadOnlyList<LeftToolbarItem> Items { get; }

    public LeftToolbarModel(IReadOnlyList<LeftToolbarItem> items)
    {
        Items = items;
    }

    /// <summary>Core-owned contributions that are always present.</summary>
    public static IReadOnlyList<SlotContribution> CoreContributions { get; } = new[]
    {
        new SlotContribution(Slot.LeftToolbar, "core:cursor", "Icon.Cursor", 0, "core", "core.cursor"),
        new SlotContribution(Slot.LeftToolbar, "core:crosshair", "Icon.Crosshair", 10, "core", "core.crosshair"),
    };

    /// <summary>Canonical first-party tool orders from the spec (PG5 / T6.04).</summary>
    public static IReadOnlyDictionary<string, int> FirstPartyToolOrders { get; } =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["TrendLine"] = 100,
            ["Rectangle"] = 110,
            ["Arrow"] = 120,
            ["Text"] = 130,
            ["Fibonacci"] = 140,
            ["Measure"] = 150,
        };

    /// <summary>
    /// Build the left toolbar from registry contributions for Slot.LeftToolbar.
    /// If core contributions are missing they are injected so Cursor/Crosshair are always present.
    /// </summary>
    public static LeftToolbarModel FromContributions(
        IEnumerable<SlotContribution> contributions,
        string? selectedItemId = null)
    {
        var list = contributions
            .Where(c => c.Slot == Slot.LeftToolbar)
            .ToList();

        foreach (var core in CoreContributions)
        {
            if (!list.Any(c => c.ItemId == core.ItemId))
                list.Add(core);
        }

        var sorted = list
            .OrderBy(c => c.Order)
            .ThenBy(c => c.ItemId, StringComparer.Ordinal)
            .Select(c =>
            {
                var kind = c.ItemId switch
                {
                    "core:cursor" => LeftToolbarItemKind.Cursor,
                    "core:crosshair" => LeftToolbarItemKind.Crosshair,
                    _ => LeftToolbarItemKind.DrawingTool
                };
                var selected = selectedItemId is not null
                    && string.Equals(c.ItemId, selectedItemId, StringComparison.Ordinal);
                return new LeftToolbarItem(kind, c.ItemId, c.IconKey, c.Order, c.Group, c.CommandRef, selected);
            })
            .ToList();

        return new LeftToolbarModel(sorted);
    }

    /// <summary>Default inventory with core + six first-party tools (no plugin host required).</summary>
    public static LeftToolbarModel CreateDefault(string? selectedItemId = "core:cursor")
    {
        var extras = FirstPartyToolOrders.Select(kv =>
            new SlotContribution(
                Slot.LeftToolbar,
                $"tool:{kv.Key}",
                $"Icon.{kv.Key}",
                kv.Value,
                "tools",
                $"tool.{kv.Key}"));

        return FromContributions(CoreContributions.Concat(extras), selectedItemId);
    }
}

/// <summary>Flyout options on the Crosshair button (T6.04).</summary>
public enum CrosshairFlyoutOption
{
    Analysis,
    DataInspector,
    Magnet
}

/// <summary>
/// T6.04 interaction: select tool, toggle crosshair, flyout, Esc cancel, return to Cursor after commit.
/// </summary>
public sealed class LeftToolbarController
{
    private string _selectedId = "core:cursor";
    private bool _crosshairActive;
    private bool _flyoutOpen;
    private CrosshairFlyoutOption? _flyoutSelection;
    private readonly List<string> _events = new();

    public string SelectedItemId => _selectedId;
    public bool CrosshairActive => _crosshairActive;
    public bool FlyoutOpen => _flyoutOpen;
    public CrosshairFlyoutOption? FlyoutSelection => _flyoutSelection;
    public IReadOnlyList<string> Events => _events;

    public bool Select(LeftToolbarItem item)
    {
        if (item.Kind == LeftToolbarItemKind.Crosshair)
        {
            _crosshairActive = !_crosshairActive;
            _events.Add($"crosshair:toggle:{_crosshairActive}");
            return true;
        }

        _selectedId = item.ItemId;
        _flyoutOpen = false;
        _events.Add($"select:{item.ItemId}");
        return true;
    }

    public bool OpenCrosshairFlyout()
    {
        _flyoutOpen = true;
        _events.Add("crosshair:flyout:open");
        return true;
    }

    public bool ApplyFlyout(CrosshairFlyoutOption option)
    {
        if (!_flyoutOpen)
            return false;
        _flyoutSelection = option;
        _flyoutOpen = false;
        _events.Add($"crosshair:flyout:{option}");
        return true;
    }

    public void CloseFlyout() => _flyoutOpen = false;

    /// <summary>Esc cancels the active drawing tool and returns to Cursor.</summary>
    public void Cancel()
    {
        _selectedId = "core:cursor";
        _flyoutOpen = false;
        _events.Add("cancel");
    }

    /// <summary>After a drawing tool commits, selection returns to Cursor.</summary>
    public void OnToolCommitted()
    {
        _selectedId = "core:cursor";
        _events.Add("commit->cursor");
    }
}
