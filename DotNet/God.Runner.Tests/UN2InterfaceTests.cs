using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using God.Runner.Tests.Support;
using God.Runner.Warehouse;
using God.Sim;
using God.Sim.Comms;
using God.Sim.Divinity;
using God.Sim.Population;
using God.Sim.Unified;
using NUnit.Framework;
using TargetKind = God.Sim.Unified.TargetKind;
using BuildingDef = God.Sim.Unified.BuildingDef;

namespace God.Runner.Tests
{
    /// <summary>
    /// UN2b (UN2 spec 13: T7, T8, T11-T13, T15-T18, the headless demonstration): the USE resolver over a code-defined test catalogue,
    /// interruption, the observation encoder, the legality mask, the possession adapter, save/replay parity, events, scale and the
    /// static guard. Fixture streams are scripted mechanics, never autonomous behaviour.
    /// </summary>
    [Parallelizable(ParallelScope.None)]
    public class UN2InterfaceTests
    {
        // ---------------------------------------------------------------- the test catalogue and the counting stub

        /// <summary>A counting effect (tests only): counts validations and applications; an application drops one rock at the actor's node.</summary>
        public sealed class CountingEffect : IEffectHandler
        {
            public int Validates, Applies;
            public readonly List<short> Applied = new List<short>();
            public MotorStatus Refuse = MotorStatus.Accepted;

            public MotorStatus Validate(GodState s, in EffectCall c) { Validates++; return Refuse; }

            public MotorStatus Apply(GodState s, EventBuffer ev, in EffectCall c)
            {
                Applies++; Applied.Add(c.Effect.A);
                var n = s.Population.Find(c.Actor);
                Goods.Produce(s, ev, Holder.Ground(n.Node), Ware.Rock, 1, 0, 0, Owner.None, ProduceReason.Crafted, c.Actor, 0);
                return MotorStatus.Accepted;
            }
        }

        internal static readonly ushort LogCat = CatId.Object(1), RockCat = CatId.Object(2), AxeCat = CatId.Object(3), HutCat = CatId.Building(1);

        internal static InteractionBook TestBook()
        {
            var play = new Func<short, EffectDef>(a => new EffectDef(EffectKind.Play, a));
            var log = new ObjectDef(LogCat, Ware.Log, false, 4, 0, 0, 0, null, new[]
            {
                new InteractionDef(TargetKind.Lot, LogCat, 1, SourceReq.EmptyHand, 0, 1, 10, play(1)),
                new InteractionDef(TargetKind.Lot, LogCat, 2, SourceReq.Ware, (int)Ware.Rock, 1, 10, play(2)),
                new InteractionDef(TargetKind.Lot, LogCat, 3, SourceReq.Tool, AxeCat, 1, 10, play(3)),
            });
            var rock = new ObjectDef(RockCat, Ware.Rock, false, 2, 0, 0, 0, null, null);
            var axe = new ObjectDef(AxeCat, Ware.Axe, false, 2, 0, 10, 0, new[] { new ToolMul(EffectKind.Play, 500) }, null);
            var hut = new BuildingDef(HutCat, new BuildingLevelDef[0], PlacementRule.Land, 0, Capabilities.None, new[]
            {
                new InteractionDef(TargetKind.Building, HutCat, 1, SourceReq.Class, Resolver.SourceClass.Tool, 1, 4, play(4)),
            }, null);
            var place = new InteractionDef(TargetKind.Placement, 0, 1, SourceReq.EmptyHand, 0, 2, 3, play(5));
            return new InteractionBook(new[] { log, rock, axe }, new[] { hut }, place);
        }

        internal static EffectTable TableOf(CountingEffect h)
        {
            var r = new EffectRegistrar();
            r.Register(EffectKind.Play, h);
            return r.Freeze();
        }

        private static TargetRef T(TargetKind k, int id) => new TargetRef(k, id);

        private static int Rocks(PrimitiveFixture f) => f.S.Population.Lots.Where(x => x.Lot.Ware == Ware.Rock).Sum(x => x.Lot.Count);

        // ---------------------------------------------------------------- T7

