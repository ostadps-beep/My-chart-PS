using MyChart.Core.Contracts.Data;
using MyChart.Core.Models.Indicators;
using MyChart.Indicators;

namespace MyChart.Tests.Indicators;

/// <summary>Test-only SMA(period) for T3.05 VERIFY. Not a product indicator.</summary>
public sealed class SmaIndicator : IndicatorBase
{
    private readonly int _period;

    public SmaIndicator(int period)
        : base(
            name: "SMA",
            inputs: new[]
            {
                new IndicatorParameter("Period", "Period", period, Min: 1, Max: 500, Step: 1)
            },
            outputNames: new[] { "SMA" },
            pane: IndicatorPane.Main,
            includeInAutoScale: true)
    {
        if (period < 1) throw new ArgumentOutOfRangeException(nameof(period));
        _period = period;
    }

    public override int ParameterHash() => HashCode.Combine(Name, _period);

    public override IReadOnlyList<IndicatorOutput> Calculate(ISeriesView series, int fromIndex)
    {
        int n = series.Count;
        var values = new double[n];
        for (int i = 0; i < n; i++)
            values[i] = double.NaN;

        if (n == 0 || fromIndex >= n)
            return new[] { new IndicatorOutput("SMA", values) };

        int start = Math.Max(fromIndex, 0);

        // Warm-up: need period samples ending at i
        for (int i = start; i < n; i++)
        {
            if (i + 1 < _period)
            {
                values[i] = double.NaN;
                continue;
            }

            double sum = 0;
            for (int k = i - _period + 1; k <= i; k++)
                sum += series[k].Close;
            values[i] = sum / _period;
        }

        return new[] { new IndicatorOutput("SMA", values) };
    }
}
