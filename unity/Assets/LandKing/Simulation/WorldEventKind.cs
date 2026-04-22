namespace LandKing.Simulation
{
    /// <summary>Simulation-emitted world events (chronicle / filtering / later persistence).</summary>
    public enum WorldEventKind
    {
        System = 0,
        DroughtStart = 1,
        DroughtSevere = 2,
        Rain = 3,
        Birth = 4,
        Starvation = 5,
        NaturalDeath = 6,
        FoodDepleted = 7,
        EastShore = 8,
        /// <summary>从邻格成年/年长者处习得「坚果敲裂」等。</summary>
        SkillLearned = 9,
        /// <summary>掌握某技艺的最后个体死亡。</summary>
        SkillExtinct = 10,
        /// <summary>走兽肉（猎物）被取食。</summary>
        PreyHunted = 11,
        /// <summary>被掠食者扑杀。</summary>
        Predation = 12,
        /// <summary>同族相邻扭打，双方挂彩与压力上升（未致死）。</summary>
        ApeConflict = 13,
        /// <summary>在同类冲突中伤重死亡。</summary>
        ApeKilledInConflict = 14,
        /// <summary>本世界首次有活体掌握该技艺；文案由数据 <c>milestoneDiscoveryPhrase</c> 与聚落/个体名拼成（隐式时代锚点）。</summary>
        MilestoneFirstDiscovery = 15
    }
}
