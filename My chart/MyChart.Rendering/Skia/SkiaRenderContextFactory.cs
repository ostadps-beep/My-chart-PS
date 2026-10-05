using MyChart.Core.Contracts.Rendering;
using MyChart.Core.Models.Rendering;
using SkiaSharp;

namespace MyChart.Rendering.Skia;

/// <summary>
/// Factory for creating SkiaRenderContext instances (T4.01).
/// Manages lifecycle and resource ownership.
/// </summary>
public sealed class SkiaRenderContextFactory
{
    private PaintCache? _sharedPaintCache;

    /// <summary>
    /// Create a SkiaRenderContext with explicit SKCanvas.
    /// Paint cache is created once and shared for efficiency.
    /// </summary>
    public IRenderContext CreateContext(SKCanvas canvas, int width, int height, double dpiScale)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        if (width <= 0 || height <= 0)
            throw new ArgumentException("Width and height must be positive.");

        _sharedPaintCache ??= new PaintCache();
        return new SkiaRenderContext(canvas, width, height, dpiScale, _sharedPaintCache);
    }

    /// <summary>Dispose the shared paint cache (call once at shutdown).</summary>
    public void Dispose()
    {
        _sharedPaintCache?.Dispose();
        _sharedPaintCache = null;
    }
}
