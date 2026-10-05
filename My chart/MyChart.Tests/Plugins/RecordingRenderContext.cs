using MyChart.Core.Contracts.Rendering;
using MyChart.Core.Models.Geometry;
using MyChart.Core.Models.Rendering;

namespace MyChart.Tests.Plugins;

/// <summary>
/// Test double for IRenderContext. Records operations and layer calls for pipeline VERIFY.
/// </summary>
public sealed class RecordingRenderContext : IRenderContext
{
    public int Width { get; set; } = 800;
    public int Height { get; set; } = 600;
    public double DpiScale { get; set; } = 1.0;

    public int DrawLineCount { get; private set; }
    public int DrawPathCount { get; private set; }
    public List<string> Operations { get; } = new();
    public List<RenderLayer> LayerCalls { get; } = new();

    public void Clear(RgbaColor color) => Operations.Add("Clear");
    public void FillRect(int x, int y, int w, int h, RgbaColor color) => Operations.Add("FillRect");
    public void StrokeRect(int x, int y, int w, int h, int thickness, RgbaColor color) => Operations.Add("StrokeRect");

    public void DrawLine(double x1, double y1, double x2, double y2, RgbaColor color, double width, double[]? dash)
    {
        DrawLineCount++;
        Operations.Add($"DrawLine:{x1},{y1}->{x2},{y2}");
    }

    public void DrawText(string text, double x, double y, TextStyle style, RgbaColor color, TextAlign align)
        => Operations.Add("DrawText:" + text);

    public void DrawPath(IReadOnlyList<PointD> points, RgbaColor color, double width, bool closed, bool fill)
    {
        DrawPathCount++;
        Operations.Add("DrawPath:" + points.Count);
    }

    public SizeD MeasureText(string text, TextStyle style) => new(text.Length * 6.0, 12.0);
    public void PushClip(RectD rect) => Operations.Add("PushClip");
    public void PopClip() => Operations.Add("PopClip");

    /// <summary>Record that a given layer was issued (used by RenderPipeline tests).</summary>
    public void NoteLayer(RenderLayer layer) => LayerCalls.Add(layer);
}
