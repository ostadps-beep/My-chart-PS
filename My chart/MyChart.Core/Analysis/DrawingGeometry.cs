using System.Text.Json.Nodes;
using MyChart.Core.Contracts.Plugins;
using MyChart.Core.Models.Drawing;
using MyChart.Core.Models.Geometry;

namespace MyChart.Core.Analysis;

/// <summary>T3.04 result of a drawing hit test: the topmost object that was hit and how (Body or Handle(i)).</summary>
public readonly record struct DrawingHit(DrawingObject Object, HitResult Result);

/// <summary>
/// T3.04 DrawingGeometryAndHitTest — the GENERIC rules only (revision 1.3 A4): screen position, handles, topmost wins,
/// MOVE, RESIZE, CLONE, LOCK, HIDE. Shape-specific hit rules and geometry live in the DataTool engine (PG2).
/// Pure functions of (object, mapper): no UI state, no IO. All screen values are DIP.
/// </summary>
public static class DrawingGeometry
{
    // ---------- screen position ----------

    /// <summary>SCREEN POSITION = (X(IndexOfTime(t)), Y(price)).</summary>
    public static PointD ScreenPosition(DrawingAnchor anchor, IChartMapper map)
        => new(map.X(map.IndexOfTime(anchor.TimeUtc)), map.Y(anchor.Price));

    /// <summary>
    /// Inverse of ScreenPosition for a free (unsnapped) point: (TimeAtIndex(U(x)), Price(y)).
    /// Snapping (IToolContext.Snap) is applied by the caller before or after.
    /// </summary>
    public static DrawingAnchor AnchorAt(PointD point, IChartMapper map)
        => new(map.TimeAtIndex(map.U(point.X)), map.Price(point.Y));

    // ---------- handles ----------

    /// <summary>Handles are 8x8 DIP squares at the anchors.</summary>
    public static RectD HandleRect(PointD anchorScreen)
        => new(
            anchorScreen.X - DrawingConstants.HandleSizeDip / 2,
            anchorScreen.Y - DrawingConstants.HandleSizeDip / 2,
            DrawingConstants.HandleSizeDip,
            DrawingConstants.HandleSizeDip);

    /// <summary>
    /// Index of the handle hit by the point: the nearest anchor within the handle hit radius (6 DIP, inclusive),
    /// the lowest index on a tie; -1 when no handle is hit.
    /// </summary>
    public static int HitHandle(DrawingObject obj, PointD point, IChartMapper map)
    {
        int best = -1;
        double bestDistance = double.MaxValue;

        for (int i = 0; i < obj.Anchors.Count; i++)
        {
            var s = ScreenPosition(obj.Anchors[i], map);
            double dx = point.X - s.X;
            double dy = point.Y - s.Y;
            double d = Math.Sqrt(dx * dx + dy * dy);

            if (d <= DrawingConstants.HandleHitRadiusDip && d < bestDistance)
            {
                best = i;
                bestDistance = d;
            }
        }

        return best;
    }

    // ---------- hit test ----------

    /// <summary>
    /// Hit test over a drawing list in creation order. The topmost object wins (last created on top).
    /// Hidden objects are never hit; locked objects are (they stay selectable). An object whose TypeId has no
    /// hit tester (unknown or disabled component) is skipped. Per object the handles win over the body:
    /// first the anchors (HitHandle), then the shape's own tester. Tolerance defaults to 6 DIP.
    /// </summary>
    public static DrawingHit? HitTest(
        IReadOnlyList<DrawingObject> objects,
        PointD point,
        IChartMapper map,
        Func<string, IDrawingHitTester?> testerFor,
        double toleranceDip = DrawingConstants.HitToleranceDip)
    {
        for (int i = objects.Count - 1; i >= 0; i--)
        {
            var obj = objects[i];
            if (!IsHittable(obj)) continue;

            var tester = testerFor(obj.TypeId);
            if (tester is null) continue;

            int handle = HitHandle(obj, point, map);
            if (handle >= 0)
                return new DrawingHit(obj, new HitResult(HitKind.Handle, handle));

            var result = tester.HitTest(obj, point, map, toleranceDip);
            if (result.Kind != HitKind.None)
                return new DrawingHit(obj, result);
        }

        return null;
    }

