using System.Collections.Generic;

namespace LandKing.Simulation
{
    /// <summary>合并后的可生成种组（表驱动，供 <see cref="WorldSimulation"/> 生成实体）。</summary>
    public sealed class WildlifeRuntimeResult
    {
        public List<PreyGroup> PreyGroups = new List<PreyGroup>(4);
        public List<PredatorGroup> PredatorGroups = new List<PredatorGroup>(2);
    }

    public struct PreyGroup
    {
        public string SpeciesId;
        public int Count;
        public float MeatHunger;
        public int RespawnDelayTicks;
    }

    public struct PredatorGroup
    {
        public string SpeciesId;
        public int Count;
        public float SpookMaxStress;
        public int SpookRadius;
    }
}
