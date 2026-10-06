using MyChart.Core.Candles;
using MyChart.Core.Models.Market;
using MyChart.Core.Serialization;
using Xunit;

namespace MyChart.Tests.Market;

/// <summary>T5.04 SymbolGroupsFavoritesSearch VERIFY.</summary>
public class SymbolGroupsFavoritesSearchTests
{
    private static SymbolInfo S(string name, SymbolGroup g, string? provider = null, int digits = 5)
        => new(name, provider ?? name, g, digits);

    private static List<SymbolInfo> SampleUniverse() =>
    [
        S("EURUSD", SymbolGroup.Forex),
        S("GBPUSD", SymbolGroup.Forex),
        S("USDJPY", SymbolGroup.Forex, "USDJPY.m", 3),
        S("BTCUSD", SymbolGroup.Crypto),
        S("ETHUSD", SymbolGroup.Crypto),
        S("US30", SymbolGroup.Indices),
        S("AAPL", SymbolGroup.Stocks),
        S("MSFT", SymbolGroup.Stocks),
        S("XAUUSD", SymbolGroup.Commodities, digits: 2),
        S("XAGUSD", SymbolGroup.Commodities, digits: 3),
    ];

    [Fact]
    public void AllGroups_AreFinalFive()
    {
        Assert.Equal(
            new[] { SymbolGroup.Forex, SymbolGroup.Crypto, SymbolGroup.Indices, SymbolGroup.Stocks, SymbolGroup.Commodities },
            SymbolSearch.AllGroups);
    }

    [Fact]
    public void ByGroup_ReturnsOnlyMatching()
    {
        var forex = SymbolSearch.ByGroup(SampleUniverse(), SymbolGroup.Forex);
        Assert.Equal(3, forex.Count);
        Assert.All(forex, s => Assert.Equal(SymbolGroup.Forex, s.Group));

        var crypto = SymbolSearch.ByGroup(SampleUniverse(), SymbolGroup.Crypto);
        Assert.Equal(2, crypto.Count);
    }

    [Fact]
    public void Search_PrefixBeforeContains()
    {
        var universe = SampleUniverse();
        universe.Add(S("EURAUD", SymbolGroup.Forex));
        universe.Add(S("XEUR", SymbolGroup.Stocks));

        var hits = SymbolSearch.Search(universe, "EUR");
        Assert.True(hits.Count >= 2);
        Assert.Equal("EURAUD", hits[0].Name);
        Assert.Equal("EURUSD", hits[1].Name);
    }

    [Fact]
    public void Search_MatchesProviderName()
    {
        var hits = SymbolSearch.Search(SampleUniverse(), "USDJPY.M");
        Assert.Single(hits);
        Assert.Equal("USDJPY", hits[0].Name);
    }

    [Fact]
    public void Search_CaseInsensitive()
    {
        var hits = SymbolSearch.Search(SampleUniverse(), "btcusd");
        Assert.Single(hits);
        Assert.Equal("BTCUSD", hits[0].Name);
    }

    [Fact]
    public void Search_Max50()
    {
        var many = Enumerable.Range(0, 80)
            .Select(i => S($"SYM{i:D3}", SymbolGroup.Stocks))
            .ToList();
        var hits = SymbolSearch.Search(many, "SYM");
        Assert.Equal(50, hits.Count);
    }

    [Fact]
    public void Search_EmptyQuery_ReturnsOrderedUpToMax()
    {
        var hits = SymbolSearch.Search(SampleUniverse(), "  ", maxResults: 5);
        Assert.Equal(5, hits.Count);
        Assert.Equal("AAPL", hits[0].Name);
    }

    [Fact]
    public void Favorites_AddRemoveContains_Normalized()
    {
        var fav = new FavoritesList();
        Assert.True(fav.Add("eurusd"));
        Assert.False(fav.Add("EURUSD"));
        Assert.True(fav.Contains("EurUsd"));
        Assert.Equal(new[] { "EURUSD" }, fav.Names);
        Assert.True(fav.Remove("eurusd"));
        Assert.False(fav.Contains("EURUSD"));
        Assert.Empty(fav.Names);
    }

    [Fact]
    public void Workspace_Favorites_RoundTrip()
    {
        var doc = new WorkspaceDocument
        {
            Favorites = { "EURUSD", "BTCUSD" }
        };
        var json = doc.Save();
        var loaded = WorkspaceDocument.Load(json);
        Assert.Equal(new[] { "EURUSD", "BTCUSD" }, loaded.Favorites);

        var json2 = loaded.Save();
        var loaded2 = WorkspaceDocument.Load(json2);
        Assert.Equal(loaded.Favorites, loaded2.Favorites);
    }

    [Fact]
    public void Registry_Search_Integration()
    {
        var reg = new SymbolRegistry();
        foreach (var s in SampleUniverse())
            reg.Register(s);

        var hits = SymbolSearch.Search(reg, "usd");
        Assert.NotEmpty(hits);
        Assert.True(hits.Count <= SymbolSearch.MaxResults);
        Assert.All(hits, s =>
            Assert.True(
                s.Name.Contains("USD", StringComparison.OrdinalIgnoreCase)
                || s.ProviderName.Contains("USD", StringComparison.OrdinalIgnoreCase)));
    }
}
