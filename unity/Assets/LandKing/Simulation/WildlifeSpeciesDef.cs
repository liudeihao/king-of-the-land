using System;

namespace LandKing.Simulation
{
    /// <summary>单条种定义，见 <see cref="WildlifeFileV1"/>；由 L1 包合并（同 <c>id</c> 后写覆盖）。</summary>
    [Serializable]
    public sealed class WildlifeSpeciesDef
    {
        public string id;
        public string kind;
        public int count;
        /// <summary>取食饱食加算；&lt;=0 时用 <see cref="SimParams.PreyMeatHunger"/>。</summary>
        public float meatHunger;
        /// <summary>死后重生延迟 tick；&lt;=0 时用 <see cref="SimParams.PreyRespawnDelayTicks"/>。</summary>
        public int respawnDelayTicks;
        /// <summary>惊扰上界；&lt;=0 时用 <see cref="SimParams.PredatorSpookMaxStress"/>。</summary>
        public float spookMaxStress;
        /// <summary>惊扰格距；&lt;=0 时用 <see cref="SimParams.PredatorSpookRadius"/>。</summary>
        public int spookRadius;
    }
}
