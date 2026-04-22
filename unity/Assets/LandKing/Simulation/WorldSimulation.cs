using System;
using System.Collections.Generic;

namespace LandKing.Simulation
{
    /// <summary>Headless world: 双岸、旱/雨、东岸；压力/勇气/好奇；地点与同族记忆；婴幼少弱随亲；可配置文化技艺（依赖图）+文化断裂；地上猎物/掠食者。编年史为 <see cref="WorldEventRecord"/>。Tick 分阶段，<see cref="SimParams"/>（可与 L1 合并）。</summary>
    public sealed class WorldSimulation
    {
        private readonly SimParams _p;
        private readonly CultureRuntimeResult _culture;
        private SimRng _rng;
        private readonly int _initialSeed;
        private readonly MapData _map;
        private readonly List<ApeCell> _apes;
        private readonly List<int> _newViewApeIds = new List<int>(4);
        private readonly List<WorldEventRecord> _eventQueue = new List<WorldEventRecord>(12);
        private List<WorldEventRecord> _chronicle;
        private int _tickCount;
        private int _nextId;
        private float _waterLeft = 1f;
        private float _waterRight = 1f;
        private bool _droughtActive;
        private bool _rainUsed;
        private bool _droughtLogged;
        private bool _droughtSevereLogged;
        private bool _eastHintLogged;
        private readonly List<PreyEntity> _prey;
        private readonly List<PredatorEntity> _predators;
        private int _nextPreyId;
        private int _nextPredatorId;
        private readonly WildlifeRuntimeResult _wildMaterialized;
        private HashSet<string> _milestoneFirstDone;
        private bool _milestoneEventsEnabled;
        private string _settlementNameLeft;
        private string _settlementNameRight;

        public WorldSimulation(int randomSeed = 42, SimParams parameters = null, WildlifeRuntimeResult wildlife = null, CultureRuntimeResult culture = null)
        {
            _milestoneEventsEnabled = false;
            _initialSeed = randomSeed;
            _p = parameters != null ? parameters.Copy() : SimParams.Default.Copy();
            _wildMaterialized = wildlife ?? WildlifeTableBuilder.FromParamsOnly(_p);
            _culture = culture ?? CultureTableBuilder.FromParamsOnly(_p);
            _chronicle = new List<WorldEventRecord>(ChronicleCap());
            _rng = new SimRng(randomSeed);
            InitSettlementNamesForNewGame();
            _map = CreateMapAndFood(_rng, _p, out var leftCells, out var rightCells);
            _apes = new List<ApeCell>(20);
            _prey = new List<PreyEntity>(8);
            _predators = new List<PredatorEntity>(2);
            _nextPreyId = 0;
            _nextPredatorId = 0;
            _nextId = 0;
            _nextId = PlaceInitialApes(_rng, _apes, leftCells, rightCells, _nextId);
            GrantAllInitialMentors();
            FillDefaultWildlife();
            _milestoneFirstDone = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _milestoneEventsEnabled = true;
        }

        private WorldSimulation(SimParams p, MapData map, List<ApeCell> apes, int tickCount, int nextId, int initialSeed, SimRng rng,
            float waterLeft, float waterRight, bool droughtActive, bool rainUsed, bool droughtLogged,
            List<PreyEntity> prey, List<PredatorEntity> predators, int nextPreyId, int nextPredId, bool fillDefaultWildIfEmpty, CultureRuntimeResult culture)
        {
            _p = p;
            _culture = culture ?? CultureTableBuilder.FromParamsOnly(p);
            _map = map;
            _apes = apes;
            _tickCount = tickCount;
            _nextId = nextId;
            _initialSeed = initialSeed;
            _rng = rng;
            _waterLeft = waterLeft;
            _waterRight = waterRight;
            _droughtActive = droughtActive;
            _rainUsed = rainUsed;
            _droughtLogged = droughtLogged;
            _prey = prey ?? new List<PreyEntity>(8);
            _predators = predators ?? new List<PredatorEntity>(2);
            _nextPreyId = nextPreyId;
            _nextPredatorId = nextPredId;
            _wildMaterialized = WildlifeTableBuilder.FromParamsOnly(p);
            if (fillDefaultWildIfEmpty) FillDefaultWildlife();
        }

