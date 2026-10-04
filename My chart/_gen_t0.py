# Temporary generator; deleted after T0 skeleton. Not part of the product tree.
from pathlib import Path

root = Path(r"C:\Users\OstadPS\Desktop\MyProject.P\MyProject.1\My chart")

def write(rel: str, content: str) -> None:
    path = root / rel
    path.parent.mkdir(parents=True, exist_ok=True)
    if not content.endswith("\n"):
        content += "\n"
    path.write_text(content, encoding="utf-8", newline="\r\n")

def stub_class(ns: str, name: str) -> str:
    return f"namespace {ns};\n\npublic sealed class {name}\n{{\n}}\n"

def stub_static(ns: str, name: str) -> str:
    return f"namespace {ns};\n\npublic static class {name}\n{{\n}}\n"

def stub_iface(ns: str, name: str, body: str = "") -> str:
    inner = body if body else ""
    return f"namespace {ns};\n\npublic interface {name}\n{{\n{inner}}}\n"

# --- delete Class1 ---
for p in root.rglob("Class1.cs"):
    p.unlink()

# Core geometry / primitives needed by contracts
write("MyChart.Core/Models/Geometry/PointD.cs", """namespace MyChart.Core.Models.Geometry;

public readonly record struct PointD(double X, double Y);
""")
write("MyChart.Core/Models/Geometry/SizeD.cs", """namespace MyChart.Core.Models.Geometry;

public readonly record struct SizeD(double Width, double Height);
""")
write("MyChart.Core/Models/Geometry/RectD.cs", """namespace MyChart.Core.Models.Geometry;

public readonly record struct RectD(double X, double Y, double Width, double Height);
""")
write("MyChart.Core/Models/Rendering/RgbaColor.cs", """namespace MyChart.Core.Models.Rendering;

public readonly record struct RgbaColor(uint Argb);
""")
write("MyChart.Core/Models/Rendering/TextStyle.cs", """namespace MyChart.Core.Models.Rendering;

public sealed record TextStyle(string FontFamily, double SizeDip, bool Bold = false);
""")
write("MyChart.Core/Models/Rendering/TextAlign.cs", """namespace MyChart.Core.Models.Rendering;

public enum TextAlign
{
    Left,
    Center,
    Right
}
""")
write("MyChart.Core/Models/Rendering/ThemeTokens.cs", """namespace MyChart.Core.Models.Rendering;

public sealed class ThemeTokens
{
}
""")
write("MyChart.Core/Models/Rendering/RenderLayer.cs", """namespace MyChart.Core.Models.Rendering;

public enum RenderLayer
{
    Background = 1,
    Grid = 2,
    Candle = 3,
    Indicator = 4,
    Drawing = 5,
    Selection = 6,
    Crosshair = 7,
    Hud = 8,
    Tooltip = 9,
    Overlay = 10
}
""")

# Market stubs (filled in T1.01)
write("MyChart.Core/Models/Market/Candle.cs", """namespace MyChart.Core.Models.Market;

public readonly record struct Candle(
    DateTimeOffset Timestamp,
    double Open,
    double High,
    double Low,
    double Close,
    double Volume)
{
    public bool IsBullish => Close >= Open;
    public bool IsBearish => Close < Open;
}
""")
write("MyChart.Core/Models/Market/Tick.cs", """namespace MyChart.Core.Models.Market;

public readonly record struct Tick(DateTimeOffset Timestamp, double Bid, double Ask, double Volume);
""")
write("MyChart.Core/Models/Market/Timeframe.cs", """namespace MyChart.Core.Models.Market;

public enum Timeframe
{
    Tick,
    M1,
    M5,
    M15,
    M30,
    H1,
    H4,
    D1,
    W1,
    MN1
}
""")
write("MyChart.Core/Models/Market/SymbolGroup.cs", """namespace MyChart.Core.Models.Market;

public enum SymbolGroup
{
    Forex,
    Crypto,
    Indices,
    Stocks,
    Commodities
}
""")
write("MyChart.Core/Models/Market/SymbolInfo.cs", """namespace MyChart.Core.Models.Market;

public sealed record SymbolInfo(string Name, string ProviderName, SymbolGroup Group, int Digits);
""")
write("MyChart.Core/Models/Market/GapKind.cs", """namespace MyChart.Core.Models.Market;

public enum GapKind
{
    Missing,
    Weekend,
    Holiday,
    Unrepairable
}
""")
write("MyChart.Core/Models/Market/GapInfo.cs", """namespace MyChart.Core.Models.Market;

public sealed record GapInfo(DateTimeOffset From, DateTimeOffset To, GapKind Kind);
""")
write("MyChart.Core/Models/Market/ValidationReport.cs", """namespace MyChart.Core.Models.Market;

public sealed class ValidationReport
{
}
""")

