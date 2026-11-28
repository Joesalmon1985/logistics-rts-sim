using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using God.Runner.Tests.Support;
using God.Sim;
using God.Sim.Comms;
using God.Sim.Learning;
using God.Sim.Population;
using God.Sim.Social;
using God.Sim.Unified;
using NUnit.Framework;
using TargetKind = God.Sim.Unified.TargetKind;

namespace God.Runner.Tests
{
    /// <summary>
    /// UN2a (UN2 spec 13: T1-T6, T9, T10, T14): the primitive executor driven by scripted fixture streams (origin Fixture unless a
    /// test compares origins). Mechanics only: no stream here is autonomous behaviour.
    /// </summary>
    [Parallelizable(ParallelScope.None)]
    public class UN2MotorTests
    {
        internal static PrimitiveFixture Two(ulong seed, out Npc a, out Npc b, int node = -1, int perFaction = 3)
        {
            var f = PrimitiveFixture.Create(seed, perFaction);
            a = f.Person(0); b = f.Person(1);
            int v = node >= 0 ? node : f.InteriorNode();
            f.Place(a, v, 0);
            f.Place(b, f.Grid.Neighbour(v, 0), 3);
            // everyone else far away (no listeners, no carriers in reach)
            foreach (var n in f.People().Skip(2)) f.Place(n, FarFrom(f, v), 0);
            return f;
        }

        internal static int FarFrom(PrimitiveFixture f, int v)
        {
            var g = f.Grid; int best = -1, bd = -1;
            for (int x = 0; x < g.Count; x++) if (g.IsLand(x) && g.Distance(x, v) > bd) { bd = g.Distance(x, v); best = x; }
            return best;
        }

        private static TargetRef T(TargetKind k, int id) => new TargetRef(k, id);

        // ---------------------------------------------------------------- T1

        [Test]
        public void Motor_TurnAndStep_Lattice()
        {
            var f = Two(1, out var a, out var b);
            var g = f.Grid;
            int v = a.Node;
            var r = f.Rec(a);
            // turning: one sector per turn, after TurnTicks (1)
            Assert.AreEqual(MotorStatus.Accepted, f.Submit(f.C(Prim.TurnRight, a)));
            Assert.AreEqual(0, r.Facing, "a turn takes effect when it completes");
            Assert.IsTrue(Motor.Busy(f.S, a.Id));
            Assert.AreEqual(MotorStatus.Busy, f.Submit(f.C(Prim.TurnRight, a)), "one action per body");
            f.Step();
            Assert.AreEqual(1, r.Facing); Assert.IsFalse(Motor.Busy(f.S, a.Id));
            f.Submit(f.C(Prim.TurnLeft, a)); f.Step(); f.Submit(f.C(Prim.TurnLeft, a)); f.Step();
            Assert.AreEqual(5, r.Facing, "left wraps 0 -> 5");
            // geometry: forward = the facing direction, back = opposite, left/right = the lower id of the two neighbours 30 degrees off
            for (byte fc = 0; fc < 6; fc++)
            {
                Assert.AreEqual(g.Neighbour(v, fc), Motor.StepNode(g, v, fc, Prim.StepForward), "forward " + fc);
                Assert.AreEqual(g.Neighbour(v, (fc + 3) % 6), Motor.StepNode(g, v, fc, Prim.StepBack), "back " + fc);
                Assert.AreEqual(Math.Min(g.Neighbour(v, (fc + 5) % 6), g.Neighbour(v, (fc + 4) % 6)), Motor.StepNode(g, v, fc, Prim.StepLeft), "left " + fc);
                Assert.AreEqual(Math.Min(g.Neighbour(v, (fc + 1) % 6), g.Neighbour(v, (fc + 2) % 6)), Motor.StepNode(g, v, fc, Prim.StepRight), "right " + fc);
            }
            // no neighbour within 30 degrees: node 0 (the bounding box corner) facing sector 3 has no neighbour at bearing 180
            Assert.AreEqual(-1, g.Neighbour(0, 3));
            Assert.AreEqual(-1, Motor.StepNode(g, 0, 3, Prim.StepForward), "more than 30 degrees off every neighbour");
            // a step takes StepTicks (6) and moves exactly one edge
            r.Facing = 2;
            int to = g.Neighbour(v, 2);
            Assert.AreEqual(MotorStatus.Accepted, f.Submit(f.C(Prim.StepForward, a)));
            for (int k = 0; k < 5; k++) { f.Step(); Assert.AreEqual(v, a.Node, "still on the start node after " + (k + 1) + " ticks"); }
            f.Step();
            Assert.AreEqual(to, a.Node, "one edge after 6 ticks");
            Assert.AreEqual(1, g.Distance(v, to));
            // sea is Blocked
            int coast = f.CoastNode(out int seaDir);
            f.Place(a, coast, (byte)seaDir);
            Assert.AreEqual(MotorStatus.Blocked, f.Submit(f.C(Prim.StepForward, a)));
            Assert.AreEqual(MotorStatus.Blocked, r.LastResult);
            Assert.IsTrue(f.Of<ControlFailed>().Any(e => e.Actor == a.Id && e.Status == MotorStatus.Blocked && e.Origin == ControlOrigin.Fixture));
            Assert.AreEqual(coast, a.Node);
            // an occupied footprint (a building) is Blocked
            f.Place(a, v, 1);
            f.NewBuilding(CatId.Building(1), g.Neighbour(v, 1));
            Assert.AreEqual(MotorStatus.Blocked, f.Submit(f.C(Prim.StepForward, a)));
            Assert.AreEqual(MotorStatus.Accepted, f.Submit(f.C(Prim.StepBack, a)), "the other way is free");
            TestContext.WriteLine($"T1: facing 0->1->5, step {v}->{to} in 6 ticks, sea and footprint Blocked");
        }

