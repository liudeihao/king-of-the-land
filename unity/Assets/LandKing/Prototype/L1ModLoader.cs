using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LandKing.Simulation;
using UnityEngine;

namespace LandKing.Prototype
{
    [Serializable]
    public sealed class ModDependency
    {
        public string id;
        public string version;
    }

    [Serializable]
    public sealed class ModManifest
    {
        public string id;
        public string name;
        public string version;
        public string kind;
        public ModDependency[] dependencies;
        public string[] conflicts;
    }

        public sealed class ModInstance
        {
            public string Folder;
            public string Id;
            public string Version;
            public ModManifest Manifest;
            public bool HasSimParams;
            public bool HasWildlife;
            public bool HasCulture;
        }

    /// <summary>从 <c>StreamingAssets/Mods/&lt;folder&gt;/</c> 发现包，解析 <c>mod.json</c> 依赖/冲突，<c>sim_params.json</c> 按解析顺序合并（见 <c>docs/实现/微内核与Mod扩展.md</c> §9）。</summary>
    public static class L1ModLoader
    {
        public const string ModsSubfolder = "Mods";

        public sealed class Result
        {
            public bool Success;
            public SimParams Sim;
            public WildlifeRuntimeResult Wildlife;
            public CultureRuntimeResult Culture;
            public IReadOnlyList<string> ModFolderNames;
            public IReadOnlyList<string> ModDisplayNames;
            public IReadOnlyList<string> LoadOrderDisplay;
            public IReadOnlyList<string> Errors;
        }

        public static Result Load()
        {
            var err = new List<string>();
            var def = SimParams.Default.Copy();
            var root = Path.Combine(Application.streamingAssetsPath, ModsSubfolder);
            if (!Directory.Exists(root))
            {
                return new Result
                {
                    Success = true,
                    Sim = def,
                    Wildlife = WildlifeTableBuilder.FromParamsOnly(def),
                    Culture = CultureTableBuilder.FromParamsOnly(def),
                    ModFolderNames = Array.Empty<string>(),
                    ModDisplayNames = Array.Empty<string>(),
                    LoadOrderDisplay = Array.Empty<string>(),
                    Errors = Array.Empty<string>()
                };
            }

            var dirs = Directory.GetDirectories(root).OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToArray();
            var packages = new List<ModInstance>();
            var idToFolder = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var dir in dirs)
            {
                var folder = Path.GetFileName(dir);
                if (string.IsNullOrEmpty(folder)) continue;
                var manPath = Path.Combine(dir, "mod.json");
                var paramPath = Path.Combine(dir, "sim_params.json");
                var wildPath = Path.Combine(dir, "wildlife.json");
                var culturePath = Path.Combine(dir, "culture_skills.json");
                var hasMan = File.Exists(manPath);
                var hasParams = File.Exists(paramPath);
                var hasWild = File.Exists(wildPath);
                var hasCulture = File.Exists(culturePath);
                if (!hasMan && !hasParams && !hasWild && !hasCulture) continue;
                ModManifest m = null;
                if (hasMan) m = JsonUtility.FromJson<ModManifest>(File.ReadAllText(manPath));
                var id = m != null && !string.IsNullOrEmpty(m.id) ? m.id : folder;
                if (idToFolder.ContainsKey(id)) { err.Add($"重复 mod id「{id}」：文件夹 {idToFolder[id]} 与 {folder}"); continue; }
                idToFolder[id] = folder;
                var ver = m != null && !string.IsNullOrEmpty(m.version) ? m.version : "0.0.0";
                packages.Add(new ModInstance
                {
                    Folder = folder,
                    Id = id,
                    Version = ver,
                    Manifest = m,
                    HasSimParams = hasParams,
                    HasWildlife = hasWild,
                    HasCulture = hasCulture
                });
            }

            if (err.Count > 0) return Failed(def, err);

            if (HasConflictErrors(packages, err)) return Failed(def, err);
            if (HasDependencyErrors(packages, err)) return Failed(def, err);

            if (!TryTopologicalSort(packages, out var order, out var topErr))
            {
                if (!string.IsNullOrEmpty(topErr)) err.Add(topErr);
                return Failed(def, err);
            }

            var sim = def;
            var wildFiles = new List<WildlifeFileV1>(4);
            var cultureFiles = new List<CultureFileV1>(4);
            var folderOrder = new List<string>();
            var display = new List<string>();
            var loadOrder = new List<string>();
            foreach (var id in order)
            {
                var p = packages.First(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
                if (p.HasSimParams)
                {
                    var json = File.ReadAllText(Path.Combine(root, p.Folder, "sim_params.json"));
                    var file = JsonUtility.FromJson<L1ParamPatchFile>(json);
                    SimParamsPatchUtil.Apply(sim, file);
                }
                if (p.HasWildlife)
                {
                    var wjson = File.ReadAllText(Path.Combine(root, p.Folder, "wildlife.json"));
                    if (!string.IsNullOrEmpty(wjson))
                    {
                        var wf = JsonUtility.FromJson<WildlifeFileV1>(wjson);
                        if (wf != null) wildFiles.Add(wf);
                    }
                }
                if (p.HasCulture)
                {
                    var cj = File.ReadAllText(Path.Combine(root, p.Folder, "culture_skills.json"));
                    if (!string.IsNullOrEmpty(cj))
                    {
                        var cf = JsonUtility.FromJson<CultureFileV1>(cj);
                        if (cf != null) cultureFiles.Add(cf);
                    }
                }
                folderOrder.Add(p.Folder);
                if (p.Manifest != null)
                {
                    if (!string.IsNullOrEmpty(p.Manifest.name)) display.Add(p.Manifest.name);
                    else display.Add(p.Id);
                }
                else display.Add(p.Folder);
                var k = p.Manifest != null && !string.IsNullOrEmpty(p.Manifest.kind) ? p.Manifest.kind : "content";
                loadOrder.Add($"{k}:{p.Id}@{p.Version}");
            }

            var wildlife = WildlifeTableBuilder.Build(sim, wildFiles);
            if (!CultureTableBuilder.TryBuild(sim, cultureFiles, out var culture, out var cultErr))
            {
                if (!string.IsNullOrEmpty(cultErr)) err.Add(cultErr);
                else err.Add("文化技艺表合并失败。");
                return Failed(def, err);
            }

            return new Result
            {
                Success = true,
                Sim = sim,
                Wildlife = wildlife,
                Culture = culture,
                ModFolderNames = folderOrder,
                ModDisplayNames = display,
                LoadOrderDisplay = loadOrder,
                Errors = Array.Empty<string>()
            };
        }

