using System;
using System.Collections.Generic;

namespace LandKing.Simulation
{
    /// <summary>
    /// Headless world step for milestone 2. No Unity types.
    /// </summary>
    public sealed class WorldSimulation
    {
        public const float HungerPerTick = 0.02f;
        public const float EatHunger = 0.3f;
        public const float SeekHungerThreshold = 0.7f;
        public const float MaxFoodPerCell = 5f;
        public const int DroughtStartTick = 100;
        public const float DroughtPerTick = 0.002f;
        public const float RainLeftWater = 0.8f;
        public const float DroughtButtonThreshold = 0.2f;
        public const int FoodRegenIntervalTicks = 10;

        private readonly System.Random _rng;
        private readonly MapData _map;
        private readonly ApeCell[] _apes;
        private readonly List<string> _logQueue = new List<string>(8);

        private int _tickCount;
        private float _waterLeft = 1f;
        private float _waterRight = 1f;
        private bool _droughtActive;
        private bool _rainUsed;
        private bool _droughtLogged;

        public WorldSimulation(int randomSeed = 42)
        {
            _rng = new System.Random(randomSeed);
            _map = CreateMapAndFood(_rng, out int[] leftCells, out int[] rightCells);
            _apes = new ApeCell[10];
            PlaceApes(_rng, _apes, _map, leftCells, rightCells);
        }

        public int TickCount => _tickCount;
        public MapData Map => _map;
        public float WaterLeft => _waterLeft;
        public float WaterRight => _waterRight;
        public bool DroughtActive => _droughtActive;
        public bool RainUsed => _rainUsed;
        public IReadOnlyList<string> StealLogQueue()
        {
            if (_logQueue.Count == 0) return Array.Empty<string>();
            var c = _logQueue.ToArray();
            _logQueue.Clear();
            return c;
        }

        public IReadOnlyList<ApeState> GetApeStates()
        {
            var list = new ApeState[_apes.Length];
            for (var i = 0; i < _apes.Length; i++) list[i] = _apes[i].ToState();
            return list;
        }

        public ApeState? FindApe(int id)
        {
            for (var i = 0; i < _apes.Length; i++)
            {
                if (_apes[i].Id == id) return _apes[i].ToState();
            }
            return null;
        }

        public void SetApeNickname(int id, string name)
        {
            for (var i = 0; i < _apes.Length; i++)
            {
                if (_apes[i].Id == id) _apes[i].Nickname = name ?? string.Empty;
            }
        }

        public bool CanShowRain => !_rainUsed && _droughtActive && System.Math.Min(_waterLeft, _waterRight) < DroughtButtonThreshold;

        public void ApplyRain()
        {
            if (!CanShowRain) return;
            _rainUsed = true;
            _waterLeft = RainLeftWater;
            _logQueue.Add($"降雨发生在河流西侧 (tick {_tickCount})");
        }

        public void Step()
        {
            _tickCount++;
            if (_tickCount == DroughtStartTick)
            {
                _droughtActive = true;
                if (!_droughtLogged)
                {
                    _droughtLogged = true;
                    _logQueue.Add($"水位开始下降 (tick {_tickCount})");
                }
            }

            if (_droughtActive)
            {
                if (!_rainUsed)
                {
                    _waterLeft = System.Math.Max(0f, _waterLeft - DroughtPerTick);
                    _waterRight = System.Math.Max(0f, _waterRight - DroughtPerTick);
                }
                else
                {
                    _waterRight = System.Math.Max(0f, _waterRight - DroughtPerTick);
                }
            }

            if (_tickCount % FoodRegenIntervalTicks == 0) RegenFood();
            for (var i = 0; i < _apes.Length; i++) ApeHungerAndAct(_apes[i]);
        }

        private void RegenFood()
        {
            for (var y = 0; y < MapData.Size; y++)
            for (var x = 0; x < MapData.Size; x++)
            {
                if (_map.Tiles[x, y] != TileType.FruitTree) continue;
                var w = x < MapData.RiverX ? _waterLeft : _waterRight;
                if (w <= 0f) continue;
                _map.Food[x, y] = System.Math.Min(MaxFoodPerCell, _map.Food[x, y] + 0.5f * w);
            }
        }

        private void ApeHungerAndAct(ApeCell ape)
        {
            if (!ape.Alive) return;
            ape.Age += 0.01f;
            ape.Hunger -= HungerPerTick;
            if (ape.Hunger < 0f) ape.Hunger = 0f;
            if (ape.Hunger <= 0f)
            {
                Kill(ape);
                return;
            }

            var t = _map.Tiles[ape.X, ape.Y];
            if (t == TileType.FruitTree && _map.Food[ape.X, ape.Y] >= 0.3f)
            {
                ape.Hunger = System.Math.Min(1f, ape.Hunger + EatHunger);
                _map.Food[ape.X, ape.Y] -= 0.3f;
                if (_map.Food[ape.X, ape.Y] < 0f) _map.Food[ape.X, ape.Y] = 0f;
                if (_map.Food[ape.X, ape.Y] <= 0.01f) _logQueue.Add($"区域({ape.X},{ape.Y})的果树食物耗尽");
                return;
            }

            if (ape.Hunger < SeekHungerThreshold)
            {
                if (TryFindTargetTree(ape, out var tx, out var ty) && (ape.X != tx || ape.Y != ty))
                {
                    StepToward(ape, tx, ty);
                    return;
                }
            }
            WanderingStep(ape);
        }

