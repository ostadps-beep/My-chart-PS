using SkiaSharp;

namespace MyChart.Rendering.Skia;

/// <summary>
/// Offscreen surface for layers 1-5. When the base is dirty the pipeline redraws
/// into this cache; subsequent overlay-only frames blit the cached image then
/// draw layers 6-10 on top.
/// </summary>
public sealed class SkiaLayerCache : IDisposable
{
    private SKBitmap? _bitmap;
    private SKCanvas? _canvas;
    private int _width;
    private int _height;
    private bool _valid;
    private bool _disposed;

    public bool IsValid => _valid && _bitmap is not null;
    public int Width => _width;
    public int Height => _height;

    /// <summary>Ensure the cache surface matches the given size. Invalidates on resize.</summary>
    public void EnsureSize(int width, int height)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentOutOfRangeException();
        if (_bitmap is not null && _width == width && _height == height)
            return;
        DisposeSurface();
        _width = width;
        _height = height;
        _bitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        _canvas = new SKCanvas(_bitmap);
        _valid = false;
    }

    public SKCanvas Canvas
    {
        get
        {
            if (_canvas is null)
                throw new InvalidOperationException("Call EnsureSize before accessing Canvas.");
            return _canvas;
        }
    }

    public void MarkValid()
    {
        _canvas?.Flush();
        _valid = true;
    }

    public void Invalidate() => _valid = false;

    /// <summary>Blit the cached base layers onto the target canvas.</summary>
    public void BlitTo(SKCanvas target)
    {
        if (!IsValid || _bitmap is null)
            return;
        target.DrawBitmap(_bitmap, 0, 0);
    }

    private void DisposeSurface()
    {
        _canvas?.Dispose();
        _canvas = null;
        _bitmap?.Dispose();
        _bitmap = null;
        _valid = false;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        DisposeSurface();
    }
}
