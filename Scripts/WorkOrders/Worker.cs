using Godot;
using System;
using System.Collections.Generic;

public enum WorkerState
{
    Idle,
    MovingToWork,
    Working
}

/*
 * Worker is a plain component - the equivalent of an Unreal
 * ActorComponent. It has no visual identity, no skills, no
 * needs, and no idea what kind of actor it's attached to. It
 * only knows how to execute whatever assignment
 * WorkOrderManager hands it:
 *
 *   - move to a work position, via a GridMovement it was given
 *   - perform the work itself once it arrives
 *   - report completion back to WorkOrderManager
 *
 * Anything that wants to be assignable work just needs to host
 * a Worker + GridMovement pair and, optionally, implement
 * IWorkSkillProvider to weigh in on order priority.
 */
public partial class Worker : Node
{
    private const float WorkSpeed = 1.0f;

    public Vector2I GridPosition => _movement.GridPosition;

    public WorkerState State { get; private set; } = WorkerState.Idle;

    public bool IsIdle => State == WorkerState.Idle;

    // A worker mid-climb or mid-fall is physically committed
    // and cannot be redirected to a different order.
    public bool CanBeInterrupted => !_movement.IsCommitted;

    public WorkOrder CurrentOrder => _currentWorkOrder;

    // Exposed so WorkOrderManager can revalidate the path to
    // this position when the world changes underneath a worker
    // that's still MovingToWork. Null when there's no active
    // assignment to revalidate against.
    public Vector2I? WorkPosition =>
        State == WorkerState.MovingToWork || State == WorkerState.Working
            ? _workPosition
            : null;

    public Action OnWorkStateChanged;

    private GridWorld _world;
    private WorkOrderManager _workOrderManager;
    private GridMovement _movement;
    private IWorkSkillProvider _skillProvider;

    private WorkOrder _currentWorkOrder;
    private Vector2I _workPosition;


    // ------------------------------------------------
    // Initialization
    // ------------------------------------------------

    // movement is a GridMovement already living on the same
    // owner (a sibling component) - Worker doesn't create or
    // own it, just uses it. skillProvider is optional; a
    // Worker with none just scores every order at its base
    // priority.
    public void Initialize(GridMovement movement, IWorkSkillProvider skillProvider = null)
    {
        _movement = movement;
        _movement.OnPathCompleted += HandlePathCompleted;

        _world = GameServices.GetGridWorld();
        _workOrderManager = GameServices.GetWorkOrderManager();
        _skillProvider = skillProvider;

        State = WorkerState.Idle;
    }


    // ------------------------------------------------
    // Process
    // ------------------------------------------------

    public override void _Process(double delta)
    {
        if (State == WorkerState.Working)
        {
            UpdateWorking(delta);
        }
    }


    // ------------------------------------------------
    // Assignment (called by WorkOrderManager)
    // ------------------------------------------------

    public void AssignWork(WorkOrder order, Vector2I workPosition, List<PathStep> path)
    {
        _currentWorkOrder = order;

        _workPosition = workPosition;

        OnWorkStateChanged?.Invoke();

        StartPath(path);
    }

    // Lets the owner be driven along a path with no work order
    // attached, for pathfinding debug/visualization only.
    public void DebugMoveAlongPath(List<PathStep> path)
    {
        _currentWorkOrder = null;

        StartPath(path);
    }

    private void StartPath(List<PathStep> path)
    {
        if (path == null || path.Count == 0)
        {
            BeginWorking();

            return;
        }

        State = WorkerState.MovingToWork;

        _movement.MoveAlongPath(path);
    }

    private void HandlePathCompleted()
    {
        // Ignore completions that fire while we weren't
        // actually mid-assignment (e.g. spontaneous gravity
        // settling while idle).
        if (State != WorkerState.MovingToWork)
            return;

        BeginWorking();
    }


    // ------------------------------------------------
    // Working
    // ------------------------------------------------

    private void BeginWorking()
    {
        if (_currentWorkOrder == null)
        {
            State = WorkerState.Idle;

            return;
        }

        if (!IsWorkOrderStillValid())
        {
            CancelCurrentWork();

            return;
        }

        State = WorkerState.Working;
    }

    private void UpdateWorking(double delta)
    {
        if (_currentWorkOrder == null)
        {
            State = WorkerState.Idle;

            return;
        }

        if (!IsWorkOrderStillValid())
        {
            CancelCurrentWork();

            return;
        }

        bool complete = _currentWorkOrder.AddWork(WorkSpeed * (float)delta);

        if (!complete)
            return;

        WorkOrder completedOrder = _currentWorkOrder;

        _currentWorkOrder = null;

        _workOrderManager.CompleteOrder(completedOrder);

        State = WorkerState.Idle;

        OnWorkStateChanged?.Invoke();
    }

    private bool IsWorkOrderStillValid()
    {
        if (_currentWorkOrder == null)
            return false;

        if (_currentWorkOrder.Type == WorkOrderType.Dig)
        {
            return _world.GetTile(_currentWorkOrder.TilePosition) == TileType.Dirt;
        }

        return true;
    }


    // ------------------------------------------------
    // Cancel Work
    // ------------------------------------------------

    public void CancelCurrentWork()
    {
        _currentWorkOrder?.Release();

        _currentWorkOrder = null;

        _movement.Stop();

        State = WorkerState.Idle;

        OnWorkStateChanged?.Invoke();
    }


    // ------------------------------------------------
    // Priority Scoring
    // ------------------------------------------------

    // Forwards to whatever owns this Worker, if anything does.
    public int GetPriorityModifier(WorkOrderType type)
    {
        return _skillProvider?.GetPriorityModifier(type) ?? 0;
    }
}