namespace MyChart.PluginHost.Loading;

/// <summary>
/// PG1.03 CatalogSource. App/composition root supplies components from generated catalogs.
/// PluginHost references Core only (PLUGIN_DEPENDENCY_DIRECTION).
/// </summary>
public sealed class CatalogSource : IComponentSource
{
    private readonly Func<IEnumerable<ComponentSet>> _provider;

    public CatalogSource(Func<IEnumerable<ComponentSet>> provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    /// <summary>Empty catalog (no first-party components registered yet).</summary>
    public static CatalogSource Empty { get; } = new(() => Array.Empty<ComponentSet>());

    public IEnumerable<ComponentSet> Load(PluginLoadReport report) => _provider();
}
