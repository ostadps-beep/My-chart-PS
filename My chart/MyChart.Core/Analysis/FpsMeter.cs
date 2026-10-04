namespace MyChart.Core.Analysis;

/// <summary>
/// T3.02 FPS = frames per second averaged over 1 s: the number of frames recorded in the last second.
/// A frame exactly one second old is no longer counted. Time is passed in, so the meter is deterministic.
/// </summary>
public sealed class FpsMeter
{
    public static readonly TimeSpan Window = TimeSpan.FromSeconds(1);

    private readonly Queue<DateTimeOffset> _frames = new();

    public void RecordFrame(DateTimeOffset now)
    {
        _frames.Enqueue(now);
        Trim(now);
    }

    public double Fps(DateTimeOffset now)
    {
        Trim(now);
        return _frames.Count;
    }

    private void Trim(DateTimeOffset now)
    {
        while (_frames.Count > 0 && now - _frames.Peek() >= Window)
            _frames.Dequeue();
    }
}
