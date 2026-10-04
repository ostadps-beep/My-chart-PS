using MyChart.Core.Contracts.Services;

namespace MyChart.Core.Models.Settings;

/// <summary>
/// Default implementation of IChartSettings used until the real Settings panel is integrated at T6.08.
/// Values equal the defaults listed in SETTINGS_KEY_MAP.
/// </summary>
public sealed class DefaultChartSettings : IChartSettings
{
    public ChartSettingValues Values { get; } = new();

    public event Action<IReadOnlyList<string>>? Changed;

    public static DefaultChartSettings Create() => new();

    /// <summary>Raises Changed for the given keys (used by future SettingsBridge).</summary>
    public void RaiseChanged(params string[] keys)
    {
        if (keys.Length > 0)
            Changed?.Invoke(keys);
    }
}