write("MyChart.Core/Models/Chart/ChartType.cs", """namespace MyChart.Core.Models.Chart;

public enum ChartType
{
    Candles,
    HollowCandles,
    Ohlc
}
""")
write("MyChart.Core/Models/Chart/CrosshairState.cs", """namespace MyChart.Core.Models.Chart;

public sealed class CrosshairState
{
}
""")
write("MyChart.Core/Models/Chart/HudState.cs", """namespace MyChart.Core.Models.Chart;

public sealed class HudState
{
}
""")

write("MyChart.Core/Models/Viewport/ViewState.cs", """namespace MyChart.Core.Models.Viewport;

public sealed class ViewState
{
}
""")
write("MyChart.Core/Models/Viewport/PriceScaleState.cs", """namespace MyChart.Core.Models.Viewport;

public sealed class PriceScaleState
{
}
""")
write("MyChart.Core/Models/Viewport/ScaleFit.cs", """namespace MyChart.Core.Models.Viewport;

public enum ScaleFit
{
    Auto,
    Manual
}
""")
write("MyChart.Core/Models/Viewport/ScaleTransform.cs", """namespace MyChart.Core.Models.Viewport;

public enum ScaleTransform
{
    Linear,
    Log,
    Percentage
}
""")
write("MyChart.Core/Models/Viewport/RenderWindowRange.cs", """namespace MyChart.Core.Models.Viewport;

public sealed record RenderWindowRange(int From, int To, int RenderFrom, int RenderTo);
""")

write("MyChart.Core/Models/Indicator/IndicatorParameter.cs", """namespace MyChart.Core.Models.Indicator;

public sealed class IndicatorParameter
{
}
""")
write("MyChart.Core/Models/Indicator/IndicatorOutput.cs", """namespace MyChart.Core.Models.Indicator;

public sealed class IndicatorOutput
{
}
""")
write("MyChart.Core/Models/Workspace/WorkspaceModel.cs", """namespace MyChart.Core.Models.Workspace;

public sealed class WorkspaceModel
{
}
""")
write("MyChart.Core/Models/Workspace/ChartLayoutModel.cs", """namespace MyChart.Core.Models.Workspace;

public sealed class ChartLayoutModel
{
}
""")

