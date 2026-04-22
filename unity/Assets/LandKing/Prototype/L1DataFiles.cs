namespace LandKing.Prototype
{
    /// <summary>L1 数据包内约定的文件名；宿主与文档共用。</summary>
    public static class L1DataFiles
    {
        public const string ModManifest = "mod.json";
        public const string SimParams = "sim_params.json";
        public const string Wildlife = "wildlife.json";
        public const string CultureSkills = "culture_skills.json";
        /// <summary>可选：新局时合入 L1 会话的初始 JSON 文本，随 F5 写入 <see cref="LandKing.Simulation.L1ModPersistentV1"/>。</summary>
        public const string InitialPersistent = "l1_initial_persistent.json";
    }
}
