using Godot;
using System.Collections.Generic;

public enum ColonistState
{
    Idle,
    MovingToWork,
    Climbing,
    Working,
    Falling
}

public partial class Colonist : Node2D
{
    private const int TileSize = 32;

    private const float MoveSpeed =
        100.0f;

    private const float ClimbSpeed =
        100.0f;

    private const float FallSpeed =
        400.0f;

    private const float WorkSpeed =
        1.0f;

    public Vector2I GridPosition
    {
        get;
        private set;
    }

    public ColonistState State
    {
        get;
        private set;
    }

    private GridWorld _world;
    private GridPathfinder _pathfinder;
    private WorkOrderManager _workOrderManager;

    private WorkOrder _currentWorkOrder;

    private Vector2I _workPosition;

    private List<PathStep> _path =
        new();

    private int _pathIndex;

    private Vector2I _targetGridPosition;

    private bool _isMoving;

    public System.Action OnWorkStateChanged;


    // ------------------------------------------------
    // Initialization
    // ------------------------------------------------

    public void Initialize(
        Vector2I startingPosition,
        GridWorld world,
        GridPathfinder pathfinder,
        WorkOrderManager workOrderManager)
    {
        GridPosition =
            startingPosition;

        _targetGridPosition =
            startingPosition;

        _world =
            world;

        _pathfinder =
            pathfinder;

        _workOrderManager =
            workOrderManager;

        State =
            ColonistState.Idle;

        _isMoving = false;

        Position =
            GridToWorld(
                GridPosition
            );

        QueueRedraw();
    }


    // ------------------------------------------------
    // Process
    // ------------------------------------------------

    public override void _Process(
        double delta)
    {
        // Gravity only applies when the colonist
        // is not already executing a movement step.
        //
        // This is important because climbing can
        // temporarily have no solid tile directly
        // underneath the colonist.
        if (State != ColonistState.Falling &&
            State != ColonistState.Climbing &&
            !_isMoving)
        {
            if (TryStartFalling())
            {
                return;
            }
        }

        // Priority 10 orders can interrupt
        // whatever the colonist is currently doing.
        //
        // Do not interrupt an active climb or fall.
        if (State != ColonistState.Falling &&
            State != ColonistState.Climbing)
        {
            if (TryHandleEmergencyOrder())
            {
                return;
            }
        }

        switch (State)
        {
            case ColonistState.Idle:

                UpdateIdle();

                break;

            case ColonistState.MovingToWork:

                UpdateMovement(delta);

                break;

            case ColonistState.Climbing:

                UpdateClimbing(delta);

                break;

            case ColonistState.Working:

                UpdateWorking(delta);

                break;

            case ColonistState.Falling:

                UpdateFalling(delta);

                break;
        }
    }


    // ------------------------------------------------
    // Idle
    // ------------------------------------------------

    private void UpdateIdle()
    {
        WorkOrder order =
            FindBestAvailableOrder();

        if (order == null)
            return;

        TryStartWork(order);
    }


    // ------------------------------------------------
    // Work Order Selection
    // ------------------------------------------------

    private WorkOrder FindBestAvailableOrder()
    {
        IReadOnlyList<WorkOrder> orders =
            _workOrderManager.GetAvailableOrders();

        WorkOrder bestOrder = null;

        int bestPriority = 0;
        int bestPathCost = int.MaxValue;

        foreach (WorkOrder order in orders)
        {
            if (order.IsClaimed)
                continue;

            Vector2I? workPosition =
                FindWorkPosition(order);

            // No valid/reachable work position.
            // Treat this order as unavailable.
            if (!workPosition.HasValue)
                continue;

            List<PathStep> path =
                _pathfinder.FindPath(
                    GridPosition,
                    workPosition.Value
                );

            int pathCost =
                path.Count;

            if (GridPosition ==
                workPosition.Value)
            {
                pathCost = 0;
            }

            int priority =
                GetEffectivePriority(order);

            bool isBetter =
                bestOrder == null ||
                priority > bestPriority ||
                (priority == bestPriority &&
                 pathCost < bestPathCost);

            if (!isBetter)
                continue;

            bestOrder =
                order;

            bestPriority =
                priority;

            bestPathCost =
                pathCost;
        }

        return bestOrder;
    }


