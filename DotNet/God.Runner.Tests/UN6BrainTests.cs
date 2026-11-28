using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using God.Runner.Tests.Support;
using God.Runner.Warehouse;
using God.Sim;
using God.Sim.Comms;
using God.Sim.Learning;
using God.Sim.Population;
using God.Sim.Unified;
using TargetKind = God.Sim.Unified.TargetKind;
using NUnit.Framework;

namespace God.Runner.Tests
{
    /// <summary>
    /// UN6 (spec 13): the brain runtime. Until UN2's encoder and motor merge, world-level tests run over <see cref="FixtureBody"/>
    /// (MECHANICAL: toy perception, every control accepted, fixture-installed USE physics); nothing here is evidence of learned
    /// behaviour. Inference, artifact, claims and state tests are exact.
    /// </summary>
    [Parallelizable(ParallelScope.None)]
    public class UN6BrainTests
    {
        private static string Root
        {
            get
            {
                var d = new DirectoryInfo(AppContext.BaseDirectory);
                while (d != null && !Directory.Exists(Path.Combine(d.FullName, "Assets", "God", "Sim"))) d = d.Parent;
                return d.FullName;
            }
        }

        private static void Step(GodSim sim, int ticks, List<SimEvent> into) { for (int i = 0; i < ticks; i++) { sim.Step(); sim.DrainInto(into); } }
        private static string BrainDir => Path.Combine(Root, "Assets", "God", "Sim", "Unified", "Brain");

        private static short[] RandomObs(Random r)
        {
            var o = new short[ObsSchema.Length];
            for (int i = 0; i < o.Length; i++) o[i] = (short)r.Next(-1500, 1501);
            return o;
        }

        private static ActMask AllLegal()
        {
            return new ActMask
            {
                Control = 0x1FFEUL, Target = (1UL << 19) - 1, Source = (1UL << 5) - 1, Slot = 0xF, Design = (1UL << 33) - 1,
                Gesture = 0xFFFFFFFFUL, Referent1 = (1UL << 22) - 1, Referent2 = (1UL << 22) - 1, Quantity = 0xFFFF,
            };
        }

        private static BrainNet UntrainedNet(ulong seed = 1) { var sh = BrainShape.Default; return new BrainNet(sh, BrainNet.UntrainedWeights(sh, seed)); }

        // ---------------------------------------------------------------- T1

        /// <summary>The golden vector: decisions of the untrained default network on a fixed observation stream (seed 1), pinned.</summary>
        public const string GoldenTrace = "golden";

        [Test]
        public void Brain_Inference_Deterministic()
        {
            string Run()
            {
                var net = UntrainedNet();
                var h = new short[BrainState.RecurrentSize];
                var r = new Random(7); var rng = new Rng(11);
                var m = AllLegal();
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < 200; i++)
                {
                    var a = net.Act(RandomObs(r), h, in m, (i & 1) == 0 ? rng : null);
                    sb.Append(a.ToString()).Append('|');
                }
                foreach (var x in h) sb.Append(x).Append(',');
                return sb.ToString();
            }
            string a1 = Run(), a2 = Run();
            Assert.AreEqual(a1, a2, "same observations and state give the same actions");
            string digest = SchemaText.Sha256(a1).Substring(0, 16);
            TestContext.WriteLine("UN6-T1 golden digest " + digest);
            Assert.AreEqual(UN6Golden.UntrainedTraceDigest, digest, "the pinned golden vector of the untrained net (seed 1)");
            // the test artifact's golden decision, greedy, from a zero state
            var art = ArtifactLoader.Read(UN6TestArtifacts.Valid, out var err);
            Assert.IsNotNull(art, err);
            var tn = new BrainNet(BrainShape.Parse(art.NetShape), art.Weights);
            var th = new short[BrainState.RecurrentSize];
            var obs = RandomObs(new Random(3)); var am = AllLegal();
            var c = tn.Act(obs, th, in am, null);
            TestContext.WriteLine("UN6-T1 test artifact golden choice " + c + " h " + th[0] + "," + th[1]);
            Assert.AreEqual(UN6Golden.TestArtifactChoice, c.ToString() + " h " + th[0] + "," + th[1]);
            Assert.AreEqual(BrainShape.Default.Count, 22654, "default shape weights (UN6t-a's count)");
        }

        // ---------------------------------------------------------------- T2

        [Test]
        public void Brain_Budget()
        {
            var sh = BrainShape.Default;
            Assert.AreEqual(19616, sh.MacPerDecision, "worst-case MAC of the default shape");
            Assert.LessOrEqual(sh.MacPerDecision, BrainShape.MacBudget);
            var net = UntrainedNet();
            var h = new short[BrainState.RecurrentSize];
            var r = new Random(5); var rng = new Rng(5); var m = AllLegal();
            var inputs = Enumerable.Range(0, 64).Select(_ => RandomObs(r)).ToArray();
            int maxMacs = 0;
            for (int i = 0; i < 2000; i++) { net.Act(inputs[i & 63], h, in m, rng); if (net.Macs > maxMacs) maxMacs = net.Macs; }
            Assert.LessOrEqual(maxMacs, BrainShape.MacBudget, "measured MAC per decision");
            var b = new BrainState();
            Assert.LessOrEqual(b.Bytes, BrainState.MaxBytes, "brain state bytes");
            TestContext.WriteLine("UN6-T2 MAC worst " + sh.MacPerDecision + ", measured max " + maxMacs + "; brain state " + b.Bytes + " bytes; overlay store "
                + new AssociationStore(new LearningCaps(64, 0, 8, 0, 0)).Bytes + " bytes");

            // 10,000 full decisions (sense, credit, overlay, policy, submit) allocate nothing
            var sim = UN6Games.Game(21, 3);
            var body = UN6Games.Attach(sim);
            var s = sim.State;
            var rt = s.Unification.BrainRuntime;
            var people = UN6Games.People(s);
            for (int i = 0; i < 200; i++) foreach (var n in people) BrainDecider.Decide(s, null, n, s.Unification.PersonOf(n.Id), rt);
            long before = GC.GetAllocatedBytesForCurrentThread();
            int k = 0;
            while (k < 10000) foreach (var n in people) { BrainDecider.Decide(s, null, n, s.Unification.PersonOf(n.Id), rt); k++; }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            TestContext.WriteLine("UN6-T2 allocated over " + k + " decisions: " + allocated + " bytes");
            Assert.AreEqual(0, allocated, "zero allocation per decision");

            // no floating point and no Math in the run-time brain folder
            var bad = new List<string>();
            foreach (var f in Directory.GetFiles(BrainDir, "*.cs", SearchOption.AllDirectories))
            {
                var text = StripComments(File.ReadAllText(f));
                foreach (var w in new[] { "float", "double", "decimal", "Math." })
                    if (Regex.IsMatch(text, @"(?<![A-Za-z0-9_])" + Regex.Escape(w) + (w.EndsWith(".") ? "" : @"(?![A-Za-z0-9_])"))) bad.Add(Path.GetFileName(f) + ": " + w);
            }
            Assert.IsEmpty(bad, "floating point or Math in Unified/Brain");
        }

