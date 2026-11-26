using System;
using System.Collections.Generic;
using God.Sim;
using God.Sim.Learning;
using God.Sim.Population;
using God.Sim.Social;

namespace God.Sim.Tests
{
    /// <summary>
    /// M5's integration fixtures (SHARED_CONTRACTS 11): built from real NPCs, real lots and the sanctioned APIs.
    /// <see cref="SetRelation"/> is test-only (a direct write of a mind's record); everything else goes through the sim.
    /// </summary>
    public static class SocialFixtures
    {
        /// <summary>Goods made in a person's carry (<c>Goods.Produce</c>, reason Placed: no learning reward, ledger kept).</summary>
        public static int Give(GodSim sim, Npc n, Ware w, int count, EventBuffer ev = null)
        {
            var s = sim.State;
            int day = (int)SimCalendar.Of(s).Day(s.Tick);
            int spoil = Wares.IsFood(w) ? day + PopulationModule.SpoilDays(s.Tuning.Ecology, w) : -1;
            return Goods.Produce(s, ev, Holder.Carry(n.Id), w, count, spoil, w == Ware.Axe ? 20 : 0, Owner.Npc(n.Id), ProduceReason.Placed, n.Id, 0);
        }

        /// <summary>
        /// <paramref name="a"/> and <paramref name="b"/> meet <paramref name="interactions"/> times: each time a gives b one
        /// berry, by a real hand-over (a gift deal, <c>Goods.Move</c>, <see cref="GiftGiven"/>) on adjacent nodes. Returns the events.
        /// </summary>
        public static List<SimEvent> Meet(GodSim sim, Npc a, Npc b, int interactions)
        {
            var s = sim.State;
            var ev = new EventBuffer { CurrentTick = s.Tick };
            var g = s.Territory.Grid;
            if (g.Distance(a.Node, b.Node) > Reach.HandOver) b.Node = a.Node;
            for (int k = 0; k < interactions; k++)
            {
                Give(sim, a, Ware.Berries, 1, ev);
                if (!SocialSim.GiftNow(s, ev, a, b, Ware.Berries, 1)) throw new InvalidOperationException("the gift did not pass");
            }
            return new List<SimEvent>(ev.Items);
        }

        /// <summary>Test-only: sets a's record of b (made if needed).</summary>
        public static void SetRelation(GodSim sim, Npc a, Npc b, int trust, int familiarity)
        {
            var m = a.Mind.Social;
            int i = m.Find(b.Id);
            if (i < 0) i = SocialMemory.Touch(m, b.Id, (int)sim.State.Tick, SimCalendar.Of(sim.State).TicksPerDay, 0, 0, out _, out _);
            if (i < 0) throw new InvalidOperationException("no room for the relation");
            m.Rel[i].Trust = (short)trust;
            m.Rel[i].Familiarity = (short)familiarity;
        }

        /// <summary>Two NPCs side by side with complementary goods (a has aGoods, b has bGoods), learning on, default instincts.</summary>
        public static (Npc a, Npc b) TraderPair(GodSim sim, int node, (Ware w, int n)[] aGoods, (Ware w, int n)[] bGoods)
        {
            var a = LearningFixtures.Npc(sim, node, InstinctSet.Default, LearningMode.On);
            var b = LearningFixtures.Npc(sim, LearningFixtures.Step(sim, node, 0, 1), InstinctSet.Default, LearningMode.On);
            foreach (var x in aGoods) Give(sim, a, x.w, x.n);
            foreach (var x in bGoods) Give(sim, b, x.w, x.n);
            return (a, b);
        }
    }
}
