using MyChart.Core.Contracts.Commands;
using MyChart.Core.Models.Drawing;

namespace MyChart.Core.Commands;

/// <summary>Edit color, width, opacity, style, text, lock, hide (any fields on the replacement object).</summary>
public sealed class EditDrawingCommand : IChartCommand
{
    private readonly DrawingList _list;
    private readonly DrawingObject _before;
    private readonly DrawingObject _after;

    public EditDrawingCommand(DrawingList list, DrawingObject before, DrawingObject after)
    {
        if (before.Id != after.Id) throw new ArgumentException("Edit requires same Id.");
        _list = list;
        _before = before;
        _after = after;
    }

    public string Name => $"EditDrawing:{_before.Id}";

    public void Execute() => _list.Replace(_after.Id, _after);

    public void Undo() => _list.Replace(_before.Id, _before);
}
