using System.IO;
using LandKing.Simulation;
using UnityEngine;

namespace LandKing.Prototype
{
    /// <summary>将 <see cref="WorldSaveV1"/> 以 JSON 写入 <c>Application.persistentDataPath</c>；与 L1 并行，后续可加 Mod 集哈希与 schema。</summary>
    public static class GameSaveV1File
    {
        public const string FileName = "landking_save_v1.json";

        public static string FullPath => Path.Combine(Application.persistentDataPath, FileName);

        public static void Write(WorldManager world)
        {
            if (world == null || world.Sim == null) return;
            var data = world.Sim.ExportSave();
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
                var sim = WorldSimulation.FromSave(data);
                world.ReplaceSimulation(sim, clearSelection: true);
                return true;
            }
            catch (System.Exception e)
            {
                error = e.Message;
                return false;
            }
        }
    }
}
