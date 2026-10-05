using MyChart.Core.Analysis;
using MyChart.Core.Commands;
using MyChart.Core.Models.Drawing;
using MyChart.Core.Models.Rendering;
using Xunit;

namespace MyChart.Tests.Commands;

/// <summary>
/// T3.06 VERIFY: execute 5, undo 5, redo 5 restores identical drawing lists;
/// drag of 30 mouse moves = 1 history entry.
/// </summary>
public class CommandBusTests
{
    private static readonly RgbaColor Blue = RgbaColor.FromRgb(33, 150, 243);

    private static DrawingObject MakeDrawing(string id, double price = 1.10)
    {
        var anchors = new DrawingAnchor[]
        {
            new(DateTimeOffset.UnixEpoch, price),
            new(DateTimeOffset.UnixEpoch.AddMinutes(5), price + 0.01)
        };
        return new DrawingObject(
            id,
            "test.segment",
            1,
            anchors,
            DrawingStyleRules.Default(Blue),
            Locked: false,
            Hidden: false,
            Extra: null);
    }

    [Fact]
    public void Execute5_Undo5_Redo5_RestoresIdenticalDrawingLists()
    {
        var list = new DrawingList();
        var bus = new CommandBus();

        for (int i = 0; i < 5; i++)
        {
            var d = MakeDrawing($"d{i}", 1.10 + i * 0.01);
            bus.Execute(new AddDrawingCommand(list, d));
        }

        Assert.Equal(5, list.Items.Count);
        var afterExecute = list.Items.Select(x => x.Id).ToArray();

        for (int i = 0; i < 5; i++)
            bus.Undo();

        Assert.Empty(list.Items);

        for (int i = 0; i < 5; i++)
            bus.Redo();

        Assert.Equal(5, list.Items.Count);
        for (int i = 0; i < 5; i++)
            Assert.Equal(afterExecute[i], list.Items[i].Id);
    }

    [Fact]
    public void DragOf30MouseMoves_IsOneHistoryEntry()
    {
        var list = new DrawingList();
        var bus = new CommandBus();
        var original = MakeDrawing("drag1", 1.1000);
        bus.Execute(new AddDrawingCommand(list, original));
        bus.ClearHistory();

        var session = new DrawingDragSession(list, bus, list.Items[0]);

        for (int i = 1; i <= 30; i++)
        {
            var current = list.Find("drag1")!;
            var next = current with
            {
                Anchors = new DrawingAnchor[]
                {
                    new(DateTimeOffset.UnixEpoch, 1.1000 + i * 0.0001),
                    new(DateTimeOffset.UnixEpoch.AddMinutes(5), 1.1100 + i * 0.0001)
                }
            };
            session.Preview(next);
        }

        Assert.Equal(30, session.MoveCount);
        session.Commit();

        Assert.Single(bus.History);
        Assert.StartsWith("MoveDrawing:", bus.History[0]);
        Assert.True(bus.CanUndo);
        Assert.False(bus.CanRedo);

        Assert.Equal(1.1000 + 30 * 0.0001, list.Find("drag1")!.Anchors[0].Price, 10);

        bus.Undo();
        Assert.Equal(1.1000, list.Find("drag1")!.Anchors[0].Price, 10);

        bus.Redo();
        Assert.Equal(1.1000 + 30 * 0.0001, list.Find("drag1")!.Anchors[0].Price, 10);
    }

    [Fact]
    public void HistoryDepth_CapsAt200()
    {
        var list = new DrawingList();
        var bus = new CommandBus();
        for (int i = 0; i < 210; i++)
            bus.Execute(new AddDrawingCommand(list, MakeDrawing($"x{i}")));

        Assert.Equal(CommandConstants.HistoryDepth, bus.History.Count);
        Assert.Equal(210, list.Items.Count);
    }

    [Fact]
    public void ClearHistory_OnWorkspaceLoad_EmptiesStacks()
    {
        var list = new DrawingList();
        var bus = new CommandBus();
        bus.Execute(new AddDrawingCommand(list, MakeDrawing("a")));
        bus.Execute(new AddDrawingCommand(list, MakeDrawing("b")));
        bus.Undo();
        Assert.True(bus.CanUndo);
        Assert.True(bus.CanRedo);

        bus.ClearHistory();
        Assert.False(bus.CanUndo);
        Assert.False(bus.CanRedo);
        Assert.Empty(bus.History);
        Assert.Single(list.Items);
    }

    [Fact]
    public void EditDrawing_LockHide_Undoable()
    {
        var list = new DrawingList();
        var bus = new CommandBus();
        var d = MakeDrawing("e1");
        bus.Execute(new AddDrawingCommand(list, d));

        var locked = d with { Locked = true, Hidden = true };
        bus.Execute(new EditDrawingCommand(list, d, locked));
        Assert.True(list.Find("e1")!.Locked);
        Assert.True(list.Find("e1")!.Hidden);

        bus.Undo();
        Assert.False(list.Find("e1")!.Locked);
        Assert.False(list.Find("e1")!.Hidden);
    }

    [Fact]
    public void IndicatorCommands_AddRemoveEdit_RoundTrip()
    {
        var list = new IndicatorList();
        var bus = new CommandBus();
        var inst = new IndicatorInstance("ind1", "SMA", 3, new Dictionary<string, double> { ["Period"] = 14 });
        bus.Execute(new AddIndicatorCommand(list, inst));
        Assert.Single(list.Items);

        var edited = inst with { Parameters = new Dictionary<string, double> { ["Period"] = 21 } };
        bus.Execute(new EditIndicatorCommand(list, inst, edited));
        Assert.Equal(21, list.Find("ind1")!.Parameters["Period"]);

        bus.Execute(new RemoveIndicatorCommand(list, edited));
        Assert.Empty(list.Items);

        bus.Undo();
        Assert.Single(list.Items);
        bus.Undo();
        Assert.Equal(14, list.Find("ind1")!.Parameters["Period"]);
    }

    [Fact]
    public void CloneDrawing_AddsIndependentCopy()
    {
        var list = new DrawingList();
        var bus = new CommandBus();
        var d = MakeDrawing("src");
        bus.Execute(new AddDrawingCommand(list, d));
        var clone = d with { Id = "clone1" };
        bus.Execute(new CloneDrawingCommand(list, clone));
        Assert.Equal(2, list.Items.Count);
        bus.Undo();
        Assert.Single(list.Items);
        Assert.Equal("src", list.Items[0].Id);
    }

    [Fact]
    public void RemoveDrawings_UndoRestoresObjects()
    {
        var list = new DrawingList();
        var bus = new CommandBus();
        bus.Execute(new AddDrawingCommand(list, MakeDrawing("a")));
        bus.Execute(new AddDrawingCommand(list, MakeDrawing("b")));
        bus.Execute(new AddDrawingCommand(list, MakeDrawing("c")));
        bus.Execute(new RemoveDrawingsCommand(list, new[] { "a", "c" }));
        Assert.Single(list.Items);
        Assert.Equal("b", list.Items[0].Id);
        bus.Undo();
        Assert.Equal(3, list.Items.Count);
    }
}