# Settings at T0.07
write("MyChart.Core/Models/Settings/ChartSettingValues.cs", """namespace MyChart.Core.Models.Settings;

public sealed class ChartSettingValues
{
    public bool ChartOffline { get; set; }
    public bool ChartShift { get; set; } = true;
    public bool ChartAutoscroll { get; set; } = true;
    public bool ChartScaleFix { get; set; }
    public double ChartScaleFixedMinimum { get; set; }
    public double ChartScaleFixedMaximum { get; set; }
    public string ChartZoomBehavior { get; set; } = "Both";
    public string ChartMouseWheel { get; set; } = "Zoom";
    public int ChartZoomSpeed { get; set; } = 50;
    public int ChartScrollSpeed { get; set; } = 50;
    public int ChartVisibleCandles { get; set; } = 500;
    public bool GridBackgroundShowGrid { get; set; } = true;
    public string GridBackgroundHorizontalColor { get; set; } = "#2A2E39";
    public string GridBackgroundGridStyle { get; set; } = "Solid";
    public bool GridBackgroundShowHorizontal { get; set; } = true;
    public bool AxesShowVerticalGrid { get; set; } = true;
    public int GridBackgroundGridTransparency { get; set; } = 40;
    public string ChartDisplayMode { get; set; } = "Candlesticks";
    public bool ChartShowOhlc { get; set; } = true;
    public string AxesPricePosition { get; set; } = "Right";
    public bool AxesShowLastPrice { get; set; } = true;
    public string AxesTimePosition { get; set; } = "Bottom";
    public string AxesTimeFormat { get; set; } = "HH:mm";
    public string AxesAxisColor { get; set; } = "#888888";
    public int AxesAxisThickness { get; set; } = 1;
    public string CandlesType { get; set; } = "Candlestick";
    public string CandlesBullColor { get; set; } = "#26A69A";
    public string CandlesBearColor { get; set; } = "#EF5350";
    public string CandlesWickColor { get; set; } = "#CCCCCC";
    public string CandlesBorderColor { get; set; } = "#1A1A1A";
    public bool CandlesShowBody { get; set; } = true;
    public int CandlesBodyThickness { get; set; } = 1;
    public int CandlesSpacing { get; set; } = 2;
    public bool CandlesShowWicks { get; set; } = true;
    public int CandlesWickThickness { get; set; } = 1;
    public string GridBackgroundBackgroundColor { get; set; } = "#131722";
    public string DrawingToolsToolColor { get; set; } = "#2196F3";
    public int DrawingToolsToolThickness { get; set; } = 1;
    public string DrawingToolsToolStyle { get; set; } = "Solid";
    public int DrawingToolsToolTransparency { get; set; }
    public bool DrawingToolsShowLabels { get; set; } = true;
    public string DrawingToolsSnapMode { get; set; } = "Candle";
    public string DrawingToolsSelectionMode { get; set; } = "Single";
    public string DrawingToolsDragBehavior { get; set; } = "Free";
    public IReadOnlyList<string> HudOverlayHudItems { get; set; } = ["OHLC", "Change", "Volume"];
    public string HudOverlayHudPosition { get; set; } = "TopLeft";
    public string HudOverlayFixedFollow { get; set; } = "Fixed";
    public string HudOverlayTextColor { get; set; } = "#FFFFFF";
    public int HudOverlayFontSize { get; set; } = 12;
    public string HudOverlayFontType { get; set; } = "Segoe UI";
    public int HudOverlayHudTransparency { get; set; }
    public int PerformanceFpsLimit { get; set; } = 60;
    public bool PerformanceAntiAliasing { get; set; } = true;
    public string PerformanceLogLevel { get; set; } = "Info";
    public string PerformanceErrorBehavior { get; set; } = "Continue";
    public int AdvancedCacheSize { get; set; } = 256;
}
""")
write("MyChart.Core/Models/Settings/DefaultChartSettings.cs", """namespace MyChart.Core.Models.Settings;

public sealed class DefaultChartSettings : MyChart.Core.Contracts.Services.IChartSettings
{
    public ChartSettingValues Values { get; } = new();

    public event Action<IReadOnlyList<string>>? Changed;

    public static DefaultChartSettings Create() => new();
}
""")

# Candles L2 stubs
for name in [
    "SessionCalendar", "ServerTimeRule", "CalendarBuckets", "FormingCandle",
    "CandleFold", "CandleAggregator"
]:
    write(f"MyChart.Core/Candles/{name}.cs", stub_class("MyChart.Core.Candles", name) if name != "ServerTimeRule" else """namespace MyChart.Core.Candles;

public abstract record ServerTimeRule
{
    public sealed record Fixed(TimeSpan Offset) : ServerTimeRule;

    public sealed record EetUsDst : ServerTimeRule;

    public static ServerTimeRule Utc { get; } = new Fixed(TimeSpan.Zero);
}
""")

write("MyChart.Core/Data/Validation/CandleValidator.cs", stub_static("MyChart.Core.Data.Validation", "CandleValidator"))
write("MyChart.Core/Data/Validation/TickValidator.cs", stub_static("MyChart.Core.Data.Validation", "TickValidator"))
write("MyChart.Core/Data/SampleDataProvider.cs", stub_class("MyChart.Core.Data", "SampleDataProvider"))

for name in [
    "ChartConstants", "TimeIndexMapper", "CoordinateConverter", "RenderWindow",
    "ZoomEngine", "PanEngine", "ScrollLimits", "Viewport", "StartupView", "ChartSession"
]:
    kind = stub_static if name == "ChartConstants" else stub_class
    write(f"MyChart.Core/ChartEngine/{name}.cs", kind("MyChart.Core.ChartEngine", name))

for name in ["PriceScaleEngine", "PriceTransform", "NiceTicks"]:
    write(f"MyChart.Core/ChartEngine/PriceScale/{name}.cs", stub_class("MyChart.Core.ChartEngine.PriceScale", name))

