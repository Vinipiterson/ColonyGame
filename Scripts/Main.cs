using Godot;
using System.Collections.Generic;

public enum TileType
{
    Empty,
    Dirt,
    Concrete
}

public partial class Main : Node2D
{
    private const int Width = 40;
    private const int Height = 25;
    private const int TileSize = 32;

    private int _nextOrderPriority = 5;
    private readonly Vector2I PathDebugStart =
        new Vector2I(32, 16);

    private List<PathStep> _path =
        new();

    // World
    private GridWorld _world;
    private GridPathfinder _pathfinder;

    private TileType[,] _tiles =
        new TileType[Width, Height];

    // Colony
    private Colonist _colonist;

    private WorkOrderManager _workOrderManager;

    public override void _Ready()
    {
        GD.Print("MAIN IS RUNNING");

        GenerateWorld();

        _world =
            new GridWorld(_tiles);

        _pathfinder =
            new GridPathfinder(_world);

        _workOrderManager =
            new WorkOrderManager(_world);

        _colonist =
            new Colonist();

        AddChild(_colonist);

        _colonist.OnWorkStateChanged += QueueRedraw;

        _colonist.Initialize(
            PathDebugStart,
            _world,
            _pathfinder,
            _workOrderManager
        );

        QueueRedraw();
    }

    private void GenerateWorld()
    {
        for (int x = 0;
             x < Width;
             x++)
        {
            for (int y = 0;
                 y < Height;
                 y++)
            {
                _tiles[x, y] =
                    y >= 17
                        ? TileType.Dirt
                        : TileType.Empty;
            }
        }

        // Your world test presets.

        // WorldPresets.AddConcreteRow(_tiles);
        // WorldPresets.AddConcreteWall2(_tiles);
        // WorldPresets.AddConcreteWall3(_tiles);

        // WorldPresets.AddOneTileHole(_tiles);
        // WorldPresets.AddTwoTileHole(_tiles);
        // WorldPresets.AddThreeTileHole(_tiles);
    }

    public override void _Input(
        InputEvent @event)
    {
        if (@event is InputEventKey keyEvent &&
            keyEvent.Pressed &&
            !keyEvent.Echo)
        {
            switch (keyEvent.Keycode)
            {
                case Key.Key1:
                    _nextOrderPriority = 1;
                    break;

                case Key.Key2:
                    _nextOrderPriority = 2;
                    break;

                case Key.Key3:
                    _nextOrderPriority = 3;
                    break;

                case Key.Key4:
                    _nextOrderPriority = 4;
                    break;

                case Key.Key5:
                    _nextOrderPriority = 5;
                    break;

                case Key.Key6:
                    _nextOrderPriority = 6;
                    break;

                case Key.Key7:
                    _nextOrderPriority = 7;
                    break;

                case Key.Key8:
                    _nextOrderPriority = 8;
                    break;

                case Key.Key9:
                    _nextOrderPriority = 9;
                    break;

                case Key.Key0:
                    _nextOrderPriority = 10;
                    break;
            }

            return;
        }

        if (@event is not
            InputEventMouseButton mouseEvent)
        {
            return;
        }

        if (!mouseEvent.Pressed)
            return;

        Vector2 mousePosition =
            GetGlobalMousePosition();

        Vector2I tilePosition =
            new Vector2I(
                Mathf.FloorToInt(
                    mousePosition.X /
                    TileSize
                ),

                Mathf.FloorToInt(
                    mousePosition.Y /
                    TileSize
                )
            );

        if (!_world.IsInside(
                tilePosition))
        {
            return;
        }

        // RMB = place concrete immediately.
        if (mouseEvent.ButtonIndex ==
            MouseButton.Right)
        {
            _world.SetTile(
                tilePosition,
                TileType.Concrete
            );

            QueueRedraw();
            return;
        }

        // LMB.
        if (mouseEvent.ButtonIndex ==
            MouseButton.Left)
        {
            // Dirt = create Dig order.
            if (_world.GetTile(
                    tilePosition) ==
                TileType.Dirt)
            {
                WorkOrder order =
                    _workOrderManager.CreateDigOrder(
                        tilePosition,
                        _nextOrderPriority
                    );

                if (order != null)
                {
                    GD.Print(
                        $"Created Dig order at {tilePosition} " +
                        $"with priority {order.Priority}"
                    );
                }

                QueueRedraw();
                return;
            }

            // Empty = pathfinding debug.
            if (_world.GetTile(
                    tilePosition) ==
                TileType.Empty)
            {
                DebugFindPath(
                    tilePosition
                );
            }
        }
    }

