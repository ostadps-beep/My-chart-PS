namespace MyChart.Core.Plugins.Expressions;

/// <summary>Evaluation context for tool expressions (AT13).</summary>
public sealed class ExpressionContext
{
    public IReadOnlyList<(double Price, double Index)> Anchors { get; init; } = Array.Empty<(double, double)>();
    public IReadOnlyDictionary<string, double> Params { get; init; } = new Dictionary<string, double>();
    public IReadOnlyDictionary<string, double> Named { get; init; } = new Dictionary<string, double>();
    public string Text { get; init; } = "";
    public int SymbolDigits { get; init; } = 5;
    public double SymbolPoint { get; init; } = 0.00001;
    public double SymbolPip { get; init; } = 0.0001;
    public bool HasPip { get; init; } = true;
    public double Level { get; init; }
    public double Price { get; init; }
}
