using MyChart.Core.Contracts.Commands;
using MyChart.Core.Models.Drawing;

namespace MyChart.Core.Commands;

/// <summary>
/// T3.06: a continuous drag (move or resize) becomes ONE command created on mouse-up.
/// Intermediate Preview() calls do not touch the undo stack.
/// </summary>
public sealed class DrawingDragSession
{
    private readonly DrawingList _list;
    private readonly ICommandBus _bus;
    private readonly DrawingObject _original;
    private DrawingObject _current;
    private int _moveCount;

    public DrawingDragSession(DrawingList list, ICommandBus bus, DrawingObject original)
    {
        _list = list;
        _bus = bus;
        _original = original;
        _current = original;
    }

    public DrawingObject Current => _current;
    public int MoveCount => _moveCount;

    /// <summary>Apply live preview (not a history entry).</summary>
    public void Preview(DrawingObject intermediate)
    {
        if (intermediate.Id != _original.Id)
            throw new ArgumentException("Preview must keep the same Id.");
        _current = intermediate;
        _list.Replace(_original.Id, intermediate);
        _moveCount++;
    }

    /// <summary>Commit as a single MoveDrawingCommand (or no-op if unchanged).</summary>
    public void Commit()
    {
        if (ReferenceEquals(_current, _original) || _current == _original)
        {
            _list.Replace(_original.Id, _original);
            return;
        }

        // Restore original first so Execute applies the final state cleanly
        _list.Replace(_original.Id, _original);
        _bus.Execute(new MoveDrawingCommand(_list, _original, _current));
    }

    /// <summary>Cancel: restore original, no history entry.</summary>
    public void Cancel()
    {
        _list.Replace(_original.Id, _original);
        _current = _original;
    }
}
