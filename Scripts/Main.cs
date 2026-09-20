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
    private int _nextOrderPriority = 5;
    private readonly Vector2I ColonistStart =
        new Vector2I(32, 16);

    // World
    private GridWorld _world;
    private GridPathFinder _pathfinder;
    private WorkOrderManager _workOrderManager;
    
    public override void _Ready()
    {
        _world = GameServices.GetGridWorld();
        _pathfinder = GameServices.GetGridPathfinder();
        _workOrderManager = GameServices.GetWorkOrderManager();

        for (int i=0; i<2; i++)
        {
            Colonist colonist =new Colonist();
            AddChild(colonist);

            colonist.Initialize(ColonistStart);
            colonist.OnWorkStateChanged += QueueRedraw;

            _workOrderManager.RegisterWorker(colonist.Worker);
        }

        QueueRedraw();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
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

        if (@event is not InputEventMouseButton mouseEvent)
        {
            return;
        }

        if (!mouseEvent.Pressed)
            return;

        Vector2 mousePosition = GetGlobalMousePosition();

        Vector2I tilePosition =
            new Vector2I(
                Mathf.FloorToInt(
                    mousePosition.X /
                    _world.TileSize
                ),

                Mathf.FloorToInt(
                    mousePosition.Y /
                    _world.TileSize
                )
            );

        if (!_world.IsInside(tilePosition))
        {
            return;
        }

        // RMB = place concrete immediately.
        if (mouseEvent.ButtonIndex ==
            MouseButton.Right)
        {
            _world.SetTile(
                tilePosition,
                TileType.Dirt
            );

            QueueRedraw();
            return;
        }

        // LMB.
        if (mouseEvent.ButtonIndex == MouseButton.Left)
        {
            // Dirt = create Dig order.
            if (_world.GetTile(tilePosition) == TileType.Dirt)
            {
                WorkOrder order =
                    _workOrderManager.CreateDigOrder(tilePosition, _nextOrderPriority);

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
        }
    }

    public override void _Draw()
    {
        DrawTiles();
        //DrawGrid();
        DrawWorkOrders();
    }

    private void DrawTiles()
    {
        for (int x = 0;
             x < _world.Width;
             x++)
        {
            for (int y = 0;
                 y < _world.Height;
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
                        x * _world.TileSize,
                        y * _world.TileSize,
                        _world.TileSize,
                        _world.TileSize
                    ),
                    tileColor
                );
            }
        }
    }

    private void DrawGrid()
    {
        for (int x = 0;
             x <= _world.Width;
             x++)
        {
            float xPosition =
                x * _world.TileSize;

            DrawLine(
                new Vector2(
                    xPosition,
                    0
                ),

                new Vector2(
                    xPosition,
                    _world.Height * _world.TileSize
                ),

                Colors.Black,
                2.0f
            );
        }

        for (int y = 0;
             y <= _world.Height;
             y++)
        {
            float yPosition =
                y * _world.TileSize;

            DrawLine(
                new Vector2(
                    0,
                    yPosition
                ),

                new Vector2(
                    _world.Width * _world.TileSize,
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
                tile.X * _world.TileSize,
                tile.Y * _world.TileSize,
                _world.TileSize,
                _world.TileSize
            );

    Color priorityColor;

    if (!order.IsClaimed &&
        !_workOrderManager.HasReachableWorkPosition(order))
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
        // order has been claimed by a worker.
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
}