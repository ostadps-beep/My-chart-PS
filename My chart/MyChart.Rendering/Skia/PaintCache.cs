using MyChart.Core.Models.Rendering;
using SkiaSharp;

namespace MyChart.Rendering.Skia;

/// <summary>
/// Reuses SKPaint instances so the hot path does not allocate per candle/line.
/// Candles are never antialiased; lines/text/markers honour the antiAlias flag.
/// Text uses SKPaint (string overloads) for SkiaSharp 2.88 compatibility.
/// </summary>
public sealed class PaintCache : IDisposable
{
    private readonly Dictionary<(uint argb, float width, bool aa, bool fill, SKStrokeCap cap), SKPaint> _paints = new();
    private readonly Dictionary<(string family, float size, bool bold, uint argb, bool aa), SKPaint> _textPaints = new();
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

    /// <summary>Text paint with typeface and size (DIP * dpiScale).</summary>
    public SKPaint GetTextPaint(TextStyle style, double dpiScale, RgbaColor color, bool antiAlias)
    {
        var sizePx = (float)(style.SizeDip * dpiScale);
        var key = (style.FontFamily, sizePx, style.Bold, color.Argb, antiAlias);
        if (_textPaints.TryGetValue(key, out var paint))
            return paint;

        var typeface = SKTypeface.FromFamilyName(
            style.FontFamily,
            style.Bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
            SKFontStyleWidth.Normal,
            SKFontStyleSlant.Upright);

        paint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = ToSkColor(color),
            IsAntialias = antiAlias,
            Typeface = typeface,
            TextSize = sizePx,
            IsStroke = false
        };
        _textPaints[key] = paint;
        return paint;
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
        foreach (var p in _textPaints.Values)
            p.Dispose();
        _textPaints.Clear();
    }
}