for name in ["PriceAxisModel", "TimeAxisModel", "TimeLabelPlanner", "LabelCollision"]:
    write(f"MyChart.Core/ChartEngine/Axis/{name}.cs", stub_class("MyChart.Core.ChartEngine.Axis", name))

for name in ["SymbolMath", "CrosshairCalculator", "MeasureCalculator", "HudDataProvider", "NavigatorCalculator"]:
    write(f"MyChart.Core/Analysis/{name}.cs", stub_class("MyChart.Core.Analysis", name))

write("MyChart.Core/Analysis/DrawingMath.cs", """namespace MyChart.Core.Analysis;

public static class DrawingMath
{
    public static double DistancePointToSegment(double px, double py, double x1, double y1, double x2, double y2)
    {
        var dx = x2 - x1;
        var dy = y2 - y1;
        var lengthSquared = (dx * dx) + (dy * dy);
        if (lengthSquared == 0)
        {
            return Hypot(px - x1, py - y1);
        }

        var t = ((px - x1) * dx + (py - y1) * dy) / lengthSquared;
        t = Math.Clamp(t, 0, 1);
        var projX = x1 + (t * dx);
        var projY = y1 + (t * dy);
        return Hypot(px - projX, py - projY);
    }

    public static double DistancePointToRectEdges(double px, double py, double left, double top, double right, double bottom)
    {
        var d1 = DistancePointToSegment(px, py, left, top, right, top);
        var d2 = DistancePointToSegment(px, py, right, top, right, bottom);
        var d3 = DistancePointToSegment(px, py, right, bottom, left, bottom);
        var d4 = DistancePointToSegment(px, py, left, bottom, left, top);
        return Math.Min(Math.Min(d1, d2), Math.Min(d3, d4));
    }

    public static bool PointInRect(double px, double py, double left, double top, double right, double bottom)
    {
        return px >= Math.Min(left, right)
            && px <= Math.Max(left, right)
            && py >= Math.Min(top, bottom)
            && py <= Math.Max(top, bottom);
    }

    public static double DistancePointToEllipseEdge(double px, double py, double left, double top, double right, double bottom)
    {
        var cx = (left + right) / 2.0;
        var cy = (top + bottom) / 2.0;
        var rx = Math.Abs(right - left) / 2.0;
        var ry = Math.Abs(bottom - top) / 2.0;
        if (rx == 0 || ry == 0)
        {
            return Hypot(px - cx, py - cy);
        }

        var nx = (px - cx) / rx;
        var ny = (py - cy) / ry;
        var mag = Hypot(nx, ny);
        if (mag == 0)
        {
            return Math.Min(rx, ry);
        }

        var ex = cx + (nx / mag * rx);
        var ey = cy + (ny / mag * ry);
        return Hypot(px - ex, py - ey);
    }

    private static double Hypot(double a, double b) => Math.Sqrt((a * a) + (b * b));
}
""")

for name in ["CandleGeometry", "CandleGeometryCalculator", "ColumnAggregator"]:
    write(f"MyChart.Core/Rendering/Geometry/{name}.cs", stub_class("MyChart.Core.Rendering.Geometry", name))

for name in [
    "BackgroundRenderer", "GridRenderer", "PriceAxisRenderer", "TimeAxisRenderer",
    "CandleRenderer", "CurrentPriceMarkerRenderer", "DualMarkerRenderer",
    "CrosshairRenderer", "HudRenderer", "DrawingRenderer", "RenderPipeline"
]:
    write(f"MyChart.Core/Rendering/{name}.cs", stub_class("MyChart.Core.Rendering", name))

write("MyChart.Core/Commands/IChartCommand.cs", """namespace MyChart.Core.Commands;

public interface IChartCommand
{
    void Execute();
    void Undo();
}
""")
write("MyChart.Core/Commands/ICommandBus.cs", """namespace MyChart.Core.Commands;

public interface ICommandBus
{
    void Execute(IChartCommand command);
}
""")
write("MyChart.Core/Commands/CommandBus.cs", stub_class("MyChart.Core.Commands", "CommandBus"))
write("MyChart.Core/Commands/CommandHistory.cs", stub_class("MyChart.Core.Commands", "CommandHistory"))
write("MyChart.Core/Commands/AddDrawingCommand.cs", stub_class("MyChart.Core.Commands", "AddDrawingCommand"))
write("MyChart.Core/Commands/RemoveDrawingsCommand.cs", stub_class("MyChart.Core.Commands", "RemoveDrawingsCommand"))
write("MyChart.Core/Commands/MoveDrawingCommand.cs", stub_class("MyChart.Core.Commands", "MoveDrawingCommand"))
write("MyChart.Core/Commands/EditDrawingCommand.cs", stub_class("MyChart.Core.Commands", "EditDrawingCommand"))

