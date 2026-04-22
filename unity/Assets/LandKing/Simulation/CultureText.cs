using System.Collections.Generic;

namespace LandKing.Simulation
{
    /// <summary>个体文化栏位展示（按定义序排列已会技艺）。</summary>
    public static class CultureText
    {
        public static string FormatLine(CultureRuntimeResult cult, IReadOnlyList<string> skillIds)
        {
            if (skillIds == null || skillIds.Count == 0) return "无";
            if (cult == null || cult.SkillsInDependencyOrder == null || cult.ById == null)
                return string.Join("、", skillIds);
            var had = new HashSet<string>(skillIds);
            var parts = new List<string>(skillIds.Count);
            for (var i = 0; i < cult.SkillsInDependencyOrder.Count; i++)
            {
                var d = cult.SkillsInDependencyOrder[i];
                if (d == null || string.IsNullOrEmpty(d.Id)) continue;
                if (!had.Contains(d.Id)) continue;
                parts.Add(!string.IsNullOrEmpty(d.DisplayName) ? d.DisplayName : d.Id);
            }
            for (var i = 0; i < skillIds.Count; i++)
            {
                var id = skillIds[i];
                if (string.IsNullOrEmpty(id)) continue;
                if (cult.ById.ContainsKey(id)) continue;
                parts.Add(id);
            }
            return parts.Count == 0 ? "无" : string.Join("、", parts);
        }
    }
}
