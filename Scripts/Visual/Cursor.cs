using Godot;
using System;
using System.Collections.Generic;

[GlobalClass]
public partial class Cursor : Node2D
{
	public Vector2I GridPos { get; private set; }
	public Vector2 RoundedWorldPos { get; private set; }
	private GridWorld _gridWorld;
	private DragController _dragController;

	private bool _shouldDraw = true;

    public override void _EnterTree()
    {
        AddToGroup("Cursor");
    }

	public override void _Ready()
	{
		_gridWorld = GameServices.GetGridWorld();

		_dragController = GameServices.GetDragController();

		_dragController.DragStarted += DragStarted;
		_dragController.DragCompleted += DragCompleted;
		_dragController.DragCancelled += DragCancelled;
	}

	public override void _Process(double delta)
	{
		if (_dragController == null)
		{
					_dragController = GameServices.GetDragController();

		_dragController.DragStarted += DragStarted;
		_dragController.DragCompleted += DragCompleted;
		_dragController.DragCancelled += DragCancelled;
		}

		Vector2 mousePosition = GetGlobalMousePosition();
		GridPos = _gridWorld.WorldToGrid(mousePosition);
		RoundedWorldPos = _gridWorld.GridToWorld(GridPos);
		QueueRedraw();
	}

	private void DragStarted()
	{
		_shouldDraw	= false;
	}
	private void DragCompleted(List<Vector2I> tiles)
	{
		_shouldDraw	= true;
	}
	private void DragCancelled()
	{
		_shouldDraw	= true;
	}

    public override void _Draw()
    {
		if (_shouldDraw)
		{
        	DrawRect(new Rect2(RoundedWorldPos, _gridWorld.TileSize, _gridWorld.TileSize), Colors.White, false, 2f);
		}
    }
}
