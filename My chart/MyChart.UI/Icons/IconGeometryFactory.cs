using System.Windows.Media;
using MyChart.Core.Models.Plugins;
using MyChart.Core.Plugins.Identity;
using MyChart.Core.UI.Icons;

namespace MyChart.UI.Icons;

/// <summary>
/// T7.01 — converts IconDescriptor.PathData to frozen StreamGeometry at load.
/// Does not create MyChart.UI/Icons/IconGeometry.xaml (A3).
/// </summary>
public static class IconGeometryFactory
{
    public static StreamGeometry FromPathData(string pathData, int viewBox = IconSystemModel.IconViewBox)
    {
        if (string.IsNullOrWhiteSpace(pathData))
            throw new ArgumentException("PathData is required.", nameof(pathData));
        if (!GeometryPathGrammar.IsValid(pathData))
            throw new ArgumentException("PathData fails GeometryPathGrammar.", nameof(pathData));
        if (viewBox <= 0)
            throw new ArgumentOutOfRangeException(nameof(viewBox));

        var parsed = Geometry.Parse(pathData.Trim());
        if (parsed is not StreamGeometry stream)
            throw new InvalidOperationException("Icon PathData must parse to StreamGeometry.");

        if (!stream.IsFrozen)
            stream.Freeze();
        return stream;
    }

    public static StreamGeometry FromDescriptor(IconDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return FromPathData(descriptor.PathData, descriptor.ViewBox);
    }

    /// <summary>
    /// Load every descriptor into a key → frozen StreamGeometry map (fail-fast on invalid path).
    /// </summary>
    public static IReadOnlyDictionary<string, StreamGeometry> LoadAll(IEnumerable<IconDescriptor> descriptors)
    {
        ArgumentNullException.ThrowIfNull(descriptors);
        var map = new Dictionary<string, StreamGeometry>(StringComparer.Ordinal);
        foreach (var d in descriptors)
        {
            ArgumentNullException.ThrowIfNull(d);
            map[d.IconKey] = FromDescriptor(d);
        }

        return map;
    }
}
