namespace MyChart.Core.Models.Plugins;

public enum Slot
{
    LeftToolbar,
    TopToolbar,
    ContextToolbar,
    ChartContextMenu,
    DrawingContextMenu
}

public sealed record SlotContribution(
    Slot Slot,
    string ItemId,
    string IconKey,
    int Order,
    string Group,
    string CommandRef);
