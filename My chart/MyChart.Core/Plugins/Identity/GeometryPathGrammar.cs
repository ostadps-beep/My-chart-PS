using System.Globalization;
using System.Text.RegularExpressions;

namespace MyChart.Core.Plugins.Identity;

/// <summary>AT5 V6: SVG path subset M/L (and common) commands; max 4096 chars; complete coordinate pairs.</summary>
public static partial class GeometryPathGrammar
{
    private const int MaxLength = 4096;

    public static bool IsValid(string? pathData)
    {
        if (string.IsNullOrWhiteSpace(pathData)) return false;
        if (pathData.Length > MaxLength) return false;

        var normalized = pathData.Trim();
        foreach (char c in normalized)
        {
            if (char.IsLetter(c) && !"MmLlZzHhVvCcSsQqTtAa".Contains(c))
                return false;
        }

        var first = normalized.SkipWhile(char.IsWhiteSpace).FirstOrDefault();
        if (first is not ('M' or 'm')) return false;

        // Tokenize: commands and numbers must form complete pairs for M/L
        return TokensValid(normalized);
    }

    private static bool TokensValid(string path)
    {
        // Split into command letters and number tokens
        var tokens = new List<string>();
        int i = 0;
        while (i < path.Length)
        {
            if (char.IsWhiteSpace(path[i]) || path[i] == ',')
            {
                i++;
                continue;
            }
            if (char.IsLetter(path[i]))
            {
                tokens.Add(path[i].ToString());
                i++;
                continue;
            }
            // number
            int start = i;
            if (path[i] is '+' or '-') i++;
            bool digits = false;
            while (i < path.Length && char.IsDigit(path[i])) { digits = true; i++; }
            if (i < path.Length && path[i] == '.')
            {
                i++;
                while (i < path.Length && char.IsDigit(path[i])) { digits = true; i++; }
            }
            if (!digits) return false;
            tokens.Add(path[start..i]);
        }

        if (tokens.Count == 0) return false;

        int t = 0;
        while (t < tokens.Count)
        {
            var cmd = tokens[t];
            if (cmd.Length != 1 || !char.IsLetter(cmd[0])) return false;
            t++;
            char c = cmd[0];
            int need = c switch
            {
                'Z' or 'z' => 0,
                'H' or 'h' or 'V' or 'v' => 1,
                'M' or 'm' or 'L' or 'l' or 'T' or 't' => 2,
                'S' or 's' or 'Q' or 'q' => 4,
                'C' or 'c' => 6,
                'A' or 'a' => 7,
                _ => -1
            };
            if (need < 0) return false;
            if (t + need > tokens.Count) return false; // incomplete e.g. "M2 2 L14"
            for (int k = 0; k < need; k++)
            {
                if (!double.TryParse(tokens[t + k], NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                    return false;
            }
            t += need;
            // implicit line-to after moveto extra pairs
            if (c is 'M' or 'm' or 'L' or 'l')
            {
                while (t < tokens.Count && !char.IsLetter(tokens[t][0]))
                {
                    if (t + 2 > tokens.Count) return false;
                    if (!double.TryParse(tokens[t], NumberStyles.Float, CultureInfo.InvariantCulture, out _)) return false;
                    if (!double.TryParse(tokens[t + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out _)) return false;
                    t += 2;
                }
            }
        }
        return true;
    }

    public static string? Validate(string? pathData) =>
        IsValid(pathData) ? null : Manifest.ErrorCodes.InvalidGeometry;
}
