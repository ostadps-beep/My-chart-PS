namespace MyChart.Core.Models.Settings;

/// <summary>
/// T6.08 SETTINGS_KEY_MAP routing — applies (key,value) into ChartSettingValues.
/// NOT_IN_V1 and unknown keys ignored (R7, R14). Unsupported options ignored (R8).
/// </summary>
public static class SettingsKeyRouter
{
    public static readonly HashSet<string> KnownKeys = new(StringComparer.Ordinal)
    {
        "chart.offline", "chart.shift", "chart.autoscroll", "chart.scale.fix",
        "chart.scale.fixed.minimum", "chart.scale.fixed.maximum",
        "chart.zoom.behavior", "chart.mouse.wheel", "chart.zoom.speed", "chart.scroll.speed",
        "chart.visible.candles", "chart.display.mode", "chart.show.ohlc",
        "grid.show", "grid.horizontal.color", "grid.style", "grid.horizontal", "grid.vertical",
        "grid.transparency", "axes.price.position", "axes.show.last.price", "candles.spacing",
        "hud.overlay.hud.items", "hud.overlay.hud.position", "hud.overlay.hud.text.color",
        "hud.overlay.hud.font.size", "hud.overlay.hud.font.type", "hud.overlay.hud.transparency",
        "performance.fps.limit", "performance.anti.aliasing", "performance.log.level",
        "performance.error.behavior", "drawing.tools.show.labels",
        "grid.background.background.color", "axes.axis.color",
        "candles.bull.color", "candles.bear.color", "candles.wick.color", "candles.border.color",
        "drawing.tools.tool.color", "hud.overlay.text.color", "theme.profile",
    };

    public static readonly HashSet<string> NotInV1 = new(StringComparer.Ordinal)
    {
        "workspace.reset", "workspace.import", "workspace.export",
        "candles.type",
    };

