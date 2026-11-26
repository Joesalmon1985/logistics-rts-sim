using System.Collections.Generic;
using God.Sim.Population;

namespace God.Sim.Information
{
    /// <summary>The household tab of a person's card (F0.15 stub; OWNER: M4): its life stage, household, believed kin and care, as the faction's records hold them.</summary>
    public sealed class HouseholdCard
    {
        /// <summary>Lines for the tab, in order (empty until M4 fills them).</summary>
        public List<string> Lines = new List<string>();
    }

    /// <summary>Fills <see cref="HouseholdCard"/> for the inspector (F0.15 stub; OWNER: M4). Called by <c>PopulationKnowledge.Inspect</c>; read-only.</summary>
    public static class HouseholdCards
    {
        public static void Fill(GodState s, Npc n, NpcInspection card) { }
    }
}
