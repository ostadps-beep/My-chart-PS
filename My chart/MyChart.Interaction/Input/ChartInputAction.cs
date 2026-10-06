namespace MyChart.Interaction.Input;

/// <summary>Actions produced by the input router (consumed by UI / host).</summary>
public enum ChartInputAction
{
    None,
    Zoom,
    PrecisionZoom,
    PriceZoom,
    Pan,
    TemporaryPan,
    Select,
    MultiSelect,
    ManualPriceScale,
    FitAuto,
    ContextMenu,
    CancelTool,
    FinishDrawing,
    GoToLatest,
    LatestMarketPosition,
    NavigationDialog,
    OpenProperties
}