        // ---------------------------------------------------------------- T2

        [Test]
        public void Motor_NoTeleportNoPather()
        {
            var f = PrimitiveFixture.Create(2, perFaction: 2);
            var people = f.People();
            int v = f.InteriorNode();
            foreach (var n in people) f.Place(n, v, 0);
            var rnd = new Random(2);
            var g = f.Grid;
            int submitted = 0, moves = 0;
            var last = people.ToDictionary(n => n.Id, n => n.Node);
            while (submitted < 5000)
            {
                foreach (var n in people)
                {
                    var c = new PrimitiveControl
                    {
                        Control = (Prim)rnd.Next(0, 14), Actor = rnd.Next(10) == 0 ? rnd.Next(0, 50) : n.Id,
                        Target = new TargetRef((TargetKind)rnd.Next(0, 14), rnd.Next(-2, g.Count + 2)), Slot = (byte)rnd.Next(0, 5),
                        Gesture = (byte)rnd.Next(0, 3),
                    };
                    f.Submit(c); submitted++;
                }
                f.Step();
                foreach (var n in people)
                {
                    int d = g.Distance(last[n.Id], n.Node);
                    Assert.LessOrEqual(d, 1, "never more than one edge per tick");
                    if (d == 1) moves++;
                    last[n.Id] = n.Node;
                }
            }
            Assert.Greater(moves, 10, "steps happened");
            // static: no pather and no MoveTo on the unified path
            string root = Path.Combine(UN0RunTests.SimRoot(), "Unified");
            var bad = new List<string>();
            foreach (var p in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories).OrderBy(x => x, StringComparer.Ordinal))
            {
                var lines = UN0RunTests.CodeLines(File.ReadAllText(p));
                for (int i = 0; i < lines.Length; i++)
                    foreach (var tok in new[] { "NodePather", "MoveTo", "ControlInputKind.MoveTo", "WalkTo(", ".Path =" })
                        if (lines[i].Contains(tok)) bad.Add(Path.GetFileName(p) + ":" + (i + 1) + " " + tok);
            }
            Assert.IsEmpty(bad, string.Join("\n", bad));
            TestContext.WriteLine($"T2: {submitted} controls, {moves} single-edge moves, no pather reference");
        }

