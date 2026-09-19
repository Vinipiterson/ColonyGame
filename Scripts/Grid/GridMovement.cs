using Godot;
using System;
using System.Collections.Generic;

public enum MovementState
{
    Idle,
    Walking,
    Climbing,
    Falling
}

/*
 * GridMovement owns everything about moving a visual Node2D
 * one grid tile at a time along a path (walk / climb / fall),
 * plus spontaneous gravity when nothing is holding it up.
 *
 * It knows nothing about work orders, colonists, or workers -
 * it only moves a Node2D around a GridWorld.
 */
public partial class GridMovement : Node
{
    private const int TileSize = 32;

    private const float WalkSpeed = 100.0f;
    private const float ClimbSpeed = 100.0f;
    private const float FallSpeed = 400.0f;

    public Vector2I GridPosition { get; private set; }

    public MovementState State { get; private set; } = MovementState.Idle;

    // True while the mover is physically committed to a step
    // (mid-climb or mid-fall) and should not be redirected.
    public bool IsCommitted => State == MovementState.Climbing || State == MovementState.Falling;

    // Raised whenever a path finishes (or was empty to begin
    // with) or spontaneous gravity settles.
    public event Action OnPathCompleted;

    private Node2D _visual;
    private GridWorld _world;

    private List<PathStep> _path = new();

    private int _pathIndex;

    private Vector2I _targetGridPosition;

    private bool _followingPath;


    // ------------------------------------------------
    // Initialization
    // ------------------------------------------------

    public void Initialize(Node2D visual, Vector2I startingGridPosition)
    {
        _visual = visual;
        _world = GameServices.GetGridWorld();

        GridPosition = startingGridPosition;

        _targetGridPosition = startingGridPosition;

        _visual.Position = GridToWorld(GridPosition);
    }


    // ------------------------------------------------
    // Process
    // ------------------------------------------------

    public override void _Process(double delta)
    {
        // Gravity only applies when nothing else is already
        // driving movement this frame.
        if (State == MovementState.Idle)
        {
            TryStartSpontaneousFalling();
        }

        switch (State)
        {
            case MovementState.Walking:
                UpdateMovementTowardsTarget(WalkSpeed, delta);
                break;

            case MovementState.Climbing:
                UpdateMovementTowardsTarget(ClimbSpeed, delta);
                break;

            case MovementState.Falling:
                UpdateFalling(delta);
                break;
        }
    }


    // ------------------------------------------------
    // Path
    // ------------------------------------------------

    public void MoveAlongPath(List<PathStep> path)
    {
        _path = path ?? new List<PathStep>();

        _pathIndex = 0;
        _followingPath = true;

        if (_path.Count == 0)
        {
            _followingPath = false;

            OnPathCompleted?.Invoke();

            return;
        }

        MoveToNextPathStep();
    }

    // Cancels any in-progress walk/climb immediately. A commit
    // step (climb/fall) should be checked with IsCommitted
    // before calling this.
    public void Stop()
    {
        _path.Clear();

        _pathIndex = 0;
        _followingPath = false;

        if (State == MovementState.Walking || State == MovementState.Climbing)
        {
            State = MovementState.Idle;
        }
    }

    private void MoveToNextPathStep()
    {
        if (_pathIndex >= _path.Count)
        {
            _followingPath = false;

            State = MovementState.Idle;

            OnPathCompleted?.Invoke();

            return;
        }

        PathStep step = _path[_pathIndex];

        _pathIndex++;

        _targetGridPosition = step.Position;

        if (_targetGridPosition == GridPosition)
        {
            MoveToNextPathStep();

            return;
        }

        switch (step.MovementType)
        {
            case MovementType.Walk:

                State = MovementState.Walking;
                break;

            case MovementType.Climb:

                State = MovementState.Climbing;
                break;

            case MovementType.Fall:

                State = MovementState.Falling;
                break;
        }
    }

    // ------------------------------------------------
    // Movement Steps
    // ------------------------------------------------
    private void UpdateMovementTowardsTarget(float speed, double delta)
    {
        Vector2 targetPosition = GridToWorld(_targetGridPosition);

        _visual.Position = _visual.Position.MoveToward(targetPosition, speed * (float)delta);

        UpdateGridPositionFromVisual();

        if (_visual.Position != targetPosition)
            return;

        _visual.Position = targetPosition;
        GridPosition = _targetGridPosition;

        MoveToNextPathStep();
    }


    // ------------------------------------------------
    // Gravity / Falling
    // ------------------------------------------------

    private bool TryStartSpontaneousFalling()
    {
        Vector2I? fallDestination =
            _world.GetFallDestination(
                GridPosition
            );

        if (!fallDestination.HasValue)
            return false;

        _targetGridPosition =
            fallDestination.Value;

        _followingPath = false;

        State =
            MovementState.Falling;

        return true;
    }

    private void UpdateFalling(
        double delta)
    {
        Vector2 targetPosition =
            GridToWorld(
                _targetGridPosition
            );

        _visual.Position =
            _visual.Position.MoveToward(
                targetPosition,
                FallSpeed *
                (float)delta
            );

        UpdateGridPositionFromVisual();

        if (_visual.Position != targetPosition)
            return;

        _visual.Position =
            targetPosition;

        GridPosition =
            _targetGridPosition;

        // If this fall was part of a path, keep going.
        if (_followingPath)
        {
            MoveToNextPathStep();

            return;
        }

        // Otherwise this was spontaneous gravity settling.
        State =
            MovementState.Idle;

        OnPathCompleted?.Invoke();
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
            WorldToGrid(_visual.Position);
    }
}