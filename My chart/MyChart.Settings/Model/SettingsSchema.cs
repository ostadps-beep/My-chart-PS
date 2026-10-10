namespace MyChart.Settings.Model;

/// <summary>
/// Single source of truth for chart.settings: keys, UI grouping, controls,
/// defaultValue, ranges/options, Immediate/OnApply, and dependencies.
/// Button behavior is not defined here.
/// </summary>
public static class SettingsSchema
{
    public const string RootName = "chart.settings";

    public static readonly IReadOnlyList<string> JsonSections =
    [
        "advanced.python.bridge",
        "analytical.modules",
        "drawing.tools",
        "grid.background",
        "hud.overlay",
        "performance",
        "workspace",
        "advanced",
        "candles",
        "chart",
        "axes"
    ];

    public static readonly IReadOnlyList<CategoryDefinition> Categories =
    [
        new()
        {
            Id = "chart",
            Name = "Chart",
            Title = "Chart Settings",
            Description = "Configure chart appearance and behavior."
        },
        new()
        {
            Id = "axes",
            Name = "Axes",
            Title = "Axes Settings",
            Description = "Configure price and time axes."
        },
        new()
        {
            Id = "candles",
            Name = "Candles",
            Title = "Candle Settings",
            Description = "Configure candle type, colors, appearance, and background."
        },
        new()
        {
            Id = "drawing",
            Name = "Drawing Tools",
            Title = "Drawing Tools",
            Description = "Configure tool style, snap, and interaction."
        },
        new()
        {
            Id = "modules",
            Name = "Analytical Modules",
            Title = "Analytical Modules",
            Description = "Configure module inputs, display, and colors."
        },
        new()
        {
            Id = "hud",
            Name = "HUD & Overlay",
            Title = "HUD & Overlay",
            Description = "Configure overlay items, position, and typography."
        },
        new()
        {
            Id = "performance",
            Name = "Performance",
            Title = "Performance",
            Description = "Configure frame rate, GPU, buffer, and logging."
        },
        new()
        {
            Id = "workspace",
            Name = "Workspace",
            Title = "Workspace",
            Description = "Save, load, export, and reset layout independently of Reset."
        },
        new()
        {
            Id = "advanced",
            Name = "Advanced",
            Title = "Advanced",
            Description = "Configure engine, cache, backup, and Python bridge endpoints."
        }
    ];

    public static readonly IReadOnlyList<FieldDefinition> Fields = BuildFields();

    public static IReadOnlyDictionary<string, FieldDefinition> FieldsByKey { get; } =
        Fields.ToDictionary(f => f.Key, StringComparer.Ordinal);

    public static CategoryDefinition Category(string id) =>
        Categories.First(c => c.Id == id);

    public static IEnumerable<FieldDefinition> FieldsInCategory(string categoryId) =>
        Fields.Where(f => f.CategoryId == categoryId);

    public static bool TrySplitKey(string key, out string section, out string leaf)
    {
        foreach (var prefix in JsonSections)
        {
            if (key.Equals(prefix, StringComparison.Ordinal))
            {
                section = prefix;
                leaf = "";
                return true;
            }

            if (key.StartsWith(prefix + ".", StringComparison.Ordinal))
            {
                section = prefix;
                leaf = key[(prefix.Length + 1)..];
                return true;
            }
        }

        section = "";
        leaf = key;
        return false;
    }

