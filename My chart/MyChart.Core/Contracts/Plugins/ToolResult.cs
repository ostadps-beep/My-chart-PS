using MyChart.Core.Models.Drawing;

namespace MyChart.Core.Contracts.Plugins;

public abstract record ToolResult
{
    public sealed record None : ToolResult;
    public sealed record Commit(DrawingObject Object) : ToolResult;
    public sealed record Cancel : ToolResult;
}
