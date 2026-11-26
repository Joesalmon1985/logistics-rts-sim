using System;
using System.Collections.Generic;
using System.Linq;
using God.Sim;
using God.Sim.Comms;
using God.Sim.Learning;
using God.Sim.Policy;
using God.Sim.Population;
using God.Sim.Social;
using God.Sim.Tests;

namespace God.Runner.Tests
{
    /// <summary>
    /// A controlled social scenario for M5's mechanism tests: an empty glade (no wild food, no wood) with a few real NPCs,
    /// some driven by benchmark counterparts (<see cref="BenchSocialPolicy"/>), the learner by the default associative
    /// policy. A test-only keeper system (order 249, before the population) keeps the counterparts fed and rested and tops
    /// up stocks by <c>Goods.Produce</c> (reason Placed: no learning reward; the ledger and events stay exact). Nothing
    /// scripts the learner's choices.
    /// </summary>
    internal sealed class M5SocialLab
    {
        public readonly GodSim Sim;
        public readonly PopulationSystem Pop;
        public readonly int Glade;
        public readonly List<SimEvent> Events = new List<SimEvent>();
        private readonly Keeper _keeper = new Keeper();
        private readonly List<SimEvent> _buf = new List<SimEvent>();
        public bool KeepEvents = true;
        /// <summary>Called with each tick's events (before they are kept).</summary>
        public Action<List<SimEvent>> OnEvents;

        public M5SocialLab(ulong seed, Action<SimTuning> tune = null)
        {
            Sim = LearningFixtures.Empty(seed, t => { t.Population.CheckPerception = true; tune?.Invoke(t); });
            Pop = Sim.Systems.OfType<PopulationSystem>().First();
            Glade = LearningFixtures.Glade(Sim);
            Sim.AddSystem(249, _keeper);
        }

        public int At(int dir, int steps) => LearningFixtures.Step(Sim, Glade, dir, steps);

        /// <summary>A person at a node: its policy (null: the associative learner), its learning mode, and the stocks the keeper holds it at (0: never topped up).</summary>
        public Npc Add(int node, INpcPolicy policy, LearningMode mode = LearningMode.On, bool fed = false, params (Ware w, int n)[] stock) => AddH(node, policy, mode, fed, 200, stock);

        public Npc AddH(int node, INpcPolicy policy, LearningMode mode, bool fed, int hunger, params (Ware w, int n)[] stock)
        {
            var n = LearningFixtures.Npc(Sim, node, InstinctSet.Default, mode, hunger: hunger, fatigue: 100);
            if (policy != null)
            {
                Pop.SetPolicy(policy.Name, policy);
                n.Policy = policy.Name;
            }
            foreach (var x in stock) SocialFixtures.Give(Sim, n, x.w, x.n);
            _keeper.Kept.Add(new Kept { Npc = n, Fed = fed, Stock = stock });
            return n;
        }

        public void Step(int ticks)
        {
            for (int i = 0; i < ticks; i++)
            {
                Sim.Step();
                _buf.Clear();
                Sim.DrainInto(_buf);
                OnEvents?.Invoke(_buf);
                if (KeepEvents) Events.AddRange(_buf);
            }
        }

        public int Day => (int)SimCalendar.Of(Sim.State).Day(Sim.State.Tick);

        /// <summary>A counterpart uses up (as if building with it) every unit of this ware it receives (test-only: keeps a trader's carry free).</summary>
        public void Sink(Npc n, Ware w) { foreach (var k in _keeper.Kept) if (k.Npc == n) k.Sink = w; }

        private sealed class Kept
        {
            public Npc Npc;
            public bool Fed;
            public (Ware w, int n)[] Stock;
            public Ware Sink;
        }

        private sealed class Keeper : ISimSystem
        {
            public readonly List<Kept> Kept = new List<Kept>();

            public void Tick(GodState s, EventBuffer ev)
            {
                foreach (var k in Kept)
                {
                    var n = k.Npc;
                    if (!n.Alive) continue;
                    if (k.Fed)
                    {
                        // a benchmark counterpart has no needs of its own (test-only)
                        n.Drives[Drive.Hunger] = 0; n.Drives[Drive.Fatigue] = 0; n.Nutrition = 1000000;
                        System.Array.Copy(n.Drives, n.RewardSnap, Drive.Count);
                    }
                    if (s.Tick % 50 != 0) continue;
                    if (k.Sink != Ware.None)
                        foreach (var l in n.Carry)
                            if (!l.Empty && l.Ware == k.Sink) Goods.Consume(s, ev, Holder.Carry(n.Id), l.Id, l.Count, ConsumeReason.Built, n.Id);
                    foreach (var x in k.Stock)
                    {
                        int have = PopulationModule.CountOf(n, x.w);
                        if (have > 0) continue; // restocked when it runs out
                        int room = PopulationModule.Room(s, n, x.w);
                        if (room <= 0) continue;
                        have = System.Math.Max(0, x.n - room);
                        int day = (int)SimCalendar.Of(s).Day(s.Tick);
                        int spoil = Wares.IsFood(x.w) ? day + PopulationModule.SpoilDays(s.Tuning.Ecology, x.w) : -1;
                        Goods.Produce(s, ev, Holder.Carry(n.Id), x.w, x.n - have, spoil, 0, Owner.Npc(n.Id), ProduceReason.Placed, n.Id, 0);
                    }
                }
            }
        }

        // ---------------- reading the events ----------------

        public IEnumerable<T> Of<T>() where T : SimEvent => Events.OfType<T>();

        /// <summary>Offers made by <paramref name="speaker"/> and, for each, whether <paramref name="listener"/> accepted it (its own Accept gesture on that deal).</summary>
        public List<(GestureMade offer, bool accepted)> OffersAnswered(int speaker, int listener)
        {
            var accepts = new HashSet<int>(Of<GestureMade>().Where(g => g.Speaker == listener && g.Act == GestureAct.Accept).Select(g => g.DealId));
            var heard = new HashSet<int>(Of<MessageDecoded>().Where(d => d.Listener == listener).Select(d => d.MessageId));
            return Of<GestureMade>().Where(g => g.Speaker == speaker && g.Act == GestureAct.Offer && heard.Contains(g.MessageId))
                .Select(g => (g, accepts.Contains(g.DealId))).ToList();
        }
    }
}
