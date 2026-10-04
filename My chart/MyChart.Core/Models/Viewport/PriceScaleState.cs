namespace MyChart.Core.Models.Viewport;

/// <summary>
/// T2.07 PriceScaleState — Fit Auto|Manual; Transform Linear|Log|Percentage.
/// MinPrice/MaxPrice are stored in price space for Linear display range after Auto;
/// ManualMin/ManualMax are in transform space.
/// </summary>
public sealed class PriceScaleState
{
    public ScaleFit Fit { get; set; } = ScaleFit.Auto;
    public ScaleTransformKind Transform { get; set; } = ScaleTransformKind.Linear;
    public double ManualMin { get; set; }
    public double ManualMax { get; set; } = 1;
    public bool IsManualLocked { get; set; }
    public bool LogUnavailable { get; set; }

    /// <summary>Visible range in price space (after Auto or inverse of Manual).</summary>
    public double MinPrice { get; set; }
    public double MaxPrice { get; set; } = 1;

    public double PercentageBase { get; set; } = 1;

    public double TransformPrice(double price)
    {
        return Transform switch
        {
            ScaleTransformKind.Log => Math.Log(price),
            ScaleTransformKind.Percentage => (price / PercentageBase - 1.0) * 100.0,
            _ => price
        };
    }

    public double InverseTransform(double transformed)
    {
        return Transform switch
        {
            ScaleTransformKind.Log => Math.Exp(transformed),
            ScaleTransformKind.Percentage => PercentageBase * (1.0 + transformed / 100.0),
            _ => transformed
        };
    }

    // Compatibility aliases used by CoordinateConverter
    public double Transform(double price) => TransformPrice(price);
}
