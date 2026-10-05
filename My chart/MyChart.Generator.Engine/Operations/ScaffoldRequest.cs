namespace MyChart.Generator.Engine.Operations;

/// <summary>Inputs for PG3.03 Scaffolder. Placeholders: ComponentId, Name, TypeId, IconKey, Anchors.</summary>
public sealed class ScaffoldRequest
{
    public required ScaffoldKind Kind { get; init; }
    public required string ComponentId { get; init; }
    public required string Name { get; init; }
    public string? TypeId { get; init; }
    public string? IconKey { get; init; }
    public string? ToolId { get; init; }
    public int Anchors { get; init; } = 2;
}
