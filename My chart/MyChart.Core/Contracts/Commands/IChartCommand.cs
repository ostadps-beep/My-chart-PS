namespace MyChart.Core.Contracts.Commands;

/// <summary>T3.06 — a state-changing user action. Execute applies; Undo reverses.</summary>
public interface IChartCommand
{
    string Name { get; }
    void Execute();
    void Undo();
}
