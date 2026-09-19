using Godot;
using System.Collections.Generic;

/*
 * WorkOrderManager is now the brains of the assignment flow:
 *
 *     chooses order -> chooses worker -> chooses work position
 *     -> chooses path -> hands the assignment to the Worker
 *
 * Workers no longer pick their own orders or work positions;
 * they only execute what they're handed.
 */
 [GlobalClass]
public partial class WorkOrderManager : Node
{
    // Work-position search tuning (moved here from Colonist).
    private const int HorizontalRange = 2;
    private const int VerticalRange = 2;

    private const float HorizontalWeight = 10.0f;
    private const float VerticalWeight = 1.0f;
    private const float PathWeight = 0.25f;

    private readonly List<WorkOrder> _workOrders = new();

    private readonly List<Worker> _workers = new();

    private GridWorld _world;
    private GridPathFinder _pathfinder;

    public IReadOnlyList<WorkOrder> WorkOrders => _workOrders;

    private struct WorkAssignment
    {
        public WorkOrder Order;
        public Vector2I WorkPosition;
        public List<PathStep> Path;
    }


    // ------------------------------------------------
    // Initialization
    // ------------------------------------------------

    public override void _EnterTree()
    {
        AddToGroup("WorkOrderManager");
    }

    public override void _Ready()
    {
        _world = GameServices.GetGridWorld();
        _pathfinder = GameServices.GetGridPathfinder();
    }

    public void RegisterWorker(Worker worker)
    {
        _workers.Add(worker);
    }


    // ------------------------------------------------
    // Process
    // ------------------------------------------------

    public override void _Process(double delta)
    {
        AssignIdleWorkers();
        HandleEmergencyOrders();
    }


    // ------------------------------------------------
    // Order Creation / Completion
    // ------------------------------------------------

