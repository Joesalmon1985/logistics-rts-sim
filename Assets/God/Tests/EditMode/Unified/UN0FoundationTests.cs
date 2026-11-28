using System;
using System.Collections.Generic;
using System.Linq;
using God.Sim.Divinity;
using God.Sim.Population;
using God.Sim.Unified;
using NUnit.Framework;
using UBuilding = God.Sim.Unified.Building;

namespace God.Sim.Tests
{
    /// <summary>
    /// UN0 (contracts 23, UN0 spec 13): the unification foundation. The switch and its JSON, the state slot and hash part, the
    /// install gates and the person-tick site, physical goods without title, origins, the invariants U-C1..U-C6 with mutations, the
    /// id surfaces and hash layouts, the schemas, the effect registry, the telemetry ranges, save null-acceptance and round trip,
    /// and the Primitive control seam. OWNER: UN0.
    /// </summary>
    public class UN0FoundationTests
    {
        internal static GodSim Game(ulong seed, bool unified, int divinity = 0, Action<SimTuning> tune = null, int npcs = 3)
        {
            var t = new SimTuning();
            t.Divinity.Enabled = divinity;
            if (unified) t.Unification.Enabled = 1;
            tune?.Invoke(t);
            var setup = new GameSetup(seed, GameSetup.NoHuman, 2) { Population = true, StartNpcsPerFaction = npcs, Victory = VictorySpec.SandboxOnly() };
            return GodSim.Create(setup, t);
        }

        private static List<Npc> People(GodState s) => s.Population.Npcs.Where(Bodies.IsLivingPerson).ToList();

        /// <summary>Drops everything a person carries (the founder pack) so a fixture has room (carry weight 6).</summary>
        private static void Free(GodState s, Npc n)
        {
            for (int i = 0; i < n.Carry.Length; i++)
                if (!n.Carry[i].Empty) Assert.IsTrue(Goods.Move(s, null, Holder.Carry(n.Id), n.Carry[i].Id, n.Carry[i].Count, Holder.Ground(n.Node), MoveReason.Drop, n.Id).Ok);
        }

        // ---------------------------------------------------------------- the switch (23.1)

        [Test]
        public void Switch_OmittedWhenDefault_PathsSettable()
        {
            var t = new SimTuning();
            string json = t.ToJson();
            Assert.IsFalse(json.Contains("Unification"), "a default unified block is not written");
            Assert.IsTrue(t.TrySet("Unification.Enabled", 0, out var err), err);
            Assert.AreEqual(json, t.ToJson(), "Enabled = 0 is the default: the tuning JSON (and the core part) is unchanged");
            foreach (var p in new[] { "Unification.World", "Unification.Interactions", "Unification.Construction", "Unification.Exchange", "Unification.Conflict",
                "Unification.Needs.ActiveMask", "Unification.Brain.Kind", "Unification.Brain.Artifact", "Unification.Brain.Planning", "Unification.Settlements",
                "Unification.Roads", "Unification.Groups", "Unification.Board.Radius", "Unification.Board.SeaRing", "Unification.Start.Tribes",
                "Unification.Start.Adults", "Unification.Start.Children", "Unification.Start.SettlementsPerTribe" })
                Assert.IsTrue(t.TryGet(p, out _), p);
            Assert.IsTrue(t.TryGet("Unification.Needs.ActiveMask", out long mask)); Assert.AreEqual(0xFFF, mask);
            Assert.IsTrue(t.TryGet("Unification.Board.Radius", out long r)); Assert.AreEqual(4, r);
            Assert.IsTrue(t.TryGet("Unification.Start.Adults", out long a)); Assert.AreEqual(7, a);
            Assert.IsTrue(t.TryGet("Unification.Start.Children", out long c)); Assert.AreEqual(5, c);
            Assert.IsTrue(t.TrySet("Unification.Enabled", 1, out err), err);
            Assert.IsTrue(t.ToJson().Contains("\"Unification\":{\"Enabled\":1"), "written once changed");
            Assert.AreEqual(1, t.Clone().Unification.Enabled, "survives the save format");
        }

        [Test]
        public void Off_NoState_NoPart()
        {
            var sim = Game(1, false);
            var s = sim.State;
            Assert.IsNull(s.Unification);
            Assert.IsFalse(Unified.Unified.On(s));
            Assert.IsFalse(GodSim.PartNamesOf(s).Contains("unification"));
            Assert.AreEqual(GodSim.PartNamesOf(s).Length, sim.StateHashParts().Length);
            Assert.IsFalse(SaveFile.From(sim).ToJson().Contains("\"Unification\""), "a legacy save has no unified slot");
        }

        [Test]
        public void On_StatePartAndRecords()
        {
            var sim = Game(2, true, tune: t => { t.Civ.Enabled = 1; t.Orgs.Enabled = 1; });
            var s = sim.State;
            Assert.IsNotNull(s.Unification);
            Assert.IsTrue(Unified.Unified.On(s));
            var names = GodSim.PartNamesOf(s);
            Assert.AreEqual("unification", names[names.Length - 1], "after divine (and every other part)");
            Assert.AreEqual(names.Length, sim.StateHashParts().Length);
            Assert.IsNull(s.Civ, "no Civ state in a unified game"); Assert.IsNull(s.Organisations, "no Orgs state in a unified game");
            foreach (var n in People(s)) Assert.IsNotNull(s.Unification.PersonOf(n.Id), "a unified record for person " + n.Id);
            Assert.Throws<ArgumentOutOfRangeException>(() => GodSim.Create(new GameSetup(3), new SimTuning { Unification = { Enabled = 1 } }), "a unified game needs a population");
        }

