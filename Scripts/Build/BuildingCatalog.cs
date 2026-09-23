using Godot;
using System.Collections.Generic;

[GlobalClass]
public partial class BuildingCatalog : Node
{
    [Export] public PackedScene BedScene { get; set; }
    [Export] public PackedScene ComfortableBedScene { get; set; }
    [Export] public PackedScene ManualGeneratorScene { get; set; }
    [Export] public PackedScene CoalGeneratorScene { get; set; }

    private readonly List<BuildingDefinition> _buildings = new();

    public IReadOnlyList<BuildingDefinition> Buildings => _buildings;

    public override void _EnterTree()
    {
        AddToGroup("BuildingCatalog");
    }

    public override void _Ready()
    {
        RegisterBuildings();
    }

    private void RegisterBuildings()
    {
        if (BedScene != null)
        {
            _buildings.Add(new BuildingDefinition(
                "bed",
                "Bed",
                new Vector2I(2, 2),
                2.0f,
                BedScene));
        }

        if (ComfortableBedScene != null)
        {
            _buildings.Add(new BuildingDefinition(
                "comfortable_bed",
                "Comfortable Bed",
                new Vector2I(2, 2),
                3.0f,
                ComfortableBedScene));
        }

        if (ManualGeneratorScene != null)
        {
            _buildings.Add(new BuildingDefinition(
                "manual_generator",
                "Manual Generator",
                new Vector2I(2, 2),
                3.0f,
                ManualGeneratorScene));
        }

        if (CoalGeneratorScene != null)
        {
            _buildings.Add(new BuildingDefinition(
                "coal_generator",
                "Coal Generator",
                new Vector2I(2, 2),
                4.0f,
                CoalGeneratorScene));
        }
    }

    public BuildingDefinition GetBuilding(string id)
    {
        foreach (BuildingDefinition building in _buildings)
        {
            if (building.Id == id)
                return building;
        }

        return null;
    }
}