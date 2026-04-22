namespace LandKing.Simulation
{
    /// <summary>
    /// 可观察的猿状态（里程碑二起；里程碑三亲缘；里程碑五压力、果记、同族印象）。
    /// </summary>
    public struct ApeState
    {
        public int Id;
        public float Hunger;
        public float Health;
        public float Age;
        /// <summary>0=平静 1=紧绷（简化情绪轴）。</summary>
        public float Stress;
        /// <summary>0..1，最近饱食果树的地点记忆强度。</summary>
        public float FoodMemoryStrength;
        /// <summary>社交印象所指的同族 ID，-1=无。</summary>
        public int PeerImpressionId;
        /// <summary>0..1 同族「印象」强度。</summary>
        public float PeerImpressionStrength;
        public bool Alive;
        public string Nickname;
        /// <summary>开局/出生自随机池的称呼，可被 <see cref="Nickname"/> 覆盖展示。</summary>
        public string GivenName;
        public int GridX;
        public int GridY;
        public ApeSide Side;
        public bool IsMale;
        public float Courage;
        public float Curiosity;
        public LifeStage Stage;
        public int ParentId0;
        public int ParentId1;
        public float BodyScale;
        /// <summary>已掌握的文化技艺 id 列表（见 L1 <c>culture_skills.json</c> 合并表）。</summary>
        public string[] CultureSkillIds;
    }
}
