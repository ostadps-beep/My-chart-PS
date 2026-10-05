using MyChart.Core.Contracts.Commands;

namespace MyChart.Core.Commands;

public sealed class RemoveIndicatorCommand : IChartCommand
{
    private readonly IndicatorList _list;
    private readonly IndicatorInstance _removed;

    public RemoveIndicatorCommand(IndicatorList list, IndicatorInstance removed)
    {
        _list = list;
        _removed = removed;
    }

    public string Name => $"RemoveIndicator:{_removed.Id}";

    public void Execute() => _list.RemoveById(_removed.Id);

    public void Undo() => _list.Add(_removed);
}
