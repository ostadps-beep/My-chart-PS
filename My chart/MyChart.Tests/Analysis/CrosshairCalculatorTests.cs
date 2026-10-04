using MyChart.Core.Analysis;
using MyChart.Core.Models.Chart;
using MyChart.Core.Models.Market;
using MyChart.Core.Models.Viewport;
using MyChart.Core.Scale;
using Xunit;

namespace MyChart.Tests.Analysis;

/// <summary>
/// T3.01 CrosshairCalculations — GOLDEN_TEST_VECTORS Crosshair / Analysis plus cursor, magnet and inspector rules.
/// Fixture: N=100, plotLeft 0, plotWidth 800, plotHeight 500, BarSpacing 8, RightOffset 5 (X(99)=756),
/// price range [1.0940, 1.1060], M15 bars from 2024-03-13T12:00Z, every bar O 1.1000 H 1.1010 L 1.0990 C 1.1005.
/// </summary>
public class CrosshairCalculatorTests
{
    private sealed class Fx
    {
        public ViewState Vs { get; }
        public CoordinateConverter Conv { get; }
        public TimeIndexMapper Mapper { get; }
        public List<Candle> Bars { get; }
        public DateTimeOffset Start { get; } = new(2024, 3, 13, 12, 0, 0, TimeSpan.Zero);

        public Fx(int n = 100)
        {
            Vs = new ViewState
            {
                Width = 864,
                Height = 524,
                PriceAxisWidth = 64,
                TimeAxisHeight = 24,
                BarSpacing = 8,
                RightOffset = 5
            };
            Vs.PriceScale.MinPrice = 1.0940;
            Vs.PriceScale.MaxPrice = 1.1060;

            Bars = new List<Candle>();
            var opens = new List<DateTimeOffset>();
            for (int i = 0; i < n; i++)
            {
                var ts = Start.AddMinutes(15 * i);
                opens.Add(ts);
                Bars.Add(new Candle(ts, 1.1000, 1.1010, 1.0990, 1.1005, 100 + i));
            }

            var cal = new SessionCalendar(ServerTimeRule.Utc, SymbolGroup.Forex);
            Mapper = new TimeIndexMapper(opens, Timeframe.M15, cal);
            Conv = new CoordinateConverter(Vs, n);
        }

        public CrosshairState Compute(double x, double y, CrosshairFlags flags, AnalysisAnchor? anchor = null, TimeZoneInfo? zone = null)
            => CrosshairCalculator.Compute(x, y, Conv, Mapper, Bars, Eurusd, Timeframe.M15, flags, anchor, zone);
    }

    private static readonly SymbolInfo Eurusd = new("EURUSD", "EURUSD", SymbolGroup.Forex, 5);
    private static readonly SymbolInfo Usdjpy = new("USDJPY", "USDJPY", SymbolGroup.Forex, 3);
    private static readonly SymbolInfo Xauusd = new("XAUUSD", "XAUUSD", SymbolGroup.Commodities, 2);

    private static readonly CrosshairFlags None = new(false, false, false);

    // ---------- CURSOR ----------

    [Fact]
    public void Cursor_X_SnapsToCentreOfNearestBar()
    {
        var fx = new Fx();

        // U(753) = 98.625 -> SnapIndex 99 -> X(99) = 756
        var s1 = fx.Compute(753, 250, None);
        Assert.Equal(99, s1.SnapIndex);
        Assert.Equal(756.0, s1.X, 9);

        // U(750) = 98.25 -> SnapIndex 98 -> X(98) = 748
        var s2 = fx.Compute(750, 250, None);
        Assert.Equal(98, s2.SnapIndex);
        Assert.Equal(748.0, s2.X, 9);
    }

    [Fact]
    public void Cursor_Price_IsMappedBackFromY()
    {
        var fx = new Fx();
        var s = fx.Compute(756, 250, None);

        // middle of [1.0940, 1.1060]
        Assert.Equal(1.1000, s.Price, 9);
        Assert.Equal(250.0, s.Y, 9);
        Assert.False(s.IsMagnetSnapped);
        Assert.Equal("1.10000", s.PriceLabel);
    }

