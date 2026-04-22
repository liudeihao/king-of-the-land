namespace LandKing.Simulation
{
    /// <summary>地上猎物格点（同格可被猿取食，死后计时重生）；种参数来自 L1 表驱动。</summary>
    internal sealed class PreyEntity
    {
        public int Id;
        public string SpeciesId = WildlifeIds.CoreGrassPrey;
        public int X, Y;
        public bool Alive;
        public int RespawnAtTick;
        public float MeatHunger = 0.22f;
        public int RespawnDelayTicks = 45;
    }

    /// <summary>掠食者，每 tick 追近最近猿，同格则扑杀；惊扰半径/上界可 per-种。</summary>
    internal sealed class PredatorEntity
    {
        public int Id;
        public string SpeciesId = WildlifeIds.CoreStalker;
        public int X, Y;
        public float SpookMaxStress = 0.08f;
        public int SpookRadius = 2;
    }
}
