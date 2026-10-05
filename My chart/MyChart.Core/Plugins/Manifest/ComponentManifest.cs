namespace MyChart.Core.Plugins.Manifest;

/// <summary>
/// Component Manifest.json model (PG3.02).
/// Property names match JSON camelCase used by UserComponentLoader.
/// </summary>
public sealed class ComponentManifest
{
    public string ComponentId { get; set; } = "";
    public string Kind { get; set; } = "DataTool"; // DataTool | CodeTool | Icon
    public string Name { get; set; } = "";
    public string Version { get; set; } = "1.0.0";
    public string ContractVersion { get; set; } = "1.0";
    public string? ToolId { get; set; }
    public string? TypeId { get; set; }
    public string? IconKey { get; set; }
    public string? Hotkey { get; set; }
    public string Category { get; set; } = "Drawing";
    public int Anchors { get; set; } = 2;
    public bool Active { get; set; } = true;
    public int Order { get; set; }
    public string? Slot { get; set; }
    public List<string> DependsOn { get; set; } = new();
}