    // ------------------------------------------------
    // Emergency Orders
    // ------------------------------------------------

    private bool TryHandleEmergencyOrder()
    {
        WorkOrder emergencyOrder =
            FindBestEmergencyOrder();

        if (emergencyOrder == null)
            return false;

        // An active priority 10 order cannot
        // be interrupted by another priority 10.
        if (_currentWorkOrder != null &&
            GetEffectivePriority(
                _currentWorkOrder
            ) >= 10)
        {
            return false;
        }

        CancelCurrentWork();

        return TryStartWork(
            emergencyOrder
        );
    }

    private WorkOrder FindBestEmergencyOrder()
    {
        IReadOnlyList<WorkOrder> orders =
            _workOrderManager.GetAvailableOrders();

        WorkOrder bestOrder = null;

        int bestPriority = 0;
        int bestPathCost = int.MaxValue;

        foreach (WorkOrder order in orders)
        {
            if (order.IsClaimed)
                continue;

            int priority =
                GetEffectivePriority(order);

            if (priority < 10)
                continue;

            Vector2I? workPosition =
                FindWorkPosition(order);

            // Unreachable emergency orders should
            // not block other available orders.
            if (!workPosition.HasValue)
                continue;

            List<PathStep> path =
                _pathfinder.FindPath(
                    GridPosition,
                    workPosition.Value
                );

            int pathCost =
                path.Count;

            if (GridPosition ==
                workPosition.Value)
            {
                pathCost = 0;
            }

            if (bestOrder == null ||
                priority > bestPriority ||
                (priority == bestPriority &&
                 pathCost < bestPathCost))
            {
                bestOrder =
                    order;

                bestPriority =
                    priority;

                bestPathCost =
                    pathCost;
            }
        }

        return bestOrder;
    }


    // ------------------------------------------------
    // Priority
    // ------------------------------------------------

    /*
     * This is the colonist's priority function.
     *
     * The order has its base priority, while
     * the colonist can modify it based on
     * personality, skills, settings, etc.
     *
     * For now every modifier is 0.
     */

    private int GetEffectivePriority(
        WorkOrder order)
    {
        int priority =
            order.Priority;

        priority +=
            GetWorkTypePriorityModifier(
                order.Type
            );

        return Mathf.Clamp(
            priority,
            1,
            10
        );
    }

    /*
     * Future examples:
     *
     * Dig     -> -2
     * Build   -> +1
     * Harvest -> +3
     *
     * based on this specific colonist's
     * preferences and abilities.
     */

    private int GetWorkTypePriorityModifier(
        WorkOrderType type)
    {
        switch (type)
        {
            case WorkOrderType.Dig:
                return 0;

            case WorkOrderType.Build:
                return 0;

            default:
                return 0;
        }
    }


    // ------------------------------------------------
    // Start Work
    // ------------------------------------------------

    private bool TryStartWork(
        WorkOrder order)
    {
        if (!order.TryClaim(this))
            return false;

        OnWorkStateChanged?.Invoke();

        Vector2I? workPosition =
            FindWorkPosition(order);

        if (!workPosition.HasValue)
        {
            order.Release();

            OnWorkStateChanged?.Invoke();

            return false;
        }

        _currentWorkOrder =
            order;

        _workPosition =
            workPosition.Value;

        List<PathStep> path =
            _pathfinder.FindPath(
                GridPosition,
                _workPosition
            );

        if (GridPosition !=
                _workPosition &&
            path.Count == 0)
        {
            _currentWorkOrder = null;

            order.Release();

            OnWorkStateChanged?.Invoke();

            return false;
        }

        SetPath(path);

        return true;
    }


    // ------------------------------------------------
    // Work Position
    // ------------------------------------------------

