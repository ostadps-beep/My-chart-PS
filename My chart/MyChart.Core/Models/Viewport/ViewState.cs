namespace MyChart.Core.Models.Viewport;

/// <summary>
/// T2.02 ViewState — all sizes are DIP.
/// RightOffset = distance in bars between the centre-slot edge of the LAST bar and the right edge of the plot.
/// </summary>
public sealed class ViewState
{
    public double Width { get; set; }
    public double Height { get; set; }
    public double PriceAxisWidth { get; set; } = 64;
    public double TimeAxisHeight { get; set; } = 24;
    public PriceAxisSide PriceAxisSide { get; set; } = PriceAxisSide.Right;
    public double BarSpacing { get; set; } = 8;
    public double RightOffset { get; set; } = 5;
    public PriceScaleState PriceScale { get; set; } = new();

    public double PlotLeft => PriceAxisSide == PriceAxisSide.Left ? PriceAxisWidth : 0;
    public double PlotWidth => Width - PriceAxisWidth;
    public double PlotTop => 0;
    public double PlotHeight => Height - TimeAxisHeight;
    public double PlotRight => PlotLeft + PlotWidth;
}
