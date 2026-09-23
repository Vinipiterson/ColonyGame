using Godot;

public class BuildingDefinition
{
    public string Id { get; }
    public string DisplayName { get; }
    public Vector2I Size { get; }
    public float WorkRequired { get; }
    public PackedScene Scene { get; }

    public BuildingDefinition(
        string id,
        string displayName,
        Vector2I size,
        float workRequired,
        PackedScene scene)
    {
        Id = id;
        DisplayName = displayName;
        Size = size;
        WorkRequired = workRequired;
        Scene = scene;
    }
}