        internal static string StripComments(string t)
        {
            t = Regex.Replace(t, @"/\*.*?\*/", "", RegexOptions.Singleline);
            t = Regex.Replace(t, @"//[^\n]*", "");
            return Regex.Replace(t, "\"(?:[^\"\\\\]|\\\\.)*\"", "\"\"");
        }

        // ---------------------------------------------------------------- T3

        [Test]
        public void Artifact_HashRefusal()
        {
            var ok = ArtifactLoader.Read(UN6TestArtifacts.Valid, out var err);
            Assert.IsNotNull(ok, err);
            Assert.IsNull(ArtifactLoader.Verify(ok, ArtifactLoader.CurrentCatalogueHash()), "the valid test artifact verifies");
            var flip = ok.Copy(); flip.Weights[100] ^= 4;
            Assert.AreEqual("weights hash mismatch", ArtifactLoader.Verify(flip, ArtifactLoader.CurrentCatalogueHash()));
            var schema = ok.Copy(); schema.ActSchemaHash = "x";
            Assert.AreEqual("Act schema changed", ArtifactLoader.Verify(schema, ArtifactLoader.CurrentCatalogueHash()));
            var cat = ok.Copy(); cat.CatalogueHash = "other";
            Assert.AreEqual("catalogue changed", ArtifactLoader.Verify(cat, ArtifactLoader.CurrentCatalogueHash()));

            // Kind = 1 with each bad artifact refuses to start the game
            foreach (var (v, why) in new[] { (UN6TestArtifacts.FlippedWeight, "weights hash mismatch"), (UN6TestArtifacts.SchemaChanged, "Obs schema changed"),
                (UN6TestArtifacts.CatalogueChanged, "catalogue changed"), (UN6TestArtifacts.ClaimsTrained, "cannot be TRAINED"), (8765, "not found") })
            {
                var ex = Assert.Throws<BrainRefused>(() => UN6Games.Game(1, 2, t => { t.Unification.Brain.Kind = 1; t.Unification.Brain.Artifact = v; }), "v" + v);
                StringAssert.Contains(why, ex.Message);
                StringAssert.Contains("No silent downgrade", ex.Message);
            }

            // Kind 1 (valid test artifact), 0 and 2 start, labelled; BrainAttached carries kind, version and hash
            foreach (var kind in new[] { 0, 1, 2 })
            {
                var sim = UN6Games.Game(2, 2, t => { t.Unification.Brain.Kind = kind; if (kind == 1) t.Unification.Brain.Artifact = UN6TestArtifacts.Valid; });
                var events = new List<SimEvent>();
                Step(sim, 1, events);
                var a = events.OfType<BrainAttached>().Single();
                Assert.AreEqual(kind, a.Kind);
                var badge = Brain.Badge(sim.State);
                Assert.AreEqual(kind, badge.Kind);
                Assert.IsFalse(badge.Trained, "no kind here is a TRAINED artifact");
                if (kind == 1) { Assert.AreEqual(UN6TestArtifacts.Valid, a.ArtifactVersion); Assert.AreEqual(ok.WeightsSha256, a.WeightsSha256); Assert.AreEqual("ARTIFACT v9001 SELF-TEST", a.Label); }
                if (kind == 0) { Assert.AreEqual("UNTRAINED", a.Label); Assert.AreEqual(64, a.WeightsSha256.Length); }
                if (kind == 2) { Assert.AreEqual("FALLBACK", a.Label); Assert.AreEqual("", a.WeightsSha256); }
                Step(sim, 5, events);
                Assert.AreEqual(1, events.OfType<BrainAttached>().Count(), "emitted once");
                var loaded = SaveFile.FromJson(SaveFile.From(sim).ToJson()).ToSim();
                var ev2 = new List<SimEvent>(); Step(loaded, 3, ev2);
                Assert.IsEmpty(ev2.OfType<BrainAttached>(), "a load does not attach again");
            }
            // a save names its brain: loading it under another brain is refused
            var s0 = UN6Games.Game(3, 2); s0.Step(1);
            var json = SaveFile.From(s0).ToJson();
            var file = SaveFile.FromJson(json);
            file.State.Tuning.Unification.UN6.UntrainedSeed = 2;
            Assert.Throws<BrainRefused>(() => file.ToSim(), "different untrained weights than the save ran");
        }

        // ---------------------------------------------------------------- T4

        private sealed class BusyProbe : ISimSystem
        {
            public readonly Dictionary<int, (bool busy, bool asleep, int decisions)> Before = new Dictionary<int, (bool, bool, int)>();
            public void Tick(GodState s, EventBuffer ev)
            {
                Before.Clear();
                foreach (var n in s.Population.Npcs)
                {
                    var u = s.Unification.PersonOf(n.Id);
                    if (u != null) Before[n.Id] = (u.Busy, n.Action.Asleep, u.Brain.Decisions);
                }
            }
        }

