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

	private BuildingCatalog _buildingCatalog;

	private BuildingDefinition _selectedBuilding;
	private Building _buildingPreview;

	private StructureGrid _structureGrid;

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
		_structureGrid = GameServices.GetStructureGrid();

		_buildingCatalog = GetTree().GetFirstNodeInGroup("BuildingCatalog") as BuildingCatalog;

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

		UpdateBuildingPreview(gridPosition);
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

		if (keyEvent.Keycode == Key.F1)
			SelectBuilding(0);
		else if (keyEvent.Keycode == Key.F2)
			SelectBuilding(1);

		else if (keyEvent.Keycode == Key.F3)
			SelectBuilding(2);

		else if (keyEvent.Keycode == Key.F4)
			SelectBuilding(3);

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
		else if (CurrentMode == PlayerMode.Build)
		{
			BuildAt(cells);
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

	private void UpdateBuildingPreview(Vector2I gridPosition)
	{
		if (CurrentMode != PlayerMode.Build ||
			_selectedBuilding == null)
		{
			DestroyBuildingPreview();
			return;
		}

		if (_buildingPreview == null)
			CreateBuildingPreview();

		_buildingPreview.SetGridPosition(
			gridPosition,
			_world.GridToWorld(gridPosition));

		bool canPlace = _structureGrid.CanPlaceBuilding(
			_selectedBuilding,
			gridPosition);

		_buildingPreview.SetBlueprintValidity(canPlace);
	}

	private void CreateBuildingPreview()
	{
		if (_selectedBuilding == null ||
			_selectedBuilding.Scene == null)
			return;

		_buildingPreview =
			_selectedBuilding.Scene.Instantiate<Building>();

		AddChild(_buildingPreview);

		_buildingPreview.Initialize(
			_selectedBuilding,
			Vector2I.Zero,
			Vector2.Zero,
			BuildingState.UnderConstruction);
	}

	private void DestroyBuildingPreview()
	{
		if (_buildingPreview == null)
			return;

		_buildingPreview.QueueFree();
		_buildingPreview = null;
	}

	private void SelectBuilding(int index)
	{
		if (_buildingCatalog == null)
			return;

		if (index < 0 || index >= _buildingCatalog.Buildings.Count)
			return;

		_selectedBuilding = _buildingCatalog.Buildings[index];

		CurrentMode = PlayerMode.Build;

		DestroyBuildingPreview();
	}

	private void BuildAt(List<Vector2I> cells)
	{
		if (_selectedBuilding == null)
			return;

		if (cells.Count != 1)
			return;

		Vector2I position = cells[0];

		if (!_structureGrid.CanPlaceBuilding(
			_selectedBuilding,
			position))
		{
			return;
		}

		_orderManager.CreateBuildOrder(
			_selectedBuilding,
			position,
			CurrentPriority);
	}
}
