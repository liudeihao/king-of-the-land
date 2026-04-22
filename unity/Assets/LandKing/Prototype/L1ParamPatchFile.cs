using System;
using System.Globalization;
using LandKing.Simulation;
using UnityEngine;

namespace LandKing.Prototype
{
    /// <summary>L1 数据条目的 JSON 形状，供 <see cref="L1ModLoader"/> 解析（Unity JsonUtility 友好）。</summary>
    [Serializable]
    public sealed class L1ParamPatchFile
    {
        public L1ParamPatch[] patches;
    }

    [Serializable]
    public sealed class L1ParamPatch
    {
        public string key;
        public string value;
    }

    /// <summary>将补丁应用到 <see cref="SimParams"/>（内核字段名，大小写不敏感）。</summary>
    public static class SimParamsPatchUtil
    {
        public static void Apply(SimParams target, L1ParamPatchFile file)
        {
            if (target == null || file?.patches == null) return;
            foreach (var p in file.patches)
            {
                if (p == null || string.IsNullOrEmpty(p.key) || p.value == null) continue;
                ApplyOne(target, p.key.Trim(), p.value.Trim());
            }
        }

        private static void ApplyOne(SimParams t, string key, string value)
        {
            switch (key.ToLowerInvariant())
            {
                case "droughtstarttick": if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var d1)) t.DroughtStartTick = d1; break;
                case "droughtpertick": if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d2)) t.DroughtPerTick = d2; break;
                case "droughtbuttonthreshold": if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d3)) t.DroughtButtonThreshold = d3; break;
                case "foodregenintervalticks": if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var f1)) t.FoodRegenIntervalTicks = f1; break;
                case "matingroll": if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var m1)) t.MatingRoll = m1; break;
                case "maxapecount": if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var a1)) t.MaxApeCount = a1; break;
                case "agepertick": if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var a2)) t.AgePerTick = a2; break;
                case "rainleftwater": if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var r1)) t.RainLeftWater = r1; break;
                case "foodregenwatermultiplier": if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var w1)) t.FoodRegenWaterMultiplier = w1; break;
                case "pregnancydurationticks": if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var p1)) t.PregnancyDurationTicks = p1; break;
                case "elderdeathchance": if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var e1)) t.ElderDeathChance = e1; break;
            }
        }
    }
}
