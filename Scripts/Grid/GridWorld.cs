using Godot;

[GlobalClass]
public partial class GridWorld : Node
{
    [Export]
    public float TileSize {get; private set; } = 32;
    [Export]
    public int Width { get; private set; } = 40;
    [Export]
    public int Height { get; private set; } = 25;

    private TileType[,] _tiles;

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

        return _tiles[
            position.X,
            position.Y
        ];
    }

    public void SetTile(Vector2I position, TileType type)
    {
        if (!IsInside(position))
            return;

        _tiles[
            position.X,
            position.Y
        ] = type;
    }

    public bool IsSolid(Vector2I position)
    {
        if (!IsInside(position))
            return false;

        return GetTile(position) != TileType.Empty;
    }

    public bool CanStand(Vector2I position)
    {
        if (!IsInside(position))
            return false;

        // Body must be empty.
        if (IsSolid(position))
            return false;

        // Head must be empty.
        Vector2I head = position + Vector2I.Up;

        if (!IsInside(head))
            return false;

        if (IsSolid(head))
            return false;

        // Ground must be solid.
        Vector2I below = position + Vector2I.Down;

        if (!IsInside(below))
            return false;

        return IsSolid(below);
    }

    public bool CanWalkTo(Vector2I from, Vector2I to)
    {
        if (!IsInside(from) || !IsInside(to))
        {
            return false;
        }

        if (from.Y != to.Y)
            return false;

        if (Mathf.Abs(from.X - to.X) != 1)
        {
            return false;
        }

        return CanStand(to);
    }

    public Vector2I? GetClimbDestination(
        Vector2I from,
        Vector2I to)
    {
        if (!IsInside(from) || !IsInside(to))
        {
            return null;
        }

        // Must move exactly one tile horizontally.
        if (Mathf.Abs(from.X - to.X) != 1)
        {
            return null;
        }

        // Only climb upward.
        if (to.Y >= from.Y)
            return null;

        int climbHeight =
            from.Y - to.Y;

        // Maximum climb height = 2.
        if (climbHeight < 1 || climbHeight > 2)
        {
            return null;
        }

        // ------------------------------------------------
        // Check vertical climbing space.
        //
        // The colonist stays on his current X while
        // climbing upward.
        // ------------------------------------------------

        for (int height = 1; height <= climbHeight; height++)
        {
            Vector2I bodyPosition = new Vector2I(from.X, from.Y - height);

            Vector2I headPosition = bodyPosition + Vector2I.Up;

            if (!IsInside(bodyPosition) || !IsInside(headPosition))
            {
                return null;
            }

            if (IsSolid(bodyPosition) || IsSolid(headPosition))
            {
                return null;
            }
        }

        // ------------------------------------------------
        // Check the final horizontal step.
        // ------------------------------------------------

        Vector2I climbPosition =
            new Vector2I(from.X, to.Y);

        Vector2I climbHead = climbPosition + Vector2I.Up;

        if (IsSolid(climbPosition) || IsSolid(climbHead))
        {
            return null;
        }

        // Final destination must be standable.
        if (!CanStand(to))
            return null;

        return to;
    }

    public Vector2I? GetFallDestination(
        Vector2I position)
    {
        if (!IsInside(position))
            return null;

        if (IsSolid(position))
            return null;

        Vector2I current = position + Vector2I.Down;

        while (IsInside(current))
        {
            Vector2I below = current + Vector2I.Down;

            if (!IsInside(below))
                return null;

            if (IsSolid(below))
            {
                if (CanStand(current))
                    return current;
            }

            current += Vector2I.Down;
        }

        return null;
    }

    public Vector2I? GetStandableTileBelow(
        Vector2I position)
    {
        if (!IsInside(position))
            return null;

        Vector2I current = position;

        while (IsInside(current))
        {
            if (CanStand(current))
                return current;

            current += Vector2I.Down;
        }

        return null;
    }

    public Vector2I WorldToGrid(Vector2 worldPosition)
    {
        return new Vector2I(
            Mathf.FloorToInt(
                worldPosition.X / TileSize
            ),
            Mathf.FloorToInt(
                worldPosition.Y / TileSize
            )
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

