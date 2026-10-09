using MyChart.Core.Models.Rendering;
using MyChart.Core.Services;
using MyChart.Core.UI.Theme;
using Xunit;

namespace MyChart.Tests.Theme;

/// <summary>T7.02 ThemeProfiles — ChartMy panel palette is the single global Dark source.</summary>
public class ThemeProfileTests
{
    [Fact]
    public void Dark_Matches_ChartMy_Panel_Palette()
    {
        Assert.True(PanelThemeMap.MatchesChartMyOfficial(ThemeTokens.Dark));
    }

    [Fact]
    public void Dark_Token_Hex_Exact()
    {
        var d = ThemeTokens.Dark;
        Assert.Equal(RgbaColor.ParseHex("#1E1E1E"), d.BackgroundColor);
        Assert.Equal(RgbaColor.ParseHex("#252525"), d.GridColor);
        Assert.Equal(RgbaColor.ParseHex("#2A2A2A"), d.GridMajorColor);
        Assert.Equal(RgbaColor.ParseHex("#AAAAAA"), d.AxisColor);
        Assert.Equal(RgbaColor.ParseHex("#FFFFFF"), d.HudColor);
        Assert.Equal(RgbaColor.ParseHex("#4CAF50"), d.AccentColor);
        Assert.Equal(RgbaColor.ParseHex("#4CAF50"), d.SelectionColor);
        Assert.Equal(RgbaColor.ParseHex("#333333"), d.BorderColor);
    }

    [Fact]
    public void Profiles_Dark_Light_ProDark_Exist()
    {
        Assert.NotEqual(ThemeTokens.Dark.BackgroundColor, ThemeTokens.Light.BackgroundColor);
        Assert.NotEqual(ThemeTokens.Dark.BackgroundColor, ThemeTokens.ProDark.BackgroundColor);
        Assert.Equal(RgbaColor.ParseHex("#4CAF50"), ThemeTokens.Light.AccentColor);
        Assert.Equal(RgbaColor.ParseHex("#4CAF50"), ThemeTokens.ProDark.AccentColor);
    }

    [Fact]
    public void ThemeService_Switches_Profile()
    {
        var svc = new ThemeService(ThemeProfileKind.Dark);
        Assert.Equal("Dark", svc.ProfileName);
        Assert.Equal(ThemeTokens.Dark.BackgroundColor, svc.Current.BackgroundColor);

        svc.SetProfile(ThemeProfileKind.Light);
        Assert.Equal("Light", svc.ProfileName);
        Assert.Equal(ThemeTokens.Light.BackgroundColor, svc.Current.BackgroundColor);
    }

    [Fact]
    public void Custom_Overrides_Token()
    {
        var svc = new ThemeService();
        svc.SetCustom(new Dictionary<string, string> { ["Accent"] = "#FF0000" });
        Assert.Equal("Custom", svc.ProfileName);
        Assert.Equal(RgbaColor.ParseHex("#FF0000"), svc.Current.AccentColor);
        Assert.Equal(ThemeTokens.Dark.BackgroundColor, svc.Current.BackgroundColor);
    }

    [Fact]
    public void PanelThemeMap_Keys_Are_Complete()
    {
        var keys = PanelThemeMap.ToPanelHex(ThemeTokens.Dark).Keys.OrderBy(k => k).ToArray();
        Assert.Equal(
            new[]
            {
                "AccentColor", "ActiveColor", "BgColor", "BorderColor", "ButtonBorderColor",
                "ControlColor", "HoverColor", "MutedColor", "PanelColor", "TextColor"
            }.OrderBy(k => k).ToArray(),
            keys);
    }
}
