using MyChart.Core.Contracts.Data;
using MyChart.Core.Models.Indicators;

namespace MyChart.Core.Contracts.Indicators;

/// <summary>T3.05 indicator contract. Outputs are numbers only (causal).</summary>
public interface IIndicator
{
    string Name { get; }
    IReadOnlyList<IndicatorParameter> Inputs { get; }
    IReadOnlyList<string> OutputNames { get; }
    IndicatorPane Pane { get; }
    bool IncludeInAutoScale { get; }

    /// <summary>
    /// Compute outputs for indices [fromIndex .. series.Count-1].
    /// Causal: value at i may use candles 0..i only. Warm-up slots are NaN.
    /// </summary>
    IReadOnlyList<IndicatorOutput> Calculate(ISeriesView series, int fromIndex);
}
