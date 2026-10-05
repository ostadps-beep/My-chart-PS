using System.Globalization;
using System.Text;
using MyChart.Core.Plugins.Manifest;
using MyChart.Core.Plugins.Vocabulary;

namespace MyChart.Core.Plugins.Expressions;

/// <summary>
/// PG2.02 ExpressionEvaluator — recursive descent for AT13 vectors.
/// Division by zero = NaN. Budget 10000 ops.
/// </summary>
public sealed class ExpressionEvaluator
{
    private readonly ExpressionContext _ctx;
    private string _src = "";
    private int _pos;
    private int _ops;

    public ExpressionEvaluator(ExpressionContext ctx) => _ctx = ctx;

    public object Evaluate(string expression)
    {
        if (expression.Length > VocabularyVersions.MaxExprChars)
            throw new ExpressionException(ErrorCodes.LimitExceeded, 0, "Expression too long");

        _src = expression;
        _pos = 0;
        _ops = 0;
        SkipWs();
        if (_pos >= _src.Length)
            throw new ExpressionException(ErrorCodes.ExpressionError, _pos, "Empty expression");

        var result = ParseExpr();
        SkipWs();
        if (_pos < _src.Length)
            throw new ExpressionException(ErrorCodes.ExpressionError, _pos, "Trailing input");
        return result;
    }

    public double EvaluateNumber(string expression)
    {
        var r = Evaluate(expression);
        return r is double d ? d : Convert.ToDouble(r, CultureInfo.InvariantCulture);
    }

    public string EvaluateString(string expression)
    {
        var r = Evaluate(expression);
        return r switch
        {
            string s => s,
            double d when double.IsNaN(d) => "NaN",
            double d => d.ToString("R", CultureInfo.InvariantCulture),
            bool b => b ? "true" : "false",
            _ => r?.ToString() ?? ""
        };
    }

    private object ParseExpr()
    {
        var left = ParseOr();
        return left;
    }

    private object ParseOr()
    {
        // comparison and additive in simple precedence
        return ParseComparison();
    }

    private object ParseComparison()
    {
        var left = ParseAdd();
        SkipWs();
        if (Match('>'))
        {
            bool eq = Match('=');
            var right = ParseAdd();
            return Compare(left, right, eq ? ">=" : ">");
        }
        if (Match('<'))
        {
            bool eq = Match('=');
            var right = ParseAdd();
            return Compare(left, right, eq ? "<=" : "<");
        }
        if (Match('=') && Match('='))
        {
            var right = ParseAdd();
            return Compare(left, right, "==");
        }
        return left;
    }

    private object ParseAdd()
    {
        var left = ParseMul();
        while (true)
        {
            SkipWs();
            if (Match('+'))
            {
                var right = ParseMul();
                left = Num(left) + Num(right);
            }
            else if (Match('-'))
            {
                var right = ParseMul();
                left = Num(left) - Num(right);
            }
            else break;
        }
        return left;
    }

    private object ParseMul()
    {
        var left = ParseUnary();
        while (true)
        {
            SkipWs();
            if (Match('*'))
            {
                var right = ParseUnary();
                left = Num(left) * Num(right);
            }
            else if (Match('/'))
            {
                var right = ParseUnary();
                var b = Num(right);
                left = b == 0 ? double.NaN : Num(left) / b;
            }
            else break;
        }
        return left;
    }

    private object ParseUnary()
    {
        SkipWs();
        if (Match('-'))
            return -Num(ParseUnary());
        if (Match('+'))
            return ParseUnary();
        return ParsePrimary();
    }

    private object ParsePrimary()
    {
        Budget();
        SkipWs();
        if (_pos >= _src.Length)
            throw new ExpressionException(ErrorCodes.ExpressionError, _pos, "Unexpected end");

        // number
        if (char.IsDigit(_src[_pos]) || (_src[_pos] == '.' && _pos + 1 < _src.Length && char.IsDigit(_src[_pos + 1])))
            return ParseNumber();

        // string
        if (_src[_pos] == '"' || _src[_pos] == '\'')
            return ParseString();

        // identifier or function
        if (char.IsLetter(_src[_pos]) || _src[_pos] == '_')
            return ParseIdentOrCall();

