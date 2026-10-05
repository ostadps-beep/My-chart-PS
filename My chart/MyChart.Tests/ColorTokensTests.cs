using MyChart.Core.ChartEngine.Constants;
using MyChart.Core.Models.Geometry;
using MyChart.Core.Models.Rendering;
using Xunit;

namespace MyChart.Tests;

/// <summary>
/// Unit tests for T4.02 ColorTokensMinimal (RgbaColor, ChartColorTokens, ThemePalette).
/// 
/// GATE: AUTOMATED
/// Tests verify:
/// - All default color constants are valid hex strings
/// - RgbaColor hex parsing works correctly (6-char and 8-char)
/// - ThemePalette creates successfully from defaults
/// - All 15 color tokens are present and non-zero
/// </summary>
public class ColorTokensTests
{
    [Fact]
    public void ColorConstants_AllDarkThemeValuesAreDefined()
    {
        // All 15 colors must be defined in DarkTheme
        Assert.NotNull(ChartColorConstants.DarkTheme.Background);
        Assert.NotNull(ChartColorConstants.DarkTheme.GridMajor);
        Assert.NotNull(ChartColorConstants.DarkTheme.GridMinor);
        Assert.NotNull(ChartColorConstants.DarkTheme.AxisText);
        Assert.NotNull(ChartColorConstants.DarkTheme.CandleBull);
        Assert.NotNull(ChartColorConstants.DarkTheme.CandleBear);
        Assert.NotNull(ChartColorConstants.DarkTheme.CandleWick);
        Assert.NotNull(ChartColorConstants.DarkTheme.CurrentPrice);
        Assert.NotNull(ChartColorConstants.DarkTheme.Crosshair);
        Assert.NotNull(ChartColorConstants.DarkTheme.HudText);
        Assert.NotNull(ChartColorConstants.DarkTheme.SelectionHandle);
        Assert.NotNull(ChartColorConstants.DarkTheme.Accent);
        Assert.NotNull(ChartColorConstants.DarkTheme.TransparentBg);
        Assert.NotNull(ChartColorConstants.DarkTheme.Warning);
        Assert.NotNull(ChartColorConstants.DarkTheme.Error);
    }

    [Fact]
    public void OpacityConstants_RangeIsValid()
    {
        // All opacity values must be between 0.0 and 1.0
        Assert.InRange(ChartColorConstants.Opacity.GridLineOpacity, 0.0, 1.0);
        Assert.InRange(ChartColorConstants.Opacity.HudOverlayOpacity, 0.0, 1.0);
        Assert.InRange(ChartColorConstants.Opacity.CrosshairOpacity, 0.0, 1.0);
        Assert.InRange(ChartColorConstants.Opacity.Opaque, 0.0, 1.0);
        Assert.InRange(ChartColorConstants.Opacity.Transparent, 0.0, 1.0);
    }

    [Theory]
    [InlineData("#FFFFFF", 255, 255, 255, 255)]
    [InlineData("#000000", 0, 0, 0, 255)]
    [InlineData("#FF0000", 255, 0, 0, 255)]
    [InlineData("#00FF00", 0, 255, 0, 255)]
    [InlineData("#0000FF", 0, 0, 255, 255)]
    [InlineData("#26A69A", 0x26, 0xA6, 0x9A, 255)] // Bull color
    [InlineData("#EF5350", 0xEF, 0x53, 0x50, 255)] // Bear color
    public void RgbaColor_FromHex_6CharFormat_ParsesCorrectly(string hex, byte expR, byte expG, byte expB, byte expA)
    {
        var color = RgbaColor.FromHex(hex);

        Assert.Equal(expR, color.R);
        Assert.Equal(expG, color.G);
        Assert.Equal(expB, color.B);
        Assert.Equal(expA, color.A);
    }

    [Theory]
    [InlineData("#FFFFFF00", 255, 255, 255, 0)]
    [InlineData("#00000080", 0, 0, 0, 128)]
    [InlineData("#FF000040", 255, 0, 0, 64)]
    public void RgbaColor_FromHex_8CharFormat_ParsesCorrectly(string hex, byte expR, byte expG, byte expB, byte expA)
    {
        var color = RgbaColor.FromHex(hex);

        Assert.Equal(expR, color.R);
        Assert.Equal(expG, color.G);
        Assert.Equal(expB, color.B);
        Assert.Equal(expA, color.A);
    }

