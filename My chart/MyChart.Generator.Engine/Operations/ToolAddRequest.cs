namespace MyChart.Generator.Engine.Operations;

public enum ComponentTarget
{
    Repo,
    User
}

public enum ToolKind
{
    DataTool,
    CodeTool
}

public sealed class ToolAddRequest
{
    public required string ComponentId { get; init; }
    public required string Name { get; init; }
    public ToolKind Kind { get; init; } = ToolKind.DataTool;
    public string Category { get; init; } = "Drawing";
    public int Anchors { get; init; } = 2;
    public string? IconKey { get; init; }
    public string? Hotkey { get; init; }
    public string? TypeId { get; init; }
    public string? ToolId { get; init; }
    public bool ClickThenText { get; init; }
    public ComponentTarget Target { get; init; } = ComponentTarget.Repo;
    public bool DryRun { get; init; }
    public bool AllowDirty { get; init; }
}

public sealed class IconAddRequest
{
    public required string ComponentId { get; init; }
    public required string Name { get; init; }
    public required string IconKey { get; init; }
    public required string GeometryPathData { get; init; }
    public ComponentTarget Target { get; init; } = ComponentTarget.Repo;
    public bool DryRun { get; init; }
    public bool AllowDirty { get; init; }
}
