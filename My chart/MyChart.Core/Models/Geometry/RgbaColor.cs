using System.Globalization;

namespace MyChart.Core.Models.Geometry;

/// <summary>
/// 32-bit RGBA color (T4.02 ColorTokensMinimal and L5 Renderer support).
/// Immutable value type; supports hex string parsing.
/// 
/// All values are in the range [0, 255].
/// </summary>
public readonly struct RgbaColor : IEquatable<RgbaColor>
{
    /// <summary>Red channel (0-255).</summary>
    public byte R { get; }

    /// <summary>Green channel (0-255).</summary>
    public byte G { get; }

    /// <summary>Blue channel (0-255).</summary>
    public byte B { get; }

    /// <summary>Alpha channel (0-255); 255 is fully opaque, 0 is fully transparent.</summary>
    public byte A { get; }

    /// <summary>
    /// Create an RGBA color from component bytes.
    /// </summary>
    public RgbaColor(byte r, byte g, byte b, byte a = 255)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    /// <summary>
    /// Parse a hex color string (with or without alpha).
    /// 
    /// Formats supported:
    /// - #RRGGBB (opaque; alpha = 255)
    /// - #RRGGBBAA (with alpha)
    /// - RRGGBB (opaque; alpha = 255)
    /// - RRGGBBAA (with alpha)
    /// 
    /// Case-insensitive. Whitespace is trimmed.
    /// </summary>
    /// <param name="hex">Hex color string.</param>
    /// <returns>Parsed RgbaColor.</returns>
    /// <exception cref="ArgumentNullException">If hex is null.</exception>
    /// <exception cref="FormatException">If hex format is invalid.</exception>
    public static RgbaColor FromHex(string hex)
    {
        ArgumentNullException.ThrowIfNull(hex);

        hex = hex.Trim().TrimStart('#');

        // Default to opaque if no alpha channel provided
        if (hex.Length == 6)
            hex += "FF";

        if (hex.Length != 8)
            throw new FormatException($"Invalid hex color format: {hex}. Expected #RRGGBB or #RRGGBBAA.");

        try
        {
            var r = byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            var g = byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            var b = byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            var a = byte.Parse(hex.Substring(6, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

            return new RgbaColor(r, g, b, a);
        }
        catch (FormatException ex)
        {
            throw new FormatException($"Failed to parse hex color: {hex}", ex);
        }
    }

    /// <summary>Format as #RRGGBBAA.</summary>
    public override string ToString() => $"#{R:X2}{G:X2}{B:X2}{A:X2}";

    /// <summary>RGBA equality.</summary>
    public bool Equals(RgbaColor other) => R == other.R && G == other.G && B == other.B && A == other.A;

    /// <summary>Object equality.</summary>
    public override bool Equals(object? obj) => obj is RgbaColor other && Equals(other);

    /// <summary>Hash code.</summary>
    public override int GetHashCode() => HashCode.Combine(R, G, B, A);

    /// <summary>Equality operator.</summary>
    public static bool operator ==(RgbaColor left, RgbaColor right) => left.Equals(right);

    /// <summary>Inequality operator.</summary>
    public static bool operator !=(RgbaColor left, RgbaColor right) => !left.Equals(right);
}
