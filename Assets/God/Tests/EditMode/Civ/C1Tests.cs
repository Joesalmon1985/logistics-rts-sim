using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using God.Sim.Civ;
using God.Sim.Cognition;
using God.Sim.Learning;
using God.Sim.Policy;
using God.Sim.Population;
using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>
    /// C1 (docs/civdiv/DEVELOPMENT_PACKAGES/C1.md section 13): productive assets built from real materials, worked with
    /// real labour and inputs (output owned by the asset's owner), hauled to, believed owned from perception, and theft
    /// classified by the sim's truth (never judged). Each act runs through the same <c>CivHooks</c> dispatch the population
    /// system calls (the intent is set as the policy, a compulsion or a possession would set it).
    /// </summary>
    public class C1Tests
    {
        private sealed class W
        {
            public GodSim Sim; public GodState S; public int Glade; public EventBuffer Ev; public List<SimEvent> Log = new List<SimEvent>();
            public void Drain() { Log.AddRange(Ev.Drain()); }
            public IEnumerable<T> Of<T>() { Drain(); return Log.OfType<T>(); }
        }

        private static W World(ulong seed = 3, Action<SimTuning> tune = null)
        {
            var sim = LearningFixtures.Empty(seed, t => { t.Civ.Enabled = 1; tune?.Invoke(t); });
            var w = new W { Sim = sim, S = sim.State, Glade = LearningFixtures.Glade(sim) };
            w.Ev = new EventBuffer { CurrentTick = w.S.Tick };
            return w;
        }

        /// <summary>The sim's per-tick classification over the events since the last call (then drained, so each is classified once, as in a tick).</summary>
        private static void Classify(W w) { Ownership.Classify(w.S, w.Ev); w.Drain(); }

        private static Npc Person(W w, int node) => LearningFixtures.Npc(w.Sim, node, InstinctSet.Default, LearningMode.On);
        private static int At(W w, int dir, int steps) => LearningFixtures.Step(w.Sim, w.Glade, dir, steps);

        private static int Give(W w, Npc n, Ware ware, int count) =>
            Goods.Produce(w.S, w.Ev, Holder.Carry(n.Id), ware, count, -1, 0, Owner.Npc(n.Id), ProduceReason.Placed, 0, 0);

        /// <summary>Sets the intent (as a decision would) and runs the act through the Civ dispatch until it ends.</summary>
        private static void Run(W w, Npc n, byte verb, IntentTarget kind, int id, int node, int cls = 0, int max = 3000)
        {
            n.Action = new NpcAction { Verb = verb, TargetKind = kind, TargetId = id, TargetNode = node, Since = w.S.Tick };
            var c = new Candidate { Verb = verb, Kind = kind, TargetId = id, TargetNode = node, Class = (byte)cls };
            Assert.IsTrue(CivHooks.TryStart(w.S, w.Ev, n, in c, 0), "the Civ dispatch takes C1's verbs");
            for (int i = 0; i < max && n.Action.Verb == verb; i++) Assert.IsTrue(CivHooks.TryAct(w.S, w.Ev, n, 0));
        }

        /// <summary>A finished workshop of <paramref name="owner"/>, built by it from two logs (the real path).</summary>
        private static ProductiveAsset BuildWorkshop(W w, Npc owner)
        {
            Give(w, owner, Ware.Log, 1);
            Run(w, owner, Work.VerbBuild, IntentTarget.Node, 0, owner.Node, CivClass.Workshop);
            var a = Assets.StateOf(w.S).Assets.Last();
            Give(w, owner, Ware.Log, 1);
            Run(w, owner, Work.VerbBuild, IntentTarget.Building, a.Id, a.Node);
            Run(w, owner, Work.VerbBuild, IntentTarget.Building, a.Id, a.Node);
            Assert.IsTrue(a.IsFinished, "two logs and the build work finish it");
            return a;
        }

        private static ThinkScratch ScratchOf(Npc n)
        {
            var sc = new ThinkScratch { Mind = n.Mind };
            sc.Self.Id = n.Id; sc.Self.Household = n.Household;
            return sc;
        }

        // ---------------------------------------------------------------- C1-T2

        [Test]
        public void Asset_BuiltFromRealMaterials()
        {
            var w = World();
            var a0 = Person(w, w.Glade);
            var empty = Person(w, At(w, 3, 4));
            // without materials: no site, no asset (the act frustrates)
            Run(w, empty, Work.VerbBuild, IntentTarget.Node, 0, empty.Node, CivClass.Workshop);
            Assert.AreEqual(0, Assets.StateOf(w.S).Assets.Count, "an NPC without the materials never builds");
            long logsBefore = w.S.Population.Ledger.ConsumedOf(Ware.Log);
            var a = BuildWorkshop(w, a0);
            var built = w.Of<AssetBuilt>().Single();
            Assert.AreEqual(a.Id, built.Asset); Assert.AreEqual(OwnerKind.Npc, built.OwnerKind); Assert.AreEqual(a0.Id, built.OwnerId);
            Assert.AreEqual(OwnershipBasis.Built, built.Basis); Assert.AreEqual(a0.Id, built.Builder);
            var used = w.Of<Consumed>().Where(c => c.Ware == Ware.Log).ToList();
            Assert.AreEqual(2, used.Count, "both logs left the world"); Assert.IsTrue(used.All(c => c.Reason == ConsumeReason.Built && c.RefId == a.Id && c.Npc == a0.Id));
            Assert.AreEqual(logsBefore + 2, w.S.Population.Ledger.ConsumedOf(Ware.Log));
            Assert.AreEqual(0, PopulationModule.CountOf(a0, Ware.Log));
            var bin = Goods.BinById(w.S.Population, a.Store);
            Assert.AreEqual(BinKind.AssetStore, bin.Kind); Assert.AreEqual(a.Node, bin.Node);
            Assert.AreEqual(1, a.Id); Assert.AreEqual(2, Assets.StateOf(w.S).NextAssetId, "AssetId from C1's counter");
            Assert.IsEmpty(SimInvariants.Check(w.S), "C1 conservation and the Civ invariants hold");
        }

        // ---------------------------------------------------------------- C1-T3, T8

        [Test]
        public void Work_OutputOwnedByAssetOwner_AndHaulDelivers()
        {
            var w = World();
            var a0 = Person(w, w.Glade);
            var b = Person(w, At(w, 0, 1));
            var shop = BuildWorkshop(w, a0);
            // T8: the owner walks from three nodes away and carries a log into its store
            a0.Node = At(w, 2, 3);
            Give(w, a0, Ware.Log, 1);
            Run(w, a0, Work.VerbHaul, IntentTarget.Building, shop.Id, shop.Node);
            var d = w.Of<ItemMoved>().Single(m => m.Reason == MoveReason.Deliver);
            Assert.AreEqual(a0.Id, d.Actor); Assert.AreEqual(HolderKind.Bin, d.ToKind); Assert.AreEqual(shop.Store, d.ToId);
            Assert.LessOrEqual(w.S.Territory.Grid.Distance(d.ActorNode, d.ToNode), Reach.Store, "the carrier stood within reach: no remote transfer");
            // T3: A grants B a Work right; B works one cycle
            Assert.IsTrue(Assets.GrantUse(w.S, w.Ev, shop.Id, Owner.Npc(b.Id), UseKind.Work, Owner.Npc(a0.Id), -1));
            Assert.AreEqual(1, w.Of<UseRightGranted>().Count());
            Assert.IsTrue(Permits.BelievedAsset(ScratchOf(b), shop.Id, UseKind.Work).Allowed, "B was told by the grant");
            int prodBefore = w.Of<Produced>().Count();
            Run(w, b, Work.VerbWork, IntentTarget.Building, shop.Id, shop.Node);
            var pr = w.Of<Produced>().Skip(prodBefore).Single();
            Assert.AreEqual(Ware.Plank, pr.Ware); Assert.AreEqual(ProduceReason.Crafted, pr.Reason); Assert.AreEqual(b.Id, pr.Npc, "actor: the worker");
            Assert.AreEqual(OwnerKind.Npc, pr.OwnerKind); Assert.AreEqual(a0.Id, pr.OwnerId, "the output belongs to the asset's owner");
            var bin = Goods.BinById(w.S.Population, shop.Store);
            Assert.AreEqual(1, bin.Lots.Count(l => l.Ware == Ware.Plank && l.Owner.Same(Owner.Npc(a0.Id))), "the plank lies in the asset store");
            Assert.AreEqual(0, bin.Lots.Count(l => l.Ware == Ware.Log), "the recipe's input was consumed");
            var use = w.Of<AssetUsed>().Single();
            Assert.AreEqual(b.Id, use.Worker); Assert.AreEqual(a0.Id, use.OwnerId); Assert.AreEqual(Ownership.RightUse, use.Right); Assert.AreEqual(0, use.Control);
            Assert.AreEqual(3, w.S.Population.Ledger.ConsumedOf(Ware.Log), "2 built in, 1 by the recipe: exactly");
            Assert.IsEmpty(w.Of<TheftCommitted>(), "a right holder steals nothing");
            Assert.IsEmpty(SimInvariants.Check(w.S));
        }

        // ---------------------------------------------------------------- C1-T4

        [Test]
        public void Work_NeedsReachAndInputs()
        {
            var w = World();
            var a0 = Person(w, w.Glade);
            var shop = BuildWorkshop(w, a0);
            // a worker 5 nodes away walks first; with no inputs in store or carry nothing is produced
            a0.Node = At(w, 4, 5);
            int produced = w.Of<Produced>().Count();
            Run(w, a0, Work.VerbWork, IntentTarget.Building, shop.Id, shop.Node);
            Assert.LessOrEqual(w.S.Territory.Grid.Distance(a0.Node, shop.Node), Reach.Touch, "it walked within touch first");
            Assert.AreEqual(produced, w.Of<Produced>().Count(), "no goods from nothing");
            Assert.AreEqual(0, shop.Units);
            // an intent set from outside (as a compulsion sets it) goes through the same act and the same rule
            var forced = Person(w, shop.Node);
            Run(w, forced, Work.VerbWork, IntentTarget.Building, shop.Id, shop.Node);
            Assert.AreEqual(produced, w.Of<Produced>().Count());
            // with an input in its own carry the owner works a cycle
            Give(w, a0, Ware.Log, 1);
            Run(w, a0, Work.VerbWork, IntentTarget.Building, shop.Id, shop.Node);
            Assert.AreEqual(1, shop.Units);
            Assert.AreEqual(Ware.Plank, w.Of<Produced>().Last().Ware);
            Assert.AreEqual(0, PopulationModule.CountOf(a0, Ware.Log), "the carried log went into the plank");
            Assert.AreEqual(1000, Work.Speed(w.S, a0, shop), "base speed: no skill record exists (default recorded)");
        }

        // ---------------------------------------------------------------- C1-T5, T6

        [Test]
        public void Theft_ClassifiedNotJudged()
        {
            var w = World();
            var a0 = Person(w, w.Glade);
            var b = Person(w, w.Glade);
            int lot = Give(w, a0, Ware.Roots, 2);
            Assert.IsTrue(Goods.Move(w.S, w.Ev, Holder.Carry(a0.Id), lot, 2, Holder.Ground(a0.Node), MoveReason.Drop, a0.Id).Ok);
            Classify(w);
            Assert.AreEqual(a0.Id, Ownership.LostOwner(w.S, lot, out int units).Id, "O7: the dropped lot is still A's"); Assert.AreEqual(2, units);
            var r = Goods.Move(w.S, w.Ev, Holder.Ground(a0.Node), lot, 2, Holder.Carry(b.Id), MoveReason.PickUp, b.Id, OwnerRule.ToReceiver);
            Assert.IsTrue(r.Ok, "the take is physical: nothing prevents it");
            Classify(w);
            var t = w.Of<TheftCommitted>().Single();
            Assert.AreEqual(TheftCommitted.Goods, t.Kind); Assert.AreEqual(a0.Id, t.OwnerId); Assert.AreEqual(OwnerKind.Npc, t.OwnerKind);
            Assert.AreEqual(b.Id, t.Taker); Assert.AreEqual(2, t.Count); Assert.AreEqual(Ware.Roots, t.Ware);
            // a lot A gave B is B's: B drops it and takes it up again: no theft
            int gift = Give(w, a0, Ware.Berries, 1);
            Assert.IsTrue(Goods.Move(w.S, w.Ev, Holder.Carry(a0.Id), gift, 1, Holder.Carry(b.Id), MoveReason.HandOver, a0.Id, OwnerRule.ToReceiver).Ok);
            int given = b.Carry.First(c => !c.Empty && c.Ware == Ware.Berries).Id;
            Assert.IsTrue(Goods.Move(w.S, w.Ev, Holder.Carry(b.Id), given, 1, Holder.Ground(b.Node), MoveReason.Drop, b.Id).Ok);
            Classify(w);
            int onGround = w.S.Population.Lots.First(g => g.Lot.Ware == Ware.Berries).Lot.Id;
            Assert.IsTrue(Goods.Move(w.S, w.Ev, Holder.Ground(b.Node), onGround, 1, Holder.Carry(b.Id), MoveReason.PickUp, b.Id, OwnerRule.ToReceiver).Ok);
            Classify(w);
            Assert.AreEqual(1, w.Of<TheftCommitted>().Count(), "no theft of one's own lot");
            Assert.AreEqual(1, Assets.StateOf(w.S).Thefts);
        }

        [Test]
        public void OwnershipBelief_FromPerception_PermissionIsAQueryOnly()
        {
            var w = World();
            var a0 = Person(w, w.Glade);
            var c = Person(w, At(w, 1, 3));
            var d = Person(w, At(w, 5, 8));
            d.Node = LearningFixtures.Step(w.Sim, d.Node, 5, 8); // far beyond sight
            Assert.Greater(w.S.Territory.Grid.Distance(d.Node, w.Glade), w.S.Tuning.Population.SightDay);
            var shop = BuildWorkshop(w, a0);
            var cm = c.Mind.Civ;
            Assert.IsNotNull(cm, "C watched A build it");
            var bel = cm.Own.Single(x => x.Asset == shop.Id);
            Assert.AreEqual(a0.Id, bel.BelievedOwner.Id); Assert.AreEqual(OwnerKind.Npc, bel.BelievedOwner.Kind);
            Assert.IsNull(d.Mind.Civ, "D never saw it: no belief");
            var scC = ScratchOf(c);
            Assert.IsFalse(Permits.Believed(scC, Permits.BelievedOwnerOf(scC, shop.Id)).Allowed, "C believes the store is not its to take from");
            Assert.IsFalse(Permits.BelievedAsset(scC, shop.Id, UseKind.Store).Allowed);
            Assert.IsFalse(Permits.BelievedAsset(ScratchOf(d), shop.Id, UseKind.Store).Allowed);
            Assert.IsTrue(Permits.BelievedAsset(ScratchOf(a0), shop.Id, UseKind.Work).Allowed, "the builder believes it owns it");
            // the query enforces nothing: C can physically withdraw A's goods; the sim classifies it as theft
            a0.Node = shop.Node; Give(w, a0, Ware.Log, 1);
            Run(w, a0, Work.VerbHaul, IntentTarget.Building, shop.Id, shop.Node);
            c.Node = shop.Node;
            int logLot = Goods.BinById(w.S.Population, shop.Store).Lots.Single().Id;
            Assert.IsTrue(Goods.Move(w.S, w.Ev, Holder.Bin(shop.Store), logLot, 1, Holder.Carry(c.Id), MoveReason.Withdraw, c.Id, OwnerRule.Keep).Ok);
            Classify(w);
            var t = w.Of<TheftCommitted>().Single();
            Assert.AreEqual(c.Id, t.Taker); Assert.AreEqual(a0.Id, t.OwnerId); Assert.AreEqual(shop.Id, t.Asset); Assert.AreEqual(HolderKind.Bin, t.FromKind);
            Assert.IsFalse(Ownership.Truth(w.S, c, Goods.BinById(w.S.Population, shop.Store), Owner.Npc(a0.Id)).Allowed);
            Assert.IsTrue(Ownership.Truth(w.S, a0, Goods.BinById(w.S.Population, shop.Store), Owner.Npc(a0.Id)).Allowed);
        }

        [Test]
        public void UseTheft_WorkingWithoutAnyRight_IsClassifiedAsUse()
        {
            var w = World();
            var a0 = Person(w, w.Glade);
            var shop = BuildWorkshop(w, a0);
            var x = Person(w, shop.Node);
            Give(w, x, Ware.Log, 1);
            Run(w, x, Work.VerbWork, IntentTarget.Building, shop.Id, shop.Node);
            var t = w.Of<TheftCommitted>().Single();
            Assert.AreEqual(TheftCommitted.Use, t.Kind); Assert.AreEqual(x.Id, t.Taker); Assert.AreEqual(shop.Id, t.Asset);
            Assert.AreEqual(Ownership.RightNone, w.Of<AssetUsed>().Single().Right);
            Assert.AreEqual(a0.Id, w.Of<Produced>().Last().OwnerId, "even then the output is the owner's");
        }

        // ---------------------------------------------------------------- C1-T7

        [Test]
        public void LostProperty_O7()
        {
            var w = World();
            var a0 = Person(w, w.Glade);
            int lot = Give(w, a0, Ware.Log, 1);
            int day0 = (int)SimCalendar.Of(w.S).Day(w.S.Tick);
            Goods.Move(w.S, w.Ev, Holder.Carry(a0.Id), lot, 1, Holder.Ground(a0.Node), MoveReason.Drop, a0.Id);
            Classify(w);
            Assert.IsTrue(w.S.Population.Lots.Single(g => g.Lot.Id == lot).Lot.Owner.IsNone, "Goods keeps ground lots ownerless (C4 on the lot itself)");
            int days = w.S.Tuning.Civ.C1.LostPropertyDays;
            Assert.AreEqual(3, days);
            for (int d = 1; d < days; d++) { Ownership.DayStart(w.S, w.Ev, day0 + d); Assert.AreEqual(a0.Id, Ownership.LostOwner(w.S, lot, out _).Id, "still A's on day +" + d); }
            Ownership.DayStart(w.S, w.Ev, day0 + days);
            Assert.IsTrue(Ownership.LostOwner(w.S, lot, out _).IsNone, "abandoned after LostPropertyDays");
            Assert.AreEqual(1, Assets.StateOf(w.S).Abandoned);
            var drop = w.Of<OwnershipChanged>().Last();
            Assert.AreEqual(OwnershipBasis.Abandoned, drop.Basis);
            // Civ off: a drop is ownerless at once and no overlay exists (M4/M5 unchanged)
            var off = LearningFixtures.Empty(3);
            Assert.IsNull(off.State.Civ);
            var p = LearningFixtures.Npc(off, LearningFixtures.Glade(off), InstinctSet.Default, LearningMode.On);
            int l2 = Goods.Produce(off.State, null, Holder.Carry(p.Id), Ware.Log, 1, -1, 0, Owner.Npc(p.Id), ProduceReason.Placed, 0, 0);
            Assert.IsTrue(Goods.Move(off.State, null, Holder.Carry(p.Id), l2, 1, Holder.Ground(p.Node), MoveReason.Drop, p.Id).After.IsNone);
            Assert.IsTrue(Ownership.LostOwner(off.State, l2, out _).IsNone);
        }

        // ---------------------------------------------------------------- property-record API

        [Test]
        public void Transfer_GrantEnd_Destroy_KeepOneIdSpace()
        {
            var w = World();
            var a0 = Person(w, w.Glade);
            var b = Person(w, At(w, 0, 2));
            var field = Assets.Register(w.S, w.Ev, AssetKinds.Field, At(w, 2, 4), Owner.Npc(a0.Id), OwnershipBasis.StateGrant, 0);
            var shop = BuildWorkshop(w, a0);
            Assert.AreEqual(field.Id + 1, shop.Id, "one id space for every asset");
            Assets.GrantUse(w.S, w.Ev, shop.Id, Owner.Npc(b.Id), UseKind.Work, Owner.Npc(a0.Id), 5);
            Assert.IsTrue(Assets.Transfer(w.S, w.Ev, shop.Id, Owner.Npc(b.Id), OwnershipBasis.Gift, a0.Id));
            Assert.AreEqual(UseRightEnded.Transferred, w.Of<UseRightEnded>().Single().Reason, "the old owner's grants end with the transfer");
            var tr = w.Of<AssetTransferred>().Single();
            Assert.AreEqual(a0.Id, tr.FromId); Assert.AreEqual(b.Id, tr.ToId); Assert.AreEqual(OwnershipBasis.Gift, tr.Basis);
            Assert.IsTrue(Assets.Destroy(w.S, w.Ev, field.Id, 4));
            Assert.IsNull(Assets.Find(w.S, field.Id));
            Assert.AreEqual(3, Assets.StateOf(w.S).NextAssetId, "ids are never reused");
            Assert.IsTrue(Property.Claim(w.S, w.Ev, Property.Source, 7, Owner.Npc(b.Id), OwnershipBasis.FirstArrival));
            Assert.IsFalse(Property.Claim(w.S, w.Ev, Property.Source, 7, Owner.Npc(a0.Id), OwnershipBasis.FirstArrival), "the first arrival keeps the claim");
            Assert.AreEqual(1, w.Of<SourceClaimed>().Count());
            Assert.IsEmpty(SimInvariants.Check(w.S));
        }

        // ---------------------------------------------------------------- C1-T12 (save/load parity)

        [Test]
        public void C1_SaveLoadParity()
        {
            GodSim Game()
            {
                var w = World(11);
                var a0 = Person(w, w.Glade);
                var b = Person(w, At(w, 0, 2));
                var shop = BuildWorkshop(w, a0);
                Give(w, a0, Ware.Log, 1);
                Run(w, a0, Work.VerbHaul, IntentTarget.Building, shop.Id, shop.Node);
                // a half-done work unit
                Give(w, a0, Ware.Log, 1);
                a0.Action = new NpcAction { Verb = Work.VerbWork, TargetKind = IntentTarget.Building, TargetId = shop.Id, TargetNode = shop.Node, Since = w.S.Tick };
                for (int i = 0; i < 30; i++) CivHooks.TryAct(w.S, w.Ev, a0, 0);
                Assert.Greater(shop.WorkProgress, 0); Assert.AreEqual(0, shop.Units);
                // a dropped owned lot mid lost-property timer
                int lot = Give(w, b, Ware.Roots, 1);
                Goods.Move(w.S, w.Ev, Holder.Carry(b.Id), lot, 1, Holder.Ground(b.Node), MoveReason.Drop, b.Id);
                Classify(w);
                Assert.AreEqual(1, Assets.StateOf(w.S).Lost.Count);
                LearningFixtures.Bush(w.Sim, SourceKind.Roots, At(w, 3, 3));
                w.Sim.Step(200);
                return w.Sim;
            }
            var straight = Game();
            var other = Game();
            Assert.AreEqual(straight.StateHash(), other.StateHash(), "deterministic set-up");
            var loaded = SaveFile.FromJson(SaveFile.From(other, "x").ToJson()).ToSim();
            Assert.AreEqual(straight.StateHash(), loaded.StateHash(), "same hash straight after the load");
            Assert.IsNotNull(loaded.State.Civ.Ownership);
            for (int k = 0; k < 8; k++)
            {
                straight.Step(600); loaded.Step(600);
                CollectionAssert.AreEqual(straight.StateHashParts(), loaded.StateHashParts(), "checkpoint " + k);
            }
            Assert.AreEqual(SaveFile.From(straight).ToJson(), SaveFile.From(loaded).ToJson());
            Assert.IsEmpty(SimInvariants.Check(loaded.State));
            Assert.AreEqual(0, loaded.State.Civ.Ownership.Lost.Count, "the lost lot expired (or was taken) after the 3 days");
        }

        // ---------------------------------------------------------------- C1-T11 and the truth boundary (static)

        private static string Src(string rel) => File.ReadAllText(Path.Combine(GoodsTests.RepoRoot(), "Assets", "God", "Sim", rel));

        [Test]
        public void Production_DoesNotBranchOnOwnerKind()
        {
            foreach (var f in new[] { "Civ/Work.cs", "Civ/Assets.cs" })
            {
                var lines = Src(f).Split('\n').Where(l => !l.TrimStart().StartsWith("//")).ToList();
                Assert.IsEmpty(lines.Where(l => Regex.IsMatch(l, @"OwnerKind\.|OwnershipModel")).ToList(), f + " branches on the owner's kind");
            }
        }

        [Test]
        public void Theft_And_Truth_AreNeverReadByRulesPoliciesOrMinds()
        {
            string root = Path.Combine(GoodsTests.RepoRoot(), "Assets", "God", "Sim");
            var allowedTheft = new[] { "CivEvents.C1.cs", "Ownership.cs", "Work.cs" };
            var allowedTruth = new[] { "Affordances.cs", "Ownership.cs", "Work.cs" };
            var bad = new List<string>();
            foreach (var f in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories).OrderBy(x => x, StringComparer.Ordinal))
            {
                string name = Path.GetFileName(f);
                foreach (var line in File.ReadAllLines(f))
                {
                    var t = line.TrimStart();
                    if (t.StartsWith("//")) continue;
                    if (t.Contains("TheftCommitted") && !allowedTheft.Contains(name)) bad.Add(name + ": " + t);
                    if (Regex.IsMatch(t, @"Permits\.Truth|Ownership\.Truth|Ownership\.RightOn") && !allowedTruth.Contains(name)) bad.Add(name + ": " + t);
                }
            }
            Assert.IsEmpty(bad, "only the sim's classifier and the act emit or read theft and truth:\n" + string.Join("\n", bad));
            // reflection: the mind hooks cannot see the true state at all (so neither Truth nor any event buffer)
            var types = TypeReferenceScanner.TypesOf(typeof(CivMindHooks));
            Assert.IsFalse(types.Contains(typeof(GodState)), "CivMindHooks references GodState");
            Assert.IsFalse(types.Contains(typeof(TheftCommitted)));
            Assert.IsFalse(types.Contains(typeof(EventBuffer)));
            Assert.IsFalse(TypeReferenceScanner.TypesOf(typeof(Permits)).Contains(typeof(GodState)), "Permits (mind side) cannot reach the truth");
        }

        [Test]
        public void CivMindHooks_AddsVerbsRows_AndIdleWithoutAssets()
        {
            Assert.IsTrue(VerbTable.TryGet(Work.VerbWork, out var v) && v.Intent == Work.IntentWork);
            Assert.IsTrue(VerbTable.TryGet(Work.VerbBuild, out _) && VerbTable.TryGet(Work.VerbHaul, out _));
            var w = World(5, t => t.Civ.C1.BuildEnabled = 0);
            var p = Person(w, w.Glade);
            Give(w, p, Ware.Log, 1);
            var near = new Sensed[16];
            int n = CivHooks.SenseExtra(w.S, p, 10, w.S.Territory.Grid, near, 0);
            Assert.AreEqual(0, n, "BuildEnabled 0: nothing to perceive");
            var w2 = World(5);
            var q = Person(w2, w2.Glade);
            Give(w2, q, Ware.Log, 1);
            n = CivHooks.SenseExtra(w2.S, q, 10, w2.S.Territory.Grid, near, 0);
            Assert.AreEqual(1, n); Assert.AreEqual(CivClass.Plot, near[0].Class, "a free plot while it carries a log");
        }
    }
}
