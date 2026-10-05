using MyChart.Core.Contracts.Commands;

namespace MyChart.Core.Commands;

/// <summary>
/// T3.06 CommandBus. Undo stack depth 200. ClearHistory on workspace load.
/// Ctrl+Z = Undo; Ctrl+Y / Ctrl+Shift+Z = Redo (binding is UI later).
/// </summary>
public sealed class CommandBus : ICommandBus
{
    private readonly List<IChartCommand> _undo = new();
    private readonly List<IChartCommand> _redo = new();
    private readonly List<string> _history = new();

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;
    public IReadOnlyList<string> History => _history;

    public void Execute(IChartCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        command.Execute();
        _undo.Add(command);
        if (_undo.Count > CommandConstants.HistoryDepth)
            _undo.RemoveAt(0);
        _redo.Clear();
        _history.Add(command.Name);
        if (_history.Count > CommandConstants.HistoryDepth)
            _history.RemoveAt(0);
    }

    public void Undo()
    {
        if (_undo.Count == 0) return;
        var cmd = _undo[^1];
        _undo.RemoveAt(_undo.Count - 1);
        cmd.Undo();
        _redo.Add(cmd);
    }

    public void Redo()
    {
        if (_redo.Count == 0) return;
        var cmd = _redo[^1];
        _redo.RemoveAt(_redo.Count - 1);
        cmd.Execute();
        _undo.Add(cmd);
        if (_undo.Count > CommandConstants.HistoryDepth)
            _undo.RemoveAt(0);
    }

    public void ClearHistory()
    {
        _undo.Clear();
        _redo.Clear();
        _history.Clear();
    }
}
