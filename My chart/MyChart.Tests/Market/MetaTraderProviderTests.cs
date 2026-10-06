using MyChart.Core.Models.Market;
using MyChart.Data.Providers.MetaTrader;
using Xunit;

namespace MyChart.Tests.Market;

public class MetaTraderProviderTests
{
    [Fact]
    public void HandleLine_DeliversTicks()
    {
        using var ms = new MemoryStream();
        var provider = new MetaTraderProvider(ms);
        var ticks = new List<Tick>();
        using var sub = provider.SubscribeTicks("EURUSD", t => ticks.Add(t));

        provider.HandleLine("HELLO|1.0|0");
        provider.HandleLine("T|EURUSD|1710000000000|1.1000|1.1002|1");

        Assert.Single(ticks);
        Assert.Equal(1.1000, ticks[0].Bid);
        Assert.Equal(1.1002, ticks[0].Ask);
    }

    [Fact]
    public async Task HandleLine_HistoryBars_CompleteOnHend()
    {
        using var ms = new MemoryStream();
        var provider = new MetaTraderProvider(ms);
        provider.HandleLine("HELLO|1.0|0");

        var historyTask = provider.GetHistoryAsync("EURUSD", Timeframe.M1, null, null, 10, CancellationToken.None);
        await Task.Delay(50);

        provider.HandleLine("H|EURUSD|M1|1710000000|1.1|1.11|1.09|1.105|5");
        provider.HandleLine("H|EURUSD|M1|1710000060|1.105|1.12|1.10|1.11|6");
        provider.HandleLine("HEND|EURUSD|2");

        var bars = await historyTask.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Equal(2, bars.Count);
        Assert.Equal(1.105, bars[0].Close);
        Assert.Equal(1.11, bars[1].Close);
    }

    [Fact]
    public void Hello_SetsFixedServerTimeRule()
    {
        using var ms = new MemoryStream();
        var provider = new MetaTraderProvider(ms);
        provider.HandleLine("HELLO|2.0|180");
        var fixedRule = Assert.IsType<ServerTimeRule.Fixed>(provider.ServerTimeRule);
        Assert.Equal(TimeSpan.FromMinutes(180), fixedRule.Offset);
    }

    [Fact]
    public async Task SymbolInfo_Collected()
    {
        using var ms = new MemoryStream();
        var provider = new MetaTraderProvider(ms);
        provider.HandleLine("SYM|EURUSD|5|Forex");
        var symbols = await provider.GetSymbolsAsync(CancellationToken.None);
        Assert.Single(symbols);
        Assert.Equal("EURUSD", symbols[0].Name);
        Assert.Equal(5, symbols[0].Digits);
        Assert.Equal(SymbolGroup.Forex, symbols[0].Group);
    }
}
