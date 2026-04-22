namespace LandKing.Simulation
{
    /// <summary>
    /// 可观察的猿状态（里程碑二起；里程碑三性格/亲缘；里程碑五压力与地点记忆强度）。
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
        public bool Alive;
        public string Nickname;
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
    }
}
