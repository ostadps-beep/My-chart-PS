using MyChart.Data.Providers.MetaTrader;
using Xunit;

namespace MyChart.Tests.Market;

public class MessageCodecTests
{
    [Theory]
    [InlineData("HELLO|1.00|180")]
    [InlineData("SYM|EURUSD|5|Forex")]
    [InlineData("T|EURUSD|1710000000000|1.1|1.1001|1")]
    [InlineData("H|EURUSD|M1|1710000000|1.1|1.11|1.09|1.105|10")]
    [InlineData("HEND|EURUSD|3")]
    [InlineData("PING")]
    [InlineData("PONG")]
    [InlineData("GETH|EURUSD|M1|100|200|500")]
    public void RoundTrip(string line)
    {
        var msg = MessageCodec.Decode(line);
        var encoded = MessageCodec.Encode(msg);
        var again = MessageCodec.Decode(encoded);
        Assert.Equal(msg.GetType(), again.GetType());
        Assert.Equal(encoded, MessageCodec.Encode(again));
    }

    [Fact]
    public void Hello_ParsesOffset()
    {
        var msg = Assert.IsType<MtMessage.Hello>(MessageCodec.Decode("HELLO|2.1|120"));
        Assert.Equal("2.1", msg.EaVersion);
        Assert.Equal(120, msg.ServerUtcOffsetMinutes);
    }

    [Fact]
    public void Tick_ParsesFields()
    {
        var msg = Assert.IsType<MtMessage.Tick>(MessageCodec.Decode("T|EURUSD|1000|1.1|1.2|3"));
        Assert.Equal("EURUSD", msg.Symbol);
        Assert.Equal(1000, msg.ServerTimeMs);
        Assert.Equal(1.1, msg.Bid);
        Assert.Equal(1.2, msg.Ask);
        Assert.Equal(3, msg.Volume);
    }
}
