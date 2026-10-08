namespace FastyPDF.Core.Undo;

public interface IUndoRedoService<T>
{
    bool CanUndo { get; }

    bool CanRedo { get; }

    void Push(T state);

    T? Undo(T current);

    T? Redo(T current);

    void Clear();
}

public sealed class UndoRedoService<T> : IUndoRedoService<T>
{
    private readonly Stack<T> _undo = new();
    private readonly Stack<T> _redo = new();
    private readonly int _limit;

    public UndoRedoService(int limit = 50)
    {
        _limit = limit;
    }

    public bool CanUndo => _undo.Count > 0;

    public bool CanRedo => _redo.Count > 0;

    public void Push(T state)
    {
        _undo.Push(state);
        _redo.Clear();
        while (_undo.Count > _limit)
        {
            var buffer = _undo.Reverse().Skip(1).Reverse().ToArray();
            _undo.Clear();
            foreach (var item in buffer)
            {
                _undo.Push(item);
            }
        }
    }

    public T? Undo(T current)
    {
        if (_undo.Count == 0)
        {
            return default;
        }

        _redo.Push(current);
        return _undo.Pop();
    }

    public T? Redo(T current)
    {
        if (_redo.Count == 0)
        {
            return default;
        }

        _undo.Push(current);
        return _redo.Pop();
    }

    public void Clear()
    {
        _undo.Clear();
        _redo.Clear();
    }
}
