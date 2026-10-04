using MyChart.Core.Models.Drawing;
using MyChart.Core.Models.Geometry;

namespace MyChart.Core.Contracts.Plugins;

public interface IDrawingHitTester
{
    HitResult HitTest(DrawingObject obj, PointD p, IChartMapper map, double toleranceDip);
}
