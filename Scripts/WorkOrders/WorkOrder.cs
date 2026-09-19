using Godot;

public enum WorkOrderType
{
    Dig,
    Build
}

public class WorkOrder
{
    public WorkOrderType Type;
    public Vector2I TilePosition;

    public int Priority;

    public float WorkRequired;
    public float WorkProgress;

    public bool IsClaimed { get; private set; }

    public Worker ClaimedBy { get; private set; }

    public WorkOrder(
        WorkOrderType type,
        Vector2I tilePosition,
        float workRequired,
        int priority = 5)
    {
        Type = type;
        TilePosition = tilePosition;

        Priority =
            Mathf.Clamp(
                priority,
                1,
                10
            );

        WorkRequired = workRequired;
        WorkProgress = 0.0f;

        IsClaimed = false;
        ClaimedBy = null;
    }

    public bool TryClaim(
        Worker worker)
    {
        if (IsClaimed)
            return false;

        IsClaimed = true;
        ClaimedBy = worker;

        return true;
    }

    public void Release()
    {
        IsClaimed = false;
        ClaimedBy = null;
    }

    public bool IsComplete()
    {
        return WorkProgress >= WorkRequired;
    }

    public bool AddWork(
        float amount)
    {
        WorkProgress += amount;

        return IsComplete();
    }
}