namespace God.Sim
{
    /// <summary>F0.12: the household module's state (M4). Null unless M4 starts it, so every older game keeps its hash. OWNER: M4.</summary>
    public sealed partial class GodState
    {
        /// <summary>Households, partnerships, care arrangements and estates (M4); null when the module is not running.</summary>
        public Population.Households.HouseholdState Households;
    }
}

namespace God.Sim.Population.Households
{
    /// <summary>
    /// The household module's state (SHARED_CONTRACTS 9.3): public fields, ascending-id lists, no dictionaries. F0 stub.
    /// OWNER: M4. Its hash part ("household") is appended only while <c>GodState.Households</c> is non-null.
    /// </summary>
    public sealed class HouseholdState
    {
        public int NextHouseholdId = 1, NextPartnershipId = 1, NextCareId = 1;

        public void AddTo(ref StateHasher h)
        {
            h.Add(NextHouseholdId); h.Add(NextPartnershipId); h.Add(NextCareId);
        }
    }
}
