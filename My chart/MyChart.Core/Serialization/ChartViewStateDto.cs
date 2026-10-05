using MyChart.Core.Models.Chart;
using MyChart.Core.Models.Market;
using MyChart.Core.Models.Viewport;

namespace MyChart.Core.Serialization;

/// <summary>Per-chart view state in the workspace (T3.07).</summary>
public sealed class ChartViewStateDto
{
    public double BarSpacing { get; set; } = 8;
    public double RightOffset { get; set; } = 5;
    public ScaleFit PriceScaleFit { get; set; } = ScaleFit.Auto;
    public ScaleTransformKind PriceScaleTransform { get; set; } = ScaleTransformKind.Linear;
    public double ManualMin { get; set; }
    public double ManualMax { get; set; } = 1;
    public ChartType ChartType { get; set; } = ChartType.Candle;
    public string Symbol { get; set; } = "EURUSD";
    public Timeframe Timeframe { get; set; } = Timeframe.M1;

    public static ChartViewStateDto FromViewState(ViewState vs, string symbol, Timeframe tf, ChartType chartType)
    {
        return new ChartViewStateDto
        {
            BarSpacing = vs.BarSpacing,
            RightOffset = vs.RightOffset,
            PriceScaleFit = vs.PriceScale.Fit,
            PriceScaleTransform = vs.PriceScale.TransformKind,
            ManualMin = vs.PriceScale.ManualMin,
            ManualMax = vs.PriceScale.ManualMax,
            ChartType = chartType,
            Symbol = symbol,
            Timeframe = tf
        };
    }
}
