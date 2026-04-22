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
        /// <summary>双亲 <c>GenSocial</c> 平均相对 0.5 每偏 0.5 对交配概率的额外斜率；0=不影响。</summary>
        public float MatingGenSocialSigma = 0.1f;
        /// <summary>好奇 0..1 降低游荡「本 tick 不挪步」基础概率 20%（0=与旧版一致）。</summary>
        public float CuriosityWanderLively = 0.5f;
        /// <summary>幼幼/少每 tick 跟在世亲代挪一步的基准概率（再乘好奇）；0=关。</summary>
        public float ParentImitateBaseChance = 0.07f;
        /// <summary>随亲步阶段乘子：婴。</summary>
        public float ParentImitateInfantMult = 1f;
        /// <summary>随亲步阶段乘子：幼。</summary>
        public float ParentImitateChildMult = 0.9f;
        /// <summary>随亲步阶段乘子：青（可单独调低，削弱青年「黏亲」）。</summary>
        public float ParentImitateYouthMult = 0.3f;
        /// <summary>亲代死亡时，在世且仍记该亲 ID 的子女压力上跳；0=关。</summary>
        public float KinLossStress = 0.14f;
        /// <summary>开局赋予「坚果敲裂」技艺的成年/年长个体数（随机抽）；0=全族从零摸索。</summary>
        public int InitialNutCrackMentorCount = 2;
        /// <summary>邻格（格距≤2）观摩成年示范者时，每 tick 基础习得概率，再乘好奇；0=只能发明。</summary>
        public float ObserveLearnNutCrack = 0.012f;
        /// <summary>成年、尚无技艺、站在果边或果上时，每 tick 自发明概率；0=关。</summary>
        public float NutCrackInventPerTick = 0.00035f;
        /// <summary>活猎物（走兽）数量上界，随机铺在草地。</summary>
        public int PreyCount = 5;
        /// <summary>掠食者个体数，随机铺在草地，逐 tick 追近猿；0=关。</summary>
        public int PredatorCount = 1;
        /// <summary>取食同格活猎物时增加的饱食度。</summary>
        public float PreyMeatHunger = 0.22f;
        /// <summary>猎物被吃后，于此 tick 数后在同岸空草地重生；0=本 tick 即重生。</summary>
        public int PreyRespawnDelayTicks = 45;
        /// <summary>具坚果敲裂时，在果树上喰果额外增加饱食；0=无加成。</summary>
        public float NutCrackEatBonus = 0.042f;
        /// <summary>距最近掠食者格距（曼哈顿）不超过本值时，每 tick 按距离衰减加压力；0=无惊扰。</summary>
        public int PredatorSpookRadius = 2;
        /// <summary>与掠食者同格时（格距=0）每 tick 压力上跳，远处按格距比例衰减到 0。</summary>
        public float PredatorSpookMaxStress = 0.08f;
        /// <summary>第二技艺「果记精描」：观望习得（位 flag=2）。</summary>
        public float ObserveLearnFruitScout = 0.01f;
        /// <summary>成年、果边自悟果记精描。</summary>
        public float FruitScoutInventPerTick = 0.00028f;
        /// <summary>果记精描：在果上进食时，果记强度至少抬到 min(1, 1+本值)（叠加强记）。</summary>
        public float FruitScoutMemBoost = 0.1f;
        /// <summary>开局随机赋予果记精描的示范者数。</summary>
        public int InitialFruitScoutMentorCount = 0;
        /// <summary>每 tick：若存在可冲突邻格对，以该概率**触发其中一对**扭打；0=关。</summary>
        public double ConflictEventChance = 0.01;
        /// <summary>扭打中判定败方多扣的额外健康（胜方也扣 <see cref="ConflictWinnerHealth"/>）。</summary>
        public float ConflictLoserExtraHealth = 0.1f;
        /// <summary>扭打胜方也挂彩；双方至少各扣本值。</summary>
        public float ConflictWinnerHealth = 0.035f;
        /// <summary>扭打后双方压力上跳。</summary>
        public float ConflictStress = 0.1f;
        /// <summary>饱腹时健康自然回升/ tick，利于从轻伤恢复；0=关。</summary>
        public float SatedHealthRegenPerTick = 0.0018f;
        /// <summary>自饱食度高于本值时应用 <see cref="SatedHealthRegenPerTick"/>。</summary>
        public float SatedHealthRegenHunger = 0.68f;
        /// <summary>遗传学习力：对「发明每 tick 概率」与观摩基础概率的乘子，genLearn=0 时。</summary>
        public float GeneticLearnScaleAt0 = 0.68f;
        /// <summary>遗传学习力乘子，genLearn=1 时。</summary>
        public float GeneticLearnScaleAt1 = 1.22f;
        /// <summary>社会性遗传：genSocial=0 时对 <see cref="SocialAdultStepChance"/> 的乘子。</summary>
        public float GeneticSocialScaleAt0 = 0.74f;
        /// <summary>社会性遗传：genSocial=1 时乘子。</summary>
        public float GeneticSocialScaleAt1 = 1.2f;
        /// <summary>遗传体质：越高略减饥饿损耗；0=关闭（乘子恒为 1）。</summary>
        public float GeneticVigorHungerSigma = 0.14f;
        /// <summary>饱腹回血时乘 (1 + 本值×(genVigor-0.5)×2)。</summary>
        public float GeneticVigorRegenSigma = 0.18f;

        public SimParams Copy()
        {
            return (SimParams)MemberwiseClone();
        }
    }
}
