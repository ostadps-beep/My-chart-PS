using MyChart.Core.Analysis;
using MyChart.Core.Models.Geometry;
using Xunit;

namespace MyChart.Tests.Smoke;

public class DrawingMathTests
{
    [Fact]
    public void DistancePointToSegment_MidpointOffset_IsCorrect()
    {
        // segment (0,0)-(100,0): point (50,5) distance 5
        var d = DrawingMath.DistancePointToSegment(
            new PointD(50, 5),
            new PointD(0, 0),
            new PointD(100, 0));
        Assert.Equal(5.0, d, 6);
    }

    [Fact]
    public void DistancePointToSegment_BeyondEnd_IsCorrect()
    {
        // segment (0,0)-(100,0): point (110,0) distance 10
        var d = DrawingMath.DistancePointToSegment(
            new PointD(110, 0),
            new PointD(0, 0),
            new PointD(100, 0));
        Assert.Equal(10.0, d, 6);
    }

    [Fact]
    public void DistancePointToSegment_NearEndOffset_IsCorrect()
    {
        // segment (0,0)-(100,0): point (103,4) distance 5
        var d = DrawingMath.DistancePointToSegment(
            new PointD(103, 4),
            new PointD(0, 0),
            new PointD(100, 0));
        Assert.Equal(5.0, d, 6);
    }
}
