namespace MyChart.Core.Models.Viewport;

/// <summary>T2.07 fills transform details; T2.02 needs min/max for Y mapping.</summary>
public sealed class PriceScaleState
{
    public double MinPrice { get; set; }
    public double MaxPrice { get; set; } = 1;

    /// <summary>Identity transform until T2.07.</summary>
    public double Transform(double price) => price;

    public double InverseTransform(double transformed) => transformed;
}
