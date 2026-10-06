namespace MyChart.Core.Candles;

/// <summary>
/// T5.04 — list of canonical symbol names stored in workspace LayoutData (not Settings).
/// </summary>
public sealed class FavoritesList
{
    private readonly List<string> _names = new();

    public FavoritesList() { }

    public FavoritesList(IEnumerable<string> names)
    {
        foreach (var n in names)
            Add(n);
    }

    public IReadOnlyList<string> Names => _names;

    public int Count => _names.Count;

    public bool Contains(string canonicalName)
    {
        var key = SymbolRegistry.Normalize(canonicalName);
        return _names.Exists(n => n == key);
    }

    public bool Add(string canonicalName)
    {
        var key = SymbolRegistry.Normalize(canonicalName);
        if (string.IsNullOrEmpty(key) || _names.Contains(key))
            return false;
        _names.Add(key);
        return true;
    }

    public bool Remove(string canonicalName)
    {
        var key = SymbolRegistry.Normalize(canonicalName);
        return _names.Remove(key);
    }

    public void Clear() => _names.Clear();
}
