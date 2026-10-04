namespace MyChart.Core.Models.Market;

public sealed record SymbolInfo(
    string Name,
    string ProviderName,
    SymbolGroup Group,
    int Digits);