    private void DebugFindPath(
        Vector2I target)
    {
        if (_world.GetTile(target) !=
            TileType.Empty)
        {
            GD.Print(
                $"Path target {target} is not empty."
            );

            return;
        }

        Vector2I? standableTarget =
            _world.GetStandableTileBelow(
                target
            );

        if (!standableTarget.HasValue)
        {
            GD.Print(
                $"No standable tile below {target}."
            );

            _path.Clear();

            QueueRedraw();

            return;
        }

        target =
            standableTarget.Value;

        _path =
            _pathfinder.FindPath(
                PathDebugStart,
                target
            );

        GD.Print(
            $"Path: {PathDebugStart} -> {target}"
        );

        GD.Print(
            $"Path length: {_path.Count}"
        );

        if (_path.Count == 0)
        {
            GD.Print("No path found.");

            QueueRedraw();

            return;
        }

        foreach (PathStep step in _path)
        {
            GD.Print(
                $"{step.Position} - " +
                $"{step.MovementType}"
            );
        }

        _colonist.SetPath(
            _path
        );

        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawTiles();
        DrawGrid();
        DrawWorkOrders();
        DrawPath();
        DrawPathDebugStart();
    }

    private void DrawTiles()
    {
        for (int x = 0;
             x < Width;
             x++)
        {
            for (int y = 0;
                 y < Height;
                 y++)
            {
                Vector2I position =
                    new Vector2I(
                        x,
                        y
                    );

                TileType tileType =
                    _world.GetTile(
                        position
                    );

                Color tileColor =
                    tileType switch
                    {
                        TileType.Empty =>
                            Colors.LightSkyBlue,

                        TileType.Dirt =>
                            Colors.DarkGoldenrod,

                        TileType.Concrete =>
                            Colors.LightGray,

                        _ =>
                            Colors.Magenta
                    };

                DrawRect(
                    new Rect2(
                        x * TileSize,
                        y * TileSize,
                        TileSize,
                        TileSize
                    ),
                    tileColor
                );
            }
        }
    }

    private void DrawGrid()
    {
        for (int x = 0;
             x <= Width;
             x++)
        {
            float xPosition =
                x * TileSize;

            DrawLine(
                new Vector2(
                    xPosition,
                    0
                ),

                new Vector2(
                    xPosition,
                    Height * TileSize
                ),

                Colors.Black,
                2.0f
            );
        }

        for (int y = 0;
             y <= Height;
             y++)
        {
            float yPosition =
                y * TileSize;

            DrawLine(
                new Vector2(
                    0,
                    yPosition
                ),

                new Vector2(
                    Width * TileSize,
                    yPosition
                ),

                Colors.Black,
                2.0f
            );
        }
    }

    private void DrawWorkOrders()
{
    foreach (WorkOrder order
             in _workOrderManager.WorkOrders)
    {
        Vector2 tile =
            order.TilePosition;

        Rect2 rect =
            new Rect2(
                tile.X * TileSize,
                tile.Y * TileSize,
                TileSize,
                TileSize
            );

    Color priorityColor;

    if (!order.IsClaimed &&
        !_colonist.HasReachableWorkPosition(order))
    {
        priorityColor =
            Colors.Magenta;
    }
    else
    {
        priorityColor =
            GetPriorityColor(
                order.Priority
            );
    }

        // Priority is represented by the
        // outline color.
        DrawRect(
            rect,
            priorityColor,
            false,
            2.0f
        );

        // A small circle means that the
        // order has been claimed by a colonist.
        if (order.IsClaimed)
        {
            Vector2 center =
                rect.Position +
                rect.Size / 2.0f;

            DrawCircle(
                center,
                3.0f,
                priorityColor
            );
        }
    }
}

    private Color GetPriorityColor(
        int priority)
    {
        priority =
            Mathf.Clamp(
                priority,
                1,
                10
            );

        /*
        * Priority 1:
        *     Blue
        *
        * Priority 5:
        *     Yellow
        *
        * Priority 10:
        *     Red
        *
        * This gives us an easy visual gradient
        * without needing a UI yet.
        */

        float normalized =
            (priority - 1) / 9.0f;

        if (normalized < 0.5f)
        {
            float t =
                normalized * 2.0f;

            return Colors.Blue.Lerp(
                Colors.Yellow,
                t
            );
        }

        float highT =
            (normalized - 0.5f) * 2.0f;

        return Colors.Yellow.Lerp(
            Colors.Red,
            highT
        );
    }

    private void DrawPath()
    {
        foreach (PathStep step
                 in _path)
        {
            Vector2 position =
                new Vector2(
                    step.Position.X *
                        TileSize +
                        TileSize / 2.0f,

                    step.Position.Y *
                        TileSize +
                        TileSize / 2.0f
                );

            DrawCircle(
                position,
                10.0f,
                Colors.Red
            );
        }
    }

    private void DrawPathDebugStart()
    {
        Vector2 position =
            new Vector2(
                PathDebugStart.X *
                    TileSize +
                    TileSize / 2.0f,

                PathDebugStart.Y *
                    TileSize +
                    TileSize / 2.0f
            );

        DrawCircle(
            position,
            14.0f,
            Colors.Blue,
            false,
            3.0f
        );
    }
}