using System;
using System.Collections.Generic;
using God.Sim.Estate;
using God.Sim.Learning;
using God.Sim.Population;
using God.Sim.Population.Genetics;
using God.Sim.Population.Households;

namespace God.Sim.Tests
{
    /// <summary>The ids of a fixture family (SHARED_CONTRACTS 11).</summary>
    public sealed class FamilyIds
    {
        public int Household, Hut, Bin;
        public int[] Adults, Children;
    }

    /// <summary>
    /// M4's integration fixtures (SHARED_CONTRACTS 11; OWNER: M4): real people (M4 genomes and phenotypes), real partnerships,
    /// households, kin beliefs from real cues, huts and stores filled through <see cref="Goods"/>. Only the marked helpers write
    /// state directly (test-only). Every fixture runs inside an M4 game (<c>Households.Enabled = 1</c>).
    /// </summary>
    public static class HouseholdFixtures
    {
        /// <summary>A quiet M4 game (no founders, no wild sources; LearningFixtures.Empty) with M4 on.</summary>
        public static GodSim Empty(ulong seed, Action<SimTuning> tune = null) => LearningFixtures.Empty(seed, tune);

        /// <summary>A person of M4 (founder genome from its id, the given sex and age), faction 0, at a node. The traits come from its genome.</summary>
        public static Npc Person(GodSim sim, int node, byte sex, int ageMilli, Faction f = Faction.F0, int hunger = 200)
        {
            var s = sim.State;
            var hs = s.Households ?? throw new InvalidOperationException("M4 is off in this game");
            hs.Creating = true;
            Npc npc;
            try { npc = PopulationModule.AddNpc(s, f, node, InstinctSet.Default, s.Tuning.Learning.DefaultMode, false); }
            finally { hs.Creating = false; }
            var genome = GenomeOps.Founder(s.Rng.Fork("genome-founder", npc.Id), s.Tuning.Genetics.FounderSpread);
            Founders.MakeLife(s, npc, sex, genome, 0, 0, 0);
            Founders.SetAge(s, npc, ageMilli);
            npc.Drives[Drive.Hunger] = hunger * 1000;
            Array.Copy(npc.Drives, npc.RewardSnap, Drive.Count);
            return npc;
        }

        /// <summary>
        /// A family (SHARED_CONTRACTS 11 "Family"): <paramref name="adults"/> adults (alternating sexes: female, male, ...) with a
        /// real partnership between the first two, children of the given ages raised by them (kin beliefs from being raised),
        /// one household; optionally a built hut at the node with its store holding <paramref name="store"/>.
        /// </summary>
        public static FamilyIds Family(GodSim sim, int node, int adults, int[] childAgesMilli, bool hut, params (Ware ware, int count)[] store)
        {
            var s = sim.State;
            var ids = new FamilyIds { Adults = new int[adults], Children = new int[childAgesMilli?.Length ?? 0] };
            Household h = null;
            var people = new List<Npc>();
            for (int i = 0; i < adults; i++)
            {
                var a = Person(sim, node, (byte)(i % 2), 1500 + 100 * i);
                ids.Adults[i] = a.Id;
                people.Add(a);
                if (h == null) h = HouseholdModule.Form(s, null, a, JoinReason.Founded);
                else HouseholdModule.Join(s, null, h, a, JoinReason.Founded, 0);
            }
            if (adults >= 2)
            {
                var a = people[0]; var b = people[1];
                Partnerships.Form(s, null, a.Id, b.Id, a.Id, h.Id);
                KinCues.Believe(s, null, a, b.Id, KinRole.Partner, 900, KinSource.Partnered);
                KinCues.Believe(s, null, b, a.Id, KinRole.Partner, 900, KinSource.Partnered);
            }
            for (int c = 0; c < ids.Children.Length; c++)
            {
                var k = Person(sim, node, (byte)(c % 2), childAgesMilli[c]);
                ids.Children[c] = k.Id;
                if (h == null) h = HouseholdModule.Form(s, null, k, JoinReason.Founded);
                else HouseholdModule.Join(s, null, h, k, JoinReason.Born, 0);
                foreach (var a in people)
                {
                    KinCues.Believe(s, null, k, a.Id, KinCues.ParentRole(a.Life.Sex), 900, KinSource.Raised);
                    KinCues.Believe(s, null, a, k.Id, KinCues.ChildRole(k.Life.Sex), 900, KinSource.Raised);
                }
            }
            for (int i = 0; i < ids.Children.Length; i++)
                for (int j = 0; j < ids.Children.Length; j++)
                    if (i != j) KinCues.Believe(s, null, s.Population.Find(ids.Children[i]), ids.Children[j], KinRole.FullSibling, 800, KinSource.Raised);
            ids.Household = h?.Id ?? 0;
            if (hut && h != null)
            {
                var rights = new List<int>();
                foreach (var a in people) if (rights.Count < s.Tuning.Population.HutBeds) rights.Add(a.Id);
                var built = PopulationModule.AddHut(s, Faction.F0, node, true, rights.ToArray());
                HouseholdModule.AdoptHut(s, null, h, built);
                ids.Hut = built.Id; ids.Bin = h.Bin;
                int day = (int)SimCalendar.Of(s).Day(s.Tick);
                foreach (var (ware, count) in store)
                    if (count > 0)
                        Goods.Produce(s, null, Holder.Bin(h.Bin), ware, count, Wares.IsFood(ware) ? day + PopulationModule.SpoilDays(s.Tuning.Ecology, ware) : -1,
                            ware == Ware.Axe ? s.Tuning.Population.AxeUses : 0, Owner.Household(h.Id), ProduceReason.Placed, 0, 0);
            }
            return ids;
        }