        // ---------------------------------------------------------------- install gates and the person tick (23.3)

        private static string[] SystemNames(GodSim sim) => sim.Systems.Select(x => x.GetType().Name).ToArray();

        [Test]
        public void Unified_InstallGate()
        {
            string[] legacyOnly = { "DiceSystem", "EconomySystem", "MilitarySystem", "TradeSystem", "FactionSystem", "EstateSystem", "CivSystem", "OrganisationSystem" };
            var off = SystemNames(Game(4, false, divinity: 1, tune: t => { t.Civ.Enabled = 1; t.Orgs.Enabled = 1; }));
            foreach (var n in legacyOnly) CollectionAssert.Contains(off, n, "legacy installs " + n);
            CollectionAssert.DoesNotContain(off, "UnifiedSystem");
            var on = SystemNames(Game(4, true, divinity: 1, tune: t => { t.Civ.Enabled = 1; t.Orgs.Enabled = 1; }));
            foreach (var n in legacyOnly) CollectionAssert.DoesNotContain(on, n, "unified skips " + n);
            foreach (var n in new[] { "PopulationSystem", "LifeCycleSystem", "SocialSystem", "DivinitySystem", "MiraclesSystem", "UnifiedSystem" }) CollectionAssert.Contains(on, n, "unified keeps " + n);
            // a loaded unified game installs the same
            var sim = Game(4, true, divinity: 1);
            var loaded = SaveFile.FromJson(SaveFile.From(sim).ToJson()).ToSim();
            CollectionAssert.AreEqual(SystemNames(sim), SystemNames(loaded));
        }

        [Test]
        public void Unified_PersonTickSite_ReplacesThinkAndAct()
        {
            var off = Game(5, false); off.Step(300);
            Assert.Greater(off.State.Population.Stats.Decisions, 0, "the legacy path decides");
            var on = Game(5, true); on.Step(300);
            Assert.AreEqual(0, on.State.Population.Stats.Decisions, "Thinker.Think never runs in a unified game");
            foreach (var n in People(on.State)) { Assert.AreEqual(0, n.Action.Verb, "no legacy verb starts"); Assert.AreEqual(0, n.Mind.DayDecisions); }
            Assert.Less(on.State.Population.Npcs.First(Bodies.IsPerson).Drives[God.Sim.Learning.Drive.Hunger], 1000001, "Body() still runs");
            Assert.Greater(on.State.Population.Npcs.First(Bodies.IsPerson).Drives[God.Sim.Learning.Drive.Hunger], 0, "Body() still runs: hunger rises");
        }

        // ---------------------------------------------------------------- physical goods without title (23.4)

        [Test]
        public void Unified_GoodsNoTitle()
        {
            var sim = Game(6, true);
            var s = sim.State; var ev = new EventBuffer();
            var ppl = People(s); var a = ppl[0]; var b = ppl[1];
            b.Node = a.Node; Free(s, a); Free(s, b);
            long reward0 = a.Mind.Store.RewardSumDay + a.Mind.Store.PunishSumDay;
            int lot = Goods.Produce(s, ev, Holder.Carry(a.Id), Ware.Log, 1, -1, 0, Owner.Npc(a.Id), ProduceReason.HandGathered, a.Id, 0);
            Assert.Greater(lot, 0);
            Assert.IsTrue(Find(s, lot, out var l)); Assert.IsTrue(l.Owner.IsNone, "the owner argument is ignored");
            Assert.AreEqual(reward0, a.Mind.Store.RewardSumDay + a.Mind.Store.PunishSumDay, "PayProduce pays nothing");
            foreach (OwnerRule rule in Enum.GetValues(typeof(OwnerRule)))
            {
                var r1 = Goods.Move(s, ev, Holder.Carry(a.Id), lot, 1, Holder.Ground(a.Node), MoveReason.Drop, a.Id, rule, Owner.Npc(a.Id));
                Assert.IsTrue(r1.Ok, rule + " " + r1.Status);
                var r2 = Goods.Move(s, ev, Holder.Ground(a.Node), r1.IntoLot, 1, Holder.Carry(b.Id), MoveReason.PickUp, b.Id, rule, Owner.Npc(b.Id));
                Assert.IsTrue(r2.Ok, rule + " " + r2.Status);
                Assert.IsTrue(r2.After.IsNone && r2.Before.IsNone, rule + ": no owner before or after");
                var r3 = Goods.Move(s, ev, Holder.Carry(b.Id), r2.IntoLot, 1, Holder.Carry(a.Id), MoveReason.HandOver, b.Id, rule, Owner.Npc(a.Id));
                Assert.IsTrue(r3.Ok, rule + " " + r3.Status);
                lot = r3.IntoLot;
            }
            Assert.IsFalse(Goods.SetOwner(s, ev, Holder.Carry(a.Id), lot, Owner.Npc(b.Id), OwnershipBasis.Gift, 0), "SetOwner refuses");
            int bin = Goods.NewBin(s, BinKind.Storage, Owner.Household(1), a.Node, 0);
            Assert.IsTrue(Goods.BinById(s.Population, bin).Owner.IsNone, "a unified bin has no owner");
            Assert.AreEqual(reward0, a.Mind.Store.RewardSumDay + a.Mind.Store.PunishSumDay, "no reward from any move");
            Assert.AreEqual(0, b.Mind.Store.RewardSumDay + b.Mind.Store.PunishSumDay, "no reward to the receiver");
            // death spill: goods stay where the body lies; no estate, no title
            PopulationModule.Kill(s, ev, a, DeathCause.Violence);
            Assert.IsTrue(a.Carry.All(x => x.Empty));
            Assert.IsTrue(s.Population.Lots.Any(g => g.Node == a.Node && g.Lot.Ware == Ware.Log && g.Lot.Owner.IsNone));
            Assert.IsTrue(s.Households == null || s.Households.Estates.Count == 0, "no estate opened");
            AssertNoTitleEvents(ev.Items);
            Assert.IsEmpty(SimInvariants.Check(s), "invariants after the moves and the death");
        }