    [Fact]
    public void Cursor_PriceLabel_UsesDigits()
    {
        Assert.Equal("150.500", CrosshairCalculator.FormatPrice(150.5, 3));
        Assert.Equal("1.10000", CrosshairCalculator.FormatPrice(1.1, 5));
        Assert.Equal("2310", CrosshairCalculator.FormatPrice(2310.0, 0));
    }

    [Fact]
    public void Cursor_Price_FollowsPercentageScale()
    {
        var fx = new Fx();
        fx.Vs.PriceScale.TransformKind = ScaleTransformKind.Percentage;
        fx.Vs.PriceScale.PercentageBase = 1.1000;

        // the mapping back must round-trip a real price through Y
        double y = fx.Conv.Y(1.1055);
        var s = fx.Compute(756, y, None);
        Assert.Equal(1.1055, s.Price, 9);
    }

    [Fact]
    public void Cursor_Time_UsesBarOpenAndExtrapolatesFutureSlots()
    {
        var fx = new Fx();

        // bar 99 = start + 99 * 15 min = Thu 2024-03-14 12:45Z
        var s = fx.Compute(756, 250, None);
        Assert.Equal(fx.Start.AddMinutes(99 * 15), s.TimeUtc);

        // U(790) = 103.25 -> SnapIndex 103 (future slot): last open + 4 buckets
        var future = fx.Compute(790, 250, None);
        Assert.Equal(103, future.SnapIndex);
        Assert.Equal(fx.Start.AddMinutes(103 * 15), future.TimeUtc);
    }

    [Fact]
    public void Cursor_TimeLabel_IsInDisplayTimeZone()
    {
        var fx = new Fx();

        var utc = fx.Compute(756, 250, None);
        Assert.Equal("Thu 14 Mar '24 12:45", utc.TimeLabel);

        var plus3 = TimeZoneInfo.CreateCustomTimeZone("T3", TimeSpan.FromHours(3), "T3", "T3");
        var shifted = fx.Compute(756, 250, None, null, plus3);
        Assert.Equal("Thu 14 Mar '24 15:45", shifted.TimeLabel);
    }

    [Fact]
    public void TimeLabel_Formats_PerTimeframe()
    {
        var t = new DateTimeOffset(2024, 3, 13, 14, 30, 0, TimeSpan.Zero);

        Assert.Equal("Wed 13 Mar '24 14:30", CrosshairCalculator.FormatTimeLabel(t, Timeframe.M15));
        Assert.Equal("Wed 13 Mar '24 14:30", CrosshairCalculator.FormatTimeLabel(t, Timeframe.H4));
        Assert.Equal("Wed 13 Mar '24", CrosshairCalculator.FormatTimeLabel(t, Timeframe.D1));
        Assert.Equal("Wed 13 Mar '24", CrosshairCalculator.FormatTimeLabel(t, Timeframe.W1));
        Assert.Equal("Mar 2024", CrosshairCalculator.FormatTimeLabel(t, Timeframe.MN1));
        Assert.Equal("Wed 13 Mar '24 14:30:00", CrosshairCalculator.FormatTimeLabel(t, Timeframe.M15, null, "HH:mm:ss"));
    }

    [Fact]
    public void Cursor_IsInsidePlot()
    {
        var fx = new Fx();
        Assert.True(fx.Compute(400, 250, None).IsInsidePlot);
        Assert.False(fx.Compute(-5, 250, None).IsInsidePlot);
        Assert.False(fx.Compute(400, 600, None).IsInsidePlot);
        Assert.False(fx.Compute(830, 250, None).IsInsidePlot);
    }

    // ---------- MAGNET ----------

