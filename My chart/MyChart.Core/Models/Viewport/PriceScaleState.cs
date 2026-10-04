namespace MyChart.Core.Models.Viewport;

/// <summary>
/// T2.07 PriceScaleState — Fit Auto|Manual; Transform Linear|Log|Percentage.
/// </summary>
public sealed class PriceScaleState
{
    public ScaleFit Fit { get; set; } = ScaleFit.Auto;
    public ScaleTransformKind TransformKind { get; set; } = ScaleTransformKind.Linear;
    public double ManualMin { get; set; }
    public double ManualMax { get; set; } = 1;
    public bool IsManualLocked { get; set; }
    public bool LogUnavailable { get; set; }

    public double MinPrice { get; set; }
    public double MaxPrice { get; set; } = 1;
    public double PercentageBase { get; set; } = 1;

    public double TransformPrice(double price)
    {
        return TransformKind switch
        {
            ScaleTransformKind.Log => Math.Log(price),
            ScaleTransformKind.Percentage => (price / PercentageBase - 1.0) * 100.0,
            _ => price
        };
    }

    public double InverseTransform(double transformed)
    {
        return TransformKind switch
        {
            ScaleTransformKind.Log => Math.Exp(transformed),
            ScaleTransformKind.Percentage => PercentageBase * (1.0 + transformed / 100.0),
            _ => transformed
        };
    }

    /// <summary>Alias for CoordinateConverter.</summary>
    public double Transform(double price) => TransformPrice(price);
}