write("MyChart.Core/Serialization/SerializationCategory.cs", """namespace MyChart.Core.Serialization;

public enum SerializationCategory
{
    Workspace,
    Drawings,
    Viewport,
    Indicators
}
""")
write("MyChart.Core/Serialization/SerializationException.cs", """namespace MyChart.Core.Serialization;

public sealed class SerializationException : Exception
{
    public SerializationException(string message) : base(message)
    {
    }
}
""")

write("MyChart.Core/Events/.gitkeep", "")
write("MyChart.Core/State/.gitkeep", "")

# Contracts T0.04 (plugin contracts later T0.08)
write("MyChart.Core/Contracts/Data/IDataProvider.cs", """using MyChart.Core.Candles;
using MyChart.Core.Models.Market;

namespace MyChart.Core.Contracts.Data;

public interface IDataProvider
{
    Task ConnectAsync(CancellationToken cancellationToken);
    Task DisconnectAsync();
    Task<IReadOnlyList<Candle>> GetHistoryAsync(string symbol, Timeframe tf, DateTimeOffset? from, DateTimeOffset? to, int maxBars, CancellationToken cancellationToken);
    IDisposable SubscribeTicks(string symbol, Action<Tick> onTick);
    void UnsubscribeTicks(string symbol);
    Task<IReadOnlyList<SymbolInfo>> GetSymbolsAsync(CancellationToken cancellationToken);
    IReadOnlyList<Timeframe> GetTimeframes();
    bool IsConnected { get; }
    string Name { get; }
    bool SupportsNativeHigherTimeframes { get; }
    ServerTimeRule ServerTimeRule { get; }
}
""")
write("MyChart.Core/Contracts/Data/ISeriesView.cs", """using MyChart.Core.Models.Market;

namespace MyChart.Core.Contracts.Data;

public interface ISeriesView
{
    int Count { get; }
    Candle this[int index] { get; }
    bool HasForming { get; }
    DateTimeOffset OpenTime(int index);
}
""")
write("MyChart.Core/Contracts/Data/IMarketDataBus.cs", """namespace MyChart.Core.Contracts.Data;

public interface IMarketDataBus
{
    IDisposable Subscribe<T>(Action<T> handler);
    void Publish<T>(T evt);
}
""")
write("MyChart.Core/Contracts/Data/IAggregationEngine.cs", """using MyChart.Core.Models.Market;

namespace MyChart.Core.Contracts.Data;

public interface IAggregationEngine
{
    void SetBaseHistory(string symbol, IReadOnlyList<Candle> candles);
    void OnTick(string symbol, Tick tick);
    ISeriesView GetSeries(string symbol, Timeframe tf);
}
""")
write("MyChart.Core/Contracts/Data/ISymbolManager.cs", """using MyChart.Core.Models.Market;

namespace MyChart.Core.Contracts.Data;

public interface ISymbolManager
{
    void Register(SymbolInfo symbol);
    IReadOnlyList<SymbolInfo> Search(string query);
    IReadOnlyList<SymbolInfo> Favorites { get; }
}
""")
write("MyChart.Core/Contracts/Data/ITimeframeManager.cs", """using MyChart.Core.Models.Market;

namespace MyChart.Core.Contracts.Data;

public interface ITimeframeManager
{
    IReadOnlyList<Timeframe> GetTimeframes();
}
""")
write("MyChart.Core/Contracts/Data/IDataProviderManager.cs", """namespace MyChart.Core.Contracts.Data;

public interface IDataProviderManager
{
    void Load(IDataProvider provider);
    void Switch(string providerName);
    Task ReconnectAsync(CancellationToken cancellationToken);
}
""")
write("MyChart.Core/Contracts/Data/IDataCacheManager.cs", """using MyChart.Core.Models.Market;

namespace MyChart.Core.Contracts.Data;

public interface IDataCacheManager
{
    IReadOnlyList<Candle>? Get(string symbol, Timeframe timeframe);
}
""")
write("MyChart.Core/Contracts/Data/IHistoryManager.cs", """namespace MyChart.Core.Contracts.Data;

public interface IHistoryManager
{
    Task DownloadAsync(string symbol, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken);
    Task UpdateAsync(string symbol, CancellationToken cancellationToken);
}
""")
write("MyChart.Core/Contracts/Data/ITickManager.cs", """using MyChart.Core.Models.Market;

namespace MyChart.Core.Contracts.Data;

public interface ITickManager
{
    void Receive(string symbol, Tick tick);
}
""")
write("MyChart.Core/Contracts/Data/IDataStorage.cs", """using MyChart.Core.Models.Market;

namespace MyChart.Core.Contracts.Data;

public interface IDataStorage
{
    Task SaveCandlesAsync(string symbol, Timeframe timeframe, IReadOnlyList<Candle> candles, CancellationToken cancellationToken);
    Task<IReadOnlyList<Candle>> LoadCandlesAsync(string symbol, Timeframe timeframe, CancellationToken cancellationToken);
}
""")

