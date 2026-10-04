using MyChart.Core.Models.Drawing;

namespace MyChart.Core.Contracts.Plugins;

public interface IDrawingTool
{
    string ToolId { get; }
    void Activate();
    void Deactivate();
    ToolResult OnPointer(PointerEvent e);
    ToolResult OnKey(KeyEvent e);
    DrawingObject? Preview { get; }
}
