using MyChart.Core.Analysis;
using MyChart.Core.Models.Geometry;
using MyChart.PluginHost.DataTools;
using Xunit;

namespace MyChart.Tests.Plugins;

/// <summary>PG2.03 / AT15 DataToolHitVectors (screen-space geometry).</summary>
public class ShapeGeometryTests
{
    private const double Tol = 6.0;

    [Fact]
    public void TrendLine_At15_Vectors()
    {
        // anchors at screen (0,0) and (100,0)
        var a = new PointD(0, 0);
        var b = new PointD(100, 0);

        Assert.True(ShapeGeometry.HitLine(new PointD(50, 5), a, b, Tol));   // Body
        Assert.False(ShapeGeometry.HitLine(new PointD(50, 7), a, b, Tol));  // None
        Assert.True(ShapeGeometry.HitLine(new PointD(103, 4), a, b, Tol));  // Body (near end)
        Assert.False(ShapeGeometry.HitLine(new PointD(110, 0), a, b, Tol)); // None

        // Handles are at anchors — distance 0
        Assert.True(ShapeGeometry.HitLine(new PointD(0, 0), a, b, Tol));
        Assert.True(ShapeGeometry.HitLine(new PointD(100, 0), a, b, Tol));
    }

    [Fact]
    public void Rectangle_At15_Vectors()
    {
        var p1 = new PointD(0, 0);
        var p2 = new PointD(100, 50);

        Assert.True(ShapeGeometry.HitRect(new PointD(50, 25), p1, p2, Tol));  // inside Body
        Assert.True(ShapeGeometry.HitRect(new PointD(50, 4), p1, p2, Tol));   // on edge Body
        Assert.False(ShapeGeometry.HitRect(new PointD(150, 25), p1, p2, Tol)); // outside None
    }

    [Fact]
    public void Fibonacci_At15_Prices()
    {
        // P1 1.1000 P2 1.1100
        var levels = ShapeGeometry.StandardFibonacciLevels;
        var prices = ShapeGeometry.FibonacciPrices(1.1000, 1.1100, levels);

        Assert.Equal(1.11000, prices[0], 5); // level 0
        Assert.Equal(1.10764, prices[1], 5); // 0.236
        Assert.Equal(1.10618, prices[2], 5); // 0.382
        Assert.Equal(1.10500, prices[3], 5); // 0.5
        Assert.Equal(1.10382, prices[4], 5); // 0.618
        Assert.Equal(1.10214, prices[5], 5); // 0.786
        Assert.Equal(1.10000, prices[6], 5); // 1
    }

    [Fact]
    public void SegmentDistance_GoldenDrawingHit()
    {
        var a = new PointD(0, 0);
        var b = new PointD(100, 0);
        Assert.Equal(5, DrawingMath.DistancePointToSegment(new PointD(50, 5), a, b), 9);
        Assert.Equal(10, DrawingMath.DistancePointToSegment(new PointD(110, 0), a, b), 9);
        Assert.Equal(5, DrawingMath.DistancePointToSegment(new PointD(103, 4), a, b), 9);
    }
}
