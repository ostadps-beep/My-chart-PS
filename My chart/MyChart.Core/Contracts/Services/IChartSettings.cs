using MyChart.Core.Models.Settings;

namespace MyChart.Core.Contracts.Services;

public interface IChartSettings
{
    ChartSettingValues Values { get; }
    event Action<IReadOnlyList<string>>? Changed;
}
