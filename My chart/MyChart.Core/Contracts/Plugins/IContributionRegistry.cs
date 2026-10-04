using MyChart.Core.Models.Plugins;

namespace MyChart.Core.Contracts.Plugins;

public interface IContributionRegistry
{
    void Add(SlotContribution contribution);
}
