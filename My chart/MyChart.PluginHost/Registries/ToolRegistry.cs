using MyChart.Core.Contracts.Plugins;
using MyChart.Core.Models.Plugins;

namespace MyChart.PluginHost.Registries;

public sealed class ToolRegistry : IToolRegistry
{
    private readonly Dictionary<string, (ToolDescriptor Descriptor, Func<IToolContext, IDrawingTool> Factory)> _byToolId =
        new(StringComparer.Ordinal);
    private readonly List<(ToolDescriptor Descriptor, Func<IToolContext, IDrawingTool> Factory)> _order = new();

    public void Register(ToolDescriptor descriptor, Func<IToolContext, IDrawingTool> factory)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(factory);
        if (_byToolId.ContainsKey(descriptor.ToolId))
            throw new DuplicateRegistrationException(descriptor.ToolId);
        var entry = (descriptor, factory);
        _byToolId[descriptor.ToolId] = entry;
        _order.Add(entry);
    }

    public bool TryGet(string toolId, out ToolDescriptor descriptor, out Func<IToolContext, IDrawingTool> factory)
    {
        if (_byToolId.TryGetValue(toolId, out var e))
        {
            descriptor = e.Descriptor;
            factory = e.Factory;
            return true;
        }
        descriptor = null!;
        factory = null!;
        return false;
    }

    public IReadOnlyList<ToolDescriptor> InRegistrationOrder()
        => _order.Select(e => e.Descriptor).ToList();
}
