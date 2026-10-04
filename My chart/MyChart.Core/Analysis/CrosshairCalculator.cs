using System.Globalization;
using MyChart.Core.Candles;
using MyChart.Core.Models.Chart;
using MyChart.Core.Models.Market;
using MyChart.Core.Scale;

namespace MyChart.Core.Analysis;

/// <summary>
/// T3.01 CrosshairCalculations — pure calculation of the cursor, Analysis, DataInspector and Magnet.
/// No pixels beyond the coordinates given, no IO, no UI state.
/// </summary>
public static class CrosshairCalculator
{
    /// <summary>Magnet radius in DIP (vertical distance to Open, High, Low or Close).</summary>
    public const double MagnetRadiusDip = 24.0;

    /// <summary>
    /// CURSOR + optional DATA_INSPECTOR, MAGNET and ANALYSIS for one pointer position.
    /// converter.N must equal bars.Count and mapper.N.
    /// </summary>
    public static CrosshairState Compute(
        double mouseX,
        double mouseY,
        CoordinateConverter converter,
        TimeIndexMapper mapper,
        IReadOnlyList<Candle> bars,
        SymbolInfo symbol,
        Timeframe timeframe,
        CrosshairFlags flags,
        AnalysisAnchor? anchor = null,
        TimeZoneInfo? displayZone = null)
    {
        var vs = converter.ViewState;
        bool inside = mouseX >= vs.PlotLeft && mouseX <= vs.PlotRight
                      && mouseY >= vs.PlotTop && mouseY <= vs.PlotTop + vs.PlotHeight;

        // X snaps to the centre of the nearest bar
        double u = converter.U(mouseX);
        int snap = mapper.SnapIndex(u);
        double x = converter.X(snap);

        bool hasBar = snap >= 0 && snap <= bars.Count - 1;

        // price mapped back to real price space (Log and Percentage aware by the converter)
        double price = converter.Price(mouseY);
        double y = mouseY;
        bool snapped = false;

        if (flags.Magnet && hasBar)
        {
            if (TryMagnetPrice(bars[snap], mouseY, converter, out double magnetPrice))
            {
                price = magnetPrice;
                y = converter.Y(magnetPrice);
                snapped = true;
            }
        }

        DateTimeOffset timeUtc = default;
        string timeLabel = string.Empty;
        if (mapper.N > 0)
        {
            timeUtc = mapper.TimeAtIndex(snap);
            timeLabel = FormatTimeLabel(timeUtc, timeframe, displayZone);
        }

        DataInspectorValues? inspector = null;
        if (flags.DataInspector && hasBar)
        {
            var c = bars[snap];
            inspector = new DataInspectorValues(
                ToDisplay(c.Timestamp, displayZone),
                c.Open, c.High, c.Low, c.Close, c.Volume);
        }

        AnalysisValues? analysis = null;
        if (flags.Analysis && anchor.HasValue && mapper.N > 0)
            analysis = ComputeAnalysis(anchor.Value, snap, price, symbol, mapper);

        return new CrosshairState
        {
            IsInsidePlot = inside,
            SnapIndex = snap,
            X = x,
            Y = y,
            Price = price,
            IsMagnetSnapped = snapped,
            TimeUtc = timeUtc,
            TimeLabel = timeLabel,
            PriceLabel = FormatPrice(price, symbol.Digits),
            DataInspector = inspector,
            Analysis = analysis
        };
    }

    /// <summary>
    /// MAGNET: within 24 DIP (vertical) of Open, High, Low or Close of the candle -> the NEAREST of the four
    /// (ties resolved in the order Open, High, Low, Close). Otherwise false (price is free).
    /// </summary>
    public static bool TryMagnetPrice(Candle candle, double mouseY, CoordinateConverter converter, out double price)
    {
        double best = double.MaxValue;
        double bestPrice = 0;
        Nearest(candle.Open, mouseY, converter, ref best, ref bestPrice);
        Nearest(candle.High, mouseY, converter, ref best, ref bestPrice);
        Nearest(candle.Low, mouseY, converter, ref best, ref bestPrice);
        Nearest(candle.Close, mouseY, converter, ref best, ref bestPrice);

        price = bestPrice;
        return best <= MagnetRadiusDip;
    }

    private static void Nearest(double value, double mouseY, CoordinateConverter converter, ref double best, ref double bestPrice)
    {
        double d = Math.Abs(mouseY - converter.Y(value));
        if (d < best)
        {
            best = d;
            bestPrice = value;
        }
    }