    [Fact]
    public void Magnet_SnapsToNearestOfFour_WithinRadius()
    {
        var fx = new Fx();
        var flags = new CrosshairFlags(false, false, true);

        // Y(High 1.1010) = 208.33 ; Y(Close 1.1005) = 229.17 ; Y(Open 1.1000) = 250 ; Y(Low 1.0990) = 291.67
        // mouse Y 215: High is nearest (6.7 DIP), Close is also inside 24 DIP but farther
        var s = fx.Compute(756, 215, flags);
        Assert.True(s.IsMagnetSnapped);
        Assert.Equal(1.1010, s.Price, 9);
        Assert.Equal(fx.Conv.Y(1.1010), s.Y, 9);
        Assert.Equal("1.10100", s.PriceLabel);

        // mouse Y 300: Low is nearest (8.3 DIP)
        var low = fx.Compute(756, 300, flags);
        Assert.True(low.IsMagnetSnapped);
        Assert.Equal(1.0990, low.Price, 9);
    }

    [Fact]
    public void Magnet_RadiusEdges()
    {
        var fx = new Fx();
        var flags = new CrosshairFlags(false, false, true);
        double yHigh = fx.Conv.Y(1.1010);

        // 23 DIP above High: inside
        Assert.True(fx.Compute(756, yHigh - 23, flags).IsMagnetSnapped);

        // 25 DIP above High: outside, price stays free
        var outside = fx.Compute(756, yHigh - 25, flags);
        Assert.False(outside.IsMagnetSnapped);
        Assert.Equal(fx.Conv.Price(yHigh - 25), outside.Price, 9);
        Assert.Equal(yHigh - 25, outside.Y, 9);
    }

    [Fact]
    public void Magnet_Off_KeepsPriceFree()
    {
        var fx = new Fx();
        var s = fx.Compute(756, 215, None);
        Assert.False(s.IsMagnetSnapped);
        Assert.Equal(fx.Conv.Price(215), s.Price, 9);
        Assert.Equal(215.0, s.Y, 9);
    }

    [Fact]
    public void Magnet_NotInFutureSlots()
    {
        var fx = new Fx();
        var flags = new CrosshairFlags(false, false, true);

        // SnapIndex 103 has no candle
        var s = fx.Compute(790, 208, flags);
        Assert.Equal(103, s.SnapIndex);
        Assert.False(s.IsMagnetSnapped);
    }

    // ---------- DATA INSPECTOR ----------

    [Fact]
    public void DataInspector_ShowsCandleAtSnapIndex()
    {
        var fx = new Fx();
        var flags = new CrosshairFlags(false, true, false);

        var s = fx.Compute(756, 250, flags);
        Assert.True(s.DataInspector.HasValue);

        var di = s.DataInspector.GetValueOrDefault();
        Assert.Equal(fx.Start.AddMinutes(99 * 15), di.DisplayTime);
        Assert.Equal(1.1000, di.Open, 9);
        Assert.Equal(1.1010, di.High, 9);
        Assert.Equal(1.0990, di.Low, 9);
        Assert.Equal(1.1005, di.Close, 9);
        Assert.Equal(199.0, di.Volume, 9);
    }

    [Fact]
    public void DataInspector_FirstAndLastBarInside_FutureAndPastHidden()
    {
        var fx = new Fx();
        var flags = new CrosshairFlags(false, true, false);

        // X(0) = -36 is outside the plot but SnapIndex 0 is a real bar
        Assert.True(fx.Compute(fx.Conv.X(0), 250, flags).DataInspector.HasValue);
        Assert.True(fx.Compute(fx.Conv.X(99), 250, flags).DataInspector.HasValue);

        // index 100 (future) and -1 (before the first bar)
        Assert.False(fx.Compute(fx.Conv.X(100), 250, flags).DataInspector.HasValue);
        Assert.False(fx.Compute(fx.Conv.X(-1), 250, flags).DataInspector.HasValue);
    }

    [Fact]
    public void DataInspector_Off_ReturnsNull()
    {
        var fx = new Fx();
        Assert.False(fx.Compute(756, 250, None).DataInspector.HasValue);
    }

    // ---------- ANALYSIS golden vectors ----------

