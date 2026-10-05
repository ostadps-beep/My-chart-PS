using MyChart.Core.Models.Market;
using MyChart.Indicators;
using MyChart.Tests.Indicators;
using Xunit;

namespace MyChart.Tests.Indicators;

/// <summary>
/// T3.05 VERIFY: test-only SMA(period); SMA(3) of FoldM5 M1 closes matches hand values;
/// update of the last index equals full recompute.
/// </summary>
public class IndicatorFrameworkTests
{
    // FoldM5 input M1 closes from GOLDEN_TEST_VECTORS.FoldM5
    private static readonly double[] FoldM5Closes = { 1.1005, 1.1015, 1.0995, 1.0990, 1.1002 };

    private static ListSeriesView FoldM5Series()
    {
        var t0 = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var list = new List<Candle>();
        for (int i = 0; i < FoldM5Closes.Length; i++)
        {
            double c = FoldM5Closes[i];
            list.Add(new Candle(t0.AddMinutes(i), c, c, c, c, 1));
        }
        return new ListSeriesView(list);
    }

    [Fact]
    public void Sma3_OfFoldM5Closes_MatchesHandValues()
    {
        // Hand SMA(3):
        // i0, i1: NaN (warm-up)
        // i2: (1.1005+1.1015+1.0995)/3 = 1.1005
        // i3: (1.1015+1.0995+1.0990)/3 = 1.1000
        // i4: (1.0995+1.0990+1.1002)/3 = 1.0995666...
        var sma = new SmaIndicator(3);
        var series = FoldM5Series();
        var outputs = sma.Calculate(series, 0);
        Assert.Single(outputs);
        var v = outputs[0].Values;
        Assert.Equal(5, v.Length);
        Assert.True(double.IsNaN(v[0]));
        Assert.True(double.IsNaN(v[1]));
        Assert.Equal(1.1005, v[2], 10);
        Assert.Equal(1.1000, v[3], 10);
        Assert.Equal((1.0995 + 1.0990 + 1.1002) / 3.0, v[4], 10);
    }

    [Fact]
    public void Sma_UpdateLastIndex_EqualsFullRecompute()
    {
        var sma = new SmaIndicator(3);
        var series = FoldM5Series();

        var full = sma.Calculate(series, 0);
        var lastOnly = sma.Calculate(series, series.Count - 1);

        Assert.Equal(full[0].Values[^1], lastOnly[0].Values[^1], 12);
        // earlier slots in partial calc remain NaN (not filled)
        for (int i = 0; i < series.Count - 1; i++)
            Assert.True(double.IsNaN(lastOnly[0].Values[i]));
    }

    [Fact]
    public void IndicatorHost_OnCandleUpdated_MatchesFullRecompute()
    {
        var host = new IndicatorHost(new SmaIndicator(3));
        var series = FoldM5Series();
        host.RecalculateAll(series);

        // Simulate update of last bar close
        var t0 = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var mutated = new List<Candle>();
        for (int i = 0; i < FoldM5Closes.Length; i++)
        {
            double c = i == FoldM5Closes.Length - 1 ? 1.1010 : FoldM5Closes[i];
            mutated.Add(new Candle(t0.AddMinutes(i), c, c, c, c, 1));
        }
        var updatedSeries = new ListSeriesView(mutated);

        host.OnCandleUpdated(updatedSeries);
        var full = new SmaIndicator(3).Calculate(updatedSeries, 0);

        Assert.Equal(full[0].Values[^1], host.Outputs![0].Values[^1], 12);
    }

    [Fact]
    public void IndicatorHost_OnCandleClosed_ExtendsSeries()
    {
        var host = new IndicatorHost(new SmaIndicator(3));
        var t0 = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var four = new List<Candle>();
        for (int i = 0; i < 4; i++)
        {
            double c = FoldM5Closes[i];
            four.Add(new Candle(t0.AddMinutes(i), c, c, c, c, 1));
        }
        host.RecalculateAll(new ListSeriesView(four));

        var five = new List<Candle>(four)
        {
            new Candle(t0.AddMinutes(4), 1.1002, 1.1002, 1.1002, 1.1002, 1)
        };
        host.OnCandleClosed(new ListSeriesView(five));

        var full = new SmaIndicator(3).Calculate(new ListSeriesView(five), 0);
        Assert.Equal(full[0].Length, host.Outputs![0].Length);
        Assert.Equal(full[0].Values[^1], host.Outputs[0].Values[^1], 12);
    }

    [Fact]
    public void SeparatePaneScale_Padding10Percent()
    {
        var (min, max) = SeparatePaneScale.ComputeRange(new[] { 10.0, 20.0 });
        Assert.Equal(9.0, min, 10);   // 10 - 1
        Assert.Equal(21.0, max, 10);  // 20 + 1
        Assert.Equal(0.25, SeparatePaneScale.HeightFractionOfChart);
        Assert.Equal(4, SeparatePaneScale.MaxSeparatePanes);
    }

    [Fact]
    public void Sma_IsCausal_DoesNotLookAhead()
    {
        var sma = new SmaIndicator(3);
        var series = FoldM5Series();
        var outputs = sma.Calculate(series, 0);
        // Value at index 2 only uses closes 0..2
        double expected = (FoldM5Closes[0] + FoldM5Closes[1] + FoldM5Closes[2]) / 3.0;
        Assert.Equal(expected, outputs[0].Values[2], 12);
    }

    [Fact]
    public void CacheKey_StableForSameParameters()
    {
        var a = new SmaIndicator(3);
        var b = new SmaIndicator(3);
        var c = new SmaIndicator(5);
        Assert.Equal(a.ParameterHash(), b.ParameterHash());
        Assert.NotEqual(a.ParameterHash(), c.ParameterHash());
        var key = a.CacheKey("EURUSD", Timeframe.M5);
        Assert.Equal("EURUSD", key.Symbol);
        Assert.Equal(Timeframe.M5, key.Timeframe);
        Assert.Equal("SMA", key.IndicatorName);
    }
}
