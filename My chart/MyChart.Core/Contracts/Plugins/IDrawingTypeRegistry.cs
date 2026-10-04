using MyChart.Core.Models.Plugins;

namespace MyChart.Core.Contracts.Plugins;

public interface IDrawingTypeRegistry
{
    void Register(
        DrawingTypeDescriptor descriptor,
        IDrawingObjectPainter painter,
        IDrawingHitTester hitTester,
        IDrawingObjectMigrator? migrator = null);
}
