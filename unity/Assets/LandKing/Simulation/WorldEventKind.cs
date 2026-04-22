namespace LandKing.Simulation
{
    /// <summary>Simulation-emitted world events (chronicle / filtering / later persistence).</summary>
    public enum WorldEventKind
    {
        System = 0,
        DroughtStart = 1,
        DroughtSevere = 2,
        Rain = 3,
        Birth = 4,
        Starvation = 5,
        NaturalDeath = 6,
        FoodDepleted = 7,
        EastShore = 8
    }
}
