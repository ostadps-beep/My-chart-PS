using MyChart.Core.Models.Drawing;

namespace MyChart.Core.Commands;

/// <summary>
/// Mutable ordered list of drawings (creation order = z-order). Commands replace the list immutably per op.
/// </summary>
public sealed class DrawingList
{
    private readonly List<DrawingObject> _items = new();

    public IReadOnlyList<DrawingObject> Items => _items;

    public void Set(IEnumerable<DrawingObject> items)
    {
        _items.Clear();
        _items.AddRange(items);
    }

    public void Add(DrawingObject obj) => _items.Add(obj);

    public bool RemoveById(string id) => _items.RemoveAll(d => d.Id == id) > 0;

    public void RemoveByIds(IEnumerable<string> ids)
    {
        var set = new HashSet<string>(ids, StringComparer.Ordinal);
        _items.RemoveAll(d => set.Contains(d.Id));
    }

    public DrawingObject? Find(string id) => _items.FirstOrDefault(d => d.Id == id);

    public void Replace(string id, DrawingObject next)
    {
        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i].Id == id)
            {
                _items[i] = next;
                return;
            }
        }
        throw new InvalidOperationException($"Drawing '{id}' not found.");
    }

    public DrawingList Snapshot()
    {
        var copy = new DrawingList();
        copy._items.AddRange(_items);
        return copy;
    }

    public static bool EqualLists(IReadOnlyList<DrawingObject> a, IReadOnlyList<DrawingObject> b)
    {
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
        {
            if (!ReferenceEquals(a[i], b[i]) && a[i] != b[i])
                return false;
        }
        return true;
    }
}