    /// <summary>
    /// ANALYSIS between anchor A and the cursor B (index iB, price priceB).
    /// PercentageChange base is A in every scale mode.
    /// </summary>
    public static AnalysisValues ComputeAnalysis(
        AnalysisAnchor a,
        int indexB,
        double priceB,
        SymbolInfo symbol,
        TimeIndexMapper mapper)
    {
        double diff = priceB - a.Price;

        double pointSize = SymbolMath.PointSize(symbol.Digits);
        long points = (long)Math.Round(diff / pointSize, MidpointRounding.AwayFromZero);

        double? pipSize = SymbolMath.PipSize(symbol.Group, symbol.Digits);
        double? pips = pipSize.HasValue
            ? Math.Round(diff / pipSize.Value, 1, MidpointRounding.AwayFromZero)
            : null;

        var time = mapper.TimeAtIndex(indexB) - mapper.TimeAtIndex(a.Index);
        int count = Math.Abs(indexB - a.Index);

        double pct = a.Price == 0
            ? double.NaN
            : Math.Round(diff / a.Price * 100.0, 2, MidpointRounding.AwayFromZero);

        return new AnalysisValues(diff, points, pips, time, count, pct);
    }

    /// <summary>First LeftClick while Analysis is on sets the anchor; a second LeftClick clears it. (Esc: caller sets null.)</summary>
    public static AnalysisAnchor? NextAnchorOnLeftClick(AnalysisAnchor? current, int snapIndex, double price)
        => current.HasValue ? null : new AnalysisAnchor(snapIndex, price);

    // ---------- formatting ----------

    /// <summary>Price label: decimals = Digits.</summary>
    public static string FormatPrice(double price, int digits)
        => price.ToString("F" + Math.Max(0, digits), CultureInfo.InvariantCulture);

    /// <summary>
    /// Time label in the display time zone (UTC when null).
    /// Intraday "ddd d MMM 'yy HH:mm" ; D1 and W1 "ddd d MMM 'yy" ; MN1 "MMM yyyy".
    /// timeOfDayFormat is the intraday time part (setting axes.time.format, default HH:mm).
    /// </summary>
    public static string FormatTimeLabel(
        DateTimeOffset utc,
        Timeframe timeframe,
        TimeZoneInfo? displayZone = null,
        string timeOfDayFormat = "HH:mm")
    {
        var t = ToDisplay(utc, displayZone);
        var inv = CultureInfo.InvariantCulture;

        if (timeframe == Timeframe.MN1)
            return t.ToString("MMM yyyy", inv);

        string day = t.ToString("ddd d MMM", inv) + " '" + t.ToString("yy", inv);

        if (timeframe == Timeframe.D1 || timeframe == Timeframe.W1)
            return day;

        return day + " " + t.ToString(timeOfDayFormat, inv);
    }

    /// <summary>Signed value with a fixed number of decimals: +0.00250, -250, +25.0. NaN or infinity gives "-".</summary>
    public static string FormatSigned(double value, int decimals)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            return "-";

        int d = Math.Max(0, decimals);
        double r = Math.Round(value, d, MidpointRounding.AwayFromZero);
        string sign = r < 0 ? "-" : "+";
        return sign + Math.Abs(r).ToString("F" + d, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Elapsed time as "d h m": "3h 45m", "1d 2h 30m", "45m", "0m". Negative values get a leading "-".
    /// Seconds are rounded to the nearest minute.
    /// </summary>
    public static string FormatTimeDifference(TimeSpan span)
    {
        long totalMinutes = (long)Math.Round(Math.Abs(span.TotalMinutes), MidpointRounding.AwayFromZero);
        long days = totalMinutes / 1440;
        long hours = totalMinutes % 1440 / 60;
        long minutes = totalMinutes % 60;

        string text;
        if (days > 0)
            text = $"{days}d {hours}h {minutes}m";
        else if (hours > 0)
            text = $"{hours}h {minutes}m";
        else
            text = $"{minutes}m";

        return span < TimeSpan.Zero && totalMinutes > 0 ? "-" + text : text;
    }

    private static DateTimeOffset ToDisplay(DateTimeOffset utc, TimeZoneInfo? displayZone)
        => TimeZoneInfo.ConvertTime(utc, displayZone ?? TimeZoneInfo.Utc);
}
