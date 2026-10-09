namespace MyChart.Core.Analysis.Performance;

/// <summary>
/// T7.03 ObjectPooling — simple pool for reusable list buffers (paths / paint batches).
/// Host maps these to Skia paths; Core stays allocation-light.
/// </summary>
public sealed class PaintPathPool
{
    private readonly Stack<List<float>> _pool = new();
    private readonly int _maxRetained;

    public PaintPathPool(int maxRetained = 32) => _maxRetained = Math.Max(1, maxRetained);

    public int Count => _pool.Count;

    public List<float> Rent()
    {
        if (_pool.Count > 0)
        {
            var list = _pool.Pop();
            list.Clear();
            return list;
        }

        return new List<float>(64);
    }

    public void Return(List<float>? buffer)
    {
        if (buffer is null) return;
        buffer.Clear();
        if (_pool.Count < _maxRetained)
            _pool.Push(buffer);
    }
}
