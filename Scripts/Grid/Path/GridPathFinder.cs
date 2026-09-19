using Godot;
using System.Collections.Generic;

public class GridPathfinder
{
    private readonly GridWorld _world;

    public GridPathfinder(GridWorld world)
    {
        _world = world;
    }

    public List<PathStep> FindPath(
        Vector2I start,
        Vector2I target)
    {
        var path =
            new List<PathStep>();

        if (!_world.IsInside(start) ||
            !_world.IsInside(target))
        {
            return path;
        }

        if (!_world.CanStand(start) ||
            !_world.CanStand(target))
        {
            return path;
        }

        var openSet =
            new PriorityQueue<Vector2I, int>();

        var cameFrom =
            new Dictionary<Vector2I, Vector2I>();

        var gScore =
            new Dictionary<Vector2I, int>();

        gScore[start] = 0;

        openSet.Enqueue(
            start,
            Heuristic(start, target)
        );

        while (openSet.Count > 0)
        {
            Vector2I current =
                openSet.Dequeue();

            if (current == target)
            {
                return ReconstructPath(
                    cameFrom,
                    current
                );
            }

            foreach (Vector2I neighbor
                     in GetNeighbors(current))
            {
                int movementCost =
                    GetMovementCost(
                        current,
                        neighbor
                    );

                int newCost =
                    gScore[current] +
                    movementCost;

                if (!gScore.TryGetValue(
                        neighbor,
                        out int oldCost) ||
                    newCost < oldCost)
                {
                    gScore[neighbor] = newCost;

                    int priority =
                        newCost +
                        Heuristic(
                            neighbor,
                            target
                        );

                    cameFrom[neighbor] =
                        current;

                    openSet.Enqueue(
                        neighbor,
                        priority
                    );
                }
            }
        }

        return path;
    }

    private IEnumerable<Vector2I> GetNeighbors(
        Vector2I position)
    {
        // ------------------------------------------------
        // Walk left
        // ------------------------------------------------

        Vector2I left =
            position + Vector2I.Left;

        if (_world.CanWalkTo(
                position,
                left))
        {
            yield return left;
        }

        // ------------------------------------------------
        // Walk right
        // ------------------------------------------------

        Vector2I right =
            position + Vector2I.Right;

        if (_world.CanWalkTo(
                position,
                right))
        {
            yield return right;
        }

        // ------------------------------------------------
        // Climb left
        // ------------------------------------------------

        for (int height = 1;
             height <= 2;
             height++)
        {
            Vector2I destination =
                new Vector2I(
                    position.X - 1,
                    position.Y - height
                );

            if (_world.GetClimbDestination(
                    position,
                    destination
                ).HasValue)
            {
                yield return destination;

                break;
            }
        }

        // ------------------------------------------------
        // Climb right
        // ------------------------------------------------

        for (int height = 1;
             height <= 2;
             height++)
        {
            Vector2I destination =
                new Vector2I(
                    position.X + 1,
                    position.Y - height
                );

            if (_world.GetClimbDestination(
                    position,
                    destination
                ).HasValue)
            {
                yield return destination;

                break;
            }
        }

        // ------------------------------------------------
        // Fall left
        // ------------------------------------------------

        Vector2I leftPosition =
            position + Vector2I.Left;

        Vector2I? leftFall =
            _world.GetFallDestination(
                leftPosition
            );

        if (leftFall.HasValue)
        {
            yield return leftFall.Value;
        }

        // ------------------------------------------------
        // Fall right
        // ------------------------------------------------

        Vector2I rightPosition =
            position + Vector2I.Right;

        Vector2I? rightFall =
            _world.GetFallDestination(
                rightPosition
            );

        if (rightFall.HasValue)
        {
            yield return rightFall.Value;
        }
    }

    private int GetMovementCost(
        Vector2I from,
        Vector2I to)
    {
        int verticalDifference =
            Mathf.Abs(
                from.Y - to.Y
            );

        // Walking.
        if (verticalDifference == 0)
            return 1;

        // Climbing.
        if (to.Y < from.Y)
        {
            return 2 + verticalDifference;
        }

        // Falling.
        return 1 + verticalDifference;
    }

    private int Heuristic(
        Vector2I a,
        Vector2I b)
    {
        return Mathf.Abs(a.X - b.X) +
               Mathf.Abs(a.Y - b.Y);
    }

    private MovementType GetMovementType(
        Vector2I from,
        Vector2I to)
    {
        if (from.Y == to.Y)
            return MovementType.Walk;

        if (to.Y < from.Y)
            return MovementType.Climb;

        return MovementType.Fall;
    }

    private List<PathStep> ReconstructPath(
        Dictionary<Vector2I, Vector2I> cameFrom,
        Vector2I current)
    {
        var positions =
            new List<Vector2I>();

        while (cameFrom.ContainsKey(current))
        {
            positions.Add(current);

            current =
                cameFrom[current];
        }

        positions.Reverse();

        var path =
            new List<PathStep>();

        Vector2I previous =
            current;

        foreach (Vector2I position in positions)
        {
            MovementType movementType =
                GetMovementType(
                    previous,
                    position
                );

            path.Add(
                new PathStep(
                    position,
                    movementType
                )
            );

            previous = position;
        }

        return path;
    }
}