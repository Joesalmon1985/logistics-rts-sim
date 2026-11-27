using System.Collections.Generic;
using God.Sim.Population;

namespace God.Sim.Information
{
    /// <summary>F1.14: the organisations tab of a person's card (OWNER: C6). Its rows carry their knowledge layer.</summary>
    public sealed class OrgCard
    {
        public KnowledgeLayer Layer = KnowledgeLayer.Belief;
        /// <summary>Lines for the tab, in order (empty in F1).</summary>
        public List<string> Lines = new List<string>();
    }

    /// <summary>Fills <see cref="OrgCard"/> for the inspector (OWNER: C6). Called by <c>PopulationKnowledge.Inspect</c>; read-only. A stub in F1.</summary>
    public static partial class OrgCards
    {
        public static void Fill(GodState s, Npc n, NpcInspection card)
        {
        }
    }
}