        private static Result Failed(SimParams def, IReadOnlyList<string> e) =>
            new Result
            {
                Success = false,
                Sim = def,
                Wildlife = WildlifeTableBuilder.FromParamsOnly(def),
                Culture = CultureTableBuilder.FromParamsOnly(def),
                ModFolderNames = Array.Empty<string>(),
                ModDisplayNames = Array.Empty<string>(),
                LoadOrderDisplay = Array.Empty<string>(),
                Errors = e
            };

        private static bool HasConflictErrors(List<ModInstance> packages, List<string> err)
        {
            var set = new HashSet<string>(packages.Select(p => p.Id), StringComparer.OrdinalIgnoreCase);
            var had = false;
            foreach (var p in packages)
            {
                if (p.Manifest == null || p.Manifest.conflicts == null) continue;
                foreach (var c in p.Manifest.conflicts)
                {
                    if (string.IsNullOrEmpty(c)) continue;
                    if (set.Contains(c)) { err.Add($"Mod「{p.Id}」声明与「{c}」冲突，但两者同批已加载。请只启用其一。"); had = true; }
                }
            }
            return had;
        }

        private static bool HasDependencyErrors(List<ModInstance> packages, List<string> err)
        {
            var byId = packages.ToDictionary(p => p.Id, p => p, StringComparer.OrdinalIgnoreCase);
            var had = false;
            foreach (var p in packages)
            {
                if (p.Manifest == null || p.Manifest.dependencies == null) continue;
                foreach (var d in p.Manifest.dependencies)
                {
                    if (d == null || string.IsNullOrEmpty(d.id)) continue;
                    if (!byId.TryGetValue(d.id, out var target))
                    {
                        err.Add($"Mod「{p.Id}」依赖「{d.id}」：同批中未找到该包（请把依赖放入 StreamingAssets/Mods/）。");
                        had = true;
                        continue;
                    }
                    if (!ModVersionSpec.SatisfiesRange(target.Version, d.version))
                    {
                        err.Add($"Mod「{p.Id}」需要「{d.id}」版本 {d?.version}，实际 {target.Version}。");
                        had = true;
                    }
                }
            }
            return had;
        }

        private static bool TryTopologicalSort(IReadOnlyList<ModInstance> packages, out List<string> order, out string error)
        {
            order = null;
            error = null;
            var byId = packages.ToDictionary(p => p.Id, p => p, StringComparer.OrdinalIgnoreCase);
            var inDeg = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var rev = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in packages)
            {
                var deps = new List<string>();
                if (p.Manifest != null && p.Manifest.dependencies != null)
                {
                    foreach (var d in p.Manifest.dependencies)
                    {
                        if (d == null || string.IsNullOrEmpty(d.id) || !byId.ContainsKey(d.id)) continue;
                        if (!deps.Contains(d.id, StringComparer.OrdinalIgnoreCase)) deps.Add(d.id);
                    }
                }
                inDeg[p.Id] = deps.Count;
                foreach (var d in deps)
                {
                    if (!rev.ContainsKey(d)) rev[d] = new List<string>();
                    rev[d].Add(p.Id);
                }
            }

            var result = new List<string>();
            var remaining = new HashSet<string>(packages.Select(p => p.Id), StringComparer.OrdinalIgnoreCase);
            for (var guard = 0; guard < 4096 && remaining.Count > 0; guard++)
            {
                var ready = remaining
                    .Where(x => inDeg[x] == 0)
                    .OrderBy(x =>
                    {
                        if (!byId.TryGetValue(x, out var inst)) return 1;
                        if (inst.Manifest == null) return 1;
                        return string.Equals(inst.Manifest.kind, "core", StringComparison.OrdinalIgnoreCase) ? 0 : 1;
                    })
                    .ThenBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (ready.Count == 0)
                {
                    error = "Mod 依赖存在环，无法排加载顺序。请检查相互依赖的 mod。";
                    return false;
                }
                foreach (var u in ready)
                {
                    result.Add(u);
                    remaining.Remove(u);
                    if (!rev.TryGetValue(u, out var outs)) continue;
                    foreach (var v in outs)
                    {
                        if (inDeg.ContainsKey(v)) inDeg[v]--;
                    }
                }
            }
            if (remaining.Count > 0) { error = "Mod 依赖解析未收敛。请检查依赖关系。"; return false; }
            order = result;
            return true;
        }
    }
}
