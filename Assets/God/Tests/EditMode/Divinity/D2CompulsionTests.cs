using System;
using System.Collections.Generic;
using System.Linq;
using God.Sim.Divinity;
using God.Sim.Learning;
using God.Sim.Population;
using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>
    /// D2 complete (D2 spec 14): temporary compulsion through the F1 control seam (brief §47 scenario 9): it overrides
    /// hunger, never physics; it expires. T6 (DP4), T7 (DP5), T8, T9, plus save/load mid-spell (T13's sim part). OWNER: D2.
    /// </summary>
    public class D2CompulsionTests
    {
        private static int GroundLot(GodSim sim, int node, Ware w = Ware.Log) =>
            Goods.Produce(sim.State, null, Holder.Ground(node), w, 1, Wares.IsFood(w) ? 30 : -1, 0, Owner.None, ProduceReason.Placed, 0, 0);

        private static void NoUpkeep(SimTuning t) => t.Divinity.Mythic.Upkeep.CompulsionPerTick = 0;

        // ---------------------------------------------------------------- D2-T6 (DP4)

        [Test]
        public void Compulsion_OverridesHungerNotPhysics()
        {
            var sc = D2Fixtures.Glade(41, InstinctSet.Default, 900, NoUpkeep, gladeRadius: 10);
            var sim = sc.Sim;
            int far = LearningFixtures.Step(sim, sc.Centre, 3, 10);
            int lot = GroundLot(sim, far);
            Assert.AreEqual(10, sim.State.Territory.Grid.Distance(sc.Npc.Node, far));
            long before = sim.State.Divinity.Gods.Of(1).Mythic;
            Assert.IsTrue(sim.Submit(new IssueCompulsion { God = 1, Act = DivineAct.Take, Target = D2Fixtures.Person(sc.Npc.Id, 0, lot), ExpireTicks = 600 }).Ok);
            int prev = sc.Npc.Node, steps = 0;
            bool ate = false;
            CompulsionEnded end = null;
            for (int t = 0; t < 600 && end == null; t++)
            {
                sc.Probe.Step(1);
                int d = sim.State.Territory.Grid.Distance(prev, sc.Npc.Node);
                Assert.LessOrEqual(d, 1, "one node per step: never teleported");
                steps += d; prev = sc.Npc.Node;
                if (sc.Npc.Action.Verb == Verb.Eat || sc.Npc.Action.Verb == Verb.ForageAt) ate = true;
                end = sc.Probe.Of<CompulsionEnded>().FirstOrDefault();
            }
            Assert.IsNotNull(end, "the compulsion ends");
            Assert.AreEqual(ControlEndReason.Completed, end.Reason);
            Assert.IsFalse(ate, "a starving person walks past food while compelled");
            Assert.GreaterOrEqual(steps, 9, "it walked to the log (within reach 1)");
            var moved = sc.Probe.Of<ItemMoved>().Single(e => e.Lot == lot || e.EndedLot == lot || e.IntoLot == lot);
            Assert.AreEqual(MoveReason.PickUp, moved.Reason);
            Assert.AreEqual(sc.Npc.Id, moved.Actor);
            Assert.LessOrEqual(sim.State.Territory.Grid.Distance(moved.ActorNode, far), Reach.Touch, "picked up within reach");
            Assert.AreEqual(before - sim.State.Tuning.Divinity.Mythic.Costs.IssueCompulsion, sim.State.Divinity.Gods.Of(1).Mythic, "Mythic falls by exactly the configured cost");
            Assert.AreEqual(1, sc.Probe.Of<CompulsionStarted>().Count);
            TestContext.Progress.WriteLine("DP4 take: walked " + steps + " nodes, ended " + end.Reason + " at tick " + sim.State.Tick + ", hunger " + sc.Npc.DriveOf(Drive.Hunger));

            // compelled to give a fish it does not hold: blocked to expiry, nothing moves, nothing is conjured
            var other = LearningFixtures.Npc(sim, LearningFixtures.Step(sim, sc.Npc.Node, 0, 1), InstinctSet.Default, LearningMode.On);
            long fishBefore = sim.State.Population.Ledger.ProducedOf(Ware.Fish);
            Assert.IsTrue(sim.Submit(new IssueCompulsion { God = 1, Act = DivineAct.Give, Target = D2Fixtures.Person(sc.Npc.Id, (int)Ware.Fish, other.Id), ExpireTicks = 200 }).Ok);
            int startTick = (int)sim.State.Tick;
            sc.Probe.Step(220);
            var ends = sc.Probe.Of<CompulsionEnded>();
            Assert.AreEqual(2, ends.Count);
            Assert.AreEqual(ControlEndReason.Timeout, ends[1].Reason, "blocked to expiry");
            Assert.IsFalse(ends[1].Started, "the compelled intent never started");
            Assert.GreaterOrEqual(sc.Probe.Of<CompulsionBlocked>().Count, 1);
            Assert.AreEqual(0, sc.Probe.Of<ItemMoved>().Count(e => e.Reason == MoveReason.HandOver), "no ItemMoved hand-over");
            Assert.AreEqual(0, other.Carry.Count(l => !l.Empty && l.Ware == Ware.Fish), "no fish appears");
            Assert.AreEqual(fishBefore, sim.State.Population.Ledger.ProducedOf(Ware.Fish), "no fish produced");
        }

        // ---------------------------------------------------------------- D2-T7 (DP5)

        [Test]
        public void Compulsion_EndsOnDeathAndExhaustion()
        {
            var sc = D2Fixtures.Glade(42, InstinctSet.Default, 300);
            var sim = sc.Sim;
            int lot = GroundLot(sim, LearningFixtures.Step(sim, sc.Centre, 3, 6));
            Assert.IsTrue(sim.Submit(new IssueCompulsion { God = 1, Act = DivineAct.Take, Target = D2Fixtures.Person(sc.Npc.Id, 0, lot), ExpireTicks = 600 }).Ok);
            sc.Probe.Step(15);
            HouseholdFixtures.Kill(sim, sc.Npc.Id, DeathCause.Violence);
            sc.Probe.Step(30);
            var ends = sc.Probe.Of<CompulsionEnded>();
            Assert.AreEqual(1, ends.Count, "start/end pair once (CT1)");
            Assert.AreEqual(ControlEndReason.Death, ends[0].Reason);
            Assert.AreEqual(sc.Probe.Of<Population.NpcDied>().Single().Tick, ends[0].Tick, "same tick as the death");
            Assert.AreEqual(ControlKind.Self, Control.Of(sim.State, sc.Npc.Id));

            // upkeep exhausts the god: the spell ends the tick the god cannot pay, once
            var s2 = D2Fixtures.Glade(43, InstinctSet.Default, 300, t => { t.Divinity.Mythic.Start = 10_000 + 2_500; t.Divinity.Mythic.Upkeep.CompulsionPerTick = 1_000; });
            int lot2 = GroundLot(s2.Sim, LearningFixtures.Step(s2.Sim, s2.Centre, 3, 8));
            Assert.IsTrue(s2.Sim.Submit(new IssueCompulsion { God = 1, Act = DivineAct.Take, Target = D2Fixtures.Person(s2.Npc.Id, 0, lot2), ExpireTicks = 600 }).Ok);
            s2.Probe.Step(40);
            var e2 = s2.Probe.Of<CompulsionEnded>();
            Assert.AreEqual(1, e2.Count);
            Assert.AreEqual(ControlEndReason.MythicExhausted, e2[0].Reason);
            Assert.LessOrEqual(s2.Sim.State.Divinity.Gods.Of(1).Mythic, 1_000);
            long endTick = e2[0].Tick;
            var lastCompelled = s2.Sim.State.Population.Find(s2.Npc.Id).Mind.Last;
            Assert.IsTrue(lastCompelled.Tick < endTick || lastCompelled.Control == ControlKind.Self, "no compelled decision after the end");
            Assert.AreEqual(ControlKind.Self, Control.Of(s2.Sim.State, s2.Npc.Id));
        }

        // ---------------------------------------------------------------- D2-T8

        [Test]
        public void Compulsion_Precedence()
        {
            var sc = D2Fixtures.Glade(44, InstinctSet.Default, 300, NoUpkeep);
            var sim = sc.Sim;
            int lot = GroundLot(sim, LearningFixtures.Step(sim, sc.Centre, 3, 6));
            Assert.IsTrue(sim.Submit(new IssueCompulsion { God = 1, Act = DivineAct.Take, Target = D2Fixtures.Person(sc.Npc.Id, 0, lot), ExpireTicks = 600 }).Ok);
            sc.Probe.Step(2);
            // the Human seat passes to god 2 (sandbox), which tries to compel the same body
            Assert.IsTrue(sim.Submit(new SwitchGod { God = 1, From = 1, To = 2 }).Ok);
            sc.Probe.Step(1);
            Assert.AreEqual(DivineReasons.AlreadyControlled, sim.Submit(new IssueCompulsion { God = 2, Act = DivineAct.MoveTo, Target = D2Fixtures.Person(sc.Npc.Id, sc.Centre) }).Reason);
            Assert.IsTrue(sim.Submit(new SwitchGod { God = 2, From = 2, To = 1 }).Ok);
            sc.Probe.Step(1);
            // the same god's new compulsion replaces its own
            Assert.IsTrue(sim.Submit(new IssueCompulsion { God = 1, Act = DivineAct.MoveTo, Target = D2Fixtures.Person(sc.Npc.Id, sc.BushNode) }).Ok);
            sc.Probe.Step(1);
            var ends = sc.Probe.Of<CompulsionEnded>();
            Assert.AreEqual(1, ends.Count);
            Assert.AreEqual(ControlEndReason.Superseded, ends[0].Reason);
            Assert.AreEqual(2, sc.Probe.Of<CompulsionStarted>().Count);
            var e = Control.EntryOf(sim.State, sc.Npc.Id);
            Assert.AreEqual(DivineAct.MoveTo, e.Act);
            Assert.AreEqual(ends[0].Tick, sc.Probe.Of<CompulsionStarted>()[1].Tick, "superseded the tick the new one opened");
        }

        // ---------------------------------------------------------------- D2-T9

        private static (bool inContext, int keys, int sumAbs) CompelledLearning(int permille)
        {
            // a compelled walk of 8 nodes while hunger rises: the body's experience is the reward the trace receives
            var sc = D2Fixtures.Glade(45, InstinctSet.Default, 500, t => { NoUpkeep(t); t.Divinity.Instruction.CompelLearnPermille = permille; });
            var sim = sc.Sim;
            int goal = LearningFixtures.Step(sim, sc.Centre, 3, 8);
            Assert.IsTrue(sim.Submit(new IssueCompulsion { God = 1, Act = DivineAct.MoveTo, Target = D2Fixtures.Person(sc.Npc.Id, goal), ExpireTicks = 600 }).Ok);
            uint key = DivinityMindHooks.DivineKey(InterventionKind.Compulsion);
            bool inContext = false;
            for (int i = 0; i < 300 && sc.Probe.Of<CompulsionEnded>().Count == 0; i++)
            {
                sc.Probe.Step(1);
                var st0 = sc.Npc.Mind.Store;
                if (!inContext) for (int k = 0; k < st0.RingCount * AssociationStore.MaxKeys && k < st0.TraceKeys.Length; k++) if (st0.TraceKeys[k] == key) { inContext = true; break; }
            }
            Assert.AreEqual(ControlEndReason.Completed, sc.Probe.Of<CompulsionEnded>().Single().Reason);
            sc.Probe.Step(40); // the next thinks credit the compelled decision
            var st = sc.Npc.Mind.Store;
            int n = 0, sum = 0;
            for (int i = 0; i < st.Count; i++) if (st.Entries[i].Verb == Verb.Approach) { if (st.Entries[i].Key == key) n++; sum += Math.Abs(st.Entries[i].Stw) + Math.Abs(st.Entries[i].Ltw); }
            return (inContext, n, sum);
        }

        [Test]
        public void Compulsion_LearnsWithTag()
        {
            var full = CompelledLearning(1000);
            var none = CompelledLearning(0);
            TestContext.Progress.WriteLine("T9: entries of the compelled verb (Divine(Compelled) key count) at 1000 permille: " + full.keys + " (|w| " + full.sumAbs + "), at 0: " + none.keys + " (|w| " + none.sumAbs + ")");
            Assert.IsTrue(full.inContext && none.inContext, "the trace context carried Feature.Divine(Compelled)");
            Assert.Greater(full.sumAbs, 0, "full learning");
            Assert.Less(none.sumAbs, full.sumAbs, "rewards under compulsion scaled to 0");
        }

        // ---------------------------------------------------------------- save/load mid-spell (contracts 11.5 Control_SaveLoadMidSpell)

        [Test]
        public void D2_SaveLoadParity()
        {
            var sc = D2Fixtures.Glade(46, InstinctSet.Default, 600, NoUpkeep);
            var sim = sc.Sim;
            int lot = GroundLot(sim, LearningFixtures.Step(sim, sc.Centre, 3, 8));
            var b = LearningFixtures.Npc(sim, LearningFixtures.Step(sim, sc.Centre, 0, 1), InstinctSet.Default, LearningMode.On);
            var c = LearningFixtures.Npc(sim, LearningFixtures.Step(sim, sc.Centre, 1, 1), InstinctSet.Default, LearningMode.On);
            Assert.IsTrue(sim.Submit(new IssueCompulsion { God = 1, Act = DivineAct.Take, Target = D2Fixtures.Person(sc.Npc.Id, 0, lot), ExpireTicks = 600 }).Ok);
            Assert.IsTrue(sim.Submit(new IssueCompulsion { God = 1, Act = DivineAct.Give, Target = D2Fixtures.Person(b.Id, (int)Ware.Fish, c.Id), ExpireTicks = 600 }).Ok);
            sc.Probe.Step(25);
            Assert.AreEqual(ControlStatus.Running, Control.EntryOf(sim.State, sc.Npc.Id).Status);
            Assert.AreEqual(ControlStatus.Blocked, Control.EntryOf(sim.State, b.Id).Status);
            // an unread command to the third person (it has not thought since)
            Assert.IsTrue(sim.Submit(new IssueCommand { God = 1, Act = DivineAct.MoveTo, Target = D2Fixtures.Person(c.Id, sc.BushNode), Scope = DivineScope.Individual }).Ok);
            sim.Step();
            Assert.AreEqual(1, c.Mind.Divine.InboxCount);
            var json = SaveFile.From(sim).ToJson();
            var loaded = SaveFile.FromJson(json).ToSim();
            Assert.AreEqual(sim.StateHash(), loaded.StateHash());
            for (int i = 0; i < 40; i++)
            {
                sim.Step(10); loaded.Step(10);
                CollectionAssert.AreEqual(sim.StateHashParts(), loaded.StateHashParts(), "checkpoint " + i);
            }
        }
    }
}
