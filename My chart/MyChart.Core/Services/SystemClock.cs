using MyChart.Core.Contracts.Services;

namespace MyChart.Core.Services;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

public sealed class FixedClock : IClock
{
    public FixedClock(DateTimeOffset start) => UtcNow = start;
    public DateTimeOffset UtcNow { get; set; }
}
