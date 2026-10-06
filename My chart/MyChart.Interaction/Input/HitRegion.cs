namespace MyChart.Interaction.Input;

/// <summary>T4.07 hit regions (priority: price axis > time axis > plot).</summary>
public enum HitRegion
{
    Outside,
    Plot,
    PriceAxis,
    TimeAxis
}
