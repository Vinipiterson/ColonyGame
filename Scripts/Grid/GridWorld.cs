using Godot;
using System;

[GlobalClass]
public partial class GridWorld : Node
{
    [Export]
    public float TileSize { get; private set; } = 32;
    [Export]
    public int Width { get; private set; } = 40;
    [Export]
    public int Height { get; private set; } = 25;

    private TileType[,] _tiles;

    // Fired whenever a tile's value actually changes (not on a
    // no-op SetTile to the same type). WorkOrderManager listens
    // to this to know when an in-progress path might have been
    // invalidated - e.g. a bridge tile getting dug out from
    // under a colonist who's mid-walk toward a different order.
    public event Action<Vector2I> TileChanged;

    public override void _EnterTree()
    {
        AddToGroup("GridWorld");
    }

    public override void _Ready()
    {
        _tiles = new TileType[Width, Height];

        GenerateWorld();
    }

    private void GenerateWorld()
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                _tiles[x, y] =
                    y >= 17
                        ? TileType.Dirt
                        : TileType.Empty;
            }
        }

        // Your world test presets.

        // WorldPresets.AddConcreteRow(_tiles);
        WorldPresets.AddConcreteWall2(_tiles);
        // WorldPresets.AddConcreteWall3(_tiles);

        // WorldPresets.AddOneTileHole(_tiles);
        // WorldPresets.AddTwoTileHole(_tiles);
        // WorldPresets.AddThreeTileHole(_tiles);
    }

    public bool IsInside(Vector2I position)
    {
        return position.X >= 0 &&
               position.X < Width &&
               position.Y >= 0 &&
               position.Y < Height;
    }

    public TileType GetTile(Vector2I position)
    {
        if (!IsInside(position))
            return TileType.Empty;

        return _tiles[position.X, position.Y];
    }

    public void SetTile(Vector2I position, TileType type)
    {
        if (!IsInside(position))
            return;

        if (_tiles[position.X, position.Y] == type)
            return;

        _tiles[position.X, position.Y] = type;

        TileChanged?.Invoke(position);
    }

    public bool IsSolid(Vector2I position)
    {
        if (!IsInside(position))
            return false;

        return GetTile(position) != TileType.Empty;
    }

    public Vector2I WorldToGrid(Vector2 worldPosition)
    {
        return new Vector2I(
            Mathf.FloorToInt(worldPosition.X / TileSize),
            Mathf.FloorToInt(worldPosition.Y / TileSize)
        );
    }

    public Vector2 GridToWorld(Vector2I gridPosition)
    {
        return new Vector2(
            Mathf.RoundToInt(gridPosition.X * TileSize),
            Mathf.RoundToInt(gridPosition.Y * TileSize)
        );
    }
}