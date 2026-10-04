namespace MyChart.Core.Contracts.Plugins;

public interface IPluginHost
{
    IToolRegistry Tools { get; }
    IDrawingTypeRegistry DrawingTypes { get; }
    IIconRegistry Icons { get; }
    IContributionRegistry Contributions { get; }
}
