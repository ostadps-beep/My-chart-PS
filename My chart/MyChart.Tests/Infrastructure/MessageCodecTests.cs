using MyChart.Core.Models.Market;
using MyChart.Infrastructure.Providers.MetaTrader;
using Xunit;

namespace MyChart.Tests.Infrastructure;

public class MessageCodecTests
{
    [Fact]
    public void Hello_round_trip()
    {
        var line = MessageCodec.EncodeHello("1.0.0", 120);
        Assert.True(MessageCodec.TryParse(line, out var msg));
        var hello = Assert.IsType<MtMessage.Hello>(msg);
        Assert.Equal("1.0.0", hello.EaVersion);
        Assert.Equal(120, hello.ServerUtcOffsetMinutes);
    }

    [Fact]
    public void Symbol_round_trip()
    {
        var line = MessageCodec.EncodeSymbol("EURUSD", 5, "Forex");
        Assert.True(MessageCodec.TryParse(line, out var msg));
        var sym = Assert.IsType<MtMessage.Symbol>(msg);
        Assert.Equal("EURUSD", sym.Name);
        Assert.Equal(5, sym.Digits);
        Assert.Equal("Forex", sym.GroupHint);
        Assert.Equal(SymbolGroup.Forex, MessageCodec.ParseGroupHint(sym.GroupHint));
    }

    [Fact]
    public void Tick_round_trip()
    {
        var line = MessageCodec.EncodeTick("EURUSD", 1_710_000_000_000L, 1.10010, 1.10020, 1.5);
        Assert.True(MessageCodec.TryParse(line, out var msg));
        var t = Assert.IsType<MtMessage.TickMsg>(msg);
        Assert.Equal("EURUSD", t.Symbol);
        Assert.Equal(1_710_000_000_000L, t.ServerTimeMs);
        Assert.Equal(1.10010, t.Bid, 5);
        Assert.Equal(1.10020, t.Ask, 5);
        Assert.Equal(1.5, t.Volume, 5);
    }

    [Fact]
    public void HistoryBar_and_HEnd_round_trip()
    {
        var bar = MessageCodec.EncodeHistoryBar("EURUSD", 1_710_000_000L, 1.10, 1.11, 1.09, 1.105, 100);
        Assert.True(MessageCodec.TryParse(bar, out var msg));
        var h = Assert.IsType<MtMessage.HistoryBar>(msg);
        Assert.Equal("EURUSD", h.Symbol);
        Assert.Equal("M1", h.Tf);
        Assert.Equal(1_710_000_000L, h.ServerTimeSec);
        Assert.Equal(1.10, h.O, 5);
        Assert.Equal(1.11, h.H, 5);
        Assert.Equal(1.09, h.L, 5);
        Assert.Equal(1.105, h.C, 5);
        Assert.Equal(100, h.Vol, 5);

        var end = MessageCodec.EncodeHistoryEnd("EURUSD", 1);
        Assert.True(MessageCodec.TryParse(end, out var endMsg));
        var he = Assert.IsType<MtMessage.HistoryEnd>(endMsg);
        Assert.Equal("EURUSD", he.Symbol);
        Assert.Equal(1, he.Count);
    }

    [Fact]
    public void GetHistory_encode_and_parse()
    {
        var from = DateTimeOffset.FromUnixTimeSeconds(1_700_000_000);
        var to = DateTimeOffset.FromUnixTimeSeconds(1_700_003_600);
        var line = MessageCodec.EncodeGetHistory("EURUSD", from, to, 500);
        Assert.True(MessageCodec.TryParse(line, out var msg));
        var g = Assert.IsType<MtMessage.GetHistory>(msg);
        Assert.Equal("EURUSD", g.Symbol);
        Assert.Equal("M1", g.Tf);
        Assert.Equal(1_700_000_000L, g.FromUtcSeconds);
        Assert.Equal(1_700_003_600L, g.ToUtcSeconds);
        Assert.Equal(500, g.MaxBars);
    }

    [Fact]
    public void Ping_Pong_parse()
    {
        Assert.True(MessageCodec.TryParse("PING", out var p1));
        Assert.IsType<MtMessage.PingMsg>(p1);
        Assert.True(MessageCodec.TryParse("PONG", out var p2));
        Assert.IsType<MtMessage.PongMsg>(p2);
    }

    [Fact]
    public void ServerMsToUtc_subtracts_offset()
    {
        var offset = TimeSpan.FromHours(2);
        var serverMs = DateTimeOffset.Parse("2024-03-13T14:00:00Z").ToUnixTimeMilliseconds();
        var utc = MessageCodec.ServerMsToUtc(serverMs, offset);
        Assert.Equal(DateTimeOffset.Parse("2024-03-13T12:00:00Z"), utc);
    }

    [Fact]
    public void Invalid_line_returns_false()
    {
        Assert.False(MessageCodec.TryParse("NOPE|x", out _));
        Assert.False(MessageCodec.TryParse("", out _));
    }
}
