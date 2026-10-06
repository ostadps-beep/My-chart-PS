namespace MyChart.Core.UI.Menus;

/// <summary>
/// T6.02 DRAWING MENU inventory: Edit, Lock/Unlock, Clone, Hide, Delete, Object Settings.
/// </summary>
public static class DrawingContextMenuBuilder
{
    public const string MenuId = "drawing.context";

    public static ContextMenuModel Build(bool isLocked = false, bool isHidden = false)
    {
        var items = new[]
        {
            new ContextMenuItem("dwg.edit", "Edit", MenuActionKind.Command, "Drawing.Edit"),
            new ContextMenuItem(
                "dwg.lock",
                isLocked ? "Unlock" : "Lock",
                MenuActionKind.Command,
                isLocked ? "Drawing.Unlock" : "Drawing.Lock"),
            new ContextMenuItem("dwg.clone", "Clone", MenuActionKind.Command, "Drawing.Clone"),
            new ContextMenuItem(
                "dwg.hide",
                isHidden ? "Show" : "Hide",
                MenuActionKind.Command,
                isHidden ? "Drawing.Show" : "Drawing.Hide"),
            new ContextMenuItem("dwg.delete", "Delete", MenuActionKind.Command, "Drawing.Delete"),
            new ContextMenuItem("dwg.settings", "Object Settings", MenuActionKind.Command, "Drawing.ObjectSettings"),
        };

        return new ContextMenuModel(MenuId, new[]
        {
            new ContextMenuSection("drawing", "Drawing", items)
        });
    }
}
