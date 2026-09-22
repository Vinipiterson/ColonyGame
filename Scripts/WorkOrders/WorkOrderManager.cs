using Godot;
using System.Collections.Generic;

/*
 * WorkOrderManager is now the brains of the assignment flow:
 *
 *     chooses order -> chooses worker -> chooses work position
 *     -> chooses path -> hands the assignment to the Worker
 *
 * Workers no longer pick their own orders or work positions;
 * they only execute what they're handed. WorkOrderManager also
 * listens for world changes and re-checks any worker who is
 * currently walking toward an order, in case the path they're
 * committed to has been broken out from under them (e.g. a
 * bridge tile getting dug by someone else mid-walk).
 */
[GlobalClass]
public partial class WorkOrderManager : Node
{
    // Work-position search range (moved here from Colonist).
    private const int HorizontalRange = 2;
    private const int VerticalRange = 2;

    private readonly List<WorkOrder> _workOrders = new();

    private readonly List<Worker> _workers = new();

    // Workers whose path might have been invalidated by a
    // recent tile change and still need to be re-checked.
    // A worker stays in this set across frames if it was
    // physically committed (mid-climb/mid-fall) when the tile
    // changed, until it's free to be safely interrupted.
    private readonly HashSet<Worker> _pendingRevalidation = new();

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

