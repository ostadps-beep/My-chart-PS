using MyChart.Core.Contracts.Rendering;
using MyChart.Core.Models.Geometry;
using MyChart.Core.Models.Rendering;
using SkiaSharp;

namespace MyChart.Rendering.Skia;

/// <summary>
/// IRenderContext backed by an SKCanvas. All coordinates are device pixels.
/// Callers multiply DIP by DpiScale before calling.
/// </summary>
public sealed class SkiaRenderContext : IRenderContext, IDisposable
{
    private readonly SKCanvas _canvas;
    private readonly PaintCache _paints;
    private readonly Stack<int> _clipSaveCount = new();
    private readonly bool _ownsPaints;
    private bool _disposed;

    /// <summary>
    /// When true, lines/text/paths are antialiased.
    /// Candles (FillRect/StrokeRect for bodies) never use antialiasing.
    /// </summary>
    public bool AntiAlias { get; set; } = true;

    public int Width { get; }
    public int Height { get; }
    public double DpiScale { get; }

    public SkiaRenderContext(SKCanvas canvas, int width, int height, double dpiScale, PaintCache? paints = null)
    {
        _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
        Width = width;
        Height = height;
        DpiScale = dpiScale <= 0 ? 1.0 : dpiScale;
        if (paints is null)
        {
            _paints = new PaintCache();
            _ownsPaints = true;
        }
        else
        {
            _paints = paints;
            _ownsPaints = false;
        }
    }

    public void Clear(RgbaColor color) => _canvas.Clear(PaintCache.ToSkColor(color));

    public void FillRect(int x, int y, int w, int h, RgbaColor color)
    {
        // Candles and solid fills: no antialiasing (spec).
        var paint = _paints.GetFill(color, antiAlias: false);
        _canvas.DrawRect(x, y, w, h, paint);
    }

    public void StrokeRect(int x, int y, int w, int h, int thickness, RgbaColor color)
    {
        var paint = _paints.GetStroke(color, thickness, antiAlias: false);
        _canvas.DrawRect(x + thickness / 2f, y + thickness / 2f, Math.Max(0, w - thickness), Math.Max(0, h - thickness), paint);
    }

    public void DrawLine(double x1, double y1, double x2, double y2, RgbaColor color, double width, double[]? dash)
    {
        if (dash is { Length: > 0 })
        {
            // Temporary paint so PathEffect does not pollute the cache.
            using var paint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = PaintCache.ToSkColor(color),
                StrokeWidth = (float)width,
                IsAntialias = AntiAlias,
                PathEffect = SKPathEffect.CreateDash(Array.ConvertAll(dash, d => (float)d), 0)
            };
            _canvas.DrawLine((float)x1, (float)y1, (float)x2, (float)y2, paint);
        }
        else
        {
            var paint = _paints.GetStroke(color, (float)width, AntiAlias);
            _canvas.DrawLine((float)x1, (float)y1, (float)x2, (float)y2, paint);
        }
    }

    public void DrawText(string text, double x, double y, TextStyle style, RgbaColor color, TextAlign align)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(style);
        var font = _paints.GetFont(style, DpiScale);
        var paint = _paints.GetFill(color, AntiAlias);

        var width = font.MeasureText(text);
        var drawX = align switch
        {
            TextAlign.Center => (float)x - width / 2f,
            TextAlign.Right => (float)x - width,
            _ => (float)x
        };
        font.GetFontMetrics(out var metrics);
        var baseline = (float)y - metrics.Ascent;
        _canvas.DrawText(text, drawX, baseline, font, paint);
    }

    public void DrawPath(IReadOnlyList<PointD> points, RgbaColor color, double width, bool closed, bool fill)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count < 2)
            return;

        using var path = new SKPath();
        path.MoveTo((float)points[0].X, (float)points[0].Y);
        for (var i = 1; i < points.Count; i++)
            path.LineTo((float)points[i].X, (float)points[i].Y);
        if (closed)
            path.Close();

        if (fill)
        {
            var fillPaint = _paints.GetFill(color, AntiAlias);
            _canvas.DrawPath(path, fillPaint);
        }
        else
        {
            var stroke = _paints.GetStroke(color, (float)width, AntiAlias);
            _canvas.DrawPath(path, stroke);
        }
    }

    public SizeD MeasureText(string text, TextStyle style)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(style);
        var font = _paints.GetFont(style, DpiScale);
        var width = font.MeasureText(text);
        font.GetFontMetrics(out var metrics);
        var height = metrics.Descent - metrics.Ascent;
        return new SizeD(width, height);
    }

    public void PushClip(RectD rect)
    {
        var save = _canvas.Save();
        _clipSaveCount.Push(save);
        _canvas.ClipRect(new SKRect((float)rect.X, (float)rect.Y, (float)rect.Right, (float)rect.Bottom));
    }

    public void PopClip()
    {
        if (_clipSaveCount.Count == 0)
            return;
        _canvas.RestoreToCount(_clipSaveCount.Pop());
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        if (_ownsPaints)
            _paints.Dispose();
    }
}
