using Godot;
using System;

[GlobalClass]
public partial class Cursor : Node2D
{
	private Vector2I _gridPos;
	private Vector2 _roundedWorldPos;
	private GridWorld _gridWorld;

	public override void _Ready()
	{
		_gridWorld = GameServices.GetGridWorld();
	}

	public override void _Process(double delta)
	{
		Vector2 mousePosition = GetGlobalMousePosition();
		_gridPos = _gridWorld.WorldToGrid(mousePosition);
		_roundedWorldPos = _gridWorld.GridToWorld(_gridPos);
		QueueRedraw();
	}

    public override void _Draw()
    {
        DrawRect(new Rect2(_roundedWorldPos, _gridWorld.TileSize, _gridWorld.TileSize), Colors.Black, false, 2f);
    }
}
