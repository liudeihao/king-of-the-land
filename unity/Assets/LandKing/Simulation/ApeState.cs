namespace LandKing.Simulation
{
    /// <summary>
    /// 可观察的猿状态（里程碑二起；里程碑三增加性格/性别/亲缘/生命阶段）。
    /// </summary>
    public struct ApeState
    {
        public int Id;
        public float Hunger;
        public float Health;
        public float Age;
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
