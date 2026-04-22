using System.IO;
using LandKing.Simulation;
using UnityEngine;

namespace LandKing.Prototype
{
    /// <summary>新局时从各 Mod 目录读入 <c>l1_initial_persistent.json</c> 并写入 <see cref="L1ModSession"/>；F5 时随 <see cref="WorldSaveV1.l1ModPersistent"/> 持久化。</summary>
    public static class L1ModPersistence
    {
        public static void OnNewGame(L1ModLoader.Result r)
        {
            L1ModSession.ClearPersistent();
            if (r == null || !r.Success) return;
            if (r.ModFolderNames == null || r.ModIds == null) return;
            if (r.ModFolderNames.Count != r.ModIds.Count) return;
            var root = Path.Combine(Application.streamingAssetsPath, L1ModLoader.ModsSubfolder);
            for (var i = 0; i < r.ModFolderNames.Count; i++)
            {
                var folder = r.ModFolderNames[i];
                var modId = r.ModIds[i];
                if (string.IsNullOrEmpty(modId)) continue;
                var path = Path.Combine(root, folder, L1DataFiles.InitialPersistent);
                if (!File.Exists(path)) continue;
                var json = File.ReadAllText(path);
                L1ModSession.SetPersistentData(modId, string.IsNullOrEmpty(json) ? "{}" : json);
            }
        }
    }
}
