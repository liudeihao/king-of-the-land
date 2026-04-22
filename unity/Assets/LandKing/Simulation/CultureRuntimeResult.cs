using System.Collections.Generic;

namespace LandKing.Simulation
{
    /// <summary>合并后的文化技艺表：供模拟与 UI 展示。</summary>
    public sealed class CultureRuntimeResult
    {
        /// <summary>前置边已满足时的处理序（父技艺先于子技艺）。</summary>
        public IReadOnlyList<CultureSkillDef> SkillsInDependencyOrder { get; internal set; }

        public IReadOnlyDictionary<string, CultureSkillDef> ById { get; internal set; }

        public static CultureRuntimeResult Empty { get; } = new CultureRuntimeResult
        {
            SkillsInDependencyOrder = new CultureSkillDef[0],
            ById = new Dictionary<string, CultureSkillDef>()
        };
    }
}
