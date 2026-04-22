using System;
using LandKing.Simulation;

namespace LandKing.Prototype
{
    /// <summary>本局 L2 脚本包 id 列表（与 <see cref="L1ModLoader.Result.L2ScriptEntries"/> 同序，随 F5 写入 <see cref="WorldSaveV1.l2ModIds"/>）。</summary>
    public static class L2ModSession
    {
        public static string[] ModIds = Array.Empty<string>();

        public static void ApplyFrom(L1ModLoader.Result r)
        {
            if (r == null || !r.Success || r.L2ScriptEntries == null || r.L2ScriptEntries.Count == 0)
            {
                ModIds = Array.Empty<string>();
                return;
            }
            var n = r.L2ScriptEntries.Count;
            var a = new string[n];
            for (var i = 0; i < n; i++)
                a[i] = r.L2ScriptEntries[i] != null ? (r.L2ScriptEntries[i].modId ?? string.Empty) : string.Empty;
            ModIds = a;
        }
    }
}
