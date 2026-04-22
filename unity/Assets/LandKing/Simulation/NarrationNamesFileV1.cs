using System;

namespace LandKing.Simulation
{
    /// <summary>L1 可选 <c>narration_names.json</c>；多包按依赖序去重合并。</summary>
    [Serializable]
    public sealed class NarrationNamesFileV1
    {
        public string[] settlements;
        public string[] callNames;
    }
}
