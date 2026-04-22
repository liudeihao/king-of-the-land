using System;

namespace LandKing.Simulation
{
    /// <summary>L1 数据表：<c>StreamingAssets/Mods/.../culture_skills.json</c>，按 mod 依赖序叠放合并。</summary>
    [Serializable]
    public sealed class CultureFileV1
    {
        /// <summary>为 true 时，在应用本文件前丢弃已合并表（可完全替代内建默认，慎用）。</summary>
        public bool clearBaseline;
        public CultureEntryV1[] entries;
    }

    [Serializable]
    public sealed class CultureEntryV1
    {
        public string id;
        public string displayName;
        public string[] requires;
        public float observeLearn;
        public double inventPerTick;
        public int initialMentorCount;
        public int inventContext;
        public float eatHungerBonus;
        public float foodMemBoost;
    }
}
