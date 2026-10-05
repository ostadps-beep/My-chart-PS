using System.Text.Json.Nodes;
using MyChart.Core.Analysis;
using MyChart.Core.Contracts.Plugins;
using MyChart.Core.Models.Drawing;
using MyChart.Core.Models.Geometry;
using MyChart.Core.Models.Rendering;
using MyChart.Tests.Support;
using Xunit;

namespace MyChart.Tests.Analysis;

/// <summary>
/// T3.04 DrawingGeometryAndHitTest (generic rules). View = ViewChartMapper.Standard(): X(i) = 8 * i - 36,
/// Y(price) = (1.1060 - price) / 0.012 * 500, bar i opens 12:00Z + 15 min * i.
/// </summary>
public class DrawingGeometryTests
{
    private const string SegmentType = "test.segment";
    private static readonly DateTimeOffset First = ViewChartMapper.FirstOpen;
    private static readonly DrawingStyle Style = DrawingStyleRules.Default(RgbaColor.FromRgb(10, 20, 30));

    private static DateTimeOffset Bar(int i) => First.AddMinutes(15 * i);

    private static DrawingObject Segment(
        string id,
        int bar0,
        int bar1,
        double price = 1.1000,
        bool locked = false,
        bool hidden = false,
        string typeId = SegmentType,
        JsonObject? extra = null)
        => new(
            id,
            typeId,
            1,
            new[] { new DrawingAnchor(Bar(bar0), price), new DrawingAnchor(Bar(bar1), price) },
            Style,
            locked,
            hidden,
            extra);

    private sealed class SegmentTester : IDrawingHitTester
    {
        public HitResult HitTest(DrawingObject obj, PointD p, IChartMapper map, double toleranceDip)
        {
            var a = DrawingGeometry.ScreenPosition(obj.Anchors[0], map);
            var b = DrawingGeometry.ScreenPosition(obj.Anchors[1], map);
            return DrawingMath.DistancePointToSegment(p, a, b) <= toleranceDip
                ? new HitResult(HitKind.Body)
                : new HitResult(HitKind.None);
        }
    }

    private static IDrawingHitTester? Resolve(string typeId)
        => typeId == SegmentType ? new SegmentTester() : null;

    private static void AssertTimeNear(DateTimeOffset expected, DateTimeOffset actual)
        => Assert.True(
            Math.Abs((expected - actual).TotalMilliseconds) < 1.0,
            $"expected {expected:O} but was {actual:O}");

    // ---------- SCREEN POSITION ----------

    [Fact]
    public void ScreenPosition_IsXOfIndexOfTime_And_YOfPrice()
    {
        var map = ViewChartMapper.Standard();

        var last = DrawingGeometry.ScreenPosition(new DrawingAnchor(Bar(99), 1.1000), map);
        Assert.Equal(756.0, last.X, 9);
        Assert.Equal(250.0, last.Y, 9);

        var top = DrawingGeometry.ScreenPosition(new DrawingAnchor(Bar(10), 1.1060), map);
        Assert.Equal(44.0, top.X, 9);
        Assert.Equal(0.0, top.Y, 9);

        // half way through bar 10 -> half a bar to the right of its centre column
        var inside = DrawingGeometry.ScreenPosition(new DrawingAnchor(Bar(10).AddMinutes(7.5), 1.0940), map);
        Assert.Equal(48.0, inside.X, 9);
        Assert.Equal(500.0, inside.Y, 9);
    }

    [Fact]
    public void AnchorAt_IsTheInverseOfScreenPosition()
    {
        var map = ViewChartMapper.Standard();
        var original = new DrawingAnchor(Bar(37), 1.0987);

        var back = DrawingGeometry.AnchorAt(DrawingGeometry.ScreenPosition(original, map), map);

        AssertTimeNear(original.TimeUtc, back.TimeUtc);
        Assert.Equal(original.Price, back.Price, 9);
    }

    [Fact]
    public void Anchors_AreTimeBased_NotIndexBased()
    {
        // the same anchor keeps its time when the view scrolls: only the screen position changes
        var map = ViewChartMapper.Standard();
        var anchor = new DrawingAnchor(Bar(10), 1.1000);

        double before = DrawingGeometry.ScreenPosition(anchor, map).X;

        // RightOffset 5 -> 0 moves every bar 5 * BarSpacing = 40 DIP to the right
        map.ViewState.RightOffset -= 5;
        double after = DrawingGeometry.ScreenPosition(anchor, map).X;

        Assert.Equal(44.0, before, 9);
        Assert.Equal(84.0, after, 9);
        Assert.Equal(Bar(10), anchor.TimeUtc);
    }

