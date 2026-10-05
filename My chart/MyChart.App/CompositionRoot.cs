using MyChart.PluginHost.Loading;

namespace MyChart.App;

/// <summary>
/// PG4.01 App composition — creates PluginHost with CatalogSource and UserFolderSource.
/// </summary>
public static class CompositionRoot
{
    public static ChartComposition CreateChart(
        string? userComponentsRoot = null,
        IEnumerable<IComponentSource>? extraSources = null)
    {
        var composition = new ChartComposition();
        var sources = new List<IComponentSource>
        {
            CatalogSource.Empty
        };

        if (!string.IsNullOrWhiteSpace(userComponentsRoot))
            sources.Add(new UserComponentLoader(userComponentsRoot, Array.Empty<string>()));

        if (extraSources is not null)
            sources.AddRange(extraSources);

        composition.Load(sources.ToArray());
        return composition;
    }
}
