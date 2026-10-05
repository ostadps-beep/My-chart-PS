namespace MyChart.Core.Rendering;

/// <summary>
/// Reasons that mark the render pipeline dirty.
/// Size/Theme/Range/Data/Series force rebuild of layers 1-5 (base).
/// Crosshair/Hud/Selection/Overlay force redraw of layers 6-10 only.
/// </summary>
public enum InvalidateReason
{
    SizeChanged,
    ThemeChanged,
    RangeChanged,
    DataChanged,
    SeriesChanged,
    CrosshairMoved,
    HudChanged,
    SelectionChanged,
    OverlayChanged
}
