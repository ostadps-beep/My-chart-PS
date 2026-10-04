namespace MyChart.Core.Models.Market;

/// <summary>
/// T1.02 — Fixed offset or EET_US_DST (UTC+2 / UTC+3).
/// </summary>
public abstract record ServerTimeRule
{
    public sealed record Fixed(TimeSpan Offset) : ServerTimeRule;

    /// <summary>
    /// UTC+2 normally; UTC+3 from the second Sunday of March to the first Sunday of November.
    /// Allowed only for Forex, Indices, Commodities.
    /// </summary>
    public sealed record EetUsDst : ServerTimeRule;

    public static Fixed Utc { get; } = new(TimeSpan.Zero);
}
