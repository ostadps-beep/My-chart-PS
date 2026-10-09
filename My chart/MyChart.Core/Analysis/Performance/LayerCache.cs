namespace MyChart.Core.Analysis.Performance;

/// <summary>
/// T7.03 DrawingCache — layers 1–5 are cached; invalidate only when their inputs change.
/// Layers 6–10 use dirty-region redraw (see DirtyRegionTracker).
/// </summary>
public sealed class LayerCache
{
    public const int CachedLayerMin = 1;
    public const int CachedLayerMax = 5;
    public const int DirtyLayerMin = 6;
    public const int DirtyLayerMax = 10;

    private readonly bool[] _valid = new bool[11]; // index 1..10
    private readonly object?[] _payload = new object?[11];

    public bool IsCachedLayer(int layer) => layer is >= CachedLayerMin and <= CachedLayerMax;

    public bool IsDirtyLayer(int layer) => layer is >= DirtyLayerMin and <= DirtyLayerMax;

    public bool TryGet(int layer, out object? payload)
    {
        payload = null;
        if (layer is < 1 or > 10) return false;
        if (!_valid[layer]) return false;
        payload = _payload[layer];
        return true;
    }

    public void Set(int layer, object? payload)
    {
        if (layer is < 1 or > 10) return;
        _payload[layer] = payload;
        _valid[layer] = true;
    }

    public void Invalidate(int layer)
    {
        if (layer is < 1 or > 10) return;
        _valid[layer] = false;
        _payload[layer] = null;
    }

    public void InvalidateCachedLayers()
    {
        for (var i = CachedLayerMin; i <= CachedLayerMax; i++)
            Invalidate(i);
    }

    public void InvalidateAll()
    {
        for (var i = 1; i <= 10; i++)
            Invalidate(i);
    }
}
