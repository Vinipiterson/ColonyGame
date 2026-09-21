using Godot;
using System;

[GlobalClass]
public partial class GridWorld : Node
{
    private const string TestBuildingScenePath = "res://Scenes/Buildings/TestBuilding.tscn";
    private PackedScene _testBuildingScene;
    
    private const string TestBuildingDefinitionPath ="res://Resources/Buildings/TestBuildingDefinition.tres";
    private BuildingDefinition _testBuildingDefinition;

    [Export]
    public float TileSize { get; private set; } = 32;
    [Export]
    public int Width { get; private set; } = 40;
    [Export]
    public int Height { get; private set; } = 25;

    private TileType[,] _tiles;

    public StructureGrid Structures { get; private set; }

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

        Structures = new StructureGrid();
        AddChild(Structures);

        _testBuildingScene =
        GD.Load<PackedScene>(
            TestBuildingScenePath
        );
        _testBuildingDefinition =
        GD.Load<BuildingDefinition>(
            TestBuildingDefinitionPath
        );

        GenerateWorld();
    }

    private void GenerateWorld()
    {
            // Fill the entire world with dirt.
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                _tiles[x, y] = TileType.Dirt;
            }
        }

        // 7x2 upper room.
        CreateEmptyRoom(
            width: 7,
            height: 2,
            centerX: Width / 2,
            startY: 10);

        // 11x2 lower room.
        CreateEmptyRoom(
            width: 11,
            height: 2,
            centerX: Width / 2,
            startY: 12);

        /*
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
        */
    }

    private void CreateEmptyRoom(
    int width,
    int height,
    int centerX,
    int startY)
    {
        int startX =
            centerX - width / 2;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2I position =
                    new Vector2I(
                        startX + x,
                        startY + y);

                if (IsInside(position))
                    _tiles[position.X, position.Y] =
                        TileType.Empty;
            }
        }
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

    public bool HasWorkExposure(Vector2I target)
    {
        Vector2I[] directions =
        {
            Vector2I.Left,
            Vector2I.Right,
            Vector2I.Up,
            Vector2I.Down
        };

        foreach (Vector2I direction in directions)
        {
            Vector2I workTile = target + direction;

            if (!IsInside(workTile))
                continue;

            if (GridMovementRules.CanStand(this, workTile))
                return true;
        }

        return false;
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

    public void PlaceTestBuilding(Vector2 worldPosition)
    {
        Vector2I gridPosition =
            WorldToGrid(worldPosition);

        if (!Structures.CanPlaceBuilding(
                _testBuildingDefinition,
                gridPosition))
        {
            GD.Print(
                $"Cannot place building at {gridPosition}"
            );

            return;
        }

        GameServices.GetWorkOrderManager().CreateBuildOrder(_testBuildingDefinition, _testBuildingScene, gridPosition);
    }
}