        [Test]
        public void Use_ResolvesCatalogueRows()
        {
            var f = UN2MotorTests.Two(7, out var a, out var b);
            f.Place(b, UN2MotorTests.FarFrom(f, a.Node));
            var h = new CountingEffect();
            f.UseTables(TestBook(), TableOf(h));
            var g = f.Grid; int v = a.Node;
            int log = f.Ground(g.Neighbour(v, 0), Ware.Log, 1);
            // slot 0, empty hand: the first matching row (slot 1, empty hand); no effect and no output before completion
            Assert.AreEqual(MotorStatus.Accepted, f.Submit(f.C(Prim.Use, a, T(TargetKind.Lot, log))));
            Assert.AreEqual(1, h.Validates); Assert.AreEqual(10, f.Rec(a).ActionTicksLeft, "duration = Ticks");
            for (int k = 0; k < 9; k++) { f.Step(); Assert.AreEqual(0, h.Applies, "nothing before completion"); Assert.AreEqual(0, Rocks(f)); }
            f.Step();
            Assert.AreEqual(1, h.Applies); CollectionAssert.AreEqual(new short[] { 1 }, h.Applied); Assert.AreEqual(1, Rocks(f), "the output appears at completion");
            // slot 0 with a rock in hand: the Ware row (2); slot 1 with a rock: NoInteraction (slot 1 wants the empty hand)
            int rock = f.InCarry(a, Ware.Rock, 1);
            Assert.AreEqual(MotorStatus.NoInteraction, f.Submit(f.C(Prim.Use, a, T(TargetKind.Lot, log), rock, 1)));
            Assert.AreEqual(MotorStatus.Accepted, f.Submit(f.C(Prim.Use, a, T(TargetKind.Lot, log), rock)));
            f.UntilIdle(a);
            Assert.AreEqual(2, h.Applied.Last());
            // an axe: the Tool row (3), shortened by its multiplier (500 permille: 5 ticks); an explicit slot 3 too
            f.Submit(new PrimitiveControl { Control = Prim.Drop, Actor = a.Id, SourceLot = rock }); f.Step();
            int axe = f.InCarry(a, Ware.Axe, 1);
            Assert.AreEqual(MotorStatus.Accepted, f.Submit(f.C(Prim.Use, a, T(TargetKind.Lot, log), axe)));
            Assert.AreEqual(5, f.Rec(a).ActionTicksLeft, "tool multiplier halves the duration");
            Assert.AreEqual(5, f.UntilIdle(a)); Assert.AreEqual(3, h.Applied.Last());
            Assert.AreEqual(MotorStatus.Accepted, f.Submit(f.C(Prim.Use, a, T(TargetKind.Lot, log), axe, 3)));
            f.UntilIdle(a); Assert.AreEqual(3, h.Applied.Last());
            Assert.AreEqual(MotorStatus.NoInteraction, f.Submit(f.C(Prim.Use, a, T(TargetKind.Lot, log), axe, 2)), "the Ware row wants a rock, not an axe");
            // Class (a tool class) on a building: the axe meets it, the empty hand does not
            var hut = f.NewBuilding(HutCat, g.Neighbour(v, 3));
            Assert.AreEqual(MotorStatus.NoInteraction, f.Submit(f.C(Prim.Use, a, T(TargetKind.Building, hut.Id))));
            Assert.AreEqual(MotorStatus.Accepted, f.Submit(f.C(Prim.Use, a, T(TargetKind.Building, hut.Id), axe)));
            Assert.AreEqual(2, f.Rec(a).ActionTicksLeft, "4 ticks x 500 permille"); f.UntilIdle(a); Assert.AreEqual(4, h.Applied.Last());
            // reach: a log two edges away
            int far = f.Ground(g.Neighbour(g.Neighbour(v, 5), 5), Ware.Log, 1);
            Assert.AreEqual(MotorStatus.OutOfReach, f.Submit(f.C(Prim.Use, a, T(TargetKind.Lot, far))));
            // unmatched (a grain lot: no row) and an unknown design
            int grain = f.Ground(v, Ware.Grain, 1);
            Assert.AreEqual(MotorStatus.NoInteraction, f.Submit(f.C(Prim.Use, a, T(TargetKind.Lot, grain))));
            var pl = new PrimitiveControl { Control = Prim.Use, Actor = a.Id, Target = T(TargetKind.Placement, g.Neighbour(v, 1)), Design = HutCat };
            Assert.AreEqual(MotorStatus.NotKnown, f.Submit(pl));
            Knowledge.Learn(f.Rec(a).Brain, KnowledgeKind.Design, CatId.Row(HutCat));
            f.Submit(new PrimitiveControl { Control = Prim.Drop, Actor = a.Id, SourceLot = axe }); f.Step();
            Assert.AreEqual(MotorStatus.Accepted, f.Submit(pl), "a known design: the placement row (reach 2)");
            f.UntilIdle(a); Assert.AreEqual(5, h.Applied.Last());
            // an effect's validation refusal is the submission's result
            h.Refuse = MotorStatus.Occupied;
            Assert.AreEqual(MotorStatus.Occupied, f.Submit(f.C(Prim.Use, a, T(TargetKind.Lot, log))));
            // the default (generated, empty at UN0) catalogue and the empty registry: NoInteraction, never an exception
            var d = UN2MotorTests.Two(7, out var a2, out _);
            int l2 = d.Ground(a2.Node, Ware.Log, 1);
            Assert.AreEqual(Catalogue.IsEmpty ? MotorStatus.NoInteraction : Motor.Check(d.S, d.C(Prim.Use, a2, T(TargetKind.Lot, l2))), d.Submit(d.C(Prim.Use, a2, T(TargetKind.Lot, l2))));
            Assert.IsEmpty(f.Invariants());
            TestContext.WriteLine($"T7: rows applied {string.Join(",", h.Applied)}; durations 10/10/5/2; OutOfReach, NoInteraction, NotKnown, Occupied refused");
        }

        // ---------------------------------------------------------------- T8

        [Test]
        public void Use_InterruptedAndRepeatable()
        {
            var f = UN2MotorTests.Two(8, out var a, out var b);
            var h = new CountingEffect();
            f.UseTables(TestBook(), TableOf(h));
            int log = f.Ground(a.Node, Ware.Log, 1);
            var use = f.C(Prim.Use, a, T(TargetKind.Lot, log));
            Assert.AreEqual(MotorStatus.Accepted, f.Submit(use));
            f.Step(5);
            var ev = new EventBuffer { CurrentTick = f.S.Tick };
            Motor.Interrupt(f.S, ev, a.Id, InterruptReason.Strike);
            var ai = ev.Items.OfType<ActionInterrupted>().Single();
            Assert.AreEqual(Prim.Use, ai.Control); Assert.AreEqual(5, ai.TicksDone); Assert.AreEqual(InterruptReason.Strike, ai.Reason); Assert.AreEqual(ControlOrigin.Fixture, ai.Origin);
            Assert.IsFalse(Motor.Busy(f.S, a.Id)); f.Step(10);
            Assert.AreEqual(0, h.Applies, "the unfinished application applied nothing"); Assert.AreEqual(0, Rocks(f));
            // a full unit, then a second interrupted one: the first unit's output stays
            Assert.AreEqual(MotorStatus.Accepted, f.Submit(use)); f.UntilIdle(a);
            Assert.AreEqual(1, h.Applies); Assert.AreEqual(1, Rocks(f));
            Assert.AreEqual(MotorStatus.Accepted, f.Submit(use)); f.Step(3);
            Motor.Interrupt(f.S, null, a.Id, InterruptReason.Pain);
            f.Step(10);
            Assert.AreEqual(1, h.Applies); Assert.AreEqual(1, Rocks(f), "earlier completed units persist");
            // repeating continues from the world state
            Assert.AreEqual(MotorStatus.Accepted, f.Submit(use)); f.UntilIdle(a);
            Assert.AreEqual(2, h.Applies); Assert.AreEqual(2, Rocks(f));
            Assert.IsEmpty(f.Invariants());
            TestContext.WriteLine("T8: interrupted at 5/10 applied nothing; 2 completed units persist across a second interruption");
        }

