using MyChart.Core.Contracts.Commands;
using MyChart.Core.Models.Drawing;

namespace MyChart.Core.Commands;

public sealed class AddDrawingCommand : IChartCommand
{
    private readonly DrawingList _list;
    private readonly DrawingObject _object;

    public AddDrawingCommand(DrawingList list, DrawingObject obj)
    {
        _list = list;
        _object = obj;
    }

    public string Name => $"AddDrawing:{_object.TypeId}";

    public void Execute() => _list.Add(_object);

    public void Undo() => _list.RemoveById(_object.Id);
}
