using System;
using System.IO;

namespace LandKing.Prototype
{
    /// <summary>与 L1 同目录的 L2 脚本约定名；<see cref="ModManifest.l2Entry"/> 可覆写为相对本包根的路径。</summary>
    public static class L2DataFiles
    {
        public const string L2EntryLua = "l2_entry.lua";
        /// <summary>单包 Lua 源码最大字符数（C# 字符串 code units），防止恶意巨型脚本；约 256 KiB 量级。</summary>
        public const int MaxScriptSourceChars = 262144;

        public static bool IsPathUnderModRoot(string modRootFull, string fileFull)
        {
            if (string.IsNullOrEmpty(fileFull) || string.IsNullOrEmpty(modRootFull)) return false;
            var root = modRootFull.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (string.Equals(fileFull, root, StringComparison.OrdinalIgnoreCase)) return true;
            var prefix = root + Path.DirectorySeparatorChar;
            if (fileFull.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return true;
            if (Path.DirectorySeparatorChar != Path.AltDirectorySeparatorChar)
            {
                var alt = root + Path.AltDirectorySeparatorChar;
                if (fileFull.StartsWith(alt, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }
    }
}
