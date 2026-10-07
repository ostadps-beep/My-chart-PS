using MyChart.Core.Models.Indicators;
using MyChart.Core.Models.Rendering;

namespace MyChart.Core.UI.Indicators;

/// <summary>One continuous polyline segment (NaN breaks produce separate segments).</summary>
public sealed record PolylineSegment(IReadOnlyList<(int Index, double Value)> Points);

/// <summary>Layer-4 series ready for the renderer: color from IndicatorPalette in order.</summary>
public sealed record IndicatorSeriesDraw(
    string SeriesName,
    RgbaColor Color,
    int PaletteIndex,
    IReadOnlyList<PolylineSegment> Segments);

/// <summary>
/// T6.06 IndicatorLayer — layer 4: polyline through visible outputs;
/// NaN breaks the line; default colors from IndicatorPalette in order.
/// Pure model; no Skia/WPF.
/// </summary>
public static class IndicatorLayerModel
{
    public const int LayerIndex = 4;

    /// <summary>
    /// Build draw descriptors for all outputs across instances.
    /// Palette index increments per output series in encounter order.
    /// </summary>
    public static IReadOnlyList<IndicatorSeriesDraw> Build(
        IReadOnlyList<(string InstanceId, IndicatorOutput Output)> series,
        ThemeTokens theme,
        int visibleFromIndex,
        int visibleToIndexExclusive)
    {
        ArgumentNullException.ThrowIfNull(series);
        ArgumentNullException.ThrowIfNull(theme);

        var result = new List<IndicatorSeriesDraw>();
        var paletteIndex = 0;

        foreach (var (instanceId, output) in series)
        {
            var color = theme.IndicatorColor(paletteIndex);
            var segments = BuildSegments(output, visibleFromIndex, visibleToIndexExclusive);
            result.Add(new IndicatorSeriesDraw(
                $"{instanceId}:{output.Name}",
                color,
                paletteIndex,
                segments));
            paletteIndex++;
        }

        return result;
    }

    /// <summary>Split values into polyline segments; NaN or out-of-range breaks the line.</summary>
    public static IReadOnlyList<PolylineSegment> BuildSegments(
        IndicatorOutput output,
        int visibleFromIndex,
        int visibleToIndexExclusive)
    {
        ArgumentNullException.ThrowIfNull(output);
        if (visibleToIndexExclusive < visibleFromIndex)
            return Array.Empty<PolylineSegment>();

        var segments = new List<PolylineSegment>();
        List<(int Index, double Value)>? current = null;

        var start = Math.Max(0, visibleFromIndex);
        var end = Math.Min(output.Length, visibleToIndexExclusive);

        for (var i = start; i < end; i++)
        {
            var v = output[i];
            if (double.IsNaN(v) || double.IsInfinity(v))
            {
                if (current is { Count: > 0 })
                {
                    segments.Add(new PolylineSegment(current));
                    current = null;
                }
                continue;
            }

            current ??= new List<(int, double)>();
            current.Add((i, v));
        }

        if (current is { Count: > 0 })
            segments.Add(new PolylineSegment(current));

        return segments;
    }
}
