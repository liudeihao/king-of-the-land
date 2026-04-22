using System;
using System.IO;
using LandKing.Simulation;
using UnityEngine;

namespace LandKing.Prototype
{
    /// <summary>将 <see cref="WorldSaveV1"/> 以 JSON 写入 <c>Application.persistentDataPath</c>；附带 L1 清单与读档校验。</summary>
    public static class GameSaveV1File
    {
        public const string FileName = "landking_save_v1.json";

        public static string FullPath => Path.Combine(Application.persistentDataPath, FileName);

        public static void Write(WorldManager world)
        {
            if (world == null || world.Sim == null) return;
            var data = world.Sim.ExportSave();
            data.l1ModFolders = L1ModSession.Folders;
            data.l1ModDisplayNames = L1ModSession.DisplayNames;
            data.l1ModIds = L1ModSession.ModIds;
            data.l1ModPersistent = L1ModSession.ToSaveRecords();
            var json = JsonUtility.ToJson(data, true);
            File.WriteAllText(FullPath, json);
            Debug.Log($"[LandKing] 已存盘: {FullPath} (Tick {data.TickCount}, seed {data.RandomSeed})");
        }

        public static bool TryRead(WorldManager world, out string error)
        {
            error = null;
            if (world == null) { error = "WorldManager 为空"; return false; }
            if (!File.Exists(FullPath)) { error = "无存档: " + FullPath; return false; }
            var json = File.ReadAllText(FullPath);
            var data = JsonUtility.FromJson<WorldSaveV1>(json);
            if (data == null) { error = "JSON 反序列化失败"; return false; }
            try
            {
                var mods = L1ModLoader.Load();
                L1ModSession.ApplyFrom(mods);
                if (mods == null || !mods.Success)
                {
                    if (mods?.Errors != null && mods.Errors.Count > 0) error = "L1 未加载: " + string.Join(" ", mods.Errors);
                    else error = "L1 加载失败。";
                    return false;
                }
                if (data.l1ModIds != null && data.l1ModIds.Length > 0)
                {
                    if (!SameStringSequence(data.l1ModIds, L1ModSession.ModIds))
                    {
                        var want = string.Join("->", data.l1ModIds);
                        var have = L1ModSession.ModIds == null || L1ModSession.ModIds.Length == 0
                            ? "(无)"
                            : string.Join("->", L1ModSession.ModIds);
                        error = "读档拒绝：当前 L1 的 mod id 顺序与存档不一致。存:" + want + " 现:" + have + "。";
                        return false;
                    }
                }
                var p = data.Params != null ? data.Params : SimParams.Default;
                var cult = mods.Culture ?? CultureTableBuilder.FromParamsOnly(p);
                var sim = WorldSimulation.FromSave(data, cult);
                world.ReplaceSimulation(sim, clearSelection: true);
                L1ModSession.ApplyPersistentFromSave(data.l1ModPersistent);
                CompareL1ToSession(data, world.GetComponent<EventLog>());
                return true;
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
        }

        private static void CompareL1ToSession(WorldSaveV1 data, EventLog log)
        {
            if (data.l1ModIds != null && data.l1ModIds.Length > 0) return;
            if (data.l1ModFolders == null || data.l1ModFolders.Length == 0) return;
            var cur = L1ModSession.Folders;
            if (cur == null) cur = Array.Empty<string>();
            if (SameStringSequence(data.l1ModFolders, cur)) return;
            var msg = "读档：当前 L1 文件夹顺序与存盘时不同 (存: " + string.Join("->", data.l1ModFolders) + " 现: " + string.Join("->", cur) + ")。世界状态以存档内 SimParams 与地图为准。";
            if (log != null) log.Add(msg);
            Debug.LogWarning("[LandKing] " + msg);
        }

        private static bool SameStringSequence(string[] a, string[] b)
        {
            if (a == null) a = Array.Empty<string>();
            if (b == null) b = Array.Empty<string>();
            if (a.Length != b.Length) return false;
            for (var i = 0; i < a.Length; i++)
            {
                if (!string.Equals(a[i] ?? string.Empty, b[i] ?? string.Empty, StringComparison.OrdinalIgnoreCase)) return false;
            }
            return true;
        }
    }
}
