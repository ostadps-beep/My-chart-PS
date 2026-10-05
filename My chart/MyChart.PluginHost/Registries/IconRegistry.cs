using MyChart.Core.Contracts.Plugins;
using MyChart.Core.Models.Plugins;

namespace MyChart.PluginHost.Registries;

public sealed class IconRegistry : IIconRegistry
{
    private readonly Dictionary<string, IconDescriptor> _byKey = new(StringComparer.Ordinal);
    private readonly List<IconDescriptor> _order = new();

    public void Register(IconDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        if (_byKey.ContainsKey(descriptor.IconKey))
            throw new DuplicateRegistrationException(descriptor.IconKey);
        _byKey[descriptor.IconKey] = descriptor;
        _order.Add(descriptor);
    }

    public bool TryGet(string iconKey, out IconDescriptor descriptor)
        => _byKey.TryGetValue(iconKey, out descriptor!);

    public bool Contains(string iconKey) => _byKey.ContainsKey(iconKey);

    public IReadOnlyList<IconDescriptor> InRegistrationOrder() => _order;
}
