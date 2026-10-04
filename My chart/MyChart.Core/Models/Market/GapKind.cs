namespace MyChart.Core.Models.Market;

/// <summary>
/// T1.03 GapKind — Expected / Missing / Unclassified.
/// </summary>
public enum GapKind
{
    /// <summary>Gap contains at least one local Saturday (Forex/Indices/Commodities), or Stocks gap &gt;= 8h. Crypto: never Expected.</summary>
    Expected,

    /// <summary>A gap that is not Expected and not Unclassified.</summary>
    Missing,

    /// <summary>Gap &gt; 24h without a Saturday for Indices/Stocks/Commodities (holidays). Reported only, never auto-repaired.</summary>
    Unclassified
}
