namespace MyChart.Core.Models.Rendering;

public readonly record struct RgbaColor(uint Argb)
{
    public static RgbaColor FromRgb(byte r, byte g, byte b) => new(0xFF000000u | ((uint)r << 16) | ((uint)g << 8) | b);
    public static RgbaColor FromArgb(byte a, byte r, byte g, byte b) => new(((uint)a << 24) | ((uint)r << 16) | ((uint)g << 8) | b);
}