        private sealed class BusyCheck : ISimSystem
        {
            public BusyProbe Probe; public int Violations, Decided;
            public void Tick(GodState s, EventBuffer ev)
            {
                foreach (var kv in Probe.Before)
                {
                    var u = s.Unification.PersonOf(kv.Key);
                    if (u.Brain.Decisions == kv.Value.decisions) continue;
                    Decided++;
                    if (kv.Value.busy || kv.Value.asleep) Violations++;
                }
            }
        }

        private sealed class Viewer : ISimSystem
        {
            public long Reads;
            public void Tick(GodState s, EventBuffer ev)
            {
                var b = Brain.Badge(s);
                foreach (var u in s.Unification.People) Reads += u.Brain.Decisions + Brain.Claims(s, u.Npc).Length + b.Kind;
            }
        }

        private static GodSim ScheduleRun(bool view, out FixtureBody body, out BusyCheck check, int sleeper)
        {
            var sim = UN6Games.Game(31, 3);
            body = UN6Games.Attach(sim);
            body.ActionTicks = 7;
            var probe = new BusyProbe();
            check = new BusyCheck { Probe = probe };
            sim.AddSystem(248, probe); // before the fake motor and the person ticks
            sim.AddSystem(260, check);
            if (view) sim.AddSystem(990, new Viewer());
            sim.State.Population.Find(sleeper).Action.Asleep = true;
            return sim;
        }

        [Test]
        public void Decision_OnCompletionOnly()
        {
            var people = UN6Games.People(UN6Games.Game(31, 3).State);
            int sleeper = people[1].Id;
            var a = ScheduleRun(false, out var bodyA, out var checkA, sleeper);
            var sleeperState = a.State.Unification.PersonOf(sleeper);
            for (int i = 0; i < 300; i++) { a.Step(1); a.State.Population.Find(sleeper).Action.Asleep = true; }
            Assert.Greater(checkA.Decided, 0);
            Assert.AreEqual(0, checkA.Violations, "a busy or sleeping person never decides");
            Assert.AreEqual(0, sleeperState.Brain.Decisions, "the sleeper never decided");
            // a 7-tick action: each awake person decides about once per 7 ticks (completion), never every tick
            var rt = a.State.Unification.BrainRuntime;
            int awake = people.Count - 1;
            TestContext.WriteLine("UN6-T4 decisions " + rt.Decisions + " over 300 ticks x " + awake + " awake persons; skipped (no decision due) " + rt.Skipped);
            Assert.LessOrEqual(rt.Decisions, awake * (300 / 7 + 1));
            Assert.GreaterOrEqual(rt.Decisions, awake * (300 / 8 - 1));
            // with and without a view attached: identical
            var b = ScheduleRun(true, out _, out _, sleeper);
            for (int i = 0; i < 300; i++) { b.Step(1); b.State.Population.Find(sleeper).Action.Asleep = true; }
            Assert.AreEqual(a.StateHash(), b.StateHash(), "a read-only view changes nothing");
        }

        // ---------------------------------------------------------------- T5

        /// <summary>Holds every person's hunger at <see cref="H"/> before the person ticks (the fixture's physics; only hunger is active).</summary>
        private sealed class HungerPin : ISimSystem
        {
            public int H = 900;
            public void Tick(GodState s, EventBuffer ev) { foreach (var n in s.Population.Npcs) n.Drives[Drive.Hunger] = H * 1000; }
        }

        [Test]
        public void Overlay_NeedCredit_NotGoods()
        {
            // only Hunger is an active need; a hungry person USEs the food thing. Run 1: each USE lowers hunger by 50 (relief).
            // Run 2: the identical sequence, but each USE only moves a new lot into the person's carry (a holder change), no need change.
            int Bias(GodSim sim, Npc n, int hunger)
            {
                var s = sim.State; var rt = s.Unification.BrainRuntime;
                var u = s.Unification.PersonOf(n.Id);
                n.Drives[Drive.Hunger] = hunger * 1000;
                rt.Body.Sense(s, n, ref rt.P, rt.Obs, ref rt.Mask);
                int k = BrainDecider.Keys(rt.Obs, rt);
                return BrainDecider.BiasOf(s.Unification.OverlayOf(u.Brain.Overlay), rt.Keys, k, (byte)Prim.Use, s.Tuning.Unification.UN6);
            }
            GodSim Run(bool relief, out Npc eater, out int bias0, out int carried)
            {
                var sim = UN6Games.Game(41, 1, t => t.Unification.Needs.ActiveMask = 1);
                var s = sim.State;
                var body = UN6Games.Attach(sim);
                var pin = new HungerPin();
                sim.AddSystem(246, pin);
                eater = UN6Games.People(s)[0];
                var me = eater;
                for (int i = 0; i < me.Carry.Length; i++)
                    if (!me.Carry[i].Empty) Goods.Move(s, null, Holder.Carry(me.Id), me.Carry[i].Id, me.Carry[i].Count, Holder.Ground(me.Node), MoveReason.Drop, me.Id);
                foreach (var o in UN6Games.People(s)) if (o.Id != me.Id) o.Node = me.Node + 50; // alone
                body.Things.Add(new FixtureBody.Thing { Class = 1500, Node = me.Node, Id = 77, Kind = TargetKind.Lot });
                s.Unification.BrainRuntime.Policy = new ScriptedPolicy(new PrimitiveControl { Control = Prim.Use, Target = new TargetRef(TargetKind.Lot, 77) });
                s.Unification.BrainRuntime.Origin = ControlOrigin.Fixture;
                int moved = 0;
                body.OnUse = (st, n, c) =>
                {
                    if (relief) pin.H -= 50;
                    else if (Goods.Produce(st, null, Holder.Carry(n.Id), Ware.Log, 1, -1, 0, Owner.Npc(n.Id), ProduceReason.HandGathered, n.Id, 0) != 0) moved++;
                };
                bias0 = Bias(sim, eater, 900);
                sim.Step(60); // 12 decisions (4-tick actions)
                carried = moved;
                return sim;
            }
            var fed = Run(true, out var e1, out int b0, out _);
            int b1 = Bias(fed, e1, 900);
            TestContext.WriteLine("UN6-T5 USE bias at hunger 900: before " + b0 + ", after 12 relieving meals " + b1);
            Assert.Greater(b1, b0, "eating when hungry raises the USE bias in that context");
            Assert.LessOrEqual(Math.Abs(b1), fed.State.Tuning.Unification.UN6.OverlayBiasCap, "bounded");
            var held = Run(false, out var e2, out int c0, out int carried);
            int c1 = Bias(held, e2, 900);
            TestContext.WriteLine("UN6-T5 USE bias with only holder changes (" + carried + " lots into carry): before " + c0 + ", after " + c1);
            Assert.Greater(carried, 0, "the holder changed");
            Assert.AreEqual(c0, c1, "a holder or lot change with no need change leaves the bias unchanged");
            Assert.IsTrue(held.State.Unification.Overlays.All(o => o.Store.Entries.Take(o.Store.Count).All(x => x.Stw == 0)), "no weight moved at all");
            foreach (var o in fed.State.Unification.Overlays) foreach (var x in o.Store.Entries.Take(o.Store.Count)) Assert.LessOrEqual(Math.Abs((int)x.Stw), Fx.WeightCap);
            // a delayed relief: the meal's relief arrives two decisions later; the USE trace still earns credit
            var late = UN6Games.Game(42, 1, t => t.Unification.Needs.ActiveMask = 1);
            var lb = UN6Games.Attach(late); var lpin = new HungerPin(); late.AddSystem(246, lpin);
            var le = UN6Games.People(late.State)[0];
            foreach (var o in UN6Games.People(late.State)) if (o.Id != le.Id) o.Node = le.Node + 50;
            lb.Things.Add(new FixtureBody.Thing { Class = 1500, Node = le.Node, Id = 77, Kind = TargetKind.Lot });
            late.State.Unification.BrainRuntime.Policy = new ScriptedPolicy(new PrimitiveControl { Control = Prim.Use, Target = new TargetRef(TargetKind.Lot, 77) },
                new PrimitiveControl { Control = Prim.Wait }, new PrimitiveControl { Control = Prim.Wait });
            var due = new Queue<long>();
            lb.OnUse = (st, n, c) => { if (n.Id == le.Id) due.Enqueue(st.Tick + 9); };
            late.AddSystem(245, new Poke(st => { while (due.Count > 0 && st.Tick >= due.Peek()) { due.Dequeue(); lpin.H -= 60; } }));
            late.Step(120);
            int lateBias = Bias(late, le, 900);
            TestContext.WriteLine("UN6-T5 USE bias with the relief 9 ticks (two decisions) after the USE: " + lateBias);
            Assert.Greater(lateBias, 0, "delayed outcome credited through the eligibility traces");
        }

