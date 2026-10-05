using MyChart.Core.Contracts.Commands;
using MyChart.Core.Models.Drawing;

namespace MyChart.Core.Commands;

public sealed class CloneDrawingCommand : IChartCommand
{
    private readonly DrawingList _list;
    private readonly DrawingObject _clone;

    public CloneDrawingCommand(DrawingList list, DrawingObject clone)
    {
        _list = list;
        _clone = clone;
    }

    public string Name => $"CloneDrawing:{_clone.Id}";

    public void Execute() => _list.Add(_clone);

    public void Undo() => _list.RemoveById(_clone.Id);
}
