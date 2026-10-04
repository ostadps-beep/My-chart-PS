using MyChart.Core.Models.Market;

namespace MyChart.Core.Contracts.Data;

public interface ITickManager
{
    void Receive(string symbol, Tick tick);
}
