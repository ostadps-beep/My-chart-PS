namespace MyChart.Core.Contracts.Commands;

/// <summary>T3.06 CommandBus: Execute, Undo, Redo, History; stack depth 200.</summary>
public interface ICommandBus
{
    void Execute(IChartCommand command);
    bool CanUndo { get; }
    bool CanRedo { get; }
    void Undo();
    void Redo();
    IReadOnlyList<string> History { get; }
    void ClearHistory();
}
