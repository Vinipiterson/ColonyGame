using Godot;
using System;
using System.Collections.Generic;

/*
 * Colonist is the actual game entity - the "Actor" in the UE
 * sense. It owns identity-level data (skills, appearance) and
 * composes the components that do the work:
 *
 *   - GridMovement: moves this Colonist around the grid
 *   - Worker: executes WorkOrderManager assignments
 *
 * Worker doesn't know it's attached to a Colonist specifically
 * - any other Node could host the same GridMovement + Worker
 * pair (an animal, a robot, a hired hand) and be assignable
 * work the exact same way.
 */
public partial class Colonist : Node2D, IWorkSkillProvider
{
    private const int TileSize = 32;

    // ------------------------------------------------
    // Components
    // ------------------------------------------------

    public Worker Worker { get; private set; }

    public Vector2I GridPosition => _movement.GridPosition;

    public Action OnWorkStateChanged;

    private GridMovement _movement;

    public override void _Ready()
    {
        AddToGroup("colonists");
    }


    // ------------------------------------------------
    // Initialization
    // ------------------------------------------------

    public void Initialize(Vector2I startingPosition)
    {
        _movement = new GridMovement();
        AddChild(_movement);

        _movement.Initialize(this, startingPosition);

        Worker = new Worker();
        AddChild(Worker);

        Worker.Initialize(_movement, this);
        Worker.OnWorkStateChanged += HandleWorkStateChanged;

        QueueRedraw();
    }

    private void HandleWorkStateChanged()
    {
        OnWorkStateChanged?.Invoke();
    }

    // ------------------------------------------------
    // IWorkSkillProvider
    // ------------------------------------------------

    public int GetPriorityModifier(WorkOrderType type)
    {
        return 0;
    }

    // ------------------------------------------------
    // Drawing
    // ------------------------------------------------

    public override void _Draw()
    {
        const float width = 20.0f;

        float height = TileSize * 2.0f;

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