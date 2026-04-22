using System;
using System.Collections.Generic;

namespace LandKing.Simulation
{
    /// <summary>内建 <see cref="LandKingCultureIds"/> 与 L1 <c>culture_skills.json</c> 合并、依赖校验、拓扑序。</summary>
    public static class CultureTableBuilder
    {
        public static CultureRuntimeResult FromParamsOnly(SimParams p)
        {
            if (TryBuild(p, Array.Empty<CultureFileV1>(), out var r, out _)) return r;
            return CultureRuntimeResult.Empty;
        }

        public static bool TryBuild(SimParams p, IReadOnlyList<CultureFileV1> files, out CultureRuntimeResult result, out string error)
        {
            result = null;
            error = null;
            if (p == null) p = SimParams.Default;
            var dict = new Dictionary<string, CultureEntryV1>(StringComparer.OrdinalIgnoreCase);
            AddCoreDefaults(p, dict);
            if (files != null)
            {
                for (var fi = 0; fi < files.Count; fi++)
                {
                    var f = files[fi];
                    if (f == null) continue;
                    if (f.clearBaseline) dict.Clear();
                    if (f.entries == null) continue;
                    for (var ei = 0; ei < f.entries.Length; ei++)
                    {
                        var e = f.entries[ei];
                        if (e == null || string.IsNullOrEmpty(e.id)) continue;
                        dict[e.id.Trim()] = e;
                    }
                }
            }
            ApplySimParamsOverlay(dict, p);
            return Materialize(dict, out result, out error);
        }

        private static void AddCoreDefaults(SimParams p, IDictionary<string, CultureEntryV1> dict)
        {
            dict[LandKingCultureIds.NutCrack] = new CultureEntryV1
            {
                id = LandKingCultureIds.NutCrack,
                displayName = "坚果敲裂",
                requires = Array.Empty<string>(),
                observeLearn = p.ObserveLearnNutCrack,
                inventPerTick = p.NutCrackInventPerTick,
                initialMentorCount = p.InitialNutCrackMentorCount,
                inventContext = (int)CultureInventContext.NearFruitTree,
                eatHungerBonus = p.NutCrackEatBonus,
                foodMemBoost = 0f
            };
            dict[LandKingCultureIds.FruitScout] = new CultureEntryV1
            {
                id = LandKingCultureIds.FruitScout,
                displayName = "果记精描",
                requires = new[] { LandKingCultureIds.NutCrack },
                observeLearn = p.ObserveLearnFruitScout,
                inventPerTick = p.FruitScoutInventPerTick,
                initialMentorCount = p.InitialFruitScoutMentorCount,
                inventContext = (int)CultureInventContext.NearFruitTree,
                eatHungerBonus = 0f,
                foodMemBoost = p.FruitScoutMemBoost
            };
        }

        private static void ApplySimParamsOverlay(Dictionary<string, CultureEntryV1> dict, SimParams p)
        {
            if (dict.TryGetValue(LandKingCultureIds.NutCrack, out var n) && n != null)
            {
                n.observeLearn = p.ObserveLearnNutCrack;
                n.inventPerTick = p.NutCrackInventPerTick;
                n.initialMentorCount = p.InitialNutCrackMentorCount;
                n.eatHungerBonus = p.NutCrackEatBonus;
            }
            if (dict.TryGetValue(LandKingCultureIds.FruitScout, out var f) && f != null)
            {
                f.observeLearn = p.ObserveLearnFruitScout;
                f.inventPerTick = p.FruitScoutInventPerTick;
                f.initialMentorCount = p.InitialFruitScoutMentorCount;
                f.foodMemBoost = p.FruitScoutMemBoost;
            }
        }

        private static bool Materialize(Dictionary<string, CultureEntryV1> dict, out CultureRuntimeResult result, out string error)
        {
            result = null;
            error = null;
            foreach (var kv in dict)
            {
                var e = kv.Value;
                if (e == null) continue;
                var req = e.requires;
                if (req == null) continue;
                for (var i = 0; i < req.Length; i++)
                {
                    var r = req[i];
                    if (string.IsNullOrEmpty(r)) continue;
                    r = r.Trim();
                    if (!dict.ContainsKey(r))
                    {
                        error = $"文化技艺「{e.id}」的前置「{r}」未在合并表中出现。请补全 id 或调整加载顺序。";
                        return false;
                    }
                }
            }

            if (!TryTopologicalOrder(dict, out var order, out error)) return false;

            var defs = new CultureSkillDef[order.Count];
            var byId = new Dictionary<string, CultureSkillDef>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < order.Count; i++)
            {
                var id = order[i];
                var e = dict[id];
                var req = e.requires;
                if (req == null) req = Array.Empty<string>();
                var cleanReq = new string[req.Length];
                for (var j = 0; j < req.Length; j++) cleanReq[j] = (req[j] ?? string.Empty).Trim();

                var def = new CultureSkillDef
                {
                    Id = id,
                    DisplayName = string.IsNullOrEmpty(e.displayName) ? id : e.displayName,
                    Requires = cleanReq,
                    ObserveLearn = e.observeLearn,
                    InventPerTick = e.inventPerTick,
                    InitialMentorCount = e.initialMentorCount,
                    InventContext = (CultureInventContext)System.Math.Max(0, e.inventContext),
                    EatHungerBonus = e.eatHungerBonus,
                    FoodMemBoost = e.foodMemBoost,
                    MilestoneDiscoveryPhrase = string.IsNullOrEmpty(e.milestoneDiscoveryPhrase) ? null : e.milestoneDiscoveryPhrase.Trim()
                };
                defs[i] = def;
                byId[id] = def;
            }

            result = new CultureRuntimeResult
            {
                SkillsInDependencyOrder = defs,
                ById = byId
            };
            return true;
        }

        private static bool TryTopologicalOrder(Dictionary<string, CultureEntryV1> dict, out List<string> order, out string error)
        {
            order = null;
            error = null;
            var inDeg = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var rev = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in dict) inDeg[kv.Key] = 0;
            foreach (var kv in dict)
            {
                var id = kv.Key;
                var req = kv.Value?.requires;
                if (req == null) continue;
                inDeg[id] = req.Length; // 无 requires 的条目前面已为 0
                for (var i = 0; i < req.Length; i++)
                {
                    var r = (req[i] ?? string.Empty).Trim();
                    if (string.IsNullOrEmpty(r) || !dict.ContainsKey(r)) continue;
                    if (!rev.ContainsKey(r)) rev[r] = new List<string>();
                    rev[r].Add(id);
                }
            }
            var q = new Queue<string>();
            foreach (var kv in inDeg) if (kv.Value == 0) q.Enqueue(kv.Key);
            order = new List<string>(dict.Count);
            var guard = 0;
            while (q.Count > 0)
            {
                if (guard++ > 4096) break;
                var u = q.Dequeue();
                order.Add(u);
                if (!rev.TryGetValue(u, out var outs)) continue;
                for (var i = 0; i < outs.Count; i++)
                {
                    var v = outs[i];
                    if (!inDeg.ContainsKey(v)) continue;
                    inDeg[v]--;
                    if (inDeg[v] == 0) q.Enqueue(v);
                }
            }
            if (order.Count != dict.Count)
            {
                error = "文化技艺依赖图存在环，或 in-degree 不一致。请检查 requires 关系。";
                return false;
            }
            return true;
        }
    }
}
