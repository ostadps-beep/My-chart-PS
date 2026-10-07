using MyChart.Core.Models.Plugins;

namespace MyChart.Core.UI.Toolbar;

/// <summary>Built-in ContextToolbar actions (orders 0..70 per Spec).</summary>
public enum ContextToolbarAction
{
    Color = 0,
    Width = 10,
    Opacity = 20,
    Style = 30,
    Template = 40,
    Lock = 50,
    Clone = 60,
    Delete = 70
}

/// <summary>One button on the floating context toolbar (slot-driven).</summary>
public sealed record ContextToolbarItem(
    ContextToolbarAction Action,
    string ItemId,
    string IconKey,
    int Order,
    string CommandRef);

/// <summary>
/// Placement of the floating bar relative to the selection bounds (DIP).
/// Gap = ContextToolbarGapDip = 8.
/// Preferred: 8 DIP above the selection; if no room, 8 DIP below.
/// </summary>
public enum ContextToolbarPlacement
{
    Above,
    Below
}

/// <summary>
/// T6.05 ContextToolbar — visible only when selection is not empty.
/// Floats ContextToolbarGapDip (8) above selection bounds; below when no room.
/// Items: Color, Width, Opacity, Style, Template, Lock, Clone, Delete.
/// Every style change is an EditDrawing command; Clone/Delete use their commands.
/// </summary>
public sealed class ContextToolbarModel
{
    public const double GapDip = 8;
    public const double ButtonSizeDip = 32;

    public IReadOnlyList<ContextToolbarItem> Items { get; }
    public bool IsVisible { get; }
    public ContextToolbarPlacement Placement { get; }
    public double AnchorXDip { get; }
    public double AnchorYDip { get; }

    public ContextToolbarModel(
        IReadOnlyList<ContextToolbarItem> items,
        bool isVisible,
        ContextToolbarPlacement placement,
        double anchorXDip,
        double anchorYDip)
    {
        Items = items;
        IsVisible = isVisible;
        Placement = placement;
        AnchorXDip = anchorXDip;
        AnchorYDip = anchorYDip;
    }

    public static IReadOnlyList<SlotContribution> CoreContributions { get; } = new[]
    {
        new SlotContribution(Slot.ContextToolbar, "ctx:color", "Icon.Color", 0, "style", "drawing.edit.color"),
        new SlotContribution(Slot.ContextToolbar, "ctx:width", "Icon.Width", 10, "style", "drawing.edit.width"),
        new SlotContribution(Slot.ContextToolbar, "ctx:opacity", "Icon.Opacity", 20, "style", "drawing.edit.opacity"),
        new SlotContribution(Slot.ContextToolbar, "ctx:style", "Icon.Style", 30, "style", "drawing.edit.style"),
        new SlotContribution(Slot.ContextToolbar, "ctx:template", "Icon.Template", 40, "style", "drawing.edit.template"),
        new SlotContribution(Slot.ContextToolbar, "ctx:lock", "Icon.Lock", 50, "object", "drawing.edit.lock"),
        new SlotContribution(Slot.ContextToolbar, "ctx:clone", "Icon.Clone", 60, "object", "drawing.clone"),
        new SlotContribution(Slot.ContextToolbar, "ctx:delete", "Icon.Delete", 70, "object", "drawing.delete"),
    };

    public static IReadOnlyList<ContextToolbarItem> DefaultItems { get; } = new[]
    {
        new ContextToolbarItem(ContextToolbarAction.Color, "ctx:color", "Icon.Color", 0, "drawing.edit.color"),
        new ContextToolbarItem(ContextToolbarAction.Width, "ctx:width", "Icon.Width", 10, "drawing.edit.width"),
        new ContextToolbarItem(ContextToolbarAction.Opacity, "ctx:opacity", "Icon.Opacity", 20, "drawing.edit.opacity"),
        new ContextToolbarItem(ContextToolbarAction.Style, "ctx:style", "Icon.Style", 30, "drawing.edit.style"),
        new ContextToolbarItem(ContextToolbarAction.Template, "ctx:template", "Icon.Template", 40, "drawing.edit.template"),
        new ContextToolbarItem(ContextToolbarAction.Lock, "ctx:lock", "Icon.Lock", 50, "drawing.edit.lock"),
        new ContextToolbarItem(ContextToolbarAction.Clone, "ctx:clone", "Icon.Clone", 60, "drawing.clone"),
        new ContextToolbarItem(ContextToolbarAction.Delete, "ctx:delete", "Icon.Delete", 70, "drawing.delete"),
    };

