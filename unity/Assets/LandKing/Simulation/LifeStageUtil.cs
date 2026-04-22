namespace LandKing.Simulation
{
    public static class LifeStageUtil
    {
        /// <summary>年龄（游戏年）→ 阶段。节奏偏快，便于在原型中观察到一生。</summary>
        public static LifeStage FromAge(float ageYears)
        {
            if (ageYears < 1f) return LifeStage.Infant;
            if (ageYears < 4f) return LifeStage.Child;
            if (ageYears < 10f) return LifeStage.Youth;
            if (ageYears < 28f) return LifeStage.Adult;
            return LifeStage.Elder;
        }

        /// <summary>仅成年个体可参与文档中的“配对/繁殖”。</summary>
        public static bool CanBreed(LifeStage s) => s == LifeStage.Adult;
    }
}
