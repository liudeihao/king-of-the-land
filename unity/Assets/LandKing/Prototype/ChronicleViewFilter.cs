using LandKing.Simulation;

namespace LandKing.Prototype
{
    /// <summary>右侧编年史显示模式；筛选仅影响展示，不丢事件。</summary>
    public enum ChronicleViewFilter
    {
        /// <summary>与模拟产出一致（截断上限内）。</summary>
        All = 0,
        /// <summary>隐藏高频细条：猎食、传艺；其余保留。</summary>
        Highlights = 1
    }

    public static class ChronicleViewFilterUtil
    {
        public static bool IsKindVisible(WorldEventKind k, ChronicleViewFilter f)
        {
            if (f == ChronicleViewFilter.All) return true;
            if (k == WorldEventKind.PreyHunted) return false;
            if (k == WorldEventKind.SkillLearned) return false;
            return true;
        }

        public static string Label(ChronicleViewFilter f) =>
            f == ChronicleViewFilter.Highlights ? "要事" : "全部";
    }
}
