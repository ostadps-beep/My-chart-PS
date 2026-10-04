using MyChart.Core.Candles;
using MyChart.Core.Models.Market;
using Xunit;

namespace MyChart.Tests.Market;

/// <summary>
/// T1.04 VERIFY: EURUSD digits5 → Point 0.00001, Pip 0.0001;
/// USDJPY digits3 → Point 0.001, Pip 0.01;
/// XAUUSD (Commodities) → Pip undefined.
/// </summary>
public class SymbolRegistryTests
{
    [Fact]
    public void SymbolMath_EURUSD_Digits5()
    {
        Assert.Equal(0.00001, SymbolMath.PointSize(5), 12);
        Assert.Equal(0.0001, SymbolMath.PipSize(SymbolGroup.Forex, 5)!.Value, 12);
    }

    [Fact]
    public void SymbolMath_USDJPY_Digits3()
    {
        Assert.Equal(0.001, SymbolMath.PointSize(3), 12);
        Assert.Equal(0.01, SymbolMath.PipSize(SymbolGroup.Forex, 3)!.Value, 12);
    }

    [Fact]
    public void SymbolMath_XAUUSD_Commodities_PipUndefined()
    {
        Assert.Equal(0.01, SymbolMath.PointSize(2), 12);
        Assert.Null(SymbolMath.PipSize(SymbolGroup.Commodities, 2));
    }

    [Fact]
    public void SymbolMath_Forex_Digits2and4_PipEqualsPoint()
    {
        Assert.Equal(0.01, SymbolMath.PipSize(SymbolGroup.Forex, 2)!.Value, 12);
        Assert.Equal(0.0001, SymbolMath.PipSize(SymbolGroup.Forex, 4)!.Value, 12);
    }

    [Fact]
    public void Registry_Register_Get_List_Alias()
    {
        var reg = new SymbolRegistry();
        reg.Register(new SymbolInfo("eurusd", "EURUSD.m", SymbolGroup.Forex, 5));

        var got = reg.Get("EURUSD");
        Assert.NotNull(got);
        Assert.Equal("EURUSD", got!.Name);
        Assert.Equal("EURUSD.M", got.ProviderName); // suffix NOT stripped; only uppercased
        Assert.Equal(5, got.Digits);

        // alias via provider name
        Assert.NotNull(reg.Get("EURUSD.M"));

        reg.AddAlias("EURUSDm", "EURUSD");
        Assert.NotNull(reg.Get("eurusdm"));

        Assert.Single(reg.List());
    }

    [Fact]
    public void InferGroup_SixLetterCurrencyPair_IsForex()
    {
        Assert.Equal(SymbolGroup.Forex, SymbolRegistry.InferGroup("EURUSD"));
        Assert.Equal(SymbolGroup.Forex, SymbolRegistry.InferGroup("usdjpy"));
        Assert.Equal(SymbolGroup.Stocks, SymbolRegistry.InferGroup("AAPL"));
        Assert.Equal(SymbolGroup.Stocks, SymbolRegistry.InferGroup("EURUSDm")); // 7 chars
    }

    [Fact]
    public void Normalize_TrimAndUpper_Only()
    {
        Assert.Equal("EURUSD.M", SymbolRegistry.Normalize("  eurusd.m  "));
    }
}
