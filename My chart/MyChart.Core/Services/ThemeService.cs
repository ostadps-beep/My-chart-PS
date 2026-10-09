using MyChart.Core.Contracts.UI;
using MyChart.Core.Models.Rendering;

namespace MyChart.Core.Services;

/// <summary>T7.02 — profile switch + optional Custom overrides.</summary>
public sealed class ThemeService : IThemeService
{
    private ThemeTokens _current;
    private ThemeProfileKind _kind;
    private IReadOnlyDictionary<string, string>? _customOverrides;

    public ThemeService(ThemeProfileKind initial = ThemeProfileKind.Dark)
    {
        _kind = initial;
        _current = ThemeTokens.ForProfile(initial);
    }

    public ThemeTokens Current => _current;
    public string ProfileName => _kind.ToString();
    public ThemeProfileKind ProfileKind => _kind;

    public void SetProfile(ThemeProfileKind kind)
    {
        _kind = kind;
        _current = kind == ThemeProfileKind.Custom && _customOverrides is not null
            ? ThemeTokens.ApplyOverrides(ThemeTokens.Dark, _customOverrides)
            : ThemeTokens.ForProfile(kind);
    }

    /// <summary>Custom profile: overrides on Dark (ChartMy) base.</summary>
    public void SetCustom(IReadOnlyDictionary<string, string> overrides)
    {
        _customOverrides = overrides;
        _kind = ThemeProfileKind.Custom;
        _current = ThemeTokens.ApplyOverrides(ThemeTokens.Dark, overrides);
    }
}
