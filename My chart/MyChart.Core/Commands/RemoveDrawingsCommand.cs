using MyChart.Core.Contracts.Commands;
using MyChart.Core.Models.Drawing;

namespace MyChart.Core.Commands;

public sealed class RemoveDrawingsCommand : IChartCommand
{
    private readonly DrawingList _list;
    private readonly IReadOnlyList<DrawingObject> _before;
    private readonly HashSet<string> _ids;

    public RemoveDrawingsCommand(DrawingList list, IEnumerable<string> ids)
    {
        _list = list;
        _ids = new HashSet<string>(ids, StringComparer.Ordinal);
        _before = list.Items.ToList();
    }

    public string Name => $"RemoveDrawings:{_ids.Count}";

    public void Execute() => _list.RemoveByIds(_ids);

    public void Undo() => _list.Set(_before);
}