        _world.TileChanged += HandleTileChanged;
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
        RevalidatePendingWorkers();
        AssignIdleWorkers();
        HandleEmergencyOrders();
    }


    // ------------------------------------------------
    // Path Revalidation
    // ------------------------------------------------

    private void HandleTileChanged(Vector2I position)
    {
        foreach (Worker worker in _workers)
        {
            if (worker.State != WorkerState.MovingToWork)
                continue;

            _pendingRevalidation.Add(worker);
        }
    }

    private void RevalidatePendingWorkers()
    {
        if (_pendingRevalidation.Count == 0)
            return;

        // Snapshot - CancelCurrentWork() below can trigger
        // reassignment logic elsewhere, so don't mutate the
        // set we're iterating.
        var toCheck = new List<Worker>(_pendingRevalidation);

        foreach (Worker worker in toCheck)
        {
            // No longer relevant - finished, cancelled, or
            // reassigned since the tile change.
            if (worker.State != WorkerState.MovingToWork)
            {
                _pendingRevalidation.Remove(worker);
                continue;
            }

            // Mid-climb/mid-fall - leave it queued and check
            // again once it's free to be interrupted.
            if (!worker.CanBeInterrupted)
                continue;

            _pendingRevalidation.Remove(worker);

            Vector2I? workPosition = worker.WorkPosition;

            if (!workPosition.HasValue)
                continue;

            if (worker.GridPosition == workPosition.Value)
                continue;

            List<PathStep> path = _pathfinder.FindPath(worker.GridPosition, workPosition.Value);

            if (path.Count == 0)
            {
                // Path is broken. Release the order and go
                // idle - AssignIdleWorkers() will pick the next
                // best reachable order (possibly this same one
                // from a different angle) next frame.
                worker.CancelCurrentWork();
            }
        }
    }


    // ------------------------------------------------
    // Order Creation / Completion
    // ------------------------------------------------

    public WorkOrder CreateDigOrder(Vector2I tilePosition, int priority = 5)
    {
        if (!_world.IsInside(tilePosition))
            return null;

        if (_world.GetTile(tilePosition) != TileType.Dirt)
            return null;

        // Prevent duplicate orders on the same tile.
        foreach (WorkOrder existingOrder in _workOrders)
        {
            if (existingOrder.TilePosition == tilePosition)
                return null;
        }

        var order = new WorkOrder(WorkOrderType.Dig, tilePosition, 0.5f, priority);

        _workOrders.Add(order);

        return order;
    }

    public WorkOrder CreateBuildOrder(BuildingDefinition definition, PackedScene scene, Vector2I tilePosition, int priority = 5)
    {
        if (definition == null)
            return null;

        Building building = scene.Instantiate<Building>();
        building.Initialize(definition, tilePosition, _world.GridToWorld(tilePosition), BuildingState.UnderConstruction);
        AddChild(building);

        if (!GameServices.GetStructureGrid().RegisterBuilding(building))
        {
            building.QueueFree();

            GD.Print(
                $"Failed to register building at {tilePosition}"
            );

            return null;
        }
        else
        {
            GD.Print($"Placed building at {tilePosition}");   
        }

        // Prevent duplicate orders on the same tile.
        foreach (WorkOrder existingOrder in _workOrders)
        {
            if (existingOrder.Type == WorkOrderType.Build && existingOrder.Building == building)
                return null;
        }

        var order = new WorkOrder(building, building.Definition.WorkRequired, priority);

        _workOrders.Add(order);

        return order;
    }

    public IReadOnlyList<WorkOrder> GetAvailableOrders()
    {
        return _workOrders;
    }

    public void CompleteOrder(WorkOrder order)
    {
        if (!_workOrders.Contains(order))
            return;

        switch (order.Type)
        {
            case WorkOrderType.Dig:
                _world.SetTile(order.TilePosition, TileType.Empty);
                break;

            case WorkOrderType.Build:
                if (order.Building != null)
                {
                    order.Building.CompleteConstruction();
                }
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

            if (assignment.Value.Order == worker.CurrentOrder)
                continue;

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

            best = new WorkAssignment { Order = order, WorkPosition = workPosition.Value, Path = path };

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

    private Vector2I? FindWorkPosition(Worker worker, WorkOrder order)
    {
        if (order.Type == WorkOrderType.Build)
            return FindBuildWorkPosition(worker, order);

        return FindDigWorkPosition(worker, order);
    }

    private Vector2I? FindDigWorkPosition(Worker worker, WorkOrder order)
    {
        Vector2I target = order.TilePosition;

        Vector2I? bestPosition = null;
        int bestPathCost = int.MaxValue;

        for (int xOffset = -HorizontalRange; xOffset <= HorizontalRange; xOffset++)
        {
            for (int yOffset = -VerticalRange; yOffset <= VerticalRange; yOffset++)
            {
                Vector2I workPosition =
                    new Vector2I(
                        target.X + xOffset,
                        target.Y + yOffset);

                if (!CanSafelyWorkFrom(
                        worker,
                        workPosition,
                        target))
                {
                    continue;
                }

                int pathCost =
                    GetPathCostTo(
                        worker,
                        workPosition);

                if (pathCost < 0)
                    continue;

                if (pathCost >= bestPathCost)
                    continue;

                bestPathCost = pathCost;
                bestPosition = workPosition;
            }
        }

        if (bestPosition.HasValue)
            return bestPosition;

        return FindOnTopWorkPosition(
            worker,
            target);
    }

        private Vector2I? FindBuildWorkPosition(Worker worker, WorkOrder order)
    {
        Building building = order.Building;

        if (building == null)
            return null;

        Vector2I origin = building.GridPosition;
        Vector2I size = building.Definition.Size;

        Vector2I? bestPosition = null;
        int bestPathCost = int.MaxValue;

        // Top and bottom edges.
        for (int x = 0; x < size.X; x++)
        {
            CheckBuildWorkPosition(
                worker,
                new Vector2I(origin.X + x, origin.Y - 1),
                ref bestPosition,
                ref bestPathCost);

            CheckBuildWorkPosition(
                worker,
                new Vector2I(origin.X + x, origin.Y + size.Y),
                ref bestPosition,
                ref bestPathCost);
        }

        // Left and right edges.
        for (int y = 0; y < size.Y; y++)
        {
            CheckBuildWorkPosition(
                worker,
                new Vector2I(origin.X - 1, origin.Y + y),
                ref bestPosition,
                ref bestPathCost);

            CheckBuildWorkPosition(
                worker,
                new Vector2I(origin.X + size.X, origin.Y + y),
                ref bestPosition,
                ref bestPathCost);
        }

        return bestPosition;
    }

    private void CheckBuildWorkPosition(Worker worker, Vector2I position, ref Vector2I? bestPosition, ref int bestPathCost)
    {
        if (!_world.IsInside(position))
            return;

        if (!GridMovementRules.CanStand(_world, position))
            return;

        int pathCost = GetPathCostTo(worker, position);

        if (pathCost < 0)
            return;

        if (pathCost >= bestPathCost)
            return;

        bestPathCost = pathCost;
        bestPosition = position;
    }

    private Vector2I? FindOnTopWorkPosition(Worker worker, Vector2I target)
    {
        Vector2I workPosition = target + Vector2I.Up;

        if (!GridMovementRules.CanStand(_world, workPosition))
            return null;

        int pathCost = GetPathCostTo(worker, workPosition);

        return pathCost >= 0 ? workPosition : null;
    }

    // Returns the number of path steps from the worker's
    // current position to workPosition, 0 if already there, or
    // -1 if unreachable.
    private int GetPathCostTo(Worker worker, Vector2I workPosition)
    {
        if (worker.GridPosition == workPosition)
            return 0;

        List<PathStep> path = _pathfinder.FindPath(worker.GridPosition, workPosition);

        return path.Count > 0 ? path.Count : -1;
    }

    private bool CanSafelyWorkFrom(Worker worker, Vector2I workPosition, Vector2I target)
    {
        if (!GridMovementRules.CanStand(_world, workPosition))
            return false;

        Vector2I ground = workPosition + Vector2I.Down;

        if (ground == target)
        {
            // Standing on top of the block being dug is fine
            // as long as it's only a one-tile drop once the
            // block is gone - i.e. there's solid ground right
            // beneath it. If there's nothing under it, treat
            // this position as unsafe so the search prefers
            // somewhere that won't drop the colonist further.
            Vector2I belowTarget = target + Vector2I.Down;

            if (!_world.IsSolid(belowTarget))
                return false;
        }

        List<PathStep> path = _pathfinder.FindPath(worker.GridPosition, workPosition);

        return worker.GridPosition == workPosition || path.Count > 0;
    }

    public void CancelOrders(List<Vector2I> tiles)
    {
        HashSet<Vector2I> tileSet = new(tiles);

        for (int i = _workOrders.Count - 1; i >= 0; i--)
        {
            WorkOrder order = _workOrders[i];

            if (!tileSet.Contains(order.TilePosition))
                continue;

            if (order.IsClaimed)
            {
                order.Release();
            }

            _workOrders.RemoveAt(i);
        }

        // Notify UI / visualizers if you have an event for this.
    }
}