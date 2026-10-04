namespace MyChart.Core.Contracts.Services;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
