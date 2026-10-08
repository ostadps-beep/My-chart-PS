using MyChart.Core.Models.Drawing;
using MyChart.Core.Models.Geometry;
using MyChart.Core.UI.ToolBuilder;

namespace MyChart.Core.UI.ToolBuilder;

/// <summary>Preview display state (PG6.02).</summary>
public enum LivePreviewState
{
    Normal,
    Selected,
    Preview
}

/// <summary>One sample anchor the user can drag in the preview.</summary>
public sealed class PreviewAnchor
{
    public int Index { get; init; }
    public double XDip { get; set; }
    public double YDip { get; set; }
}

/// <summary>Snapshot of a successful preview render (kept when later input is invalid).</summary>
public sealed record LivePreviewSnapshot(
    string DefinitionJson,
    IReadOnlyList<PreviewAnchor> Anchors,
    LivePreviewState State,
    long Version);

/// <summary>
/// PG6.02 LivePreview engine (pure; no WPF timer required for tests).
/// Updates 250 ms after the last valid change. Invalid input keeps last valid preview.
/// Per-shape expression errors are recorded without discarding the last good snapshot.
/// Uses its own logical host id so the main chart does not need restart.
/// </summary>
public sealed class LivePreviewEngine
{
    public const int DebounceMilliseconds = 250;
    public const string PreviewHostId = "toolbuilder.preview.host";

    private readonly List<PreviewAnchor> _anchors = new();
    private LivePreviewSnapshot? _lastValid;
    private string? _pendingJson;
    private long _clockMs;
    private long? _debounceDeadlineMs;
    private long _version;
    private readonly Dictionary<int, string> _shapeErrors = new();

    public LivePreviewState State { get; private set; } = LivePreviewState.Preview;
    public LivePreviewSnapshot? LastValid => _lastValid;
    public IReadOnlyList<PreviewAnchor> Anchors => _anchors;
    public IReadOnlyDictionary<int, string> ShapeErrors => _shapeErrors;
    public bool HasPendingDebounce => _debounceDeadlineMs is not null;

    public void SetState(LivePreviewState state) => State = state;

    public void SetSampleAnchors(int count, double widthDip = 800, double heightDip = 500)
    {
        _anchors.Clear();
        if (count <= 0) return;
        for (var i = 0; i < count; i++)
        {
            double t = count == 1 ? 0.5 : (double)i / (count - 1);
            _anchors.Add(new PreviewAnchor
            {
                Index = i,
                XDip = 80 + t * (widthDip - 160),
                YDip = heightDip - 80 - t * (heightDip - 160)
            });
        }
    }

    public bool DragAnchor(int index, double xDip, double yDip)
    {
        if (index < 0 || index >= _anchors.Count) return false;
        _anchors[index].XDip = xDip;
        _anchors[index].YDip = yDip;
        return true;
    }

    /// <summary>Notify form changed. Only schedules apply when definition validates.</summary>
    public void OnFormChanged(ToolDefinitionFormModel form, long nowMs)
    {
        _clockMs = nowMs;
        _shapeErrors.Clear();

        // Collect per-shape errors without blocking last valid
        for (var i = 0; i < form.Shapes.Count; i++)
        {
            if (!string.IsNullOrEmpty(form.Shapes[i].ExpressionError))
                _shapeErrors[i] = form.Shapes[i].ExpressionError!;
        }

        if (!form.Validate())
        {
            // keep last valid; do not update pending to invalid
            _debounceDeadlineMs = null;
            _pendingJson = null;
            return;
        }

        _pendingJson = form.BuildDefinitionJson();
        if (_anchors.Count != form.Anchors)
            SetSampleAnchors(form.Anchors);
        _debounceDeadlineMs = nowMs + DebounceMilliseconds;
    }

    /// <summary>Advance clock; apply pending when debounce elapsed.</summary>
    public bool Tick(long nowMs)
    {
        _clockMs = nowMs;
        if (_debounceDeadlineMs is null || _pendingJson is null)
            return false;
        if (nowMs < _debounceDeadlineMs.Value)
            return false;

        _version++;
        var anchorsCopy = _anchors
            .Select(a => new PreviewAnchor { Index = a.Index, XDip = a.XDip, YDip = a.YDip })
            .ToList();
        _lastValid = new LivePreviewSnapshot(_pendingJson, anchorsCopy, State, _version);
        _pendingJson = null;
        _debounceDeadlineMs = null;
        return true;
    }

    /// <summary>Force immediate apply of current valid form (tests / explicit refresh).</summary>
    public bool ApplyNow(ToolDefinitionFormModel form)
    {
        if (!form.Validate())
            return false;
        if (_anchors.Count != form.Anchors)
            SetSampleAnchors(form.Anchors);
        _version++;
        var json = form.BuildDefinitionJson();
        var anchorsCopy = _anchors
            .Select(a => new PreviewAnchor { Index = a.Index, XDip = a.XDip, YDip = a.YDip })
            .ToList();
        _lastValid = new LivePreviewSnapshot(json, anchorsCopy, State, _version);
        _pendingJson = null;
        _debounceDeadlineMs = null;
        return true;
    }
}