        internal static void AssertNoTitleEvents(IEnumerable<SimEvent> events)
        {
            foreach (var e in events)
            {
                Assert.IsFalse(e is OwnershipChanged, "OwnershipChanged emitted");
                Assert.IsFalse(e is God.Sim.Civ.TheftCommitted, "TheftCommitted emitted");
                Assert.IsFalse(e is God.Sim.Civ.UseRightGranted, "UseRightGranted emitted");
                Assert.IsFalse(e is God.Sim.Population.Households.EstateTransfer, "EstateTransfer emitted");
                Assert.IsFalse(e is God.Sim.Population.Households.EstateOpened, "EstateOpened emitted");
                if (e is ItemMoved m) Assert.IsTrue(m.OwnerBeforeKind == OwnerKind.None && m.OwnerAfterKind == OwnerKind.None, "ItemMoved with an owner");
                if (e is Produced p) Assert.AreEqual(OwnerKind.None, p.OwnerKind, "Produced with an owner");
            }
        }

        private static bool Find(GodState s, int lot, out ItemLot l)
        {
            foreach (var n in s.Population.Npcs) foreach (var x in n.Carry) if (!x.Empty && x.Id == lot) { l = x; return true; }
            foreach (var g in s.Population.Lots) if (g.Lot.Id == lot) { l = g.Lot; return true; }
            foreach (var b in s.Population.Bins) foreach (var x in b.Lots) if (x.Id == lot) { l = x; return true; }
            l = default; return false;
        }

        [Test]
        public void Legacy_GoodsKeepTitle()
        {
            // the same calls in a legacy game still re-own (the unified branch is not taken)
            var sim = Game(6, false);
            var s = sim.State; var ev = new EventBuffer();
            var a = People(s)[0]; Free(s, a);
            int lot = Goods.Produce(s, ev, Holder.Carry(a.Id), Ware.Log, 1, -1, 0, Owner.Npc(a.Id), ProduceReason.HandGathered, a.Id, 0);
            Assert.IsTrue(Find(s, lot, out var l)); Assert.AreEqual(Owner.Npc(a.Id), l.Owner);
            Assert.IsTrue(Goods.SetOwner(s, ev, Holder.Carry(a.Id), lot, Owner.None, OwnershipBasis.Abandoned, 0));
            Assert.IsTrue(ev.Items.Any(e => e is OwnershipChanged));
        }

        [Test]
        public void Unified_MergePolicy()
        {
            var sim = Game(7, true);
            var s = sim.State; var ev = new EventBuffer();
            var ppl = People(s); var a = ppl[0]; var b = ppl[1]; Free(s, a);
            int node = a.Node;
            int l1 = Goods.Produce(s, ev, Holder.Ground(node), Ware.Stone, 1, -1, 0, Owner.None, ProduceReason.Placed, a.Id, 0);
            s.Tick += 5;
            int l2 = Goods.Produce(s, ev, Holder.Ground(node), Ware.Stone, 2, -1, 0, Owner.None, ProduceReason.Placed, b.Id, 0);
            Assert.AreEqual(l1, l2, "equal ware, spoil day and uses merge whatever the provenance");
            Assert.IsTrue(UnifiedGoods.OriginOf(s, l1, out var o)); Assert.IsTrue(o.Mixed); Assert.AreEqual(s.Tick - 5, o.Tick, "the earliest tick");
            int s1 = Goods.Produce(s, ev, Holder.Ground(node), Ware.Stool, 1, -1, 0, Owner.None, ProduceReason.Placed, a.Id, 0);
            int s2 = Goods.Produce(s, ev, Holder.Ground(node), Ware.Stool, 1, -1, 0, Owner.None, ProduceReason.Placed, a.Id, 0);
            Assert.AreNotEqual(s1, s2, "a unique ware never merges");
            int f1 = Goods.Produce(s, ev, Holder.Carry(a.Id), Ware.Berries, 1, 10, 0, Owner.None, ProduceReason.Foraged, a.Id, 0);
            int f2 = Goods.Produce(s, ev, Holder.Carry(a.Id), Ware.Berries, 1, 11, 0, Owner.None, ProduceReason.Foraged, a.Id, 0);
            Assert.AreNotEqual(f1, f2, "different spoil days do not merge in unified mode");
            int f3 = Goods.Produce(s, ev, Holder.Carry(a.Id), Ware.Berries, 1, 10, 0, Owner.Npc(b.Id), ProduceReason.Foraged, b.Id, 0);
            Assert.AreEqual(f1, f3, "same spoil day merges (owner and producer ignored)");
            Assert.IsEmpty(SimInvariants.Check(s));
        }

