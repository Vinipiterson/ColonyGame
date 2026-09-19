using Godot;

public static class WorldPresets
{
    public static void AddConcreteWall3(
        TileType[,] tiles)
    {
        for (int x = 12; x <= 13; x++)
        {
            for (int y = 14; y <= 20; y++)
            {
                tiles[x, y] =
                    TileType.Concrete;
            }
        }
    }

        public static void AddConcreteWall2(
        TileType[,] tiles)
    {
        for (int x = 12; x <= 13; x++)
        {
            for (int y = 15; y <= 20; y++)
            {
                tiles[x, y] =
                    TileType.Concrete;
            }
        }
    }

    public static void AddConcreteRow(
        TileType[,] tiles)
    {
        // One tile high horizontal platform
        // above the normal surface.

        // Normal surface = y 17
        // Platform occupies y 14.

        for (int x = 10; x <= 13; x++)
        {
            tiles[x, 16] =
                TileType.Concrete;
        }
    }

    public static void AddOneTileHole(
        TileType[,] tiles)
    {
        // Normal surface:
        // y = 17 dirt
        //
        // Remove one tile from the surface.

        int holeX = 12;

        tiles[holeX, 17] =
            TileType.Empty;
    }

        public static void AddTwoTileHole(
        TileType[,] tiles)
    {
        // Normal surface:
        // y = 17 dirt
        //
        // Remove one tile from the surface.

        int holeX = 12;

        tiles[holeX, 17] =
            TileType.Empty;

            
        tiles[holeX, 18] =
            TileType.Empty;
    }

    public static void AddThreeTileHole(
        TileType[,] tiles)
    {
        // Remove three tiles vertically
        // into the ground.

        int holeX = 12;

        tiles[holeX, 17] =
            TileType.Empty;

        tiles[holeX, 18] =
            TileType.Empty;

        tiles[holeX, 19] =
            TileType.Empty;
    }
}