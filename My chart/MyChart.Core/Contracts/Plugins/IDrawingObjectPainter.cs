using MyChart.Core.Models.Drawing;

namespace MyChart.Core.Contracts.Plugins;

public interface IDrawingObjectPainter
{
    void Paint(DrawContext ctx, DrawingObject obj);
}