    // ---------- MOVE / RESIZE / CLONE ----------

    /// <summary>
    /// Pointer movement in screen space -> (delta bars, delta price) = (U(to.X) - U(from.X), Price(to.Y) - Price(from.Y)).
    /// </summary>
    public static (double DeltaU, double DeltaPrice) DeltaFromScreen(PointD from, PointD to, IChartMapper map)
        => (map.U(to.X) - map.U(from.X), map.Price(to.Y) - map.Price(from.Y));

    /// <summary>
    /// MOVE: every anchor shifts by (delta bars, delta price):
    /// newTime = TimeAtIndex(IndexOfTime(t) + deltaU) ; newPrice = price + deltaPrice.
    /// A locked object is returned unchanged (same instance).
    /// </summary>
    public static DrawingObject Move(DrawingObject obj, double deltaU, double deltaPrice, IChartMapper map)
    {
        if (!CanMove(obj)) return obj;

        var moved = new DrawingAnchor[obj.Anchors.Count];
        for (int i = 0; i < moved.Length; i++)
        {
            var a = obj.Anchors[i];
            moved[i] = new DrawingAnchor(
                map.TimeAtIndex(map.IndexOfTime(a.TimeUtc) + deltaU),
                a.Price + deltaPrice);
        }

        return obj with { Anchors = moved };
    }

    /// <summary>
    /// RESIZE: only the dragged anchor changes. A locked object, or a handle index outside the anchors,
    /// returns the object unchanged (same instance).
    /// </summary>
    public static DrawingObject Resize(DrawingObject obj, int handleIndex, DrawingAnchor newAnchor)
    {
        if (!CanResize(obj) || handleIndex < 0 || handleIndex >= obj.Anchors.Count)
            return obj;

        var anchors = obj.Anchors.ToArray();
        anchors[handleIndex] = newAnchor;
        return obj with { Anchors = anchors };
    }

    /// <summary>
    /// CLONE: every anchor moves +12 DIP right and +12 DIP down in screen space and is converted back to
    /// (time, price). The clone gets the given id; style, flags and TypeId are kept; Extra is deep-copied.
    /// Locking blocks move, resize and delete only, so a locked drawing can be cloned (the copy stays locked).
    /// </summary>
    public static DrawingObject Clone(DrawingObject obj, string newId, IChartMapper map)
    {
        var anchors = new DrawingAnchor[obj.Anchors.Count];
        for (int i = 0; i < anchors.Length; i++)
        {
            var s = ScreenPosition(obj.Anchors[i], map);
            anchors[i] = AnchorAt(
                new PointD(s.X + DrawingConstants.CloneOffsetDip, s.Y + DrawingConstants.CloneOffsetDip),
                map);
        }

        return obj with
        {
            Id = newId,
            Anchors = anchors,
            Extra = (JsonObject?)obj.Extra?.DeepClone()
        };
    }

    // ---------- LOCK and HIDE ----------

    /// <summary>LOCK: move is blocked.</summary>
    public static bool CanMove(DrawingObject obj) => !obj.Locked;

    /// <summary>LOCK: resize is blocked.</summary>
    public static bool CanResize(DrawingObject obj) => !obj.Locked;

    /// <summary>LOCK: delete is blocked.</summary>
    public static bool CanDelete(DrawingObject obj) => !obj.Locked;

    /// <summary>LOCK: the object stays selectable and shows its properties; HIDE: a hidden object is not selectable.</summary>
    public static bool IsSelectable(DrawingObject obj) => !obj.Hidden;

    /// <summary>HIDE: a hidden object is never hit (it is still serialized).</summary>
    public static bool IsHittable(DrawingObject obj) => !obj.Hidden;

    /// <summary>HIDE: a hidden object is not rendered (it is still serialized).</summary>
    public static bool IsRendered(DrawingObject obj) => !obj.Hidden;
}