        [Test]
        public void Unified_Goods_PhysicalOnly()
        {
            var sim = Game(8, true);
            var s = sim.State; var ev = new EventBuffer();
            var a = People(s)[0]; Free(s, a);
            int bin = Goods.NewBin(s, BinKind.Storage, Owner.None, a.Node, 4);
            var b = Goods.BinById(s.Population, bin);
            int lot = Goods.Produce(s, ev, Holder.Bin(bin), Ware.Log, 1, -1, 0, Owner.None, ProduceReason.Placed, 0, 0);
            foreach (var legacy in new[] { Owner.Household(1), new Owner(OwnerKind.Org, 2), Owner.Estate(a.Id) })
            {
                b.Owner = legacy; // a fixture writes the legacy fields directly
                for (int i = 0; i < b.Lots.Count; i++) { var x = b.Lots[i]; x.Owner = legacy; b.Lots[i] = x; }
                Assert.AreEqual(MoveStatus.NotEnough, Goods.Move(s, ev, Holder.Bin(bin), lot, 5, Holder.Carry(a.Id), MoveReason.Withdraw, a.Id).Status);
                var r = Goods.Move(s, ev, Holder.Bin(bin), lot, 1, Holder.Carry(a.Id), MoveReason.Withdraw, a.Id);
                Assert.IsTrue(r.Ok, legacy + ": anyone in reach may take, whatever the legacy owner: " + r.Status);
                Assert.IsTrue(r.After.IsNone);
                var back = Goods.Move(s, ev, Holder.Carry(a.Id), r.IntoLot, 1, Holder.Bin(bin), MoveReason.Deposit, a.Id);
                Assert.IsTrue(back.Ok, back.Status.ToString());
                lot = back.IntoLot;
            }
            b.Owner = Owner.None; for (int i = 0; i < b.Lots.Count; i++) { var x = b.Lots[i]; x.Owner = Owner.None; b.Lots[i] = x; }
            // physical refusals stay: full bin, out of reach
            Goods.Produce(s, ev, Holder.Carry(a.Id), Ware.Log, 1, -1, 0, Owner.None, ProduceReason.Placed, a.Id, 0);
            var carried = a.Carry.First(x => !x.Empty && x.Ware == Ware.Log);
            Assert.AreEqual(MoveStatus.NoRoom, Goods.Move(s, ev, Holder.Carry(a.Id), carried.Id, 1, Holder.Bin(bin), MoveReason.Deposit, a.Id).Status, "a full bin refuses (cap 4, one log of weight 4 in it)");
            b.Node = FarNode(s, a.Node);
            Assert.AreEqual(MoveStatus.OutOfReach, Goods.Move(s, ev, Holder.Bin(bin), lot, 1, Holder.Carry(a.Id), MoveReason.Withdraw, a.Id).Status);
        }

        private static int FarNode(GodState s, int from)
        {
            var g = s.Territory.Grid;
            for (int v = 0; v < g.Count; v++) if (g.Distance(from, v) > 4) return v;
            throw new InvalidOperationException("no far node");
        }

        [Test]
        public void Unified_Origins()
        {
            var sim = Game(9, true);
            var s = sim.State; var ev = new EventBuffer();
            var a = People(s)[0]; Free(s, a);
            int n0 = s.Unification.Origins.Count;
            int lot = Goods.Produce(s, ev, Holder.Carry(a.Id), Ware.Herbs, 3, -1, 0, Owner.None, ProduceReason.Collected, a.Id, 77);
            Assert.AreEqual(n0 + 1, s.Unification.Origins.Count);
            Assert.IsTrue(UnifiedGoods.OriginOf(s, lot, out var o));
            Assert.AreEqual(a.Id, o.Producer); Assert.AreEqual((byte)ProduceReason.Collected, o.Reason); Assert.AreEqual(77, o.SourceId); Assert.IsFalse(o.Mixed);
            UnifiedGoods.SetSource(s, lot, OriginSource.RawNode, 5);
            Assert.IsTrue(UnifiedGoods.OriginOf(s, lot, out o)); Assert.AreEqual(OriginSource.RawNode, o.SourceKind); Assert.AreEqual(5, o.SourceId);
            ulong h0 = sim.StateHash();
            var r = Goods.Move(s, ev, Holder.Carry(a.Id), lot, 1, Holder.Ground(a.Node), MoveReason.Drop, a.Id);
            Assert.AreNotEqual(lot, r.Lot, "a split");
            Assert.IsTrue(UnifiedGoods.OriginOf(s, r.IntoLot, out var o2)); Assert.AreEqual(a.Id, o2.Producer, "the split part keeps the origin");
            Assert.AreNotEqual(h0, sim.StateHash(), "origins are hashed");
            Assert.IsTrue(Goods.Consume(s, ev, Holder.Carry(a.Id), lot, 2, ConsumeReason.ProcessInput, a.Id));
            Assert.IsFalse(UnifiedGoods.OriginOf(s, lot, out _), "removed when the lot ends");
            var ids = s.Unification.Origins.Select(x => x.Lot).ToList();
            CollectionAssert.AreEqual(ids.OrderBy(x => x).ToList(), ids, "sorted by lot id");
            Assert.IsEmpty(SimInvariants.Check(s));
        }

        // ---------------------------------------------------------------- invariants (23.11)