        // ---------------------------------------------------------------- T11

        [Test]
        public void Percept_LocalOnly()
        {
            Func<int, PrimitiveFixture> world = extra =>
            {
                var f = UN2MotorTests.Two(11, out var a, out var b);
                var g = f.Grid; int v = a.Node;
                f.Ground(g.Neighbour(v, 1), Ware.Rock, 1);                 // in sight
                for (int i = 0; i < extra; i++) f.Ground(UN2MotorTests.FarFrom(f, v), Ware.Grain, 1); // ids shift
                int farNode = UN2MotorTests.FarFrom(f, v);
                for (int i = 0; i < extra; i++) f.ClearGroundAt(farNode);
                f.Ground(farNode, Ware.Log, 1);                            // beyond sight
                int store = f.NewBin(BinKind.Storage, UN2MotorTests.FarFrom(f, v)); f.InBin(store, Ware.Log, 1); // a far store
                int cache = f.NewBin(BinKind.Container, g.Neighbour(v, 2)); f.InBin(cache, Ware.Axe, 1);          // a concealed cache in reach
                return f;
            };
            var f0 = world(0); var f3 = world(3);
            var a0 = f0.Person(0); var a3 = f3.Person(0);
            var p0 = new Percept(); var p3 = new Percept();
            Percept.Encode(f0.S, a0.Id, ref p0); Percept.Encode(f3.S, a3.Id, ref p3);
            var o0 = new short[ObsSchema.Length]; var o3 = new short[ObsSchema.Length];
            Percept.ToVector(in p0, o0); Percept.ToVector(in p3, o3);
            Assert.AreEqual(ObsSchema.Length, o0.Length); Assert.AreEqual(284, ObsSchema.Length);
            CollectionAssert.AreEqual(o0, o3, "the ids of unperceived entities change nothing");
            int sight = p0.Sight;
            Assert.Greater(sight, 0);
            for (int e = 0; e < p0.EntityCount; e++)
            {
                Assert.LessOrEqual(f0.Grid.Distance(a0.Node, p0.EntityNode[e]), sight, "only within sight");
                Assert.AreNotEqual(SpatialKind.None, p0.EntityKind[e]);
                if (p0.EntityKind[e] == SpatialKind.Lot) Assert.IsTrue(f0.S.Population.Lots.Any(x => x.Lot.Id == p0.EntityId[e]), "lot entities are ground lots: no bin content is an entity");
            }
            Assert.IsTrue(Enumerable.Range(0, p0.EntityCount).Any(e => p0.EntityKind[e] == SpatialKind.Lot), "the near rock is seen");
            var axeLot = Goods.BinById(f0.S.Population, f0.S.Population.Bins.Last().Id).Lots[0].Id;
            Assert.IsFalse(p0.EntityId.Take(p0.EntityCount).Contains(axeLot), "the cache's contents are not perceived");
            // the holder perceives a resisted TAKE in the taker's state bits
            var b0 = f0.Person(1);
            f0.InCarry(b0, Ware.Grain, 1);
            Assert.AreEqual(MotorStatus.Resisted, f0.Submit(f0.C(Prim.Take, a0, T(TargetKind.Npc, b0.Id))));
            var pb = new Percept(); Percept.Encode(f0.S, b0.Id, ref pb);
            int ea = Enumerable.Range(0, pb.EntityCount).Single(e => pb.EntityKind[e] == SpatialKind.Npc && pb.EntityId[e] == a0.Id);
            int bits = pb.V[ObsSchema.OffsetOf("entities") + ea * ObsSchema.EntityFields + 3];
            Assert.AreNotEqual(0, bits & Percept.Bits.ResistedMe, "the victim's observation contains the failed attempt");
            // no owner, path or truth field in the encoder (static)
            string src = File.ReadAllText(Path.Combine(UN0RunTests.SimRoot(), "Unified", "Percept", "Percept.UN2.cs"));
            var code = string.Join("\n", UN0RunTests.CodeLines(src));
            foreach (var tok in new[] { ".Owner", "Truth(", "NodePather", ".Path", "Permits.", "Household", "Faction" }) StringAssert.DoesNotContain(tok, code, tok);
            TestContext.WriteLine($"T11: {p0.EntityCount} entities within sight {sight}; far log, far store and the cache absent; vectors equal under id shifts");
        }

        // ---------------------------------------------------------------- T12

