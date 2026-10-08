using FastyPDF.Core.Undo;
using Xunit;

namespace FastyPDF.Core.Tests;

public class OrganizeUndoRedoLogicTests
{
    [Fact]
    public void PagePool_RestoreAfterDeletion_PreservesAllPages()
    {
        var undo = new UndoRedoService<int[]>();
        var pagePool = new Dictionary<int, string>(); // string represents PageItemViewModel

        // Initial 3 pages: [0, 1, 2]
        var currentPages = new List<int> { 0, 1, 2 };
        foreach (var p in currentPages)
        {
            pagePool[p] = $"Page_{p + 1}";
        }

        // User deletes page 1 (second page)
        undo.Push(currentPages.ToArray());
        currentPages.Remove(1); // Now [0, 2]

        Assert.True(undo.CanUndo);
        Assert.Equal(2, currentPages.Count);

        // Undo operation
        var previous = undo.Undo(currentPages.ToArray());
        Assert.NotNull(previous);

        // Restore using pagePool
        currentPages.Clear();
        foreach (var index in previous!)
        {
            if (pagePool.TryGetValue(index, out _))
            {
                currentPages.Add(index);
            }
        }

        // Page 1 is successfully restored!
        Assert.Equal(new[] { 0, 1, 2 }, currentPages);

        // Redo operation
        var next = undo.Redo(currentPages.ToArray());
        Assert.NotNull(next);

        currentPages.Clear();
        foreach (var index in next!)
        {
            if (pagePool.TryGetValue(index, out _))
            {
                currentPages.Add(index);
            }
        }

        // After redo, page 1 is removed again
        Assert.Equal(new[] { 0, 2 }, currentPages);
    }
}