        // ---------------------------------------------------------------- T3

        [Test]
        public void Motor_FuzzNeverCorrupts()
        {
            var f = PrimitiveFixture.Create(3, perFaction: 2);
            var people = f.People();
            int v = f.InteriorNode();
            for (int i = 0; i < people.Count; i++) f.Place(people[i], i == 0 ? v : f.Grid.Neighbour(v, i % 6), 0);
            var g = f.Grid;
            f.Ground(v, Ware.Rock, 3); f.Ground(g.Neighbour(v, 2), Ware.Log, 1); f.InCarry(people[1], Ware.Grain, 1);
            int bin = f.NewBin(BinKind.Storage, g.Neighbour(v, 3)); f.InBin(bin, Ware.Rock, 2);
            // one asleep, one dead
            Assert.AreEqual(MotorStatus.Accepted, f.Submit(f.C(Prim.Sleep, people[2])));
            var dead = people[3];
            PopulationModule.Kill(f.S, new EventBuffer(), dead, DeathCause.Accident);
            var rnd = new Random(3);
            var origins = new[] { ControlOrigin.Policy, ControlOrigin.Possessed, ControlOrigin.Fixture };
            int refused = 0, accepted = 0;
            var statuses = new int[16];
            ulong h = f.HashIgnoringLastResult();
            for (int k = 0; k < 20000; k++)
            {
                var actor = rnd.Next(12) == 0 ? rnd.Next(-3, 9999) : people[rnd.Next(people.Count)].Id;
                var lots = f.S.Population.Lots.Select(x => x.Lot.Id).Concat(people.SelectMany(p => p.Carry).Where(l => !l.Empty).Select(l => l.Id)).ToList();
                int tid;
                var tk = (TargetKind)rnd.Next(0, 16);
                if (tk == TargetKind.Lot && lots.Count > 0 && rnd.Next(2) == 0) tid = lots[rnd.Next(lots.Count)];
                else if (tk == TargetKind.Npc) tid = people[rnd.Next(people.Count)].Id;
                else tid = rnd.Next(-5, g.Count + 5);
                var c = new PrimitiveControl
                {
                    Control = (Prim)rnd.Next(0, 16), Actor = actor, Target = new TargetRef(tk, tid),
                    SourceLot = rnd.Next(3) == 0 && lots.Count > 0 ? lots[rnd.Next(lots.Count)] : rnd.Next(4) == 0 ? rnd.Next(-3, 500) : 0,
                    Slot = (byte)rnd.Next(0, 8), Gesture = (byte)rnd.Next(0, 40), Quantity = (short)rnd.Next(-5, 1200),
                    R1 = new Referent { Kind = (byte)rnd.Next(0, 6), Id = rnd.Next(0, 50), Ware = (short)rnd.Next(0, 30) },
                    Corr = (uint)rnd.Next(),
                };
                if (rnd.Next(6) == 0) c.Quantity = 999;
                if (rnd.Next(6) == 0) c.Slot = 7;
                MotorStatus st = MotorStatus.Accepted;
                Assert.DoesNotThrow(() => st = f.Submit(c, origins[k % 3]), "control " + k);
                statuses[(int)st]++;
                ulong after = f.HashIgnoringLastResult();
                if (st == MotorStatus.Accepted) { accepted++; h = after; }
                else
                {
                    refused++;
                    if (st == MotorStatus.Resisted) h = after; // the holder perceived the attempt (its ResistedBy): nothing moved
                    else Assert.AreEqual(h, after, "refusal " + st + " changed state at control " + k);
                }
                if (k % 7 == 0) { f.Step(); h = f.HashIgnoringLastResult(); }
                if (k % 500 == 0) { var bad = f.Invariants(); Assert.IsEmpty(bad, "U-C1..U-C6 at " + k + ":\n" + string.Join("\n", bad)); }
            }
            Assert.IsEmpty(f.Invariants());
            Assert.Greater(accepted, 100); Assert.Greater(refused, 10000);
            TestContext.WriteLine($"T3: 20000 controls, {accepted} accepted, {refused} refused; by status " +
                string.Join(", ", Enumerable.Range(0, 16).Where(i => statuses[i] > 0).Select(i => (MotorStatus)i + "=" + statuses[i])));
        }