    // ---------- HANDLES ----------

    [Fact]
    public void HandleRect_Is8x8CentredOnTheAnchor()
    {
        var r = DrawingGeometry.HandleRect(new PointD(44, 250));

        Assert.Equal(40.0, r.X, 9);
        Assert.Equal(246.0, r.Y, 9);
        Assert.Equal(8.0, r.Width, 9);
        Assert.Equal(8.0, r.Height, 9);
    }

    [Fact]
    public void HitHandle_WithinSixDip_IsInclusive_AndPicksTheNearest()
    {
        var map = ViewChartMapper.Standard();
        var seg = Segment("a", 10, 20);   // anchors at (44, 250) and (124, 250)

        Assert.Equal(0, DrawingGeometry.HitHandle(seg, new PointD(44, 250), map));
        Assert.Equal(1, DrawingGeometry.HitHandle(seg, new PointD(124, 250), map));
        Assert.Equal(0, DrawingGeometry.HitHandle(seg, new PointD(50, 250), map));   // exactly 6 DIP
        Assert.Equal(-1, DrawingGeometry.HitHandle(seg, new PointD(50.1, 250), map)); // just outside

        // two anchors 4 DIP apart: the nearer one wins, the lower index wins a tie
        var close = new DrawingObject(
            "c", SegmentType, 1,
            new[] { new DrawingAnchor(Bar(10), 1.1000), new DrawingAnchor(Bar(10).AddMinutes(7.5), 1.1000) },
            Style, false, false, null);
        Assert.Equal(1, DrawingGeometry.HitHandle(close, new PointD(47, 250), map));
        Assert.Equal(0, DrawingGeometry.HitHandle(close, new PointD(46, 250), map));
    }

    // ---------- HIT TEST ----------

    [Fact]
    public void HitTest_SegmentVectors_ScaledToTheView()
    {
        var map = ViewChartMapper.Standard();
        var list = new[] { Segment("seg", 10, 20) };   // segment (44,250)-(124,250)

        // golden DrawingHit vectors: (50,5) hit ; (50,7) miss ; (103,4) hit ; (110,0) miss, shifted by the segment origin
        var mid = DrawingGeometry.HitTest(list, new PointD(84, 255), map, Resolve);
        Assert.True(mid.HasValue);
        Assert.Equal(new HitResult(HitKind.Body), mid.GetValueOrDefault().Result);

        Assert.False(DrawingGeometry.HitTest(list, new PointD(84, 257), map, Resolve).HasValue);

        // 3 DIP past the end and 4 DIP down: distance 5 -> a hit (the end handle wins over the body)
        var nearEnd = DrawingGeometry.HitTest(list, new PointD(127, 254), map, Resolve);
        Assert.True(nearEnd.HasValue);
        Assert.Equal(new HitResult(HitKind.Handle, 1), nearEnd.GetValueOrDefault().Result);

        Assert.False(DrawingGeometry.HitTest(list, new PointD(134, 250), map, Resolve).HasValue);
    }

    [Fact]
    public void HitTest_HandlesWinOverTheBody()
    {
        var map = ViewChartMapper.Standard();
        var list = new[] { Segment("seg", 10, 20) };

        // (44, 250) lies on the segment AND on handle 0: the handle wins
        var hit = DrawingGeometry.HitTest(list, new PointD(44, 250), map, Resolve);

        Assert.True(hit.HasValue);
        Assert.Equal(new HitResult(HitKind.Handle, 0), hit.GetValueOrDefault().Result);
    }

    [Fact]
    public void HitTest_TopmostObjectWins_LastCreatedOnTop()
    {
        var map = ViewChartMapper.Standard();
        var list = new[] { Segment("first", 10, 20), Segment("second", 10, 20) };

        var hit = DrawingGeometry.HitTest(list, new PointD(84, 250), map, Resolve);

        Assert.True(hit.HasValue);
        Assert.Equal("second", hit.GetValueOrDefault().Object.Id);
    }

