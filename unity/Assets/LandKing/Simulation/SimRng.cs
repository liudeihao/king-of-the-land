using System;

namespace LandKing.Simulation
{
    /// <summary>可存档的 64 位状态 PRNG（SplitMix64 变体），替代 <see cref="System.Random"/> 以保证读档后续随机流可复现。</summary>
    public sealed class SimRng
    {
        private ulong _state;

        public SimRng(int seed)
        {
            var s = (ulong)(uint)seed;
            _state = s == 0 ? 0xD4B72C5E3A1F9087UL : s;
            for (var i = 0; i < 4; i++) NextU64();
        }

        public SimRng(ulong state)
        {
            _state = state == 0 ? 1UL : state;
        }

        public ulong State
        {
            get => _state;
            set => _state = value == 0 ? 1UL : value;
        }

        public int Next(int maxExclusive)
        {
            if (maxExclusive <= 0) return 0;
            return (int)(NextU64() % (ulong)(uint)maxExclusive);
        }

        public int Next(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            return minInclusive + Next(maxExclusive - minInclusive);
        }

        public double NextDouble() => (NextU64() >> 11) * (1.0 / 9007199254740992.0);

        private ulong NextU64()
        {
            _state += 0x9E3779B97F4A7C15UL;
            var z = _state;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }
}