        [Test]
        public void Percept_DeterministicOrder()
        {
            var f = UN2MotorTests.Two(12, out var a, out var b);
            var g = f.Grid; int v = a.Node;
            // many entities at equal distances, created in a scrambled order
            var nodes = Enumerable.Range(0, 6).Select(d => g.Neighbour(v, d)).Concat(Enumerable.Range(0, 6).Select(d => g.Neighbour(g.Neighbour(v, d), d))).ToList();
            var order = new[] { 7, 2, 11, 0, 5, 9, 3, 1, 10, 4, 8, 6 };
            var wares = new[] { Ware.Rock, Ware.Grain, Ware.Log };
            foreach (int k in order) f.Ground(nodes[k], wares[k % 3], 1);
            foreach (var n in f.People().Skip(2).Take(3)) f.Place(n, nodes[order[0]]);
            var p = new Percept();
            Percept.Encode(f.S, a.Id, ref p);
            Assert.AreEqual(ObsSchema.Entities, p.EntityCount, "16 nearest");
            for (int e = 1; e < p.EntityCount; e++)
            {
                int d0 = g.Distance(v, p.EntityNode[e - 1]), d1 = g.Distance(v, p.EntityNode[e]);
                Assert.IsTrue(d0 < d1 || d0 == d1 && (p.EntityKind[e - 1] < p.EntityKind[e] || p.EntityKind[e - 1] == p.EntityKind[e] && p.EntityId[e - 1] < p.EntityId[e]),
                    "sorted by distance, then kind, then id at " + e);
            }
            var first = (short[])p.V.Clone();
            f.U.Index.Reset();
            Percept.Encode(f.S, a.Id, ref p);
            CollectionAssert.AreEqual(first, p.V, "a rebuilt index gives the same vector");
            // the same world built in a different insertion order gives the same vector
            var f2 = UN2MotorTests.Two(12, out var a2, out _);
            foreach (var n in f2.People().Skip(2).Take(3).Reverse()) { f2.Place(n, nodes[order[0]]); Percept.Encode(f2.S, a2.Id, ref p); }
            foreach (int k in order) { f2.Ground(nodes[k], wares[k % 3], 1); Percept.Encode(f2.S, a2.Id, ref p); }
            var q = new Percept(); Percept.Encode(f2.S, a2.Id, ref q);
            CollectionAssert.AreEqual(first, q.V, "the order bodies were placed and the index was built in changes nothing");
            // encoding does not allocate (warm)
            for (int i = 0; i < 10; i++) Percept.Encode(f.S, a.Id, ref p);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) Percept.Encode(f.S, a.Id, ref p);
            long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.AreEqual(0, bytes, "Percept.Encode allocates nothing once warm");
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 2000; i++) Percept.Encode(f.S, a.Id, ref p);
            double us = sw.Elapsed.TotalMilliseconds * 1000 / 2000;
            TestContext.WriteLine($"T12: 16 entities sorted; equal across rebuild and insertion order; 0 bytes per 1000 encodes; {us:F2} us per encode");
        }

        // ---------------------------------------------------------------- T13

        [Test]
        public void Legal_MaskPhysicalOnly()
        {
            var f = UN2MotorTests.Two(13, out var a, out var b);
            var g = f.Grid; int v = a.Node;
            var p = new Percept(); var m = new ActMask();
            Percept.Encode(f.S, a.Id, ref p); Motor.Legal(f.S, a.Id, in p, ref m);
            Assert.IsFalse(ActMask.Allows(m.Control, (int)Prim.Take), "nothing to take: b's hands are empty, the ground is bare");
            f.InCarry(b, Ware.Grain, 1);
            Percept.Encode(f.S, a.Id, ref p); Motor.Legal(f.S, a.Id, in p, ref m);
            Assert.IsFalse(ActMask.Allows(m.Control, (int)Prim.Drop), "empty hand: no DROP");
            Assert.IsTrue(ActMask.Allows(m.Control, (int)Prim.Take), "b is in reach: a TAKE attempt is legal (it may be resisted)");
            for (var st = Prim.StepForward; st <= Prim.StepRight; st++)
                Assert.AreEqual(Motor.Check(f.S, f.C(st, a)) == MotorStatus.Accepted, ActMask.Allows(m.Control, (int)st), st.ToString());
            Assert.IsTrue(ActMask.Allows(m.Source, Motor.SourceEmpty)); Assert.IsFalse(ActMask.Allows(m.Source, 0));
            Assert.AreEqual(1UL << Motor.DesignNone, m.Design, "no known design");
            // goods in a legacy-owned bin, beside another person, are not masked
            int bin = f.NewBin(BinKind.Container, g.Neighbour(v, 1)); int lot = f.InBin(bin, Ware.Rock, 1);
            Goods.BinById(f.S.Population, bin).Owner = Owner.Household(1);
            f.Place(b, g.Neighbour(v, 1));
            var take = f.C(Prim.Take, a, T(TargetKind.Lot, lot));
            Assert.AreEqual(MotorStatus.Accepted, Motor.Check(f.S, take), "a household's bin is taken from like any other");
            // a known design, a carried lot, a sea step
            Knowledge.Learn(f.Rec(a).Brain, KnowledgeKind.Design, 7);
            f.InCarry(a, Ware.Rock, 1);
            int coast = f.CoastNode(out int dir); f.Place(a, coast, (byte)dir);
            Percept.Encode(f.S, a.Id, ref p); Motor.Legal(f.S, a.Id, in p, ref m);
            Assert.IsFalse(ActMask.Allows(m.Control, (int)Prim.StepForward), "sea ahead");
            Assert.IsTrue(ActMask.Allows(m.Control, (int)Prim.Drop));
            Assert.IsTrue(ActMask.Allows(m.Source, 0));
            Assert.IsTrue(ActMask.Allows(m.Design, 0)); Assert.IsFalse(ActMask.Allows(m.Design, 1));
            for (int e = 0; e < ObsSchema.Entities; e++) Assert.AreEqual(e < p.EntityCount, ActMask.Allows(m.Target, e), "target " + e + " = perceived");
            // busy and asleep: only the none control
            f.Submit(f.C(Prim.Wait, a));
            Percept.Encode(f.S, a.Id, ref p); Motor.Legal(f.S, a.Id, in p, ref m);
            Assert.AreEqual(1UL << (int)Prim.None, m.Control, "busy");
            f.UntilIdle(a); f.Submit(f.C(Prim.Sleep, a));
            Percept.Encode(f.S, a.Id, ref p); Motor.Legal(f.S, a.Id, in p, ref m);
            Assert.AreEqual(1UL << (int)Prim.None, m.Control, "asleep");
            // the mask has exactly the nine physical heads of Act v1 (no social, moral or advisability head)
            var heads = typeof(ActMask).GetFields().Where(x => !x.IsStatic).Select(x => x.Name).ToArray();
            CollectionAssert.AreEqual(new[] { "Control", "Target", "Source", "Slot", "Design", "Gesture", "Referent1", "Referent2", "Quantity" }, heads);
            TestContext.WriteLine("T13: mask follows existence, perception, reach, room, passability, busy, asleep and known designs only");
        }

        // ---------------------------------------------------------------- T15

        /// <summary>A host's click-to-move: one primitive at a time towards <paramref name="goal"/> (face the best neighbour, then step), labelled Possessed through the seam.</summary>
        private static int HostClickToMove(PrimitiveFixture f, Npc body, int goal, int maxTicks, Func<PrimitiveControl, MotorStatus> submit)
        {
            var g = f.Grid; int n = 0;
            for (int t = 0; t < maxTicks && body.Node != goal; t++)
            {
                if (!Motor.Busy(f.S, body.Id))
                {
                    int bestDir = -1, bestD = int.MaxValue;
                    for (int d = 0; d < 6; d++) { int x = g.Neighbour(body.Node, d); if (x >= 0 && Motor.Passable(f.S, x) && g.Distance(x, goal) < bestD) { bestD = g.Distance(x, goal); bestDir = d; } }
                    int face = f.Rec(body).Facing;
                    var c = new PrimitiveControl { Actor = body.Id };
                    c.Control = bestDir == face ? Prim.StepForward : ((bestDir - face + 6) % 6 <= 3 ? Prim.TurnRight : Prim.TurnLeft);
                    submit(c); n++;
                }
                f.Step();
            }
            return n;
        }

        [Test]
        public void Possession_PrimitiveAdapter()
        {
            Func<PrimitiveFixture> build = () =>
            {
                var f = PrimitiveFixture.Create(15, 3, divinity: true);
                var body = f.Person(0); f.Place(body, f.InteriorNode());
                foreach (var n in f.People().Skip(1)) f.Place(n, UN2MotorTests.FarFrom(f, body.Node));
                Assert.IsTrue(Control.TryAcquire(f.S, new EventBuffer(), new ControlEntry { Kind = ControlKind.Possessed, God = 1, Body = body.Id, Intervention = 1 }, out var why), why);
                return f;
            };
            var f1 = build(); var a = f1.Person(0);
            // the command route: validated by the same motor checks (CheckPossessed), applied with origin Possessed
            var handler = (ICommandHandler)new DivineControlInputHandler();
            var turn = new PrimitiveControl { Control = Prim.TurnRight, Actor = a.Id };
            var cmd = new DivineControlInput { God = 1, Body = a.Id, Kind = ControlInputKind.Primitive, Primitive = turn };
            Assert.AreEqual(Motor.Check(f1.S, in turn), UnifiedHooks.CheckPossessed(f1.S, a.Id, in turn));
            Assert.IsTrue(handler.Validate(f1.S, cmd).Ok);
            var ev = new EventBuffer { CurrentTick = f1.S.Tick };
            Assert.IsTrue(handler.Apply(f1.S, cmd, ev).Ok);
            var applied = ev.Items.OfType<ControlApplied>().Single();
            Assert.AreEqual(ControlOrigin.Possessed, applied.Origin); Assert.AreEqual(Prim.TurnRight, applied.Control);
            Assert.IsTrue(Motor.Busy(f1.S, a.Id));
            // the same statuses: a busy body refuses the command exactly as the motor refuses a fixture
            Assert.AreEqual(MotorStatus.Busy, UnifiedHooks.CheckPossessed(f1.S, a.Id, in turn));
            Assert.IsFalse(handler.Validate(f1.S, cmd).Ok);
            var wrong = new PrimitiveControl { Control = Prim.TurnRight, Actor = a.Id + 1 };
            Assert.AreEqual(MotorStatus.Invalid, UnifiedHooks.CheckPossessed(f1.S, a.Id, in wrong), "a possession input acts only on its body");
            // click-to-move: the host expands it into primitives labelled Possessed; the same stream from a fixture gives the same world
            var fp = build(); var bp = fp.Person(0);
            int goal = fp.Grid.Neighbour(fp.Grid.Neighbour(fp.Grid.Neighbour(bp.Node, 2), 2), 1);
            int sent = HostClickToMove(fp, bp, goal, 200, c => UnifiedHooks.SubmitPossessed(fp.S, null, bp.Id, ControlKind.Possessed, in c));
            Assert.AreEqual(goal, bp.Node, "arrived, one primitive at a time");
            var fh = build(); var bh = fh.Person(0);
            var rec = new List<ScriptedControl>();
            HostClickToMove(fh, bh, goal, 200, c => { rec.Add(new ScriptedControl(fh.S.Tick, c)); return UnifiedHooks.SubmitPossessed(fh.S, null, bh.Id, ControlKind.Possessed, in c); });
            var ff = build();
            ff.Play(rec, ControlOrigin.Fixture, fh.S.Tick);
            Assert.AreEqual(fh.Part("unification"), ff.Part("unification"), "possessed click-to-move = the same fixture stream");
            Assert.AreEqual(fh.Part("population"), ff.Part("population"));
            Assert.AreEqual(fp.Hash(), fh.Hash());
            // R87: Embodiment.CheckAction (primitive overload) is the validation, and it is the motor's own check
            Assert.AreEqual(Motor.Check(f1.S, in turn), Embodiment.CheckAction(f1.S, ControlKind.Possessed, in turn));
            Assert.AreEqual(MotorStatus.Invalid, Embodiment.CheckAction(f1.S, ControlKind.Self, in turn), "only a possessed or avatar body");
            Assert.AreEqual(MotorStatus.Disabled, Embodiment.CheckAction(UN2MotorTests.LegacyGame(), ControlKind.Possessed, in turn), "legacy games: disabled");
            TestContext.WriteLine($"T15: command route Possessed; click-to-move {sent} primitives to node {goal}; equal to the fixture stream {ff.Hash():x16}");
        }

        // ---------------------------------------------------------------- T16

        private static PrimitiveFixture SaveWorld(CountingEffect h)
        {
            var f = UN2MotorTests.DemoWorld();
            f.UseTables(TestBook(), TableOf(h));
            var c = f.Person(2); f.Place(c, f.Grid.Neighbour(f.Person(0).Node, 5));
            f.Ground(c.Node, Ware.Log, 1); // c's log (the demo log has the lower id)
            return f;
        }

        private static List<ScriptedControl> SaveStream(PrimitiveFixture f)
        {
            var l = UN2MotorTests.DemoStream(f);
            var a = f.Person(0); var c = f.Person(2);
            int log = f.S.Population.Lots.Last(x => x.Lot.Ware == Ware.Log).Lot.Id;
            l.Add(new ScriptedControl(50, new PrimitiveControl { Control = Prim.StepForward, Actor = c.Id }));
            l.Add(new ScriptedControl(56, new PrimitiveControl { Control = Prim.Gesture, Actor = c.Id, Gesture = (byte)GestureAct.Greet, R1 = new Referent { Kind = ReferentKind.Person, Id = a.Id }, Quantity = 12, Corr = 3 }));
            l.Add(new ScriptedControl(60, new PrimitiveControl { Control = Prim.TurnLeft, Actor = c.Id }));
            l.Add(new ScriptedControl(62, new PrimitiveControl { Control = Prim.Use, Actor = c.Id, Target = new TargetRef(TargetKind.Lot, log) }));
            l.Add(new ScriptedControl(64, new PrimitiveControl { Control = Prim.StepRight, Actor = f.Person(1).Id }));
            return l;
        }

        [Test]
        public void Unified_SaveLoadParity_Stub()
        {
            var probe = SaveWorld(new CountingEffect());
            var stream = SaveStream(probe);
            // save at 67: a asleep with a lot in hand, b mid-step, c mid-USE (5 of 10 ticks), c's gesture in a's inbox
            PrimitiveFixture.AssertSaveLoadParity(() => SaveWorld(new CountingEffect()), stream, 67, 120, 1, f =>
            {
                var a = f.Person(0); var b = f.Person(1); var c = f.Person(2);
                Assert.IsTrue(a.Action.Asleep, "asleep at the save");
                Assert.AreEqual(Prim.Use, f.Rec(c).ActionKind); Assert.AreEqual(5, f.Rec(c).ActionTotal - f.Rec(c).ActionTicksLeft, "mid-USE");
                Assert.IsTrue(f.Rec(b).ActionKind >= Prim.StepForward && f.Rec(b).ActionKind <= Prim.StepRight && f.Rec(b).Busy, "mid-step");
                Assert.IsTrue(a.Carry.Any(x => !x.Empty) || b.Carry.Any(x => !x.Empty), "a held lot");
                Assert.IsTrue(f.U.MessageExt.Count >= 2, "gestures with side records");
            });
            // a recorded stream replays to the same hash at every checkpoint
            var straight = SaveWorld(new CountingEffect());
            var hs = new List<ulong>();
            straight.Play(stream, ControlOrigin.Fixture, 120, 0, f => hs.Add(f.Hash()));
            var replay = SaveWorld(new CountingEffect());
            int i = 0;
            replay.Play(straight.Recorded, ControlOrigin.Policy, 120, 0, f => { Assert.AreEqual(hs[i], f.Hash(), "replay checkpoint " + i); i++; });
            Assert.AreEqual(hs.Count, i);
            TestContext.WriteLine($"T16 (U-P6 stub): save at 67 (asleep, mid-step, mid-USE 5/10, held lot, gesture), load = straight at every tick to 120; replay of {straight.Recorded.Count} controls equal at {i} checkpoints, final {straight.Hash():x16}");
        }

        [Test]
        public void UN2_EventsRoundTrip()
        {
            var types = new[] { typeof(ControlApplied), typeof(ControlFailed), typeof(ActionInterrupted), typeof(Equipped), typeof(Unequipped) };
            var reg = EventMapRegistry.CreateDefault();
            for (int i = 0; i < types.Length; i++)
            {
                Assert.IsTrue(EventCatalog.TryOf(types[i], out var info), types[i].Name);
                Assert.AreEqual(1220 + i, info.Code); Assert.AreEqual(TelemetryDomain.Motor, info.Domain);
                Assert.AreEqual(EventMapRegistry.GenericTable, reg.Resolve(types[i]).Table, "generic until UN11a's fact_control");
                var fields = types[i].GetFields().Where(x => x.DeclaringType == types[i]).Select(x => x.Name).ToList();
                CollectionAssert.Contains(fields, "Origin", "every UN2 event carries its origin");
            }
            var e = new ControlApplied { Actor = 3, Control = Prim.Take, Origin = ControlOrigin.Possessed, Status = MotorStatus.Accepted, TargetKind = TargetKind.Lot, TargetId = 9, Ware = 4, Tick = 5, Seq = 1 };
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(e);
            var back = Newtonsoft.Json.JsonConvert.DeserializeObject<ControlApplied>(json);
            Assert.AreEqual(json, Newtonsoft.Json.JsonConvert.SerializeObject(back));
            // the demo stream's events are all catalogued
            var f = UN2MotorTests.DemoWorld(); f.Play(UN2MotorTests.DemoStream(f), ControlOrigin.Fixture, 60);
            foreach (var x in f.Events.OfType<DomainEvent>()) Assert.IsTrue(EventCatalog.TryOf(x.GetType(), out _), x.GetType().Name);
            Assert.Greater(f.Of<ControlApplied>().Count(), 0); Assert.Greater(f.Of<ControlFailed>().Count(), 0);
        }

        // ---------------------------------------------------------------- T17

        private struct ScaleRun { public double MsPerTick, IndexMs, EncodeMs, SubmitMs, TickMs; public long MotorBytes, EncodeBytes, EntityTests, CellsTouched, Encodes; }

        private static ScaleRun Scale(int persons, int ticks)
        {
            var f = PrimitiveFixture.Create(17, persons / 2);
            var s = f.S;
            var people = f.People();
            Assert.AreEqual(persons, people.Count);
            var land = Enumerable.Range(0, f.Grid.Count).Where(v => f.Grid.IsLand(v)).ToList();
            var rnd = new Random(17);
            foreach (var n in people) { n.Node = land[rnd.Next(land.Count)]; f.Rec(n).Facing = (byte)rnd.Next(6); }
            s.Population.IndexDirty = true; f.U.Index.Reset();
            var percepts = new Percept[people.Count];
            var ev = new EventBuffer(); var sink = new List<SimEvent>(1 << 16);
            var ix = f.U.Index;
            var sw = new Stopwatch(); var sIx = new Stopwatch(); var sEnc = new Stopwatch(); var sSub = new Stopwatch(); var sTick = new Stopwatch();
            var r = new ScaleRun();
            long tests0 = 0, cells0 = 0;
            int warm = 50;
            for (int t = 0; t < ticks; t++)
            {
                if (t == warm) { tests0 = ix.EntityTests; cells0 = ix.CellsTouched; r.Encodes = 0; }
                s.Tick++; ev.CurrentTick = s.Tick;
                sw.Start();
                sIx.Start(); ix.Ensure(s); sIx.Stop();
                for (int i = 0; i < people.Count; i++)
                {
                    var n = people[i];
                    if (Motor.Busy(s, n.Id)) continue;
                    long b0 = GC.GetAllocatedBytesForCurrentThread();
                    sEnc.Start(); Percept.Encode(s, n.Id, ref percepts[i]); sEnc.Stop();
                    if (t >= warm) { r.EncodeBytes += GC.GetAllocatedBytesForCurrentThread() - b0; r.Encodes++; }
                    var c = new PrimitiveControl { Control = (Prim)(1 + rnd.Next(6)), Actor = n.Id };
                    sSub.Start(); Motor.Submit(s, ev, in c, ControlOrigin.Fixture); sSub.Stop();
                }
                long m0 = GC.GetAllocatedBytesForCurrentThread();
                sTick.Start(); Motor.Tick(s, ev); sTick.Stop();
                if (t >= warm) r.MotorBytes += GC.GetAllocatedBytesForCurrentThread() - m0;
                sw.Stop();
                ev.DrainInto(sink); sink.Clear();
            }
            r.MsPerTick = sw.Elapsed.TotalMilliseconds / ticks; r.IndexMs = sIx.Elapsed.TotalMilliseconds / ticks; r.EncodeMs = sEnc.Elapsed.TotalMilliseconds / ticks;
            r.SubmitMs = sSub.Elapsed.TotalMilliseconds / ticks; r.TickMs = sTick.Elapsed.TotalMilliseconds / ticks;
            r.EntityTests = ix.EntityTests - tests0; r.CellsTouched = ix.CellsTouched - cells0;
            return r;
        }

        [Test]
        public void Motor_Scale_NoAllPairs()
        {
            Scale(200, 60); // JIT warm-up
            var small = Scale(600, 1000);
            var big = Scale(1200, 1000);
            double ratio = big.MsPerTick / small.MsPerTick;
            double testsPerEncodeSmall = (double)small.EntityTests / Math.Max(1, small.Encodes), testsPerEncodeBig = (double)big.EntityTests / Math.Max(1, big.Encodes);
            double cellsPerEncode = (double)big.CellsTouched / Math.Max(1, big.Encodes);
            TestContext.WriteLine($"T17: 600 persons {small.MsPerTick:F3} ms/tick, 1200 persons {big.MsPerTick:F3} ms/tick (ratio {ratio:F2}); " +
                $"entity tests per encode {testsPerEncodeSmall:F1} / {testsPerEncodeBig:F1}; cells per encode {cellsPerEncode:F1}; " +
                $"steady-state bytes: Motor.Tick {big.MotorBytes}, Encode {big.EncodeBytes} over {big.Encodes} encodes; " +
                $"1200 split ms/tick: index {big.IndexMs:F3}, submit {big.SubmitMs:F3}, Motor.Tick {big.TickMs:F3}, encode {big.EncodeMs:F3} ({big.EncodeMs * 1000 * 950 / Math.Max(1, big.Encodes) :F2} us each); " +
                $"600: index {small.IndexMs:F3}, submit {small.SubmitMs:F3}, tick {small.TickMs:F3}, encode {small.EncodeMs:F3}");
            Assert.AreEqual(0, big.MotorBytes, "Motor.Tick allocates nothing in steady state");
            Assert.AreEqual(0, big.EncodeBytes, "Percept.Encode allocates nothing in steady state");
            Assert.LessOrEqual(ratio, 2.3, "2x persons, at most 2.3x time");
            Assert.Less(testsPerEncodeBig, 1200 / 4.0, "no all-pairs scan: an encode tests a bounded neighbourhood, not every person");
            Assert.LessOrEqual(cellsPerEncode, 3 * 10 * 10 + 3 * 10 + 1 + 6 + 7, "cells touched within the sight radius (plus the cell checks)");
        }

        // ---------------------------------------------------------------- T18

        [Test]
        public void UN2_NoLegacyTitle_Static()
        {
            new UN0RunTests().Unified_NoLegacyTitle_Static();
            string root = Path.Combine(UN0RunTests.SimRoot(), "Unified");
            var bad = new List<string>();
            foreach (var dir in new[] { "Motor", "Percept", "Interactions", "Spatial" })
                foreach (var p in Directory.GetFiles(Path.Combine(root, dir), "*.cs").OrderBy(x => x, StringComparer.Ordinal))
                    bad.AddRange(UN0RunTests.ScanUnified(dir + "/" + Path.GetFileName(p), File.ReadAllText(p)));
            Assert.IsEmpty(bad, string.Join("\n", bad));
        }

        // ---------------------------------------------------------------- the headless demonstration (spec 14)

        [Test]
        public void UN2_Demonstration()
        {
            Func<PrimitiveFixture> build = () => SaveWorld(new CountingEffect());
            var probe = build();
            var a = probe.Person(0); var b = probe.Person(1); var c = probe.Person(2);
            var log = probe.S.Population.Lots.First(x => x.Lot.Ware == Ware.Log).Lot.Id;
            var l = new List<ScriptedControl>();
            long t = 0;
            void Add(PrimitiveControl x, int gap) { l.Add(new ScriptedControl(t, x)); t += gap; }
            PrimitiveControl P(Prim p, Npc n, TargetRef tr = default, int src = 0) => new PrimitiveControl { Control = p, Actor = n.Id, Target = tr, SourceLot = src };
            // a: turn to face the log's way, walk, take it, drop it, sleep; c gestures an offer; b uses the log
            Add(P(Prim.TurnRight, a), 1); Add(P(Prim.TurnRight, a), 1);
            Add(P(Prim.StepForward, a), 7); Add(P(Prim.Take, a, T(TargetKind.Lot, log)), 2); Add(P(Prim.StepBack, a), 7);
            Add(P(Prim.StepLeft, a), 7); Add(P(Prim.StepRight, a), 7); Add(P(Prim.TurnLeft, a), 2);
            Add(P(Prim.Drop, a, src: log), 2);
            Add(new PrimitiveControl { Control = Prim.Gesture, Actor = c.Id, Gesture = (byte)GestureAct.Offer, Target = T(TargetKind.Npc, a.Id), R1 = new Referent { Kind = ReferentKind.Ware, Ware = (short)Ware.Rock }, R2 = new Referent { Kind = ReferentKind.Person, Id = a.Id }, Quantity = 120, Corr = 1 }, 2);
            Add(P(Prim.Use, a, T(TargetKind.Lot, log)), 12);
            Add(P(Prim.Sleep, a), 1);
            // fill to 200 controls: b and c alternate turns, waits, steps, takes and gestures (many refused: busy, blocked)
            var cycle = new[] { Prim.TurnLeft, Prim.StepForward, Prim.Wait, Prim.TurnRight, Prim.StepBack, Prim.Take, Prim.Gesture };
            int k = 0;
            while (l.Count < 200)
            {
                var who = k % 2 == 0 ? b : c;
                var p = cycle[k % cycle.Length];
                var x = P(p, who, p == Prim.Take ? T(TargetKind.Lot, log) : default);
                if (p == Prim.Gesture) { x.Gesture = (byte)GestureAct.Greet; x.R1 = new Referent { Kind = ReferentKind.Person, Id = a.Id }; }
                Add(x, k % 3 == 0 ? 2 : 1);
                k++;
            }
            long until = t + 20;
            var f = build();
            f.Play(l, ControlOrigin.Fixture, until);
            var lines = new List<string>();
            foreach (var e in f.Events)
            {
                if (e is ControlApplied ca) lines.Add($"t{e.Tick} applied {ca.Control} actor {ca.Actor} target {ca.TargetKind}:{ca.TargetId}");
                else if (e is ControlFailed cf) lines.Add($"t{e.Tick} failed {cf.Control} actor {cf.Actor} {cf.Status}");
                else if (e is ActionInterrupted ai) lines.Add($"t{e.Tick} interrupted {ai.Control} actor {ai.Actor}");
                else if (e is ItemMoved im && im.Actor > 0) lines.Add($"t{e.Tick} item {im.Lot} {im.Reason} by {im.Actor}");
                else if (e is Slept sl) lines.Add($"t{e.Tick} slept {sl.Npc} woke={sl.Woke}");
                else if (e is God.Sim.Social.GestureMade gm) lines.Add($"t{e.Tick} gesture {gm.Act} by {gm.Speaker} to {gm.Target}");
            }
            TestContext.WriteLine("UN2 demonstration: " + l.Count + " scripted controls (Fixture), " + lines.Count + " event lines:");
            foreach (var x in lines) TestContext.WriteLine("  " + x);
            ulong hf = f.Hash();
            var fs = PrimitiveFixture.AssertOriginNeutral(build, l, until);
            TestContext.WriteLine($"final hash Fixture {hf:x16}; Policy {fs[0].Hash():x16}; Possessed {fs[1].Hash():x16}; Fixture {fs[2].Hash():x16}");
            Assert.AreEqual(hf, fs[2].Hash());
            PrimitiveFixture.AssertSaveLoadParity(build, l, 90, until, 1);
            TestContext.WriteLine($"save at tick 90, load and continue to {until}: equal to the straight run at every tick");
            Assert.AreEqual(200, l.Count);
            Assert.Greater(f.Of<ControlApplied>().Count(), 50);
        }
    }
}
