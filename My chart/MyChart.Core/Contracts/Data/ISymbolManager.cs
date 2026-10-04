using MyChart.Core.Models.Market;

namespace MyChart.Core.Contracts.Data;

public interface ISymbolManager
{
    IReadOnlyList<SymbolInfo> GetAll();
    SymbolInfo? Get(string name);
}
