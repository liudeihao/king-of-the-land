using System;

namespace LandKing.Simulation
{
    /// <summary>存档头与 v1 世界快照（与《微内核》§6 方向一致；不含 Mod 清单，由宿主另外写或后续版本合并）。</summary>
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
