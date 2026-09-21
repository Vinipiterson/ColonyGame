using Godot;

public static class GameServices
{
    public static GridWorld GetGridWorld()
    {
        return (GridWorld) ((SceneTree)Engine.GetMainLoop())
            .GetFirstNodeInGroup("GridWorld");
    }

    public static GridPathFinder GetGridPathfinder()
    {
        return (GridPathFinder) ((SceneTree)Engine.GetMainLoop())
            .GetFirstNodeInGroup("GridPathFinder");
    }

    public static WorkOrderManager GetWorkOrderManager()
    {
        return (WorkOrderManager) ((SceneTree)Engine.GetMainLoop())
            .GetFirstNodeInGroup("WorkOrderManager");
    }

    public static StructureGrid GetStructureGrid()
    {
        return (StructureGrid) ((SceneTree)Engine.GetMainLoop())
            .GetFirstNodeInGroup("StructureGrid");
    }
}