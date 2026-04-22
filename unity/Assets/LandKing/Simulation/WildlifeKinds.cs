namespace LandKing.Simulation
{
    /// <summary>表驱动种 kind（L1 <c>wildlife.json</c>）。</summary>
    public static class WildlifeKinds
    {
        public const string PreyWander = "prey_wander";
        public const string PredatorHuntApe = "predator_hunt_ape";

        public static bool IsPrey(string kind) => string.Equals(kind, PreyWander, System.StringComparison.OrdinalIgnoreCase);

        public static bool IsPredator(string kind) => string.Equals(kind, PredatorHuntApe, System.StringComparison.OrdinalIgnoreCase);
    }
}
