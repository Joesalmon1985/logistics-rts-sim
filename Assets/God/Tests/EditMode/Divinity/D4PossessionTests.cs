using System;
using System.Collections.Generic;
using System.Linq;
using God.Sim.Divinity;
using God.Sim.Learning;
using God.Sim.Policy;
using God.Sim.Population;
using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>
    /// D4a (D4 spec 14, brief §47 scenario 10): possession through the F1 control seam. T1 identity, T2 body, T4 learning,
    /// T5 no policy draws, T6 one at a time and exhaustion, T7 precedence, T8 lease and replay, T15 save/load mid-spell.
    /// OWNER: D4.
    /// </summary>
    public class D4PossessionTests
    {
        internal static int GroundLot(GodSim sim, int node, Ware w = Ware.Log, Owner owner = default) =>
            Goods.Produce(sim.State, null, Holder.Ground(node), w, 1, Wares.IsFood(w) ? 30 : -1, 0, owner, ProduceReason.Placed, 0, 0);

        internal static DivineControlInput Heading(int body, int heading, int god = 1) =>
            new DivineControlInput { God = god, Body = body, Kind = ControlInputKind.Heading, Heading = (byte)heading, Gait = 1 };

        internal static DivineControlInput Act(int body, DivineAct act, DivineTarget target, int god = 1) =>
            new DivineControlInput { God = god, Body = body, Kind = ControlInputKind.Action, Act = act, Target = target };

        internal static DivineControlInput Stop(int body, int god = 1) => new DivineControlInput { God = god, Body = body, Kind = ControlInputKind.Stop };

        internal static DivineControlInput MoveTo(int body, int node, int god = 1) => new DivineControlInput { God = god, Body = body, Kind = ControlInputKind.MoveTo, Node = node };

        /// <summary>A policy that never acts (an idle control twin: the possessed body without inputs idles too).</summary>
        internal sealed class IdlePolicy : INpcPolicy
        {
            public string Name => "test-idle";
            public PolicyLevel Level => PolicyLevel.Civilian;
            public NpcIntent Decide(in NpcObservation obs, MindView mind, ref Rng rng) => NpcIntent.Nothing;
        }

        internal static void Idle(GodSim sim, Npc npc)
        {
            sim.Systems.OfType<PopulationSystem>().First().SetPolicy("test-idle", new IdlePolicy());
            npc.Policy = "test-idle";
        }

        /// <summary>A fixture person (<c>LearningFixtures.Npc</c>) born 1.5 sim years ago (an adult: old enough to be possessed).</summary>
        internal static Npc Person(GodSim sim, int node, InstinctSet instincts, LearningMode mode, int hunger = 200, int fatigue = 150)
        {
            var n = LearningFixtures.Npc(sim, node, instincts, mode, hunger, fatigue);
            n.BornTick = sim.State.Tick - SimCalendar.Of(sim.State).TicksPerYear * 3 / 2;
            return n;
        }

        private static void NoUpkeep(SimTuning t) { t.Divinity.Mythic.Upkeep.PossessionPerTick = 0; t.Divinity.Mythic.Upkeep.AvatarPerTick = 0; }

        /// <summary>Walks a body one heading step at a time (an input every 2 ticks, inside the 3-tick lease) until it stands on <paramref name="goal"/>.</summary>
        internal static int WalkByHeading(GodSim sim, DivineProbe probe, Npc body, int heading, int goal, int limit = 600, int god = 1)
        {
            var g = sim.State.Territory.Grid;
            int prev = body.Node, steps = 0;
            for (int t = 0; t < limit && body.Node != goal; t++)
            {
                if (t % 2 == 0) Assert.IsTrue(sim.Submit(Heading(body.Id, heading, god)).Ok, "heading input");
                probe.Step(1);
                int d = g.Distance(prev, body.Node);
                Assert.LessOrEqual(d, 1, "one node per step: never teleported");
                steps += d; prev = body.Node;
            }
            return steps;
        }

        // ---------------------------------------------------------------- D4-T1 (PA1)

        [Test]
        public void Possession_ReturnsSameIdentity()
        {
            var sim = D2Fixtures.Game(51);
            var g = sim.State.Territory.Grid;
            int c = LearningFixtures.Glade(sim, 14);
            var n = Person(sim, c, InstinctSet.Default, LearningMode.On, 200, 150);
            int end = LearningFixtures.Step(sim, c, 0, 12);
            int lot = GroundLot(sim, end);
            var m = Person(sim, LearningFixtures.Step(sim, c, 0, 13), InstinctSet.Default, LearningMode.On, 200, 150);
            Idle(sim, m);
            var probe = new DivineProbe(sim);
            int id = n.Id;
            var record = n;
            string policy = n.Policy;
            Assert.IsTrue(sim.Submit(new BeginPossession { God = 1, Npc = id }).Ok);
            probe.Step(1);
            Assert.AreEqual(ControlKind.Possessed, Control.Of(sim.State, id));
            int steps = WalkByHeading(sim, probe, n, 0, end);
            Assert.AreEqual(end, n.Node, "walked to the log by heading inputs");
            Assert.AreEqual(12, steps, "12 nodes");
            Assert.IsTrue(sim.Submit(Stop(id)).Ok);
            probe.Step(8);
            Assert.AreEqual(end, n.Node, "a stop halts it");
            Assert.IsTrue(sim.Submit(Act(id, DivineAct.Take, new DivineTarget(DivineTargetKind.Lot, lot))).Ok);
            for (int i = 0; i < 40 && !n.Carry.Any(l => !l.Empty && l.Ware == Ware.Log); i++) probe.Step(1);
            var pick = probe.Of<ItemMoved>().Single(e => e.Reason == MoveReason.PickUp);
            Assert.AreEqual(id, pick.Actor);
            Assert.IsTrue(sim.Submit(Act(id, DivineAct.Give, new DivineTarget(DivineTargetKind.Npc, m.Id, (int)Ware.Log))).Ok);
            for (int i = 0; i < 60 && !probe.Of<ItemMoved>().Any(e => e.Reason == MoveReason.HandOver); i++) probe.Step(1);
            probe.Step(1); // PossessedActionStarted is reported at the tick after the think that started the act
            var give = probe.Of<ItemMoved>().Single(e => e.Reason == MoveReason.HandOver);
            Assert.AreEqual(id, give.Actor, "the possessed person gave it");
            Assert.AreEqual(m.Id, give.ToId);
            Assert.AreEqual(OwnerKind.Npc, give.OwnerAfterKind); Assert.AreEqual(m.Id, give.OwnerAfterId, "a gift: the receiver owns it");
            var acts = probe.Of<PossessedActionStarted>();
            Assert.IsTrue(acts.Any(a => a.Verb == Verb.PickUp && a.TargetId == lot), "PossessedActionStarted for the take");
            Assert.IsTrue(acts.Any(a => a.Verb == EmbodiedVerb.Give && a.TargetId == m.Id), "PossessedActionStarted for the give");
            Assert.AreEqual(ControlKind.Possessed, n.Mind.Last.Control, "the decision record says the act was controlled");
            Assert.IsTrue(sim.Submit(new EndPossession { God = 1 }).Ok);
            long endTick = sim.State.Tick;
            probe.Step(1);
            var ended = probe.Of<PossessionEnded>().Single();
            Assert.AreEqual(ControlEndReason.Cancelled, ended.Reason);
            Assert.AreEqual(1, probe.Of<PossessionStarted>().Count);
            Assert.GreaterOrEqual(ended.Actions, 2);
            Assert.AreEqual(ControlKind.Self, Control.Of(sim.State, id));
            Assert.AreSame(record, sim.State.Population.Find(id), "the same record");
            Assert.AreEqual(policy, n.Policy, "the policy seat was never rewritten");
            // the neighbour remembers a gift from n (not from a god)
            var rel = m.Mind.Social.Rel[m.Mind.Social.Find(id)];
            Assert.GreaterOrEqual(rel.GiftsIn, 1, "the receiver's social memory records a gift from n");
            // n's next decision is its own
            for (int i = 0; i < 30 && n.Mind.Last.Tick <= endTick; i++) probe.Step(1);
            Assert.Greater(n.Mind.Last.Tick, endTick);
            Assert.AreEqual(ControlKind.Self, n.Mind.Last.Control, "after the end its own policy decides");
            TestContext.Progress.WriteLine("T1: walked " + steps + " nodes, took lot " + lot + ", gave it to " + m.Id + " (gifts in " + rel.GiftsIn + "), inputs applied "
                + ended.InputsApplied + ", refused " + ended.InputsRefused + ", actions " + ended.Actions + ", ticks " + ended.Ticks);
        }

        // ---------------------------------------------------------------- D4-T2 (PA2)

        [Test]
        public void Possession_BodyContinues()
        {
            var sim = D2Fixtures.Game(52, t => t.Divinity.Mode = DivinityMode.Unlimited);
            int c = LearningFixtures.Glade(sim, 10);
            var p = Person(sim, c, InstinctSet.Default, LearningMode.On, 900, 150);
            var q = Person(sim, LearningFixtures.Step(sim, c, 3, 9), InstinctSet.Default, LearningMode.On, 900, 150);
            foreach (var x in new[] { p, q }) x.Nutrition = 120_000;
            Idle(sim, q);
            var probe = new DivineProbe(sim);
            Assert.IsTrue(sim.Submit(new BeginPossession { God = 1, Npc = p.Id }).Ok);
            var cal = SimCalendar.Of(sim.State);
            long age0 = Bodies.AgeMilli(cal, p, sim.State.Tick);
            int samples = 0;
            for (int t = 0; t < 2 * D2Fixtures.Day && p.Alive; t++)
            {
                probe.Step(1);
                if (t % 60 != 0 || !p.Alive) continue;
                samples++;
                for (int d = 0; d < Drive.Count; d++) Assert.AreEqual(q.Drives[d], p.Drives[d], "drive " + d + " at " + t + ": the same rules as the idle twin");
                Assert.AreEqual(q.Nutrition, p.Nutrition, "nutrition"); Assert.AreEqual(q.Health, p.Health, "health"); Assert.AreEqual(q.Stage, p.Stage);
                Assert.AreEqual(Bodies.AgeMilli(cal, q, sim.State.Tick), Bodies.AgeMilli(cal, p, sim.State.Tick), "ageing");
            }
            Assert.Greater(Bodies.AgeMilli(cal, p, sim.State.Tick), age0, "it aged");
            for (int t = 0; t < 30 * D2Fixtures.Day && p.Alive; t++) probe.Step(1);
            Assert.IsFalse(p.Alive, "unfed, the possessed body starves");
            var died = probe.Of<NpcDied>().Single(e => e.Npc == p.Id);
            Assert.AreEqual(DeathCause.Starvation, died.Cause);
            var ended = probe.Of<PossessionEnded>().Single();
            Assert.AreEqual(ControlEndReason.Death, ended.Reason);
            Assert.AreEqual(died.Tick, ended.Tick, "the possession ends the tick it dies");
            Assert.IsTrue(probe.Probe.Events.IndexOf(died) < probe.Probe.Events.IndexOf(ended), "after NpcDied (Kill order)");
            var qd = probe.Of<NpcDied>().SingleOrDefault(e => e.Npc == q.Id);
            Assert.IsNotNull(qd, "the idle twin starves too");
            Assert.AreEqual(qd.Tick, died.Tick, "on the same tick: possession changes no body process");
            TestContext.Progress.WriteLine("T2: " + samples + " samples equal to the idle twin; starved at tick " + died.Tick + " (twin " + qd.Tick + "), possession ended " + ended.Reason);
        }

        // ---------------------------------------------------------------- D4-T4 (PA4)

        private static (int value, bool tagged) LearnAtFishSpot(ulong seed, int permille)
        {
            var sim = D2Fixtures.Game(seed, t => { t.Divinity.Possession.LearnPermille = permille; NoUpkeep(t); });
            int c = LearningFixtures.Glade(sim, 10);
            int fish = LearningFixtures.Step(sim, c, 1, 6);
            LearningFixtures.Bush(sim, SourceKind.Fish, fish);
            var n = Person(sim, c, InstinctSet.Default, LearningMode.On, 700, 150);
            var probe = new DivineProbe(sim);
            uint place = Feature.PlaceRef(fish), key = DivinityMindHooks.PossessedKey;
            Assert.AreEqual(0, Value(n, place), "it never knew the spot");
            Assert.IsTrue(sim.Submit(new BeginPossession { God = 1, Npc = n.Id }).Ok);
            probe.Step(1);
            Assert.IsTrue(sim.Submit(MoveTo(n.Id, fish)).Ok);
            for (int i = 0; i < 200 && n.Node != fish; i++) probe.Step(1);
            Assert.AreEqual(fish, n.Node);
            probe.Step(3);
            Assert.IsTrue(sim.Submit(Act(n.Id, DivineAct.Eat, new DivineTarget(DivineTargetKind.Location, 0, fish))).Ok);
            bool tagged = false;
            for (int i = 0; i < 120 && !probe.Probe.Events.OfType<Ate>().Any(); i++)
            {
                probe.Step(1);
                var st0 = n.Mind.Store;
                for (int k = 0; k < st0.RingCount * AssociationStore.MaxKeys && k < st0.TraceKeys.Length; k++) if (st0.TraceKeys[k] == key) tagged = true;
            }
            Assert.IsTrue(probe.Probe.Events.OfType<Ate>().Any(), "it ate there");
            long ate = sim.State.Tick;
            for (int i = 0; i < 20 && n.LastThinkTick < ate; i++) probe.Step(1); // the think that credits the meal (still possessed)
            Assert.AreEqual(ControlKind.Possessed, Control.Of(sim.State, n.Id));
            return (Value(n, place), tagged);
        }

        private static int Value(Npc n, uint key)
        {
            var st = n.Mind.Store;
            int v = 0;
            for (int i = 0; i < st.Count; i++) if (st.Entries[i].Verb == Verb.Eat && st.Entries[i].Key == key) v += st.Entries[i].Stw + st.Entries[i].Ltw;
            return v;
        }

        [Test]
        public void Possession_LearnsWithControlTag()
        {
            int okZero = 0, okHalf = 0, okFull = 0, tagged = 0;
            var line = new List<string>();
            for (ulong seed = 1; seed <= 20; seed++)
            {
                var full = LearnAtFishSpot(seed, 1000);
                var half = LearnAtFishSpot(seed, 500);
                var none = LearnAtFishSpot(seed, 0);
                if (full.tagged && half.tagged && none.tagged) tagged++;
                if (none.value == 0) okZero++;
                if (full.value > 0) okFull++;
                if (full.value > 0 && half.value * 4 >= full.value && half.value * 4 <= full.value * 3) okHalf++;
                line.Add(full.value + "/" + half.value + "/" + none.value);
            }
            TestContext.Progress.WriteLine("T4 Eat-at-place value (1000/500/0 per mille) by seed: " + string.Join(" ", line));
            Assert.AreEqual(20, tagged, "every context carried Feature.Divine(Possessed)");
            Assert.AreEqual(20, okFull, "full value learned at 1000");
            Assert.AreEqual(20, okZero, "nothing learned at 0");
            Assert.AreEqual(20, okHalf, "about half at 500 (between a quarter and three quarters of the full value)");
        }

        // ---------------------------------------------------------------- D4-T5 (PA5)

        [Test]
        public void Possession_NoPolicyDraws()
        {
            var sim = D2Fixtures.Game(55, NoUpkeep);
            int c = LearningFixtures.Glade(sim, 10);
            LearningFixtures.Bush(sim, SourceKind.Berries, LearningFixtures.Step(sim, c, 2, 3));
            var n = Person(sim, c, InstinctSet.Default, LearningMode.On, 600, 150);
            var probe = new DivineProbe(sim);
            probe.Step(30); // its own life first: the policy draws
            Assert.IsTrue(sim.Submit(new BeginPossession { God = 1, Npc = n.Id }).Ok);
            probe.Step(1);
            ulong before = n.RngPolicy.State;
            for (int t = 0; t < 300; t++)
            {
                if (t < 120 && t % 2 == 0) Assert.IsTrue(sim.Submit(Heading(n.Id, (t / 40) * 5 % 16)).Ok);
                probe.Step(1);
            }
            Assert.AreEqual(before, n.RngPolicy.State, "a possessed person makes no policy draw (I-PA-5)");
            Assert.Greater(n.Mind.Decisions, 0);
        }

        // ---------------------------------------------------------------- D4-T6 (PA6)

        [Test]
        public void Possession_OneAtATime_MythicEnds()
        {
            long cost = new SimTuning().Divinity.Mythic.Costs.BeginPossession, rate = new SimTuning().Divinity.Mythic.Upkeep.PossessionPerTick;
            long left = 5_030;
            var sim = D2Fixtures.Game(56, t => { t.Divinity.Mythic.Start = cost + left; t.Divinity.Mythic.RegenPerDay = 0; });
            int c = LearningFixtures.Glade(sim, 8);
            var a = Person(sim, c, InstinctSet.Default, LearningMode.On);
            var b = Person(sim, LearningFixtures.Step(sim, c, 0, 2), InstinctSet.Default, LearningMode.On);
            var probe = new DivineProbe(sim);
            Assert.IsTrue(sim.Submit(new BeginPossession { God = 1, Npc = a.Id }).Ok);
            probe.Step(1);
            long t0 = probe.Of<PossessionStarted>().Single().Tick; // the first upkeep is charged the tick it opens
            var second = sim.Submit(new BeginPossession { God = 1, Npc = b.Id });
            Assert.IsFalse(second.Ok); Assert.AreEqual(DivineReasons.ControlLimit, second.Reason, "one possession per god");
            for (int i = 0; i < 400 && probe.Of<PossessionEnded>().Count == 0; i++) probe.Step(1);
            var end = probe.Of<PossessionEnded>().Single();
            Assert.AreEqual(ControlEndReason.MythicExhausted, end.Reason);
            long expect = t0 + left / rate - 1; // pays rate each tick from the opening tick; ends the tick what is left cannot pay the next
            Assert.AreEqual(expect, end.Tick, "Exhaustion_EndsSameTick: on the computed tick");
            Assert.AreEqual(end.Tick, probe.Of<PossessionStarted>().Single().Tick + end.Ticks);
            TestContext.Progress.WriteLine("T6: second possession refused " + second.Reason + "; ended " + end.Reason + " at tick " + end.Tick + " (computed " + expect + "), balance "
                + sim.State.Divinity.Gods.Of(1).Mythic);
        }

        // ---------------------------------------------------------------- D4-T7 (possession rows of Control_Precedence_Table)

        [Test]
        public void Control_Precedence_PossessionRows()
        {
            var sim = D2Fixtures.Game(57, NoUpkeep);
            int c = LearningFixtures.Glade(sim, 8);
            var n = Person(sim, c, InstinctSet.Default, LearningMode.On, 300);
            var probe = new DivineProbe(sim);
            Assert.IsTrue(sim.Submit(new IssueCompulsion { God = 1, Act = DivineAct.MoveTo, Target = D2Fixtures.Person(n.Id, LearningFixtures.Step(sim, c, 2, 5)), ExpireTicks = 600 }).Ok);
            probe.Step(2);
            int compulsion = probe.Of<CompulsionStarted>().Single().Intervention;
            Assert.IsTrue(sim.Submit(new BeginPossession { God = 1, Npc = n.Id }).Ok);
            probe.Step(1);
            var ce = probe.Of<CompulsionEnded>().Single();
            Assert.AreEqual(ControlEndReason.Superseded, ce.Reason, "a possession ends a compulsion on the body");
            Assert.AreEqual(compulsion, probe.Of<PossessionStarted>().Single().SupersededCompulsion);
            Assert.AreEqual(ControlKind.Possessed, Control.Of(sim.State, n.Id));
            // another god: refused already_controlled (checked through the handler: god 2 holds no human seat here)
            var other = new BeginPossessionHandler().Validate(sim.State, new BeginPossession { God = 2, Npc = n.Id });
            Assert.AreEqual(DivineReasons.AlreadyControlled, other.Reason);
            // a compulsion on a possessed body: refused
            var comp = sim.Submit(new IssueCompulsion { God = 1, Act = DivineAct.MoveTo, Target = D2Fixtures.Person(n.Id, c), ExpireTicks = 100 });
            Assert.IsFalse(comp.Ok); Assert.AreEqual(DivineReasons.AlreadyControlled, comp.Reason);
            // an avatar is not a person: never possessed
            var av = F1FoundationTests.Avatar(sim, 2, c);
            Assert.AreEqual(DivineReasons.NotAPerson, new BeginPossessionHandler().Validate(sim.State, new BeginPossession { God = 1, Npc = av.Id }).Reason);
            sim.State.Population.Npcs.Remove(av);
        }

        // ---------------------------------------------------------------- D4-T8 (PA10)

        private static List<ulong> HeadingRun(ulong seed, int[] headings, out int nodes, out Npc body)
        {
            var sim = D2Fixtures.Game(seed, NoUpkeep);
            int c = LearningFixtures.Glade(sim, 12);
            body = Person(sim, c, InstinctSet.Default, LearningMode.On, 300);
            Person(sim, LearningFixtures.Step(sim, c, 4, 3), InstinctSet.Default, LearningMode.On, 300);
            var hashes = new List<ulong>();
            Assert.IsTrue(sim.Submit(new BeginPossession { God = 1, Npc = body.Id }).Ok);
            sim.Step();
            int prev = body.Node; nodes = 0;
            var g = sim.State.Territory.Grid;
            for (int t = 0; t < headings.Length; t++)
            {
                if (headings[t] >= 0) Assert.IsTrue(sim.Submit(Heading(body.Id, headings[t])).Ok);
                sim.Step();
                nodes += g.Distance(prev, body.Node); prev = body.Node;
                hashes.Add(sim.StateHash());
            }
            return hashes;
        }

        [Test]
        public void ControlInput_LeaseAndReplay()
        {
            // a recorded stick: north, then turning east, south-west; an input every 2 ticks (the host's rule), with gaps
            var seq = new int[160];
            for (int t = 0; t < seq.Length; t++) seq[t] = t % 2 != 0 ? -1 : t < 50 ? 0 : t < 100 ? 4 : t < 130 ? 10 : -1;
            var a = HeadingRun(61, seq, out int na, out _);
            var b = HeadingRun(61, seq, out int nb, out _);
            CollectionAssert.AreEqual(a, b, "the recorded input sequence replays exactly");
            Assert.Greater(na, 10, "it walked");

            // a lapsed lease: one input, then nothing: it finishes the edge it is on and halts
            var one = new int[80];
            for (int t = 0; t < one.Length; t++) one[t] = t == 0 ? 0 : -1;
            HeadingRun(62, one, out int n1, out var body);
            Assert.AreEqual(1, n1, "one edge, then it halts");
            Assert.AreEqual(0, body.StepTicks, "never stopped mid-edge");
            TestContext.Progress.WriteLine("T8: replay of " + seq.Length + " ticks equal (" + na + " nodes); a single input walked " + n1 + " node");
        }

        // ---------------------------------------------------------------- D4-T15 (possession half): save/load mid-spell

        [Test]
        public void Control_SaveLoadMidSpell_Possession()
        {
            var sim = D2Fixtures.Game(63);
            int c = LearningFixtures.Glade(sim, 12);
            var n = Person(sim, c, InstinctSet.Default, LearningMode.On, 300);
            Person(sim, LearningFixtures.Step(sim, c, 3, 2), InstinctSet.Default, LearningMode.On, 300);
            Assert.IsTrue(sim.Submit(new BeginPossession { God = 1, Npc = n.Id }).Ok);
            sim.Step();
            for (int t = 0; t < 21; t++) { if (t % 2 == 0) Assert.IsTrue(sim.Submit(Heading(n.Id, 2)).Ok); sim.Step(); }
            var e = Control.EntryOf(sim.State, n.Id);
            Assert.IsTrue(Control.LeaseHolds(in e, sim.State.Tick), "saved with a held heading lease");
            Assert.Greater(n.StepTicks + (n.Path != null ? 1 : 0), 0, "saved while walking");
            var loaded = SaveFile.FromJson(SaveFile.From(sim).ToJson()).ToSim();
            CollectionAssert.AreEqual(sim.StateHashParts(), loaded.StateHashParts());
            for (int t = 0; t < 300; t++)
            {
                if (t < 60 && t % 2 == 0) { Assert.IsTrue(sim.Submit(Heading(n.Id, 6)).Ok); Assert.IsTrue(loaded.Submit(Heading(n.Id, 6)).Ok); }
                if (t == 100) { Assert.IsTrue(sim.Submit(new EndPossession { God = 1 }).Ok); Assert.IsTrue(loaded.Submit(new EndPossession { God = 1 }).Ok); }
                sim.Step(); loaded.Step();
                if (t % 10 == 0) CollectionAssert.AreEqual(sim.StateHashParts(), loaded.StateHashParts(), "tick " + t);
            }
            CollectionAssert.AreEqual(sim.StateHashParts(), loaded.StateHashParts());
        }
    }
}
