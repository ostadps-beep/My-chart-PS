using System.Windows.Media;
using MyChart.Core.Contracts.UI;
using MyChart.Core.Models.Plugins;
using MyChart.Core.Models.Rendering;
using MyChart.Core.UI.Icons;

namespace MyChart.UI.Icons;

/// <summary>
/// T7.01 WPF icon provider — loads PathData → StreamGeometry once; colours via theme rules.
/// </summary>
public sealed class IconProvider : IIconProvider
{
    private readonly IconSystemModel _model;
    private readonly IReadOnlyDictionary<string, StreamGeometry> _geometries;

    public IconProvider(IEnumerable<IconDescriptor> descriptors)
    {
        _model = new IconSystemModel(descriptors);
        _geometries = IconGeometryFactory.LoadAll(_model.All);
    }

    public int ViewBox => _model.ViewBox;

    public IReadOnlyList<IconDescriptor> All => _model.All;

    public IconSystemModel Model => _model;

    public bool TryGet(string iconKey, out IconDescriptor descriptor)
        => _model.TryGet(iconKey, out descriptor);

    public bool Contains(string iconKey) => _model.Contains(iconKey);

    public bool TryGetGeometry(string iconKey, out StreamGeometry geometry)
        => _geometries.TryGetValue(iconKey, out geometry!);

    public SolidColorBrush BrushFor(ThemeTokens theme, IconVisualState state)
        => IconBrushFactory.Create(theme, state);
}
