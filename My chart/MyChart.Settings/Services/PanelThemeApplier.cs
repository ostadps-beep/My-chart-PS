using System.Windows;
using System.Windows.Media;

namespace MyChart.Settings.Services;

/// <summary>
/// T7.02 — applies MyChart theme hex values onto Settings Themes/Dark.xaml colour keys.
/// Host passes colours already resolved by Core PanelThemeMap (Settings must not reference Core).
/// Visual approval: Settings panel body (BgColor / PanelColor) shows the active theme.
/// </summary>
public static class PanelThemeApplier
{
    public static readonly string[] ColorKeys =
    {
        "BgColor", "PanelColor", "ControlColor", "HoverColor", "ActiveColor",
        "BorderColor", "ButtonBorderColor", "TextColor", "MutedColor", "AccentColor"
    };

    public static void Apply(ResourceDictionary resources, IReadOnlyDictionary<string, string> colorHexByKey)
    {
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(colorHexByKey);

        foreach (var key in ColorKeys)
        {
            if (!colorHexByKey.TryGetValue(key, out var hex) || string.IsNullOrWhiteSpace(hex))
                continue;
            var color = (Color)ColorConverter.ConvertFromString(hex)!;
            resources[key] = color;

            var brushKey = key.EndsWith("Color", StringComparison.Ordinal)
                ? key[..^5] + "Brush"
                : key + "Brush";
            if (resources.Contains(brushKey))
            {
                var brush = new SolidColorBrush(color);
                brush.Freeze();
                resources[brushKey] = brush;
            }
        }
    }
}
