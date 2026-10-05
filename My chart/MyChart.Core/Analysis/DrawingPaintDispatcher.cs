using MyChart.Core.Commands;
using MyChart.Core.Contracts.Plugins;
using MyChart.Core.Models.Drawing;

namespace MyChart.Core.Analysis;

/// <summary>
/// PG4.01 layer 5 — Paint by TypeId through registered painters (host supplies lookup).
/// Chart code never names a concrete tool.
/// </summary>
public static class DrawingPaintDispatcher
{
    public static void PaintAll(
        DrawingList list,
        DrawContext ctx,
        Func<string, IDrawingObjectPainter?> resolvePainter)
    {
        foreach (var obj in list.Items)
        {
            if (obj.Hidden) continue;
            var painter = resolvePainter(obj.TypeId);
            painter?.Paint(ctx, obj);
        }
    }
}
