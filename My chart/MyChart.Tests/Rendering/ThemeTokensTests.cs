using MyChart.Core.Models.Rendering;
using MyChart.Core.Models.Settings;
using MyChart.Core.Services;
using Xunit;

namespace MyChart.Tests.Rendering;

/// <summary>C1 — Dark = Settings panel palette (CORRECTIONS_2026-10-10).</summary>
public class ThemeTokensTests
{
    [Fact]
    public void DarkProfile_MatchesSpecHex()
    {
        var t = ThemeService.Dark;
        Assert.Equal(RgbaColor.ParseHex("#1E1E1E"), t.BackgroundColor);
        Assert.Equal(RgbaColor.ParseHex("#2A2E39"), t.GridColor);
        Assert.Equal(RgbaColor.ParseHex("#2A2E39"), t.GridMajorColor);
        Assert.Equal(RgbaColor.FromArgb(128, 0x2A, 0x2E, 0x39), t.GridMinorColor);
        Assert.Equal(RgbaColor.ParseHex("#888888"), t.AxisColor);
        Assert.Equal(RgbaColor.ParseHex("#FFFFFF"), t.HudColor);
        Assert.Equal(RgbaColor.ParseHex("#2962FF"), t.AccentColor);
        Assert.Equal(RgbaColor.ParseHex("#2962FF"), t.SelectionColor);
        Assert.Equal(RgbaColor.ParseHex("#26A69A"), t.BullColor);
        Assert.Equal(RgbaColor.ParseHex("#EF5350"), t.BearColor);
        Assert.Equal(RgbaColor.ParseHex("#FF9800"), t.WarningColor);
        Assert.Equal(RgbaColor.ParseHex("#F23645"), t.ErrorColor);
        Assert.Equal(RgbaColor.ParseHex("#089981"), t.SuccessColor);
    }

    [Fact]
    public void ThemeTokens_Dark_Equals_ThemeService_Dark()
    {
        var a = ThemeTokens.Dark;
        var b = ThemeService.Dark;
        Assert.Equal(a.BackgroundColor, b.BackgroundColor);
        Assert.Equal(a.GridColor, b.GridColor);
        Assert.Equal(a.GridMajorColor, b.GridMajorColor);
        Assert.Equal(a.GridMinorColor, b.GridMinorColor);
        Assert.Equal(a.AxisColor, b.AxisColor);
        Assert.Equal(a.HudColor, b.HudColor);
        Assert.Equal(a.AccentColor, b.AccentColor);
        Assert.Equal(a.BullColor, b.BullColor);
        Assert.Equal(a.BearColor, b.BearColor);
        Assert.Equal(a.WickColor, b.WickColor);
        Assert.Equal(a.BorderColor, b.BorderColor);
    }

    [Fact]
    public void Dark_Matches_ChartSettingValues_Defaults()
    {
        var v = new ChartSettingValues();
        var t = ThemeService.Dark;
        Assert.Equal(RgbaColor.ParseHex(v.BackgroundColor), t.BackgroundColor);
        Assert.Equal(RgbaColor.ParseHex(v.GridHorizontalColor), t.GridColor);
        Assert.Equal(RgbaColor.ParseHex(v.AxisColor), t.AxisColor);
        Assert.Equal(RgbaColor.ParseHex(v.HudTextColor), t.HudColor);
        Assert.Equal(RgbaColor.ParseHex(v.AccentColor), t.AccentColor);
        Assert.Equal(RgbaColor.ParseHex(v.BullColor), t.BullColor);
        Assert.Equal(RgbaColor.ParseHex(v.BearColor), t.BearColor);
        Assert.Equal(RgbaColor.ParseHex(v.WickColor), t.WickColor);
        Assert.Equal(RgbaColor.ParseHex(v.BorderColor), t.BorderColor);
    }

    [Fact]
    public void IndicatorPalette_SixColors()
    {
        var t = ThemeService.Dark;
        Assert.Equal(6, t.IndicatorPalette.Count);
        Assert.Equal(RgbaColor.ParseHex("#2962FF"), t.IndicatorPalette[0]);
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
        Assert.Equal(RgbaColor.ParseHex("#1A1A1A"), ThemeService.Dark.BorderColor);
    }
}
