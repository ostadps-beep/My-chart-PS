using MyChart.Core.Contracts.Rendering;
using MyChart.Core.Models.Market;
using MyChart.Core.Models.Rendering;

namespace MyChart.Core.Contracts.Plugins;

public enum DrawState { Normal, Hover, Selected, Preview }

public sealed class DrawContext
{
    public required IRenderContext Render { get; init; }
    public required IChartMapper Map { get; init; }
    public required ThemeTokens Theme { get; init; }
    public required SymbolInfo Symbol { get; init; }
    public double DpiScale { get; init; } = 1.0;
    public bool ShowLabels { get; init; } = true;
    public DrawState State { get; init; } = DrawState.Normal;
}