        [Test]
        public void Unified_Invariants_UC1_UC6()
        {
            var sim = Game(10, true);
            var s = sim.State;
            for (int i = 0; i < 4; i++) { sim.Step(600); Assert.IsEmpty(SimInvariants.Check(s), "stub run, tick " + s.Tick); }
            var a = People(s)[0];
            var ev = new EventBuffer(); Free(s, a);
            int lot = Goods.Produce(s, ev, Holder.Carry(a.Id), Ware.Stone, 1, -1, 0, Owner.None, ProduceReason.Collected, a.Id, 0);
            Assert.IsEmpty(SimInvariants.Check(s));
            int slot = Array.FindIndex(a.Carry, x => x.Id == lot);
            // (a) a lot deleted without an event
            var keep = a.Carry[slot]; a.Carry[slot] = default;
            Assert.IsTrue(SimInvariants.Check(s).Any(x => x.StartsWith("U-C4")), "a vanished lot");
            a.Carry[slot] = keep;
            // (b) an owner
            var owned = keep; owned.Owner = Owner.Npc(a.Id); a.Carry[slot] = owned;
            Assert.IsTrue(SimInvariants.Check(s).Any(x => x.StartsWith("U-C5")), "an owner");
            a.Carry[slot] = keep;
            Assert.IsEmpty(SimInvariants.Check(s));
            // (c) an over-full bin
            int bin = Goods.NewBin(s, BinKind.Storage, Owner.None, a.Node, 4);
            Goods.Produce(s, ev, Holder.Bin(bin), Ware.Stone, 1, -1, 0, Owner.None, ProduceReason.Placed, 0, 0);
            var bb = Goods.BinById(s.Population, bin); bb.WeightCap = 2;
            Assert.IsTrue(SimInvariants.Check(s).Any(x => x.StartsWith("C5")), "an over-full bin");
            bb.WeightCap = 4;
            // (d) a dead holder still holding
            a.Alive = false; a.DiedTick = s.Tick; a.DeathCause = DeathCause.Violence; s.Population.Stats.Deaths++; s.Population.Stats.DeathsByCause[(int)DeathCause.Violence]++;
            Assert.IsTrue(SimInvariants.Check(s).Any(x => x.Contains("still carries")), "a dead holder");
            a.Alive = true; a.DeathCause = DeathCause.None; s.Population.Stats.Deaths--; s.Population.Stats.DeathsByCause[(int)DeathCause.Violence]--;
            Assert.IsEmpty(SimInvariants.Check(s));
            // (e) an over-full site bin
            int siteBin = Goods.NewBin(s, BinKind.Site, Owner.None, a.Node, 4);
            s.Unification.Sites.Add(new ConstructionSite { Id = s.Unification.NextSiteId++, Node = a.Node, Bin = siteBin });
            Assert.IsEmpty(SimInvariants.Check(s));
            Goods.Produce(s, ev, Holder.Bin(siteBin), Ware.Stone, 1, -1, 0, Owner.None, ProduceReason.Placed, 0, 0);
            Goods.BinById(s.Population, siteBin).WeightCap = 2;
            Assert.IsTrue(SimInvariants.Check(s).Any(x => x.StartsWith("U-C6")), "an over-full site");
            Goods.BinById(s.Population, siteBin).WeightCap = 4;
            // (f) an expired reservation left open
            s.Unification.Buildings.Add(new UBuilding { Id = s.Unification.NextBuildingId++, Node = a.Node });
            s.Unification.Reservations.Add(new Reservation { Building = 1, Npc = a.Id, Units = 1, Expires = s.Tick + 10 });
            Assert.IsEmpty(SimInvariants.Check(s));
            var rv = s.Unification.Reservations[0]; rv.Expires = s.Tick - 1; s.Unification.Reservations[0] = rv;
            Assert.IsTrue(SimInvariants.Check(s).Any(x => x.StartsWith("U-C6")), "an expired reservation");
        }

        // ---------------------------------------------------------------- ids and hash layouts (23.4, 23.14)

        [Test]
        public void Wares_LayoutNeutral()
        {
            Assert.AreEqual(160, Wares.PersonalCount); Assert.AreEqual(32, Wares.LegacyPersonalCount); Assert.AreEqual(40, Wares.F1PersonalCount); Assert.AreEqual(28, Wares.Count);
            Assert.AreEqual(1, (int)Ware.Log); Assert.AreEqual(20, (int)Ware.Axe); Assert.AreEqual(27, (int)Ware.LightArmour); Assert.AreEqual(31, (int)Ware.Nightshade); Assert.AreEqual(32, (int)Ware.Herbs);
            Assert.AreEqual(72, (int)Ware.Mushroom); Assert.AreEqual(79, (int)Ware.Sheep); Assert.AreEqual(93, (int)Ware.SleepingMat);
            Assert.IsFalse(Enum.GetValues(typeof(Ware)).Cast<int>().Any(w => w >= 33 && w <= 71), "33-71 stay free (industry 40-71)");
            var unique = new[] { Ware.Axe, Ware.Sheep, Ware.Bed, Ware.Stool, Ware.Bench, Ware.Toy, Ware.CarvedOrnament, Ware.StorageChest, Ware.Cradle, Ware.Latrine, Ware.StoneHammer, Ware.SleepingMat };
            foreach (Ware w in Enum.GetValues(typeof(Ware)))
            {
                Assert.AreEqual(unique.Contains(w), Wares.IsUnique(w), w.ToString());
                if ((int)w >= 72) { Assert.AreNotEqual("none", Wares.Name(w), w.ToString()); Assert.Less((int)w, Wares.PersonalCount); }
            }
            Assert.AreEqual(10, (int)ProduceReason.Collected); Assert.AreEqual(11, (int)ProduceReason.Drawn); Assert.AreEqual(12, (int)ProduceReason.Excreted);
            Assert.AreEqual(8, (int)ConsumeReason.ProcessInput); Assert.AreEqual(14, (int)ConsumeReason.Drunk); Assert.AreEqual(15, (int)ConsumeReason.Washed);
            Assert.AreEqual(16, GoodsLedger.ProduceReasons); Assert.AreEqual(16, GoodsLedger.ConsumeReasons);
            Assert.AreEqual(7, (int)BinKind.Storage); Assert.AreEqual(12, (int)BinKind.Container);
            Assert.AreEqual(14, (int)MoveReason.Contribute); Assert.AreEqual(18, (int)MoveReason.Grab);
            Assert.AreEqual(130, God.Sim.Learning.ObjClass.RawNode); Assert.AreEqual(137, God.Sim.Learning.ObjClass.Road);
            Assert.AreEqual(28, (int)God.Sim.Comms.GestureAct.Claim);
            Assert.AreEqual(5, (int)ControlInputKind.Primitive);
            Assert.AreEqual(1044, CatId.Object(44)); Assert.AreEqual(2012, CatId.Building(12)); Assert.AreEqual("OBJ-044", CatId.Code(1044)); Assert.AreEqual("BLD-012", CatId.Code(2012));
        }

