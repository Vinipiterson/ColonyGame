using Godot;

[GlobalClass]
public partial class BuildingDefinition : Resource
{
    [Export]
    public int Id;
    [Export]
    public string DisplayName;
    [Export]
    public Vector2I Size = Vector2I.One;
    [Export]
    public float WorkRequired = 1f;
    
/*
    [Export]
    public string ConstructionWork;
    
    [Export]
    public string TerrainRequirements;
    
    [Export]
    public string Materials;
    */
}