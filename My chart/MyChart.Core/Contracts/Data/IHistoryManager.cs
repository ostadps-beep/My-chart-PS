namespace MyChart.Core.Contracts.Data;

public interface IHistoryManager
{
    Task DownloadAsync(string symbol, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken);
    Task UpdateAsync(string symbol, CancellationToken cancellationToken);
}
