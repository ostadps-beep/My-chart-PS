namespace MyChart.Core.Models.Rendering;

/// <summary>Packed ARGB colour. Hex strings are parsed once when a theme loads.</summary>
public readonly record struct RgbaColor(uint Argb)
{
    public byte A => (byte)((Argb >> 24) & 0xFF);
    public byte R => (byte)((Argb >> 16) & 0xFF);
    public byte G => (byte)((Argb >> 8) & 0xFF);
    public byte B => (byte)(Argb & 0xFF);

    public static RgbaColor FromRgb(byte r, byte g, byte b) =>
        new(0xFF000000u | ((uint)r << 16) | ((uint)g << 8) | b);

    public static RgbaColor FromArgb(byte a, byte r, byte g, byte b) =>
        new(((uint)a << 24) | ((uint)r << 16) | ((uint)g << 8) | b);

    public static RgbaColor Transparent => new(0u);

    public static RgbaColor ParseHex(string hex)
    {
        ArgumentNullException.ThrowIfNull(hex);
        var s = hex.Trim();
        if (s.StartsWith('#'))
            s = s[1..];
        if (s.Length == 6)
            s = "FF" + s;
        if (s.Length != 8)
            throw new FormatException($"Invalid hex colour: '{hex}'.");
        return new RgbaColor(Convert.ToUInt32(s, 16));
    }
}
