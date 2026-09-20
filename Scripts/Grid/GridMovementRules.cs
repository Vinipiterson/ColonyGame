using Godot;

/*
 * GridMovementRules answers "what counts as a legal move" for
 * ANY mover on a GridWorld. It's deliberately stateless (a
 * static class, not a Node) because both GridPathFinder
 * (exploring a hypothetical path with no live actor) and
 * GridMovement (an actual actor moving frame by frame) need
 * the exact same rules and neither should own the other's copy
 * of them.
 *
 * GridWorld stays a pure tile data store: IsInside / GetTile /
 * SetTile / IsSolid. Everything about what a body can *do* on
 * that data lives here instead.
 */
public static class GridMovementRules
{
    public static bool CanStand(GridWorld world, Vector2I position)
    {
        if (!world.IsInside(position))
            return false;

        // Body must be empty.
        if (world.IsSolid(position))
            return false;

        // Head must be empty.
        Vector2I head = position + Vector2I.Up;

        if (!world.IsInside(head))
            return false;

        if (world.IsSolid(head))
            return false;

        // Ground must be solid.
        Vector2I below = position + Vector2I.Down;

        if (!world.IsInside(below))
            return false;

        return world.IsSolid(below);
    }

    public static bool CanWalkTo(GridWorld world, Vector2I from, Vector2I to)
    {
        if (!world.IsInside(from) || !world.IsInside(to))
            return false;

        if (from.Y != to.Y)
            return false;

        if (Mathf.Abs(from.X - to.X) != 1)
            return false;

        return CanStand(world, to);
    }

    public static Vector2I? GetClimbDestination(GridWorld world, Vector2I from, Vector2I to)
    {
        if (!world.IsInside(from) || !world.IsInside(to))
            return null;

        // Must move exactly one tile horizontally.
        if (Mathf.Abs(from.X - to.X) != 1)
            return null;

        // Only climb upward.
        if (to.Y >= from.Y)
            return null;

        int climbHeight = from.Y - to.Y;

        // Maximum climb height = 2.
        if (climbHeight < 1 || climbHeight > 2)
            return null;

        // ------------------------------------------------
        // Check vertical climbing space.
        //
        // The colonist stays on his current X while
        // climbing upward.
        // ------------------------------------------------

        for (int height = 1; height <= climbHeight; height++)
        {
            Vector2I bodyPosition = new Vector2I(from.X, from.Y - height);
            Vector2I headPosition = bodyPosition + Vector2I.Up;

            if (!world.IsInside(bodyPosition) || !world.IsInside(headPosition))
                return null;

            if (world.IsSolid(bodyPosition) || world.IsSolid(headPosition))
                return null;
        }

        // ------------------------------------------------
        // Check the final horizontal step.
        // ------------------------------------------------

        Vector2I climbPosition = new Vector2I(from.X, to.Y);
        Vector2I climbHead = climbPosition + Vector2I.Up;

        if (world.IsSolid(climbPosition) || world.IsSolid(climbHead))
            return null;

        // Final destination must be standable.
        if (!CanStand(world, to))
            return null;

        return to;
    }

    public static Vector2I? GetFallDestination(GridWorld world, Vector2I position)
    {
        if (!world.IsInside(position))
            return null;

        if (world.IsSolid(position))
            return null;

        Vector2I current = position + Vector2I.Down;

        while (world.IsInside(current))
        {
            // If the tile directly beneath us is solid, we're
            // already resting on it - no fall needed. This is
            // what makes a one-tile-thick bridge or platform
            // register as solid ground instead of the scan
            // seeing straight through it to whatever's below
            // (e.g. the floor of a chasm the bridge crosses).
            if (world.IsSolid(current))
                return null;

            Vector2I below = current + Vector2I.Down;

            if (!world.IsInside(below))
                return null;

            if (world.IsSolid(below) && CanStand(world, current))
            {
                return current;
            }

            current += Vector2I.Down;
        }

        return null;
    }

    public static Vector2I? GetStandableTileBelow(GridWorld world, Vector2I position)
    {
        if (!world.IsInside(position))
            return null;

        Vector2I current = position;

        while (world.IsInside(current))
        {
            if (CanStand(world, current))
                return current;

            current += Vector2I.Down;
        }

        return null;
    }
}