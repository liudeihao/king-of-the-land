namespace LandKing.Simulation
{
    /// <summary>单 Tick 内阶段顺序，与 <see cref="WorldSimulation"/> 的 private 方法一一对应；固定顺序便于测试与调参。</summary>
    public enum CoreLoopPhase
    {
        /// <summary>时间前进 + 旱灾/水位/降雨效应。</summary>
        Environment = 0,
        /// <summary>按间隔恢复果树（依赖水位）。</summary>
        FoodRegen = 1,
        /// <summary>怀孕倒计时与分娩。</summary>
        Reproduction = 2,
        /// <summary>年龄与自然死亡（老年）。</summary>
        Vitals = 3,
        /// <summary>饥饿、觅食、移动、游荡。</summary>
        IntentAndMovement = 4,
        /// <summary>邻格配对与受孕。</summary>
        Mating = 5,
        /// <summary>简社交（成年靠近）。</summary>
        Social = 6
    }
}
