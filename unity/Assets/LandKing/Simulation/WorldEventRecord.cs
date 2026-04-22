using System;

namespace LandKing.Simulation
{
    /// <summary>One row in the in-world event chronicle, tied to sim tick when applicable.</summary>
    [Serializable]
    public struct WorldEventRecord
    {
        public int Tick;
        public WorldEventKind Kind;
        public string Message;
    }
}
