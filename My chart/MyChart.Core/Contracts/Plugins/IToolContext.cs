using MyChart.Core.Models.Geometry;
using MyChart.Core.Models.Market;
using MyChart.Core.Contracts.Services;
using MyChart.Core.Contracts.UI;

namespace MyChart.Core.Contracts.Plugins;

public interface IToolContext
{
    IChartMapper Map { get; }
    IChartSettings Settings { get; }
    IThemeService Theme { get; }
    SymbolInfo Symbol { get; }
    PointD Snap(PointD p);
    void PromptText(Action<string?> onDone);
    void Invalidate();
}