    private static IReadOnlyList<FieldDefinition> BuildFields() =>
    [
        F("chart.offline", "chart", "Chart Behavior", "Offline Chart", ControlKind.Toggle, false),
        F("chart.on.foreground", "chart", "Chart Behavior", "Chart on Foreground", ControlKind.Toggle, false),
        F("chart.shift", "chart", "Chart Behavior", "Chart Shift", ControlKind.Toggle, true),
        F("chart.autoscroll", "chart", "Chart Behavior", "Chart Autoscroll", ControlKind.Toggle, true),

        F("chart.scale.fix.one.to.one", "chart", "Scale", "Scale Fix One to One", ControlKind.Toggle, false),
        F("chart.scale.fix", "chart", "Scale", "Scale Fix", ControlKind.Toggle, false),
        F("chart.scale.fixed.minimum", "chart", "Scale", "Fixed Minimum", ControlKind.Numeric, 0,
            min: 0, max: 1_000_000_000, enableWhen: "chart.scale.fix"),
        F("chart.scale.fixed.maximum", "chart", "Scale", "Fixed Maximum", ControlKind.Numeric, 0,
            min: 0, max: 1_000_000_000, enableWhen: "chart.scale.fix"),

        F("chart.zoom.behavior", "chart", "Navigation / Zoom", "Zoom Behavior", ControlKind.Dropdown, "Both",
            options: ["Time", "Price", "Both"]),
        F("chart.mouse.wheel", "chart", "Navigation / Zoom", "Mouse Wheel", ControlKind.Dropdown, "Zoom",
            options: ["Zoom", "Both", "Pan"]),
        F("chart.zoom.speed", "chart", "Navigation / Zoom", "Zoom Speed", ControlKind.Slider, 50, min: 0, max: 100),
        F("chart.scroll.speed", "chart", "Navigation / Zoom", "Scroll Speed", ControlKind.Slider, 50, min: 0, max: 100),
        F("chart.visible.candles", "chart", "Navigation / Zoom", "Visible Candles", ControlKind.Numeric, 500,
            apply: ApplyMode.OnApply, min: 10, max: 50000),

        F("grid.background.show.grid", "chart", "Grid", "Show Grid", ControlKind.Toggle, true),
        F("grid.background.horizontal.color", "chart", "Grid", "Grid Color", ControlKind.Color, "#2A2E39",
            enableWhen: "grid.background.show.grid"),
        F("grid.background.grid.style", "chart", "Grid", "Grid Line Style", ControlKind.Dropdown, "Solid",
            options: ["Solid", "Dash", "Dot", "DashDot"],
            enableWhen: "grid.background.show.grid"),
        F("grid.background.show.horizontal", "chart", "Grid", "Show Horizontal Grid", ControlKind.Toggle, true,
            enableWhen: "grid.background.show.grid"),
        F("axes.show.vertical.grid", "chart", "Grid", "Show Vertical Grid", ControlKind.Toggle, true,
            enableWhen: "grid.background.show.grid"),
        F("grid.background.grid.transparency", "chart", "Grid", "Grid Transparency", ControlKind.Slider, 40,
            min: 0, max: 100, enableWhen: "grid.background.show.grid"),

        F("chart.display.mode", "chart", "Chart Display", "Chart Display", ControlKind.Dropdown, "Candlesticks",
            options: ["Bar Chart", "Candlesticks", "Hollow Candlesticks", "Line Chart", "Area Chart"]),
        F("chart.show.ohlc", "chart", "Chart Display", "Show OHLC", ControlKind.Toggle, true),
        F("chart.show.ask.line", "chart", "Chart Display", "Show Ask Line", ControlKind.Toggle, false),
        F("chart.show.period.separators", "chart", "Chart Display", "Show Period Separators", ControlKind.Toggle, true),
        F("chart.show.volumes", "chart", "Chart Display", "Show Volumes", ControlKind.Toggle, true),
        F("chart.show.object.descriptions", "chart", "Chart Display", "Show Object Descriptions", ControlKind.Toggle, false),

        F("axes.price.position", "axes", "Price", "Price Position", ControlKind.Dropdown, "Right",
            options: ["Left", "Right", "Both", "Hidden"]),
        F("axes.show.last.price", "axes", "Price", "Show Last Price", ControlKind.Toggle, true),
        F("axes.decimal.count", "axes", "Price", "Decimal Count", ControlKind.Numeric, 2, min: 0, max: 8),
        F("axes.time.position", "axes", "Time", "Time Position", ControlKind.Dropdown, "Bottom",
            options: ["Top", "Bottom", "Hidden"]),
        F("axes.time.format", "axes", "Time", "Time Format", ControlKind.Dropdown, "HH:mm",
            options: ["HH:mm", "HH:mm:ss", "yyyy-MM-dd", "dd MMM HH:mm"]),
        F("axes.axis.color", "axes", "Appearance", "Axis Color", ControlKind.Color, "#888888"),
        F("axes.axis.thickness", "axes", "Appearance", "Axis Thickness", ControlKind.Numeric, 1, min: 1, max: 8),

        F("candles.type", "candles", "Candles", "Candle Type", ControlKind.Dropdown, "Candlestick",
            options: ["Candlestick", "Hollow Candlesticks", "OHLC", "Line", "Area", "Heikin Ashi"]),
        F("candles.bull.color", "candles", "Candles", "Bull Color", ControlKind.Color, "#26A69A"),
        F("candles.bear.color", "candles", "Candles", "Bear Color", ControlKind.Color, "#EF5350"),
        F("candles.wick.color", "candles", "Candles", "Wick Color", ControlKind.Color, "#CCCCCC"),
        F("candles.border.color", "candles", "Candles", "Border Color", ControlKind.Color, "#1A1A1A"),
        F("candles.show.body", "candles", "Candles", "Show Body", ControlKind.Toggle, true),
        F("candles.body.thickness", "candles", "Candles", "Body Thickness", ControlKind.Numeric, 1, min: 1, max: 10),
        F("candles.spacing", "candles", "Candles", "Spacing", ControlKind.Numeric, 2, min: 0, max: 20),
        F("candles.show.wicks", "candles", "Candles", "Show Wicks", ControlKind.Toggle, true),
        F("candles.wick.thickness", "candles", "Candles", "Wick Thickness", ControlKind.Numeric, 1,
            min: 1, max: 10, enableWhen: "candles.show.wicks"),
        // C1.1: was #131722 — must match chart bg #1E1E1E so Apply does not push old colour
        F("grid.background.background.color", "candles", "Background", "Background Color", ControlKind.Color, "#1E1E1E"),
        F("grid.background.gradient.mode", "candles", "Background", "Gradient Mode", ControlKind.Dropdown, "None",
            options: ["None", "Vertical", "Horizontal", "Radial"]),
        F("grid.background.background.transparency", "candles", "Background", "Background Transparency",
            ControlKind.Slider, 0, min: 0, max: 100),

        F("drawing.tools.tool.color", "drawing", "Style", "Tool Color", ControlKind.Color, "#2196F3"),
        F("drawing.tools.tool.thickness", "drawing", "Style", "Tool Thickness", ControlKind.Numeric, 1, min: 1, max: 12),
        F("drawing.tools.tool.style", "drawing", "Style", "Tool Style", ControlKind.Dropdown, "Solid",
            options: ["Solid", "Dash", "Dot", "DashDot"]),
        F("drawing.tools.tool.transparency", "drawing", "Style", "Tool Transparency", ControlKind.Slider, 0,
            min: 0, max: 100),
        F("drawing.tools.show.labels", "drawing", "Style", "Show Labels", ControlKind.Toggle, true),
        F("drawing.tools.snap.mode", "drawing", "Behavior", "Snap Mode", ControlKind.Dropdown, "Candle",
            options: ["None", "Candle", "Price", "Both"]),
        F("drawing.tools.selection.mode", "drawing", "Behavior", "Selection Mode", ControlKind.Dropdown, "Single",
            options: ["Single", "Multi"]),
        F("drawing.tools.drag.behavior", "drawing", "Behavior", "Drag Behavior", ControlKind.Dropdown, "Free",
            options: ["Free", "AxisLock", "Copy"]),
        F("drawing.tools.hotkey", "drawing", "Behavior", "Hotkey", ControlKind.Text, "L"),

        F("analytical.modules.enabled", "modules", "Module", "Enabled", ControlKind.Toggle, true),
        F("analytical.modules.display.type", "modules", "Module", "Display Type", ControlKind.Dropdown, "Line",
            options: ["Line", "Histogram", "Area", "Dots", "Columns"]),
        F("analytical.modules.update.mode", "modules", "Module", "Update Mode", ControlKind.Dropdown, "Realtime",
            options: ["Realtime", "OnClose", "Manual"]),
        F("analytical.modules.input.parameters", "modules", "Module", "Input Parameters", ControlKind.ParameterList,
            DefaultParameters()),
        F("analytical.modules.main.color", "modules", "Appearance", "Main Color", ControlKind.Color, "#FF9800"),
        F("analytical.modules.alert.color", "modules", "Appearance", "Alert Color", ControlKind.Color, "#FFEB3B"),
        F("analytical.modules.area.background", "modules", "Appearance", "Area Background", ControlKind.Color, "#33FF9800",
            enableWhen: "analytical.modules.display.type=Area"),
        F("analytical.modules.transparency", "modules", "Appearance", "Transparency", ControlKind.Slider, 0,
            min: 0, max: 100),
        F("analytical.modules.thickness", "modules", "Appearance", "Thickness", ControlKind.Numeric, 1, min: 1, max: 10),

        F("hud.overlay.hud.items", "hud", "HUD Items", "HUD Items", ControlKind.CheckList,
            DefaultHudItems(),
            options: ["OHLC", "Change", "Volume", "Time", "Spread", "High/Low", "Bid/Ask", "Position"]),
        F("hud.overlay.hud.position", "hud", "Layout", "HUD Position", ControlKind.Dropdown, "TopLeft",
            options: ["TopLeft", "TopRight", "BottomLeft", "BottomRight"],
            enableWhen: "hud.overlay.hud.items?"),
        F("hud.overlay.fixed.follow", "hud", "Layout", "Fixed / Follow", ControlKind.Dropdown, "Fixed",
            options: ["Fixed", "FollowPrice", "FollowCursor"]),
        F("hud.overlay.text.color", "hud", "Typography", "Text Color", ControlKind.Color, "#FFFFFF"),
        F("hud.overlay.font.size", "hud", "Typography", "Font Size", ControlKind.Numeric, 12, min: 8, max: 32),
        F("hud.overlay.font.type", "hud", "Typography", "Font Type", ControlKind.Dropdown, "Segoe UI",
            options: ["Segoe UI", "Consolas", "Tahoma", "Arial"]),
        F("hud.overlay.hud.transparency", "hud", "Typography", "HUD Transparency", ControlKind.Slider, 0,
            min: 0, max: 100),

        F("performance.fps.limit", "performance", "Rendering", "FPS Limit", ControlKind.Numeric, 60,
            apply: ApplyMode.OnApply, min: 15, max: 240),
        F("performance.gpu.acceleration", "performance", "Rendering", "GPU Acceleration", ControlKind.Toggle, true,
            apply: ApplyMode.OnApply),
        F("performance.anti.aliasing", "performance", "Rendering", "Anti Aliasing", ControlKind.Toggle, true,
            apply: ApplyMode.OnApply, enableWhen: "performance.gpu.acceleration"),
        F("performance.buffer.size", "performance", "Rendering", "Buffer Size", ControlKind.Numeric, 2048,
            apply: ApplyMode.OnApply, min: 256, max: 65536, increment: 256),
        F("performance.log.level", "performance", "Diagnostics", "Log Level", ControlKind.Dropdown, "Info",
            options: ["Error", "Warn", "Info", "Debug", "Trace"]),
        F("performance.error.behavior", "performance", "Diagnostics", "Error Behavior", ControlKind.Dropdown, "Continue",
            options: ["Continue", "Pause", "Dialog"]),

        F("workspace.save.layout", "workspace", "Layout", "Save Layout", ControlKind.Command, "",
            hint: "Save the current settings document."),
        F("workspace.load.layout", "workspace", "Layout", "Load Layout", ControlKind.Command, "",
            hint: "Load a settings document from file."),
        F("workspace.reset.layout", "workspace", "Layout", "Reset Layout", ControlKind.Command, "",
            hint: "Restore every setting to its schema defaultValue."),
        F("workspace.export.settings", "workspace", "Files", "Export Settings", ControlKind.Command, "",
            hint: "Export settings to a JSON file."),
        F("workspace.import.settings", "workspace", "Files", "Import Settings", ControlKind.Command, "",
            hint: "Import settings from a JSON file."),

        F("advanced.multi.threading", "advanced", "Engine", "Multi Threading", ControlKind.Toggle, true),
        F("advanced.engine.mode", "advanced", "Engine", "Engine Mode", ControlKind.Dropdown, "Balanced",
            apply: ApplyMode.OnApply, options: ["Performance", "Balanced", "Quality"]),
        F("advanced.cache.size", "advanced", "Engine", "Cache Size", ControlKind.Numeric, 256, min: 16, max: 4096),
        F("advanced.backup.restore", "advanced", "Maintenance", "Backup / Restore", ControlKind.Command, "",
            hint: "Write or restore settings/chart.backup.json."),
        F("advanced.python.bridge.host", "advanced", "Python Bridge", "Host", ControlKind.Text, "127.0.0.1"),
        F("advanced.python.bridge.port", "advanced", "Python Bridge", "Port", ControlKind.Numeric, 8765,
            min: 1, max: 65535),
        F("advanced.python.bridge.timeout", "advanced", "Python Bridge", "Timeout (ms)", ControlKind.Numeric, 3000,
            min: 100, max: 60000, increment: 100)
    ];

    private static List<string> DefaultHudItems() => ["OHLC", "Change", "Volume"];

    private static List<ParameterEntry> DefaultParameters() =>
    [
        new() { Name = "Period", Value = "14" }
    ];

    private static FieldDefinition F(
        string key,
        string categoryId,
        string group,
        string label,
        ControlKind control,
        object defaultValue,
        ApplyMode apply = ApplyMode.Immediate,
        double? min = null,
        double? max = null,
        double increment = 1,
        IReadOnlyList<string>? options = null,
        string? enableWhen = null,
        string? hint = null) =>
        new()
        {
            Key = key,
            CategoryId = categoryId,
            Group = group,
            Label = label,
            Control = control,
            DefaultValue = defaultValue,
            ApplyMode = apply,
            Min = min,
            Max = max,
            Increment = increment,
            Options = options,
            EnableWhen = enableWhen,
            CommandHint = hint
        };
}
