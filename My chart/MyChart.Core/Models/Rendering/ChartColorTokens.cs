namespace MyChart.Core.Models.Rendering;

/// <summary>
/// Immutable color token set for the entire chart (T4.02 ColorTokensMinimal).
/// One instance per render session; never modified.
/// 
/// Maps to the 10-layer render pipeline:
/// - Layer 1 (Background): uses Background
/// - Layer 2 (Grid/Axis): uses GridMajor, GridMinor, AxisText
/// - Layer 3 (Candle): uses CandleBull, CandleBear, CandleWick
/// - Layer 6 (Selection): uses SelectionHandle
/// - Layer 7 (Crosshair): uses Crosshair
/// - Layer 8 (HUD): uses HudText, CurrentPrice
/// - Layer 10 (Overlay): uses Accent, TransparentBg, Warning, Error
/// 
/// RULE: This record is immutable; all colors are determined at creation.
/// RULE: All colors are RgbaColor; conversion from hex is done at palette level.
/// </summary>
public sealed record ChartColorTokens(
    RgbaColor Background,          // Layer 1: Canvas background
    RgbaColor GridMajor,           // Layer 2: Major grid lines (with labels)
    RgbaColor GridMinor,           // Layer 2: Minor grid lines (optional)
    RgbaColor AxisText,            // Layer 2: Axis labels and text
    RgbaColor CandleBull,          // Layer 3: Bull candle body and wick
    RgbaColor CandleBear,          // Layer 3: Bear candle body and wick
    RgbaColor CandleWick,          // Layer 3: Wick color (can differ from body)
    RgbaColor CurrentPrice,        // Layer 10: Current price marker line
    RgbaColor Crosshair,           // Layer 7: Crosshair cursor
    RgbaColor HudText,             // Layer 8: HUD overlay text
    RgbaColor SelectionHandle,     // Layer 6: Selection handles and borders
    RgbaColor Accent,              // Layer 10: Accent/status/alerts
    RgbaColor TransparentBg,       // Layer 10: Transparent background
    RgbaColor Warning,             // Layer 10: Warning color
    RgbaColor Error                // Layer 10: Error color
);
