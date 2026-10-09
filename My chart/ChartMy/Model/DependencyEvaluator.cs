namespace ChartMy.Model;

public static class DependencyEvaluator
{
    public static bool IsEnabled(FieldDefinition field, SettingsDocument document)
    {
        var rule = field.EnableWhen;
        if (string.IsNullOrWhiteSpace(rule))
            return true;

        if (rule.EndsWith('?'))
        {
            var key = rule[..^1];
            return document.GetStringList(key).Count > 0;
        }

        var equals = rule.IndexOf('=');
        if (equals > 0)
        {
            var key = rule[..equals];
            var expected = rule[(equals + 1)..];
            return string.Equals(document.GetString(key), expected, StringComparison.OrdinalIgnoreCase);
        }

        if (rule[0] == '!')
            return !IsTruthy(document, rule[1..]);

        return IsTruthy(document, rule);
    }

    private static bool IsTruthy(SettingsDocument document, string key)
    {
        var value = document.Get(key);
        return value switch
        {
            bool b => b,
            string s => !string.IsNullOrWhiteSpace(s),
            int i => i != 0,
            double d => Math.Abs(d) > double.Epsilon,
            IEnumerable<string> list => list.Any(),
            _ => value is not null
        };
    }
}
