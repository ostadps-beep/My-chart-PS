using MyChart.Core.Contracts.Rendering;
using MyChart.Core.Models.Drawing;
using MyChart.Core.Models.Geometry;
using MyChart.Core.Models.Rendering;
using MyChart.Core.Plugins.Vocabulary;
using MyChart.Core.Services;
using MyChart.PluginHost.DataTools;
using MyChart.Tests.Support;

namespace MyChart.Tests.Support;

/// <summary>PG5.01 ComponentScenes — standard vector view for first-party tools.</summary>
public static class ComponentScenes
{
    public const int Width = 800;
    public const int Height = 500;

    /// <summary>Canonical anchors: N points on rising diagonal index 20..40, price 1.1000..1.1050.</summary>
    public static IReadOnlyList<DrawingAnchor> CanonicalAnchors(int n)
    {
        if (n <= 0) return Array.Empty<DrawingAnchor>();
        var list = new List<DrawingAnchor>(n);
        var t0 = ViewChartMapper.FirstOpen.AddMinutes(15 * 20);
        for (int i = 0; i < n; i++)
        {
            double t = n == 1 ? 0.5 : (double)i / (n - 1);
            int idx = 20 + (int)Math.Round(t * 20);
            double price = 1.1000 + t * 0.0050;
            list.Add(new DrawingAnchor(ViewChartMapper.FirstOpen.AddMinutes(15 * idx), price));
        }
        return list;
    }

    public static byte[] RenderTrendLine(double dpi = 1.0, bool write = false)
    {
        var theme = new ThemeService();
        var painter = new DataToolPainter("Line");
        var anchors = CanonicalAnchors(2);
        var obj = new DrawingObject(
            "tl", "drawing.trendline", 1, anchors.ToList(),
            DrawingStyleRules.Default(theme.Current.AccentColor), false, false, null);

        return VisualProof.Render("T4.09", "TrendLine", Width, Height, ctx =>
        {
            ctx.Clear(theme.Current.BackgroundColor);
            var map = ViewChartMapper.Standard();
            painter.Paint(new MyChart.Core.Contracts.Plugins.DrawContext
            {
                Render = ctx,
                Map = map,
                Theme = theme.Current,
                Symbol = new MyChart.Core.Models.Market.SymbolInfo("EURUSD", "EURUSD", MyChart.Core.Models.Market.SymbolGroup.Forex, 5),
                DpiScale = dpi,
                State = MyChart.Core.Contracts.Plugins.DrawState.Normal
            }, obj);
        }, dpiScale: dpi, writeToDisk: write);
    }
}
