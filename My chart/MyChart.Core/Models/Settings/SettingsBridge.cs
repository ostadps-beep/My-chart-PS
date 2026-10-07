using MyChart.Core.Contracts.Services;

namespace MyChart.Core.Models.Settings;

/// <summary>
/// T6.08 SettingsBridge core — implements IChartSettings.
/// Protocol OnUpdate/OnApplied/OnSave without referencing the Settings panel (R2/R5).
/// App hosts the real SettingsWindow and forwards protocol messages here.
/// </summary>
public sealed class SettingsBridge : IChartSettings
{
    private readonly ChartSettingValues _values = new();
    private readonly List<string> _pending = new();
    private int _invalidationCount;

    public ChartSettingValues Values => _values;
    public event Action<IReadOnlyList<string>>? Changed;
    public int InvalidationCount => _invalidationCount;

    public void OnUpdate(string key, object? value)
    {
        if (SettingsKeyRouter.TryApply(_values, key, value))
        {
            _pending.Add(key);
            Changed?.Invoke(new[] { key });
        }
    }

    public void OnApplied(IReadOnlyList<string>? changed = null)
    {
        _invalidationCount++;
        if (changed is { Count: > 0 })
            Changed?.Invoke(changed);
        else if (_pending.Count > 0)
            Changed?.Invoke(_pending.ToList());
        _pending.Clear();
    }

    public void OnSave(string path)
        => System.Diagnostics.Debug.WriteLine($"[Settings] saved: {path}");

    /// <summary>R6 SEED — notify listeners for every known key once.</summary>
    public void Seed()
    {
        var keys = SettingsKeyRouter.KnownKeys.OrderBy(k => k, StringComparer.Ordinal).ToList();
        if (keys.Count > 0)
            Changed?.Invoke(keys);
    }
}