    public WorkOrder CreateDigOrder(Vector2I tilePosition, int priority = 5)
    {
        if (!_world.IsInside(tilePosition))
            return null;

        if (_world.GetTile(tilePosition) !=
            TileType.Dirt)
        {
            return null;
        }

        // Prevent duplicate orders on the same tile.
        foreach (WorkOrder existingOrder in _workOrders)
        {
            if (existingOrder.TilePosition == tilePosition)
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

    public IReadOnlyList<WorkOrder> GetAvailableOrders()
    {
        return _workOrders;
    }

    public void CompleteOrder( WorkOrder order)
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

    public void RemoveOrder(WorkOrder order)
    {
        _workOrders.Remove(order);
    }


    // ------------------------------------------------
    // Assignment
    // ------------------------------------------------

    private void AssignIdleWorkers()
    {
        foreach (Worker worker in _workers)
        {
            if (!worker.IsIdle)
                continue;

            WorkAssignment? assignment = FindBestAssignmentFor(worker, minimumPriority: 1);

            if (!assignment.HasValue)
                continue;

            TryAssignOrderToWorker(assignment.Value, worker);
        }
    }

    private void HandleEmergencyOrders()
    {
        foreach (Worker worker in _workers)
        {
            // A worker mid-climb or mid-fall is physically
            // committed and cannot be redirected.
            if (!worker.CanBeInterrupted)
                continue;

            // An active priority 10 order cannot be
            // interrupted by another priority 10.
            if (worker.CurrentOrder != null &&
                GetEffectivePriority(worker, worker.CurrentOrder) >= 10)
            {
                continue;
            }

            WorkAssignment? assignment = FindBestAssignmentFor(worker, minimumPriority: 10);

            if (!assignment.HasValue)
                continue;

            if (assignment.Value.Order ==
                worker.CurrentOrder)
            {
                continue;
            }

            worker.CancelCurrentWork();

            TryAssignOrderToWorker(assignment.Value, worker);
        }
    }

    private void TryAssignOrderToWorker(WorkAssignment assignment, Worker worker)
    {
        if (!assignment.Order.TryClaim(worker))
            return;

        worker.AssignWork(assignment.Order, assignment.WorkPosition, assignment.Path);
    }

    private WorkAssignment? FindBestAssignmentFor(Worker worker, int minimumPriority)
    {
        WorkAssignment? best = null;

        int bestPriority = 0;
        int bestPathCost = int.MaxValue;

        foreach (WorkOrder order in _workOrders)
        {
            if (order.IsClaimed)
                continue;

            int priority = GetEffectivePriority(worker, order);

            if (priority < minimumPriority)
                continue;

            Vector2I? workPosition = FindWorkPosition(worker, order);

            // No valid/reachable work position - treat this
            // order as unavailable to this worker.
            if (!workPosition.HasValue)
                continue;

            List<PathStep> path = _pathfinder.FindPath(worker.GridPosition, workPosition.Value);

            int pathCost = worker.GridPosition == workPosition.Value ? 0 : path.Count;

            bool isBetter = best == null || priority > bestPriority || (priority == bestPriority && pathCost < bestPathCost);

            if (!isBetter)
                continue;

            best = new WorkAssignment {Order = order, WorkPosition = workPosition.Value, Path = path};

            bestPriority = priority;
            bestPathCost = pathCost;
        }

        return best;
    }


    // ------------------------------------------------
    // Priority
    // ------------------------------------------------

    private int GetEffectivePriority(Worker worker, WorkOrder order)
    {
        int priority = order.Priority + worker.GetPriorityModifier(order.Type);

        return Mathf.Clamp(priority, 1, 10);
    }


    // ------------------------------------------------
    // Work Position
    // ------------------------------------------------

    // Used by Main for the "unreachable order" debug outline -
    // true if any registered worker could reach this order.
    public bool HasReachableWorkPosition(WorkOrder order)
    {
        foreach (Worker worker in _workers)
        {
            if (FindWorkPosition(worker, order).HasValue)
                return true;
        }

        return false;
    }

    private Vector2I? FindWorkPosition(
        Worker worker,
        WorkOrder order)
    {
        Vector2I target = order.TilePosition;

        Vector2I? bestPosition = null;

        float bestScore = float.MaxValue;

        for (int xOffset = -HorizontalRange; xOffset <= HorizontalRange; xOffset++)
        {
            for (int yOffset = -VerticalRange; yOffset <= VerticalRange; yOffset++)
            {
                Vector2I workPosition =
                    new Vector2I(
                        target.X + xOffset,
                        target.Y + yOffset
                    );

                if (!CanSafelyWorkFrom(
                        worker,
                        workPosition,
                        target))
                {
                    continue;
                }

                List<PathStep> path =
                    _pathfinder.FindPath(
                        worker.GridPosition,
                        workPosition
                    );

                if (worker.GridPosition !=
                        workPosition &&
                    path.Count == 0)
                {
                    continue;
                }

                float horizontalDistance =
                    Mathf.Abs(
                        workPosition.X -
                        target.X
                    );

                float verticalDistance =
                    Mathf.Abs(
                        workPosition.Y -
                        worker.GridPosition.Y
                    );

                float pathDistance =
                    worker.GridPosition ==
                        workPosition
                        ? 0.0f
                        : path.Count;

                float score =
                    horizontalDistance *
                    HorizontalWeight +

                    verticalDistance *
                    VerticalWeight +

                    pathDistance *
                    PathWeight;

                if (score >= bestScore)
                    continue;

                bestScore =
                    score;

                bestPosition =
                    workPosition;
            }
        }

        return bestPosition;
    }

    private bool CanSafelyWorkFrom(
        Worker worker,
        Vector2I workPosition,
        Vector2I target)
    {
        if (!_world.CanStand(
                workPosition))
        {
            return false;
        }

        // The tile supporting the worker must not be the
        // tile being destroyed.
        Vector2I ground =
            workPosition + Vector2I.Down;

        if (ground == target)
            return false;

        List<PathStep> path =
            _pathfinder.FindPath(
                worker.GridPosition,
                workPosition
            );

        return worker.GridPosition ==
                   workPosition ||
               path.Count > 0;
    }
}