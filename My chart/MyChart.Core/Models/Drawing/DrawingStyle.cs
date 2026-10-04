using MyChart.Core.Models.Rendering;

namespace MyChart.Core.Models.Drawing;

public sealed record DrawingStyle(
    RgbaColor Color,
    double Thickness,
    bool ShowLabels = true);
