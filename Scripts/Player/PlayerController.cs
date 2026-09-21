using Godot;
using System;
using System.Collections.Generic;

public enum PlayerMode
{
    None,
    Dig,
    Build,
    Destroy
}

[GlobalClass]
public partial class PlayerController : Node
{
    public PlayerMode CurrentMode { get; private set; }

	private GridWorld _world;
	private DragController _dragController;
	private Cursor _cursor;

    public void SetMode(PlayerMode mode)
    {
        CurrentMode = mode;
    }

    public void CancelMode()
    {
        CurrentMode = PlayerMode.None;
    }

    public override void _EnterTree()
    {
        AddToGroup("PlayerController");
    }

    public override void _Ready()
    {
		_world = GameServices.GetGridWorld();

        _dragController = new DragController();
		AddChild(_dragController);

		_cursor = new Cursor();
		AddChild(_cursor);

		_dragController.DragStarted += OnDragStarted;
		_dragController.DragUpdated += OnDragUpdated;
		_dragController.DragCompleted += OnDragCompleted;
		_dragController.DragCancelled += OnDragCancelled;
    }

	public override void _Process(double delta)
	{
		Vector2I gridPosition = GetMouseGridPosition();
		_dragController.UpdateDrag(gridPosition);
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is not InputEventMouseButton mouseButton)
			return;

		if (mouseButton.ButtonIndex != MouseButton.Left)
			return;

		Vector2I gridPosition = GetMouseGridPosition();

		if (mouseButton.Pressed)
		{
			_dragController.StartDrag(gridPosition);
		}
		else
		{
			_dragController.CompleteDrag();
		}
	}

	private Vector2I GetMouseGridPosition()
    {
        // We'll connect this to your GridWorld/camera code.
        return _cursor.GridPos;
    }

	private void OnDragStarted()
	{
	}

	private void OnDragUpdated()
	{
	}

	private void OnDragCompleted(List<Vector2I> cells)
	{
		foreach (Vector2I tile in cells)
		{
			if (_world.GetTile(tile) == TileType.Empty)
				continue;

			GameServices.GetWorkOrderManager().CreateDigOrder(tile, 5);
		}
	}

	private void OnDragCancelled()
	{
	}
}
