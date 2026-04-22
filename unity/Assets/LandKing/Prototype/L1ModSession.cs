using System;
using System.Collections.Generic;

namespace LandKing.Prototype
{
    /// <summary>本局 L1 解析结果快照，供存档写入与与读档时比对（内存态，不序列化本类自身）。</summary>
    public static class L1ModSession
    {
        public static string[] Folders = Array.Empty<string>();
        public static string[] DisplayNames = Array.Empty<string>();

        public static void ApplyFrom(L1ModLoader.Result r)
        {
            if (r == null) { Folders = Array.Empty<string>(); DisplayNames = Array.Empty<string>(); return; }
            Folders = ToArray(r.ModFolderNames);
            DisplayNames = ToArray(r.ModDisplayNames);
        }

        private static string[] ToArray(IReadOnlyList<string> list)
        {
            if (list == null || list.Count == 0) return Array.Empty<string>();
            var a = new string[list.Count];
            for (var i = 0; i < list.Count; i++) a[i] = list[i] ?? string.Empty;
            return a;
        }
    }
}
