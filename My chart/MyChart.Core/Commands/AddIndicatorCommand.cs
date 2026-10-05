using MyChart.Core.Contracts.Commands;

namespace MyChart.Core.Commands;

public sealed class AddIndicatorCommand : IChartCommand
{
    private readonly IndicatorList _list;
    private readonly IndicatorInstance _instance;

    public AddIndicatorCommand(IndicatorList list, IndicatorInstance instance)
    {
        _list = list;
        _instance = instance;
    }

    public string Name => $"AddIndicator:{_instance.IndicatorName}";

    public void Execute() => _list.Add(_instance);

    public void Undo() => _list.RemoveById(_instance.Id);
}
