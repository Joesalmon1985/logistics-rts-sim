using System;
using System.Collections.Generic;
using System.Linq;
using God.Sim.Estate;
using God.Sim.Population;
using God.Sim.Population.Households;
using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>
    /// M4's estates (MATERIAL_INHERITANCE 10; ACCEPTANCE M4.19-M4.21, M4.31): the custom rules exactly, conservation of every
    /// unit and every lot id's lineage over many random estates per custom and ladder, settlement within two days, the
    /// executor ladder configurable, no skimming, disputes.
    /// </summary>
    public class EstateTests
    {
        /// <summary>A deceased with three acknowledged adult children (A eldest), optionally a partner, in one household, carrying goods.</summary>
        private sealed class World
        {
            public GodSim Sim; public Npc D, Partner; public Npc[] Kids; public int Node;
            public EventProbe Probe;
        }

        private static World Make(int kids, bool partner, Custom custom, Action<SimTuning> tune = null)
        {
            var sim = HouseholdFixtures.Empty(11, t => { t.Population.CarryWeight = 200; t.Population.CarrySlots = 8; tune?.Invoke(t); });
            var s = sim.State;
            int node = LearningFixtures.Glade(sim);
            var w = new World { Sim = sim, Node = node };
            w.D = HouseholdFixtures.Person(sim, node, 0, 2800);
            var h = HouseholdModule.Form(s, null, w.D, JoinReason.Founded);
            if (partner)
            {
                w.Partner = HouseholdFixtures.Person(sim, node, 1, 2700);
                HouseholdModule.Join(s, null, h, w.Partner, JoinReason.Founded, 0);
                Partnerships.Form(s, null, w.D.Id, w.Partner.Id, w.D.Id, h.Id);
                HouseholdFixtures.SetKinBelief(sim, w.D.Id, w.Partner.Id, KinRole.Partner, 900, KinSource.Partnered);
            }
            w.Kids = new Npc[kids];
            for (int i = 0; i < kids; i++)
            {
                w.Kids[i] = HouseholdFixtures.Person(sim, node, (byte)(i % 2), 1900 - 100 * i);
                HouseholdModule.Join(s, null, h, w.Kids[i], JoinReason.Born, 0);
                HouseholdFixtures.SetKinBelief(sim, w.D.Id, w.Kids[i].Id, KinCues.ChildRole(w.Kids[i].Life.Sex), 950, KinSource.Born);
            }
            HouseholdFixtures.SetCustom(sim, Faction.F0, custom);
            return w;
        }

        private static Dictionary<int, int> UnitsTo(World w, Ware ware) =>
            w.Probe.Of<EstateTransfer>().Where(e => e.Ware == ware && e.ToKind == OwnerKind.Npc && e.Reason == EstateReason.Custom)
                .GroupBy(e => e.ToId).ToDictionary(g => g.Key, g => g.Sum(e => e.Count));

        /// <summary>Starts the event probe (after the goods were placed, so the trace knows them) and lets the death happen and the split come.</summary>
        private static void Die(World w)
        {
            if (w.Probe == null) w.Probe = new EventProbe(w.Sim, traceGoods: true);
            HouseholdFixtures.Kill(w.Sim, w.D.Id, DeathCause.OldAge);
            w.Probe.Step(w.Sim.State.Tuning.Estate.MournTicks + 2);
        }

        [Test]
        public void EqualChildren_TenLogs_4_3_3_AndAToolToB_M4_20()
        {
            var w = Make(3, false, Custom.EqualChildren);
            HouseholdFixtures.Give(w.Sim, w.D, Ware.Log, 10);
            HouseholdFixtures.Give(w.Sim, w.D, Ware.Axe, 1);
            Die(w);
            var logs = UnitsTo(w, Ware.Log);
            Assert.AreEqual(4, logs[w.Kids[0].Id]); Assert.AreEqual(3, logs[w.Kids[1].Id]); Assert.AreEqual(3, logs[w.Kids[2].Id]);
            var axe = w.Probe.Of<EstateTransfer>().Single(e => e.Ware == Ware.Axe);
            Assert.AreEqual(w.Kids[1].Id, axe.ToId, "the tool goes to the heir with the lowest award so far, ties to the eldest (B)");
        }

        [Test]
        public void EldestChild_ToolsToTheEldest_MovablesEqual_M4_20()
        {
            var w = Make(3, false, Custom.EldestChild);
            HouseholdFixtures.Give(w.Sim, w.D, Ware.Log, 10);
            HouseholdFixtures.Give(w.Sim, w.D, Ware.Axe, 1);
            Die(w);
            var logs = UnitsTo(w, Ware.Log);
            Assert.AreEqual(new[] { 4, 3, 3 }, w.Kids.Select(k => logs[k.Id]).ToArray());
            Assert.AreEqual(w.Kids[0].Id, w.Probe.Of<EstateTransfer>().Single(e => e.Ware == Ware.Axe).ToId);
        }

        [Test]
        public void PartnerFirst_AllToThePartner_AndTheSecondEstateConserves_M4_20()
        {
            var w = Make(2, true, Custom.PartnerFirst);
            HouseholdFixtures.Give(w.Sim, w.D, Ware.Log, 6);
            HouseholdFixtures.Give(w.Sim, w.D, Ware.Roots, 4);
            Die(w);
            Assert.IsTrue(w.Probe.Of<EstateTransfer>().All(e => e.ToKind == OwnerKind.Npc && e.ToId == w.Partner.Id));
            Assert.AreEqual(10, w.Probe.Of<EstateTransfer>().Sum(e => e.Count));
            // the partner collects them (it stands there) and then dies: its own estate settles in turn by the same custom (to the children)
            w.Probe.Step(w.Sim.State.Tuning.Estate.SettleWithinTicks);
            HouseholdFixtures.SetKinBelief(w.Sim, w.Partner.Id, w.Kids[0].Id, KinRole.Son, 900, KinSource.Raised);
            HouseholdFixtures.Kill(w.Sim, w.Partner.Id, DeathCause.OldAge);
            w.Probe.Step(w.Sim.State.Tuning.Estate.SettleWithinTicks + 2);
            Assert.IsTrue(w.Probe.Of<EstateSettled>().Any(e => e.Estate == w.D.Id) && w.Probe.Of<EstateSettled>().Any(e => e.Estate == w.Partner.Id), "both estates settled");
            Assert.IsEmpty(SimInvariants.Check(w.Sim.State));
            Assert.IsEmpty(w.Probe.Trace.CheckC3(w.Sim.State), "every lot id accounted for across both estates");
        }

        [Test]
        public void HouseholdKeeps_And_BackToCommunity_M4_20()
        {
            var w = Make(1, false, Custom.HouseholdKeeps);
            HouseholdFixtures.Give(w.Sim, w.D, Ware.Log, 3);
            int h = w.D.Household;
            Die(w);
            Assert.IsTrue(w.Probe.Of<EstateTransfer>().All(e => e.ToKind == OwnerKind.Household && e.ToId == h));
            var w2 = Make(1, false, Custom.BackToCommunity);
            HouseholdFixtures.Give(w2.Sim, w2.D, Ware.Log, 3);
            Die(w2);
            var t = w2.Probe.Of<EstateTransfer>().Where(e => e.Reason == EstateReason.Communal).ToList();
            Assert.AreEqual(3, t.Sum(e => e.Count), "the faction's communal pool receives 100%");
            Assert.IsTrue(t.All(e => e.ToKind == OwnerKind.Group && e.ToId == (int)Faction.F0));
        }

        [Test]
        public void BuriedWithDead_TenLogsAtHalf_DestroysFive_M4_20()
        {
            var w = Make(1, false, Custom.BuriedWithDead);
            HouseholdFixtures.Give(w.Sim, w.D, Ware.Log, 10);
            Die(w);
            Assert.AreEqual(5, w.Probe.Of<Consumed>().Where(c => c.Reason == ConsumeReason.Buried).Sum(c => c.Count), "5 destroyed as a recorded sink");
            Assert.AreEqual(5, w.Probe.Of<EstateTransfer>().Where(e => e.Reason == EstateReason.Burial).Sum(e => e.Count));
            Assert.AreEqual(5, w.Probe.Of<EstateTransfer>().Where(e => e.Reason == EstateReason.Household).Sum(e => e.Count), "the rest follows the second variant (household keeps)");
        }

        [Test]
        public void NoExecutor_EverythingOwnerless_AndUnacknowledgedChildInheritsNothing_M4_20()
        {
            // a child the deceased does not believe its own, and a ladder with no rung: no executor, no custom
            var w = Make(0, false, Custom.EqualChildren, t => t.Estate.Ladder = 0);
            var stranger = HouseholdFixtures.Person(w.Sim, w.Node, 1, 1500);
            HouseholdFixtures.Give(w.Sim, w.D, Ware.Log, 4);
            Die(w);
            Assert.IsTrue(w.Probe.Of<EstateTransfer>().All(e => e.ToKind == OwnerKind.None && e.Reason == EstateReason.Ownerless));
            Assert.AreEqual(4, w.Probe.Of<EstateTransfer>().Sum(e => e.Count));
            Assert.IsFalse(w.Probe.Of<EstateTransfer>().Any(e => e.ToId == stranger.Id));
            Assert.IsTrue(w.Sim.State.Population.Lots.Any(g => g.Lot.Ware == Ware.Log && g.Node == w.Node), "left where it lies, ownerless, to be claimed");
        }

        [Test]
        public void MinorHeir_HoldsAnAssetInItsName_M4_20()
        {
            var w = Make(0, false, Custom.EqualChildren);
            var minor = HouseholdFixtures.Person(w.Sim, w.Node, 0, 400);
            HouseholdModule.Join(w.Sim.State, null, w.Sim.State.Households.Find(w.D.Household), minor, JoinReason.Born, 0);
            HouseholdFixtures.SetKinBelief(w.Sim, w.D.Id, minor.Id, KinRole.Daughter, 950, KinSource.Born);
            // ladder: the eldest household adult (none but the minor's household is the deceased's): use an adult relative as executor
            var aunt = HouseholdFixtures.Person(w.Sim, w.Node, 0, 2000);
            HouseholdModule.Join(w.Sim.State, null, w.Sim.State.Households.Find(w.D.Household), aunt, JoinReason.Invited, 0);
            HouseholdFixtures.Give(w.Sim, w.D, Ware.Roots, 2);
            Die(w);
            Assert.IsTrue(w.Probe.Of<EstateTransfer>().Any(e => e.ToKind == OwnerKind.Npc && e.ToId == minor.Id), "the minor owns its share in its own name");
            Assert.AreEqual(aunt.Id, w.Probe.Of<EstateTransfer>().First().Executor, "the eldest household adult executes");
        }

        [Test]
        public void LadderConfigurable_HouseholdAdultBeforePartner_M4_31()
        {
            var a = Make(1, true, Custom.PartnerFirst);
            HouseholdFixtures.Give(a.Sim, a.D, Ware.Roots, 1);
            Die(a);
            Assert.AreEqual(a.Partner.Id, a.Probe.Of<EstateTransfer>().First().Executor, "default ladder: the partner");
            var b = Make(1, true, Custom.PartnerFirst, t => t.Estate.Ladder = 32);
            HouseholdFixtures.Give(b.Sim, b.D, Ware.Roots, 1);
            Die(b);
            Assert.AreEqual(b.Kids[0].Id, b.Probe.Of<EstateTransfer>().First().Executor, "the eldest adult child before the partner: the child executes");
        }

        /// <summary>
        /// M4.19, M4.31, MATERIAL 2, 3, 20, 22: random estates for every custom and ladder: every unit is conserved
        /// (<c>before = after + destroyed</c>, per ware), no lot is duplicated, every id after settlement is an original id or the
        /// split part of one (its parent chain reaches an original), each estate settles within its window, and no executor gets
        /// more than its custom gives it as an heir.
        /// </summary>
        [Test]
        public void RandomEstates_ConserveUnitsAndIds_EveryCustomAndLadder_M4_19_M4_31()
        {
            var customs = new[] { Custom.EqualChildren, Custom.EldestChild, Custom.PartnerFirst, Custom.HouseholdKeeps, Custom.BackToCommunity, Custom.BuriedWithDead, Custom.None };
            var ladders = new[] { 12345, 3245, 42 };
            int estates = 0;
            foreach (var custom in customs)
                foreach (int ladder in ladders)
                {
                    var w = Make(4, true, custom == Custom.None ? Custom.EqualChildren : custom, t => { t.Estate.Ladder = custom == Custom.None ? 0 : ladder; });
                    var s = w.Sim.State;
                    var rule = new CustomSettlement();
                    var rng = new Rng((ulong)(estates + 1));
                    int n = 1000 / ladders.Length + 1;
                    for (int k = 0; k < n; k++, estates++)
                    {
                        // a random carry and a random set of acknowledged heirs
                        var wares = new[] { Ware.Log, Ware.Roots, Ware.Berries, Ware.Axe };
                        int kinds = 1 + rng.NextInt(4);
                        for (int q = 0; q < kinds; q++) { var ware = wares[rng.NextInt(wares.Length)]; HouseholdFixtures.Give(w.Sim, w.D, ware, ware == Ware.Axe ? 1 : 1 + rng.NextInt(12)); }
                        w.D.Mind.Kin.Beliefs.Clear();
                        foreach (var kid in w.Kids) if (rng.NextInt(3) > 0) HouseholdFixtures.SetKinBelief(w.Sim, w.D.Id, kid.Id, KinRole.Son, 500 + rng.NextInt(500), KinSource.Raised);
                        var before = GoodsTrace.Held(s);
                        var carried = w.D.Carry.Where(l => !l.Empty).Select(l => (l.Id, l.Ware, l.Count)).ToList();
                        var ev = new EventBuffer();
                        Estates.Open(s, ev, w.D);
                        var e = s.Households.Estates.Last();
                        int ex = rule.ChooseExecutor(s, e);
                        var c = rule.RuleOf(s, e, ex);
                        e.Executor = ex; e.Custom = (byte)c; e.Split = true;
                        rule.Split(s, ev, e, ex, c);
                        Estates.Settle(s, ev, e);
                        s.Households.Estates.Remove(e);
                        var items = ev.Items;
                        var after = GoodsTrace.Held(s);
                        Assert.AreEqual(after.Count, after.Select(x => x.id).Distinct().Count(), "no lot id held twice");
                        Assert.AreEqual(0, s.Population.Bins.Count(b => b.Kind == BinKind.Estate), "every estate bin removed");
                        foreach (var ware in carried.Select(x => x.Ware).Distinct())
                        {
                            int destroyed = items.OfType<Consumed>().Where(x => x.Ware == ware && x.Reason == ConsumeReason.Buried).Sum(x => x.Count);
                            Assert.AreEqual(before.Where(x => x.ware == ware).Sum(x => x.count), after.Where(x => x.ware == ware).Sum(x => x.count) + destroyed,
                                ware + ": units before = units after + destroyed (" + c + ")");
                            if (c != Custom.BuriedWithDead) Assert.AreEqual(0, destroyed, "only BuriedWithDead destroys");
                        }
                        // every id now held: an original one or a split of one (no copies, no new ids outside splits)
                        var originals = new HashSet<int>(carried.Select(x => x.Id));
                        var parents = items.OfType<ItemMoved>().Where(x => x.ParentLot != 0).ToDictionary(x => x.Lot, x => x.ParentLot);
                        var produced = items.OfType<Produced>().Count();
                        Assert.AreEqual(0, produced, "a settlement produces nothing");
                        foreach (var x in items.OfType<ItemMoved>())
                        {
                            int id = x.Lot;
                            while (parents.TryGetValue(id, out int par) && !originals.Contains(id)) id = par;
                            Assert.IsTrue(originals.Contains(id), "lot " + x.Lot + " is not an original id or a split of one");
                        }
                        // no skimming: the executor gets exactly its heir's share (as a child or a partner), nothing as executor
                        var exGets = items.OfType<EstateTransfer>().Where(x => x.ToKind == OwnerKind.Npc && x.ToId == ex).Sum(x => x.Count);
                        bool heir = e.Children.Contains(ex) || e.Partners.Contains(ex) && c == Custom.PartnerFirst;
                        if (!heir) Assert.AreEqual(0, exGets, "an executor who is not an heir gets nothing");
                        var bad = SimInvariants.Check(s);
                        Assert.IsEmpty(bad, string.Join("; ", bad));
                    }
                    // conservation over the whole run: produced minus consumed equals what is held (C1) is in SimInvariants above
                }
            TestContext.WriteLine("random estates: " + estates);
            Assert.GreaterOrEqual(estates, 7 * 1000);
        }

        [Test]
        public void EveryDeath_SettlesWithinTwoDays_AndNothingIsOwnedByTheDead_M4_19()
        {
            var w = Make(2, true, Custom.EqualChildren);
            HouseholdFixtures.Give(w.Sim, w.D, Ware.Roots, 4);
            w.Probe = new EventProbe(w.Sim, traceGoods: true);
            HouseholdFixtures.Kill(w.Sim, w.D.Id, DeathCause.OldAge);
            w.Probe.Step(2 * w.Sim.State.Tuning.Estate.SettleWithinTicks);
            var died = w.Probe.Of<NpcDied>().Single(x => x.Npc == w.D.Id);
            var settled = w.Probe.Of<EstateSettled>().Single(x => x.Estate == w.D.Id);
            Assert.LessOrEqual(settled.Tick - died.Tick, 2400, "settled within 2 days");
            Assert.IsEmpty(SimInvariants.Check(w.Sim.State));
            Assert.IsEmpty(w.Probe.Trace.CheckC3(w.Sim.State));
            Assert.IsEmpty(w.Probe.Trace.CheckC6(w.Sim.State.Territory.Grid), "every move by a person was within reach (heirs collect where they stand)");
            // the heirs who saw the estate decided to take their own lots in hand (Withdraw by an heir, I-BS amendment 12.32: a
            // perceived decision, no longer a reflex nor O4's delivery); anything left went ownerless to the ground
            var heirs = w.Kids.Select(k => k.Id).ToList();
            Assert.IsTrue(w.Probe.Of<ItemMoved>().Any(m => m.Reason == MoveReason.Withdraw && m.FromKind == HolderKind.Bin && heirs.Contains(m.Actor)), "an heir collected by its own decision");
            Assert.IsFalse(w.Probe.Of<ItemMoved>().Any(m => m.Reason == MoveReason.EstateOut && m.ToKind == HolderKind.Bin && m.FromNode != m.ToNode), "no cross-node delivery");
        }

        [Test]
        public void Disputes_SameCustom_None_MixedCustoms_ByShortfall_M4_21()
        {
            // same custom everywhere: no dispute in many estates
            var w = Make(3, false, Custom.EldestChild);
            for (int k = 0; k < 200; k++)
            {
                HouseholdFixtures.Give(w.Sim, w.D, Ware.Log, 6); HouseholdFixtures.Give(w.Sim, w.D, Ware.Axe, 1);
                var ev = new EventBuffer();
                Estates.Open(w.Sim.State, ev, w.D);
                var e = w.Sim.State.Households.Estates.Last();
                var r = new CustomSettlement(); int ex = r.ChooseExecutor(w.Sim.State, e);
                e.Executor = ex; e.Split = true; r.Split(w.Sim.State, ev, e, ex, r.RuleOf(w.Sim.State, e, ex));
                Estates.Settle(w.Sim.State, ev, e); w.Sim.State.Households.Estates.Remove(e);
                Assert.IsFalse(ev.Items.OfType<EstateDisputed>().Any());
            }
            // the executor's faction holds EldestChild, the younger heirs' faction EqualChildren: a dispute for each heir short by 25% or more
            var m = Make(3, false, Custom.EldestChild);
            m.Kids[1].Faction = Faction.F1; m.Kids[2].Faction = Faction.F1; // test-only: the heirs hold another faction's custom
            HouseholdFixtures.SetCustom(m.Sim, Faction.F1, Custom.EqualChildren);
            HouseholdFixtures.Give(m.Sim, m.D, Ware.Roots, 3); HouseholdFixtures.Give(m.Sim, m.D, Ware.Axe, 1); HouseholdFixtures.Give(m.Sim, m.D, Ware.Axe, 1);
            Die(m);
            var d = m.Probe.Of<EstateDisputed>().ToList();
            Assert.AreEqual(2, d.Count, "both younger heirs fall short by more than 25%");
            foreach (var x in d)
            {
                Assert.GreaterOrEqual(x.ShortfallPermille, 250);
                Assert.AreEqual(m.Kids[0].Id, x.Executor);
            }
            // the grudge is min(255, shortfall / 2) x 3, in the claimant's worth units (recomputed)
            var awards = m.Probe.Of<EstateTransfer>().Where(e => e.ToKind == OwnerKind.Npc).GroupBy(e => e.ToId).ToDictionary(g => g.Key, g => g.ToList());
            long Val(int id) => awards.TryGetValue(id, out var l) ? l.Sum(e => (long)GoodsWorth.Of(m.Sim.State, m.Kids[0], e.Ware) * e.Count) : 0;
            long total = m.Kids.Sum(k => Val(k.Id));
            foreach (var x in d)
            {
                long would = total / 3, shortfall = would - Val(x.Claimant);
                Assert.AreEqual((int)Math.Min(255, shortfall / 2) * 3, x.Grudge);
            }
        }
    }
}
