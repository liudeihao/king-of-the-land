using System;

namespace LandKing.Simulation
{
    /// <summary>Single place for <see cref="WorldEventRecord"/> → UI/日志 文本，避免与 <see cref="EventLog"/> 重复。</summary>
    public static class WorldEventFormatting
    {
        public static string ToDisplayString(in WorldEventRecord e)
        {
            if (e.Tick < 0) return e.Message ?? string.Empty;
            var tag = e.Kind switch
            {
                WorldEventKind.DroughtStart => "旱起",
                WorldEventKind.DroughtSevere => "旱情",
                WorldEventKind.Rain => "降雨",
                WorldEventKind.Birth => "出生",
                WorldEventKind.Starvation => "饥亡",
                WorldEventKind.NaturalDeath => "寿终",
                WorldEventKind.FoodDepleted => "果尽",
                WorldEventKind.EastShore => "东岸",
                WorldEventKind.SkillLearned => "传艺",
                WorldEventKind.SkillExtinct => "艺绝",
                WorldEventKind.PreyHunted => "猎食",
                WorldEventKind.Predation => "掠食",
                _ => e.Kind.ToString()
            };
            return $"[t{e.Tick}][{tag}] {e.Message}";
        }
    }
}
