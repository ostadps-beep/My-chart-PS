using MyChart.Core.Models.Market;

namespace MyChart.Core.Contracts.Data;

public interface ITimeframeManager
{
    IReadOnlyList<Timeframe> GetAll();
}
