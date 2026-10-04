namespace MyChart.Core.Contracts.Plugins;

public interface IChartPlugin
{
    string ComponentId { get; }
    void Register(IPluginHost host);
}
