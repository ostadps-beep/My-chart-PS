using MyChart.Core.Models.Market;

namespace MyChart.Core.Candles;

/// <summary>
/// T1.04 SymbolRegistry — Register / Get / List / alias map.
/// Normalization = Trim + ToUpperInvariant only; suffixes never stripped automatically.
/// </summary>
public sealed class SymbolRegistry
{
    private readonly Dictionary<string, SymbolInfo> _byName = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _aliasToCanonical = new(StringComparer.Ordinal);

    // Known ISO 4217 currency codes used for GroupFallback (Forex detection)
    private static readonly HashSet<string> IsoCurrencies = new(StringComparer.Ordinal)
    {
        "USD","EUR","GBP","JPY","CHF","AUD","NZD","CAD",
        "SEK","NOK","DKK","TRY","ZAR","MXN","SGD","HKD",
        "CNH","CNY","PLN","HUF","CZK","ILS","RUB","INR",
        "BRL","KRW","TWD","THB","MYR","IDR","PHP","AED",
        "SAR","QAR","KWD","BHD","OMR","EGP","XAU","XAG"
    };

    public void Register(SymbolInfo info)
    {
        var canonical = Normalize(info.Name);
        var provider = Normalize(info.ProviderName);

        var group = info.Group;
        // GroupFallback only when caller left group at default? Spec: provider group if given.
        // SymbolInfo always has Group; we trust the provided group unless using InferGroup.

        var stored = info with { Name = canonical, ProviderName = provider, Group = group };
        _byName[canonical] = stored;

        if (!string.IsNullOrEmpty(provider) && provider != canonical)
            _aliasToCanonical[provider] = canonical;
    }

    public void AddAlias(string providerName, string canonicalName)
    {
        _aliasToCanonical[Normalize(providerName)] = Normalize(canonicalName);
    }

    public SymbolInfo? Get(string name)
    {
        var key = Normalize(name);
        if (_byName.TryGetValue(key, out var info))
            return info;

        if (_aliasToCanonical.TryGetValue(key, out var canonical)
            && _byName.TryGetValue(canonical, out info))
            return info;

        return null;
    }

    public IReadOnlyList<SymbolInfo> List()
        => _byName.Values.OrderBy(s => s.Name, StringComparer.Ordinal).ToList();

    public static string Normalize(string name)
        => (name ?? string.Empty).Trim().ToUpperInvariant();

    /// <summary>
    /// GroupFallback: Forex when name is 6 letters made of two known ISO currency codes; else Stocks.
    /// </summary>
    public static SymbolGroup InferGroup(string name)
    {
        var n = Normalize(name);
        if (n.Length == 6
            && IsoCurrencies.Contains(n[..3])
            && IsoCurrencies.Contains(n[3..]))
            return SymbolGroup.Forex;

        return SymbolGroup.Stocks;
    }
}