        // ---------------------------------------------------------------- T4

        [Test]
        public void Take_AllSources()
        {
            var f = Two(4, out var a, out var b);
            var g = f.Grid; int v = a.Node;
            f.Place(b, FarFrom(f, v));
            // ground (PickUp), in reach 1
            int rock = f.Ground(g.Neighbour(v, 1), Ware.Rock, 1);
            Assert.AreEqual(MotorStatus.Accepted, f.Submit(f.C(Prim.Take, a, T(TargetKind.Lot, rock))));
            Assert.AreEqual(rock, a.Carry.First(l => !l.Empty).Id, "the moved lot keeps its identity");
            Assert.IsTrue(UnifiedGoods.OriginOf(f.S, rock, out var o) && o.Lot == rock, "and its LotOrigin");
            var im = f.Of<ItemMoved>().Last();
            Assert.AreEqual(a.Id, im.Actor); Assert.AreEqual(MoveReason.PickUp, im.Reason);
            f.Step();
            Assert.AreEqual(MotorStatus.Accepted, f.Submit(new PrimitiveControl { Control = Prim.Drop, Actor = a.Id, SourceLot = rock }));
            Assert.AreEqual(MoveReason.Drop, f.Of<ItemMoved>().Last().Reason);
            Assert.IsTrue(f.S.Population.Lots.Any(x => x.Lot.Id == rock && x.Node == v), "dropped at the actor's node");
            f.Step();
            // a storage bin (Withdraw)
            int st = f.NewBin(BinKind.Storage, g.Neighbour(v, 2)); int sl = f.InBin(st, Ware.Grain, 2);
            Assert.AreEqual(MotorStatus.Accepted, f.Submit(new PrimitiveControl { Control = Prim.Take, Actor = a.Id, Target = T(TargetKind.Lot, sl), Quantity = 1 }));
            Assert.AreEqual(MoveReason.Withdraw, f.Of<ItemMoved>().Last().Reason);
            Assert.AreEqual(1, f.S.Population.Bins.First(x => x.Id == st).Lots[0].Count, "one of two taken");
            f.Step();
            // workstation output of a building (Building target)
            var bld = f.NewBuilding(CatId.Building(2), g.Neighbour(v, 3), workOut: true);
            f.InBin(bld.WorkOut, Ware.Grain, 1);
            Assert.AreEqual(MotorStatus.Accepted, f.Submit(f.C(Prim.Take, a, T(TargetKind.Building, bld.Id))));
            f.Step();
            Assert.AreEqual(MotorStatus.NotEnough, f.Submit(f.C(Prim.Take, a, T(TargetKind.Building, bld.Id))), "an empty output");
            // a container
            int ct = f.NewBin(BinKind.Container, v); int cl = f.InBin(ct, Ware.Rock, 1);
            // the hand: grain 2 + rock 2 = 4 of 6: a log (4) does not fit
            Assert.AreEqual(MotorStatus.Accepted, f.Submit(f.C(Prim.Take, a, T(TargetKind.Lot, cl))));
            f.Step();
            int log = f.Ground(v, Ware.Log, 1);
            Assert.AreEqual(MotorStatus.NoRoom, f.Submit(f.C(Prim.Take, a, T(TargetKind.Lot, log))), "weight");
            // out of reach
            int far = f.Ground(g.Neighbour(g.Neighbour(v, 4), 4), Ware.Rock, 1);
            Assert.AreEqual(MotorStatus.OutOfReach, f.Submit(f.C(Prim.Take, a, T(TargetKind.Lot, far))));
            // count
            Assert.AreEqual(MotorStatus.NotEnough, f.Submit(new PrimitiveControl { Control = Prim.Take, Actor = a.Id, Target = T(TargetKind.Lot, log), Quantity = 5 }));
            Assert.IsEmpty(f.Invariants());
            TestContext.WriteLine("T4: ground, storage, workstation output, container taken; NoRoom, OutOfReach, NotEnough refused");
        }

