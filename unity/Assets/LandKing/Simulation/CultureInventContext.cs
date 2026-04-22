namespace LandKing.Simulation
{
    /// <summary>自发明/观摩生效所需的场地上下文（可扩展，供 Mod 选用）。</summary>
    public enum CultureInventContext
    {
        /// <summary>无额外格条件（由 Mod 扩展逻辑解释）。</summary>
        None = 0,
        /// <summary>当前格或四邻有结果树且果量 &gt; 0.01（与现有敲果/果记一致）。</summary>
        NearFruitTree = 1
    }
}
