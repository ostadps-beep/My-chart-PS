using MyChart.Core.Models.Market;

namespace MyChart.Core.Models.Indicators;

/// <summary>
/// Cache key = (Symbol, Timeframe, IndicatorName, ParameterHash). Storage is T5.01 IndicatorsCache.
/// </summary>
public readonly record struct IndicatorCacheKey(
    string Symbol,
    Timeframe Timeframe,
    string IndicatorName,
    int ParameterHash);
