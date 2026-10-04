using MyChart.Core.Contracts.Plugins;
using MyChart.Core.Models.Drawing;

namespace MyChart.Core.Plugins.Base;

public abstract class SinglePointToolBase : IDrawingTool
{
    public abstract string ToolId { get; }
    public DrawingObject? Preview { get; protected set; }

    public virtual void Activate() { }
    public virtual void Deactivate()
    {
        Preview = null;
    }

    public abstract ToolResult OnPointer(PointerEvent e);
    public abstract ToolResult OnKey(KeyEvent e);
}