        [Test]
        public void Unification_FoundationOnly_Neutral_LedgerLayout()
        {
            var sim = Game(11, false);
            var s = sim.State; var l = s.Population.Ledger;
            Assert.AreEqual(16 * 160, l.Produced.Length);
            ulong h0 = PopulationModule.HashPopulation(s);
            foreach (var (reason, ware) in new[] { (0, 40), (3, 72), (7, 159), (8, 1), (10, 72), (15, 0) })
            {
                l.Produced[reason * Wares.PersonalCount + ware] = 3;
                Assert.AreNotEqual(h0, PopulationModule.HashPopulation(s), "a non-zero slot (" + reason + ", " + ware + ") enters the part");
                l.Produced[reason * Wares.PersonalCount + ware] = 0;
                Assert.AreEqual(h0, PopulationModule.HashPopulation(s), "all zero again: the old layout exactly");
                l.Consumed[reason * Wares.PersonalCount + ware] = 2;
                Assert.AreNotEqual(h0, PopulationModule.HashPopulation(s));
                l.Consumed[reason * Wares.PersonalCount + ware] = 0;
                Assert.AreEqual(h0, PopulationModule.HashPopulation(s));
            }
            // the miracle ledger keeps its F1 layout too
            var m = new God.Sim.Divinity.MiracleLedger(s.FactionCount);
            var hm = new StateHasher(); m.AddTo(ref hm);
            var legacy = new StateHasher(); legacy.Add((s.FactionCount + 1) * 40); for (int i = 0; i < (s.FactionCount + 1) * 40; i++) { legacy.Add(0); legacy.Add(0); }
            Assert.AreEqual(legacy.Value, hm.Value, "an empty miracle ledger hashes exactly as at 40 wares");
            m.Created[1 * Wares.PersonalCount + 72] = 1;
            var hm2 = new StateHasher(); m.AddTo(ref hm2);
            Assert.AreNotEqual(hm.Value, hm2.Value);
        }

        [Test]
        public void Ledger_WideningNeutral_UnifiedReasons()
        {
            var sim = Game(12, true);
            var s = sim.State; var ev = new EventBuffer();
            var a = People(s)[0]; Free(s, a);
            int lot = Goods.Produce(s, ev, Holder.Carry(a.Id), Ware.Berries, 4, -1, 0, Owner.None, ProduceReason.Collected, a.Id, 0);
            Assert.IsTrue(Goods.Consume(s, ev, Holder.Carry(a.Id), lot, 1, ConsumeReason.ProcessInput, a.Id));
            Assert.IsTrue(Goods.Consume(s, ev, Holder.Carry(a.Id), lot, 1, ConsumeReason.Drunk, a.Id));
            Assert.IsTrue(Goods.Consume(s, ev, Holder.Carry(a.Id), lot, 1, ConsumeReason.Washed, a.Id));
            int w = Goods.Produce(s, ev, Holder.Carry(a.Id), Ware.Waste, 1, -1, 0, Owner.None, ProduceReason.Excreted, a.Id, 0);
            Assert.Greater(w, 0);
            Assert.AreEqual(4, s.Population.Ledger.ProducedBy(ProduceReason.Collected, Ware.Berries));
            Assert.AreEqual(1, s.Population.Ledger.ConsumedBy(ConsumeReason.Washed, Ware.Berries));
            Assert.IsEmpty(SimInvariants.Check(s), "U-C1 over 16 reasons and 160 wares");
        }

        [Test]
        public void Loaded_WidensF1Ledgers()
        {
            var sim = Game(13, false);
            var s = sim.State;
            var l = s.Population.Ledger;
            l.Produced[2 * Wares.PersonalCount + 1] = 7; l.Consumed[5 * Wares.PersonalCount + 32] = 4;
            ulong h = PopulationModule.HashPopulation(s);
            var p = l.Produced; var c = l.Consumed;
            long[] Narrow(long[] a) { var b = new long[8 * 40]; for (int r = 0; r < 8; r++) for (int w = 0; w < 40; w++) b[r * 40 + w] = a[r * Wares.PersonalCount + w]; return b; }
            l.Produced = Narrow(p); l.Consumed = Narrow(c);
            UnifiedModule.Loaded(s);
            CollectionAssert.AreEqual(p, l.Produced); CollectionAssert.AreEqual(c, l.Consumed);
            Assert.AreEqual(h, PopulationModule.HashPopulation(s));
        }

        // ---------------------------------------------------------------- schemas (23.6)

        internal const string ObsHash = "81bef353bb749321af0048d97c60a3041f1d3e285e1098b2b423a421c37b95c4";
        internal const string ActHash = "797154328f0bcb15d9b43cb8ff4bacf7d3eef71b9ef2a0971b71a1c418e23c81";