        public static WorldSimulation FromSave(WorldSaveV1 data, CultureRuntimeResult culture = null)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.Schema != 1) throw new InvalidOperationException($"WorldSave  schema {data.Schema} 不支持，需要 1。");
            if (data.MapTiles == null || data.MapFood == null) throw new InvalidOperationException("存档缺少地图数据。");
            if (data.Apes == null) throw new InvalidOperationException("存档缺少个体数据。");
            var p = data.Params != null ? data.Params.Copy() : SimParams.Default.Copy();
            var cult = culture ?? CultureTableBuilder.FromParamsOnly(p);
            var map = RebuildMap(data.MapTiles, data.MapFood);
            var apes = RebuildApes(data.Apes);
            var rng = new SimRng(data.RngState);
            var prey = RebuildPreyList(data.Prey, p, out var npid);
            var preds = RebuildPredList(data.Predators, p, out var npd);
            if (data.NextPreyId > 0) npid = data.NextPreyId;
            if (data.NextPredatorId > 0) npd = data.NextPredatorId;
            var fillEmpty = data.Prey == null || data.Prey.Length == 0;
            var sim = new WorldSimulation(
                p, map, apes, data.TickCount, data.NextId, data.RandomSeed, rng,
                data.WaterLeft, data.WaterRight, data.DroughtActive, data.RainUsed, data.DroughtLogged,
                prey, preds, npid, npd, fillEmpty, cult);
            sim.LoadChronicleFromSave(data);
            sim.HydrateNarrationFlags();
            sim.HydrateMilestonesFromSave(data.MilestoneFirstDiscoveryKeys);
            sim.HydrateSettlementNamesFromSave(data.SettlementNameLeft, data.SettlementNameRight, data.RandomSeed);
            return sim;
        }

        public int InitialSeed => _initialSeed;
        public SimRng Rng => _rng;
        public WorldSaveV1 ExportSave()
        {
            var m = (int)MapData.Size;
            var n = m * m;
            var tiles = new int[n];
            var food = new float[n];
            for (var y = 0; y < m; y++)
            for (var x = 0; x < m; x++)
            {
                var i = y * m + x;
                tiles[i] = (int)_map.Tiles[x, y];
                food[i] = _map.Food[x, y];
            }
            var rec = new ApeSaveRecord[_apes.Count];
            for (var i = 0; i < _apes.Count; i++) rec[i] = _apes[i].ToSave();
            var data = new WorldSaveV1
            {
                Schema = 1,
                RandomSeed = _initialSeed,
                RngState = _rng.State,
                TickCount = _tickCount,
                NextId = _nextId,
                WaterLeft = _waterLeft,
                WaterRight = _waterRight,
                DroughtActive = _droughtActive,
                RainUsed = _rainUsed,
                DroughtLogged = _droughtLogged,
                Params = _p.Copy(),
                MapTiles = tiles,
                MapFood = food,
                Apes = rec,
                NextPreyId = _nextPreyId,
                NextPredatorId = _nextPredatorId
            };
            if (_prey != null && _prey.Count > 0)
            {
                var pn = _prey.Count;
                data.Prey = new PreySaveV1[pn];
                for (var i = 0; i < pn; i++)
                {
                    var pr = _prey[i];
                    data.Prey[i] = new PreySaveV1
                    {
                        Id = pr.Id, X = pr.X, Y = pr.Y, Alive = pr.Alive, RespawnAtTick = pr.RespawnAtTick,
                        speciesId = pr.SpeciesId, meatHunger = pr.MeatHunger, respawnDelayTicks = pr.RespawnDelayTicks
                    };
                }
            }
            if (_predators != null && _predators.Count > 0)
            {
                var k = _predators.Count;
                data.Predators = new PredatorSaveV1[k];
                for (var i = 0; i < k; i++)
                {
                    var d = _predators[i];
                    data.Predators[i] = new PredatorSaveV1
                    {
                        Id = d.Id, X = d.X, Y = d.Y,
                        speciesId = d.SpeciesId, spookMaxStress = d.SpookMaxStress, spookRadius = d.SpookRadius
                    };
                }
            }
            if (_chronicle != null && _chronicle.Count > 0)
            {
                var cn = _chronicle.Count;
                data.Chronicle = new WorldEventSaveV1[cn];
                for (var i = 0; i < cn; i++)
                {
                    var e = _chronicle[i];
                    data.Chronicle[i] = new WorldEventSaveV1
                    {
                        Tick = e.Tick,
                        Kind = (int)e.Kind,
                        Message = e.Message ?? string.Empty
                    };
                }
            }
            if (_milestoneFirstDone != null && _milestoneFirstDone.Count > 0)
            {
                var mkeys = new string[_milestoneFirstDone.Count];
                var mi = 0;
                foreach (var s in _milestoneFirstDone) mkeys[mi++] = s;
                data.MilestoneFirstDiscoveryKeys = mkeys;
            }
            data.SettlementNameLeft = _settlementNameLeft;
            data.SettlementNameRight = _settlementNameRight;
            return data;
        }

        private void InitSettlementNamesForNewGame()
        {
            _settlementNameLeft = NarrationNamePools.PickSettlement(_rng);
            _settlementNameRight = NarrationNamePools.PickSettlement(_rng);
            var guard = 0;
            while (_settlementNameRight == _settlementNameLeft && guard++ < 48)
                _settlementNameRight = NarrationNamePools.PickSettlement(_rng);
        }

        private void HydrateSettlementNamesFromSave(string left, string right, int seed)
        {
            if (!string.IsNullOrEmpty(left) && !string.IsNullOrEmpty(right))
            {
                _settlementNameLeft = left;
                _settlementNameRight = right;
                return;
            }
            var r = new SimRng(seed == 0 ? 0x52A7C0DE : seed ^ 0x51A71EE5);
            _settlementNameLeft = NarrationNamePools.PickSettlement(r);
            _settlementNameRight = NarrationNamePools.PickSettlement(r);
            var g = 0;
            while (_settlementNameRight == _settlementNameLeft && g++ < 48)
                _settlementNameRight = NarrationNamePools.PickSettlement(r);
        }

        private void HydrateMilestonesFromSave(string[] keys)
        {
            _milestoneFirstDone = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (keys != null)
            {
                for (var i = 0; i < keys.Length; i++)
                {
                    var s = keys[i];
                    if (!string.IsNullOrEmpty(s)) _milestoneFirstDone.Add(s.Trim());
                }
            }
            _milestoneEventsEnabled = true;
        }

        private void TryRecordMilestoneFirstDiscovery(ApeCell a, string skillId)
        {
            if (!_milestoneEventsEnabled || a == null || !a.Alive) return;
            if (string.IsNullOrEmpty(skillId) || _milestoneFirstDone == null) return;
            if (_milestoneFirstDone.Contains(skillId)) return;
            if (!_culture.ById.TryGetValue(skillId, out var def) || def == null) return;
            var discovery = def.MilestoneDiscoveryPhrase;
            if (string.IsNullOrEmpty(discovery))
                discovery = string.IsNullOrEmpty(def.DisplayName) ? def.Id : def.DisplayName;
            if (string.IsNullOrEmpty(discovery)) return;
            _milestoneFirstDone.Add(skillId);
            var place = a.Side == ApeSide.Left ? _settlementNameLeft : _settlementNameRight;
            if (string.IsNullOrEmpty(place)) place = a.Side == ApeSide.Left ? "西岸" : "东岸";
            var msg = $"【{place}】聚落的【{Label(a)}】发现【{discovery}】";
            LogEvent(WorldEventKind.MilestoneFirstDiscovery, msg);
        }

        private static MapData RebuildMap(int[] tiles, float[] f)
        {
            var m = MapData.Size;
            if (tiles.Length != m * m || f.Length != m * m) throw new InvalidOperationException("地图尺寸与存档不符。");
            var t = new TileType[m, m];
            var food = new float[m, m];
            for (var y = 0; y < m; y++)
            for (var x = 0; x < m; x++)
            {
                var i = y * m + x;
                t[x, y] = (TileType)tiles[i];
                food[x, y] = f[i];
            }
            return new MapData(t, food);
        }

        private static List<ApeCell> RebuildApes(ApeSaveRecord[] rec)
        {
            var list = new List<ApeCell>(rec.Length);
            for (var i = 0; i < rec.Length; i++) list.Add(ApeCell.FromSave(rec[i]));
            return list;
        }

        private static List<PreyEntity> RebuildPreyList(PreySaveV1[] a, SimParams p, out int nextId)
        {
            nextId = 0;
            var list = new List<PreyEntity>();
            if (a == null) return list;
            var max = 0;
            for (var i = 0; i < a.Length; i++)
            {
                var s = a[i];
                if (s == null) continue;
                var meat = s.meatHunger > 0.0001f ? s.meatHunger : p.PreyMeatHunger;
                var rd = s.respawnDelayTicks > 0 ? s.respawnDelayTicks : System.Math.Max(0, p.PreyRespawnDelayTicks);
                var sid = !string.IsNullOrEmpty(s.speciesId) ? s.speciesId : WildlifeIds.CoreGrassPrey;
                list.Add(new PreyEntity
                {
                    Id = s.Id, X = s.X, Y = s.Y, Alive = s.Alive, RespawnAtTick = s.RespawnAtTick,
                    SpeciesId = sid, MeatHunger = meat, RespawnDelayTicks = rd
                });
                if (s.Id > max) max = s.Id;
            }
            nextId = max + 1;
            return list;
        }

        private static List<PredatorEntity> RebuildPredList(PredatorSaveV1[] a, SimParams p, out int nextId)
        {
            nextId = 0;
            var list = new List<PredatorEntity>();
            if (a == null) return list;
            var max = 0;
            for (var i = 0; i < a.Length; i++)
            {
                var s = a[i];
                if (s == null) continue;
                var sm = s.spookMaxStress > 0.0001f ? s.spookMaxStress : p.PredatorSpookMaxStress;
                var sr = s.spookRadius > 0 ? s.spookRadius : p.PredatorSpookRadius;
                var sid = !string.IsNullOrEmpty(s.speciesId) ? s.speciesId : WildlifeIds.CoreStalker;
                list.Add(new PredatorEntity
                {
                    Id = s.Id, X = s.X, Y = s.Y,
                    SpeciesId = sid, SpookMaxStress = sm, SpookRadius = System.Math.Max(0, sr)
                });
                if (s.Id > max) max = s.Id;
            }
            nextId = max + 1;
            return list;
        }

        public SimParams Parameters => _p;
        /// <summary>本局合并后的文化技艺（含 Mod）；UI 与事件文案可依赖显示名。</summary>
        public CultureRuntimeResult CultureDefinitions => _culture;
        public int TickCount => _tickCount;
        public MapData Map => _map;
        public int ApeCount => _apes.Count;
        public float WaterLeft => _waterLeft;
        public float WaterRight => _waterRight;
        public bool DroughtActive => _droughtActive;
        public bool RainUsed => _rainUsed;

        /// <summary>Consumes events emitted this tick (or via <see cref="ApplyRain"/>) for UI; does not include chronicle history.</summary>
        public IReadOnlyList<WorldEventRecord> DrainPendingEvents()
        {
            if (_eventQueue.Count == 0) return Array.Empty<WorldEventRecord>();
            var c = _eventQueue.ToArray();
            _eventQueue.Clear();
            return c;
        }

        /// <summary>Last entries for persistence / 读档重播（cap 由 <see cref="SimParams.ChronicleMaxEntries"/> 决定）。</summary>
        public IReadOnlyList<WorldEventRecord> GetChronicleSnapshot()
        {
            if (_chronicle == null || _chronicle.Count == 0) return Array.Empty<WorldEventRecord>();
            return _chronicle;
        }

        private int ChronicleCap()
        {
            var c = _p.ChronicleMaxEntries;
            if (c <= 0) c = 64;
            if (c < 8) c = 8;
            if (c > 256) c = 256;
            return c;
        }

        private void LogEvent(WorldEventKind kind, string message)
        {
            var rec = new WorldEventRecord { Tick = _tickCount, Kind = kind, Message = message };
            _eventQueue.Add(rec);
            var cap = ChronicleCap();
            if (_chronicle == null) _chronicle = new List<WorldEventRecord>(cap);
            _chronicle.Add(rec);
            while (_chronicle.Count > cap) _chronicle.RemoveAt(0);
        }

        private void HydrateNarrationFlags()
        {
            if (_p.EastShoreNarrativeTick <= 0) _eastHintLogged = true;
            else if (_tickCount >= _p.EastShoreNarrativeTick) _eastHintLogged = true;
            if (_droughtActive && System.Math.Min(_waterLeft, _waterRight) < 0.4f) _droughtSevereLogged = true;
        }

        private void LoadChronicleFromSave(WorldSaveV1 data)
        {
            _chronicle = new List<WorldEventRecord>(ChronicleCap());
            if (data.Chronicle == null) return;
            for (var i = 0; i < data.Chronicle.Length; i++)
            {
                var s = data.Chronicle[i];
                if (s == null) continue;
                _chronicle.Add(new WorldEventRecord
                {
                    Tick = s.Tick,
                    Kind = ToKindClamped(s.Kind),
                    Message = s.Message ?? string.Empty
                });
            }
            var cap = ChronicleCap();
            while (_chronicle.Count > cap) _chronicle.RemoveAt(0);
        }

        private static WorldEventKind ToKindClamped(int raw)
        {
            if (Enum.IsDefined(typeof(WorldEventKind), raw)) return (WorldEventKind)raw;
            return WorldEventKind.System;
        }

        public IReadOnlyList<int> PullNewApeViewIds()
        {
            if (_newViewApeIds.Count == 0) return Array.Empty<int>();
            var a = _newViewApeIds.ToArray();
            _newViewApeIds.Clear();
            return a;
        }

        public IReadOnlyList<ApeState> GetApeStates()
        {
            var list = new ApeState[_apes.Count];
            for (var i = 0; i < _apes.Count; i++) list[i] = _apes[i].ToState();
            return list;
        }

        public ApeState? FindApe(int id)
        {
            for (var i = 0; i < _apes.Count; i++)
            {
                if (_apes[i].Id == id) return _apes[i].ToState();
            }
            return null;
        }

        public PreyView[] GetAlivePreyForDisplay()
        {
            if (_prey == null || _prey.Count == 0) return System.Array.Empty<PreyView>();
            var n = 0;
            for (var i = 0; i < _prey.Count; i++)
                if (_prey[i].Alive) n++;
            if (n == 0) return System.Array.Empty<PreyView>();
            var a = new PreyView[n];
            n = 0;
            for (var i = 0; i < _prey.Count; i++)
            {
                var p = _prey[i];
                if (!p.Alive) continue;
                a[n++] = new PreyView(p.Id, p.X, p.Y, p.SpeciesId);
            }
            return a;
        }

        public PredatorView[] GetPredatorsForDisplay()
        {
            if (_predators == null || _predators.Count == 0) return System.Array.Empty<PredatorView>();
            var a = new PredatorView[_predators.Count];
            for (var i = 0; i < _predators.Count; i++)
            {
                var d = _predators[i];
                a[i] = new PredatorView(d.Id, d.X, d.Y, d.SpeciesId);
            }
            return a;
        }

        public void SetApeNickname(int id, string name)
        {
            for (var i = 0; i < _apes.Count; i++)
            {
                if (_apes[i].Id == id) _apes[i].Nickname = name ?? string.Empty;
            }
        }

        public bool CanShowRain => !_rainUsed && _droughtActive && System.Math.Min(_waterLeft, _waterRight) < _p.DroughtButtonThreshold;

        public void ApplyRain()
        {
            if (!CanShowRain) return;
            _rainUsed = true;
            _waterLeft = _p.RainLeftWater;
            LogEvent(WorldEventKind.Rain,
                $"你唤来降雨：西岸（上游）水源得到补充；东岸未同享此雨，蒸发仍在拉低下游水位 (tick {_tickCount})。");
        }

        public void Step()
        {
            _tickCount++;
            PhasePreyRevive();
            PhaseEnvironment();
            if (_tickCount % _p.FoodRegenIntervalTicks == 0) RegenFood();
            PhaseReproduction();
            PhaseVitals();
            PhaseEpisodicMemory();
            PhaseIntentAndMovement();
            PhasePreyWander();
            PhasePredator();
            PhaseCulture();
            PhaseMating();
            PhaseSocial();
            PhaseApeConflict();
            PhaseStress();
        }

        private void PhaseEnvironment()
        {
            if (_tickCount == _p.DroughtStartTick)
            {
                _droughtActive = true;
                if (!_droughtLogged) { _droughtLogged = true; LogEvent(WorldEventKind.DroughtStart, $"河流水位开始持续下降 (tick {_tickCount})。"); }
            }
            if (_droughtActive)
            {
                if (!_rainUsed) { _waterLeft = System.Math.Max(0f, _waterLeft - _p.DroughtPerTick); _waterRight = System.Math.Max(0f, _waterRight - _p.DroughtPerTick); }
                else _waterRight = System.Math.Max(0f, _waterRight - _p.DroughtPerTick);
                var wMin = System.Math.Min(_waterLeft, _waterRight);
                if (!_droughtSevereLogged && wMin < 0.4f)
                {
                    _droughtSevereLogged = true;
                    LogEvent(WorldEventKind.DroughtSevere, "旱情加重：两岸果树随水位恢复食物变慢；东岸在降雨前更依赖残存果量。");
                }
            }
            if (_p.EastShoreNarrativeTick > 0 && !_eastHintLogged && _tickCount == _p.EastShoreNarrativeTick)
            {
                _eastHintLogged = true;
                LogEvent(WorldEventKind.EastShore, "东岸果林方向传来猿声——河另一侧还有另一小群同族在活动。");
            }
        }

        private void PhaseReproduction() => TickPregnanciesAndBirths();

        private void PhaseVitals()
        {
            for (var i = 0; i < _apes.Count; i++)
            {
                if (!_apes[i].Alive) continue;
                var a = _apes[i];
                if (_p.SatedHealthRegenPerTick > 0f && a.Hunger > _p.SatedHealthRegenHunger && a.Health < 1f)
                {
                    a.Health = System.Math.Min(1f, a.Health + _p.SatedHealthRegenPerTick);
                }
                AdvanceAgeAndElderDeath(a);
            }
        }

        /// <summary>果记衰减 + 同族「印象」衰减/失效。</summary>
        private void PhaseEpisodicMemory()
        {
            if (_p.FoodMemoryDistanceBias > 0f)
            {
                for (var i = 0; i < _apes.Count; i++)
                {
                    var a = _apes[i];
                    if (!a.Alive) continue;
                    if (a.FoodMemStrength <= 0f) continue;
                    var bad = a.FoodMemX < 0 || a.FoodMemY < 0 || !MapData.InBounds(a.FoodMemX, a.FoodMemY) ||
                              _map.Tiles[a.FoodMemX, a.FoodMemY] != TileType.FruitTree || _map.Food[a.FoodMemX, a.FoodMemY] < 0.01f;
                    if (bad) a.FoodMemStrength = System.Math.Max(0f, a.FoodMemStrength - 0.28f);
                    if (_p.FoodMemoryDecayPerTick > 0f) a.FoodMemStrength -= _p.FoodMemoryDecayPerTick;
                    if (a.FoodMemStrength < 0.02f) { a.FoodMemX = -1; a.FoodMemY = -1; a.FoodMemStrength = 0f; }
                }
            }
            for (var i = 0; i < _apes.Count; i++)
            {
                var a = _apes[i];
                if (!a.Alive) continue;
                if (a.PeerId >= 0)
                {
                    var o = FindApeCellById(a.PeerId);
                    if (o == null || !o.Alive) a.PeerMemStrength = 0f;
                }
                if (a.PeerMemStrength > 0f && _p.PeerMemoryDecayPerTick > 0f) a.PeerMemStrength -= _p.PeerMemoryDecayPerTick;
                if (a.PeerMemStrength < 0.02f) { a.PeerId = -1; a.PeerMemStrength = 0f; }
            }
        }

        private void PhaseIntentAndMovement()
        {
            for (var i = 0; i < _apes.Count; i++) ApeHungerAndAct(_apes[i]);
        }

        private void PhaseMating() => TryMating();

        private void PhaseSocial() => TrySocialApproach();

        private void TickPregnanciesAndBirths()
        {
            for (var i = 0; i < _apes.Count; i++)
            {
                var a = _apes[i];
                if (!a.Alive || a.IsMale) continue;
                if (a.PregnancyCountdown <= 0) continue;
                a.PregnancyCountdown--;
                if (a.PregnancyCountdown != 0) continue;
                var sire = FindApeCellById(a.SireId);
                a.SireId = -1;
                if (sire == null) continue;
                BirthChild(a, sire);
            }
        }

        private ApeCell FindApeCellById(int id)
        {
            if (id < 0) return null;
            for (var j = 0; j < _apes.Count; j++)
                if (_apes[j].Id == id) return _apes[j];
            return null;
        }

        private void BirthChild(ApeCell mother, ApeCell sire)
        {
            if (CountAlive() >= _p.MaxApeCount) return;
            if (!FindBirthSpot(mother.X, mother.Y, out var bx, out var by)) return;
            var id = _nextId++;
            var male = _rng.Next(0, 2) == 0;
            var c = Mix(mother.Courage, sire.Courage);
            var u = Mix(mother.Curiosity, sire.Curiosity);
            var body = 0.85f + (float)_rng.NextDouble() * 0.25f;
            var s0 = 0.1f + (float)_rng.NextDouble() * 0.1f;
            var gname = NarrationNamePools.PickCallName(_rng);
            _apes.Add(new ApeCell(id, bx, by, mother.Side, male, 0f, c, u, body, mother.Id, sire.Id, s0, gname));
            _newViewApeIds.Add(id);
            LogEvent(WorldEventKind.Birth, $"{Label(mother)} 的孩子出生了 (id {id}, tick {_tickCount})");
        }

        private float Mix(float a, float b)
        {
            var v = (a + b) * 0.5f + (float)(_rng.NextDouble() * 0.3 - 0.15);
            if (v < -1f) return -1f;
            if (v > 1f) return 1f;
            return v;
        }

        private bool FindBirthSpot(int ox, int oy, out int bx, out int by)
        {
            var cands = new List<(int x, int y)>(8);
            for (var dy = -1; dy <= 1; dy++)
            for (var dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue;
                var x = ox + dx;
                var y = oy + dy;
                if (!MapData.InBounds(x, y) || !_map.IsWalkable(x, y)) continue;
                if (ApeAt(x, y)) continue;
                cands.Add((x, y));
            }
            for (var dy = -2; dy <= 2 && cands.Count == 0; dy++)
            for (var dx = -2; dx <= 2; dx++)
            {
                if (System.Math.Abs(dx) + System.Math.Abs(dy) > 2) continue;
                var x = ox + dx;
                var y = oy + dy;
                if (!MapData.InBounds(x, y) || !_map.IsWalkable(x, y)) continue;
                if (ApeAt(x, y)) continue;
                cands.Add((x, y));
            }
            if (cands.Count == 0)
            {
                bx = 0;
                by = 0;
                return false;
            }
            var pick = cands[_rng.Next(0, cands.Count)];
            bx = pick.x;
            by = pick.y;
            return true;
        }

        private int CountAlive()
        {
            var n = 0;
            for (var i = 0; i < _apes.Count; i++)
                if (_apes[i].Alive) n++;
            return n;
        }

        private void TryMating()
        {
            if (CountAlive() >= _p.MaxApeCount) return;
            for (var i = 0; i < _apes.Count; i++)
            {
                var f = _apes[i];
                if (!f.Alive || f.IsMale) continue;
                if (f.PregnancyCountdown > 0) continue;
                if (!LifeStageUtil.CanBreed(f.Stage)) continue;
                if (f.Hunger < _p.MatingMinHunger) continue;
                var m = FindAdjacentBreedableMale(f);
                if (m == null) continue;
                if (m.Hunger < _p.MatingMinHunger) continue;
                if (!LifeStageUtil.CanBreed(m.Stage)) continue;
                var effMating = _p.MatingRoll;
                if (_p.MatingStressPenalty > 0f)
                    effMating *= 1.0 - _p.MatingStressPenalty * f.Stress;
                if (effMating < 0.00005) effMating = 0.00005;
                if (_rng.NextDouble() >= effMating) continue;
                f.PregnancyCountdown = _p.PregnancyDurationTicks;
                f.SireId = m.Id;
            }
        }

        private ApeCell FindAdjacentBreedableMale(ApeCell f)
        {
            var dirs = new[] { (0, 1), (0, -1), (1, 0), (-1, 0) };
            foreach (var d in dirs)
            {
                var x = f.X + d.Item1;
                var y = f.Y + d.Item2;
                if (!MapData.InBounds(x, y)) continue;
                for (var j = 0; j < _apes.Count; j++)
                {
                    var o = _apes[j];
                    if (!o.Alive || !o.IsMale) continue;
                    if (o.X != x || o.Y != y) continue;
                    return o;
                }
            }
            return null;
        }

        private static bool CanApeBrawl(LifeStage s) =>
            s == LifeStage.Youth || s == LifeStage.Adult || s == LifeStage.Elder;

        /// <summary>同岸、曼哈顿 1 格、少/成/年长者偶发扭打；勇气与随机分胜负。0=关。</summary>
        private void PhaseApeConflict()
        {
            if (_p.ConflictEventChance <= 0 || _apes.Count < 2) return;
            var n = 0;
            for (var i = 0; i < _apes.Count; i++)
            {
                var a = _apes[i];
                if (!a.Alive || !CanApeBrawl(a.Stage)) continue;
                for (var j = i + 1; j < _apes.Count; j++)
                {
                    var b = _apes[j];
                    if (!b.Alive || !CanApeBrawl(b.Stage)) continue;
                    if (a.Side != b.Side) continue;
                    if (System.Math.Abs(a.X - b.X) + System.Math.Abs(a.Y - b.Y) != 1) continue;
                    n++;
                }
            }
            if (n == 0) return;
            if (_rng.NextDouble() >= _p.ConflictEventChance) return;
            var pick = _rng.Next(0, n);
            ApeCell p0 = null, p1 = null;
            var c = 0;
            for (var i = 0; i < _apes.Count; i++)
            {
                var a = _apes[i];
                if (!a.Alive || !CanApeBrawl(a.Stage)) continue;
                for (var j = i + 1; j < _apes.Count; j++)
                {
                    var b = _apes[j];
                    if (!b.Alive || !CanApeBrawl(b.Stage)) continue;
                    if (a.Side != b.Side) continue;
                    if (System.Math.Abs(a.X - b.X) + System.Math.Abs(a.Y - b.Y) != 1) continue;
                    if (c == pick) { p0 = a; p1 = b; goto pairResolved; }
                    c++;
                }
            }
        pairResolved:
            if (p0 == null || p1 == null) return;
            var aw = (p0.Courage + 1f) * 0.5f + (float)_rng.NextDouble() * 0.2f;
            var bw = (p1.Courage + 1f) * 0.5f + (float)_rng.NextDouble() * 0.2f;
            var winner = aw >= bw ? p0 : p1;
            var loser = aw >= bw ? p1 : p0;
            var a0 = p0;
            var b0 = p1;
            var wBase = _p.ConflictWinnerHealth;
            if (wBase < 0f) wBase = 0f;
            var lExtra = _p.ConflictLoserExtraHealth;
            if (lExtra < 0f) lExtra = 0f;
            winner.Health -= wBase;
            loser.Health -= wBase + lExtra;
            var cs = _p.ConflictStress;
            if (cs > 0f)
            {
                a0.Stress = System.Math.Min(1f, a0.Stress + cs);
                b0.Stress = System.Math.Min(1f, b0.Stress + cs);
            }
            if (winner.Health < 0f) winner.Health = 0f;
            if (loser.Health < 0f) loser.Health = 0f;
            LogEvent(WorldEventKind.ApeConflict,
                $"{Label(winner)} 与 {Label(loser)} 扭打，{Label(loser)} 更吃亏 (tick {_tickCount})");
            if (loser.Health <= 0.001f) { KillApeInConflict(loser, winner); return; }
            if (winner.Health <= 0.001f) KillApeInConflict(winner, loser);
        }

        private void KillApeInConflict(ApeCell victim, ApeCell other)
        {
            var hadC = victim.SnapshotCultures();
            var id = victim.Id;
            victim.Alive = false;
            victim.FoodMemX = -1;
            victim.FoodMemY = -1;
            victim.FoodMemStrength = 0f;
            victim.PeerId = -1;
            victim.PeerMemStrength = 0f;
            ApplyKinLossStress(id);
            MaybeSkillExtinctIfLast(hadC);
            LogEvent(WorldEventKind.ApeKilledInConflict, $"{Label(victim)} 在冲突中伤重不治；对手在旁 (tick {_tickCount})，涉及 {Label(other)}");
        }

        private void TrySocialApproach()
        {
            for (var i = 0; i < _apes.Count; i++)
            {
                var a = _apes[i];
                if (!a.Alive) continue;
                if (a.Stage != LifeStage.Adult) continue;
                if (a.Hunger < _p.SocialMinHunger) continue;
                var pSocial = (double)_p.SocialAdultStepChance;
                if (a.Stress > 0.0001f)
                    pSocial *= 1.0 - _p.SocialStressInhibit * a.Stress;
                if (_rng.NextDouble() >= pSocial) continue;
                ApeCell target = null;
                var bestScore = float.MaxValue;
                for (var j = 0; j < _apes.Count; j++)
                {
                    var o = _apes[j];
                    if (!o.Alive || o == a) continue;
                    if (o.Side != a.Side) continue;
                    var d = System.Math.Abs(a.X - o.X) + System.Math.Abs(a.Y - o.Y);
                    if (d <= 0 || d > 6) continue;
                    var score = (float)d;
                    if (_p.PeerSocialPreferBias > 0f && a.PeerMemStrength > 0.1f && o.Id == a.PeerId)
                        score -= _p.PeerSocialPreferBias * a.PeerMemStrength;
                    if (score < bestScore) { bestScore = score; target = o; }
                }
                if (target != null)
                {
                    StepToward(a, target.X, target.Y);
                    if (_p.PeerMemoryReinforce > 0f)
                    {
                        a.PeerId = target.Id;
                        a.PeerMemStrength = System.Math.Min(1f, a.PeerMemStrength + _p.PeerMemoryReinforce);
                    }
                }
            }
        }

        private void PhaseStress()
        {
            for (var i = 0; i < _apes.Count; i++)
            {
                var a = _apes[i];
                if (!a.Alive) continue;
                var rise = 0f;
                if (_droughtActive)
                {
                    var w = a.X < MapData.RiverX ? _waterLeft : _waterRight;
                    if (w < 0.999f) rise += _p.StressDroughtScale * (1f - w);
                }
                if (a.Hunger < 0.5f) rise += _p.StressHungerScale * (0.5f - a.Hunger);
                if (_predators != null && _predators.Count > 0)
                {
                    var d = MinManhattanToAnyPredator(a.X, a.Y, out var pi);
                    if (pi >= 0)
                    {
                        var pr = _predators[pi];
                        if (pr.SpookMaxStress > 0.001f && pr.SpookRadius > 0 && d <= pr.SpookRadius)
                        {
                            var te = 1f - d / (float)(pr.SpookRadius + 1);
                            if (te < 0f) te = 0f;
                            if (te > 1f) te = 1f;
                            rise += pr.SpookMaxStress * te;
                        }
                    }
                }
                var relax = a.Hunger > 0.65f ? _p.StressRelaxPerTick : _p.StressRelaxPerTick * 0.45f;
                a.Stress = a.Stress + rise - relax;
                if (a.Stress < 0f) a.Stress = 0f;
                if (a.Stress > 1f) a.Stress = 1f;
            }
        }

        private void AdvanceAgeAndElderDeath(ApeCell ape)
        {
            ape.Age += _p.AgePerTick;
            ape.Stage = LifeStageUtil.FromAge(ape.Age);
            if (ape.Stage == LifeStage.Elder && ape.Age >= _p.NaturalDeathAtAge && _rng.NextDouble() < _p.ElderDeathChance) NaturalDeath(ape);
        }

        private void NaturalDeath(ApeCell ape)
        {
            var id = ape.Id;
            var hadC = ape.SnapshotCultures();
            ape.Alive = false;
            ape.FoodMemX = -1;
            ape.FoodMemY = -1;
            ape.FoodMemStrength = 0f;
            ape.PeerId = -1;
            ape.PeerMemStrength = 0f;
            ApplyKinLossStress(id);
            MaybeSkillExtinctIfLast(hadC);
            LogEvent(WorldEventKind.NaturalDeath, $"{Label(ape)} 衰老离世 (tick {_tickCount})");
        }

        private static string Label(ApeCell a)
        {
            if (!string.IsNullOrEmpty(a.Nickname)) return a.Nickname;
            if (!string.IsNullOrEmpty(a.GivenName)) return a.GivenName;
            return $"ID{a.Id}";
        }

        private void RegenFood()
        {
            for (var y = 0; y < MapData.Size; y++)
            for (var x = 0; x < MapData.Size; x++)
            {
                if (_map.Tiles[x, y] != TileType.FruitTree) continue;
                var w = x < MapData.RiverX ? _waterLeft : _waterRight;
                if (w <= 0f) continue;
                _map.Food[x, y] = System.Math.Min(_p.MaxFoodPerCell, _map.Food[x, y] + _p.FoodRegenWaterMultiplier * w);
            }
        }

        private void ApeHungerAndAct(ApeCell ape)
        {
            if (!ape.Alive) return;
            var decay = HungerDecay(ape);
            ape.Hunger -= decay;
            if (ape.Hunger < 0f) ape.Hunger = 0f;
            if (ape.Hunger <= 0f) { Starve(ape); return; }
            if (TryConsumePrey(ape)) return;
            var t = _map.Tiles[ape.X, ape.Y];
            if (t == TileType.FruitTree && _map.Food[ape.X, ape.Y] >= _p.MinFruitToEat)
            {
                var eat = _p.EatHunger;
                AddEatBonusesFromCulture(ape, ref eat);
                ape.Hunger = System.Math.Min(1f, ape.Hunger + eat);
                _map.Food[ape.X, ape.Y] -= _p.MinFruitToEat;
                if (_p.FoodMemoryDistanceBias > 0f)
                {
                    ape.FoodMemX = ape.X;
                    ape.FoodMemY = ape.Y;
                    ape.FoodMemStrength = ComputeFoodMemStrength(ape);
                }
                if (_map.Food[ape.X, ape.Y] < 0f) _map.Food[ape.X, ape.Y] = 0f;
                if (_map.Food[ape.X, ape.Y] <= 0.01f) LogEvent(WorldEventKind.FoodDepleted, $"区域({ape.X},{ape.Y})的果树食物耗尽");
                return;
            }
            if (ape.Hunger < _p.SeekHungerThreshold)
            {
                if (TryFindTargetTree(ape, out var tx, out var ty) && (ape.X != tx || ape.Y != ty)) { StepToward(ape, tx, ty); return; }
            }
            WanderingStep(ape);
            if (ape.Alive) MaybeTendToParent(ape);
        }

        private float HungerDecay(ApeCell ape) =>
            ape.Stage switch
            {
                LifeStage.Infant => _p.InfantHungerDecay,
                LifeStage.Child => _p.ChildHungerDecay,
                _ => _p.HungerPerTick
            };

        private void Starve(ApeCell ape)
        {
            var id = ape.Id;
            var hadC = ape.SnapshotCultures();
            ape.Alive = false;
            ape.FoodMemX = -1;
            ape.FoodMemY = -1;
            ape.FoodMemStrength = 0f;
            ape.PeerId = -1;
            ape.PeerMemStrength = 0f;
            ApplyKinLossStress(id);
            MaybeSkillExtinctIfLast(hadC);
            LogEvent(WorldEventKind.Starvation, $"{Label(ape)} 因饥饿死亡 (tick {_tickCount})");
        }

        /// <summary>亲代方死亡时，仍将其记在 ParentA/ParentB 的在世血亲压力上跳；亲亡后 <see cref="MaybeTendToParent"/> 自会因 FindApeCell 失败而停。</summary>
        private void ApplyKinLossStress(int deceasedId)
        {
            if (deceasedId < 0 || _p.KinLossStress <= 0f) return;
            for (var i = 0; i < _apes.Count; i++)
            {
                var a = _apes[i];
                if (!a.Alive) continue;
                if (a.ParentA != deceasedId && a.ParentB != deceasedId) continue;
                a.Stress += _p.KinLossStress;
                if (a.Stress > 1f) a.Stress = 1f;
            }
        }

        private bool TryFindTargetTree(ApeCell ape, out int tx, out int ty)
        {
            var best = float.MaxValue;
            tx = -1;
            ty = -1;
            for (var y = 0; y < MapData.Size; y++)
            for (var x = 0; x < MapData.Size; x++)
            {
                if (_map.Tiles[x, y] != TileType.FruitTree || _map.Food[x, y] < 0.01f) continue;
                var man = (float)System.Math.Abs(ape.X - x) + System.Math.Abs(ape.Y - y);
                var score = man;
                if (ape.FoodMemStrength > 0.05f && _p.FoodMemoryDistanceBias > 0f && x == ape.FoodMemX && y == ape.FoodMemY)
                    score -= _p.FoodMemoryDistanceBias * ape.FoodMemStrength;
                if (score < best) { best = score; tx = x; ty = y; }
            }
            return tx >= 0;
        }

        private void StepToward(ApeCell ape, int targetX, int targetY)
        {
            var dx = targetX - ape.X;
            var dy = targetY - ape.Y;
            if (dx == 0 && dy == 0) return;
            if (System.Math.Abs(dx) >= System.Math.Abs(dy))
            {
                var nx = ape.X + (dx > 0 ? 1 : -1);
                if (TryMoveTo(ape, nx, ape.Y)) return;
                var ny = ape.Y + (dy > 0 ? 1 : -1);
                TryMoveTo(ape, ape.X, ny);
            }
            else
            {
                var ny = ape.Y + (dy > 0 ? 1 : -1);
                if (TryMoveTo(ape, ape.X, ny)) return;
                var nx = ape.X + (dx > 0 ? 1 : -1);
                TryMoveTo(ape, nx, ape.Y);
            }
        }

        private bool TryMoveTo(ApeCell ape, int x, int y)
        {
            if (!_map.IsWalkable(x, y)) return false;
            if (ApeAt(x, y)) return false;
            ape.X = x;
            ape.Y = y;
            return true;
        }

        private bool ApeAt(int x, int y)
        {
            for (var i = 0; i < _apes.Count; i++)
            {
                if (_apes[i].Alive && _apes[i].X == x && _apes[i].Y == y) return true;
            }
            return false;
        }

        private void WanderingStep(ApeCell ape)
        {
            var courage01 = (ape.Courage + 1f) * 0.5f;
            if (courage01 < 0f) courage01 = 0f;
            if (courage01 > 1f) courage01 = 1f;
            var freezeMul = 1f - _p.CourageWanderResist * courage01;
            if (freezeMul < 0.12f) freezeMul = 0.12f;
            if (ape.Stress > 0.45f && _p.StressWanderFreeze > 0f &&
                _rng.NextDouble() < ape.Stress * _p.StressWanderFreeze * freezeMul) return;
            var cqW = (ape.Curiosity + 1f) * 0.5f;
            if (cqW < 0f) cqW = 0f;
            if (cqW > 1f) cqW = 1f;
            var pStill = 0.2f;
            if (_p.CuriosityWanderLively > 0f) pStill *= 1f - _p.CuriosityWanderLively * cqW;
            if (_rng.NextDouble() < pStill) return;
            if (_p.PeerMemoryWanderBias > 0f && ape.PeerMemStrength > 0.08f && ape.PeerId >= 0)
            {
                var peer = FindApeCellById(ape.PeerId);
                if (peer != null && peer.Alive && _rng.NextDouble() < ape.PeerMemStrength * _p.PeerMemoryWanderBias)
                {
                    if (TryWanderNudgeTo(ape, peer.X, peer.Y)) return;
                }
            }
            if (_p.FoodMemoryWanderBias > 0f && _p.FoodMemoryDistanceBias > 0f && ape.FoodMemStrength > 0.1f &&
                MapData.InBounds(ape.FoodMemX, ape.FoodMemY) &&
                _map.Tiles[ape.FoodMemX, ape.FoodMemY] == TileType.FruitTree && _map.Food[ape.FoodMemX, ape.FoodMemY] >= 0.01f &&
                _rng.NextDouble() < ape.FoodMemStrength * _p.FoodMemoryWanderBias)
            {
                if (TryWanderNudgeTo(ape, ape.FoodMemX, ape.FoodMemY)) return;
            }
            for (var k = 0; k < 4; k++)
            {
                var d = _rng.Next(0, 4);
                var (dx, dy) = d == 0 ? (1, 0) : d == 1 ? (-1, 0) : d == 2 ? (0, 1) : (0, -1);
                if (TryMoveTo(ape, ape.X + dx, ape.Y + dy)) return;
            }
        }

        /// <summary>婴/幼/少：小概率在游荡后再朝在世亲代挪一步（观察学习雏形，不影响觅食主支）。</summary>
        private void MaybeTendToParent(ApeCell ape)
        {
            if (_p.ParentImitateBaseChance <= 0f) return;
            if (ape.Stage != LifeStage.Infant && ape.Stage != LifeStage.Child && ape.Stage != LifeStage.Youth) return;
            var pid = ape.ParentA >= 0 ? ape.ParentA : ape.ParentB;
            if (pid < 0) return;
            var par = FindApeCellById(pid);
            if (par == null || !par.Alive) return;
            var d = System.Math.Abs(ape.X - par.X) + System.Math.Abs(ape.Y - par.Y);
            if (d < 2 || d > 12) return;
            var cq = (ape.Curiosity + 1f) * 0.5f;
            if (cq < 0f) cq = 0f;
            if (cq > 1f) cq = 1f;
            var sm = ape.Stage == LifeStage.Infant
                ? _p.ParentImitateInfantMult
                : ape.Stage == LifeStage.Child
                    ? _p.ParentImitateChildMult
                    : _p.ParentImitateYouthMult;
            var pr = _p.ParentImitateBaseChance * (0.35f + 0.65f * cq) * sm;
            if (_rng.NextDouble() >= pr) return;
            StepToward(ape, par.X, par.Y);
        }

        /// <summary>游荡专用：在能缩短格距的邻格中选一格走一步（不用于觅食主逻辑）。</summary>
        private bool TryWanderNudgeTo(ApeCell ape, int tx, int ty)
        {
            var d0 = System.Math.Abs(ape.X - tx) + System.Math.Abs(ape.Y - ty);
            if (d0 <= 0) return false;
            var bestD1 = 9999;
            var bestDx = 0;
            var bestDy = 0;
            for (var di = 0; di < 4; di++)
            {
                var (dx, dy) = di == 0 ? (1, 0) : di == 1 ? (-1, 0) : di == 2 ? (0, 1) : (0, -1);
                var nx = ape.X + dx;
                var ny = ape.Y + dy;
                if (!MapData.InBounds(nx, ny) || !_map.IsWalkable(nx, ny)) continue;
                if (ApeAt(nx, ny)) continue;
                var d1 = System.Math.Abs(nx - tx) + System.Math.Abs(ny - ty);
                if (d1 < d0 && d1 < bestD1) { bestD1 = d1; bestDx = dx; bestDy = dy; }
            }
            if (bestD1 < 9999) return TryMoveTo(ape, ape.X + bestDx, ape.Y + bestDy);
            return false;
        }

        private static MapData CreateMapAndFood(SimRng rng, SimParams p, out int[] leftCells, out int[] rightCells)
        {
            var tiles = new TileType[MapData.Size, MapData.Size];
            var food = new float[MapData.Size, MapData.Size];
            for (var y = 0; y < MapData.Size; y++)
            for (var x = 0; x < MapData.Size; x++) tiles[x, y] = TileType.Grass;
            for (var y = 0; y < MapData.Size; y++) tiles[MapData.RiverX, y] = TileType.River;
            var nLeft = 8 + rng.Next(0, 3);
            var nRight = 8 + rng.Next(0, 3);
            PlaceFruits(rng, tiles, food, 0, MapData.RiverX - 1, nLeft, p.MaxFoodPerCell);
            PlaceFruits(rng, tiles, food, MapData.RiverX + 1, MapData.Size - 1, nRight, p.MaxFoodPerCell);
            leftCells = CollectWalkable(0, MapData.RiverX - 1, tiles);
            rightCells = CollectWalkable(MapData.RiverX + 1, MapData.Size - 1, tiles);
            return new MapData(tiles, food);
        }

        private static void PlaceFruits(SimRng rng, TileType[,] tiles, float[,] food, int x0, int x1, int count, float maxFoodPerCell)
        {
            var pool = new List<(int x, int y)>();
            for (var y = 0; y < MapData.Size; y++)
            for (var x = x0; x <= x1; x++) pool.Add((x, y));
            for (var i = 0; i < count && pool.Count > 0; i++)
            {
                var j = rng.Next(0, pool.Count);
                var (x, y) = pool[j];
                pool[j] = pool[pool.Count - 1];
                pool.RemoveAt(pool.Count - 1);
                tiles[x, y] = TileType.FruitTree;
                food[x, y] = maxFoodPerCell;
            }
        }

        private static int[] CollectWalkable(int x0, int x1, TileType[,] tiles)
        {
            var list = new List<int>();
            for (var y = 0; y < MapData.Size; y++)
            for (var x = x0; x <= x1; x++)
            {
                if (tiles[x, y] != TileType.River) list.Add(y * MapData.Size + x);
            }
            return list.ToArray();
        }

        private static int PlaceInitialApes(SimRng rng, List<ApeCell> apes, int[] leftCells, int[] rightCells, int nextId)
        {
            nextId = PlaceSide(rng, apes, leftCells, ApeSide.Left, 5, nextId);
            nextId = PlaceSide(rng, apes, rightCells, ApeSide.Right, 5, nextId);
            return nextId;
        }

        private static int PlaceSide(SimRng rng, List<ApeCell> apes, int[] raw, ApeSide side, int count, int nextId)
        {
            var shuf = (int[])raw.Clone();
            for (var i = shuf.Length - 1; i > 0; i--)
            {
                var j = rng.Next(0, i + 1);
                var t = shuf[i];
                shuf[i] = shuf[j];
                shuf[j] = t;
            }
            for (var k = 0; k < count && k < shuf.Length; k++)
            {
                var c = shuf[k];
                var x = c % MapData.Size;
                var y = c / MapData.Size;
                var age = 12f + (float)rng.NextDouble() * 10f;
                var cr = (float)(rng.NextDouble() * 2 - 1);
                var cur = (float)(rng.NextDouble() * 2 - 1);
                var sc = 0.88f + (float)rng.NextDouble() * 0.2f;
                var s0 = 0.1f + (float)rng.NextDouble() * 0.1f;
                var gname = NarrationNamePools.PickCallName(rng);
                apes.Add(new ApeCell(nextId, x, y, side, k % 2 == 0, age, cr, cur, sc, -1, -1, s0, gname));
                nextId++;
            }
            return nextId;
        }

        private void GrantAllInitialMentors()
        {
            var list = _culture.SkillsInDependencyOrder;
            if (list == null) return;
            for (var si = 0; si < list.Count; si++) GrantInitialMentorsFor(list[si]);
        }

        private void GrantInitialMentorsFor(CultureSkillDef def)
        {
            if (def == null || def.InitialMentorCount <= 0) return;
            var n = def.InitialMentorCount;
            var pool = new List<ApeCell>(12);
            for (var i = 0; i < _apes.Count; i++)
            {
                var a = _apes[i];
                if (!a.Alive) continue;
                if (!LifeStageUtil.CanMentorCulture(a.Stage)) continue;
                pool.Add(a);
            }
            if (pool.Count == 0) return;
            for (var i = pool.Count - 1; i > 0; i--)
            {
                var j = _rng.Next(0, i + 1);
                var t = pool[i];
                pool[i] = pool[j];
                pool[j] = t;
            }
            var c = 0;
            for (var i = 0; i < pool.Count && c < n; i++, c++) EnsurePrereqChainAndGrant(pool[i], def.Id);
        }

        private void EnsurePrereqChainAndGrant(ApeCell a, string skillId)
        {
            if (a == null || string.IsNullOrEmpty(skillId)) return;
            if (a.HasCulture(skillId)) return;
            if (!_culture.ById.TryGetValue(skillId, out var def)) return;
            if (def.Requires != null)
            {
                for (var i = 0; i < def.Requires.Length; i++)
                {
                    var r = def.Requires[i];
                    if (string.IsNullOrEmpty(r)) continue;
                    EnsurePrereqChainAndGrant(a, r);
                }
            }
            a.AddCulture(skillId);
        }

        private void AddEatBonusesFromCulture(ApeCell ape, ref float eat)
        {
            var list = _culture.SkillsInDependencyOrder;
            if (list == null) return;
            for (var i = 0; i < list.Count; i++)
            {
                var d = list[i];
                if (d == null || d.EatHungerBonus <= 0f) continue;
                if (ape.HasCulture(d.Id)) eat += d.EatHungerBonus;
            }
        }

        private float ComputeFoodMemStrength(ApeCell ape)
        {
            var mem = 1f;
            var list = _culture.SkillsInDependencyOrder;
            if (list == null) return mem;
            for (var i = 0; i < list.Count; i++)
            {
                var d = list[i];
                if (d == null || d.FoodMemBoost <= 0f) continue;
                if (ape.HasCulture(d.Id)) mem = System.Math.Max(mem, System.Math.Min(1f, 1f + d.FoodMemBoost));
            }
            return mem;
        }

        private void FillDefaultWildlife()
        {
            if (_wildMaterialized == null) return;
            for (var gi = 0; gi < _wildMaterialized.PreyGroups.Count; gi++)
            {
                var g = _wildMaterialized.PreyGroups[gi];
                for (var n = 0; n < g.Count; n++)
                {
                    if (!TrySpawnOnePreyInGroup(g)) break;
                }
            }
            for (var gi = 0; gi < _wildMaterialized.PredatorGroups.Count; gi++)
            {
                var g = _wildMaterialized.PredatorGroups[gi];
                for (var n = 0; n < g.Count; n++)
                {
                    if (!TrySpawnOnePredInGroup(g)) break;
                }
            }
        }

        private bool TrySpawnOnePreyInGroup(PreyGroup g)
        {
            for (var t = 0; t < 300; t++)
            {
                var x = _rng.Next(0, MapData.Size);
                var y = _rng.Next(0, MapData.Size);
                if (!CanPlaceWildlifeAt(x, y, -1)) continue;
                _prey.Add(new PreyEntity
                {
                    Id = _nextPreyId++,
                    X = x, Y = y, Alive = true, RespawnAtTick = 0,
                    SpeciesId = g.SpeciesId ?? WildlifeIds.CoreGrassPrey,
                    MeatHunger = g.MeatHunger, RespawnDelayTicks = g.RespawnDelayTicks
                });
                return true;
            }
            return false;
        }

        private bool TrySpawnOnePredInGroup(PredatorGroup g)
        {
            for (var t = 0; t < 300; t++)
            {
                var x = _rng.Next(0, MapData.Size);
                var y = _rng.Next(0, MapData.Size);
                if (!CanPlaceWildlifeAt(x, y, -1)) continue;
                _predators.Add(new PredatorEntity
                {
                    Id = _nextPredatorId++, X = x, Y = y,
                    SpeciesId = g.SpeciesId ?? WildlifeIds.CoreStalker,
                    SpookMaxStress = g.SpookMaxStress, SpookRadius = g.SpookRadius
                });
                return true;
            }
            return false;
        }

        private bool CanPlaceWildlifeAt(int x, int y, int ignoreLivePreyId)
        {
            if (!MapData.InBounds(x, y) || !_map.IsWalkable(x, y)) return false;
            if (_map.Tiles[x, y] != TileType.Grass) return false;
            if (ApeAt(x, y)) return false;
            if (PredatorAt(x, y, -1)) return false;
            for (var i = 0; i < _prey.Count; i++)
            {
                var p = _prey[i];
                if (!p.Alive) continue;
                if (ignoreLivePreyId >= 0 && p.Id == ignoreLivePreyId) continue;
                if (p.X == x && p.Y == y) return false;
            }
            return true;
        }

        private bool PredatorAt(int x, int y, int exceptId)
        {
            for (var i = 0; i < _predators.Count; i++)
            {
                var d = _predators[i];
                if (exceptId >= 0 && d.Id == exceptId) continue;
                if (d.X == x && d.Y == y) return true;
            }
            return false;
        }

        private void PhasePreyRevive()
        {
            for (var i = 0; i < _prey.Count; i++)
            {
                var p = _prey[i];
                if (p.Alive) continue;
                if (_tickCount < p.RespawnAtTick) continue;
                for (var t = 0; t < 200; t++)
                {
                    var x = _rng.Next(0, MapData.Size);
                    var y = _rng.Next(0, MapData.Size);
                    if (!CanPlaceWildlifeAt(x, y, -1)) continue;
                    p.X = x; p.Y = y; p.Alive = true; p.RespawnAtTick = 0;
                    break;
                }
            }
        }

        private void PhasePreyWander()
        {
            if (_prey == null || _prey.Count == 0) return;
            for (var i = 0; i < _prey.Count; i++)
            {
                var p = _prey[i];
                if (!p.Alive) continue;
                if (_rng.NextDouble() < 0.4) continue;
                var k = _rng.Next(0, 4);
                var (dx, dy) = k == 0 ? (1, 0) : k == 1 ? (-1, 0) : k == 2 ? (0, 1) : (0, -1);
                TryMovePreyTo(p, p.X + dx, p.Y + dy);
            }
        }

        private bool TryMovePreyTo(PreyEntity p, int x, int y)
        {
            if (!MapData.InBounds(x, y) || !_map.IsWalkable(x, y)) return false;
            if (ApeAt(x, y)) return false;
            if (PredatorAt(x, y, -1)) return false;
            for (var j = 0; j < _prey.Count; j++)
            {
                var o = _prey[j];
                if (o == p || !o.Alive) continue;
                if (o.X == x && o.Y == y) return false;
            }
            p.X = x; p.Y = y;
            return true;
        }

        private void PhasePredator()
        {
            if (_predators == null || _predators.Count == 0) return;
            for (var i = 0; i < _predators.Count; i++)
            {
                var p = _predators[i];
                ApeCell victim = null;
                var best = 9999;
                for (var j = 0; j < _apes.Count; j++)
                {
                    var a = _apes[j];
                    if (!a.Alive) continue;
                    var d = System.Math.Abs(p.X - a.X) + System.Math.Abs(p.Y - a.Y);
                    if (d < best) { best = d; victim = a; }
                }
                if (victim == null) continue;
                if (p.X == victim.X && p.Y == victim.Y)
                {
                    KillApeByPredation(victim);
                    continue;
                }
                StepPredatorToward(p, victim.X, victim.Y);
                for (var j = 0; j < _apes.Count; j++)
                {
                    var a = _apes[j];
                    if (!a.Alive) continue;
                    if (a.X == p.X && a.Y == p.Y) { KillApeByPredation(a); break; }
                }
            }
        }

        private void StepPredatorToward(PredatorEntity p, int tx, int ty)
        {
            var dx = tx - p.X;
            var dy = ty - p.Y;
            if (dx == 0 && dy == 0) return;
            if (System.Math.Abs(dx) >= System.Math.Abs(dy))
            {
                var nx = p.X + (dx > 0 ? 1 : -1);
                if (TryMovePredatorTo(p, nx, p.Y)) return;
                var ny = p.Y + (dy > 0 ? 1 : -1);
                TryMovePredatorTo(p, p.X, ny);
            }
            else
            {
                var ny = p.Y + (dy > 0 ? 1 : -1);
                if (TryMovePredatorTo(p, p.X, ny)) return;
                var nx = p.X + (dx > 0 ? 1 : -1);
                TryMovePredatorTo(p, nx, p.Y);
            }
        }

        private bool TryMovePredatorTo(PredatorEntity p, int x, int y)
        {
            if (!MapData.InBounds(x, y) || !_map.IsWalkable(x, y)) return false;
            if (PredatorAt(x, y, p.Id)) return false;
            p.X = x; p.Y = y;
            return true;
        }

        private void KillApeByPredation(ApeCell ape)
        {
            if (!ape.Alive) return;
            var id = ape.Id;
            var hadC = ape.SnapshotCultures();
            ape.Alive = false;
            ape.FoodMemX = -1;
            ape.FoodMemY = -1;
            ape.FoodMemStrength = 0f;
            ape.PeerId = -1;
            ape.PeerMemStrength = 0f;
            ApplyKinLossStress(id);
            MaybeSkillExtinctIfLast(hadC);
            LogEvent(WorldEventKind.Predation, $"{Label(ape)} 被掠食者扑杀 (tick {_tickCount})");
        }

        private int CountAliveWithSkill(string skillId)
        {
            if (string.IsNullOrEmpty(skillId)) return 0;
            var n = 0;
            for (var i = 0; i < _apes.Count; i++)
            {
                var a = _apes[i];
                if (!a.Alive) continue;
                if (a.HasCulture(skillId)) n++;
            }
            return n;
        }

        private void MaybeSkillExtinctIfLast(IReadOnlyList<string> hadSkills)
        {
            if (hadSkills == null || hadSkills.Count == 0) return;
            for (var hi = 0; hi < hadSkills.Count; hi++)
            {
                var sid = hadSkills[hi];
                if (string.IsNullOrEmpty(sid)) continue;
                if (CountAliveWithSkill(sid) > 0) continue;
                if (!_culture.ById.TryGetValue(sid, out var def) || def == null) continue;
                var label = "「" + (string.IsNullOrEmpty(def.DisplayName) ? def.Id : def.DisplayName) + "」";
                LogEvent(WorldEventKind.SkillExtinct, $"{label}无人再通—文化断裂 (tick {_tickCount})。");
            }
        }

        private bool TryConsumePrey(ApeCell ape)
        {
            for (var i = 0; i < _prey.Count; i++)
            {
                var pr = _prey[i];
                if (!pr.Alive) continue;
                if (pr.X != ape.X || pr.Y != ape.Y) continue;
                var add = pr.MeatHunger > 0.0001f ? pr.MeatHunger : _p.PreyMeatHunger;
                if (add <= 0.0001f) continue;
                pr.Alive = false;
                pr.RespawnAtTick = _tickCount + System.Math.Max(0, pr.RespawnDelayTicks);
                ape.Hunger = System.Math.Min(1f, ape.Hunger + add);
                LogEvent(WorldEventKind.PreyHunted, $"{Label(ape)} 取食了走兽 (tick {_tickCount})");
                return true;
            }
            return false;
        }

        private void PhaseCulture()
        {
            var list = _culture.SkillsInDependencyOrder;
            if (list == null) return;
            for (var si = 0; si < list.Count; si++)
            {
                var d = list[si];
                if (d == null) continue;
                if (d.ObserveLearn > 0f) MaybeObserveLearn(d);
                if (d.InventPerTick > 0) MaybeInvent(d);
            }
        }

        private static bool MeetsPrereqs(ApeCell a, CultureSkillDef def)
        {
            if (def == null) return false;
            if (def.Requires == null) return true;
            for (var i = 0; i < def.Requires.Length; i++)
            {
                var r = def.Requires[i];
                if (string.IsNullOrEmpty(r)) continue;
                if (!a.HasCulture(r)) return false;
            }
            return true;
        }

        private void MaybeObserveLearn(CultureSkillDef def)
        {
            for (var i = 0; i < _apes.Count; i++)
            {
                var a = _apes[i];
                if (!a.Alive) continue;
                if (a.HasCulture(def.Id)) continue;
                if (!LifeStageUtil.CanLearnCulture(a.Stage)) continue;
                if (!MeetsPrereqs(a, def)) continue;
                for (var j = 0; j < _apes.Count; j++)
                {
                    var t = _apes[j];
                    if (!t.Alive) continue;
                    if (!t.HasCulture(def.Id)) continue;
                    if (!LifeStageUtil.CanMentorCulture(t.Stage)) continue;
                    if (t.Side != a.Side) continue;
                    var dist = System.Math.Abs(a.X - t.X) + System.Math.Abs(a.Y - t.Y);
                    if (dist > 2) continue;
                    var cq = (a.Curiosity + 1f) * 0.5f;
                    if (cq < 0f) cq = 0f;
                    if (cq > 1f) cq = 1f;
                    var pr = def.ObserveLearn * (0.25f + 0.75f * cq);
                    if (_rng.NextDouble() >= pr) continue;
                    var nBefore = CountAliveWithSkill(def.Id);
                    a.AddCulture(def.Id);
                    if (nBefore == 0) TryRecordMilestoneFirstDiscovery(a, def.Id);
                    var dn = string.IsNullOrEmpty(def.DisplayName) ? def.Id : def.DisplayName;
                    LogEvent(WorldEventKind.SkillLearned, $"{Label(a)} 观摩学会了{dn} (tick {_tickCount})");
                    break;
                }
            }
        }

        private void MaybeInvent(CultureSkillDef def)
        {
            for (var i = 0; i < _apes.Count; i++)
            {
                var a = _apes[i];
                if (!a.Alive) continue;
                if (a.HasCulture(def.Id)) continue;
                if (a.Stage != LifeStage.Adult) continue;
                if (!MeetsPrereqs(a, def)) continue;
                if (!InventContextSatisfied(a, def)) continue;
                if (_rng.NextDouble() >= def.InventPerTick) continue;
                var nBefore = CountAliveWithSkill(def.Id);
                a.AddCulture(def.Id);
                if (nBefore == 0) TryRecordMilestoneFirstDiscovery(a, def.Id);
                var dn = string.IsNullOrEmpty(def.DisplayName) ? def.Id : def.DisplayName;
                LogEvent(WorldEventKind.SkillLearned, $"{Label(a)} 独自琢磨出了{dn} (tick {_tickCount})");
            }
        }

        private bool InventContextSatisfied(ApeCell a, CultureSkillDef def)
        {
            if (def == null) return false;
            switch (def.InventContext)
            {
                case CultureInventContext.NearFruitTree:
                    return IsNearFoliageFruitForInvent(a);
                case CultureInventContext.None:
                default:
                    return true;
            }
        }

        private bool IsNearFoliageFruitForInvent(ApeCell a)
        {
            if (_map.Tiles[a.X, a.Y] == TileType.FruitTree) return _map.Food[a.X, a.Y] > 0.01f;
            var dirs = new[] { (0, 1), (0, -1), (1, 0), (-1, 0) };
            foreach (var d in dirs)
            {
                var x = a.X + d.Item1;
                var y = a.Y + d.Item2;
                if (!MapData.InBounds(x, y)) continue;
                if (_map.Tiles[x, y] == TileType.FruitTree && _map.Food[x, y] > 0.01f) return true;
            }
            return false;
        }

        private int MinManhattanToAnyPredator(int x, int y, out int bestIndex)
        {
            bestIndex = -1;
            if (_predators == null || _predators.Count == 0) return 99;
            var best = 99;
            for (var i = 0; i < _predators.Count; i++)
            {
                var p = _predators[i];
                var d = System.Math.Abs(p.X - x) + System.Math.Abs(p.Y - y);
                if (d < best) { best = d; bestIndex = i; }
            }
            return best;
        }

        private sealed class ApeCell
        {
            public int Id;
            public float Hunger = 1f;
            public float Health = 1f;
            public float Age;
            public bool Alive = true;
            public string Nickname = string.Empty;
            public string GivenName = string.Empty;
            public int X, Y;
            public ApeSide Side;
            public bool IsMale;
            public float Courage, Curiosity;
            public float BodyScale;
            public LifeStage Stage;
            public int ParentA = -1, ParentB = -1;
            public int PregnancyCountdown;
            public int SireId = -1;
            public float Stress = 0.14f;
            public int FoodMemX = -1;
            public int FoodMemY = -1;
            public float FoodMemStrength;
            public int PeerId = -1;
            public float PeerMemStrength;
            private List<string> _cultureIds;

            public bool HasCulture(string skillId)
            {
                if (string.IsNullOrEmpty(skillId) || _cultureIds == null) return false;
                for (var i = 0; i < _cultureIds.Count; i++)
                {
                    if (string.Equals(_cultureIds[i], skillId, StringComparison.OrdinalIgnoreCase)) return true;
                }
                return false;
            }

            public void AddCulture(string skillId)
            {
                if (string.IsNullOrEmpty(skillId)) return;
                if (HasCulture(skillId)) return;
                if (_cultureIds == null) _cultureIds = new List<string>(4);
                _cultureIds.Add(skillId);
            }

            public List<string> SnapshotCultures()
            {
                if (_cultureIds == null || _cultureIds.Count == 0) return new List<string>();
                return new List<string>(_cultureIds);
            }

            private string[] SortedCultureIdsForSave()
            {
                if (_cultureIds == null || _cultureIds.Count == 0) return System.Array.Empty<string>();
                var a = new string[_cultureIds.Count];
                for (var i = 0; i < _cultureIds.Count; i++) a[i] = _cultureIds[i];
                System.Array.Sort(a, StringComparer.OrdinalIgnoreCase);
                return a;
            }

            public ApeCell(int id, int x, int y, ApeSide side, bool male, float age, float courage, float curiosity, float body, int p0, int p1, float initialStress = 0.14f, string givenName = "")
            {
                Id = id; X = x; Y = y; Side = side; IsMale = male;
                Age = age; Courage = courage; Curiosity = curiosity; BodyScale = body;
                ParentA = p0; ParentB = p1;
                Stage = LifeStageUtil.FromAge(age);
                Stress = initialStress;
                GivenName = givenName ?? string.Empty;
            }

            public ApeSaveRecord ToSave() => new ApeSaveRecord
            {
                Id = Id,
                Hunger = Hunger,
                Health = Health,
                Age = Age,
                Alive = Alive,
                Nickname = Nickname ?? string.Empty,
                givenName = GivenName ?? string.Empty,
                X = X,
                Y = Y,
                Side = (int)Side,
                IsMale = IsMale,
                Courage = Courage,
                Curiosity = Curiosity,
                BodyScale = BodyScale,
                Stage = (int)Stage,
                ParentA = ParentA,
                ParentB = ParentB,
                PregnancyCountdown = PregnancyCountdown,
                SireId = SireId,
                Stress = Stress,
                FoodMemX = FoodMemX,
                FoodMemY = FoodMemY,
                FoodMemStrength = FoodMemStrength,
                PeerId = PeerId,
                PeerMemStrength = PeerMemStrength,
                cultureSkillIds = SortedCultureIdsForSave(),
                CultureFlags = 0
            };

            public static ApeCell FromSave(ApeSaveRecord r)
            {
                var a = new ApeCell
                {
                    Id = r.Id,
                    Hunger = r.Hunger,
                    Health = r.Health,
                    Age = r.Age,
                    Alive = r.Alive,
                    Nickname = r.Nickname ?? string.Empty,
                    X = r.X,
                    Y = r.Y,
                    Side = (ApeSide)r.Side,
                    IsMale = r.IsMale,
                    Courage = r.Courage,
                    Curiosity = r.Curiosity,
                    BodyScale = r.BodyScale,
                    Stage = (LifeStage)r.Stage,
                    ParentA = r.ParentA,
                    ParentB = r.ParentB,
                    PregnancyCountdown = r.PregnancyCountdown,
                    SireId = r.SireId,
                    Stress = r.Stress,
                    FoodMemX = r.FoodMemX,
                    FoodMemY = r.FoodMemY,
                    FoodMemStrength = r.FoodMemStrength,
                    PeerId = r.PeerId,
                    PeerMemStrength = r.PeerMemStrength
                };
                a.GivenName = r.givenName ?? string.Empty;
                if (string.IsNullOrEmpty(a.GivenName))
                    a.GivenName = NarrationNamePools.PickCallNameForLegacyId(r.Id, r.Side);
                if (r.cultureSkillIds != null && r.cultureSkillIds.Length > 0)
                {
                    for (var i = 0; i < r.cultureSkillIds.Length; i++)
                    {
                        var s = r.cultureSkillIds[i];
                        if (!string.IsNullOrEmpty(s)) a.AddCulture(s.Trim());
                    }
                }
                else
                {
                    if ((r.CultureFlags & 1) != 0) a.AddCulture(LandKingCultureIds.NutCrack);
                    if ((r.CultureFlags & 2) != 0)
                    {
                        a.AddCulture(LandKingCultureIds.NutCrack);
                        a.AddCulture(LandKingCultureIds.FruitScout);
                    }
                }
                return a;
            }

            private ApeCell() { }

            public ApeState ToState() => new ApeState
            {
                Id = Id,
                Hunger = Hunger,
                Health = Health,
                Age = Age,
                Stress = Stress,
                FoodMemoryStrength = FoodMemStrength,
                PeerImpressionId = PeerId,
                PeerImpressionStrength = PeerMemStrength,
                Alive = Alive,
                Nickname = Nickname,
                GivenName = GivenName,
                GridX = X,
                GridY = Y,
                Side = Side,
                IsMale = IsMale,
                Courage = Courage,
                Curiosity = Curiosity,
                Stage = Stage,
                ParentId0 = ParentA,
                ParentId1 = ParentB,
                BodyScale = BodyScale,
                CultureSkillIds = SortedCultureIdsForSave()
            };
        }
    }
}