        private sealed class Poke : ISimSystem
        {
            private readonly Action<GodState> _a;
            public Poke(Action<GodState> a) { _a = a; }
            public void Tick(GodState s, EventBuffer ev) => _a(s);
        }

        // ---------------------------------------------------------------- T6

        [Test]
        public void Knowledge_OwnObservedTaught()
        {
            // own success: an actor USEs fresh water (class 2, DrinkWash), not innate in this run
            var sim = UN6Games.Game(51, 3, t => t.Unification.UN6.InnateInteractions = 0);
            var s = sim.State;
            var body = UN6Games.Attach(sim);
            var people = UN6Games.People(s);
            var actor = people[0];
            int k = (int)EffectKind.DrinkWash;
            foreach (var o in people) o.Node = actor.Node + 100 + o.Id * 20; // everyone far away
            actor.Node = 10;
            people[1].Node = 11;  // an observer at distance 1
            people[2].Node = 70;  // out of sight
            body.Things.Add(new FixtureBody.Thing { Class = 2, Node = actor.Node, Id = 5, Kind = TargetKind.WaterPoint });
            var rt = s.Unification.BrainRuntime;
            rt.Policy = new ScriptedPolicy(new PrimitiveControl { Control = Prim.Use, Target = new TargetRef(TargetKind.WaterPoint, 5) });
            rt.Origin = ControlOrigin.Fixture;
            foreach (var o in people) if (o != actor) { var ou = s.Unification.PersonOf(o.Id); ou.ActionKind = Prim.Wait; ou.ActionTicksLeft = 100000; } // only the actor decides
            s.Unification.PersonOf(actor.Id).ActionKind = Prim.None;
            Assert.IsFalse(Knowledge.Knows(s.Unification.PersonOf(actor.Id).Brain, KnowledgeKind.Interaction, k));
            var events = new List<SimEvent>();
            Step(sim, 12, events);
            Assert.IsTrue(Knowledge.Knows(s.Unification.PersonOf(actor.Id).Brain, KnowledgeKind.Interaction, k), "known after own success");
            var own = events.OfType<KnowledgeAcquired>().First(e => e.Npc == actor.Id);
            Assert.AreEqual(KnowledgeSource.Own, own.Source); Assert.AreEqual(ControlOrigin.Fixture, own.Origin);
            Assert.IsTrue(events.OfType<DemonstrationObserved>().Any(e => e.Observer == people[1].Id), "the near person perceived it");
            Assert.IsFalse(events.OfType<DemonstrationObserved>().Any(e => e.Observer == people[2].Id), "the far person did not");
            Assert.IsFalse(Knowledge.Knows(s.Unification.PersonOf(people[2].Id).Brain, KnowledgeKind.Interaction, k));

            // observation rates by distance, and with a teaching gesture (keyed draws; 400 trials each)
            double Rate(int dist, bool teach)
            {
                int learned = 0, trials = 400;
                for (int i = 0; i < trials; i++)
                {
                    var g = UN6Games.Game((ulong)(1000 + i), 1, t => t.Unification.UN6.InnateInteractions = 0);
                    var gs = g.State; var gb = UN6Games.Attach(g);
                    var pp = UN6Games.People(gs);
                    pp[0].Node = 0; pp[1].Node = dist;
                    if (teach) gs.Unification.PersonOf(pp[0].Id).Brain.TeachUntil = gs.Tick + 10;
                    learned += Brain.Demonstrated(gs, null, pp[0].Id, KnowledgeKind.Interaction, k, 2, ControlOrigin.Policy);
                }
                return learned / 400.0;
            }
            double near = Rate(1, false), far = Rate(5, false), taught = Rate(1, true);
            TestContext.WriteLine($"UN6-T6 observation rates: distance 1 {near:F3}, distance 5 {far:F3}, distance 1 taught {taught:F3}");
            Assert.Greater(near, far, "nearer observers learn more often");
            Assert.Greater(taught, near, "a teaching gesture raises the chance");
            Assert.Greater(far, 0.0);

            // a teaching gesture opens the window and emits TeachingGesture; the taught observer gets an imitation bias
            var g2 = UN6Games.Game(61, 1, t => { t.Unification.UN6.InnateInteractions = 0; t.Unification.UN6.ObserveChance = 1000; });
            var s2 = g2.State; var b2 = UN6Games.Attach(g2);
            var p2 = UN6Games.People(s2);
            p2[0].Node = 10; p2[1].Node = 10;
            b2.Things.Add(new FixtureBody.Thing { Class = 2, Node = 10, Id = 5, Kind = TargetKind.WaterPoint });
            s2.Unification.PersonOf(p2[1].Id).ActionKind = Prim.Wait; s2.Unification.PersonOf(p2[1].Id).ActionTicksLeft = 100000;
            var rt2 = s2.Unification.BrainRuntime;
            rt2.Policy = new ScriptedPolicy(
                new PrimitiveControl { Control = Prim.Gesture, Gesture = (byte)GestureAct.Teach, Target = new TargetRef(TargetKind.Npc, p2[1].Id) },
                new PrimitiveControl { Control = Prim.Use, Target = new TargetRef(TargetKind.WaterPoint, 5) });
            var ev2 = new List<SimEvent>();
            Step(g2, 20, ev2);
            Assert.IsTrue(ev2.OfType<TeachingGesture>().Any(e => e.Actor == p2[0].Id));
            var taughtEv = ev2.OfType<KnowledgeAcquired>().First(e => e.Npc == p2[1].Id);
            Assert.AreEqual(KnowledgeSource.Taught, taughtEv.Source); Assert.AreEqual(p2[0].Id, taughtEv.From);
            var ost = s2.Unification.OverlayOf(p2[1].Id);
            int at = ost.Find(Feature.Class(2), (byte)Prim.Use);
            Assert.GreaterOrEqual(at, 0, "imitation bias entry");
            Assert.AreEqual(Origin.Imitated, ost.Entries[at].Origin);

            // an unknown design is not legal: knowledge is a legality fact (UN2's Motor.Legal masks it with NotKnown)
            var bs = new BrainState();
            ushort design = CatId.Building(12);
            Assert.IsFalse(Knowledge.KnowsDesign(bs, design));
            Brain.Learn(s2, null, p2[0].Id, KnowledgeKind.Design, CatId.Row(design), KnowledgeSource.Start, 0, ControlOrigin.Policy);
            Assert.IsTrue(Knowledge.KnowsDesign(s2.Unification.PersonOf(p2[0].Id).Brain, design));
        }