        // ---------------------------------------------------------------- T5

        [Test]
        public void Take_FromContainer_RegardlessOfLegacyOwnerFields()
        {
            var owners = new[] { Owner.None, Owner.Household(1), Owner.Group(2), Owner.Estate(3), new Owner(OwnerKind.Org, 4), Owner.Npc(9) };
            var results = new List<string>();
            foreach (var ow in owners)
            {
                var f = Two(5, out var a, out var b);
                f.Place(b, FarFrom(f, a.Node));
                int bin = f.NewBin(BinKind.Container, f.Grid.Neighbour(a.Node, 0));
                int lot = f.InBin(bin, Ware.Rock, 2);
                var lb = Goods.BinById(f.S.Population, bin);
                lb.Owner = ow; // the fixture writes legacy title fields; the motor must not read them
                var l = lb.Lots[0]; l.Owner = ow; lb.Lots[0] = l;
                var st = f.Submit(f.C(Prim.Take, a, T(TargetKind.Lot, lot)));
                results.Add(st + "/" + a.Carry.Where(x => !x.Empty).Sum(x => x.Count));
                Assert.AreEqual(MotorStatus.Accepted, st, ow.Kind.ToString());
            }
            Assert.AreEqual(1, results.Distinct().Count(), string.Join(",", results));
            TestContext.WriteLine("T5: " + owners.Length + " owner settings, one result: " + results[0]);
        }

        // ---------------------------------------------------------------- T6

