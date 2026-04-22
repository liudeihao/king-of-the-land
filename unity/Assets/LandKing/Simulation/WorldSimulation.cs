using System;
using System.Collections.Generic;

namespace LandKing.Simulation
{
    /// <summary>Headless world: 里程碑二、三、四、五起（双岸、旱/雨、东岸叙事、简单压力/社交抑制）；编年史为 <see cref="WorldEventRecord"/>。Tick 分阶段，<see cref="SimParams"/>（可与 L1 合并）。</summary>
    public sealed class WorldSimulation
    {
        private readonly SimParams _p;
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

        public WorldSimulation(int randomSeed = 42, SimParams parameters = null)
        {
            _initialSeed = randomSeed;
            _p = parameters != null ? parameters.Copy() : SimParams.Default.Copy();
            _chronicle = new List<WorldEventRecord>(ChronicleCap());
            _rng = new SimRng(randomSeed);
            _map = CreateMapAndFood(_rng, _p, out var leftCells, out var rightCells);
            _apes = new List<ApeCell>(20);
            _nextId = 0;
            _nextId = PlaceInitialApes(_rng, _apes, leftCells, rightCells, _nextId);
        }

        private WorldSimulation(SimParams p, MapData map, List<ApeCell> apes, int tickCount, int nextId, int initialSeed, SimRng rng,
            float waterLeft, float waterRight, bool droughtActive, bool rainUsed, bool droughtLogged)
        {
            _p = p;
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
        }

        public static WorldSimulation FromSave(WorldSaveV1 data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.Schema != 1) throw new InvalidOperationException($"WorldSave  schema {data.Schema} 不支持，需要 1。");
            if (data.MapTiles == null || data.MapFood == null) throw new InvalidOperationException("存档缺少地图数据。");
            if (data.Apes == null) throw new InvalidOperationException("存档缺少个体数据。");
            var p = data.Params != null ? data.Params.Copy() : SimParams.Default.Copy();
            var map = RebuildMap(data.MapTiles, data.MapFood);
            var apes = RebuildApes(data.Apes);
            var rng = new SimRng(data.RngState);
            var sim = new WorldSimulation(
                p, map, apes, data.TickCount, data.NextId, data.RandomSeed, rng,
                data.WaterLeft, data.WaterRight, data.DroughtActive, data.RainUsed, data.DroughtLogged);
            sim.LoadChronicleFromSave(data);
            sim.HydrateNarrationFlags();
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
                Apes = rec
            };
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
            return data;
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

        public SimParams Parameters => _p;
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
            PhaseEnvironment();
            if (_tickCount % _p.FoodRegenIntervalTicks == 0) RegenFood();
            PhaseReproduction();
            PhaseVitals();
            PhaseEpisodicMemory();
            PhaseIntentAndMovement();
            PhaseMating();
            PhaseSocial();
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
                AdvanceAgeAndElderDeath(_apes[i]);
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
            _apes.Add(new ApeCell(id, bx, by, mother.Side, male, 0f, c, u, body, mother.Id, sire.Id, s0));
            _newViewApeIds.Add(id);
            var ln = !string.IsNullOrEmpty(mother.Nickname) ? mother.Nickname : $"ID{mother.Id}";
            LogEvent(WorldEventKind.Birth, $"{ln} 的孩子出生了 (id {id}, tick {_tickCount})");
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
                if (_rng.NextDouble() >= _p.MatingRoll) continue;
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
                var best = 99;
                for (var j = 0; j < _apes.Count; j++)
                {
                    var o = _apes[j];
                    if (!o.Alive || o == a) continue;
                    if (o.Side != a.Side) continue;
                    var d = System.Math.Abs(a.X - o.X) + System.Math.Abs(a.Y - o.Y);
                    if (d <= 0 || d > 6) continue;
                    if (d < best) { best = d; target = o; }
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
            ape.Alive = false;
            ape.FoodMemX = -1;
            ape.FoodMemY = -1;
            ape.FoodMemStrength = 0f;
            ape.PeerId = -1;
            ape.PeerMemStrength = 0f;
            LogEvent(WorldEventKind.NaturalDeath, $"{Label(ape)} 衰老离世 (tick {_tickCount})");
        }

        private static string Label(ApeCell a) => !string.IsNullOrEmpty(a.Nickname) ? a.Nickname : $"ID{a.Id}";

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
            var t = _map.Tiles[ape.X, ape.Y];
            if (t == TileType.FruitTree && _map.Food[ape.X, ape.Y] >= _p.MinFruitToEat)
            {
                ape.Hunger = System.Math.Min(1f, ape.Hunger + _p.EatHunger);
                _map.Food[ape.X, ape.Y] -= _p.MinFruitToEat;
                if (_p.FoodMemoryDistanceBias > 0f)
                {
                    ape.FoodMemX = ape.X;
                    ape.FoodMemY = ape.Y;
                    ape.FoodMemStrength = 1f;
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
            ape.Alive = false;
            ape.FoodMemX = -1;
            ape.FoodMemY = -1;
            ape.FoodMemStrength = 0f;
            ape.PeerId = -1;
            ape.PeerMemStrength = 0f;
            LogEvent(WorldEventKind.Starvation, $"{Label(ape)} 因饥饿死亡 (tick {_tickCount})");
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
            if (ape.Stress > 0.45f && _p.StressWanderFreeze > 0f && _rng.NextDouble() < ape.Stress * _p.StressWanderFreeze) return;
            if (_rng.Next(0, 10) < 2) return;
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
                apes.Add(new ApeCell(nextId, x, y, side, k % 2 == 0, age, cr, cur, sc, -1, -1, s0));
                nextId++;
            }
            return nextId;
        }

        private sealed class ApeCell
        {
            public int Id;
            public float Hunger = 1f;
            public float Health = 1f;
            public float Age;
            public bool Alive = true;
            public string Nickname = string.Empty;
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

            public ApeCell(int id, int x, int y, ApeSide side, bool male, float age, float courage, float curiosity, float body, int p0, int p1, float initialStress = 0.14f)
            {
                Id = id; X = x; Y = y; Side = side; IsMale = male;
                Age = age; Courage = courage; Curiosity = curiosity; BodyScale = body;
                ParentA = p0; ParentB = p1;
                Stage = LifeStageUtil.FromAge(age);
                Stress = initialStress;
            }

            public ApeSaveRecord ToSave() => new ApeSaveRecord
            {
                Id = Id,
                Hunger = Hunger,
                Health = Health,
                Age = Age,
                Alive = Alive,
                Nickname = Nickname ?? string.Empty,
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
                PeerMemStrength = PeerMemStrength
            };

            public static ApeCell FromSave(ApeSaveRecord r)
            {
                return new ApeCell
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
                GridX = X,
                GridY = Y,
                Side = Side,
                IsMale = IsMale,
                Courage = Courage,
                Curiosity = Curiosity,
                Stage = Stage,
                ParentId0 = ParentA,
                ParentId1 = ParentB,
                BodyScale = BodyScale
            };
        }
    }
}