        // ---------------------------------------------------------------- T7

        [Test]
        public void Child_NoAdultMemory()
        {
            var sim = UN6Games.Game(71, 3);
            var s = sim.State;
            var body = UN6Games.Attach(sim);
            body.Things.Add(new FixtureBody.Thing { Class = 2, Node = UN6Games.People(s)[0].Node, Id = 5, Kind = TargetKind.WaterPoint });
            sim.Step(200); // adults decide, remember places and people, learn
            var adults = UN6Games.People(s);
            Claim cl = new Claim { Claimant = adults[0].Id, SubjectKind = TargetKind.Lot, SubjectId = 3, Confidence = 700, Tick = s.Tick };
            foreach (var a in adults) { Brain.AddClaim(s, a.Id, in cl); Brain.Learn(s, null, a.Id, KnowledgeKind.Design, 7, KnowledgeSource.Start, 0, ControlOrigin.Policy); s.Unification.PersonOf(a.Id).Brain.Skill[3] = 300; }
            Assert.IsTrue(adults.Any(a => s.Unification.PersonOf(a.Id).Brain.Places.Any(p => p.Class != 0)), "adults have place memory");
            var child = PopulationModule.AddNpc(s, adults[0].Faction, adults[0].Node, InstinctSet.Default, LearningMode.On, false);
            child.Stage = LifeStage.Child;
            var cb = s.Unification.PersonOf(child.Id).Brain;
            var innate = BrainState.Innate(s.Tuning.Unification.UN6.InnateInteractions);
            innate.Overlay = child.Id;
            Assert.AreEqual(innate.Hash(), cb.Hash(), "the child's brain is exactly the innate start state");
            Assert.AreEqual(0, s.Unification.OverlayOf(child.Id).Count, "empty overlay");
            foreach (var a in adults)
            {
                var ab = s.Unification.PersonOf(a.Id).Brain;
                Assert.AreNotEqual(ab.Hash(), cb.Hash());
                for (int w = 0; w < cb.Designs.Length; w++) Assert.AreEqual(0UL, cb.Designs[w] & ab.Designs[w], "no design bit copied");
                Assert.IsTrue(cb.Places.All(p => p.Class == 0) && cb.People.All(p => p.Npc == 0) && cb.Claims.All(c => c.SubjectKind == TargetKind.None) && cb.Skill.All(x => x == 0));
                Assert.IsTrue(cb.Recurrent.All(x => x == 0));
            }
            // static: no code under Unified/Brain copies one person's brain or overlay into another's
            foreach (var f in Directory.GetFiles(BrainDir, "*.cs", SearchOption.AllDirectories))
            {
                var t = StripComments(File.ReadAllText(f));
                Assert.IsFalse(Regex.IsMatch(t, @"Imitation\.Copy|\.Brain\s*=|Recurrent\s*=\s*[a-z]+\.|Array\.Copy\([^)]*\.(Places|People|Claims|Designs|Recipes|Interactions|Skill)"), Path.GetFileName(f) + " copies brain state");
            }

            // matched pairs over 20 seeds: an exposed child (demonstrator at distance 1, teaching) vs an unexposed control (out of sight)
            int exposed = 0, control = 0;
            for (ulong seed = 1; seed <= 20; seed++)
            {
                var g = UN6Games.Game(700 + seed, 2, t => t.Unification.UN6.InnateInteractions = 0);
                var gs = g.State; var gb = UN6Games.Attach(g);
                var pp = UN6Games.People(gs);
                var demo = pp[0];
                var c1 = PopulationModule.AddNpc(gs, demo.Faction, 0, InstinctSet.Default, LearningMode.On, false); c1.Stage = LifeStage.Child;
                var c2 = PopulationModule.AddNpc(gs, demo.Faction, 0, InstinctSet.Default, LearningMode.On, false); c2.Stage = LifeStage.Child;
                c2.Curiosity = c1.Curiosity; c2.Traits = (int[])c1.Traits.Clone(); // matched
                foreach (var o in pp) o.Node = 500 + o.Id * 30;
                demo.Node = 10; c1.Node = 11; c2.Node = 200;
                gs.Unification.PersonOf(demo.Id).Brain.TeachUntil = gs.Tick + 1000;
                for (int r = 0; r < 3; r++) Brain.Demonstrated(gs, null, demo.Id, KnowledgeKind.Interaction, (int)EffectKind.DrinkWash, 2, ControlOrigin.Policy);
                if (Knowledge.Knows(gs.Unification.PersonOf(c1.Id).Brain, KnowledgeKind.Interaction, (int)EffectKind.DrinkWash)) exposed++;
                if (Knowledge.Knows(gs.Unification.PersonOf(c2.Id).Brain, KnowledgeKind.Interaction, (int)EffectKind.DrinkWash)) control++;
            }
            TestContext.WriteLine("UN6-T7 matched pairs (20 seeds, 3 taught demonstrations at distance 1): exposed acquired " + exposed + "/20, control " + control + "/20");
            Assert.Greater(exposed, control, "the exposed child acquires the demonstrated interaction more often");
        }

