using Godot;

public enum MovementType
{
    Walk,
    Climb,
    Fall
}

public struct PathStep
{
    public Vector2I Position;
    public MovementType MovementType;

    public PathStep(
        Vector2I position,
        MovementType movementType)
    {
        Position = position;
        MovementType = movementType;
    }
}