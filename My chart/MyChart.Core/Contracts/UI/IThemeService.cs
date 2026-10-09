using MyChart.Core.Models.Rendering;

namespace MyChart.Core.Contracts.UI;

/// <summary>T4.02 / T7.02 — renderers obtain colors only through this service.</summary>
public interface IThemeService
{
    ThemeTokens Current { get; }
    string ProfileName { get; }
}
