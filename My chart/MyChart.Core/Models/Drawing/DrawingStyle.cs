using MyChart.Core.Models.Rendering;

namespace MyChart.Core.Models.Drawing;

/// <summary>
/// Style of a drawing object. Thickness is the line width in DIP (selectable 1, 2, 3, 4).
/// Opacity is 0..1 (default 1 = 100 percent). The T3.04 default is built by DrawingStyleRules.Default.
/// </summary>
public sealed record DrawingStyle(
    RgbaColor Color,
    double Thickness,
    bool ShowLabels = true,
    double Opacity = 1.0,
    DrawingLineStyle LineStyle = DrawingLineStyle.Solid);
