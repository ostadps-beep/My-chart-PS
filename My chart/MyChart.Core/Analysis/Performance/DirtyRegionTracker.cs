namespace MyChart.Core.Analysis.Performance;

/// <summary>
/// T7.03 DirtyRegionUpdates — layers 6–10 redraw only the dirty pixel region when possible.
/// Full bounds = entire surface; empty = nothing to redraw.
/// </summary>
public sealed class DirtyRegionTracker
{
    private int _x;
    private int _y;
    private int _w;
    private int _h;
    private bool _hasDirty;
    private bool _full;

    public bool HasDirty => _hasDirty;
    public bool IsFullSurface => _full;

    public (int X, int Y, int W, int H) Region => (_x, _y, _w, _h);

    public void MarkFull()
    {
        _full = true;
        _hasDirty = true;
        _x = _y = 0;
        _w = _h = int.MaxValue;
    }

    public void Mark(int x, int y, int w, int h)
    {
        if (w <= 0 || h <= 0) return;
        if (!_hasDirty)
        {
            _x = x;
            _y = y;
            _w = w;
            _h = h;
            _hasDirty = true;
            return;
        }

        if (_full) return;

        var x2 = Math.Max(_x + _w, x + w);
        var y2 = Math.Max(_y + _h, y + h);
        var nx = Math.Min(_x, x);
        var ny = Math.Min(_y, y);
        _x = nx;
        _y = ny;
        _w = x2 - nx;
        _h = y2 - ny;
    }

    public void Clear()
    {
        _hasDirty = false;
        _full = false;
        _x = _y = _w = _h = 0;
    }
}
