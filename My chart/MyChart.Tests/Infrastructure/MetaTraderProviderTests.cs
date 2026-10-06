using MyChart.Core.Models.Market;
using MyChart.Infrastructure.Providers.MetaTrader;
using Xunit;

namespace MyChart.Tests.Infrastructure;

public class MetaTraderProviderTests
{
    [Fact]
    public async Task Fake_pipe_delivers_hello_symbols_and_ticks()
    {
        var (appSide, eaSide) = InMemoryMtTransport.CreatePair();
        await using var provider = new MetaTraderProvider(appSide);

        await provider.ConnectAsync(CancellationToken.None);
        await eaSide.ConnectAsync(CancellationToken.None);

        await eaSide.SendLineAsync(MessageCodec.EncodeHello("EA-1.2", 120), CancellationToken.None);
        await eaSide.SendLineAsync(MessageCodec.EncodeSymbol("EURUSD", 5, "Forex"), CancellationToken.None);

        Assert.Equal("EA-1.2", provider.EaVersion);
        Assert.Equal(new ServerTimeRule.Fixed(TimeSpan.FromMinutes(120)), provider.ServerTimeRule);

        var symbols = await provider.GetSymbolsAsync(CancellationToken.None);
        Assert.Single(symbols);
        Assert.Equal("EURUSD", symbols[0].Name);
        Assert.Equal(5, symbols[0].Digits);
        Assert.Equal(SymbolGroup.Forex, symbols[0].Group);

        Tick? got = null;
        using var sub = provider.SubscribeTicks("EURUSD", t => got = t);

        var serverMs = new DateTimeOffset(2024, 3, 13, 14, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();
        await eaSide.SendLineAsync(MessageCodec.EncodeTick("EURUSD", serverMs, 1.1001, 1.1003, 2), CancellationToken.None);

        Assert.NotNull(got);
        Assert.Equal(new DateTimeOffset(2024, 3, 13, 12, 0, 0, TimeSpan.Zero), got!.Value.Timestamp);
        Assert.Equal(1.1001, got.Value.Bid, 5);
        Assert.Equal(1.1003, got.Value.Ask, 5);
        Assert.Equal(2, got.Value.Volume, 5);

        await provider.DisconnectAsync();
    }

    [Fact]
    public async Task Fake_pipe_delivers_history_bars()
    {
        var (appSide, eaSide) = InMemoryMtTransport.CreatePair();
        await using var provider = new MetaTraderProvider(appSide);

        await provider.ConnectAsync(CancellationToken.None);
        await eaSide.ConnectAsync(CancellationToken.None);
        await eaSide.SendLineAsync(MessageCodec.EncodeHello("EA", 0), CancellationToken.None);

        eaSide.LineReceived += async line =>
        {
            if (!MessageCodec.TryParse(line, out var msg) || msg is not MtMessage.GetHistory g)
                return;
            var sec = g.FromUtcSeconds;
            await eaSide.SendLineAsync(
                MessageCodec.EncodeHistoryBar(g.Symbol, sec, 1.10, 1.11, 1.09, 1.105, 10),
                CancellationToken.None);
            await eaSide.SendLineAsync(
                MessageCodec.EncodeHistoryBar(g.Symbol, sec + 60, 1.105, 1.12, 1.10, 1.11, 12),
                CancellationToken.None);
            await eaSide.SendLineAsync(MessageCodec.EncodeHistoryEnd(g.Symbol, 2), CancellationToken.None);
        };

        var from = DateTimeOffset.FromUnixTimeSeconds(1_710_000_000);
        var to = from.AddHours(1);
        var bars = await provider.GetHistoryAsync("EURUSD", Timeframe.M1, from, to, 100, CancellationToken.None);

        Assert.Equal(2, bars.Count);
        Assert.Equal(1.10, bars[0].Open, 5);
        Assert.Equal(1.11, bars[1].Close, 5);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1_710_000_000), bars[0].Timestamp);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1_710_000_060), bars[1].Timestamp);
    }

    [Fact]
    public async Task Ping_from_ea_gets_pong_reply()
    {
        var (appSide, eaSide) = InMemoryMtTransport.CreatePair();
        await using var provider = new MetaTraderProvider(appSide);
        await provider.ConnectAsync(CancellationToken.None);
        await eaSide.ConnectAsync(CancellationToken.None);

        string? reply = null;
        eaSide.LineReceived += line => reply = line;

        await eaSide.SendLineAsync(MessageCodec.EncodePing(), CancellationToken.None);
        await Task.Yield();
        Assert.Equal("PONG", reply);
    }
}
