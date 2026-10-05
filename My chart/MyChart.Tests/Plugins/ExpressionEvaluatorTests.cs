using MyChart.Core.Plugins.Expressions;
using MyChart.Core.Plugins.Manifest;
using Xunit;

namespace MyChart.Tests.Plugins;

/// <summary>AT13 ExpressionVectors.</summary>
public class ExpressionEvaluatorTests
{
    private static ExpressionEvaluator Eval()
    {
        var ctx = new ExpressionContext
        {
            Anchors = new[] { (1.1000, 10.0), (1.1025, 25.0) },
            SymbolDigits = 5,
            SymbolPoint = 0.00001,
            SymbolPip = 0.0001,
            HasPip = true
        };
        return new ExpressionEvaluator(ctx);
    }

    [Theory]
    [InlineData("1+2*3", 7.0)]
    [InlineData("abs(-2.5)", 2.5)]
    public void Arithmetic(string expr, double expected)
    {
        Assert.Equal(expected, Eval().EvaluateNumber(expr), 10);
    }

    [Fact]
    public void Round_And_Fmt()
    {
        Assert.Equal(1.235, Eval().EvaluateNumber("round(1.23456,3)"), 10);
        Assert.Equal("1.10000", Eval().EvaluateString("fmt(1.1,5)"));
    }

    [Fact]
    public void If_And_DivisionByZero()
    {
        Assert.Equal(2.0, Eval().EvaluateNumber("if(1>2,1,2)"), 10);
        Assert.True(double.IsNaN(Eval().EvaluateNumber("1/0")));
    }

    [Fact]
    public void PriceDiff_Formats()
    {
        Assert.Equal("+0.00250", Eval().EvaluateString("fmtSigned(priceDiff(0,1),5)"));
        Assert.Equal("+250", Eval().EvaluateString("fmtSigned(pointDiff(0,1),0)"));
        Assert.Equal("+25.0", Eval().EvaluateString("fmtSigned(pipDiff(0,1),1)"));
        Assert.Equal("+0.23", Eval().EvaluateString("fmtSigned(pctChange(0,1),2)"));
        Assert.Equal("15 bars", Eval().EvaluateString("concat(barCount(0,1),\" bars\")"));
        Assert.Equal("3h 45m", Eval().EvaluateString("timeText(0,1)"));
        Assert.Equal(true, Eval().Evaluate("hasPip()"));
    }

    [Fact]
    public void FmtSigned_Negative()
    {
        Assert.Equal("-0.00250", Eval().EvaluateString("fmtSigned(-0.0025,5)"));
        Assert.Equal("+0.500", Eval().EvaluateString("fmtSigned(0.5,3)"));
    }

    [Fact]
    public void Errors_Codes()
    {
        var ex = Assert.Throws<ExpressionException>(() => Eval().Evaluate("1+"));
        Assert.Equal(ErrorCodes.ExpressionError, ex.Code);

        ex = Assert.Throws<ExpressionException>(() => Eval().Evaluate("anchor[5].price"));
        Assert.Equal(ErrorCodes.ExpressionError, ex.Code);

        ex = Assert.Throws<ExpressionException>(() => Eval().Evaluate("param.zzz"));
        Assert.Equal(ErrorCodes.ExpressionError, ex.Code);

        ex = Assert.Throws<ExpressionException>(() => Eval().Evaluate("foo(1)"));
        Assert.Equal(ErrorCodes.VocabularyUnsupported, ex.Code);

        var longExpr = new string('1', 257);
        ex = Assert.Throws<ExpressionException>(() => Eval().Evaluate(longExpr));
        Assert.Equal(ErrorCodes.LimitExceeded, ex.Code);
    }
}
