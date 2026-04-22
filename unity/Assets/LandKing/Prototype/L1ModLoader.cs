using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LandKing.Simulation;
using UnityEngine;

namespace LandKing.Prototype
{
    [Serializable]
    public sealed class ModManifest
    {
        public string id;
        public string name;
        public string version;
    }

    /// <summary>从 <c>StreamingAssets/Mods/&lt;folder&gt;/</c> 按文件夹名顺序加载 L1 数据：可选 <c>mod.json</c>，<c>sim_params.json</c> 为补丁表。</summary>
    public static class L1ModLoader
    {
        public const string ModsSubfolder = "Mods";

        public sealed class Result
        {
            public SimParams Sim;
            public IReadOnlyList<string> ModFolderNames;
            public IReadOnlyList<string> ModDisplayNames;
        }

        public static Result Load()
        {
            var sim = SimParams.Default.Copy();
            var folderNames = new List<string>();
            var display = new List<string>();
            var root = Path.Combine(Application.streamingAssetsPath, ModsSubfolder);
            if (!Directory.Exists(root))
                return new Result { Sim = sim, ModFolderNames = Array.Empty<string>(), ModDisplayNames = Array.Empty<string>() };
            var dirs = Directory.GetDirectories(root).OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToArray();
            foreach (var dir in dirs)
            {
                var folder = Path.GetFileName(dir);
                if (string.IsNullOrEmpty(folder)) continue;
                var paramsPath = Path.Combine(dir, "sim_params.json");
                if (!File.Exists(paramsPath)) continue;
                var json = File.ReadAllText(paramsPath);
                var file = JsonUtility.FromJson<L1ParamPatchFile>(json);
                SimParamsPatchUtil.Apply(sim, file);
                folderNames.Add(folder);
                var manPath = Path.Combine(dir, "mod.json");
                if (File.Exists(manPath))
                {
                    var m = JsonUtility.FromJson<ModManifest>(File.ReadAllText(manPath));
                    if (m != null)
                    {
                        if (!string.IsNullOrEmpty(m.name)) display.Add(m.name);
                        else if (!string.IsNullOrEmpty(m.id)) display.Add(m.id);
                        else display.Add(folder);
                    }
                    else display.Add(folder);
                }
                else display.Add(folder);
            }
            return new Result { Sim = sim, ModFolderNames = folderNames, ModDisplayNames = display };
        }
    }
}
