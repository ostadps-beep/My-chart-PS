namespace MyChart.Core.Models.Plugins;

public sealed record ToolDescriptor(
    string ComponentId,
    string ToolId,
    string DisplayName,
    string Category,
    string IconKey,
    string? Hotkey,
    string TypeId,
    int AnchorCount,
    IReadOnlyList<ParameterDescriptor> Parameters);
