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
        /// <summary>环形编年史条数，0=按 64 处理；在模拟内钳到 8..256。</summary>
        public int ChronicleMaxEntries = 64;
        /// <summary>东岸同族提示触发 tick，0=不触发。</summary>
        public int EastShoreNarrativeTick = 20;
        /// <summary>简化压力：旱时随本侧水位低而每 tick 上升，系数。</summary>
        public float StressDroughtScale = 0.012f;
        /// <summary>饥饿 &lt; 0.5 时按缺口增加压力/ tick。</summary>
        public float StressHungerScale = 0.045f;
        /// <summary>向平静回落；饱腹时回得更快（在 PhaseStress 内乘子）。</summary>
        public float StressRelaxPerTick = 0.018f;
        /// <summary>成年社交步进概率再乘 (1 - 本系数×压力)。</summary>
        public float SocialStressInhibit = 0.45f;
        /// <summary>高压力时游荡「僵一下」：概率上界 ≈ <c>Stress×本值</c>。</summary>
        public float StressWanderFreeze = 0.11f;
        /// <summary>「记得那棵果」每 tick 线性衰减；0=仅靠树枯等扣记忆。将 FoodMemoryDistanceBias 置 0 可关闭记忆系统。</summary>
        public float FoodMemoryDecayPerTick = 0.007f;
        /// <summary>觅食选树时，记忆格等效减去的格距（再乘记忆强度 0..1）。</summary>
        public float FoodMemoryDistanceBias = 2.2f;
        /// <summary>游荡时向果树记忆格迈一步的概率系数（再乘果记强度；0=关）。</summary>
        public float FoodMemoryWanderBias = 0.38f;
        /// <summary>同族「印象」每 tick 衰减；0=只在与记对象死亡时清。</summary>
        public float PeerMemoryDecayPerTick = 0.012f;
        /// <summary>游荡时向所记同族格迈一步的概率系数；0=关。</summary>
        public float PeerMemoryWanderBias = 0.3f;
        /// <summary>每次成年社交步进（命中随机后朝同类走）时增加印象，上限 1。</summary>
        public float PeerMemoryReinforce = 0.14f;
        /// <summary>勇气 -1..1 映射后，降低游荡僵停概率；0=不影响。</summary>
        public float CourageWanderResist = 0.35f;
        /// <summary>选社交目标时，有同族印象的对象等效格距最多减这么多（再乘印象强度）。</summary>
        public float PeerSocialPreferBias = 1.2f;
        /// <summary>雌性压力越高，<see cref="MatingRoll"/> 等效越低；0=不影响。</summary>
        public float MatingStressPenalty = 0.2f;

        public SimParams Copy()
        {
            return (SimParams)MemberwiseClone();
        }
    }
}
