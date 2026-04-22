using System;

namespace LandKing.Simulation
{
    /// <summary>存档头与 v1 世界快照（与《微内核》§6 方向一致；Mod 元数据为宿主写入的可选列）。</summary>
    [Serializable]
    public sealed class WorldSaveV1
    {
        public int Schema = 1;
        public int RandomSeed;
        public ulong RngState;
        public int TickCount;
        public int NextId;
        public float WaterLeft;
        public float WaterRight;
        public bool DroughtActive;
        public bool RainUsed;
        public bool DroughtLogged;
        public SimParams Params;
        public int[] MapTiles;
        public float[] MapFood;
        public ApeSaveRecord[] Apes;
        /// <summary>Last ~64 world events; optional for 旧档兼容。</summary>
        public WorldEventSaveV1[] Chronicle;
        public string[] l1ModFolders;
        public string[] l1ModDisplayNames;
        /// <summary>与 <see cref="l1ModFolders"/> 同批拓扑序的 mod id，用于读档时强匹配；旧档可缺省。</summary>
        public string[] l1ModIds;
        /// <summary>两岸聚落名（随机池抽选）；旧档可缺省，读档时按种子补。</summary>
        public string SettlementNameLeft;
        public string SettlementNameRight;
        /// <summary>L1 各 Mod 可写的 JSON 分桶（Mod 内无代码时由 <c>l1_initial_persistent.json</c> 或首方逻辑填充）。</summary>
        public L1ModPersistentV1[] l1ModPersistent;
        public int NextPreyId;
        public int NextPredatorId;
        public PreySaveV1[] Prey;
        public PredatorSaveV1[] Predators;
        /// <summary>本局已记录过 <see cref="WorldEventKind.MilestoneFirstDiscovery"/> 的技艺 id；旧档缺省=空。</summary>
        public string[] MilestoneFirstDiscoveryKeys;
    }

    [Serializable]
    public sealed class L1ModPersistentV1
    {
        public string modId;
        public string dataJson;
    }

    [Serializable]
    public sealed class WorldEventSaveV1
    {
        public int Tick;
        public int Kind;
        public string Message;
    }

    [Serializable]
    public sealed class ApeSaveRecord
    {
        public int Id;
        public float Hunger;
        public float Health;
        public float Age;
        public bool Alive;
        public string Nickname;
        /// <summary>随机池称呼；旧档可空，读档时按 id 稳定生成。</summary>
        public string givenName;
        public int X, Y;
        public int Side;
        public bool IsMale;
        public float Courage;
        public float Curiosity;
        public float BodyScale;
        public int Stage;
        public int ParentA;
        public int ParentB;
        public int PregnancyCountdown;
        public int SireId;
        /// <summary>0..1；旧档无此字段时反序列化为 0。</summary>
        public float Stress;
        /// <summary>记忆为 0 时坐标忽略。旧档=无记忆。</summary>
        public int FoodMemX;
        public int FoodMemY;
        public float FoodMemStrength;
        public int PeerId;
        public float PeerMemStrength;
        /// <summary>已掌握技艺 id。非空时优先生效；旧档仅 <see cref="CultureFlags"/> 时在模拟内迁移。</summary>
        public string[] cultureSkillIds;
        /// <summary>v1 旧位标记：1=坚果敲裂，2=果记精描。无 <see cref="cultureSkillIds"/> 时读档迁移用。</summary>
        public int CultureFlags;
        /// <summary>0..1 遗传学习力；旧档 0+0 表示未存，读档时补默认。</summary>
        public float genLearn;
        /// <summary>0..1 遗传体质。</summary>
        public float genVigor;
        /// <summary>0..1 遗传社会性。</summary>
        public float genSocial;
    }

    [Serializable]
    public sealed class PreySaveV1
    {
        public int Id;
        public int X;
        public int Y;
        public bool Alive;
        public int RespawnAtTick;
        public string speciesId;
        public float meatHunger;
        public int respawnDelayTicks;
    }

    [Serializable]
    public sealed class PredatorSaveV1
    {
        public int Id;
        public int X;
        public int Y;
        public string speciesId;
        public float spookMaxStress;
        public int spookRadius;
    }
}