    [Fact]
    public void Analysis_Eurusd_GoldenVector()
    {
        var fx = new Fx();
        var a = new AnalysisAnchor(10, 1.10000);
        var v = CrosshairCalculator.ComputeAnalysis(a, 25, 1.10250, Eurusd, fx.Mapper);

        Assert.Equal(0.00250, v.PriceDifference, 9);
        Assert.Equal(250L, v.PointDifference);
        Assert.True(v.PipDifference.HasValue);
        Assert.Equal(25.0, v.PipDifference.GetValueOrDefault(), 9);
        Assert.Equal(0.23, v.PercentageChange, 9);
        Assert.Equal(15, v.CandleCount);
        Assert.Equal(TimeSpan.FromMinutes(225), v.TimeDifference);

        Assert.Equal("+0.00250", CrosshairCalculator.FormatSigned(v.PriceDifference, 5));
        Assert.Equal("+250", CrosshairCalculator.FormatSigned(v.PointDifference, 0));
        Assert.Equal("+25.0", CrosshairCalculator.FormatSigned(v.PipDifference.GetValueOrDefault(), 1));
        Assert.Equal("+0.23", CrosshairCalculator.FormatSigned(v.PercentageChange, 2));
        Assert.Equal("3h 45m", CrosshairCalculator.FormatTimeDifference(v.TimeDifference));
    }

    [Fact]
    public void Analysis_Usdjpy_GoldenVector()
    {
        var fx = new Fx();
        var a = new AnalysisAnchor(10, 150.000);
        var v = CrosshairCalculator.ComputeAnalysis(a, 25, 150.500, Usdjpy, fx.Mapper);

        Assert.Equal(0.500, v.PriceDifference, 9);
        Assert.Equal(500L, v.PointDifference);
        Assert.Equal(50.0, v.PipDifference.GetValueOrDefault(), 9);
        Assert.Equal(0.33, v.PercentageChange, 9);

        Assert.Equal("+0.500", CrosshairCalculator.FormatSigned(v.PriceDifference, 3));
        Assert.Equal("+500", CrosshairCalculator.FormatSigned(v.PointDifference, 0));
        Assert.Equal("+50.0", CrosshairCalculator.FormatSigned(v.PipDifference.GetValueOrDefault(), 1));
        Assert.Equal("+0.33", CrosshairCalculator.FormatSigned(v.PercentageChange, 2));
    }

    [Fact]
    public void Analysis_Xauusd_PipDifferenceHidden()
    {
        var fx = new Fx();
        var a = new AnalysisAnchor(10, 2300.00);
        var v = CrosshairCalculator.ComputeAnalysis(a, 25, 2310.50, Xauusd, fx.Mapper);

        Assert.False(v.PipDifference.HasValue);
        Assert.Equal(1050L, v.PointDifference);
    }

    [Fact]
    public void Analysis_Negative_IsSigned()
    {
        var fx = new Fx();
        var a = new AnalysisAnchor(25, 1.10250);
        var v = CrosshairCalculator.ComputeAnalysis(a, 10, 1.10000, Eurusd, fx.Mapper);

        Assert.Equal(-250L, v.PointDifference);
        Assert.Equal(-25.0, v.PipDifference.GetValueOrDefault(), 9);
        Assert.Equal(-0.23, v.PercentageChange, 9);
        Assert.Equal(15, v.CandleCount);
        Assert.Equal(TimeSpan.FromMinutes(-225), v.TimeDifference);

        Assert.Equal("-0.00250", CrosshairCalculator.FormatSigned(v.PriceDifference, 5));
        Assert.Equal("-250", CrosshairCalculator.FormatSigned(v.PointDifference, 0));
        Assert.Equal("-3h 45m", CrosshairCalculator.FormatTimeDifference(v.TimeDifference));
    }

    [Fact]
    public void Analysis_ZeroAnchorPrice_PercentageIsNaN()
    {
        var fx = new Fx();
        var v = CrosshairCalculator.ComputeAnalysis(new AnalysisAnchor(10, 0.0), 25, 1.0, Eurusd, fx.Mapper);
        Assert.True(double.IsNaN(v.PercentageChange));
        Assert.Equal("-", CrosshairCalculator.FormatSigned(v.PercentageChange, 2));
    }

