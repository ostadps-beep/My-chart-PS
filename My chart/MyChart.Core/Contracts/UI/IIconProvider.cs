using MyChart.Core.Models.Plugins;

namespace MyChart.Core.Contracts.UI;

/// <summary>
/// T7.01 IconSystem — resolves icon components by key.
/// Geometry conversion (PathData → StreamGeometry) lives in MyChart.UI at load.
/// </summary>
public interface IIconProvider
{
    /// <summary>ViewBox edge length in DIP (CONSTANTS_TABLE IconViewBox).</summary>
    int ViewBox { get; }

    IReadOnlyList<IconDescriptor> All { get; }

    bool TryGet(string iconKey, out IconDescriptor descriptor);

    bool Contains(string iconKey);
}
