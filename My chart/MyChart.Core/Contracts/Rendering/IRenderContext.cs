using MyChart.Core.Models.Geometry;
using MyChart.Core.Models.Rendering;

namespace MyChart.Core.Contracts.Rendering;

public interface IRenderContext
{
    int Width { get; }
    int Height { get; }
    double DpiScale { get; }

    void Clear(RgbaColor color);
    void FillRect(int x, int y, int w, int h, RgbaColor color);
    void StrokeRect(int x, int y, int w, int h, int thickness, RgbaColor color);
    void DrawLine(double x1, double y1, double x2, double y2, RgbaColor color, double width, double[]? dash);
    void DrawText(string text, double x, double y, TextStyle style, RgbaColor color, TextAlign align);
    void DrawPath(IReadOnlyList<PointD> points, RgbaColor color, double width, bool closed, bool fill);
    SizeD MeasureText(string text, TextStyle style);
    void PushClip(RectD rect);
    void PopClip();
}