        [Test]
        public void Take_FromPersons()
        {
            var f = Two(6, out var a, out var b);
            int rock = f.InCarry(b, Ware.Rock, 1);
            var before = f.Part("population");
            Assert.AreEqual(MotorStatus.Resisted, f.Submit(f.C(Prim.Take, a, T(TargetKind.Npc, b.Id))), "awake: resisted");
            Assert.AreEqual(MotorStatus.Resisted, f.Submit(f.C(Prim.Take, a, T(TargetKind.Lot, rock))), "the lot itself: resisted");
            Assert.AreEqual(before, f.Part("population"), "nothing moved");
            Assert.AreEqual(rock, b.Carry.First(x => !x.Empty).Id);
            Assert.AreEqual(a.Id, f.Rec(b).ResistedBy, "the holder perceived the attempt");
            Assert.AreEqual(f.S.Tick, f.Rec(b).ResistedTick);
            // asleep: Grab
            Assert.AreEqual(MotorStatus.Accepted, f.Submit(f.C(Prim.Sleep, b)));
            Assert.AreEqual(MotorStatus.Accepted, f.Submit(f.C(Prim.Take, a, T(TargetKind.Npc, b.Id))));
            Assert.AreEqual(MoveReason.Grab, f.Of<ItemMoved>().Last().Reason);
            f.Step();
            // unconscious: Grab
            var c = f.Person(2); f.Place(c, f.Grid.Neighbour(a.Node, 1)); int grain = f.InCarry(c, Ware.Grain, 1);
            c.Health = 40_000;
            Assert.IsTrue(Motor.Incapacitated(f.S, c));
            Assert.AreEqual(MotorStatus.Accepted, f.Submit(f.C(Prim.Take, a, T(TargetKind.Lot, grain))));
            Assert.AreEqual(MoveReason.Grab, f.Of<ItemMoved>().Last().Reason);
            f.Step();
            // held out (the UN0 HoldOut record, stubbed by the fixture until UN7): HandOver
            var d = f.Person(3); f.Place(d, f.Grid.Neighbour(a.Node, 2)); int axe = f.InCarry(d, Ware.Axe, 1);
            f.U.HoldOuts.Add(new HoldOut { Npc = d.Id, Lot = axe, To = 0, Expires = f.S.Tick + 100 });
            for (int i = 0; i < a.Carry.Length; i++) if (!a.Carry[i].Empty) { f.Submit(new PrimitiveControl { Control = Prim.Drop, Actor = a.Id, SourceLot = a.Carry[i].Id }); f.Step(); }
            Assert.AreEqual(MotorStatus.Accepted, f.Submit(f.C(Prim.Take, a, T(TargetKind.Npc, d.Id))));
            Assert.AreEqual(MoveReason.HandOver, f.Of<ItemMoved>().Last().Reason);
            Assert.IsTrue(a.Carry.Any(x => x.Id == axe));
            // dead: the body spilled its carry at death (U-C4: a dead person holds nothing); TAKE finds nothing on it
            f.Step();
            var e = f.Person(4);
            f.Place(e, a.Node); f.InCarry(e, Ware.Rock, 1);
            PopulationModule.Kill(f.S, new EventBuffer(), e, DeathCause.Accident);
            Assert.IsTrue(Motor.Incapacitated(f.S, e));
            Assert.AreEqual(MotorStatus.NotEnough, f.Submit(f.C(Prim.Take, a, T(TargetKind.Npc, e.Id))));
            Assert.IsEmpty(f.Invariants());
            TestContext.WriteLine("T6: awake Resisted (nothing moved, perceived), asleep/unconscious Grab, held-out HandOver, dead holds nothing");
        }

        // ---------------------------------------------------------------- T9

        [Test]
        public void Gesture_EmitsMessageWithReferents()
        {
            var f = Two(9, out var a, out var b);
            var c = f.Person(2); f.Place(c, FarFrom(f, a.Node));
            Assert.Greater(f.Grid.Distance(a.Node, c.Node), CommsRange.Gesture(f.S.Tuning.Population, false), "c is out of gesture range");
            int rock = f.InCarry(a, Ware.Rock, 1);
            var pop = f.Part("population");
            uint nextCorr = f.U.NextCorr;
            var ctl = new PrimitiveControl
            {
                Control = Prim.Gesture, Actor = a.Id, Gesture = (byte)GestureAct.Offer, Target = T(TargetKind.Npc, b.Id),
                R1 = new Referent { Kind = ReferentKind.Lot, Id = rock, Ware = (short)Ware.Rock }, R2 = new Referent { Kind = ReferentKind.Person, Id = b.Id },
                Quantity = 250, Corr = 77,
            };
            Assert.AreEqual(MotorStatus.Accepted, f.Submit(ctl));
            var gm = f.Of<GestureMade>().Last();
            Assert.AreEqual(a.Id, gm.Speaker); Assert.AreEqual(GestureAct.Offer, gm.Act); Assert.AreEqual(9, gm.Quantity, "the M5 message keeps its 0-9 cap");
            Assert.AreEqual(TopicKind.Ware, gm.TopicKind); Assert.AreEqual((int)Ware.Rock, gm.TopicId); Assert.AreEqual(b.Id, gm.Target); Assert.AreEqual(0, gm.DealId);
            var heard = f.Of<MessageDecoded>().Where(x => x.MessageId == gm.MessageId).Select(x => x.Listener).ToList();
            CollectionAssert.Contains(heard, b.Id, "a listener within range perceives it");
            CollectionAssert.DoesNotContain(heard, c.Id, "one out of range does not");
            Assert.IsTrue(Motor.ExtOf(f.U, gm.MessageId, out var ext));
            Assert.AreEqual(250, ext.Quantity); Assert.AreEqual(77u, ext.Corr); Assert.AreEqual(rock, ext.R1.Id); Assert.AreEqual(b.Id, ext.R2.Id);
            Assert.AreEqual(nextCorr, f.U.NextCorr, "UN2 never allocates a correlation id");
            Assert.AreEqual(rock, a.Carry.First(x => !x.Empty).Id, "a gesture moves no goods");
            Assert.AreEqual(0, f.Of<ItemMoved>().Count(x => x.Tick == f.S.Tick && x.Actor == a.Id && x.Lot == rock));
            Assert.IsEmpty(f.Invariants());
            TestContext.WriteLine($"T9: message {gm.MessageId} heard by {heard.Count}, MsgExt q=250 corr=77, M5 q=9");
        }