    [Fact]
    public void HitTest_HiddenObjectsAreNeverHit()
    {
        var map = ViewChartMapper.Standard();
        var list = new[] { Segment("first", 10, 20), Segment("second", 10, 20, hidden: true) };

        var hit = DrawingGeometry.HitTest(list, new PointD(84, 250), map, Resolve);
        Assert.Equal("first", hit.GetValueOrDefault().Object.Id);

        // a hidden object is not hit through its handles either
        var onlyHidden = new[] { Segment("h", 10, 20, hidden: true) };
        Assert.False(DrawingGeometry.HitTest(onlyHidden, new PointD(44, 250), map, Resolve).HasValue);
    }

    [Fact]
    public void HitTest_LockedObjectsStaySelectable()
    {
        var map = ViewChartMapper.Standard();
        var list = new[] { Segment("locked", 10, 20, locked: true) };

        var hit = DrawingGeometry.HitTest(list, new PointD(84, 250), map, Resolve);

        Assert.True(hit.HasValue);
        Assert.Equal("locked", hit.GetValueOrDefault().Object.Id);
        Assert.True(DrawingGeometry.IsSelectable(list[0]));
    }

    [Fact]
    public void HitTest_ObjectWithoutATester_IsSkipped()
    {
        var map = ViewChartMapper.Standard();

        // top object belongs to an unknown or disabled component: not hit, not even by its handles
        var list = new[]
        {
            Segment("known", 10, 20),
            Segment("ghost", 10, 20, typeId: "other.removed")
        };

        var body = DrawingGeometry.HitTest(list, new PointD(84, 250), map, Resolve);
        Assert.Equal("known", body.GetValueOrDefault().Object.Id);

        var onlyGhost = new[] { list[1] };
        Assert.False(DrawingGeometry.HitTest(onlyGhost, new PointD(44, 250), map, Resolve).HasValue);
    }

    [Fact]
    public void HitTest_EmptyList_IsNull()
    {
        var map = ViewChartMapper.Standard();
        Assert.False(DrawingGeometry.HitTest(Array.Empty<DrawingObject>(), new PointD(84, 250), map, Resolve).HasValue);
    }

    [Fact]
    public void HitTest_ToleranceIsPassedToTheShapeTester()
    {
        var map = ViewChartMapper.Standard();
        var list = new[] { Segment("seg", 10, 20) };

        // 10 DIP below the segment: miss at the default 6 DIP, hit with a 12 DIP tolerance
        Assert.False(DrawingGeometry.HitTest(list, new PointD(84, 260), map, Resolve).HasValue);
        Assert.True(DrawingGeometry.HitTest(list, new PointD(84, 260), map, Resolve, 12).HasValue);
        Assert.Equal(6.0, DrawingConstants.HitToleranceDip, 9);
    }

    // ---------- MOVE ----------

    [Fact]
    public void DeltaFromScreen_IsBarsAndPrice()
    {
        var map = ViewChartMapper.Standard();

        var (du, dp) = DrawingGeometry.DeltaFromScreen(new PointD(400, 250), new PointD(440, 225), map);

        Assert.Equal(5.0, du, 9);          // 40 DIP / BarSpacing 8
        Assert.Equal(0.0006, dp, 9);       // 25 DIP up = 25 / 500 * 0.012
    }

    [Fact]
    public void Move_ShiftsEveryAnchorByBarsAndPrice()
    {
        var map = ViewChartMapper.Standard();
        var seg = Segment("a", 10, 20);

        var moved = DrawingGeometry.Move(seg, 3, 0.0010, map);

        Assert.Equal(2, moved.Anchors.Count);
        AssertTimeNear(Bar(13), moved.Anchors[0].TimeUtc);
        AssertTimeNear(Bar(23), moved.Anchors[1].TimeUtc);
        Assert.Equal(1.1010, moved.Anchors[0].Price, 9);
        Assert.Equal(1.1010, moved.Anchors[1].Price, 9);

        // the original is untouched and everything else is kept
        Assert.Equal(Bar(10), seg.Anchors[0].TimeUtc);
        Assert.Equal(seg.Id, moved.Id);
        Assert.Equal(seg.TypeId, moved.TypeId);
        Assert.Equal(seg.Style, moved.Style);
    }

    [Fact]
    public void Move_BackwardsAndByAFractionOfABar()
    {
        var map = ViewChartMapper.Standard();
        var seg = Segment("a", 10, 20);

        var moved = DrawingGeometry.Move(seg, -2.5, -0.0005, map);

        AssertTimeNear(Bar(10).AddMinutes(-37.5), moved.Anchors[0].TimeUtc);
        Assert.Equal(1.0995, moved.Anchors[0].Price, 9);
    }