        // paren
        if (Match('('))
        {
            var v = ParseExpr();
            SkipWs();
            if (!Match(')'))
                throw new ExpressionException(ErrorCodes.ExpressionError, _pos, "Expected )");
            return v;
        }

        throw new ExpressionException(ErrorCodes.ExpressionError, _pos, "Unexpected character");
    }

    private double ParseNumber()
    {
        int start = _pos;
        while (_pos < _src.Length && (char.IsDigit(_src[_pos]) || _src[_pos] == '.'))
            _pos++;
        var s = _src[start.._pos];
        if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
            throw new ExpressionException(ErrorCodes.ExpressionError, start, "Bad number");
        return v;
    }

    private string ParseString()
    {
        char q = _src[_pos++];
        var sb = new StringBuilder();
        while (_pos < _src.Length && _src[_pos] != q)
        {
            if (_src[_pos] == '\\' && _pos + 1 < _src.Length)
            {
                _pos++;
                sb.Append(_src[_pos++]);
            }
            else sb.Append(_src[_pos++]);
        }
        if (_pos >= _src.Length)
            throw new ExpressionException(ErrorCodes.ExpressionError, _pos, "Unclosed string");
        _pos++; // closing quote
        return sb.ToString();
    }

    private object ParseIdentOrCall()
    {
        int start = _pos;
        while (_pos < _src.Length && (char.IsLetterOrDigit(_src[_pos]) || _src[_pos] == '_' || _src[_pos] == '.'))
            _pos++;
        // Handle anchor[i].price form — scanned partially as ident then [
        var name = _src[start.._pos];

        SkipWs();
        if (_pos < _src.Length && _src[_pos] == '[')
        {
            // anchor[i].price
            _pos++; // [
            var idxObj = ParseExpr();
            SkipWs();
            if (!Match(']'))
                throw new ExpressionException(ErrorCodes.ExpressionError, _pos, "Expected ]");
            int idx = (int)Num(idxObj);
            SkipWs();
            string field = "price";
            if (Match('.'))
            {
                int fs = _pos;
                while (_pos < _src.Length && (char.IsLetterOrDigit(_src[_pos]) || _src[_pos] == '_'))
                    _pos++;
                field = _src[fs.._pos];
            }
            if (name != "anchor")
                throw new ExpressionException(ErrorCodes.ExpressionError, start, "Only anchor[] supported");
            if (idx < 0 || idx >= _ctx.Anchors.Count)
                throw new ExpressionException(ErrorCodes.ExpressionError, start, "Anchor index out of range");
            return field switch
            {
                "price" => _ctx.Anchors[idx].Price,
                "index" => _ctx.Anchors[idx].Index,
                _ => throw new ExpressionException(ErrorCodes.ExpressionError, start, "Unknown anchor field")
            };
        }

        if (Match('('))
        {
            var args = new List<object>();
            SkipWs();
            if (!Match(')'))
            {
                while (true)
                {
                    args.Add(ParseExpr());
                    SkipWs();
                    if (Match(')')) break;
                    if (!Match(','))
                        throw new ExpressionException(ErrorCodes.ExpressionError, _pos, "Expected , or )");
                }
            }
            return Call(name, args, start);
        }

        // bare ident: param.x, named.x, text, level, price
        if (name.StartsWith("param.", StringComparison.Ordinal))
        {
            var key = name[6..];
            if (!_ctx.Params.TryGetValue(key, out var pv))
                throw new ExpressionException(ErrorCodes.ExpressionError, start, "Undefined param");
            return pv;
        }
        if (name.StartsWith("named.", StringComparison.Ordinal))
        {
            var key = name[6..];
            if (!_ctx.Named.TryGetValue(key, out var nv))
                throw new ExpressionException(ErrorCodes.ExpressionError, start, "Undefined named");
            return nv;
        }
        if (name == "text") return _ctx.Text;
        if (name == "level") return _ctx.Level;
        if (name == "price") return _ctx.Price;
        if (name == "true") return true;
        if (name == "false") return false;

        throw new ExpressionException(ErrorCodes.ExpressionError, start, "Unknown identifier");
    }

