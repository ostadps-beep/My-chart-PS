using MyChart.Core.Commands;
using MyChart.Core.Contracts.Plugins;
using MyChart.Core.Models.Drawing;
using MyChart.Core.Models.Geometry;

namespace MyChart.Core.Analysis;

/// <summary>
/// PG4.01 hit testing — by TypeId, topmost-first (reverse creation order).
/// </summary>
public static class DrawingHitDispatcher
{
    public static (DrawingObject? Object, HitResult Result) HitTest(
        DrawingList list,
        PointD p,
        IChartMapper map,
        double toleranceDip,
        Func<string, IDrawingHitTester?> resolveHitTester)
    {
        for (int i = list.Items.Count - 1; i >= 0; i--)
        {
            var obj = list.Items[i];
            if (obj.Hidden || obj.Locked) continue;
            var tester = resolveHitTester(obj.TypeId);
            if (tester is null) continue;
            var hit = tester.HitTest(obj, p, map, toleranceDip);
            if (hit.Kind != HitKind.None)
                return (obj, hit);
        }

        return (null, new HitResult(HitKind.None));
    }
}