    /// <summary>
    /// Build from contributions. Hidden when selectionCount == 0.
    /// selectionTop/Bottom/CenterX in DIP; viewportTop/Bottom define available space.
    /// </summary>
    public static ContextToolbarModel Create(
        int selectionCount,
        double selectionLeftDip,
        double selectionTopDip,
        double selectionRightDip,
        double selectionBottomDip,
        double viewportTopDip,
        double viewportBottomDip,
        IEnumerable<SlotContribution>? contributions = null)
    {
        var items = (contributions ?? CoreContributions)
            .Where(c => c.Slot == Slot.ContextToolbar)
            .OrderBy(c => c.Order)
            .ThenBy(c => c.ItemId, StringComparer.Ordinal)
            .Select(MapContribution)
            .ToList();

        if (items.Count == 0)
            items = DefaultItems.ToList();

        var visible = selectionCount > 0;
        var centerX = (selectionLeftDip + selectionRightDip) * 0.5;
        var barHeight = ButtonSizeDip;

        // Prefer above: selectionTop - Gap - barHeight >= viewportTop
        var aboveY = selectionTopDip - GapDip - barHeight;
        ContextToolbarPlacement placement;
        double anchorY;
        if (visible && aboveY >= viewportTopDip)
        {
            placement = ContextToolbarPlacement.Above;
            anchorY = aboveY;
        }
        else
        {
            placement = ContextToolbarPlacement.Below;
            anchorY = selectionBottomDip + GapDip;
            if (anchorY + barHeight > viewportBottomDip)
                anchorY = Math.Max(viewportTopDip, viewportBottomDip - barHeight);
        }

        return new ContextToolbarModel(items, visible, placement, centerX, anchorY);
    }

    private static ContextToolbarItem MapContribution(SlotContribution c)
    {
        var action = c.Order switch
        {
            0 => ContextToolbarAction.Color,
            10 => ContextToolbarAction.Width,
            20 => ContextToolbarAction.Opacity,
            30 => ContextToolbarAction.Style,
            40 => ContextToolbarAction.Template,
            50 => ContextToolbarAction.Lock,
            60 => ContextToolbarAction.Clone,
            70 => ContextToolbarAction.Delete,
            _ => ContextToolbarAction.Color
        };
        return new ContextToolbarItem(action, c.ItemId, c.IconKey, c.Order, c.CommandRef);
    }
}

/// <summary>
/// T6.05 controller: records requested command kind for each click.
/// Actual EditDrawing / Clone / Delete are executed by the command bus (T3.06).
/// </summary>
public sealed class ContextToolbarController
{
    private readonly List<string> _commands = new();

    public IReadOnlyList<string> IssuedCommands => _commands;

    public bool TryIssue(ContextToolbarItem item, int selectionCount)
    {
        if (selectionCount <= 0)
            return false;

        var name = item.Action switch
        {
            ContextToolbarAction.Color => "EditDrawing:Color",
            ContextToolbarAction.Width => "EditDrawing:Width",
            ContextToolbarAction.Opacity => "EditDrawing:Opacity",
            ContextToolbarAction.Style => "EditDrawing:Style",
            ContextToolbarAction.Template => "EditDrawing:Template",
            ContextToolbarAction.Lock => "EditDrawing:Lock",
            ContextToolbarAction.Clone => "CloneDrawing",
            ContextToolbarAction.Delete => "RemoveDrawings",
            _ => item.CommandRef
        };
        _commands.Add(name);
        return true;
    }

    public void Clear() => _commands.Clear();
}
