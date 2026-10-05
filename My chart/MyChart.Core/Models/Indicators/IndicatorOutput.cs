namespace MyChart.Core.Models.Indicators;

/// <summary>
/// Named double series aligned 1:1 with candle indices. NaN during warm-up.
/// Numbers only — colors/styles come from settings and Theme (DataRenderingContracts).
/// </summary>
public sealed class IndicatorOutput
{
    public IndicatorOutput(string name, double[] values)
    {
        Name = name;
        Values = values;
    }

    public string Name { get; }
    public double[] Values { get; }
    public int Length => Values.Length;
    public double this[int index] => Values[index];
}
