using Godot;
using System.Collections.Generic;

public partial class DragVisualizer : Node2D
{
    [Export]
    public Color FillColor { get; set; } = new Color(1, 1, 1, 0.15f);
    [Export]
    public Color BorderColor { get; set; } = new Color(1, 1, 1, 1f);

    [Export]
    public float BorderWidth { get; set; } = 2.0f;

    [Export]
    public float TileSize { get; set; }

    private DragController _dragController;

    public override void _Ready()
    {
        TileSize = GameServices.GetGridWorld().TileSize;

        _dragController = GameServices.GetDragController();

        _dragController.DragStarted += QueueRedraw;
        _dragController.DragUpdated += QueueRedraw;
        _dragController.DragCompleted += OnDragFinished;
        _dragController.DragCancelled += OnDragFinished;
    }

    public override void _Draw()
    {
        if (_dragController == null || !_dragController.IsDragging)
            return;

        Vector2I start = _dragController.StartPosition;
        Vector2I end = _dragController.CurrentPosition;

        int minX = Mathf.Min(start.X, end.X);
        int maxX = Mathf.Max(start.X, end.X);

        int minY = Mathf.Min(start.Y, end.Y);
        int maxY = Mathf.Max(start.Y, end.Y);

        Vector2 position = new Vector2(
            minX * TileSize,
            minY * TileSize
        );

        Vector2 size = new Vector2(
            (maxX - minX + 1) * TileSize,
            (maxY - minY + 1) * TileSize
        );

        Rect2 rect = new Rect2(position, size);

        DrawRect(rect, FillColor, true);
        DrawRect(rect, BorderColor, false, BorderWidth);
    }

    private void OnDragFinished(List<Vector2I> cells)
    {
        QueueRedraw();
    }

    private void OnDragFinished()
    {
        QueueRedraw();
    }
}