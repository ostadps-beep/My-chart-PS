using MyChart.Core.Models.Market;

namespace MyChart.Core.Candles;

/// <summary>
/// T1.05 TickBuffer — ring buffer per symbol; capacity 100000;
/// overflow drops the OLDEST tick and increments OverflowCount.
/// </summary>
public sealed class TickBuffer
{
    public const int DefaultCapacity = 100_000;

    private readonly Tick[] _ring;
    private int _head; // next write
    private int _tail; // next read
    private int _count;

    public TickBuffer(int capacity = DefaultCapacity)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _ring = new Tick[capacity];
    }

    public int Capacity => _ring.Length;
    public int Count => _count;
    public int OverflowCount { get; private set; }

    public void Enqueue(Tick tick)
    {
        if (_count == _ring.Length)
        {
            // drop oldest
            _tail = (_tail + 1) % _ring.Length;
            _count--;
            OverflowCount++;
        }

        _ring[_head] = tick;
        _head = (_head + 1) % _ring.Length;
        _count++;
    }

    /// <summary>
    /// Drain up to <paramref name="max"/> ticks (T1.05 TickPump: at most 5000 per drain).
    /// </summary>
    public int Drain(Span<Tick> destination)
    {
        int n = Math.Min(destination.Length, _count);
        for (int i = 0; i < n; i++)
        {
            destination[i] = _ring[_tail];
            _tail = (_tail + 1) % _ring.Length;
            _count--;
        }
        return n;
    }

    public int DrainToList(List<Tick> destination, int max = 5000)
    {
        int n = Math.Min(max, _count);
        for (int i = 0; i < n; i++)
        {
            destination.Add(_ring[_tail]);
            _tail = (_tail + 1) % _ring.Length;
            _count--;
        }
        return n;
    }
}
