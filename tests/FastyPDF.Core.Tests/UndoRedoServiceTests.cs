using FastyPDF.Core.Undo;
using Xunit;

namespace FastyPDF.Core.Tests;

public class UndoRedoServiceTests
{
    [Fact]
    public void UndoRedo_BasicFlow_WorksAsExpected()
    {
        var service = new UndoRedoService<string>();
        Assert.False(service.CanUndo);
        Assert.False(service.CanRedo);

        service.Push("State1");
        Assert.True(service.CanUndo);
        Assert.False(service.CanRedo);

        var undone = service.Undo("State2");
        Assert.Equal("State1", undone);
        Assert.False(service.CanUndo);
        Assert.True(service.CanRedo);

        var redone = service.Redo("State1");
        Assert.Equal("State2", redone);
        Assert.True(service.CanUndo);
        Assert.False(service.CanRedo);
    }

    [Fact]
    public void Undo_WhenEmpty_ReturnsDefault()
    {
        var service = new UndoRedoService<string>();
        var result = service.Undo("Current");
        Assert.Null(result);
    }

    [Fact]
    public void Push_ClearsRedoStack()
    {
        var service = new UndoRedoService<int>();
        service.Push(1);
        service.Undo(2);
        Assert.True(service.CanRedo);

        service.Push(3);
        Assert.False(service.CanRedo);
    }

    [Fact]
    public void Push_RespectsLimit()
    {
        var service = new UndoRedoService<int>(limit: 3);
        service.Push(1);
        service.Push(2);
        service.Push(3);
        service.Push(4);

        // When limit is 3, state 1 should be dropped
        var u1 = service.Undo(5); // gets 4
        var u2 = service.Undo(u1); // gets 3
        var u3 = service.Undo(u2); // gets 2
        Assert.Equal(4, u1);
        Assert.Equal(3, u2);
        Assert.Equal(2, u3);
        Assert.False(service.CanUndo); // 1 is gone
    }

    [Fact]
    public void Clear_EmptiesBothStacks()
    {
        var service = new UndoRedoService<int>();
        service.Push(1);
        service.Undo(2);
        Assert.True(service.CanRedo);

        service.Clear();
        Assert.False(service.CanUndo);
        Assert.False(service.CanRedo);
    }
}