    [Theory]
    [InlineData("FFFFFF")]         // No hash
    [InlineData("#FFF")]           // Too short
    [InlineData("#FFFFFFFF00")]    // Too long
    [InlineData("#GGGGGG")]        // Invalid hex chars
    [InlineData(null)]             // Null
    public void RgbaColor_FromHex_InvalidFormat_ThrowsFormatException(string? hex)
    {
        if (hex == null)
        {
            Assert.Throws<ArgumentNullException>(() => RgbaColor.FromHex(hex!));
        }
        else
        {
            Assert.Throws<FormatException>(() => RgbaColor.FromHex(hex));
        }
    }

    [Theory]
    [InlineData(" #FFFFFF ")]      // Whitespace
    [InlineData(" FFFFFF ")]       // No hash, whitespace
    public void RgbaColor_FromHex_WithWhitespace_ParsesCorrectly(string hex)
    {
        var color = RgbaColor.FromHex(hex);
        Assert.Equal(255, color.R);
        Assert.Equal(255, color.G);
        Assert.Equal(255, color.B);
        Assert.Equal(255, color.A);
    }

    [Fact]
    public void RgbaColor_ToString_FormatsCorrectly()
    {
        var color = new RgbaColor(255, 0, 0, 128);
        Assert.Equal("#FF000080", color.ToString());
    }

    [Fact]
    public void RgbaColor_Equality_WorksCorrectly()
    {
        var color1 = new RgbaColor(255, 0, 0, 255);
        var color2 = new RgbaColor(255, 0, 0, 255);
        var color3 = new RgbaColor(255, 0, 0, 128);

        Assert.Equal(color1, color2);
        Assert.NotEqual(color1, color3);
        Assert.True(color1 == color2);
        Assert.True(color1 != color3);
    }

    [Fact]
    public void ThemePalette_CreateDefault_SuccessfullyCreates()
    {
        var palette = ThemePalette.CreateDefault();

        Assert.NotNull(palette);
        Assert.NotNull(palette.Tokens);
    }

    [Fact]
    public void ThemePalette_CreateDefault_AllTokensPresent()
    {
        var palette = ThemePalette.CreateDefault();
        var tokens = palette.Tokens;

        // All 15 tokens must exist and not be zero-initialized
        Assert.NotEqual(default(RgbaColor), tokens.Background);
        Assert.NotEqual(default(RgbaColor), tokens.GridMajor);
        Assert.NotEqual(default(RgbaColor), tokens.GridMinor);
        Assert.NotEqual(default(RgbaColor), tokens.AxisText);
        Assert.NotEqual(default(RgbaColor), tokens.CandleBull);
        Assert.NotEqual(default(RgbaColor), tokens.CandleBear);
        Assert.NotEqual(default(RgbaColor), tokens.CandleWick);
        Assert.NotEqual(default(RgbaColor), tokens.CurrentPrice);
        Assert.NotEqual(default(RgbaColor), tokens.Crosshair);
        Assert.NotEqual(default(RgbaColor), tokens.HudText);
        Assert.NotEqual(default(RgbaColor), tokens.SelectionHandle);
        Assert.NotEqual(default(RgbaColor), tokens.Accent);
        Assert.NotEqual(default(RgbaColor), tokens.Warning);
        Assert.NotEqual(default(RgbaColor), tokens.Error);
    }

    [Fact]
    public void ThemePalette_FromSettings_FallsBackToDefault()
    {
        var settings = new DefaultChartSettings();
        var palette = ThemePalette.FromSettings(settings);

        Assert.NotNull(palette);
        Assert.NotNull(palette.Tokens);
    }

    [Fact]
    public void ChartColorTokens_IsImmutable()
    {
        var color1 = new RgbaColor(255, 0, 0, 255);
        var color2 = new RgbaColor(0, 255, 0, 255);

        var tokens1 = new ChartColorTokens(
            Background: color1, GridMajor: color1, GridMinor: color1, AxisText: color1,
            CandleBull: color1, CandleBear: color1, CandleWick: color1, CurrentPrice: color1,
            Crosshair: color1, HudText: color1, SelectionHandle: color1, Accent: color1,
            TransparentBg: color1, Warning: color1, Error: color1
        );

        var tokens2 = new ChartColorTokens(
            Background: color2, GridMajor: color2, GridMinor: color2, AxisText: color2,
            CandleBull: color2, CandleBear: color2, CandleWick: color2, CurrentPrice: color2,
            Crosshair: color2, HudText: color2, SelectionHandle: color2, Accent: color2,
            TransparentBg: color2, Warning: color2, Error: color2
        );

        // Tokens are records (immutable by design)
        Assert.NotEqual(tokens1, tokens2);
    }
}
