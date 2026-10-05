using MyChart.Core.Contracts.Rendering;
using MyChart.Core.Models.Rendering;

namespace MyChart.Core.Rendering;

/// <summary>
/// Orchestrates the ten fixed render layers in order.
/// Layers 1-5 (base) are rebuilt only when a base invalidation occurs.
/// Layers 6-10 (overlay) are redrawn on every overlay invalidation or when base is rebuilt.
/// Nothing is drawn when no dirty flag is set.
/// </summary>
public sealed class RenderPipeline
{
    private readonly ILayerRenderer?[] _layers = new ILayerRenderer?[11]; // index 1..10
    private bool _baseDirty = true;
    private bool _overlayDirty = true;

    public bool IsBaseDirty => _baseDirty;
    public bool IsOverlayDirty => _overlayDirty;
    public bool IsDirty => _baseDirty || _overlayDirty;

    /// <summary>Register or replace the renderer for a layer (1..10).</summary>
    public void SetLayer(ILayerRenderer renderer)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        var index = (int)renderer.Layer;
        if (index < 1 || index > 10)
            throw new ArgumentOutOfRangeException(nameof(renderer), "Layer must be 1..10.");
        _layers[index] = renderer;
    }

    /// <summary>Register a layer via a paint action (useful for tests).</summary>
    public void SetLayer(RenderLayer layer, Action<IRenderContext> paint)
    {
        ArgumentNullException.ThrowIfNull(paint);
        var index = (int)layer;
        if (index < 1 || index > 10)
            throw new ArgumentOutOfRangeException(nameof(layer), "Layer must be 1..10.");
        _layers[index] = new DelegateLayerRenderer(layer, paint);
    }

    public void Invalidate(InvalidateReason reason)
    {
        switch (reason)
        {
            case InvalidateReason.SizeChanged:
            case InvalidateReason.ThemeChanged:
            case InvalidateReason.RangeChanged:
            case InvalidateReason.DataChanged:
            case InvalidateReason.SeriesChanged:
                _baseDirty = true;
                _overlayDirty = true;
                break;
            case InvalidateReason.CrosshairMoved:
            case InvalidateReason.HudChanged:
            case InvalidateReason.SelectionChanged:
            case InvalidateReason.OverlayChanged:
                _overlayDirty = true;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(reason), reason, null);
        }
    }

    /// <summary>
    /// Execute a single frame. Returns true if any layer was painted.
    /// When base is dirty, layers 1-10 are issued in order.
    /// When only overlay is dirty, only layers 6-10 are issued.
    /// When nothing is dirty, no layer is called.
    /// </summary>
    public bool Render(IRenderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!_baseDirty && !_overlayDirty)
            return false;

        if (_baseDirty)
        {
            for (var i = 1; i <= 10; i++)
                _layers[i]?.Render(context);
            _baseDirty = false;
            _overlayDirty = false;
            return true;
        }

        // overlay only
        for (var i = 6; i <= 10; i++)
            _layers[i]?.Render(context);
        _overlayDirty = false;
        return true;
    }

    private sealed class DelegateLayerRenderer : ILayerRenderer
    {
        private readonly Action<IRenderContext> _paint;
        public RenderLayer Layer { get; }

        public DelegateLayerRenderer(RenderLayer layer, Action<IRenderContext> paint)
        {
            Layer = layer;
            _paint = paint;
        }

        public void Render(IRenderContext context) => _paint(context);
    }
}
