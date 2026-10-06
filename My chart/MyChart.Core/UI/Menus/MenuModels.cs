namespace MyChart.Core.UI.Menus;

/// <summary>T6.02 — how a menu item is applied.</summary>
public enum MenuActionKind
{
    /// <summary>Maps to IChartCommand / CommandBus.</summary>
    Command,

    /// <summary>Boolean toggle on chart session state.</summary>
    Toggle,

    /// <summary>Set a discrete value (chart type, scale mode, …).</summary>
    SetState
}

/// <summary>One entry in a context menu. UI must not invent items outside the inventory.</summary>
public sealed record ContextMenuItem(
    string Id,
    string Label,
    MenuActionKind Kind,
    string Target,
    string? Value = null,
    bool IsToggle = false,
    bool IsChecked = false,
    bool IsEnabled = true);

public sealed record ContextMenuSection(
    string Id,
    string Header,
    IReadOnlyList<ContextMenuItem> Items);

public sealed record ContextMenuModel(
    string MenuId,
    IReadOnlyList<ContextMenuSection> Sections)
{
    public IEnumerable<ContextMenuItem> AllItems()
        => Sections.SelectMany(s => s.Items);
}
