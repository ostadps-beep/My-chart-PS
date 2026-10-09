using MyChart.Core.Contracts.UI;
using MyChart.Core.Models.Plugins;

namespace MyChart.Core.UI.Icons;

/// <summary>
/// T7.01 IconSystem — catalog lookup and required first-party icon keys.
/// UI converts <see cref="IconDescriptor.PathData"/> to StreamGeometry; this type owns no geometry.
/// </summary>
public sealed class IconSystemModel : IIconProvider
{
    /// <summary>CONSTANTS_TABLE IconViewBox — Fluent outline icons at 16 DIP.</summary>
    public const int IconViewBox = 16;

    /// <summary>
    /// PG5.02 / T7.01 NAMES — Icon.&lt;Name&gt; keys that must exist for chrome.
    /// </summary>
    public static IReadOnlyList<string> RequiredIconKeys { get; } = new[]
    {
        "Icon.Cursor",
        "Icon.Crosshair",
        "Icon.TrendLine",
        "Icon.Rectangle",
        "Icon.Arrow",
        "Icon.Text",
        "Icon.Fibonacci",
        "Icon.Measure",
        "Icon.Symbol",
        "Icon.Timeframe",
        "Icon.ChartType",
        "Icon.Indicators",
        "Icon.Templates",
        "Icon.Layout",
        "Icon.Settings",
        "Icon.Color",
        "Icon.Width",
        "Icon.Opacity",
        "Icon.Style",
        "Icon.Template",
        "Icon.Lock",
        "Icon.Clone",
        "Icon.Delete",
    };

    private readonly Dictionary<string, IconDescriptor> _byKey;
    private readonly IReadOnlyList<IconDescriptor> _all;

    public IconSystemModel(IEnumerable<IconDescriptor> descriptors)
    {
        ArgumentNullException.ThrowIfNull(descriptors);
        _byKey = new Dictionary<string, IconDescriptor>(StringComparer.Ordinal);
        var list = new List<IconDescriptor>();
        foreach (var d in descriptors)
        {
            ArgumentNullException.ThrowIfNull(d);
            if (string.IsNullOrWhiteSpace(d.IconKey))
                throw new ArgumentException("IconKey is required.", nameof(descriptors));
            if (!_byKey.TryAdd(d.IconKey, d))
                throw new ArgumentException($"Duplicate IconKey '{d.IconKey}'.", nameof(descriptors));
            list.Add(d);
        }

        list.Sort((a, b) => string.CompareOrdinal(a.IconKey, b.IconKey));
        _all = list;
    }

    public int ViewBox => IconViewBox;

    public IReadOnlyList<IconDescriptor> All => _all;

    public bool TryGet(string iconKey, out IconDescriptor descriptor)
        => _byKey.TryGetValue(iconKey, out descriptor!);

    public bool Contains(string iconKey) => _byKey.ContainsKey(iconKey);

    /// <summary>True when every RequiredIconKey is present.</summary>
    public bool HasAllRequiredKeys()
    {
        foreach (var key in RequiredIconKeys)
        {
            if (!_byKey.ContainsKey(key))
                return false;
        }

        return true;
    }

    /// <summary>Missing required keys (ordinal order of RequiredIconKeys).</summary>
    public IReadOnlyList<string> MissingRequiredKeys()
    {
        var missing = new List<string>();
        foreach (var key in RequiredIconKeys)
        {
            if (!_byKey.ContainsKey(key))
                missing.Add(key);
        }

        return missing;
    }
}
