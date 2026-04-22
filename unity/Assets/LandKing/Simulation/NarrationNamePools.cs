namespace LandKing.Simulation
{
    /// <summary>叙事用地名/称呼的随机池（少量占位；后续可换 L1 或外部表）。</summary>
    public static class NarrationNamePools
    {
        private static readonly string[] Settlements =
        {
            "河湾地", "溪谷", "林缘", "丘北", "泽南"
        };

        private static readonly string[] CallNames =
        {
            "阿木", "岩", "叶", "桑", "茅", "荆", "禾", "溪"
        };

        public static string PickSettlement(SimRng rng)
        {
            if (rng == null) return Settlements[0];
            return Settlements[rng.Next(0, Settlements.Length)];
        }

        public static string PickCallName(SimRng rng)
        {
            if (rng == null) return CallNames[0];
            return CallNames[rng.Next(0, CallNames.Length)];
        }

        /// <summary>旧档无称呼时，用种子稳定生成一条，避免读档后全体变 ID。</summary>
        public static string PickCallNameForLegacyId(int id, int sideInt)
        {
            var s = (id * 397) ^ (sideInt * 911) ^ 0x2B7E_1510;
            var r = new SimRng(s == 0 ? 1 : s);
            return PickCallName(r);
        }
    }
}
