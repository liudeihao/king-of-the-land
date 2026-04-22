using System;
using System.Globalization;

namespace LandKing.Prototype
{
    /// <summary>与《微内核》§9 对齐的极简版本/范围：*、精确、&gt;=x.y.z 、&gt;=a &lt;b（两边界）。</summary>
    public static class ModVersionSpec
    {
        public static bool SatisfiesRange(string modVersion, string spec)
        {
            if (string.IsNullOrEmpty(spec) || spec.Trim() == "*")
                return true;
            if (string.IsNullOrEmpty(modVersion))
                return false;
            spec = spec.Trim();
            if (spec.Contains(">=") && spec.IndexOf('<') > spec.IndexOf(">="))
            {
                var ge = spec.IndexOf(">=", StringComparison.Ordinal);
                var lt = spec.IndexOf('<', ge + 2);
                if (ge >= 0 && lt > ge)
                {
                    var low = spec.Substring(ge + 2, lt - (ge + 2)).Trim();
                    var high = spec.Substring(lt + 1).Trim();
                    if (high.StartsWith("=", StringComparison.Ordinal)) high = high.Substring(1).Trim();
                    return CompareSemver(modVersion, low) >= 0 && CompareSemver(modVersion, high) < 0;
                }
            }
            if (spec.StartsWith(">=", StringComparison.Ordinal))
                return CompareSemver(modVersion, spec.Substring(2).Trim()) >= 0;
            if (spec.Length >= 1 && spec[0] == '<' && (spec.Length < 2 || spec[1] != '='))
                return CompareSemver(modVersion, spec.Substring(1).Trim()) < 0;
            return CompareSemver(modVersion, spec) == 0;
        }

        public static int CompareSemver(string a, string b)
        {
            if (!TryParseTriplet(a, out var am, out var an, out var ap)) return string.Compare(a, b, StringComparison.Ordinal);
            if (!TryParseTriplet(b, out var bm, out var bn, out var bp)) return string.Compare(a, b, StringComparison.Ordinal);
            if (am != bm) return am < bm ? -1 : 1;
            if (an != bn) return an < bn ? -1 : 1;
            if (ap != bp) return ap < bp ? -1 : 1;
            return 0;
        }

        public static bool TryParseTriplet(string v, out int m, out int n, out int p)
        {
            m = n = p = 0;
            if (string.IsNullOrEmpty(v)) return false;
            v = v.Trim();
            var parts = v.Split('.');
            m = parts.Length > 0 && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var a) ? a : 0;
            n = parts.Length > 1 && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var b) ? b : 0;
            p = parts.Length > 2 && int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var c) ? c : 0;
            return true;
        }
    }
}
