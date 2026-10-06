namespace MyChart.Data.Replay;

/// <summary>T5.05 replay modes.</summary>
public enum ReplayMode
{
    /// <summary>As fast as possible; no wall clock.</summary>
    Backtest,

    /// <summary>Wall clock scaled by 1, 2, 5, 10, or 50.</summary>
    MarketReplay,

    /// <summary>Like MarketReplay; bars after the cursor are hidden; Step Forward available.</summary>
    Training
}
