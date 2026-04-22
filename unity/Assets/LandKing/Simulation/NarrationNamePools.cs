using System.Collections.Generic;

namespace LandKing.Simulation
{
    /// <summary>叙事用地名/称呼池；默认内嵌，可由 L1 <c>narration_names.json</c> 按序合并覆盖。</summary>
    public static class NarrationNamePools
    {
        private static readonly string[] DefaultSettlements =
        {
            "河湾地", "溪谷", "林缘", "丘北", "泽南", "岗下", "塬西", "滩头", "坳口", "坪上"
        };

        private static readonly string[] DefaultCallNames =
        {
            "阿木", "岩", "叶", "桑", "茅", "荆", "禾", "溪", "柘", "樟", "檀", "榆", "枫", "橡", "石", "丘", "岗", "崖"
        };

        private static string[] _settlements = (string[])DefaultSettlements.Clone();
        private static string[] _callNames = (string[])DefaultCallNames.Clone();

        /// <summary>新一局 L1 加载前调用，避免沿用上一进程的合并结果。</summary>
        public static void ResetToDefaults()
        {
            _settlements = (string[])DefaultSettlements.Clone();
            _callNames = (string[])DefaultCallNames.Clone();
        }

        /// <summary>将各 Mod 合并后的列表写回；某类为空则保留当前（默认应已 <see cref="ResetToDefaults"/>）。</summary>
        public static void InstallMerged(IReadOnlyList<string> settlements, IReadOnlyList<string> callNames)
        {
            if (settlements != null && settlements.Count > 0)
            {
                var a = new string[settlements.Count];
                for (var i = 0; i < settlements.Count; i++) a[i] = settlements[i];
                _settlements = a;
            }
            if (callNames != null && callNames.Count > 0)
            {
                var a = new string[callNames.Count];
                for (var i = 0; i < callNames.Count; i++) a[i] = callNames[i];
                _callNames = a;
            }
        }

        public static string PickSettlement(SimRng rng)
        {
            if (_settlements == null || _settlements.Length == 0) return "西岸";
            if (rng == null) return _settlements[0];
            return _settlements[rng.Next(0, _settlements.Length)];
        }

        public static string PickCallName(SimRng rng)
        {
            if (_callNames == null || _callNames.Length == 0) return "无名";
            if (rng == null) return _callNames[0];
            return _callNames[rng.Next(0, _callNames.Length)];
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
