using MyChart.Core.Commands;
using MyChart.Core.Models.Indicators;
using MyChart.Core.UI.Indicators;
using Xunit;

namespace MyChart.Tests.UI;

/// <summary>T6.06 Indicator dialog — list, add, edit, remove.</summary>
public class IndicatorDialogTests
{
    private static (IndicatorList list, IndicatorDialogController ctl) Create()
    {
        var list = new IndicatorList();
        var catalog = new[]
        {
            new IndicatorCatalogEntry("SMA", new[]
            {
                new IndicatorParameter("Period", "Period", 14, Min: 1, Max: 500, Step: 1)
            })
        };
        var model = new IndicatorDialogModel(list, catalog);
        return (list, new IndicatorDialogController(list, model));
    }

    [Fact]
    public void OpenClose()
    {
        var (_, ctl) = Create();
        Assert.False(ctl.Model.IsOpen);
        ctl.Open();
        Assert.True(ctl.Model.IsOpen);
        ctl.Close();
        Assert.False(ctl.Model.IsOpen);
    }

    [Fact]
    public void Add_AppearsInRows()
    {
        var (list, ctl) = Create();
        ctl.Add("SMA", new Dictionary<string, double> { ["Period"] = 20 });
        Assert.Single(list.Items);
        Assert.Single(ctl.Model.Rows);
        Assert.Equal("SMA", ctl.Model.Rows[0].IndicatorName);
        Assert.Contains("Period=20", ctl.Model.Rows[0].Summary);
    }

    [Fact]
    public void Edit_UpdatesParameters()
    {
        var (list, ctl) = Create();
        ctl.Add("SMA", new Dictionary<string, double> { ["Period"] = 14 });
        var id = list.Items[0].Id;
        Assert.True(ctl.Edit(id, new Dictionary<string, double> { ["Period"] = 50 }));
        Assert.Equal(50, list.Items[0].Parameters["Period"]);
        Assert.Contains("Period=50", ctl.Model.Rows[0].Summary);
    }

    [Fact]
    public void Remove_ClearsRow()
    {
        var (list, ctl) = Create();
        ctl.Add("SMA", new Dictionary<string, double> { ["Period"] = 14 });
        var id = list.Items[0].Id;
        Assert.True(ctl.Remove(id));
        Assert.Empty(list.Items);
        Assert.Empty(ctl.Model.Rows);
    }

    [Fact]
    public void AddCommand_Undo_Removes()
    {
        var (list, ctl) = Create();
        var cmd = ctl.CreateAddCommand("SMA", new Dictionary<string, double> { ["Period"] = 9 });
        cmd.Execute();
        Assert.Single(list.Items);
        cmd.Undo();
        Assert.Empty(list.Items);
    }

    [Fact]
    public void Catalog_ExposesAvailableIndicators()
    {
        var (_, ctl) = Create();
        Assert.Single(ctl.Model.Catalog);
        Assert.Equal("SMA", ctl.Model.Catalog[0].Name);
    }
}
