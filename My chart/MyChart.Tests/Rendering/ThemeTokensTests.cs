using MyChart.Core.Models.Rendering;
using MyChart.Core.Services;
using Xunit;

namespace MyChart.Tests.Rendering;

/// <summary>T7.02 — Dark = ChartMy panel palette (owner single global theme).</summary>
public class ThemeTokensTests
{
    [Fact]
    public void DarkProfile_MatchesChartMyPanel()
    {
        var t = ThemeService.Dark;
        Assert.Equal(RgbaColor.ParseHex("#1E1E1E"), t.BackgroundColor);
        Assert.Equal(RgbaColor.ParseHex("#252525"), t.GridColor);
        Assert.Equal(RgbaColor.ParseHex("#2A2A2A"), t.GridMajorColor);
        Assert.Equal(RgbaColor.ParseHex("#252525"), t.GridMinorColor);
        Assert.Equal(RgbaColor.ParseHex("#AAAAAA"), t.AxisColor);
        Assert.Equal(RgbaColor.ParseHex("#FFFFFF"), t.HudColor);
        Assert.Equal(RgbaColor.ParseHex("#4CAF50"), t.AccentColor);
        Assert.Equal(RgbaColor.ParseHex("#4CAF50"), t.SelectionColor);
        Assert.Equal(RgbaColor.ParseHex("#26A69A"), t.BullColor);
        Assert.Equal(RgbaColor.ParseHex("#EF5350"), t.BearColor);
        Assert.Equal(RgbaColor.ParseHex("#FF9800"), t.WarningColor);
        Assert.Equal(RgbaColor.ParseHex("#EF5350"), t.ErrorColor);
        Assert.Equal(RgbaColor.ParseHex("#4CAF50"), t.SuccessColor);
    }

    [Fact]
    public void IndicatorPalette_SixColors()
    {
        var t = ThemeService.Dark;
        Assert.Equal(6, t.IndicatorPalette.Count);
        Assert.Equal(RgbaColor.ParseHex("#4CAF50"), t.IndicatorPalette[0]);
        Assert.Equal(RgbaColor.ParseHex("#8BC34A"), t.IndicatorPalette[5]);
        Assert.Equal(t.IndicatorPalette[1], t.IndicatorColor(7));
    }

    [Fact]
    public void ThemeService_DefaultIsDark()
    {
        var svc = new ThemeService();
        Assert.Equal("Dark", svc.ProfileName);
        Assert.Equal(ThemeService.Dark.AccentColor, svc.Current.AccentColor);
    }

    [Fact]
    public void ParseHex_RgbAndArgb()
    {
        var c = RgbaColor.ParseHex("#26A69A");
        Assert.Equal(0xFF, c.A);
        Assert.Equal(0x26, c.R);
        Assert.Equal(0xA6, c.G);
        Assert.Equal(0x9A, c.B);
    }

    [Fact]
    public void WickAndBorder_Defaults()
    {
        Assert.Equal(RgbaColor.ParseHex("#CCCCCC"), ThemeService.Dark.WickColor);
        Assert.Equal(RgbaColor.ParseHex("#333333"), ThemeService.Dark.BorderColor);
    }
}
