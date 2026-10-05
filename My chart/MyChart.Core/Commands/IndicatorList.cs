namespace MyChart.Core.Commands;

public sealed class IndicatorList
{
    private readonly List<IndicatorInstance> _items = new();

    public IReadOnlyList<IndicatorInstance> Items => _items;

    public void Add(IndicatorInstance item) => _items.Add(item);

    public bool RemoveById(string id) => _items.RemoveAll(i => i.Id == id) > 0;

    public IndicatorInstance? Find(string id) => _items.FirstOrDefault(i => i.Id == id);

    public void Replace(string id, IndicatorInstance next)
    {
        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i].Id == id)
            {
                _items[i] = next;
                return;
            }
        }
        throw new InvalidOperationException($"Indicator '{id}' not found.");
    }
}
