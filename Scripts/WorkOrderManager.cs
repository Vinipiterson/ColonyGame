using Godot;
using System.Collections.Generic;

public class WorkOrderManager
{
    private readonly GridWorld _world;

    private readonly List<WorkOrder> _workOrders =
        new();

    public IReadOnlyList<WorkOrder> WorkOrders =>
        _workOrders;

    public WorkOrderManager(
        GridWorld world)
    {
        _world = world;
    }

    public WorkOrder CreateDigOrder(
        Vector2I tilePosition,
        int priority = 5)
    {
        if (!_world.IsInside(tilePosition))
            return null;

        if (_world.GetTile(tilePosition) !=
            TileType.Dirt)
        {
            return null;
        }

        // Prevent duplicate orders on the same tile.
        foreach (WorkOrder existingOrder
                 in _workOrders)
        {
            if (existingOrder.TilePosition ==
                tilePosition)
            {
                return null;
            }
        }

        var order =
            new WorkOrder(
                WorkOrderType.Dig,
                tilePosition,
                0.5f,
                priority
            );

        _workOrders.Add(order);

        return order;
    }

    public IReadOnlyList<WorkOrder>
        GetAvailableOrders()
    {
        return _workOrders;
    }

    public void CompleteOrder(
        WorkOrder order)
    {
        if (!_workOrders.Contains(order))
            return;

        switch (order.Type)
        {
            case WorkOrderType.Dig:

                _world.SetTile(
                    order.TilePosition,
                    TileType.Empty
                );

                break;

            case WorkOrderType.Build:

                // Building implementation later.

                break;
        }

        _workOrders.Remove(order);
    }

    public void RemoveOrder(
        WorkOrder order)
    {
        _workOrders.Remove(order);
    }
}