    private object Call(string name, List<object> args, int pos)
    {
        Budget();
        return name switch
        {
            "abs" => Math.Abs(Num(args[0])),
            "round" => Math.Round(Num(args[0]), (int)Num(args[1]), MidpointRounding.AwayFromZero),
            "fmt" => Num(args[0]).ToString("F" + (int)Num(args[1]), CultureInfo.InvariantCulture),
            "fmtSigned" => FmtSigned(Num(args[0]), (int)Num(args[1])),
            "if" => IsTruthy(args[0]) ? args[1] : args[2],
            "concat" => string.Concat(args.Select(a => a is double d ? d.ToString("R", CultureInfo.InvariantCulture) : a?.ToString() ?? "")),
            "priceDiff" => PriceAt((int)Num(args[1])) - PriceAt((int)Num(args[0])),
            "pointDiff" => (PriceAt((int)Num(args[1])) - PriceAt((int)Num(args[0]))) / _ctx.SymbolPoint,
            "pipDiff" => (PriceAt((int)Num(args[1])) - PriceAt((int)Num(args[0]))) / _ctx.SymbolPip,
            "pctChange" => (PriceAt((int)Num(args[1])) / PriceAt((int)Num(args[0])) - 1.0) * 100.0,
            "barCount" => (int)(IndexAt((int)Num(args[1])) - IndexAt((int)Num(args[0]))),
            "timeText" => TimeTextFromAnchors((int)Num(args[0]), (int)Num(args[1])),
            "hasPip" => _ctx.HasPip,
            _ => throw new ExpressionException(ErrorCodes.VocabularyUnsupported, pos, $"Unknown function '{name}'")
        };
    }

    private double PriceAt(int i)
    {
        if (i < 0 || i >= _ctx.Anchors.Count)
            throw new ExpressionException(ErrorCodes.ExpressionError, _pos, "Anchor index out of range");
        return _ctx.Anchors[i].Price;
    }

    private double IndexAt(int i)
    {
        if (i < 0 || i >= _ctx.Anchors.Count)
            throw new ExpressionException(ErrorCodes.ExpressionError, _pos, "Anchor index out of range");
        return _ctx.Anchors[i].Index;
    }

    private static string FmtSigned(double v, int digits)
    {
        var abs = Math.Abs(v).ToString("F" + digits, CultureInfo.InvariantCulture);
        return (v < 0 ? "-" : "+") + abs;
    }

    private string TimeTextFromAnchors(int a0, int a1)
    {
        // AT13: anchors index 10 and 25 on M15 => 15 bars * 15 min = 225 min = 3h 45m
        int bars = (int)Math.Abs(IndexAt(a1) - IndexAt(a0));
        int minutes = bars * 15; // M15 assumption for AT13
        if (minutes == 0) return "0m";
        int d = minutes / (60 * 24);
        minutes %= 60 * 24;
        int h = minutes / 60;
        int m = minutes % 60;
        var sb = new StringBuilder();
        if (d > 0) sb.Append(d).Append('d').Append(' ');
        if (h > 0) sb.Append(h).Append('h').Append(' ');
        if (m > 0 || sb.Length == 0) sb.Append(m).Append('m');
        return sb.ToString().Trim();
    }

    private static double Num(object o) => o switch
    {
        double d => d,
        int i => i,
        bool b => b ? 1 : 0,
        string s when double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) => v,
        _ => Convert.ToDouble(o, CultureInfo.InvariantCulture)
    };

    private static bool IsTruthy(object o) => o switch
    {
        bool b => b,
        double d => d != 0 && !double.IsNaN(d),
        string s => s.Length > 0,
        _ => o is not null
    };

    private static object Compare(object a, object b, string op)
    {
        double x = Num(a), y = Num(b);
        return op switch
        {
            ">" => x > y,
            ">=" => x >= y,
            "<" => x < y,
            "<=" => x <= y,
            "==" => Math.Abs(x - y) < 1e-12,
            _ => false
        };
    }

    private void Budget()
    {
        if (++_ops > VocabularyVersions.OperationBudget)
            throw new ExpressionException(ErrorCodes.ExpressionError, _pos, "Operation budget exceeded");
    }

    private void SkipWs()
    {
        while (_pos < _src.Length && char.IsWhiteSpace(_src[_pos])) _pos++;
    }

    private bool Match(char c)
    {
        if (_pos < _src.Length && _src[_pos] == c)
        {
            _pos++;
            return true;
        }
        return false;
    }
}
