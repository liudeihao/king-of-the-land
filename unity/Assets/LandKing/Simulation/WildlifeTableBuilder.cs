using System;
using System.Collections.Generic;

namespace LandKing.Simulation
{
    /// <summary>与 <c>SimParams</c> 内建种合并 + 按 L1 顺序覆盖 <c>wildlife.json</c>，生成运行时组。</summary>
    public static class WildlifeTableBuilder
    {
        public static WildlifeRuntimeResult Build(SimParams p, IReadOnlyList<WildlifeFileV1> files)
        {
            if (p == null) p = SimParams.Default;
            var dict = new Dictionary<string, WildlifeSpeciesDef>(StringComparer.OrdinalIgnoreCase);
            AddCoreDefaults(p, dict);
            if (files != null)
            {
                for (var fi = 0; fi < files.Count; fi++)
                {
                    var f = files[fi];
                    if (f == null) continue;
                    if (f.clearBaseline) dict.Clear();
                    if (f.species == null) continue;
                    for (var si = 0; si < f.species.Length; si++)
                    {
                        var s = f.species[si];
                        if (s == null || string.IsNullOrEmpty(s.id)) continue;
                        dict[s.id] = s;
                    }
                }
            }
            return Materialize(p, dict);
        }

        public static WildlifeRuntimeResult FromParamsOnly(SimParams p) => Build(p, Array.Empty<WildlifeFileV1>());

        private static void AddCoreDefaults(SimParams p, IDictionary<string, WildlifeSpeciesDef> dict)
        {
            if (p.PreyCount > 0)
            {
                dict[WildlifeIds.CoreGrassPrey] = new WildlifeSpeciesDef
                {
                    id = WildlifeIds.CoreGrassPrey,
                    kind = WildlifeKinds.PreyWander,
                    count = p.PreyCount,
                    meatHunger = 0f,
                    respawnDelayTicks = 0,
                    spookMaxStress = 0f,
                    spookRadius = 0
                };
            }
            if (p.PredatorCount > 0)
            {
                dict[WildlifeIds.CoreStalker] = new WildlifeSpeciesDef
                {
                    id = WildlifeIds.CoreStalker,
                    kind = WildlifeKinds.PredatorHuntApe,
                    count = p.PredatorCount,
                    meatHunger = 0f,
                    respawnDelayTicks = 0,
                    spookMaxStress = 0f,
                    spookRadius = 0
                };
            }
        }

        private static WildlifeRuntimeResult Materialize(SimParams p, Dictionary<string, WildlifeSpeciesDef> dict)
        {
            var r = new WildlifeRuntimeResult();
            foreach (var kv in dict)
            {
                var s = kv.Value;
                if (s == null || s.count <= 0) continue;
                var k = s.kind ?? string.Empty;
                if (WildlifeKinds.IsPrey(k))
                {
                    var meat = s.meatHunger > 0.0001f ? s.meatHunger : p.PreyMeatHunger;
                    var del = s.respawnDelayTicks > 0 ? s.respawnDelayTicks : System.Math.Max(0, p.PreyRespawnDelayTicks);
                    r.PreyGroups.Add(new PreyGroup
                    {
                        SpeciesId = s.id ?? kv.Key,
                        Count = s.count,
                        MeatHunger = meat,
                        RespawnDelayTicks = del
                    });
                }
                else if (WildlifeKinds.IsPredator(k))
                {
                    var sm = s.spookMaxStress > 0.0001f ? s.spookMaxStress : p.PredatorSpookMaxStress;
                    var sr = s.spookRadius > 0 ? s.spookRadius : p.PredatorSpookRadius;
                    r.PredatorGroups.Add(new PredatorGroup
                    {
                        SpeciesId = s.id ?? kv.Key,
                        Count = s.count,
                        SpookMaxStress = sm,
                        SpookRadius = System.Math.Max(0, sr)
                    });
                }
            }
            return r;
        }
    }
}
