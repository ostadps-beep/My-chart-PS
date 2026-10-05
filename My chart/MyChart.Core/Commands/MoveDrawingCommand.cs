using MyChart.Core.Contracts.Commands;
using MyChart.Core.Models.Drawing;

namespace MyChart.Core.Commands;

/// <summary>
/// Replaces a drawing with a moved version (new anchors). One command for a whole drag (mouse-up).
/// </summary>
public sealed class MoveDrawingCommand : IChartCommand
{
    private readonly DrawingList _list;
    private readonly DrawingObject _before;
    private readonly DrawingObject _after;

    public MoveDrawingCommand(DrawingList list, DrawingObject before, DrawingObject after)
    {
        if (before.Id != after.Id) throw new ArgumentException("Move requires same Id.");
        _list = list;
        _before = before;
        _after = after;
    }

    public string Name => $"MoveDrawing:{_before.Id}";

    public void Execute() => _list.Replace(_after.Id, _after);

    public void Undo() => _list.Replace(_before.Id, _before);
}
