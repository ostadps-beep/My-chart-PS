using System.Text.Json.Nodes;

namespace MyChart.Core.Plugins.Vocabulary;

public sealed class ToolDefinition
{
    public required string Schema { get; init; }
    public required string VocabularyVersion { get; init; }
    public int Anchors { get; init; }
    public string Workflow { get; init; } = "Click";
    public IReadOnlyList<NamedExpression> Named { get; init; } = Array.Empty<NamedExpression>();
    public IReadOnlyList<JsonObject> Shapes { get; init; } = Array.Empty<JsonObject>();
    public JsonObject Raw { get; init; } = new();
}

public sealed record NamedExpression(string Name, string Expr);