        private void Kill(ApeCell ape)
        {
            ape.Alive = false;
            var label = !string.IsNullOrEmpty(ape.Nickname) ? ape.Nickname : $"ID{ape.Id}";
            _logQueue.Add($"{label} 因饥饿死亡 (tick {_tickCount})");
        }

        private bool TryFindTargetTree(ApeCell ape, out int tx, out int ty)
        {
            var bestD = int.MaxValue;
            tx = -1;
            ty = -1;
            for (var y = 0; y < MapData.Size; y++)
            for (var x = 0; x < MapData.Size; x++)
            {
                if (_map.Tiles[x, y] != TileType.FruitTree || _map.Food[x, y] < 0.01f) continue;
                var d = System.Math.Abs(ape.X - x) + System.Math.Abs(ape.Y - y);
                if (d < bestD)
                {
                    bestD = d;
                    tx = x;
                    ty = y;
                }
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
            for (var i = 0; i < _apes.Length; i++)
            {
                if (_apes[i].Alive && _apes[i].X == x && _apes[i].Y == y) return true;
            }
            return false;
        }

        private void WanderingStep(ApeCell ape)
        {
            var r = _rng.Next(0, 10);
            if (r < 2) return;
            for (var k = 0; k < 4; k++)
            {
                var d = _rng.Next(0, 4);
                var (dx, dy) = d switch
                {
                    0 => (1, 0),
                    1 => (-1, 0),
                    2 => (0, 1),
                    _ => (0, -1)
                };
                if (TryMoveTo(ape, ape.X + dx, ape.Y + dy)) return;
            }
        }

        private static MapData CreateMapAndFood(System.Random rng, out int[] leftCells, out int[] rightCells)
        {
            var tiles = new TileType[MapData.Size, MapData.Size];
            var food = new float[MapData.Size, MapData.Size];
            for (var y = 0; y < MapData.Size; y++)
            for (var x = 0; x < MapData.Size; x++) tiles[x, y] = TileType.Grass;
            for (var y = 0; y < MapData.Size; y++) tiles[MapData.RiverX, y] = TileType.River;

            var nLeft = 8 + rng.Next(0, 3);
            var nRight = 8 + rng.Next(0, 3);
            PlaceFruits(rng, tiles, food, 0, MapData.RiverX - 1, nLeft);
            PlaceFruits(rng, tiles, food, MapData.RiverX + 1, MapData.Size - 1, nRight);
            leftCells = CollectWalkable(0, MapData.RiverX - 1, tiles);
            rightCells = CollectWalkable(MapData.RiverX + 1, MapData.Size - 1, tiles);
            return new MapData(tiles, food);
        }

        private static void PlaceFruits(System.Random rng, TileType[,] tiles, float[,] food, int x0, int x1, int count)
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
                food[x, y] = MaxFoodPerCell;
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

        private static void PlaceApes(System.Random rng, ApeCell[] apes, MapData map, int[] leftCells, int[] rightCells)
        {
            var used = new HashSet<int>();
            for (var i = 0; i < 5; i++)
            {
                int c, x, y, guard = 0;
                do
                {
                    c = leftCells[rng.Next(0, leftCells.Length)];
                    (x, y) = (c % MapData.Size, c / MapData.Size);
                    if (++guard > 200) break;
                } while (!used.Add(c));
                apes[i] = new ApeCell(i, x, y, ApeSide.Left);
            }
            for (var i = 0; i < 5; i++)
            {
                int c, x, y, guard = 0;
                do
                {
                    c = rightCells[rng.Next(0, rightCells.Length)];
                    (x, y) = (c % MapData.Size, c / MapData.Size);
                    if (++guard > 200) break;
                } while (!used.Add(c));
                apes[5 + i] = new ApeCell(5 + i, x, y, ApeSide.Right);
            }
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

            public ApeCell(int id, int x, int y, ApeSide side)
            {
                Id = id;
                X = x;
                Y = y;
                Side = side;
            }

            public ApeState ToState() => new ApeState
            {
                Id = Id,
                Hunger = Hunger,
                Health = Health,
                Age = Age,
                Alive = Alive,
                Nickname = Nickname,
                GridX = X,
                GridY = Y,
                Side = Side
            };
        }
    }
}
