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

        /// <summary>已会技艺的说明条（多行，用于详情面板；无描述则只列名）。</summary>
        public static string FormatSkillDescriptionsBlock(CultureRuntimeResult cult, string[] skillIds)
        {
            if (skillIds == null || skillIds.Length == 0) return string.Empty;
            if (cult == null || cult.SkillsInDependencyOrder == null) return string.Empty;
            var had = new System.Collections.Generic.HashSet<string>(skillIds);
            var b = new System.Text.StringBuilder();
            for (var i = 0; i < cult.SkillsInDependencyOrder.Count; i++)
            {
                var d = cult.SkillsInDependencyOrder[i];
                if (d == null || string.IsNullOrEmpty(d.Id) || !had.Contains(d.Id)) continue;
                var name = !string.IsNullOrEmpty(d.DisplayName) ? d.DisplayName : d.Id;
                b.Append("· ");
                b.AppendLine(name);
                if (!string.IsNullOrEmpty(d.Description))
                {
                    b.Append("  ");
                    b.AppendLine(d.Description);
                }
            }
            for (var i = 0; i < skillIds.Length; i++)
            {
                var id = skillIds[i];
                if (string.IsNullOrEmpty(id) || (cult.ById != null && cult.ById.ContainsKey(id))) continue;
                b.Append("· ");
                b.AppendLine(id);
            }
            return b.Length == 0 ? string.Empty : b.ToString().TrimEnd();
        }
    }
}
