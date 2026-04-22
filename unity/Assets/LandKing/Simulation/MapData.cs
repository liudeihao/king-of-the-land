namespace LandKing.Simulation
{
    /// <summary>
    /// Grid + per-cell food. River column is <see cref="RiverX"/>.
    /// </summary>
    public sealed class MapData
    {
        public const int Size = 20;
        public const int RiverX = 10;

        public TileType[,] Tiles { get; }
        public float[,] Food { get; }

        public MapData(TileType[,] tiles, float[,] food)
        {
            Tiles = tiles;
            Food = food;
        }

        public static bool InBounds(int x, int y) =>
            x >= 0 && x < Size && y >= 0 && y < Size;

        public bool IsWalkable(int x, int y) =>
            InBounds(x, y) && Tiles[x, y] != TileType.River;
    }
}