        // ---------------------------------------------------------------- T10

        [Test]
        public void Sleep_And_Wait()
        {
            var f = Two(10, out var a, out var b);
            a.Drives[Drive.Fatigue] = 600_000;
            Assert.AreEqual(MotorStatus.Accepted, f.Submit(f.C(Prim.Sleep, a)));
            Assert.IsTrue(a.Action.Asleep); Assert.IsTrue(f.Of<Slept>().Any(x => x.Npc == a.Id && !x.Woke));
            int f0 = a.Drives[Drive.Fatigue]; f.Step(); int f1 = a.Drives[Drive.Fatigue]; f.Step(); int f2 = a.Drives[Drive.Fatigue];
            int tpd = SimCalendar.Of(f.S).TicksPerDay;
            int perTick = (int)((long)f.S.Tuning.Population.SleepOpenPerDay * 1000 / tpd);
            Assert.AreEqual(perTick, f0 - f1, "the legacy open-sleep recovery per tick (Body())");
            Assert.AreEqual(f0 - f1, f1 - f2);
            Assert.AreEqual(MotorStatus.Asleep, f.Submit(f.C(Prim.TurnLeft, a)), "a sleeper's other controls");
            Assert.AreEqual(MotorStatus.Asleep, f.Submit(f.C(Prim.Sleep, a)));
            Motor.Interrupt(f.S, new EventBuffer(), a.Id, InterruptReason.Pain);
            Assert.IsFalse(a.Action.Asleep, "an interruption wakes");
            Assert.AreEqual(MotorStatus.Accepted, f.Submit(f.C(Prim.Sleep, a)));
            a.Drives[Drive.Fatigue] = (f.S.Tuning.Population.WakeFatigue - 1) * 1000;
            f.Step();
            Assert.IsFalse(a.Action.Asleep, "wakes when rested (legacy condition)");
            Assert.IsTrue(f.Of<Slept>().Any(x => x.Npc == a.Id && x.Woke));
            // WAIT: WaitTicks and nothing else (a twin that never waited has the same hash after)
            var g = Two(10, out var a2, out var b2); var h = Two(10, out var a3, out var b3);
            Assert.AreEqual(g.Hash(), h.Hash());
            Assert.AreEqual(MotorStatus.Accepted, g.Submit(g.C(Prim.Wait, a2)));
            for (int k = 0; k < 9; k++) { g.Step(); h.Step(); Assert.IsTrue(Motor.Busy(g.S, a2.Id), "waiting at " + k); }
            g.Step(); h.Step();
            Assert.IsFalse(Motor.Busy(g.S, a2.Id), "WAIT took 10 ticks");
            Assert.AreEqual(h.Hash(), g.Hash(), "WAIT changed nothing else");
            TestContext.WriteLine($"T10: sleep recovers {perTick}/tick (legacy rate), interrupt and rest wake, WAIT 10 ticks neutral");
        }

        // ---------------------------------------------------------------- T14 (U-P4)

