using System;

namespace LandKing.Simulation
{
    /// <summary>L1 数据表：<c>StreamingAssets/Mods/.../wildlife.json</c>，与 <c>sim_params.json</c> 同批依赖序叠放。</summary>
    [Serializable]
    public sealed class WildlifeFileV1
    {
        /// <summary>为 true 时，在处理本文件前清空已合并的种表（可完全替换内建默认）。</summary>
        public bool clearBaseline;
        public WildlifeSpeciesDef[] species;
    }
}
