using MyChart.Core.Contracts.Data;
using MyChart.Core.Contracts.Indicators;
using MyChart.Core.Models.Indicators;

namespace MyChart.Indicators;

/// <summary>
/// T3.05 host: on CandleClosed compute the new index; on CandleUpdated recompute only the last index.
/// </summary>
public sealed class IndicatorHost
{
    private readonly IIndicator _indicator;
    private IReadOnlyList<IndicatorOutput>? _outputs;

    public IndicatorHost(IIndicator indicator)
    {
        _indicator = indicator ?? throw new ArgumentNullException(nameof(indicator));
    }

    public IIndicator Indicator => _indicator;
    public IReadOnlyList<IndicatorOutput>? Outputs => _outputs;

    public IReadOnlyList<IndicatorOutput> RecalculateAll(ISeriesView series)
    {
        _outputs = _indicator.Calculate(series, 0);
        return _outputs;
    }

    /// <summary>CandleClosed: series grew by one closed bar — recompute from the new last index and merge.</summary>
    public IReadOnlyList<IndicatorOutput> OnCandleClosed(ISeriesView series)
    {
        if (series.Count == 0)
        {
            _outputs = _indicator.Calculate(series, 0);
            return _outputs;
        }

        if (_outputs is null || _outputs.Count == 0 || _outputs[0].Length != series.Count - 1)
        {
            _outputs = _indicator.Calculate(series, 0);
            return _outputs;
        }

        int last = series.Count - 1;
        var partial = _indicator.Calculate(series, last);
        var merged = new IndicatorOutput[_outputs.Count];
        for (int o = 0; o < _outputs.Count; o++)
        {
            var dest = new double[series.Count];
            Array.Copy(_outputs[o].Values, dest, _outputs[o].Length);
            if (o < partial.Count && last < partial[o].Values.Length)
                dest[last] = partial[o].Values[last];
            merged[o] = new IndicatorOutput(_outputs[o].Name, dest);
        }
        _outputs = merged;
        return _outputs;
    }

    /// <summary>CandleUpdated: recompute only the last index in place.</summary>
    public IReadOnlyList<IndicatorOutput> OnCandleUpdated(ISeriesView series)
    {
        if (series.Count == 0)
        {
            _outputs = Array.Empty<IndicatorOutput>();
            return _outputs;
        }

        if (_outputs is null || _outputs.Count == 0 || _outputs[0].Length != series.Count)
        {
            _outputs = _indicator.Calculate(series, 0);
            return _outputs;
        }

        int last = series.Count - 1;
        var partial = _indicator.Calculate(series, last);
        for (int o = 0; o < _outputs.Count && o < partial.Count; o++)
        {
            if (last < _outputs[o].Values.Length && last < partial[o].Values.Length)
                _outputs[o].Values[last] = partial[o].Values[last];
        }
        return _outputs;
    }
}