        /// <summary>The demonstration world: two people side by side, a log on the ground ahead of the first, a rock beside.</summary>
        internal static PrimitiveFixture DemoWorld()
        {
            var f = Two(14, out var a, out var b);
            var g = f.Grid;
            int v = a.Node;
            f.Ground(g.Neighbour(g.Neighbour(v, 2), 2), Ware.Log, 1);
            f.Ground(b.Node, Ware.Rock, 2);
            f.InCarry(a, Ware.Grain, 1);
            return f;
        }

        /// <summary>A mixed stream over <see cref="DemoWorld"/>: turns, steps, take, drop, gesture, sleep, wait, some refused.</summary>
        internal static List<ScriptedControl> DemoStream(PrimitiveFixture f)
        {
            var a = f.Person(0); var b = f.Person(1);
            var log = f.S.Population.Lots.First(x => x.Lot.Ware == Ware.Log).Lot.Id;
            var rock = f.S.Population.Lots.First(x => x.Lot.Ware == Ware.Rock).Lot.Id;
            var l = new List<ScriptedControl>();
            void At(long t, PrimitiveControl c) => l.Add(new ScriptedControl(t, c));
            PrimitiveControl P(Prim p, Npc n, TargetKind k = TargetKind.None, int id = 0, int src = 0) => new PrimitiveControl { Control = p, Actor = n.Id, Target = new TargetRef(k, id), SourceLot = src };
            At(0, P(Prim.TurnRight, a)); At(1, P(Prim.TurnRight, a));
            At(2, P(Prim.StepForward, a)); At(8, P(Prim.Take, a, TargetKind.Lot, log));
            At(3, P(Prim.Take, b, TargetKind.Lot, rock)); At(4, P(Prim.TurnLeft, b));
            At(10, P(Prim.Drop, a, src: log)); At(12, P(Prim.StepBack, a));
            At(1, P(Prim.Take, b, TargetKind.Npc, a.Id)); // resisted (awake)
            At(13, P(Prim.Take, b, TargetKind.Npc, a.Id)); // out of reach
            At(20, new PrimitiveControl { Control = Prim.Gesture, Actor = a.Id, Gesture = (byte)GestureAct.Offer, Target = new TargetRef(TargetKind.Npc, b.Id), R1 = new Referent { Kind = ReferentKind.Ware, Ware = (short)Ware.Rock }, Quantity = 3, Corr = 5 });
            At(21, P(Prim.Wait, b)); At(22, P(Prim.Sleep, a)); At(23, P(Prim.TurnLeft, a)); // asleep
            At(24, P(Prim.StepForward, b)); // busy
            At(31, P(Prim.StepLeft, b)); At(40, P(Prim.StepRight, b));
            return l;
        }

        [Test]
        public void Motor_OriginNeutral()
        {
            var probe = DemoWorld();
            var stream = DemoStream(probe);
            var fs = PrimitiveFixture.AssertOriginNeutral(DemoWorld, stream, 60);
            Assert.IsTrue(fs[0].Results.Contains(MotorStatus.Resisted));
            TestContext.WriteLine("statuses: " + string.Join(" ", fs[0].Recorded.Select((r, i) => r.Tick + ":" + r.Control.Control + "=" + fs[0].Results[i])));
            // the origin is on the events only
            Assert.IsTrue(fs[0].Of<ControlApplied>().All(e => e.Origin == ControlOrigin.Policy));
            Assert.IsTrue(fs[1].Of<ControlApplied>().All(e => e.Origin == ControlOrigin.Possessed));
            Assert.Greater(fs[0].Of<ControlApplied>().Count(), 8);
            Assert.Greater(fs[0].Of<ControlFailed>().Count(), 1);
            TestContext.WriteLine($"T14 (U-P4): {stream.Count} controls x 3 origins, hash {fs[0].Hash():x16}, unification {fs[0].Part("unification"):x16}, population {fs[0].Part("population"):x16}");
        }
    }
}
