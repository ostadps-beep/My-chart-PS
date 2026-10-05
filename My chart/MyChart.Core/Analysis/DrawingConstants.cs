using MyChart.Core.Models.Drawing;
using MyChart.Core.Models.Rendering;

namespace MyChart.Core.Analysis;

/// <summary>T3.04 constants (CONSTANTS_TABLE: HitToleranceDip=6, HandleSizeDip=8, CloneOffsetDip=12).</summary>
public static class DrawingConstants
{
    /// <summary>Hit tolerance in screen space (DIP).</summary>
    public const double HitToleranceDip = 6;

    /// <summary>Handles are 8x8 DIP squares centred on the anchors.</summary>
    public const double HandleSizeDip = 8;

    /// <summary>A handle is hit within this radius (DIP) of its anchor.</summary>
    public const double HandleHitRadiusDip = 6;

    /// <summary>CLONE offset: this many DIP to the right and this many DIP down.</summary>
    public const double CloneOffsetDip = 12;
}

/// <summary>T3.04 DEFAULT STYLE and the selectable widths and dash patterns.</summary>
public static class DrawingStyleRules
{
    /// <summary>Selectable line widths (DIP).</summary>
    public static readonly IReadOnlyList<double> SelectableWidths = new double[] { 1, 2, 3, 4 };

    /// <summary>
    /// Default style: color = AccentColor (passed in, the theme is not part of Core yet), width 1,
    /// opacity 100 percent, line style solid.
    /// </summary>
    public static DrawingStyle Default(RgbaColor accentColor)
        => new(accentColor, 1.0, true, 1.0, DrawingLineStyle.Solid);

    /// <summary>Dash pattern for IRenderContext.DrawLine: null = solid ; dashed [6,4] ; dotted [2,3].</summary>
    public static double[]? DashPattern(DrawingLineStyle style) => style switch
    {
        DrawingLineStyle.Dashed => new[] { 6.0, 4.0 },
        DrawingLineStyle.Dotted => new[] { 2.0, 3.0 },
        _ => null
    };
}
