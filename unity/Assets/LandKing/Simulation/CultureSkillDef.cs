namespace LandKing.Simulation
{
    /// <summary>合并并校验后的文化技艺（依赖序见 <see cref="CultureRuntimeResult.SkillsInDependencyOrder"/>）。</summary>
    public sealed class CultureSkillDef
    {
        public string Id;
        public string DisplayName;
        /// <summary>技艺说明（工具、考占或玩法）；可为空。</summary>
        public string Description;
        public string[] Requires;
        public float ObserveLearn;
        public double InventPerTick;
        public int InitialMentorCount;
        public CultureInventContext InventContext;
        public float EatHungerBonus;
        public float FoodMemBoost;
        /// <summary>首次发现时【】内提示语；为空则用 <see cref="DisplayName"/>。特殊技艺可写长句，一般技艺可省略。</summary>
        public string MilestoneDiscoveryPhrase;
    }
}