        // ---------------------------------------------------------------- T8

        [Test]
        public void Brain_NoHiddenOracle()
        {
            var bad = new List<string>();
            foreach (var f in Directory.GetFiles(BrainDir, "*.cs", SearchOption.AllDirectories).Concat(new[] {
                Path.Combine(Root, "Assets/God/Sim/Unified/UnifiedHooks.UN6.cs"), Path.Combine(Root, "Assets/God/Sim/Unified/UnifiedState.UN6.cs") }))
            {
                var t = StripComments(File.ReadAllText(f));
                foreach (var w in new[] { "NodePather", "Truth(", ".Owner", "OwnerRelation", "Permits." })
                    if (t.Contains(w)) bad.Add(Path.GetFileName(f) + ": " + w);
            }
            Assert.IsEmpty(bad, "hidden oracle or title read on the brain path");
            Assert.AreEqual(0, new SimTuning().Unification.Brain.Planning, "planning off by default");

            // planning on: decisions are identical (Obs v1 has no intention field yet); only the intention record differs
            GodSim Run(int planning)
            {
                var sim = UN6Games.Game(81, 3, t => t.Unification.Brain.Planning = planning);
                var body = UN6Games.Attach(sim);
                foreach (var u in sim.State.Unification.People) u.Needs[NeedChannel.Thirst - NeedChannel.Thirst] = 950;
                body.Things.Add(new FixtureBody.Thing { Class = 2, Node = UN6Games.People(sim.State)[0].Node + 1, Id = 5, Kind = TargetKind.WaterPoint });
                sim.Step(150);
                return sim;
            }
            var off = Run(0); var on = Run(1);
            var co = off.State.Unification.BrainRuntime; var cn = on.State.Unification.BrainRuntime;
            Assert.AreEqual(((FixtureBody)co.Body).ByControl, ((FixtureBody)cn.Body).ByControl, "planning issues no control");
            Assert.AreEqual(co.Decisions, cn.Decisions);
            Assert.IsTrue(off.State.Unification.People.All(u => u.Brain.IntentSlot == -1));
            Assert.Greater(cn.PlanExpansions, 0); Assert.AreEqual(0, co.PlanExpansions);
            Assert.LessOrEqual(cn.PlanExpansions, cn.Decisions * 64, "at most 64 expansions per decision");
            Assert.IsTrue(on.State.Unification.People.Any(u => u.Brain.IntentSlot >= 0 && u.Brain.IntentNeed == (byte)NeedChannel.Thirst), "an intention was formed (thirst, the water)");
        }

        // ---------------------------------------------------------------- T9

