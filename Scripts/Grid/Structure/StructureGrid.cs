using Godot;
using System.Collections.Generic;

[GlobalClass]
public partial class StructureGrid : Node
{
    private Dictionary<Vector2I, Building> _occupancy = new();

    private GridWorld _world;

    public override void _EnterTree()
    {
        base._EnterTree();

        AddToGroup("StructureGrid");
    }

    public override void _Ready()
    {
        _world = GameServices.GetGridWorld();
    }

    public Building GetBuilding(Vector2I position)
    {
        _occupancy.TryGetValue(position, out Building building);

        return building;
    }

    public bool HasBuilding(Vector2I position)
    {
        return _occupancy.ContainsKey(position);
    }

    public bool CanPlaceBuilding(BuildingDefinition definition, Vector2I position)
    {
        if (definition == null)
            return false;

        Vector2I size = definition.Size;

        for (int x = 0; x < size.X; x++)
        {
            for (int y = 0; y < size.Y; y++)
            {
                Vector2I tile =
                    position +
                    new Vector2I(x, y);

                if (!_world.IsInside(tile))
                    return false;

                if (HasBuilding(tile))
                    return false;
            }
        }

        return true;
    }

    public bool RegisterBuilding(Building building)
    {
        if (building == null || building.Definition == null)
        {
            return false;
        }

        if (!CanPlaceBuilding(building.Definition, building.GridPosition))
        {
            return false;
        }

        Vector2I position = building.GridPosition;

        Vector2I size = building.Definition.Size;

        for (int x = 0; x < size.X; x++)
        {
            for (int y = 0; y < size.Y; y++)
            {
                Vector2I tile = position + new Vector2I(x, y);

                _occupancy[tile] = building;
            }
        }

        return true;
    }

    public void UnregisterBuilding(Building building)
    {
        if (building == null || building.Definition == null)
        {
            return;
        }

        Vector2I position = building.GridPosition;

        Vector2I size = building.Definition.Size;

        for (int x = 0; x < size.X; x++)
        {
            for (int y = 0; y < size.Y; y++)
            {
                Vector2I tile = position + new Vector2I(x, y);

                if (_occupancy.TryGetValue(tile, out Building occupyingBuilding) && occupyingBuilding == building)
                {
                    _occupancy.Remove(tile);
                }
            }
        }
    }
}
