using MyChart.Core.Models.Market;
using MyChart.Core.Scale;
using MyChart.Data.Providers;
using MyChart.PluginHost.Loading;
using MyChart.UI.Chart;

namespace MyChart.App;

/// <summary>
/// T4.08 CompositionRoot — builds providers and services; no UI logic beyond wiring.
/// </summary>
public static class CompositionRoot
{
    public static ChartComposition CreatePluginHost(
        string? userComponentsRoot = null,
        IEnumerable<IComponentSource>? extraSources = null)
    {
        var composition = new ChartComposition();
        var sources = new List<IComponentSource> { CatalogSource.Empty };
        if (!string.IsNullOrWhiteSpace(userComponentsRoot))
            sources.Add(new UserComponentLoader(userComponentsRoot, Array.Empty<string>()));
        if (extraSources is not null)
            sources.AddRange(extraSources);
        composition.Load(sources.ToArray());
        return composition;
    }

    /// <summary>Load fixture CSV history and push into ChartHost (async-friendly).</summary>
    public static async Task LoadFixturesAsync(ChartHost host, string fixturesDirectory, CancellationToken ct = default)
    {
        host.SetLoading(true);
        var provider = new CsvProvider(fixturesDirectory);
        await provider.ConnectAsync(ct).ConfigureAwait(true);
        var bars = await provider.GetHistoryAsync(
            "EURUSD", Timeframe.M1, from: null, to: null, maxBars: 5000, ct).ConfigureAwait(true);

        if (bars.Count == 0)
        {
            host.SetLoading(false);
            return;
        }

        var opens = bars.Select(b => b.Timestamp).ToList();
        var calendar = new SessionCalendar(ServerTimeRule.Utc, SymbolGroup.Forex);
        var mapper = new TimeIndexMapper(opens, Timeframe.M1, calendar);
        host.SetData(bars, mapper);
    }
}
