using System.Globalization;
using MyChart.Core.Contracts.Rendering;
using MyChart.Core.Models.Geometry;
using MyChart.Core.Models.Chart;
using MyChart.Core.Models.Market;
using MyChart.Core.Models.Rendering;

namespace MyChart.Core.Rendering;

/// <summary>
/// T6.01 HudRenderer — layer 8; mouse transparent; non-interactive; no selection.
/// Text color = HudColor; opacity = 1 - transparency/100; idle dims to 40 percent after 5 s.
/// Startup uses Minimal presentation (still visible).
/// </summary>
public sealed class HudRenderer : ILayerRenderer
{
    public const double IdleSeconds = 5.0;
    public const double IdleOpacityFactor = 0.40;
    public const double MarginDip = 8;
    public const double LineGapDip = 2;

    private static readonly TextStyle DefaultStyle = new("Segoe UI", 12, Bold: false);

    public RenderLayer Layer => RenderLayer.Hud;

    public HudState? State { get; set; }
    public ThemeTokens Theme { get; set; } = ThemeTokens.Dark;

    /// <summary>0..100; opacity = 1 - value/100 (schema default 0 → full opacity).</summary>
    public int Transparency { get; set; }

    /// <summary>TopLeft (default), TopRight, BottomLeft, BottomRight.</summary>
    public string Position { get; set; } = "TopLeft";

    public TextStyle Style { get; set; } = DefaultStyle;

    /// <summary>True when pointer has been idle over the chart for >= IdleSeconds.</summary>
    public bool IsIdle { get; set; }

    public void Render(IRenderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (State is null)
            return;

        var lines = BuildLines(State);
        if (lines.Count == 0)
            return;

        double opacity = Math.Clamp(1.0 - Transparency / 100.0, 0.0, 1.0);
        if (IsIdle)
            opacity *= IdleOpacityFactor;
        if (opacity <= 0)
            return;

        var color = WithOpacity(Theme.HudColor, opacity);
        double scale = context.DpiScale <= 0 ? 1 : context.DpiScale;
        double margin = MarginDip * scale;
        double gap = LineGapDip * scale;

        var sizes = new SizeD[lines.Count];
        double maxW = 0, totalH = 0;
        for (int i = 0; i < lines.Count; i++)
        {
            sizes[i] = context.MeasureText(lines[i], Style);
            if (sizes[i].Width > maxW) maxW = sizes[i].Width;
            totalH += sizes[i].Height;
            if (i > 0) totalH += gap;
        }

        var (originX, originY, align) = ResolveOrigin(context.Width, context.Height, margin, maxW, totalH, Position);

        double y = originY;
        for (int i = 0; i < lines.Count; i++)
        {
            context.DrawText(lines[i], originX, y, Style, color, align);
            y += sizes[i].Height + gap;
        }
    }

    /// <summary>Effective opacity for tests / idle logic.</summary>
    public static double ComputeOpacity(int transparency, bool idle)
    {
        double o = Math.Clamp(1.0 - transparency / 100.0, 0.0, 1.0);
        if (idle) o *= IdleOpacityFactor;
        return o;
    }

    public static bool ShouldIdle(TimeSpan sinceLastPointerMove)
        => sinceLastPointerMove.TotalSeconds >= IdleSeconds;

    public static List<string> BuildLines(HudState state)
    {
        var lines = new List<string>();
        var v = state.Visible;

        if (v.HasFlag(HudField.Symbol) || v.HasFlag(HudField.Timeframe))
        {
            var parts = new List<string>();
            if (v.HasFlag(HudField.Symbol)) parts.Add(state.Symbol);
            if (v.HasFlag(HudField.Timeframe)) parts.Add(state.Timeframe.ToString());
            lines.Add(string.Join(" · ", parts));
        }

        if (v.HasFlag(HudField.OhlcUnderMouse) && state.OhlcUnderMouse is { } c)
        {
            lines.Add(string.Create(CultureInfo.InvariantCulture,
                $"O {c.Open}  H {c.High}  L {c.Low}  C {c.Close}"));
        }

        if (v.HasFlag(HudField.Spread))
        {
            lines.Add(state.SpreadPoints.HasValue
                ? $"Spread {state.SpreadPoints.Value}"
                : "Spread -");
        }

        if (v.HasFlag(HudField.MarketStatus))
            lines.Add(state.MarketStatus.ToString());

        if (v.HasFlag(HudField.VisibleBars))
            lines.Add($"Bars {state.VisibleBars}");

        if (v.HasFlag(HudField.Start) && state.Start.HasValue)
            lines.Add($"Start {state.Start.Value:yyyy-MM-dd HH:mm}");

        if (v.HasFlag(HudField.End) && state.End.HasValue)
            lines.Add($"End {state.End.Value:yyyy-MM-dd HH:mm}");

        if (v.HasFlag(HudField.ZoomLevel))
            lines.Add($"Zoom {state.ZoomLevelPercent}%");

        if (v.HasFlag(HudField.Fps))
            lines.Add(string.Create(CultureInfo.InvariantCulture, $"FPS {state.Fps:0.0}"));

        if (v.HasFlag(HudField.LoadedCandles))
            lines.Add($"Loaded {state.LoadedCandles}");

        if (v.HasFlag(HudField.ProviderName) && !string.IsNullOrEmpty(state.ProviderName))
            lines.Add(state.ProviderName);

        if (v.HasFlag(HudField.CacheStatus))
            lines.Add($"Cache {state.CacheStatus}");

        return lines;
    }

    private static (double x, double y, TextAlign align) ResolveOrigin(
        int width, int height, double margin, double blockW, double blockH, string position)
    {
        bool right = position.Contains("Right", StringComparison.OrdinalIgnoreCase);
        bool bottom = position.Contains("Bottom", StringComparison.OrdinalIgnoreCase);

        double x = right ? width - margin : margin;
        double y = bottom ? height - margin - blockH : margin;
        var align = right ? TextAlign.Right : TextAlign.Left;
        return (x, y, align);
    }

    private static RgbaColor WithOpacity(RgbaColor c, double opacity)
    {
        byte a = (byte)Math.Clamp(Math.Round(c.A * opacity), 0, 255);
        return RgbaColor.FromArgb(a, c.R, c.G, c.B);
    }
}
