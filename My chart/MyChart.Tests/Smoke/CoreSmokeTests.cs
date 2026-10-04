using MyChart.Core.Models.Market;
using Xunit;

namespace MyChart.Tests.Smoke;

public class CoreSmokeTests
{
    [Fact]
    public void Candle_IsBullish_WhenCloseGreaterOrEqualOpen()
    {
        var c = new Candle(DateTimeOffset.UtcNow, 1.0, 1.2, 0.9, 1.1, 100);
        Assert.True(c.IsBullish);
        Assert.False(c.IsBearish);
    }

    [Fact]
    public void Candle_IsBearish_WhenCloseLessThanOpen()
    {
        var c = new Candle(DateTimeOffset.UtcNow, 1.1, 1.2, 0.9, 1.0, 100);
        Assert.True(c.IsBearish);
        Assert.False(c.IsBullish);
    }
}