        [Test]
        public void Claims_Beliefs()
        {
            var sim = UN6Games.Game(91, 2);
            var s = sim.State;
            int me = UN6Games.People(s)[0].Id;
            Claim C(int claimant, int subject, short conf, long tick, long expiry = 0, ClaimSource src = ClaimSource.Told) =>
                new Claim { Claimant = claimant, SubjectKind = TargetKind.Building, SubjectId = subject, Source = src, Confidence = conf, Tick = tick, Expiry = expiry };
            Assert.IsFalse(Brain.AddClaim(s, me, new Claim()), "a claim names a subject");
            Assert.IsFalse(Brain.AddClaim(s, 99999, C(1, 1, 500, 0)));
            // contradictory claims coexist: two claimants of building 4
            Assert.IsTrue(Brain.AddClaim(s, me, C(11, 4, 600, 1)));
            Assert.IsTrue(Brain.AddClaim(s, me, C(12, 4, 600, 1)));
            Assert.AreEqual(2, Brain.Claims(s, me).Count(c => c.SubjectId == 4));
            // fill the ring: 8 slots
            for (int i = 0; i < 6; i++) Brain.AddClaim(s, me, C(20 + i, 10 + i, (short)(500 + i * 10), 2 + i));
            Assert.AreEqual(BrainState.ClaimSlots, Brain.Claims(s, me).Length);
            // eviction: lowest confidence (500: claimant 20) goes first
            Brain.AddClaim(s, me, C(30, 30, 900, 20));
            var now = Brain.Claims(s, me);
            Assert.IsFalse(now.Any(c => c.Claimant == 20), "lowest confidence evicted");
            Assert.IsTrue(now.Any(c => c.Claimant == 30));
            Brain.AddClaim(s, me, C(31, 31, 600, 20));
            now = Brain.Claims(s, me);
            Assert.IsFalse(now.Any(c => c.Claimant == 21), "then the next lowest confidence (510)");
            Assert.IsTrue(now.Any(c => c.Claimant == 11) && now.Any(c => c.Claimant == 12), "contradictory claims still coexist");
            // ties: same confidence -> the oldest, then the lowest claimant id
            int tie = UN6Games.People(s)[2].Id;
            for (int i = 0; i < 8; i++) Brain.AddClaim(s, tie, C(57 - i, 100 + i, 500, i < 2 ? 1 : 2 + i));
            Brain.AddClaim(s, tie, C(60, 200, 500, 30));
            now = Brain.Claims(s, tie);
            Assert.IsFalse(now.Any(c => c.Claimant == 56), "oldest (tick 1), lowest claimant (56 before 57)");
            Assert.IsTrue(now.Any(c => c.Claimant == 57));
            Brain.AddClaim(s, tie, C(61, 201, 500, 31));
            Assert.IsFalse(Brain.Claims(s, tie).Any(c => c.Claimant == 57), "then 57");
            // the same claimant, subject and source refresh in place
            Brain.AddClaim(s, me, C(30, 30, 950, 21));
            Assert.AreEqual(1, Brain.Claims(s, me).Count(c => c.Claimant == 30)); Assert.AreEqual(950, Brain.Claims(s, me).Single(c => c.Claimant == 30).Confidence);
            // expiry: an expired claim is not live and its slot is reused first
            var other = UN6Games.People(s)[1].Id;
            Brain.AddClaim(s, other, C(40, 1, 800, 0, expiry: s.Tick + 5));
            Assert.AreEqual(1, Brain.Claims(s, other).Length);
            sim.Step(6);
            Assert.AreEqual(0, Brain.Claims(s, other).Length, "expired");
            // no API returns a canonical owner or claim truth
            var api = typeof(Brain).GetMethods().Select(m => m.Name).ToList();
            Assert.IsFalse(api.Any(n => Regex.IsMatch(n, "Owner|True|Truth|Canonical|Resolve|Winner|Rightful")), string.Join(",", api));
            Assert.IsFalse(typeof(Claim).GetFields().Any(f => Regex.IsMatch(f.Name, "Owner|True|Valid")));
            // claims are hashed and saved
            var loaded = SaveFile.FromJson(SaveFile.From(sim).ToJson()).ToSim();
            Assert.AreEqual(sim.StateHash(), loaded.StateHash());
            Assert.AreEqual(Brain.Claims(s, me).Length, Brain.Claims(loaded.State, me).Length);
        }

        // ---------------------------------------------------------------- T10

        [Test]
        public void Policy_OriginNeutral()
        {
            // MECHANICAL (fixture body): the same test-artifact brain under Kind = 1 with origin Policy vs Fixture: identical world
            ulong Run(ControlOrigin origin)
            {
                var sim = UN6Games.Game(101, 3, t => { t.Unification.Brain.Kind = 1; t.Unification.Brain.Artifact = UN6TestArtifacts.Valid; });
                UN6Games.Attach(sim);
                sim.State.Unification.BrainRuntime.Origin = origin;
                sim.Step(200);
                return sim.StateHash();
            }
            Assert.AreEqual(Run(ControlOrigin.Policy), Run(ControlOrigin.Fixture), "origin changes nothing physical (re-run on the real motor after UN2 merges)");
        }

        [Test]
        public void UN6_SaveLoadParity()
        {
            foreach (var kind in new[] { 0, 1, 2 })
            {
                var sim = UN6Games.Game(111, 3, t => { t.Unification.Brain.Kind = kind; t.Unification.Brain.Artifact = kind == 1 ? UN6TestArtifacts.Valid : 0; });
                var body = UN6Games.Attach(sim);
                body.ActionTicks = 9;
                body.Things.Add(new FixtureBody.Thing { Class = 2, Node = UN6Games.People(sim.State)[0].Node, Id = 5, Kind = TargetKind.WaterPoint });
                body.OnUse = (st, n, c) => n.Drives[Drive.Hunger] = Math.Max(0, n.Drives[Drive.Hunger] - 50000);
                sim.Step(203);
                var s = sim.State;
                var p0 = UN6Games.People(s)[0];
                for (int i = 0; i < BrainState.ClaimSlots; i++)
                    Brain.AddClaim(s, p0.Id, new Claim { Claimant = 100 + i, SubjectKind = TargetKind.Lot, SubjectId = i, Confidence = (short)(300 + i), Tick = s.Tick });
                var u0 = s.Unification.PersonOf(p0.Id);
                Assert.AreEqual(BrainState.ClaimSlots, Brain.Claims(s, p0.Id).Length, "full claims ring");
                if (kind != 2) Assert.IsTrue(u0.Brain.Recurrent.Any(x => x != 0), "non-zero recurrent state");
                Assert.IsTrue(s.Unification.People.Any(u => u.Busy), "someone mid-action");
                if (kind != 2) Assert.IsTrue(s.Unification.Overlays.Any(o => o.Store.Count > 0), "a half-learned overlay");
                var json = SaveFile.From(sim).ToJson();
                var loaded = SaveFile.FromJson(json).ToSim();
                var lb = UN6Games.Attach(loaded);
                lb.ActionTicks = 9; lb.Things.AddRange(body.Things); lb.OnUse = body.OnUse;
                Assert.AreEqual(sim.StateHash(), loaded.StateHash(), "kind " + kind + " at the save");
                for (int c = 0; c < 4; c++)
                {
                    sim.Step(97); loaded.Step(97);
                    Assert.AreEqual(sim.StateHash(), loaded.StateHash(), "kind " + kind + " checkpoint " + c);
                }
                Assert.Greater(sim.State.Unification.BrainRuntime.Decisions, 0);
            }
        }

