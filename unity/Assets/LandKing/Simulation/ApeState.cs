namespace LandKing.Simulation
{
    /// <summary>
    /// Minimal ape state for the milestone-2 prototype (docs/实现/原型构建步骤).
    /// </summary>
    public struct ApeState
    {
        public int Id;
        public float Hunger;
        public float Health;
        public float Age;
        public bool Alive;
        public string Nickname;
        public int GridX;
        public int GridY;
        public ApeSide Side;
    }
}
