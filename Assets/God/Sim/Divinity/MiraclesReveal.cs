using God.Sim.Population;

namespace God.Sim.Divinity
{
    /// <summary>RevealKnowledge (Tier 2, LEAD_RULINGS R4). OWNER: D3.</summary>
    public static partial class Miracles
    {
        private static Plan PlanReveal(GodState s, CastMiracle c) => Fail(DivineReasons.ActUnavailable);
        private static void Reveal(GodState s, EventBuffer ev, CastMiracle c, Plan pl, int iv) { }
    }
}
