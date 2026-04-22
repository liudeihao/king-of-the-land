using System;
using System.Globalization;
using System.Reflection;
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

    /// <summary>将补丁应用到 <see cref="SimParams"/>：<c>key</c> 为全部<strong>公开实例字段</strong>名（与 C# 中一致、大小写不敏感）。</summary>
    public static class SimParamsPatchUtil
    {
        private static readonly FieldInfo[] SimParamInstanceFields = typeof(SimParams).GetFields(BindingFlags.Public | BindingFlags.Instance);

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
            FieldInfo field = null;
            for (var i = 0; i < SimParamInstanceFields.Length; i++)
            {
                if (string.Equals(SimParamInstanceFields[i].Name, key, StringComparison.OrdinalIgnoreCase)) { field = SimParamInstanceFields[i]; break; }
            }
            if (field == null)
            {
                Debug.LogWarning($"[L1] sim_params: unknown field \"{key}\" ignored. Use a public field name on SimParams.");
                return;
            }
            var ft = field.FieldType;
            if (ft == typeof(int))
            {
                if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)) field.SetValue(t, n);
            }
            else if (ft == typeof(float))
            {
                if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var f)) field.SetValue(t, f);
            }
            else if (ft == typeof(double))
            {
                if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)) field.SetValue(t, d);
            }
        }
    }
}
