using MyChart.Core.Contracts.Rendering;
using MyChart.Core.Models.Geometry;
using MyChart.Core.Models.Rendering;
using SkiaSharp;

namespace MyChart.Rendering.Skia;

/// <summary>
/// Headless IRenderContext that draws into an SKBitmap and can export deterministic PNG bytes.
/// Used by VisualProof for GATE=VISUAL nodes.
/// </summary>
public sealed class SkiaPngRenderContext : IRenderContext, IDisposable
{
    private readonly SKBitmap _bitmap;
    private readonly SKCanvas _canvas;
    private readonly SkiaRenderContext _inner;
    private bool _disposed;

    public int Width { get; }
    public int Height { get; }
    public double DpiScale { get; }
    public bool AntiAlias
    {
        get => _inner.AntiAlias;
        set => _inner.AntiAlias = value;
    }

    public SkiaPngRenderContext(int width, int height, double dpiScale = 1.0, bool antiAlias = true)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height));
        Width = width;
        Height = height;
        DpiScale = dpiScale <= 0 ? 1.0 : dpiScale;
        _bitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        _canvas = new SKCanvas(_bitmap);
        _inner = new SkiaRenderContext(_canvas, width, height, DpiScale);
        _inner.AntiAlias = antiAlias;
    }

    public void Clear(RgbaColor color) => _inner.Clear(color);
    public void FillRect(int x, int y, int w, int h, RgbaColor color) => _inner.FillRect(x, y, w, h, color);
    public void StrokeRect(int x, int y, int w, int h, int thickness, RgbaColor color) => _inner.StrokeRect(x, y, w, h, thickness, color);
    public void DrawLine(double x1, double y1, double x2, double y2, RgbaColor color, double width, double[]? dash) =>
        _inner.DrawLine(x1, y1, x2, y2, color, width, dash);
    public void DrawText(string text, double x, double y, TextStyle style, RgbaColor color, TextAlign align) =>
        _inner.DrawText(text, x, y, style, color, align);
    public void DrawPath(IReadOnlyList<PointD> points, RgbaColor color, double width, bool closed, bool fill) =>
        _inner.DrawPath(points, color, width, closed, fill);
    public SizeD MeasureText(string text, TextStyle style) => _inner.MeasureText(text, style);
    public void PushClip(RectD rect) => _inner.PushClip(rect);
    public void PopClip() => _inner.PopClip();

    /// <summary>Encode the current bitmap as PNG (deterministic for the same pixel buffer).</summary>
    public byte[] ToPng()
    {
        _canvas.Flush();
        using var image = SKImage.FromBitmap(_bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    /// <summary>Write PNG to a file path.</summary>
    public void SavePng(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllBytes(path, ToPng());
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _inner.Dispose();
        _canvas.Dispose();
        _bitmap.Dispose();
    }
}
