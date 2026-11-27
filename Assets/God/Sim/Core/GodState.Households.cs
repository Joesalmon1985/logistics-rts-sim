namespace God.Sim
{
    /// <summary>F0.12: the household module's state (M4). Null unless M4 starts it, so every older game keeps its hash. OWNER: M4.</summary>
    public sealed partial class GodState
    {
        /// <summary>Households, partnerships, care arrangements and estates (M4); null when the module is not running.</summary>
        public Population.Households.HouseholdState Households;
    }
}
