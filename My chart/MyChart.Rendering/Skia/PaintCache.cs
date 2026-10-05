using MyChart.Core.Models.Rendering;
using SkiaSharp;

namespace MyChart.Rendering.Skia;

/// <summary>
/// Reuses SKPaint instances so the hot path does not allocate per candle/line.
/// Candles are never antialiased; lines/text/markers honour the antiAlias flag.
/// </summary>
public sealed class PaintCache : IDisposable
{
    private readonly Dictionary<(uint argb, float width, bool aa, bool fill, SKStrokeCap cap), SKPaint> _paints = new();
    private readonly Dictionary<(string family, float size, bool bold), SKFont> _fonts = new();
    private bool _disposed;

    public SKPaint GetFill(RgbaColor color, bool antiAlias)
    {
        var key = (color.Argb, 0f, antiAlias, true, SKStrokeCap.Butt);
        if (_paints.TryGetValue(key, out var paint))
            return paint;
        paint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = ToSkColor(color),
            IsAntialias = antiAlias,
            IsStroke = false
        };
        _paints[key] = paint;
        return paint;
    }

    public SKPaint GetStroke(RgbaColor color, float width, bool antiAlias, SKStrokeCap cap = SKStrokeCap.Butt)
    {
        var key = (color.Argb, width, antiAlias, false, cap);
        if (_paints.TryGetValue(key, out var paint))
            return paint;
        paint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = ToSkColor(color),
            StrokeWidth = width,
            IsAntialias = antiAlias,
            StrokeCap = cap,
            IsStroke = true
        };
        _paints[key] = paint;
        return paint;
    }

    public SKFont GetFont(TextStyle style, double dpiScale)
    {
        var sizePx = (float)(style.SizeDip * dpiScale);
        var key = (style.FontFamily, sizePx, style.Bold);
        if (_fonts.TryGetValue(key, out var font))
            return font;
        var typeface = SKTypeface.FromFamilyName(
            style.FontFamily,
            style.Bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
            SKFontStyleWidth.Normal,
            SKFontStyleSlant.Upright);
        font = new SKFont(typeface, sizePx);
        _fonts[key] = font;
        return font;
    }

    public static SKColor ToSkColor(RgbaColor c) => new(c.R, c.G, c.B, c.A);

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        foreach (var p in _paints.Values)
            p.Dispose();
        _paints.Clear();
        foreach (var f in _fonts.Values)
            f.Dispose();
        _fonts.Clear();
    }
}
