using God.Sim.Cognition;
using God.Sim.Learning;
using God.Sim.Population;

namespace God.Sim.Divinity
{
    /// <summary>
    /// RevealKnowledge (<see cref="MiracleKind.RevealKnowledge"/>, Tier 2, LEAD_RULINGS R4): the true node of a resource source (the
    /// target's <c>Magnitude</c> is the SourceId) is put into the target person's own belief store (layer 6) as a food or wood belief,
    /// like a vision. Never into an institution's records. The belief may later be stale like any other. Until K1 exists only
    /// knowledge kinds that already exist in minds are revealed. OWNER: D3.
    /// </summary>
    public static partial class Miracles
    {
        private static int BandOf(ResourceSource src) => src.Stock <= 0 ? ObjState.StockEmpty : src.Stock * 4 >= src.Capacity ? ObjState.StockHigh : ObjState.StockLow;

        private static BeliefKind BeliefOf(ResourceSource src) => src.Kind == SourceKind.Wood ? BeliefKind.Wood : BeliefKind.Food;

        private static Plan PlanReveal(GodState s, CastMiracle c)
        {
            var pl = PersonTarget(s, c.Target, out var n);
            if (pl != null) return pl;
            if (n.Mind == null) return Fail(DivineReasons.BadTarget);
            var src = s.Population.SourceById(c.Magnitude);
            if (src == null) return Fail(BadMagnitude);
            int i = n.Mind.Beliefs.IndexOf(BeliefOf(src), src.Node);
            if (i >= 0 && n.Mind.Beliefs.Slots[i].Stock == BandOf(src)) return Fail(NoEffect);
            return Spend(s, c, new Plan { Npc = n, Source = src, Node = src.Node, Cost = s.Tuning.Divinity.Mythic.Costs.RevealKnowledge });
        }

        private static void Reveal(GodState s, EventBuffer ev, CastMiracle c, Plan pl, int iv)
        {
            var n = pl.Npc; var src = pl.Source;
            var b = n.Mind.Beliefs;
            var kind = BeliefOf(src);
            int i = b.IndexOf(kind, src.Node);
            int before = i >= 0 ? b.Slots[i].Stock : -1;
            b.Upsert(kind, src.Node, PopulationModule.ClassOf(src.Kind), BandOf(src), 0, (int)s.Tick);
            Effect(ev, iv, MiracleThing.Person, n.Id, "belief_stock@" + src.Node, before, BandOf(src));
            Sign(s, iv, c.God, c.Kind, n.Node);
        }
    }
}
