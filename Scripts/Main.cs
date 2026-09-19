using Godot;
using System;

public partial class Main : Node2D
{
	private const int Width = 40;
	private const int Height = 25;
	private const int TileSize = 32;

	private bool[,] _tiles = new bool[Width, Height];

	public override void _Ready()
	{
		GD.Print("MAIN IS RUNNING");

		for (int x = 0; x < Width; x++)
		{
			for (int y = 0; y < Height; y++)
			{
				_tiles[x, y] = true;
			}
		}

		for (int x = 0; x < Width; x++)
		{
			for (int y = 0; y < Height; y++)
			{
				_tiles[x, y] = true;
			}
		}

		QueueRedraw();
	}

	public override void _Draw()
	{
		DrawRect(
			new Rect2(0, 0, Width * TileSize, Height * TileSize),
			Colors.DarkGoldenrod
		);

		for (int x = 0; x <= Width; x++)
		{
			float xPosition = x * TileSize;

			DrawLine(
				new Vector2(xPosition, 0),
				new Vector2(xPosition, Height * TileSize),
				Colors.Black,
				2.0f
			);
		}

		for (int y = 0; y <= Height; y++)
		{
			float yPosition = y * TileSize;

			DrawLine(
				new Vector2(0, yPosition),
				new Vector2(Width * TileSize, yPosition),
				Colors.Black,
				2.0f
			);
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is not InputEventMouseButton mouseEvent)
			return;

		if (mouseEvent.ButtonIndex != MouseButton.Left)
			return;

		if (!mouseEvent.Pressed)
			return;

		Vector2 mousePosition = GetGlobalMousePosition();

		int x = Mathf.FloorToInt(mousePosition.X / TileSize);
		int y = Mathf.FloorToInt(mousePosition.Y / TileSize);

		if (x < 0 || x >= Width || y < 0 || y >= Height)
			return;

		_tiles[x, y] = !_tiles[x, y];

		QueueRedraw();
	}
}
