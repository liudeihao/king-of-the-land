namespace LandKing.Simulation
{
    /// <summary>亲代之间加性平均 + 小幅突变；值域约 0.06..0.99。</summary>
    public static class GeneticsUtil
    {
        public const float MinTrait = 0.06f;
        public const float MaxTrait = 0.99f;

        public static float HeredityTwoParents(float mother, float sire, SimRng rng, float mutationSigma = 0.1f)
        {
            if (rng == null) return ClampTrait((mother + sire) * 0.5f);
            var m = (mother + sire) * 0.5f + (float)(rng.NextDouble() * 2.0 - 1.0) * mutationSigma;
            return ClampTrait(m);
        }

        public static float RandomInitial(SimRng rng)
        {
            if (rng == null) return 0.5f;
            return ClampTrait(0.3f + (float)rng.NextDouble() * 0.42f);
        }

        public static float ClampTrait(float v)
        {
            if (v < MinTrait) return MinTrait;
            if (v > MaxTrait) return MaxTrait;
            return v;
        }
    }
}