    [Fact]
    public void Move_LockedObject_IsReturnedUnchanged()
    {
        var map = ViewChartMapper.Standard();
        var seg = Segment("a", 10, 20, locked: true);

        Assert.Same(seg, DrawingGeometry.Move(seg, 3, 0.001, map));
    }

    // ---------- RESIZE ----------

    [Fact]
    public void Resize_ChangesOnlyTheDraggedAnchor()
    {
        var seg = Segment("a", 10, 20);
        var target = new DrawingAnchor(Bar(40), 1.1020);

        var resized = DrawingGeometry.Resize(seg, 1, target);

        Assert.Equal(seg.Anchors[0], resized.Anchors[0]);
        Assert.Equal(target, resized.Anchors[1]);
        Assert.Equal(Bar(20), seg.Anchors[1].TimeUtc);   // original untouched
    }

    [Fact]
    public void Resize_FirstAnchor_LeavesTheSecondAlone()
    {
        var seg = Segment("a", 10, 20);
        var target = new DrawingAnchor(Bar(5), 1.0900);

        var resized = DrawingGeometry.Resize(seg, 0, target);

        Assert.Equal(target, resized.Anchors[0]);
        Assert.Equal(seg.Anchors[1], resized.Anchors[1]);
    }

    [Fact]
    public void Resize_LockedOrBadHandle_IsReturnedUnchanged()
    {
        var target = new DrawingAnchor(Bar(40), 1.1020);

        var locked = Segment("a", 10, 20, locked: true);
        Assert.Same(locked, DrawingGeometry.Resize(locked, 1, target));

        var open = Segment("b", 10, 20);
        Assert.Same(open, DrawingGeometry.Resize(open, 2, target));
        Assert.Same(open, DrawingGeometry.Resize(open, -1, target));
    }

    // ---------- CLONE ----------

    [Fact]
    public void Clone_OffsetsTwelveDipRightAndDown()
    {
        var map = ViewChartMapper.Standard();
        var seg = Segment("a", 10, 20);

        var copy = DrawingGeometry.Clone(seg, "b", map);

        // screen (44,250) -> (56,262): u = 11.5 and price = 1.1060 - 262 / 500 * 0.012 = 1.099712
        AssertTimeNear(Bar(11).AddMinutes(7.5), copy.Anchors[0].TimeUtc);
        Assert.Equal(1.099712, copy.Anchors[0].Price, 9);

        // second anchor (124,250) -> (136,262): u = 21.5
        AssertTimeNear(Bar(21).AddMinutes(7.5), copy.Anchors[1].TimeUtc);

        var s = DrawingGeometry.ScreenPosition(copy.Anchors[0], map);
        Assert.Equal(56.0, s.X, 6);
        Assert.Equal(262.0, s.Y, 6);
        Assert.Equal(12.0, DrawingConstants.CloneOffsetDip, 9);
    }

    [Fact]
    public void Clone_KeepsStyleAndFlags_GetsTheNewId_AndLeavesTheOriginalAlone()
    {
        var map = ViewChartMapper.Standard();
        var seg = Segment("a", 10, 20, locked: true);

        var copy = DrawingGeometry.Clone(seg, "new-id", map);

        Assert.Equal("new-id", copy.Id);
        Assert.Equal("a", seg.Id);
        Assert.Equal(seg.TypeId, copy.TypeId);
        Assert.Equal(seg.TypeVersion, copy.TypeVersion);
        Assert.Equal(seg.Style, copy.Style);
        Assert.True(copy.Locked);
        Assert.False(copy.Hidden);
        Assert.Equal(Bar(10), seg.Anchors[0].TimeUtc);
    }

    [Fact]
    public void Clone_DeepCopiesExtra()
    {
        var map = ViewChartMapper.Standard();
        var extra = new JsonObject { ["text"] = "hello" };
        var seg = Segment("a", 10, 20, extra: extra);

        var copy = DrawingGeometry.Clone(seg, "b", map);

        Assert.NotSame(extra, copy.Extra);
        Assert.Equal("hello", (string?)copy.Extra?["text"]);

        extra["text"] = "changed";
        Assert.Equal("hello", (string?)copy.Extra?["text"]);

        var noExtra = DrawingGeometry.Clone(Segment("c", 10, 20), "d", map);
        Assert.Null(noExtra.Extra);
    }

    // ---------- LOCK and HIDE ----------

