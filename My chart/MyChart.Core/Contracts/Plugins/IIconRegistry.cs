using MyChart.Core.Models.Plugins;

namespace MyChart.Core.Contracts.Plugins;

public interface IIconRegistry
{
    void Register(IconDescriptor descriptor);
}