    [Fact]
    public void Analysis_EndToEnd_ThroughCompute()
    {
        var fx = new Fx();
        var flags = new CrosshairFlags(true, false, false);
        var anchor = new AnalysisAnchor(10, 1.10000);

        // cursor B on bar 25 at price 1.1025
        var s = fx.Compute(fx.Conv.X(25), fx.Conv.Y(1.1025), flags, anchor);

        Assert.Equal(25, s.SnapIndex);
        Assert.True(s.Analysis.HasValue);

        var v = s.Analysis.GetValueOrDefault();
        Assert.Equal(250L, v.PointDifference);
        Assert.Equal(15, v.CandleCount);
        Assert.Equal(TimeSpan.FromMinutes(225), v.TimeDifference);
        Assert.Equal(0.23, v.PercentageChange, 9);
    }

    [Fact]
    public void Analysis_UsesMagnetPriceForB()
    {
        var fx = new Fx();
        var flags = new CrosshairFlags(true, false, true);
        var anchor = new AnalysisAnchor(10, 1.10000);

        // cursor near the High (1.1010) of bar 25: B price is the magnet price
        var s = fx.Compute(fx.Conv.X(25), fx.Conv.Y(1.1010) + 5, flags, anchor);

        Assert.True(s.IsMagnetSnapped);
        Assert.Equal(1.1010, s.Price, 9);
        Assert.Equal(100L, s.Analysis.GetValueOrDefault().PointDifference);
    }

    [Fact]
    public void Analysis_NullWithoutAnchorOrFlag()
    {
        var fx = new Fx();
        var anchor = new AnalysisAnchor(10, 1.10000);

        Assert.False(fx.Compute(756, 250, new CrosshairFlags(true, false, false), null).Analysis.HasValue);
        Assert.False(fx.Compute(756, 250, None, anchor).Analysis.HasValue);
    }

    [Fact]
    public void Analysis_Anchor_SetThenClearedByClick()
    {
        var first = CrosshairCalculator.NextAnchorOnLeftClick(null, 10, 1.1);
        Assert.True(first.HasValue);
        Assert.Equal(10, first.GetValueOrDefault().Index);
        Assert.Equal(1.1, first.GetValueOrDefault().Price, 9);

        var second = CrosshairCalculator.NextAnchorOnLeftClick(first, 20, 1.2);
        Assert.False(second.HasValue);
    }

    // ---------- formatting ----------

    [Fact]
    public void FormatSigned_Rules()
    {
        Assert.Equal("+0.00250", CrosshairCalculator.FormatSigned(0.0025, 5));
        Assert.Equal("-0.00250", CrosshairCalculator.FormatSigned(-0.0025, 5));
        Assert.Equal("+0.500", CrosshairCalculator.FormatSigned(0.5, 3));
        Assert.Equal("+0.00000", CrosshairCalculator.FormatSigned(0.0, 5));

        // a value that rounds to zero never shows a minus sign
        Assert.Equal("+0.00000", CrosshairCalculator.FormatSigned(-0.0000001, 5));
    }

    [Fact]
    public void FormatTimeDifference_Rules()
    {
        Assert.Equal("3h 45m", CrosshairCalculator.FormatTimeDifference(TimeSpan.FromMinutes(225)));
        Assert.Equal("1d 2h 30m", CrosshairCalculator.FormatTimeDifference(TimeSpan.FromMinutes(1440 + 120 + 30)));
        Assert.Equal("45m", CrosshairCalculator.FormatTimeDifference(TimeSpan.FromMinutes(45)));
        Assert.Equal("0m", CrosshairCalculator.FormatTimeDifference(TimeSpan.Zero));
        Assert.Equal("-1h 30m", CrosshairCalculator.FormatTimeDifference(TimeSpan.FromMinutes(-90)));
        Assert.Equal("2d 0h 0m", CrosshairCalculator.FormatTimeDifference(TimeSpan.FromDays(2)));
    }
}
