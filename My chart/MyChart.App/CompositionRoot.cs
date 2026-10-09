using MyChart.App.Integration;
using MyChart.Core.Models.Market;
using MyChart.Core.Models.Plugins;
using MyChart.Core.Models.Settings;
using MyChart.Core.Scale;
using MyChart.Core.UI.Toolbar;
using MyChart.Data.Providers;
using MyChart.PluginHost.Loading;
using MyChart.Plugins;
using MyChart.UI.Chart;
using MyChart.UI.Icons;

namespace MyChart.App;

/// <summary>
/// T4.08 / T6.08 / T7.01 CompositionRoot — builds providers and services; no UI logic beyond wiring.
/// </summary>
public static class CompositionRoot
{
    public static SettingsBridgeHost? SettingsHost { get; private set; }

    /// <summary>T7.01 — PathData loaded to StreamGeometry once per session.</summary>
    public static IconProvider? Icons { get; private set; }

    public static ChartComposition CreatePluginHost(
        string? userComponentsRoot = null,
        IEnumerable<IComponentSource>? extraSources = null)
    {
        // T6.08 — Settings bridge is available for the whole session.
        var bridge = new SettingsBridge();
        SettingsHost = new SettingsBridgeHost(bridge);
        bridge.Seed();

        // T7.01 — first-party icon catalog + optional user icons from host after load.
        Icons = CreateIconProvider();

        var composition = new ChartComposition();
        var sources = new List<IComponentSource> { CatalogSource.Empty };
        if (!string.IsNullOrWhiteSpace(userComponentsRoot))
            sources.Add(new UserComponentLoader(userComponentsRoot, Array.Empty<string>()));
        if (extraSources is not null)
            sources.AddRange(extraSources);
        composition.Load(sources.ToArray());
        return composition;
    }

    /// <summary>T7.01 — builds IconProvider from IconCatalog (and optional extra descriptors).</summary>
    public static IconProvider CreateIconProvider(IEnumerable<IconDescriptor>? extra = null)
    {
        var list = new List<IconDescriptor>(IconCatalog.All);
        if (extra is not null)
            list.AddRange(extra);
        var provider = new IconProvider(list);
        Icons = provider;
        return provider;
    }

    /// <summary>T6.08 — TopToolbar with Settings enabled after panel is linked.</summary>
    public static TopToolbarModel CreateTopToolbar(
        string? symbol = null,
        Timeframe timeframe = Timeframe.M15,
        string chartType = "Candles",
        int layoutPanels = 1)
        => TopToolbarModel.CreateDefault(
            selectedSymbol: symbol,
            selectedTf: timeframe,
            chartType: chartType,
            layoutPanels: layoutPanels,
            settingsAvailable: true);

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