    [Fact]
    public void LockAndHide_Rules()
    {
        var open = Segment("a", 10, 20);
        var locked = Segment("b", 10, 20, locked: true);
        var hidden = Segment("c", 10, 20, hidden: true);

        Assert.True(DrawingGeometry.CanMove(open));
        Assert.True(DrawingGeometry.CanResize(open));
        Assert.True(DrawingGeometry.CanDelete(open));

        Assert.False(DrawingGeometry.CanMove(locked));
        Assert.False(DrawingGeometry.CanResize(locked));
        Assert.False(DrawingGeometry.CanDelete(locked));
        Assert.True(DrawingGeometry.IsSelectable(locked));
        Assert.True(DrawingGeometry.IsRendered(locked));

        Assert.False(DrawingGeometry.IsRendered(hidden));
        Assert.False(DrawingGeometry.IsHittable(hidden));
        Assert.False(DrawingGeometry.IsSelectable(hidden));
    }

    // ---------- DEFAULT STYLE ----------

    [Fact]
    public void DefaultStyle_Rules()
    {
        var accent = RgbaColor.FromRgb(1, 2, 3);
        var style = DrawingStyleRules.Default(accent);

        Assert.Equal(accent, style.Color);
        Assert.Equal(1.0, style.Thickness, 9);
        Assert.Equal(1.0, style.Opacity, 9);
        Assert.Equal(DrawingLineStyle.Solid, style.LineStyle);

        Assert.Equal(new double[] { 1, 2, 3, 4 }, DrawingStyleRules.SelectableWidths.ToArray());

        Assert.Null(DrawingStyleRules.DashPattern(DrawingLineStyle.Solid));
        Assert.Equal(new[] { 6.0, 4.0 }, DrawingStyleRules.DashPattern(DrawingLineStyle.Dashed)!);
        Assert.Equal(new[] { 2.0, 3.0 }, DrawingStyleRules.DashPattern(DrawingLineStyle.Dotted)!);
    }

    // ---------- DrawingMath (segment and rectangle geometry, GOLDEN_TEST_VECTORS.DrawingHit) ----------

    [Fact]
    public void DrawingMath_SegmentVectors_AgainstTheSixDipTolerance()
    {
        var a = new PointD(0, 0);
        var b = new PointD(100, 0);
        double tolerance = DrawingConstants.HitToleranceDip;

        Assert.True(DrawingMath.DistancePointToSegment(new PointD(50, 5), a, b) <= tolerance);
        Assert.False(DrawingMath.DistancePointToSegment(new PointD(50, 7), a, b) <= tolerance);
        Assert.True(DrawingMath.DistancePointToSegment(new PointD(103, 4), a, b) <= tolerance);
        Assert.False(DrawingMath.DistancePointToSegment(new PointD(110, 0), a, b) <= tolerance);
    }

    [Fact]
    public void DrawingMath_Rectangle_InsideEdgeAndOutside()
    {
        var rect = new RectD(0, 0, 100, 100);

        Assert.True(DrawingMath.PointInRect(new PointD(50, 50), rect));
        Assert.True(DrawingMath.PointInRect(new PointD(0, 50), rect));
        Assert.False(DrawingMath.PointInRect(new PointD(110, 50), rect));

        Assert.Equal(50.0, DrawingMath.DistancePointToRectEdges(new PointD(50, 50), rect), 9);
        Assert.Equal(5.0, DrawingMath.DistancePointToRectEdges(new PointD(30, 5), rect), 9);
        Assert.Equal(0.0, DrawingMath.DistancePointToRectEdges(new PointD(0, 50), rect), 9);
        Assert.Equal(10.0, DrawingMath.DistancePointToRectEdges(new PointD(110, 50), rect), 9);
        Assert.Equal(Math.Sqrt(200.0), DrawingMath.DistancePointToRectEdges(new PointD(110, 110), rect), 9);
    }

    [Fact]
    public void DrawingMath_Ellipse_OnTheOutlineAndDegenerate()
    {
        var centre = new PointD(0, 0);

        Assert.Equal(0.0, DrawingMath.DistancePointToEllipseEdge(new PointD(50, 0), centre, 50, 30), 9);
        Assert.Equal(0.0, DrawingMath.DistancePointToEllipseEdge(new PointD(0, 30), centre, 50, 30), 9);
        Assert.True(double.IsPositiveInfinity(DrawingMath.DistancePointToEllipseEdge(new PointD(1, 1), centre, 0, 30)));
    }
}
