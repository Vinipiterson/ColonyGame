using Godot;
using System;
using System.Collections.Generic;

public enum PlayerMode
{
    None,
    Dig,
    Build,
    Destroy,
    Cancel
}

[GlobalClass]
public partial class PlayerController : Node
{
	[Export]
    public Godot.Collections.Array<DragColor> DragColors { get; set; } = new();

    public PlayerMode CurrentMode { get; private set; }
    public int CurrentPriority { get; private set; }

	private GridWorld _world;
	private DragController _dragController;
	private WorkOrderManager _orderManager;
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
		_orderManager = GameServices.GetWorkOrderManager();

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
		if (@event is InputEventMouseButton mouseButton)
		{
			InputMouseEvent(mouseButton);
		}

		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
			InputKeyboardEvent(keyEvent);
        }
	}

	private void InputMouseEvent(InputEventMouseButton mouseButton)
	{
		if (mouseButton.ButtonIndex == MouseButton.Left)
		{

			Vector2I gridPosition = GetMouseGridPosition();

			if (mouseButton.Pressed)
			{
				if (CurrentMode != PlayerMode.None)
					_dragController.StartDrag(gridPosition);
			}
			else
			{
				if (CurrentMode != PlayerMode.None)
					_dragController.CompleteDrag();
			}
		}
		else if (mouseButton.ButtonIndex ==  MouseButton.Right)
		{
			CurrentMode = PlayerMode.None;
		}
	}

	private void InputKeyboardEvent(InputEventKey keyEvent)
	{
		switch (keyEvent.Keycode)
        {
            case Key.Key1:
                    CurrentPriority = 1;
                    break;

                case Key.Key2:
                    CurrentPriority = 2;
                    break;

                case Key.Key3:
                    CurrentPriority = 3;
                    break;

                case Key.Key4:
                    CurrentPriority = 4;
                    break;

                case Key.Key5:
                    CurrentPriority = 5;
                    break;

                case Key.Key6:
                    CurrentPriority = 6;
                    break;

                case Key.Key7:
                    CurrentPriority = 7;
                    break;

                case Key.Key8:
                    CurrentPriority = 8;
                    break;

                case Key.Key9:
                    CurrentPriority = 9;
                    break;

                case Key.Key0:
                    CurrentPriority = 10;
                    break;
            }

		if (keyEvent.Keycode == Key.B)
		{
			CurrentMode = PlayerMode.Build;
		}	
		else if (keyEvent.Keycode == Key.G)
		{
			CurrentMode = PlayerMode.Dig;
		}
		else if (keyEvent.Keycode == Key.X)
		{
			CurrentMode = PlayerMode.Destroy;
		}
		else if (keyEvent.Keycode == Key.C)
		{
			CurrentMode = PlayerMode.Cancel;
		}

        return;
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
		if (CurrentMode == PlayerMode.Dig)
		{
			DigTiles(cells);
		}
		else if (CurrentMode == PlayerMode.Cancel)
		{
			CancelOrders(cells);
		}
	}

	private void OnDragCancelled()
	{
	}

	private void DigTiles(List<Vector2I> cells)
	{
		foreach (Vector2I tile in cells)
		{
			if (_world.GetTile(tile) == TileType.Empty)
				continue;

			GameServices.GetWorkOrderManager().CreateDigOrder(tile, CurrentPriority);
		}
	}
	private void CancelOrders(List<Vector2I> cells)
	{
		_orderManager.CancelOrders(cells);
	}
}
