using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using God.Sim.Cognition;
using God.Sim.Learning;
using God.Sim.Population;
using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>
    /// F0 (SHARED_CONTRACTS 2): the goods API. Produce, move, split, merge, own, consume and bins, each atomic; ground lots
    /// ownerless; interaction ranges enforced (D3, A1); the permission query (A8); the conservation invariants C1-C6; and the
    /// static rule that only <see cref="Goods"/> writes lots, carries and owners.
    /// </summary>
    public class GoodsTests
    {
        private static (GodSim sim, int glade, Npc a, Npc b) World()
        {
            var sim = LearningFixtures.Empty(3);
            int glade = LearningFixtures.Glade(sim);
            var a = LearningFixtures.Npc(sim, glade, InstinctSet.Default, LearningMode.On);
            var b = LearningFixtures.Npc(sim, LearningFixtures.Step(sim, glade, 0, 1), InstinctSet.Default, LearningMode.On);
            return (sim, glade, a, b);
        }

        private static List<SimEvent> Drain(EventBuffer ev) => ev.Drain().ToList();

        [Test]
        public void Produce_NewIdAlways_MergesSameWareAndOwner_BooksTheLedger_EmitsProducedV2()
        {
            var (sim, glade, a, _) = World();
            var s = sim.State; var ev = new EventBuffer();
            int next = s.Population.NextLotId;
            int l1 = Goods.Produce(s, ev, Holder.Carry(a.Id), Ware.Berries, 2, 5, 0, Owner.Npc(a.Id), ProduceReason.Foraged, a.Id, 0);
            int l2 = Goods.Produce(s, ev, Holder.Carry(a.Id), Ware.Berries, 1, 3, 0, Owner.Npc(a.Id), ProduceReason.Foraged, a.Id, 0);
            Assert.AreEqual(next, l1);
            Assert.AreEqual(l1, l2, "the same ware and owner merge into the lower id");
            Assert.AreEqual(next + 2, s.Population.NextLotId, "every produce takes a new id, merged or not");
            var lot = a.Carry.Single(c => !c.Empty);
            Assert.AreEqual(3, lot.Count); Assert.AreEqual(3, lot.SpoilDay, "the earlier spoil day"); Assert.AreEqual(OwnerKind.Npc, lot.Owner.Kind);
            var pr = Drain(ev).OfType<Produced>().ToList();
            Assert.AreEqual(2, pr.Count);
            Assert.AreEqual(next + 1, pr[1].Lot); Assert.AreEqual(next, pr[1].IntoLot); Assert.AreEqual(OwnerKind.Npc, pr[1].OwnerKind); Assert.AreEqual(a.Id, pr[1].OwnerId);
            Assert.AreEqual(3, s.Population.Ledger.ProducedOf(Ware.Berries));
            // ground: always ownerless, whatever the caller asks
            int g = Goods.Produce(s, ev, Holder.Ground(glade), Ware.Roots, 2, 5, 0, Owner.Npc(a.Id), ProduceReason.Placed, 0, 0);
            Assert.IsTrue(s.Population.Lots.Single(x => x.Lot.Id == g).Lot.Owner.IsNone);
            Assert.IsEmpty(SimInvariants.Check(s));
        }

        [Test]
        public void Move_PartialSplitsWithANewId_OwnerRulesApply_AndEmitsOneItemMovedAndOneOwnershipChanged()
        {
            var (sim, glade, a, _) = World();
            var s = sim.State; var ev = new EventBuffer();
            int lot = Goods.Produce(s, null, Holder.Ground(a.Node), Ware.Fish, 5, 9, 0, Owner.None, ProduceReason.Placed, 0, 0);
            int next = s.Population.NextLotId;
            var r = Goods.Move(s, ev, Holder.Ground(a.Node), lot, 2, Holder.Carry(a.Id), MoveReason.PickUp, a.Id, OwnerRule.ToReceiver);
            Assert.AreEqual(MoveStatus.Ok, r.Status);
            Assert.AreEqual(next, r.Lot, "the moved part gets the new id"); Assert.AreEqual(lot, r.ParentLot); Assert.AreEqual(r.Lot, r.IntoLot); Assert.AreEqual(0, r.EndedLot);
            Assert.AreEqual(3, s.Population.Lots.Single(x => x.Lot.Id == lot).Lot.Count, "the rest keeps its id");
            Assert.AreEqual(Owner.Npc(a.Id).Id, a.Carry.Single(c => !c.Empty).Owner.Id);
            var e = Drain(ev);
            Assert.AreEqual(1, e.OfType<ItemMoved>().Count()); Assert.AreEqual(1, e.OfType<OwnershipChanged>().Count());
            Assert.AreEqual(OwnershipBasis.FirstArrival, e.OfType<OwnershipChanged>().Single().Basis);
            var m = e.OfType<ItemMoved>().Single();
            Assert.AreEqual(a.Node, m.ActorNode); Assert.AreEqual(a.Node, m.FromNode);
            // the rest, taken too, merges into the carried lot: the moved id ends (C3: merged)
            r = Goods.Move(s, ev, Holder.Ground(a.Node), lot, 3, Holder.Carry(a.Id), MoveReason.PickUp, a.Id, OwnerRule.ToReceiver);
            Assert.AreEqual(MoveStatus.Ok, r.Status);
            Assert.AreEqual(lot, r.IntoLot, "the lower id survives"); Assert.AreEqual(next, r.EndedLot);
            Assert.AreEqual(5, a.Carry.Single(c => !c.Empty).Count);
            // dropping makes it ownerless
            r = Goods.Move(s, ev, Holder.Carry(a.Id), lot, 5, Holder.Ground(a.Node), MoveReason.Drop, a.Id);
            Assert.IsTrue(r.Ok); Assert.IsTrue(r.After.IsNone);
            Assert.IsEmpty(SimInvariants.Check(s));
        }

        [Test]
        public void Move_IsAtomic_EveryRefusalChangesNothingAndEmitsNothing()
        {
            var (sim, glade, a, b) = World();
            var s = sim.State; var ev = new EventBuffer();
            int lot = Goods.Produce(s, null, Holder.Carry(a.Id), Ware.Roots, 2, 9, 0, Owner.Npc(a.Id), ProduceReason.Foraged, a.Id, 0);
            Assert.AreNotEqual(0, lot);
            int far = LearningFixtures.Step(sim, a.Node, 3, 4);
            ulong before = GodSim.StateHashOf(s);
            Assert.AreEqual(MoveStatus.NotEnough, Goods.Move(s, ev, Holder.Carry(a.Id), lot, 3, Holder.Ground(a.Node), MoveReason.Drop, a.Id).Status);
            Assert.AreEqual(MoveStatus.NoSuchLot, Goods.Move(s, ev, Holder.Carry(b.Id), lot, 1, Holder.Ground(b.Node), MoveReason.Drop, b.Id).Status);
            Assert.AreEqual(MoveStatus.SameHolder, Goods.Move(s, ev, Holder.Carry(a.Id), lot, 1, Holder.Carry(a.Id), MoveReason.HandOver, a.Id).Status);
            Assert.AreEqual(MoveStatus.OutOfReach, Goods.Move(s, ev, Holder.Carry(a.Id), lot, 1, Holder.Ground(far), MoveReason.Drop, a.Id).Status, "no drop at a distance (Reach.Enter)");
            // a full carry: no room
            Assert.AreNotEqual(0, Goods.Produce(s, null, Holder.Carry(b.Id), Ware.Berries, s.Tuning.Population.CarryWeight, 9, 0, Owner.Npc(b.Id), ProduceReason.Placed, b.Id, 0));
            before = GodSim.StateHashOf(s);
            Assert.AreEqual(MoveStatus.NoRoom, Goods.Move(s, ev, Holder.Carry(a.Id), lot, 2, Holder.Carry(b.Id), MoveReason.HandOver, a.Id, OwnerRule.ToReceiver).Status);
            Assert.AreEqual(before, GodSim.StateHashOf(s), "nothing changed");
            Assert.IsEmpty(Drain(ev), "nothing emitted");
            Assert.IsFalse(Goods.Consume(s, ev, Holder.Carry(a.Id), lot, 3, ConsumeReason.Built, a.Id));
        }

        [Test]
        public void Reach_ATakeNeedsTouch_AHandOverNeedsAdjacency_NothingAtADistance()
        {
            var (sim, glade, a, b) = World();
            var s = sim.State; var ev = new EventBuffer();
            Assert.AreEqual(0, Reach.Enter); Assert.AreEqual(1, Reach.Touch); Assert.AreEqual(2, Reach.Gather); Assert.AreEqual(1, Reach.HandOver); Assert.AreEqual(1, Reach.Store);
            Assert.AreEqual(Reach.HandOver, Goods.HandOverReach);
            int two = LearningFixtures.Step(sim, a.Node, 3, 2);
            int lot = Goods.Produce(s, null, Holder.Ground(two), Ware.Berries, 1, 9, 0, Owner.None, ProduceReason.Placed, 0, 0);
            Assert.AreEqual(MoveStatus.OutOfReach, Goods.Move(s, ev, Holder.Ground(two), lot, 1, Holder.Carry(a.Id), MoveReason.PickUp, a.Id, OwnerRule.ToReceiver).Status, "seen, two nodes away: not taken");
            a.Node = LearningFixtures.Step(sim, two, 0, 1);
            Assert.AreEqual(MoveStatus.Ok, Goods.Move(s, ev, Holder.Ground(two), lot, 1, Holder.Carry(a.Id), MoveReason.PickUp, a.Id, OwnerRule.ToReceiver).Status, "within touch");
            // hand-over: adjacent only
            b.Node = LearningFixtures.Step(sim, a.Node, 0, 3);
            Assert.AreEqual(MoveStatus.OutOfReach, Goods.Move(s, ev, Holder.Carry(a.Id), lot, 1, Holder.Carry(b.Id), MoveReason.HandOver, a.Id, OwnerRule.ToReceiver).Status);
            b.Node = LearningFixtures.Step(sim, a.Node, 0, 1);
            var r = Goods.Move(s, ev, Holder.Carry(a.Id), lot, 1, Holder.Carry(b.Id), MoveReason.HandOver, a.Id, OwnerRule.ToReceiver);
            Assert.IsTrue(r.Ok); Assert.AreEqual(b.Id, r.After.Id);
            var t = new GoodsTrace(); t.AddRange(Drain(ev));
            Assert.IsEmpty(t.CheckC6(s.Territory.Grid));
            Assert.IsTrue(Goods.IsEquipped(new ItemLot { Id = 1, Ware = Ware.Axe, Count = 1 }), "a carried tool is equipped (no flag in M3)");
        }

        [Test]
        public void Bins_DepositTakesTheBinOwner_WithdrawKeepsIt_EstateNeverMerges_RemoveOnlyWhenEmpty()
        {
            var (sim, glade, a, _) = World();
            var s = sim.State; var ev = new EventBuffer();
            int store = Goods.NewBin(s, BinKind.HouseholdStore, Owner.Household(7), a.Node, 0);
            int estate = Goods.NewBin(s, BinKind.Estate, Owner.Estate(a.Id), a.Node, 0);
            Assert.AreEqual(1, store); Assert.AreEqual(2, estate); Assert.AreEqual(3, s.Population.NextBinId);
            int l1 = Goods.Produce(s, null, Holder.Carry(a.Id), Ware.Roots, 4, 9, 0, Owner.Npc(a.Id), ProduceReason.Foraged, a.Id, 0);
            var r = Goods.Move(s, ev, Holder.Carry(a.Id), l1, 2, Holder.Bin(store), MoveReason.Deposit, a.Id, OwnerRule.ToBinOwner);
            Assert.IsTrue(r.Ok); Assert.AreEqual(OwnerKind.Household, r.After.Kind);
            Assert.AreEqual(OwnershipBasis.Deposited, Drain(ev).OfType<OwnershipChanged>().Single().Basis);
            r = Goods.Move(s, ev, Holder.Bin(store), r.Lot, 1, Holder.Carry(a.Id), MoveReason.Withdraw, a.Id);
            Assert.IsTrue(r.Ok); Assert.AreEqual(OwnerKind.Household, r.After.Kind, "withdrawn household goods stay the household's");
            Assert.AreEqual(0, Drain(ev).OfType<OwnershipChanged>().Count());
            // estate: Never merges, Set owner
            int l2 = a.Carry.First(c => !c.Empty && c.Owner.Kind == OwnerKind.Npc).Id;
            Assert.IsTrue(Goods.Move(s, ev, Holder.Carry(a.Id), l2, 1, Holder.Bin(estate), MoveReason.EstateIn, 0, OwnerRule.Set, Owner.Estate(a.Id), MergeRule.Never).Ok);
            Assert.IsTrue(Goods.Move(s, ev, Holder.Carry(a.Id), l2, 1, Holder.Bin(estate), MoveReason.EstateIn, 0, OwnerRule.Set, Owner.Estate(a.Id), MergeRule.Never).Ok);
            Assert.AreEqual(2, Goods.BinById(s.Population, estate).Lots.Count, "never merged");
            Assert.IsTrue(Goods.SetOwner(s, ev, Holder.Bin(estate), Goods.BinById(s.Population, estate).Lots[0].Id, Owner.Npc(a.Id), OwnershipBasis.Inherited, 0), "ownership passes in place (A2)");
            Assert.Throws<InvalidOperationException>(() => Goods.RemoveBin(s, estate));
            Assert.IsEmpty(SimInvariants.Check(s));
        }

        [Test]
        public void Consume_SpoiledBooksTheOwnersLoss_EatenInPlaceHasNoLot()
        {
            var (sim, glade, a, _) = World();
            var s = sim.State; var ev = new EventBuffer();
            int l = Goods.Produce(s, null, Holder.Carry(a.Id), Ware.Berries, 2, 9, 0, Owner.Npc(a.Id), ProduceReason.Foraged, a.Id, 0);
            int acc = a.Mind.Store.RewardAcc;
            Assert.IsTrue(Goods.Consume(s, ev, Holder.Carry(a.Id), l, 2, ConsumeReason.Spoiled, a.Id));
            Assert.AreEqual(acc - GoodsWorth.Of(s, a, Ware.Berries) * 2, a.Mind.Store.RewardAcc, "no decision waits for the lot: a plain loss");
            Goods.ProduceInHand(s, ev, Ware.Fish, 1, ProduceReason.Foraged, a.Id, 0);
            Goods.ConsumeInHand(s, ev, Ware.Fish, 1, ConsumeReason.Eaten, a.Id);
            var e = Drain(ev);
            Assert.AreEqual(0, e.OfType<Produced>().Single().Lot); Assert.AreEqual(0, e.OfType<Consumed>().Last().Lot);
            Assert.IsEmpty(SimInvariants.Check(s));
        }

        [Test]
        public void Permits_Believed_IsOwnHouseholdOwnerlessOrNone()
        {
            var sc = new ThinkScratch();
            sc.Self.Id = 5; sc.Self.Household = 2;
            Assert.AreEqual(PermissionBasis.Own, Permits.Believed(sc, Owner.Npc(5)).Basis);
            Assert.AreEqual(PermissionBasis.Household, Permits.Believed(sc, Owner.Household(2)).Basis);
            Assert.AreEqual(PermissionBasis.Ownerless, Permits.Believed(sc, Owner.None).Basis);
            var r = Permits.Believed(sc, Owner.Npc(6));
            Assert.IsFalse(r.Allowed); Assert.AreEqual(PermissionBasis.None, r.Basis);
            Assert.IsFalse(Permits.Believed(sc, Owner.Household(3)).Allowed);
            Assert.IsFalse(Permits.Believed(sc, Owner.Estate(5)).Allowed);
        }

        [Test]
        public void Island_TenDays_GoodsAreConserved_C1ToC6_AndEveryLotIdIsAccountedFor()
        {
            var setup = new GameSetup(2, GameSetup.NoHuman, 2) { Population = true, StartNpcsPerFaction = 15, Victory = VictorySpec.SandboxOnly() };
            var sim = SimBuilder.Create(setup).Sim;
            var probe = new EventProbe(sim, traceGoods: true);
            for (int d = 0; d < 10; d++)
            {
                probe.Step(setup.TicksPerDay);
                Assert.IsEmpty(SimInvariants.Check(sim.State), "day " + d);
            }
            Assert.IsEmpty(probe.Trace.CheckC3(sim.State));
            Assert.IsEmpty(probe.Trace.CheckC6(sim.State.Territory.Grid));
            Assert.Greater(probe.Of<ItemMoved>().Count(), 0, "people picked up and set down goods");
            Assert.AreEqual(probe.Of<ItemMoved>().Count(), probe.Of<ItemMoved>().Select(m => (m.Tick, m.Seq)).Distinct().Count());
            // a consumed lot is followed from its production to its end
            var produced = new HashSet<int>(probe.Of<Produced>().Where(x => x.Lot != 0 && x.IntoLot == x.Lot).Select(x => x.Lot));
            var eaten = probe.Of<Consumed>().First(c => c.Lot != 0 && c.Reason == ConsumeReason.Eaten && produced.Contains(c.Lot));
            var chain = probe.Trace.Follow(eaten.Lot);
            Assert.IsInstanceOf<Produced>(chain[0], "the chain starts where the goods were produced");
            Assert.That(chain, Has.Some.SameAs(eaten));
        }

        // ---------------------------------------------------------------- the static rule (SHARED_CONTRACTS 2.2)

        /// <summary>Writes to lots, carries, the ground list, bins, owners and the lot counter, as regexes over source lines.</summary>
        public static readonly (string what, Regex re)[] WritePatterns =
        {
            ("carry slot", new Regex(@"\.Carry\[[^\]]*\]\s*(=(?!=)|\.\w+\s*(=(?!=)|\+=|-=|\+\+|--))")),
            ("carry slot (prefix)", new Regex(@"(\+\+|--)\s*[\w.]+\.Carry\[")),
            ("carry array", new Regex(@"\.Carry\s*=(?!=)")),
            ("ground or bin list", new Regex(@"\.Lots\.(Add|Insert|Remove|RemoveAt|RemoveAll|RemoveRange|Clear|Sort|AddRange)\s*\(")),
            ("ground or bin slot", new Regex(@"\.Lots\[[^\]]*\]\s*(=(?!=)|\.\w+\s*(=(?!=)|\+=|-=))")),
            ("ground lot", new Regex(@"\.Lot\.\w+\s*(=(?!=)|\+=|-=|\+\+|--)")),
            ("lot owner", new Regex(@"\.Owner\s*=(?!=)")),
            ("lot counter", new Regex(@"NextLotId\s*(=(?!=)|\+\+|--|\+=)|(\+\+|--)\s*\w+\.NextLotId")),
            ("bins", new Regex(@"\.Bins\.(Add|Insert|Remove|RemoveAt|Clear)\s*\(")),
        };

        /// <summary>
        /// Allowed writers: the goods API itself; in PopulationModule the id allocator (NewLot) and AddNpc's empty carry; the
        /// counter's declaration; and the think scratch's copy of the carry (not a holder).
        /// </summary>
        private static bool Allowed(string file, string what, string line) =>
            file.EndsWith("Goods.cs", StringComparison.Ordinal)
            || file.EndsWith("PopulationModule.cs", StringComparison.Ordinal) && (what == "lot counter" && line.Contains("new ItemLot { Id = s.Population.NextLotId++") || what == "carry array" && line.Contains("new ItemLot["))
            || what == "lot counter" && line.TrimStart().StartsWith("public int ", StringComparison.Ordinal)
            || what == "carry array" && line.Contains("_sc.Carry");

        public static string RepoRoot()
        {
            foreach (var start in new[] { Environment.CurrentDirectory, AppContext.BaseDirectory })
                for (var d = new DirectoryInfo(start); d != null; d = d.Parent)
                    if (Directory.Exists(Path.Combine(d.FullName, "Assets", "God", "Sim"))) return d.FullName;
            return null;
        }

        public static List<string> GoodsWrites(IEnumerable<(string file, string text)> sources)
        {
            var bad = new List<string>();
            foreach (var (file, text) in sources)
            {
                var lines = text.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    string l = lines[i];
                    string t = l.TrimStart();
                    if (t.StartsWith("//") || t.StartsWith("///") || t.StartsWith("*")) continue;
                    foreach (var (what, re) in WritePatterns)
                        if (re.IsMatch(l) && !Allowed(file, what, l)) bad.Add(Path.GetFileName(file) + ":" + (i + 1) + " writes " + what + ": " + t.Trim());
                }
            }
            return bad;
        }

        [Test]
        public void Static_OnlyGoodsWritesLotsCarriesAndOwners()
        {
            string root = RepoRoot();
            Assert.IsNotNull(root, "the repository root (Assets/God/Sim) was not found from " + Environment.CurrentDirectory);
            var files = new List<(string, string)>();
            foreach (var dir in new[] { "Sim", "Game", "Recording" })
            {
                var path = Path.Combine(root, "Assets", "God", dir);
                if (!Directory.Exists(path)) continue;
                foreach (var f in Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories).OrderBy(x => x, StringComparer.Ordinal)) files.Add((f, File.ReadAllText(f)));
            }
            Assert.Greater(files.Count, 100);
            var bad = GoodsWrites(files);
            Assert.IsEmpty(bad, "only Goods may write lots, carries, the ground, bins, owners and the lot counter:\n" + string.Join("\n", bad));
        }

        [Test]
        public void Static_TheScannerCatchesEachKindOfWrite()
        {
            var samples = new[]
            {
                "npc.Carry[i] = default;", "npc.Carry[slot].Count -= 1;", "--npc.Carry[axe].Uses;", "p.Lots.RemoveAt(i);", "g.Lot.Count += 2;",
                "lot.Owner = Owner.None;", "p.NextLotId++;", "s.Population.Bins.Add(b);", "npc.Carry = new ItemLot[4];",
            };
            foreach (var sample in samples)
                Assert.IsNotEmpty(GoodsWrites(new[] { ("Sample.cs", sample) }), sample);
            Assert.IsEmpty(GoodsWrites(new[] { ("Sample.cs", "if (npc.Carry[i].Count == 0) x = npc.Carry[i].Id;") }), "reads are fine");
        }
    }
}
