using MyChart.Core.Contracts.Plugins;
using MyChart.Core.Models.Plugins;

namespace MyChart.PluginHost.Registries;

public sealed class ContributionRegistry : IContributionRegistry
{
    private readonly List<SlotContribution> _items = new();

    public void Add(SlotContribution contribution)
    {
        ArgumentNullException.ThrowIfNull(contribution);
        _items.Add(contribution);
    }

    public IReadOnlyList<SlotContribution> Sorted()
        => _items
            .OrderBy(c => c.Slot)
            .ThenBy(c => c.Order)
            .ThenBy(c => c.ItemId, StringComparer.Ordinal)
            .ToList();

    public IReadOnlyList<SlotContribution> ForSlot(Slot slot)
        => Sorted().Where(c => c.Slot == slot).ToList();
}
