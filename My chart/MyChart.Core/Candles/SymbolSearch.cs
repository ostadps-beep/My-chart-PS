using MyChart.Core.Models.Market;

namespace MyChart.Core.Candles;

/// <summary>
/// T5.04 — case-insensitive search: prefix matches first, then contains;
/// matches Name then ProviderName; maximum 50 results.
/// Groups = Forex, Crypto, Indices, Stocks, Commodities (FINAL).
/// </summary>
public static class SymbolSearch
{
    public const int MaxResults = 50;

    public static IReadOnlyList<SymbolGroup> AllGroups { get; } =
        new[]
        {
            SymbolGroup.Forex,
            SymbolGroup.Crypto,
            SymbolGroup.Indices,
            SymbolGroup.Stocks,
            SymbolGroup.Commodities
        };

    public static IReadOnlyList<SymbolInfo> ByGroup(IEnumerable<SymbolInfo> symbols, SymbolGroup group)
        => symbols
            .Where(s => s.Group == group)
            .OrderBy(s => s.Name, StringComparer.Ordinal)
            .ToList();

    public static IReadOnlyList<SymbolInfo> Search(IEnumerable<SymbolInfo> symbols, string query, int maxResults = MaxResults)
    {
        if (maxResults <= 0)
            return Array.Empty<SymbolInfo>();

        var q = (query ?? string.Empty).Trim();
        if (q.Length == 0)
            return symbols.OrderBy(s => s.Name, StringComparer.Ordinal).Take(maxResults).ToList();

        var needle = q.ToUpperInvariant();
        var list = symbols as IList<SymbolInfo> ?? symbols.ToList();

        var prefixName = new List<SymbolInfo>();
        var prefixProvider = new List<SymbolInfo>();
        var containsName = new List<SymbolInfo>();
        var containsProvider = new List<SymbolInfo>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var s in list)
        {
            var name = s.Name ?? string.Empty;
            var provider = s.ProviderName ?? string.Empty;
            var nameU = name.ToUpperInvariant();
            var provU = provider.ToUpperInvariant();

            bool namePrefix = nameU.StartsWith(needle, StringComparison.Ordinal);
            bool provPrefix = provU.StartsWith(needle, StringComparison.Ordinal);
            bool nameContains = nameU.Contains(needle, StringComparison.Ordinal);
            bool provContains = provU.Contains(needle, StringComparison.Ordinal);

            if (namePrefix)
                TryAdd(prefixName, seen, s);
            else if (provPrefix)
                TryAdd(prefixProvider, seen, s);
            else if (nameContains)
                TryAdd(containsName, seen, s);
            else if (provContains)
                TryAdd(containsProvider, seen, s);
        }

        var result = new List<SymbolInfo>(maxResults);
        void Append(List<SymbolInfo> src)
        {
            foreach (var s in src.OrderBy(x => x.Name, StringComparer.Ordinal))
            {
                if (result.Count >= maxResults)
                    return;
                result.Add(s);
            }
        }

        Append(prefixName);
        Append(prefixProvider);
        Append(containsName);
        Append(containsProvider);
        return result;
    }

    public static IReadOnlyList<SymbolInfo> Search(SymbolRegistry registry, string query, int maxResults = MaxResults)
        => Search(registry.List(), query, maxResults);

    private static void TryAdd(List<SymbolInfo> bucket, HashSet<string> seen, SymbolInfo s)
    {
        if (seen.Add(s.Name))
            bucket.Add(s);
    }
}
