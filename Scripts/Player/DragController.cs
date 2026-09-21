using Godot;
using System;
using System.Collections.Generic;

public partial class DragController : Node
{
    public bool Enabled { get; set; } = true;

    public bool IsDragging { get; private set; } = false;

    public Vector2I StartPosition { get; private set; }
    public Vector2I CurrentPosition { get; private set; }

    public IReadOnlyCollection<Vector2I> SelectedCells => _selectedCells;

    public event Action DragStarted;
    public event Action DragUpdated;
    public event Action<List<Vector2I>> DragCompleted;
    public event Action DragCancelled;

    private readonly HashSet<Vector2I> _selectedCells = new();

    private DragVisualizer _visualizer;

    public override void _EnterTree()
    {
        AddToGroup("DragController");

        _visualizer = new DragVisualizer();
        AddChild(_visualizer);
    }

    public void StartDrag(Vector2I gridPosition)
    {
        if (!Enabled)
            return;

        IsDragging = true;

        StartPosition = gridPosition;
        CurrentPosition = gridPosition;

        UpdateSelectedCells();
        DragStarted?.Invoke();
    }

    public void UpdateDrag(Vector2I gridPosition)
    {
        if (!Enabled || !IsDragging)
            return;

        if (gridPosition == CurrentPosition)
            return;

        CurrentPosition = gridPosition;

        UpdateSelectedCells();
        DragUpdated?.Invoke();
    }

    public void CompleteDrag()
    {
        if (!IsDragging)
            return;

        IsDragging = false;

        List<Vector2I> cells = new(_selectedCells);

        DragCompleted?.Invoke(cells);

        ClearSelection();
    }

    public void CancelDrag()
    {
        if (!IsDragging)
            return;

        IsDragging = false;

        ClearSelection();

        DragCancelled?.Invoke();
    }

    private void UpdateSelectedCells()
    {
        _selectedCells.Clear();

        int minX = Mathf.Min(StartPosition.X, CurrentPosition.X);
        int maxX = Mathf.Max(StartPosition.X, CurrentPosition.X);

        int minY = Mathf.Min(StartPosition.Y, CurrentPosition.Y);
        int maxY = Mathf.Max(StartPosition.Y, CurrentPosition.Y);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                _selectedCells.Add(new Vector2I(x, y));
            }
        }
    }

    private void ClearSelection()
    {
        _selectedCells.Clear();
    }
}