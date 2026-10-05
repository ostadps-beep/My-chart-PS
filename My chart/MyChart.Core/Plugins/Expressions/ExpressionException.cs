namespace MyChart.Core.Plugins.Expressions;

public sealed class ExpressionException : Exception
{
    public string Code { get; }
    public int Position { get; }

    public ExpressionException(string code, int position, string message)
        : base(message)
    {
        Code = code;
        Position = position;
    }
}
