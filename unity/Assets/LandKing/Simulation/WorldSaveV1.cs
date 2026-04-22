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
        public string[] l1ModFolders;
        public string[] l1ModDisplayNames;
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
    }
}
