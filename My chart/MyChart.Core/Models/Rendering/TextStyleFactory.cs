using MyChart.Core.ChartEngine.Constants;

namespace MyChart.Core.Models.Rendering;

/// <summary>
/// Factory for TextStyle instances (T4.01).
/// Centralizes text styling definitions to match spec and constants.
/// </summary>
public static class TextStyleFactory
{
    /// <summary>Create default axis label style (monospace, 11 DIP, not bold).</summary>
    public static TextStyle CreateAxisLabel() => new(
        FontFamily: RenderConstants.Text.DefaultFont,
        SizeDip: RenderConstants.Text.DefaultSizeDip,
        Bold: RenderConstants.Text.DefaultBold
    );

    /// <summary>Create default HUD text style (same as axis labels).</summary>
    public static TextStyle CreateHudText() => CreateAxisLabel();

    /// <summary>Create bold variant for emphasis (headers, important values).</summary>
    public static TextStyle CreateBold() => new(
        FontFamily: RenderConstants.Text.DefaultFont,
        SizeDip: RenderConstants.Text.DefaultSizeDip,
        Bold: true
    );
}
