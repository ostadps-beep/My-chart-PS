using MyChart.Core.Contracts.Plugins;
using MyChart.Core.Models.Drawing;

namespace MyChart.Core.Plugins.Base;

/// <summary>
/// Base for tools that collect N clicks. Preview follows the cursor; Esc cancels.
/// </summary>
public abstract class ClickToolBase : IDrawingTool
{
    public abstract string ToolId { get; }
    public abstract int RequiredClicks { get; }
    public DrawingObject? Preview { get; protected set; }

    public virtual void Activate() { }
    public virtual void Deactivate()
    {
        Preview = null;
    }

    public abstract ToolResult OnPointer(PointerEvent e);
    public abstract ToolResult OnKey(KeyEvent e);
}
