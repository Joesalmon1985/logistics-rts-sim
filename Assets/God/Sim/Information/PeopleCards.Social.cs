using System.Collections.Generic;
using God.Sim.Population;

namespace God.Sim.Information
{
    /// <summary>The social tab of a person's card (F0.15 stub; OWNER: M5): its relations (trust, familiarity, obligations) and recent messages, as the faction's records hold them.</summary>
    public sealed class SocialCard
    {
        /// <summary>Lines for the tab, in order (empty until M5 fills them).</summary>
        public List<string> Lines = new List<string>();
    }

    /// <summary>Fills <see cref="SocialCard"/> for the inspector (F0.15 stub; OWNER: M5). Called by <c>PopulationKnowledge.Inspect</c>; read-only.</summary>
    public static class SocialCards
    {
        public static void Fill(GodState s, Npc n, NpcInspection card) { }
    }
}
