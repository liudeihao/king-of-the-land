namespace LandKing.Simulation
{
    /// <summary>合并并校验后的文化技艺（依赖序见 <see cref="CultureRuntimeResult.SkillsInDependencyOrder"/>）。</summary>
    public sealed class CultureSkillDef
    {
        public string Id;
        public string DisplayName;
        public string[] Requires;
        public float ObserveLearn;
        public double InventPerTick;
        public int InitialMentorCount;
        public CultureInventContext InventContext;
        public float EatHungerBonus;
        public float FoodMemBoost;
        /// <summary>非空时：本局第一次有活体通过琢磨/观摩学会该技艺，记一条 <see cref="WorldEventKind.MilestoneFirstDiscovery"/> 并供 UI 强调；与开局师傅授予无关。</summary>
        public string MilestoneDiscoveryPhrase;
    }
}
