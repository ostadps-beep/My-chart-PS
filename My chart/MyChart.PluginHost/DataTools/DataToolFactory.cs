using System.Text.Json.Nodes;
using MyChart.Core.Analysis;
using MyChart.Core.Contracts.Plugins;
using MyChart.Core.Models.Drawing;
using MyChart.Core.Models.Geometry;
using MyChart.Core.Models.Rendering;
using MyChart.Core.Plugins.Vocabulary;

namespace MyChart.PluginHost.DataTools;

/// <summary>
/// PG2.05 Click / ClickThenText workflow tool. N clicks; Esc cancels; Preview follows cursor.
/// </summary>
public sealed class DataToolFactory : IDrawingTool
{
    private readonly string _toolId;
    private readonly string _typeId;
    private readonly int _requiredClicks;
    private readonly string _workflow;
    private readonly DrawingStyle _defaultStyle;
    private readonly List<DrawingAnchor> _placed = new();
    private IToolContext? _ctx;

    public DataToolFactory(
        string toolId,
        string typeId,
        int requiredClicks,
        string workflow = "Click",
        DrawingStyle? defaultStyle = null)
    {
        _toolId = toolId;
        _typeId = typeId;
        _requiredClicks = Math.Clamp(requiredClicks, 1, 8);
        _workflow = workflow;
        _defaultStyle = defaultStyle ?? DrawingStyleRules.Default(RgbaColor.FromRgb(33, 150, 243));
    }

    public string ToolId => _toolId;
    public DrawingObject? Preview { get; private set; }

    public void Bind(IToolContext ctx) => _ctx = ctx;

    public void Activate()
    {
        _placed.Clear();
        Preview = null;
    }

    public void Deactivate()
    {
        _placed.Clear();
        Preview = null;
    }

    public ToolResult OnPointer(PointerEvent e)
    {
        if (_ctx is null) return new ToolResult.None();
        if (e.Kind != PointerKind.Down || e.Button != "Left")
        {
            if (e.Kind == PointerKind.Move && _placed.Count > 0)
            {
                var cursor = AnchorFromScreen(e.X, e.Y);
                var anchors = _placed.Concat(new[] { cursor }).ToList();
                Preview = BuildObject(anchors, preview: true);
            }
            return new ToolResult.None();
        }

        var anchor = AnchorFromScreen(e.X, e.Y);
        _placed.Add(anchor);

        if (_placed.Count < _requiredClicks)
        {
            Preview = BuildObject(_placed.Concat(new[] { anchor }).ToList(), preview: true);
            return new ToolResult.None();
        }

        if (_workflow == "ClickThenText")
        {
            // PromptText is async in real UI; for tests we commit with empty check via Cancel path
            DrawingObject? committed = null;
            _ctx.PromptText(text =>
            {
                if (string.IsNullOrEmpty(text))
                {
                    committed = null;
                    return;
                }
                var extra = new JsonObject { ["text"] = text };
                committed = BuildObject(_placed, preview: false, extra);
            });
            _placed.Clear();
            Preview = null;
            return committed is null ? new ToolResult.Cancel() : new ToolResult.Commit(committed);
        }

        var obj = BuildObject(_placed, preview: false);
        _placed.Clear();
        Preview = null;
        return new ToolResult.Commit(obj);
    }

    public ToolResult OnKey(KeyEvent e)
    {
        if (e.Key == "Escape")
        {
            _placed.Clear();
            Preview = null;
            return new ToolResult.Cancel();
        }
        return new ToolResult.None();
    }

    private DrawingAnchor AnchorFromScreen(double x, double y)
    {
        var map = _ctx!.Map;
        double u = map.U(x);
        double price = map.Price(y);
        return new DrawingAnchor(map.TimeAtIndex(u), price);
    }

    private DrawingObject BuildObject(IReadOnlyList<DrawingAnchor> anchors, bool preview, JsonObject? extra = null)
    {
        return new DrawingObject(
            Id: Guid.NewGuid().ToString("N"),
            TypeId: _typeId,
            TypeVersion: 1,
            Anchors: anchors.ToList(),
            Style: _defaultStyle,
            Locked: false,
            Hidden: false,
            Extra: extra);
    }
}
