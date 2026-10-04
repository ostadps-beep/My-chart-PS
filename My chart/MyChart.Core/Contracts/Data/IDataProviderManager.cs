namespace MyChart.Core.Contracts.Data;

public interface IDataProviderManager
{
    IDataProvider? Active { get; }
    IReadOnlyList<IDataProvider> Providers { get; }
    Task SetActiveAsync(string providerName, CancellationToken cancellationToken);
}
