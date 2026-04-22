using System;
using System.Collections.Generic;
using LandKing.Simulation;

namespace LandKing.Prototype
{
    /// <summary>本局 L1 解析结果与 Mod 持久化分桶（内存态；F5 写入 <see cref="WorldSaveV1.l1ModPersistent"/>）。</summary>
    public static class L1ModSession
    {
        public static string[] Folders = Array.Empty<string>();
        public static string[] DisplayNames = Array.Empty<string>();
        /// <summary>与 <see cref="Folders"/> 同序的 mod id。</summary>
        public static string[] ModIds = Array.Empty<string>();

        private static readonly Dictionary<string, string> PersistentByModId =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static void ApplyFrom(L1ModLoader.Result r)
        {
            if (r == null)
            {
                Folders = Array.Empty<string>();
                DisplayNames = Array.Empty<string>();
                ModIds = Array.Empty<string>();
                return;
            }
            Folders = ToArray(r.ModFolderNames);
            DisplayNames = ToArray(r.ModDisplayNames);
            ModIds = ToArray(r.ModIds);
        }

        public static void ClearPersistent() => PersistentByModId.Clear();

        public static void SetPersistentData(string modId, string json)
        {
            if (string.IsNullOrEmpty(modId)) return;
            if (string.IsNullOrEmpty(json))
            {
                PersistentByModId.Remove(modId);
                return;
            }
            PersistentByModId[modId] = json;
        }

        public static string GetPersistentData(string modId) =>
            string.IsNullOrEmpty(modId) ? null :
            PersistentByModId.TryGetValue(modId, out var s) ? s : null;

        public static L1ModPersistentV1[] ToSaveRecords()
        {
            if (PersistentByModId.Count == 0) return null;
            var keys = new List<string>(PersistentByModId.Keys);
            keys.Sort(StringComparer.OrdinalIgnoreCase);
            var a = new L1ModPersistentV1[keys.Count];
            for (var i = 0; i < keys.Count; i++)
            {
                var k = keys[i];
                a[i] = new L1ModPersistentV1 { modId = k, dataJson = PersistentByModId[k] ?? string.Empty };
            }
            return a;
        }

        public static void ApplyPersistentFromSave(L1ModPersistentV1[] records)
        {
            PersistentByModId.Clear();
            if (records == null) return;
            for (var i = 0; i < records.Length; i++)
            {
                var e = records[i];
                if (e == null || string.IsNullOrEmpty(e.modId)) continue;
                PersistentByModId[e.modId.Trim()] = e.dataJson ?? string.Empty;
            }
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
