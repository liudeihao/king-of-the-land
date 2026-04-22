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
        public int NextPreyId;
        public int NextPredatorId;
        public PreySaveV1[] Prey;
        public PredatorSaveV1[] Predators;
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
        /// <summary>文化技艺位，见 <see cref="ApeState.CultureFlags"/>；旧档缺省=0。</summary>
        public int CultureFlags;
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
