using MyChart.Core.Contracts.Data;
using MyChart.Core.Contracts.Indicators;
using MyChart.Core.Models.Indicators;

namespace MyChart.Indicators;

/// <summary>
/// T3.05 IndicatorBase — Name, Inputs, Outputs, Pane, IncludeInAutoScale, Calculate.
/// Causal only. Colors/styles are not part of the indicator (Theme / settings).
/// </summary>
public abstract class IndicatorBase : IIndicator
{
    protected IndicatorBase(
        string name,
        IReadOnlyList<IndicatorParameter> inputs,
        IReadOnlyList<string> outputNames,
        IndicatorPane pane = IndicatorPane.Main,
        bool includeInAutoScale = true)
    {
        Name = name;
        Inputs = inputs;
        OutputNames = outputNames;
        Pane = pane;
        IncludeInAutoScale = includeInAutoScale;
    }

    public string Name { get; }
    public IReadOnlyList<IndicatorParameter> Inputs { get; }
    public IReadOnlyList<string> OutputNames { get; }
    public IndicatorPane Pane { get; }
    public bool IncludeInAutoScale { get; }

    public abstract IReadOnlyList<IndicatorOutput> Calculate(ISeriesView series, int fromIndex);

    /// <summary>Stable parameter hash for IndicatorCacheKey.</summary>
    public virtual int ParameterHash()
    {
        unchecked
        {
            int h = Name.GetHashCode(StringComparison.Ordinal);
            foreach (var p in Inputs)
            {
                h = (h * 397) ^ p.Key.GetHashCode(StringComparison.Ordinal);
                h = (h * 397) ^ p.DefaultValue.GetHashCode();
            }
            return h;
        }
    }

    public IndicatorCacheKey CacheKey(string symbol, MyChart.Core.Models.Market.Timeframe tf) =>
        new(symbol, tf, Name, ParameterHash());
}