        [Test]
        public void UN6_EventsRoundTrip()
        {
            var types = new[] { typeof(BrainAttached), typeof(KnowledgeAcquired), typeof(DemonstrationObserved), typeof(TeachingGesture) };
            var reg = EventMapRegistry.CreateDefault();
            var (lo, hi) = UnifiedEvents.BlockOf("UN6");
            for (int i = 0; i < types.Length; i++)
            {
                Assert.IsTrue(EventCatalog.TryOf(types[i], out var info), types[i].Name);
                Assert.AreEqual(1320 + i, info.Code); Assert.AreEqual(TelemetryDomain.Brain, info.Domain);
                Assert.That(info.Code, Is.InRange(lo, hi));
                Assert.AreEqual(EventMapRegistry.GenericTable, reg.Resolve(types[i]).Table, "generic until UN11a's 0046");
                if (i > 0) CollectionAssert.Contains(types[i].GetFields().Select(f => f.Name).ToList(), "Origin", "action-derived events carry their origin");
            }
            Assert.IsTrue(EventCatalog.TryOf(typeof(DemonstrationObserved), out var hv) && hv.HighVolume);
            var e = new KnowledgeAcquired { Npc = 3, What = KnowledgeKind.Recipe, Index = 9, Source = KnowledgeSource.Taught, From = 4, Origin = ControlOrigin.Policy, Tick = 5, Seq = 1 };
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(e);
            Assert.AreEqual(json, Newtonsoft.Json.JsonConvert.SerializeObject(Newtonsoft.Json.JsonConvert.DeserializeObject<KnowledgeAcquired>(json)));
        }

        // ---------------------------------------------------------------- T11

        [Test]
        public void Fallback_Labelled()
        {
            var sim = UN6Games.Game(121, 3, t => t.Unification.Brain.Kind = 2);
            var body = UN6Games.Attach(sim);
            var events = new List<SimEvent>();
            Step(sim, 100, events);
            var s = sim.State;
            Assert.AreEqual(2, s.Unification.Brain.Kind); Assert.AreEqual("FALLBACK", s.Unification.Brain.Label);
            var json = SaveFile.From(sim).ToJson();
            StringAssert.Contains("\"Label\":\"FALLBACK\"", json, "the save names the fallback");
            Assert.AreEqual("FALLBACK", events.OfType<BrainAttached>().Single().Label);
            var badge = Brain.Badge(s);
            Assert.AreEqual(BrainKind.Fallback, badge.Kind); Assert.IsFalse(badge.Trained);
            StringAssert.Contains("FALLBACK", Brain.Report(s)); StringAssert.Contains("not trained", Brain.Report(s));
            Assert.Greater(body.Submits, 0, "the fallback acts through primitives");
            // nothing can make a fallback (or untrained, or a test artifact) report as trained
            foreach (var kind in new[] { 0, 2 })
            {
                var g = UN6Games.Game(122, 2, t => t.Unification.Brain.Kind = kind);
                g.State.Unification.Brain.Kind = 1; // a tampered record
                Assert.IsFalse(Brain.Badge(g.State).Trained, "kind " + kind);
            }
            Assert.IsFalse(Brain.Badge(UN6Games.Game(123, 2, t => { t.Unification.Brain.Kind = 1; t.Unification.Brain.Artifact = UN6TestArtifacts.Valid; }).State).Trained, "SELF-TEST is not trained");
            // the fallback never wraps the legacy rules
            var src = StripComments(File.ReadAllText(Path.Combine(BrainDir, "Policies.cs")));
            foreach (var w in new[] { "Verb.Eat", "Verb.Flee", "Verb.Build", "AssociativeNpcPolicy", "Instincts", "Thinker" }) Assert.IsFalse(src.Contains(w), w);
        }

        // ---------------------------------------------------------------- D10 benchmark

        [Test]
        public void Brain_Bench_D10()
        {
            // inference alone: 1,200 persons (own recurrent state each) x 10 decisions, sampled
            var net = UntrainedNet();
            const int persons = 1200, rounds = 10;
            var states = Enumerable.Range(0, persons).Select(_ => new short[BrainState.RecurrentSize]).ToArray();
            var r = new Random(1); var inputs = Enumerable.Range(0, 64).Select(_ => RandomObs(r)).ToArray();
            var rng = new Rng(1); var m = AllLegal();
            for (int i = 0; i < persons; i++) net.Act(inputs[i & 63], states[i], in m, rng);
            long macs = 0;
            var sw = Stopwatch.StartNew();
            for (int k = 0; k < rounds; k++) for (int i = 0; i < persons; i++) { net.Act(inputs[(i + k) & 63], states[i], in m, rng); macs += net.Macs; }
            sw.Stop();
            double nsInf = sw.Elapsed.TotalMilliseconds * 1e6 / (persons * rounds);
            // the full decision path (fixture body): sense, credit, overlay bias, policy, submit, trace
            var sim = UN6Games.Game(131, 36); // 72 persons
            UN6Games.Attach(sim);
            var s = sim.State; var rt = s.Unification.BrainRuntime;
            var people = UN6Games.People(s);
            for (int i = 0; i < 20; i++) foreach (var n in people) BrainDecider.Decide(s, null, n, s.Unification.PersonOf(n.Id), rt);
            long d0 = rt.Decisions;
            sw.Restart();
            for (int i = 0; i < 100; i++) foreach (var n in people) BrainDecider.Decide(s, null, n, s.Unification.PersonOf(n.Id), rt);
            sw.Stop();
            double nsFull = sw.Elapsed.TotalMilliseconds * 1e6 / (rt.Decisions - d0);
            int bytes = new BrainState().Bytes + new AssociationStore(rt.Caps).Bytes;
            TestContext.WriteLine($"UN6 bench: inference {nsInf:F0} ns/decision ({macs / (persons * rounds)} MAC mean, {BrainShape.Default.MacPerDecision} worst); "
                + $"full decision (72 persons, fixture body) {nsFull:F0} ns/decision; state {new BrainState().Bytes} B + overlay {new AssociationStore(rt.Caps).Bytes} B = {bytes} B/person; "
                + $"1,200 persons all deciding in one tick ~{nsFull * 1200 / 1e6:F2} ms, state {bytes * 1200 / 1024} KiB");
            Assert.Less(nsInf, 2_000_000, "sanity");
        }
    }
}
