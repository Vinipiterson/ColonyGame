using Godot;
using System;

public enum BuildingState
{
    UnderConstruction,
    Constructed,
    Destroyed
}

[GlobalClass]
public partial class Building : Node2D
{
    public BuildingDefinition Definition { get; set; }

    public Vector2I GridPosition { get; private set; }
    public BuildingState State { get; private set; }

    public Vector2I Size => Definition.Size;

    private GridWorld _world;

    [Export]
    public Control BlueprintVisual;
    [Export]
    public Control DefinitiveVisual;

    public void Initialize(BuildingDefinition definition, Vector2I gridPosition, Vector2 worldPosition, BuildingState state)
    {
        _world = GameServices.GetGridWorld();

        Definition = definition;
        GridPosition = gridPosition;

        SetState(state);

        // Position the building correctly
        Position = worldPosition;
    }

    public void CompleteConstruction()
    {
        SetState(BuildingState.Constructed);
    }

    public void Destroy()
    {
        SetState(BuildingState.Destroyed);
    }

    private void SetState(BuildingState state)
    {
        State = state;

        BlueprintVisual.Visible = State == BuildingState.UnderConstruction;
        DefinitiveVisual.Visible = State == BuildingState.Constructed;
    }

    public void SetGridPosition(Vector2I gridPosition, Vector2 worldPosition)
    {
        GridPosition = gridPosition;
        Position = worldPosition;
    }

    public void SetBlueprintValidity(bool valid)
    {
        if (BlueprintVisual == null)
            return;

        BlueprintVisual.Modulate = valid
            ? Colors.White
            : new Color(1.0f, 0.35f, 0.35f, 0.65f);
    }
}
