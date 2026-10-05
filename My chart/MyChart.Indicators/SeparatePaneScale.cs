namespace MyChart.Indicators;

/// <summary>
/// T3.05 Separate pane: own auto scale (min/max of visible outputs, 10% padding);
/// height 25% of chart height; at most 4 separate panes.
/// </summary>
public static class SeparatePaneScale
{
    public const double HeightFractionOfChart = 0.25;
    public const int MaxSeparatePanes = 4;
    public const double PaddingFraction = 0.10;

    /// <summary>
    /// Auto scale range for visible output values (NaN ignored).
    /// Returns (min, max) with 10% padding. If no finite values, (0, 1).
    /// </summary>
    public static (double Min, double Max) ComputeRange(IEnumerable<double> visibleValues)
    {
        double min = double.PositiveInfinity;
        double max = double.NegativeInfinity;
        foreach (var v in visibleValues)
        {
            if (double.IsNaN(v) || double.IsInfinity(v)) continue;
            if (v < min) min = v;
            if (v > max) max = v;
        }

        if (double.IsPositiveInfinity(min) || double.IsNegativeInfinity(max))
            return (0, 1);

        if (min == max)
        {
            double pad = Math.Abs(min) * PaddingFraction;
            if (pad == 0) pad = 1;
            return (min - pad, max + pad);
        }

        double range = max - min;
        double padding = range * PaddingFraction;
        return (min - padding, max + padding);
    }
}
