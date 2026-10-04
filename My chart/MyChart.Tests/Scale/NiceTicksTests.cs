using MyChart.Core.Models.Viewport;
using MyChart.Core.Scale;
using Xunit;

namespace MyChart.Tests.Scale;

public class NiceTicksTests
{
    [Fact]
    public void ComputeStep_ProducesNiceMantissa()
    {
        // span 0.015, height 400 → rawStep = 0.015 / (400/50) = 0.015/8 = 0.001875
        // mag = 0.001; smallest {1,2,2.5,5,10}*mag >= 0.001875 → 0.002
        double step = NiceTicks.ComputeStep(0.015, 400, pointSize: 0.00001);
        Assert.Equal(0.002, step, 9);
    }

    [Fact]
    public void PriceTicks_InsideRange()
    {
        var ticks = NiceTicks.PriceTicks(1.095, 1.111, 0.002);
        Assert.All(ticks, t => Assert.True(t >= 1.095 - 1e-9 && t <= 1.111 + 1e-9));
        Assert.True(ticks.Count > 0);
        Assert.True(ticks.Count <= NiceTicks.MaxPriceLabels(400));
    }

    [Fact]
    public void FormatPrice_Percentage()
    {
        Assert.Equal("+1.25%", NiceTicks.FormatPriceLabel(1.25, 0.01, 5, ScaleTransformKind.Percentage));
        Assert.Equal("-0.50%", NiceTicks.FormatPriceLabel(-0.5, 0.01, 5, ScaleTransformKind.Percentage));
    }

    [Fact]
    public void LabelCount_Cap()
    {
        Assert.Equal(10, NiceTicks.MaxPriceLabels(400)); // 400/40
        Assert.Equal(10, NiceTicks.MaxTimeLabels(800));  // 800/80
    }
}