    private Vector2I? FindWorkPosition(
        WorkOrder order)
    {
        Vector2I target =
            order.TilePosition;

        Vector2I? bestPosition = null;

        float bestScore =
            float.MaxValue;

        const int HorizontalRange = 2;
        const int VerticalRange = 4;

        const float HorizontalWeight = 10.0f;
        const float VerticalWeight = 1.0f;
        const float PathWeight = 0.25f;

        for (int xOffset = -HorizontalRange;
            xOffset <= HorizontalRange;
            xOffset++)
        {
            for (int yOffset = -VerticalRange;
                yOffset <= VerticalRange;
                yOffset++)
            {
                Vector2I workPosition =
                    new Vector2I(
                        target.X + xOffset,
                        target.Y + yOffset
                    );

                if (!CanSafelyWorkFrom(
                        workPosition,
                        target))
                {
                    continue;
                }

                List<PathStep> path =
                    _pathfinder.FindPath(
                        GridPosition,
                        workPosition
                    );

                if (GridPosition !=
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
                        GridPosition.Y
                    );

                float pathDistance =
                    GridPosition ==
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
        Vector2I workPosition,
        Vector2I target)
    {
        if (!_world.CanStand(
                workPosition))
        {
            return false;
        }

        // The tile supporting the colonist
        // must not be the tile being destroyed.
        Vector2I ground =
            workPosition + Vector2I.Down;

        if (ground == target)
            return false;

        List<PathStep> path =
            _pathfinder.FindPath(
                GridPosition,
                workPosition
            );

        return GridPosition ==
                   workPosition ||
               path.Count > 0;
    }


    // ------------------------------------------------
    // Movement
    // ------------------------------------------------

    private void UpdateMovement(
        double delta)
    {
        UpdateMovementTowardsTarget(
            MoveSpeed,
            delta
        );
    }

    private void UpdateClimbing(
        double delta)
    {
        UpdateMovementTowardsTarget(
            ClimbSpeed,
            delta
        );
    }

    private void UpdateMovementTowardsTarget(
        float speed,
        double delta)
    {
        if (!_isMoving)
        {
            BeginWorking();

            return;
        }

        Vector2 targetPosition =
            GridToWorld(
                _targetGridPosition
            );

        Position =
            Position.MoveToward(
                targetPosition,
                speed *
                (float)delta
            );

        UpdateGridPositionFromVisual();

        if (Position != targetPosition)
            return;

        Position =
            targetPosition;

        GridPosition =
            _targetGridPosition;

        MoveToNextPathPosition();
    }


    // ------------------------------------------------
    // Path
    // ------------------------------------------------

    public void SetPath(
        List<PathStep> path)
    {
        _path =
            path ??
            new List<PathStep>();

        _pathIndex = 0;

        if (_path.Count == 0)
        {
            _isMoving = false;

            BeginWorking();

            return;
        }

        MoveToNextPathPosition();
    }

    private void MoveToNextPathPosition()
    {
        if (_pathIndex >= _path.Count)
        {
            _isMoving = false;

            BeginWorking();

            return;
        }

        PathStep step =
            _path[_pathIndex];

        _pathIndex++;

        _targetGridPosition =
            step.Position;

        GD.Print(
            $"Path Step: " +
            $"{step.MovementType} -> " +
            $"{step.Position}"
        );

        if (_targetGridPosition ==
            GridPosition)
        {
            MoveToNextPathPosition();

            return;
        }

        _isMoving = true;

        switch (step.MovementType)
        {
            case MovementType.Walk:

                State =
                    ColonistState.MovingToWork;

                break;

            case MovementType.Climb:

                State =
                    ColonistState.Climbing;

                break;

            case MovementType.Fall:

                State =
                    ColonistState.Falling;

                break;
        }
    }


    // ------------------------------------------------
    // Working
    // ------------------------------------------------

    private void BeginWorking()
    {
        if (_currentWorkOrder == null)
        {
            State =
                ColonistState.Idle;

            return;
        }

        if (!IsWorkOrderStillValid())
        {
            CancelCurrentWork();

            return;
        }

        State =
            ColonistState.Working;
    }

    private void UpdateWorking(
        double delta)
    {
        if (_currentWorkOrder == null)
        {
            State =
                ColonistState.Idle;

            return;
        }

        if (!IsWorkOrderStillValid())
        {
            CancelCurrentWork();

            return;
        }

        bool complete =
            _currentWorkOrder.AddWork(
                WorkSpeed *
                (float)delta
            );

        if (!complete)
            return;

        WorkOrder completedOrder =
            _currentWorkOrder;

        _currentWorkOrder = null;

        _workOrderManager.CompleteOrder(
            completedOrder
        );

        State =
            ColonistState.Idle;

        OnWorkStateChanged?.Invoke();

        QueueRedraw();
    }


    // ------------------------------------------------
    // Gravity / Falling
    // ------------------------------------------------

    private bool TryStartFalling()
    {
        Vector2I? fallDestination =
            _world.GetFallDestination(
                GridPosition
            );

        if (!fallDestination.HasValue)
            return false;

        _targetGridPosition =
            fallDestination.Value;

        _isMoving = false;

        State =
            ColonistState.Falling;

        return true;
    }

    private void UpdateFalling(
        double delta)
    {
        Vector2 targetPosition =
            GridToWorld(
                _targetGridPosition
            );

        Position =
            Position.MoveToward(
                targetPosition,
                FallSpeed *
                (float)delta
            );

        UpdateGridPositionFromVisual();

        if (Position != targetPosition)
            return;

        Position =
            targetPosition;

        GridPosition =
            _targetGridPosition;

        // If this fall was part of a path,
        // continue executing that path.
        if (_isMoving)
        {
            MoveToNextPathPosition();

            return;
        }

        // Otherwise this was spontaneous gravity.
        State =
            ColonistState.Idle;

        QueueRedraw();
    }


    // ------------------------------------------------
    // Work Validation
    // ------------------------------------------------

    private bool IsWorkOrderStillValid()
    {
        if (_currentWorkOrder == null)
            return false;

        if (_currentWorkOrder.Type ==
            WorkOrderType.Dig)
        {
            return _world.GetTile(
                       _currentWorkOrder.TilePosition
                   ) ==
                   TileType.Dirt;
        }

        return true;
    }


    // ------------------------------------------------
    // Cancel Work
    // ------------------------------------------------

    private void CancelCurrentWork()
    {
        if (_currentWorkOrder != null)
        {
            _currentWorkOrder.Release();

            _currentWorkOrder = null;
        }

        _path.Clear();

        _pathIndex = 0;

        _isMoving = false;

        State =
            ColonistState.Idle;

        OnWorkStateChanged?.Invoke();
    }


    // ------------------------------------------------
    // Grid / World Position
    // ------------------------------------------------

    private Vector2 GridToWorld(
        Vector2I gridPosition)
    {
        return new Vector2(
            gridPosition.X *
                TileSize +
                TileSize / 2.0f,

            gridPosition.Y *
                TileSize +
                TileSize / 2.0f
        );
    }

    private Vector2I WorldToGrid(
        Vector2 worldPosition)
    {
        return new Vector2I(
            Mathf.RoundToInt(
                worldPosition.X /
                TileSize -
                0.5f
            ),

            Mathf.RoundToInt(
                worldPosition.Y /
                TileSize -
                0.5f
            )
        );
    }

    private void UpdateGridPositionFromVisual()
    {
        GridPosition =
            WorldToGrid(Position);
    }


    // ------------------------------------------------
    // Reachability
    // ------------------------------------------------

    public bool HasReachableWorkPosition(
        WorkOrder order)
    {
        return FindWorkPosition(order)
            .HasValue;
    }


    // ------------------------------------------------
    // Drawing
    // ------------------------------------------------

    public override void _Draw()
    {
        const float width =
            20.0f;

        float height =
            TileSize * 2.0f;

        DrawRect(
            new Rect2(
                -width / 2.0f,
                -TileSize * 1.5f,
                width,
                height
            ),
            Colors.Red
        );
    }
}