        /// <summary>A shared hut (SHARED_CONTRACTS 11): <paramref name="memberCount"/> unrelated adults in one household with a hut and store.</summary>
        public static FamilyIds SharedHut(GodSim sim, int node, int memberCount, params (Ware ware, int count)[] store)
        {
            var s = sim.State;
            var ids = Family(sim, node, 1, Array.Empty<int>(), true, store);
            var h = s.Households.Find(ids.Household);
            var adults = new List<int>(ids.Adults);
            var hut = s.Population.HutById(ids.Hut);
            for (int i = 1; i < memberCount; i++)
            {
                var a = Person(sim, node, (byte)(i % 2), 1500);
                HouseholdModule.Join(s, null, h, a, JoinReason.Invited, 0);
                adults.Add(a.Id);
            }
            ids.Adults = adults.ToArray();
            return ids;
        }

        /// <summary>Test-only: sets a kin belief directly (source Told).</summary>
        public static void SetKinBelief(GodSim sim, int a, int b, KinRole role, int confidence, KinSource source = KinSource.Told) =>
            KinCues.Believe(sim.State, null, sim.State.Population.Find(a), b, role, confidence, source);

        /// <summary>"Death now" (SHARED_CONTRACTS 11): the next tick kills the person through <c>PopulationModule.Kill</c>, first in the tick.</summary>
        public static void Kill(GodSim sim, int npc, DeathCause cause)
        {
            sim.AddSystem(1, new KillOnce(npc, cause));
        }

        /// <summary>Test-only: the custom a faction's executors apply.</summary>
        public static void SetCustom(GodSim sim, Faction f, Custom custom)
        {
            var e = sim.State.Tuning.Estate;
            switch ((int)f)
            {
                case 0: e.CustomF0 = (int)custom; break; case 1: e.CustomF1 = (int)custom; break; case 2: e.CustomF2 = (int)custom; break;
                case 3: e.CustomF3 = (int)custom; break; case 4: e.CustomF4 = (int)custom; break; default: e.CustomF5 = (int)custom; break;
            }
        }

        /// <summary>Test-only: goods into a person's carry (placed, owned by it) through <see cref="Goods.Produce"/>.</summary>
        public static int Give(GodSim sim, Npc npc, Ware w, int count)
        {
            var s = sim.State;
            int day = (int)SimCalendar.Of(s).Day(s.Tick);
            return Goods.Produce(s, null, Holder.Carry(npc.Id), w, count, Wares.IsFood(w) ? day + PopulationModule.SpoilDays(s.Tuning.Ecology, w) : -1,
                w == Ware.Axe ? s.Tuning.Population.AxeUses : 0, Owner.Npc(npc.Id), ProduceReason.Placed, npc.Id, 0);
        }

        private sealed class KillOnce : ISimSystem
        {
            private readonly int _npc; private readonly DeathCause _cause; private bool _done;
            public KillOnce(int npc, DeathCause cause) { _npc = npc; _cause = cause; }
            public void Tick(GodState s, EventBuffer ev)
            {
                if (_done) return;
                _done = true;
                var n = s.Population.Find(_npc);
                if (n != null && n.Alive) PopulationModule.Kill(s, ev, n, _cause);
            }
        }
    }
}