write("MyChart.Core/Contracts/Rendering/IRenderContext.cs", """using MyChart.Core.Models.Geometry;
using MyChart.Core.Models.Rendering;

namespace MyChart.Core.Contracts.Rendering;

public interface IRenderContext
{
    int Width { get; }
    int Height { get; }
    double DpiScale { get; }
    void Clear(RgbaColor color);
    void FillRect(int x, int y, int w, int h, RgbaColor color);
    void StrokeRect(int x, int y, int w, int h, int thickness, RgbaColor color);
    void DrawLine(double x1, double y1, double x2, double y2, RgbaColor color, double width, double[]? dash);
    void DrawText(string text, double x, double y, TextStyle style, RgbaColor color, TextAlign align);
    void DrawPath(IReadOnlyList<PointD> points, RgbaColor color, double width, bool closed, bool fill);
    SizeD MeasureText(string text, TextStyle style);
    void PushClip(RectD rect);
    void PopClip();
}
""")
write("MyChart.Core/Contracts/Rendering/IChartRenderer.cs", stub_iface("MyChart.Core.Contracts.Rendering", "IChartRenderer"))
write("MyChart.Core/Contracts/Rendering/ILayerRenderer.cs", stub_iface("MyChart.Core.Contracts.Rendering", "ILayerRenderer"))
write("MyChart.Core/Contracts/Rendering/ICandleRenderer.cs", stub_iface("MyChart.Core.Contracts.Rendering", "ICandleRenderer"))
write("MyChart.Core/Contracts/Rendering/IAxisRenderer.cs", stub_iface("MyChart.Core.Contracts.Rendering", "IAxisRenderer"))

write("MyChart.Core/Contracts/Interaction/IPointerInput.cs", stub_iface("MyChart.Core.Contracts.Interaction", "IPointerInput"))
write("MyChart.Core/Contracts/Interaction/IChartInputHandler.cs", stub_iface("MyChart.Core.Contracts.Interaction", "IChartInputHandler"))

write("MyChart.Core/Contracts/Indicators/IIndicator.cs", stub_iface("MyChart.Core.Contracts.Indicators", "IIndicator"))
write("MyChart.Core/Contracts/UI/IThemeService.cs", stub_iface("MyChart.Core.Contracts.UI", "IThemeService"))
write("MyChart.Core/Contracts/UI/IIconProvider.cs", stub_iface("MyChart.Core.Contracts.UI", "IIconProvider"))

write("MyChart.Core/Contracts/Services/IClock.cs", """namespace MyChart.Core.Contracts.Services;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
""")
write("MyChart.Core/Contracts/Services/ILogService.cs", """namespace MyChart.Core.Contracts.Services;

public interface ILogService
{
    void Log(string level, string message);
}
""")
write("MyChart.Core/Contracts/Services/IStateSerializer.cs", """namespace MyChart.Core.Contracts.Services;

public interface IStateSerializer
{
    string Serialize<T>(T value);
    T Deserialize<T>(string json);
}
""")
write("MyChart.Core/Contracts/Services/IChartSettings.cs", """using MyChart.Core.Models.Settings;

namespace MyChart.Core.Contracts.Services;

public interface IChartSettings
{
    ChartSettingValues Values { get; }
    event Action<IReadOnlyList<string>>? Changed;
}
""")

print("core contracts written")
""")
