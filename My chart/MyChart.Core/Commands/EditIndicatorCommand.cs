using MyChart.Core.Contracts.Commands;

namespace MyChart.Core.Commands;

public sealed class EditIndicatorCommand : IChartCommand
{
    private readonly IndicatorList _list;
    private readonly IndicatorInstance _before;
    private readonly IndicatorInstance _after;

    public EditIndicatorCommand(IndicatorList list, IndicatorInstance before, IndicatorInstance after)
    {
        if (before.Id != after.Id) throw new ArgumentException("Edit requires same Id.");
        _list = list;
        _before = before;
        _after = after;
    }

    public string Name => $"EditIndicator:{_before.Id}";

    public void Execute() => _list.Replace(_after.Id, _after);

    public void Undo() => _list.Replace(_before.Id, _before);
}
