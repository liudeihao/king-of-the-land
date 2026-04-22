namespace LandKing.Simulation
{
    /// <summary>只读：原型 UI 绘制地上猎物走兽（无模拟状态）。</summary>
    public readonly struct PreyView
    {
        public readonly int Id;
        public readonly int X;
        public readonly int Y;
        public readonly string SpeciesId;
        public PreyView(int id, int x, int y, string speciesId = null) { Id = id; X = x; Y = y; SpeciesId = speciesId; }
    }

    /// <summary>只读：掠食者格点。</summary>
    public readonly struct PredatorView
    {
        public readonly int Id;
        public readonly int X;
        public readonly int Y;
        public readonly string SpeciesId;
        public PredatorView(int id, int x, int y, string speciesId = null) { Id = id; X = x; Y = y; SpeciesId = speciesId; }
    }
}
