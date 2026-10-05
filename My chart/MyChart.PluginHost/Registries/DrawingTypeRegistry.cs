using MyChart.Core.Contracts.Plugins;
using MyChart.Core.Models.Plugins;

namespace MyChart.PluginHost.Registries;

public sealed class DrawingTypeRegistry : IDrawingTypeRegistry
{
    private readonly Dictionary<string, Entry> _byTypeId = new(StringComparer.Ordinal);
    private readonly List<Entry> _order = new();

    private sealed record Entry(
        DrawingTypeDescriptor Descriptor,
        IDrawingObjectPainter Painter,
        IDrawingHitTester HitTester,
        IDrawingObjectMigrator? Migrator);

    public void Register(
        DrawingTypeDescriptor descriptor,
        IDrawingObjectPainter painter,
        IDrawingHitTester hitTester,
        IDrawingObjectMigrator? migrator = null)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        if (_byTypeId.ContainsKey(descriptor.TypeId))
            throw new DuplicateRegistrationException(descriptor.TypeId);
        var e = new Entry(descriptor, painter, hitTester, migrator);
        _byTypeId[descriptor.TypeId] = e;
        _order.Add(e);
    }

    public bool TryGet(string typeId, out DrawingTypeDescriptor descriptor, out IDrawingObjectPainter painter, out IDrawingHitTester hitTester, out IDrawingObjectMigrator? migrator)
    {
        if (_byTypeId.TryGetValue(typeId, out var e))
        {
            descriptor = e.Descriptor;
            painter = e.Painter;
            hitTester = e.HitTester;
            migrator = e.Migrator;
            return true;
        }
        descriptor = null!;
        painter = null!;
        hitTester = null!;
        migrator = null;
        return false;
    }

    public bool IsKnown(string typeId, int typeVersion)
    {
        if (!_byTypeId.TryGetValue(typeId, out var e)) return false;
        return typeVersion <= e.Descriptor.TypeVersion;
    }

    public IReadOnlyList<DrawingTypeDescriptor> InRegistrationOrder()
        => _order.Select(e => e.Descriptor).ToList();
}