        [Test]
        public void Schema_HashStable()
        {
            TestContext.WriteLine("Obs v1 " + ObsSchema.Length + " " + ObsSchema.SchemaHash + "\nAct v1 " + ActSchema.Length + " " + ActSchema.SchemaHash);
            Assert.AreEqual(284, ObsSchema.Length);
            Assert.AreEqual(1, ObsSchema.Version); Assert.AreEqual(1, ActSchema.Version); Assert.AreEqual(1000, ObsSchema.Scale);
            Assert.AreEqual(ObsHash, ObsSchema.SchemaHash, "Obs v1 layout changed: a change needs a new version and invalidates artifacts");
            Assert.AreEqual(ActHash, ActSchema.SchemaHash, "Act v1 layout changed: a change needs a new version and invalidates artifacts");
            Assert.AreEqual(ObsSchema.SchemaHash, SchemaText.Sha256(SchemaText.Canonical("Obs", 1, 1000, ObsSchema.Fields)), "deterministic");
            var moved = ObsSchema.Fields.ToList(); var x = moved[0]; moved[0] = moved[1]; moved[1] = x;
            Assert.AreNotEqual(ObsSchema.SchemaHash, SchemaText.Sha256(SchemaText.Canonical("Obs", 1, 1000, new ReadOnlyArray<SchemaField>(moved.ToArray()))), "an order change changes the hash");
            Assert.AreEqual(0, ObsSchema.OffsetOf("facing")); Assert.AreEqual(6, ObsSchema.OffsetOf("control")); Assert.AreEqual(12, ObsSchema.OffsetOf("needs"));
            Assert.AreEqual(13, ActSchema.Heads[0].Length); Assert.AreEqual(166, ActSchema.Length);
        }

        // ---------------------------------------------------------------- effect registry (23.7)

        private sealed class Counting : IEffectHandler
        {
            public readonly int[] Calls = new int[2];
            public MotorStatus Validate(GodState s, in EffectCall c) { Calls[0]++; return MotorStatus.Accepted; }
            public MotorStatus Apply(GodState s, EventBuffer ev, in EffectCall c) { Calls[1]++; return MotorStatus.Accepted; }
        }

        [Test]
        public void Effect_RegistryDispatch()
        {
            var sim = Game(14, true);
            var s = sim.State;
            var r = new EffectRegistrar();
            var h = new Counting();
            r.Register(EffectKind.Eat, h);
            Assert.Throws<InvalidOperationException>(() => r.Register(EffectKind.Eat, new Counting()), "a kind registers once");
            var t = r.Freeze();
            var call = new EffectCall { Actor = People(s)[0].Id, Effect = new EffectDef(EffectKind.Eat) };
            Assert.AreEqual(MotorStatus.Accepted, t.Validate(s, in call)); Assert.AreEqual(MotorStatus.Accepted, t.Apply(s, null, in call));
            CollectionAssert.AreEqual(new[] { 1, 1 }, h.Calls, "Validate at start, Apply at completion");
            var other = new EffectCall { Effect = new EffectDef(EffectKind.Forage) };
            Assert.AreEqual(MotorStatus.Disabled, t.Validate(s, in other), "an unregistered kind");
            s.Tuning.Unification.Interactions = 0;
            Assert.AreEqual(MotorStatus.Disabled, t.Validate(s, in call)); Assert.AreEqual(MotorStatus.Disabled, t.Apply(s, null, in call));
            CollectionAssert.AreEqual(new[] { 1, 1 }, h.Calls, "a sub-switch at 0 refuses before the handler runs");
            s.Tuning.Unification.Interactions = 1;
            Assert.AreEqual(MotorStatus.Disabled, t.Validate(Game(14, false).State, in call), "nothing applies outside a unified game");
            for (int k = 1; k <= 20; k++) Assert.IsNotNull(EffectRegistry.OwnerOf((EffectKind)k), "owner of kind " + k);
            Assert.IsNull(EffectRegistry.OwnerOf((EffectKind)21));
            Assert.AreEqual(MotorStatus.Disabled, EffectRegistry.Validate(s, in call), "the game's table is empty at UN0");
            Assert.IsTrue(Catalogue.IsEmpty, "the catalogue is empty at UN0");
        }

        // ---------------------------------------------------------------- telemetry (23.12)

        [Test]
        public void Telemetry_UnifiedRangesNoCollision()
        {
            foreach (var d in new[] { TelemetryDomain.UnifiedWorld, TelemetryDomain.Motor, TelemetryDomain.Construction, TelemetryDomain.Interaction, TelemetryDomain.Exchange,
                TelemetryDomain.Conflict, TelemetryDomain.Brain, TelemetryDomain.Settlement, TelemetryDomain.Groups })
                Assert.AreEqual((1200, 1399), EventCatalog.RangeOf(d, false), d.ToString());
            Assert.AreEqual(80, (int)TelemetryDomain.UnifiedWorld); Assert.AreEqual(88, (int)TelemetryDomain.Groups);
            Assert.AreEqual((800, 899), EventCatalog.RangeOf(TelemetryDomain.Divine, false)); Assert.AreEqual((900, 999), EventCatalog.RangeOf(TelemetryDomain.Lab, false));
            Assert.AreEqual((100, 199), EventCatalog.RangeOf(TelemetryDomain.Population, false)); Assert.AreEqual((1, 99), EventCatalog.RangeOf(TelemetryDomain.Population, true));
            Assert.AreEqual(0, UnifiedEvents.Catalog.Count, "the owners' arrays are empty at UN0");
            foreach (var e in EventCatalog.All) Assert.IsTrue(e.Code < 1200 || e.Code > 1399, "no event in the unified block yet: " + e.Name);
            Assert.AreEqual(EventCatalog.All.Count, EventCatalog.All.Select(e => e.Code).Distinct().Count(), "codes unique");
            foreach (var pkg in new[] { "UN1", "UN5", "UN2", "UN4", "UN3b", "UN7", "UN8a", "UN6", "UN6t", "UB1", "UN8b" })
            {
                var (lo, hi) = UnifiedEvents.BlockOf(pkg);
                Assert.IsTrue(lo >= 1200 && hi <= 1379 && lo <= hi, pkg);
            }
        }

