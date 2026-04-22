using System;

namespace LandKing.Simulation
{
    /// <summary>表驱动参数（L1 数据 Mod 的合并目标；内核不做 JSON 解析）。</summary>
    [Serializable]
    public sealed class SimParams
    {
        public static readonly SimParams Default = new SimParams();

        public float HungerPerTick = 0.02f;
        public float EatHunger = 0.3f;
        public float SeekHungerThreshold = 0.7f;
        public float MaxFoodPerCell = 5f;
        public int DroughtStartTick = 100;
        public float DroughtPerTick = 0.002f;
        public float RainLeftWater = 0.8f;
        public float DroughtButtonThreshold = 0.2f;
        public int FoodRegenIntervalTicks = 10;
        public float FoodRegenWaterMultiplier = 0.5f;
        public int PregnancyDurationTicks = 28;
        public double MatingRoll = 0.012;
        public int MaxApeCount = 36;
        public float AgePerTick = 0.07f;
        public float NaturalDeathAtAge = 40f;
        public double ElderDeathChance = 0.006;
        public float InfantHungerDecay = 0.012f;
        public float ChildHungerDecay = 0.014f;
        public float MatingMinHunger = 0.35f;
        public float SocialMinHunger = 0.55f;
        public float SocialAdultStepChance = 0.06f;
        public float MinFruitToEat = 0.3f;

        public SimParams Copy()
        {
            return (SimParams)MemberwiseClone();
        }
    }
}
