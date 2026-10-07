using MyChart.Core.Commands;
using MyChart.Core.Models.Indicators;
using MyChart.Core.Models.Rendering;
using MyChart.Core.UI.Indicators;
using MyChart.Tests.Indicators;
using Xunit;

namespace MyChart.Tests.UI;

/// <summary>T6.06 IndicatorLayerAndDialog — polyline NaN breaks, palette order, dialog CRUD (GATE=VISUAL pending).</summary>
public class IndicatorLayerDialogTests
{
    [Fact]
    public void LayerIndex_Is_4()
    {
        Assert.Equal(4, IndicatorLayerModel.LayerIndex);
    }

    [Fact]
    public void NaN_Breaks_Polyline_Into_Segments()
    {
        var values = new double[] { 1, 2, double.NaN, 4, 5, double.NaN, 7 };
        var output = new IndicatorOutput("sma", values);
        var segs = IndicatorLayerModel.BuildSegments(output, 0, values.Length);
        Assert.Equal(3, segs.Count);
        Assert.Equal(new[] { (0, 1.0), (1, 2.0) }, segs[0].Points.ToArray());
        Assert.Equal(new[] { (3, 4.0), (4, 5.0) }, segs[1].Points.ToArray());
        Assert.Equal(new[] { (6, 7.0) }, segs[2].Points.ToArray());
    }

    [Fact]
    public void Visible_Range_Clips_Points()
    {
        var values = new double[] { 1, 2, 3, 4, 5 };
        var output = new IndicatorOutput("sma", values);
        var segs = IndicatorLayerModel.BuildSegments(output, 1, 4);
        Assert.Single(segs);
        Assert.Equal(new[] { (1, 2.0), (2, 3.0), (3, 4.0) }, segs[0].Points.ToArray());
    }

    [Fact]
    public void Palette_Colors_Assigned_In_Order()
    {
        var theme = ThemeTokens.Dark;
        var series = new List<(string, IndicatorOutput)>
        {
            ("a", new IndicatorOutput("o1", new[] { 1.0, 2.0 })),
            ("b", new IndicatorOutput("o2", new[] { 3.0, 4.0 })),
        };
        var draws = IndicatorLayerModel.Build(series, theme, 0, 2);
        Assert.Equal(2, draws.Count);
        Assert.Equal(theme.IndicatorPalette[0], draws[0].Color);
        Assert.Equal(theme.IndicatorPalette[1], draws[1].Color);
        Assert.Equal(0, draws[0].PaletteIndex);
        Assert.Equal(1, draws[1].PaletteIndex);
    }

    [Fact]
    public void Palette_Wraps_After_Six()
    {
        var theme = ThemeTokens.Dark;
        var series = Enumerable.Range(0, 7)
            .Select(i => ($"i{i}", new IndicatorOutput("o", new[] { 1.0 })))
            .ToList();
        var draws = IndicatorLayerModel.Build(series, theme, 0, 1);
        Assert.Equal(theme.IndicatorPalette[0], draws[6].Color);
        Assert.Equal(6, draws[6].PaletteIndex);
    }

    [Fact]
    public void Dialog_List_Add_Edit_Remove()
    {
        var list = new IndicatorList();
        var dlg = new IndicatorDialogModel(list);

        dlg.Add("1", "SMA", new Dictionary<string, double> { ["period"] = 14 });
        Assert.Single(dlg.Rows);
        Assert.Equal("SMA", dlg.Rows[0].IndicatorName);

        Assert.True(dlg.EditParameters("1", new Dictionary<string, double> { ["period"] = 21 }));
        Assert.Equal(21, dlg.Rows[0].Parameters["period"]);

        Assert.True(dlg.Remove("1"));
        Assert.Empty(dlg.Rows);

        Assert.Equal(
            new[] { "AddIndicator:SMA", "EditIndicator:1", "RemoveIndicator:1" },
            dlg.IssuedCommands.ToArray());
    }

    [Fact]
    public void Dialog_AddFromDefinition_Uses_Defaults()
    {
        var list = new IndicatorList();
        var dlg = new IndicatorDialogModel(list);
        var sma = new SmaIndicator(14);
        dlg.AddFromDefinition("sma1", sma);
        Assert.Equal(sma.Name, dlg.Rows[0].IndicatorName);
        Assert.Equal(sma.Inputs[0].DefaultValue, dlg.Rows[0].Parameters[sma.Inputs[0].Key]);
    }

    [Fact]
    public void Dialog_Edit_Unknown_Returns_False()
    {
        var dlg = new IndicatorDialogModel(new IndicatorList());
        Assert.False(dlg.EditParameters("missing", new Dictionary<string, double>()));
        Assert.False(dlg.Remove("missing"));
    }
}