        // ---------------------------------------------------------------- save (23.14)

        [Test]
        public void Unified_SaveLoadNullAccept_StateRoundTrip()
        {
            var off = Game(15, false); off.Step(50);
            var offJson = SaveFile.From(off).ToJson();
            var offLoaded = SaveFile.FromJson(offJson).ToSim();
            Assert.IsNull(offLoaded.State.Unification);
            Assert.AreEqual(off.StateHash(), offLoaded.StateHash());
            Assert.AreEqual(6, SaveFile.CurrentVersion, "the save version is the lead's");
            var on = Game(15, true); on.Step(50);
            var s = on.State;
            var a = People(s)[0];
            Goods.Produce(s, new EventBuffer(), Holder.Carry(a.Id), Ware.Stone, 2, -1, 0, Owner.None, ProduceReason.Collected, a.Id, 0);
            s.Unification.Sites.Add(new ConstructionSite { Id = s.Unification.NextSiteId++, Design = CatId.Building(1), Node = a.Node, Founder = a.Id, Placed = s.Tick,
                Contributors = { new Contribution { Npc = a.Id, Labour = 30, Last = s.Tick } } });
            var rec = s.Unification.PersonOf(a.Id); rec.Facing = 3; rec.Needs[0] = 250; Knowledge.Learn(rec.Brain, KnowledgeKind.Design, 7);
            var loaded = SaveFile.FromJson(SaveFile.From(on).ToJson()).ToSim();
            Assert.AreEqual(on.StateHash(), loaded.StateHash(), "a populated unified state round-trips");
            Assert.IsTrue(Knowledge.Knows(loaded.State.Unification.PersonOf(a.Id).Brain, KnowledgeKind.Design, 7));
            on.Step(200); loaded.Step(200);
            Assert.AreEqual(on.StateHash(), loaded.StateHash(), "and continues equal");
        }

        // ---------------------------------------------------------------- the Primitive control seam (R84)

        [Test]
        public void Primitive_ControlInputKind_Routing()
        {
            var handler = (ICommandHandler)new DivineControlInputHandler();
            foreach (bool unified in new[] { false, true })
            {
                var sim = Game(16, unified, divinity: 1);
                var s = sim.State; var ev = new EventBuffer();
                var body = People(s)[0];
                Assert.IsTrue(Control.TryAcquire(s, ev, new ControlEntry { Kind = ControlKind.Possessed, God = 1, Body = body.Id, Intervention = 1 }, out var why), why);
                var prim = new DivineControlInput { God = 1, Body = body.Id, Kind = ControlInputKind.Primitive, Primitive = new PrimitiveControl { Control = Prim.TurnLeft, Actor = body.Id } };
                var heading = new DivineControlInput { God = 1, Body = body.Id, Kind = ControlInputKind.Stop };
                var rp = handler.Validate(s, prim); var rh = handler.Validate(s, heading);
                Assert.IsFalse(rp.Ok, "no motor at UN0 (unified) / no unified game (legacy): " + rp.Reason);
                Assert.AreEqual(Reasons.Disabled, rp.Reason);
                if (unified) { Assert.IsFalse(rh.Ok, "legacy kinds are off in a unified game"); Assert.AreEqual(Reasons.Disabled, rh.Reason); }
                else Assert.IsTrue(rh.Ok, "legacy kinds unchanged in a legacy game");
                Assert.AreEqual(unified ? MotorStatus.Disabled : MotorStatus.Disabled, UnifiedHooks.CheckPossessed(s, body.Id, prim.Primitive.Value));
                Assert.AreEqual(MotorStatus.Disabled, UnifiedHooks.SubmitPossessed(s, ev, body.Id, ControlKind.Possessed, prim.Primitive.Value), "the seam answers Disabled until UN2");
            }
            // a command without the primitive writes no Primitive field (pending hash of older commands unchanged)
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(new DivineControlInput { God = 1, Kind = ControlInputKind.Stop }, SaveFile.Settings);
            Assert.IsFalse(json.Contains("Primitive"), json);
        }

        // ---------------------------------------------------------------- stub run (UN0-T17, engine side)

        [Test]
        public void Unified_StubRun_Deterministic_NoTitleEvents()
        {
            ulong[] Run(out List<SimEvent> events)
            {
                var sim = Game(17, true, divinity: 1, npcs: 6);
                events = new List<SimEvent>();
                var hashes = new List<ulong>();
                for (int i = 0; i < 8; i++)
                {
                    sim.Step(300);
                    sim.DrainInto(events);
                    hashes.Add(sim.StateHash());
                    Assert.IsEmpty(SimInvariants.Check(sim.State), "invariants at " + sim.State.Tick);
                }
                return hashes.ToArray();
            }
            var h1 = Run(out var ev1); var h2 = Run(out _);
            CollectionAssert.AreEqual(h1, h2, "deterministic");
            AssertNoTitleEvents(ev1);
        }
    }
}