    public static bool TryApply(ChartSettingValues v, string key, object? value)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;
        if (NotInV1.Contains(key)) return false;
        try
        {
            return key switch
            {
                "chart.offline" => SetBool(() => v.Offline, x => v.Offline = x, value),
                "chart.shift" => SetBool(() => v.Shift, x => v.Shift = x, value),
                "chart.autoscroll" => SetBool(() => v.Autoscroll, x => v.Autoscroll = x, value),
                "chart.scale.fix" => SetBool(() => v.ScaleFix, x => v.ScaleFix = x, value),
                "chart.scale.fixed.minimum" => SetDouble(() => v.ScaleFixedMinimum, x => v.ScaleFixedMinimum = x, value),
                "chart.scale.fixed.maximum" => SetDouble(() => v.ScaleFixedMaximum, x => v.ScaleFixedMaximum = x, value),
                "chart.zoom.behavior" => SetString(() => v.ZoomBehavior, x => v.ZoomBehavior = x, value, "Both", "Price", "Time"),
                "chart.mouse.wheel" => SetString(() => v.MouseWheel, x => v.MouseWheel = x, value, "Zoom", "Scroll"),
                "chart.zoom.speed" => SetInt(() => v.ZoomSpeed, x => v.ZoomSpeed = x, value),
                "chart.scroll.speed" => SetInt(() => v.ScrollSpeed, x => v.ScrollSpeed = x, value),
                "chart.visible.candles" => SetInt(() => v.VisibleCandles, x => v.VisibleCandles = x, value),
                "chart.display.mode" => SetString(() => v.DisplayMode, x => v.DisplayMode = x, value, "Candlesticks", "HollowCandles", "OHLC"),
                "chart.show.ohlc" => SetBool(() => v.ShowOhlc, x => v.ShowOhlc = x, value),
                "grid.show" => SetBool(() => v.ShowGrid, x => v.ShowGrid = x, value),
                "grid.horizontal.color" => SetString(() => v.GridHorizontalColor, x => v.GridHorizontalColor = x, value),
                "grid.style" => SetString(() => v.GridStyle, x => v.GridStyle = x, value, "Solid", "Dashed", "Dotted"),
                "grid.horizontal" => SetBool(() => v.ShowHorizontalGrid, x => v.ShowHorizontalGrid = x, value),
                "grid.vertical" => SetBool(() => v.ShowVerticalGrid, x => v.ShowVerticalGrid = x, value),
                "grid.transparency" => SetInt(() => v.GridTransparency, x => v.GridTransparency = x, value),
                "axes.price.position" => SetString(() => v.PriceAxisPosition, x => v.PriceAxisPosition = x, value, "Right", "Left"),
                "axes.show.last.price" => SetBool(() => v.ShowLastPrice, x => v.ShowLastPrice = x, value),
                "axes.axis.color" => SetString(() => v.AxisColor, x => v.AxisColor = x, value),
                "candles.spacing" => SetDouble(() => v.CandlesSpacing, x => v.CandlesSpacing = x, value),
                "candles.bull.color" => SetString(() => v.BullColor, x => v.BullColor = x, value),
                "candles.bear.color" => SetString(() => v.BearColor, x => v.BearColor = x, value),
                "candles.wick.color" => SetString(() => v.WickColor, x => v.WickColor = x, value),
                "candles.border.color" => SetString(() => v.BorderColor, x => v.BorderColor = x, value),
                "grid.background.background.color" => SetString(() => v.BackgroundColor, x => v.BackgroundColor = x, value),
                "drawing.tools.tool.color" => SetString(() => v.AccentColor, x => v.AccentColor = x, value),
                "theme.profile" => SetString(() => v.ThemeProfileName, x => v.ThemeProfileName = x, value, "Dark", "Light", "ProDark", "Custom"),
                "hud.overlay.hud.items" => SetString(() => v.HudItems, x => v.HudItems = x, value),
                "hud.overlay.hud.position" => SetString(() => v.HudPosition, x => v.HudPosition = x, value, "TopLeft", "TopRight", "BottomLeft", "BottomRight"),
                "hud.overlay.hud.text.color" => SetString(() => v.HudTextColor, x => v.HudTextColor = x, value),
                "hud.overlay.text.color" => SetString(() => v.HudTextColor, x => v.HudTextColor = x, value),
                "hud.overlay.hud.font.size" => SetInt(() => v.HudFontSize, x => v.HudFontSize = x, value),
                "hud.overlay.hud.font.type" => SetString(() => v.HudFontType, x => v.HudFontType = x, value),
                "hud.overlay.hud.transparency" => SetInt(() => v.HudTransparency, x => v.HudTransparency = x, value),
                "performance.fps.limit" => SetInt(() => v.FpsLimit, x => v.FpsLimit = x, value),
                "performance.anti.aliasing" => SetBool(() => v.AntiAliasing, x => v.AntiAliasing = x, value),
                "performance.log.level" => SetString(() => v.LogLevel, x => v.LogLevel = x, value, "Debug", "Info", "Warn", "Error"),
                "performance.error.behavior" => SetString(() => v.ErrorBehavior, x => v.ErrorBehavior = x, value, "Continue", "Stop"),
                "drawing.tools.show.labels" => SetBool(() => v.DrawingShowLabels, x => v.DrawingShowLabels = x, value),
                _ => false
            };
        }
        catch { return false; }
    }

    private static bool SetBool(Func<bool> get, Action<bool> set, object? value)
    {
        bool b;
        if (value is bool x) b = x;
        else if (value is string s && bool.TryParse(s, out b)) { }
        else return false;
        if (get() == b) return false;
        set(b);
        return true;
    }

    private static bool SetInt(Func<int> get, Action<int> set, object? value)
    {
        int n;
        if (value is int i) n = i;
        else if (value is long l) n = (int)l;
        else if (value is double d) n = (int)d;
        else if (value is string s && int.TryParse(s, out n)) { }
        else return false;
        if (get() == n) return false;
        set(n);
        return true;
    }

    private static bool SetDouble(Func<double> get, Action<double> set, object? value)
    {
        double n;
        if (value is double d) n = d;
        else if (value is int i) n = i;
        else if (value is float f) n = f;
        else if (value is string s && double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out n)) { }
        else return false;
        if (Math.Abs(get() - n) < 1e-12) return false;
        set(n);
        return true;
    }

    private static bool SetString(Func<string> get, Action<string> set, object? value, params string[] allowed)
    {
        if (value is not string s) return false;
        if (allowed.Length > 0 && !allowed.Contains(s, StringComparer.OrdinalIgnoreCase))
            return false;
        if (string.Equals(get(), s, StringComparison.Ordinal)) return false;
        set(s);
        return true;
    }
}
