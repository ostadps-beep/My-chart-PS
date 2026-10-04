using System.Reflection;
using MyChart.Core.Models.Market;
using Xunit;

namespace MyChart.Tests.Market;

/// <summary>
/// T1.01 MarketModels VERIFY (Canonical Spec).
/// Candle has exactly the 6 FINAL fields; Timeframe has exactly 10 members.
/// L1_MODEL allows helpers IsBullish / IsBearish — they are not counted as FINAL data fields.
/// </summary>
public class MarketModelsTests
{
    private static readonly string[] FinalCandleFields =
    {
        "Timestamp", "Open", "High", "Low", "Close", "Volume"
    };

    [Fact]
    public void Candle_HasExactlySixFinalFields()
    {
        var dataProps = typeof(Candle)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => FinalCandleFields.Contains(p.Name))
            .ToArray();

        Assert.Equal(6, dataProps.Length);

        var names = dataProps.Select(f => f.Name).OrderBy(n => n).ToArray();
        Assert.Equal(new[] { "Close", "High", "Low", "Open", "Timestamp", "Volume" }, names);

        // Forbidden visual/extra fields must not exist
        var allNames = typeof(Candle)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(p => p.Name)
            .ToHashSet();
        Assert.DoesNotContain("Color", allNames);
        Assert.DoesNotContain("Brush", allNames);
        Assert.DoesNotContain("Theme", allNames);
        Assert.DoesNotContain("Style", allNames);
        Assert.DoesNotContain("Index", allNames);
        Assert.DoesNotContain("Timeframe", allNames);
        Assert.DoesNotContain("Symbol", allNames);

        Assert.True(typeof(Candle).IsValueType);

        // L1_MODEL helpers are required
        Assert.NotNull(typeof(Candle).GetProperty("IsBullish"));
        Assert.NotNull(typeof(Candle).GetProperty("IsBearish"));
    }

    [Fact]
    public void Timeframe_HasExactlyTenMembers()
    {
        var values = Enum.GetValues<Timeframe>();
        Assert.Equal(10, values.Length);

        var expected = new[]
        {
            Timeframe.Tick,
            Timeframe.M1,
            Timeframe.M5,
            Timeframe.M15,
            Timeframe.M30,
            Timeframe.H1,
            Timeframe.H4,
            Timeframe.D1,
            Timeframe.W1,
            Timeframe.MN1
        };
        Assert.Equal(expected, values);
    }

    [Fact]
    public void Tick_HasExactConstructorShape()
    {
        var t = new Tick(DateTimeOffset.UtcNow, 1.1, 1.2, 0);
        Assert.Equal(1.1, t.Bid);
        Assert.Equal(1.2, t.Ask);
        Assert.Equal(0, t.Volume);
        Assert.True(typeof(Tick).IsValueType);
    }

    [Fact]
    public void SymbolGroup_HasExactlyFiveMembers()
    {
        var values = Enum.GetValues<SymbolGroup>();
        Assert.Equal(5, values.Length);
        Assert.Contains(SymbolGroup.Forex, values);
        Assert.Contains(SymbolGroup.Crypto, values);
        Assert.Contains(SymbolGroup.Indices, values);
        Assert.Contains(SymbolGroup.Stocks, values);
        Assert.Contains(SymbolGroup.Commodities, values);
    }

    [Fact]
    public void SymbolInfo_HasExactShape()
    {
        var s = new SymbolInfo("EURUSD", "CSV", SymbolGroup.Forex, 5);
        Assert.Equal("EURUSD", s.Name);
        Assert.Equal("CSV", s.ProviderName);
        Assert.Equal(SymbolGroup.Forex, s.Group);
        Assert.Equal(5, s.Digits);
    }

    [Fact]
    public void Candle_IsBullishAndIsBearish_MatchL1Model()
    {
        var bull = new Candle(DateTimeOffset.UtcNow, 1.0, 1.2, 0.9, 1.1, 100);
        Assert.True(bull.IsBullish);
        Assert.False(bull.IsBearish);

        var bear = new Candle(DateTimeOffset.UtcNow, 1.1, 1.2, 0.9, 1.0, 100);
        Assert.True(bear.IsBearish);
        Assert.False(bear.IsBullish);

        var flat = new Candle(DateTimeOffset.UtcNow, 1.0, 1.0, 1.0, 1.0, 0);
        Assert.True(flat.IsBullish); // Close >= Open
        Assert.False(flat.IsBearish);
    }
}
