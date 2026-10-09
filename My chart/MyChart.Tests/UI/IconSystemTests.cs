using MyChart.Core.Services;
using MyChart.Core.UI.Icons;
using MyChart.Plugins;
using Xunit;

namespace MyChart.Tests.UI;

/// <summary>
/// T7.01 IconSystem — colour rules, required keys, ViewBox; GATE=VISUAL pending owner PNG.
/// StreamGeometry conversion lives in MyChart.UI (not referenced by this test project).
/// </summary>
public class IconSystemTests
{
    [Fact]
    public void ViewBox_Is_16()
    {
        Assert.Equal(16, IconSystemModel.IconViewBox);
        var model = new IconSystemModel(IconCatalog.All);
        Assert.Equal(16, model.ViewBox);
    }

    [Fact]
    public void Required_Keys_Are_Exactly_23_Spec_Names()
    {
        Assert.Equal(23, IconSystemModel.RequiredIconKeys.Count);
        Assert.Equal(23, IconCatalog.All.Count);

        var model = new IconSystemModel(IconCatalog.All);
        Assert.True(model.HasAllRequiredKeys(), string.Join(", ", model.MissingRequiredKeys()));
        Assert.Empty(model.MissingRequiredKeys());
    }

    [Fact]
    public void Every_Catalog_Icon_Has_ViewBox_16_And_NonEmpty_Path()
    {
        foreach (var icon in IconCatalog.All)
        {
            Assert.Equal(16, icon.ViewBox);
            Assert.False(string.IsNullOrWhiteSpace(icon.PathData));
            Assert.StartsWith("Icon.", icon.IconKey, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Color_Rules_Normal_Hover_Active_Disabled()
    {
        var theme = ThemeService.Dark;

        Assert.Equal(theme.AxisColor, IconColorRules.Resolve(theme, IconVisualState.Normal));
        Assert.Equal(theme.AccentColor, IconColorRules.Resolve(theme, IconVisualState.Hover));
        Assert.Equal(theme.AccentColor, IconColorRules.Resolve(theme, IconVisualState.Active));

        var disabled = IconColorRules.Resolve(theme, IconVisualState.Disabled);
        Assert.Equal(theme.AxisColor.R, disabled.R);
        Assert.Equal(theme.AxisColor.G, disabled.G);
        Assert.Equal(theme.AxisColor.B, disabled.B);
        Assert.Equal((byte)Math.Round(theme.AxisColor.A * IconColorRules.DisabledOpacity), disabled.A);
        Assert.Equal(0.40, IconColorRules.DisabledOpacity, 5);
    }

    [Fact]
    public void Lookup_By_Key_Works()
    {
        var model = new IconSystemModel(IconCatalog.All);
        Assert.True(model.TryGet("Icon.TrendLine", out var d));
        Assert.Equal("Icon.TrendLine", d.IconKey);
        Assert.False(model.TryGet("Icon.Missing", out _));
        Assert.True(model.Contains("Icon.Settings"));
    }

    [Fact]
    public void Duplicate_Key_Throws()
    {
        var a = IconCatalog.All[0];
        Assert.Throws<ArgumentException>(() => new IconSystemModel(new[] { a, a }));
    }

    [Fact]
    public void IconGeometry_Xaml_Is_Not_Created()
    {
        // A3: MyChart.UI/Icons/IconGeometry.xaml must not exist.
        var root = FindRepoRoot();
        var forbidden = Path.Combine(root, "MyChart.UI", "Icons", "IconGeometry.xaml");
        Assert.False(File.Exists(forbidden), "IconGeometry.xaml must not be created (A3).");

        var iconsDir = Path.Combine(root, "MyChart.UI", "Icons");
        Assert.True(Directory.Exists(iconsDir), "MyChart.UI/Icons should hold conversion code.");
        Assert.True(File.Exists(Path.Combine(iconsDir, "IconGeometryFactory.cs")));
        Assert.True(File.Exists(Path.Combine(iconsDir, "IconProvider.cs")));
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "MyChart.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("MyChart.sln not found from test base directory.");
    }
}
