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
    }
}
