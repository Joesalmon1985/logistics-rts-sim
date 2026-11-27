using System;
using System.Collections.Generic;
using System.Linq;
using God.Sim.Divinity;
using God.Sim.Policy;
using God.Sim.Population;
using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>
    /// D1a (D1 spec 14): the god registry, Mythic and the divinity system. D1-T1 (DP11), T2 (MM1), T3 (MM2), T4 (MM4),
    /// T13 (MM3 precondition), plus G4 and the config commands' gating. OWNER: D1.
    /// </summary>
    public class D1MythicTests
    {
        internal const int Day = 1200;

        /// <summary>A population game with gods (Divinity.Enabled = 1), sandbox victory, the test command on.</summary>
        internal static GodSim Game(ulong seed, int factions = 2, int humanGod = 1, DivinityMode mode = DivinityMode.Standard, Action<SimTuning> tune = null, int npcs = 3)
        {
            var t = new SimTuning();
            t.Divinity.Enabled = 1; t.Divinity.Mode = mode; t.Divinity.Mythic.TestCommand = 1;
            tune?.Invoke(t);
            var setup = new GameSetup(seed, GameSetup.NoHuman, factions) { Population = true, StartNpcsPerFaction = npcs, Victory = VictorySpec.SandboxOnly(), HumanGod = humanGod };
            return GodSim.Create(setup, t);
        }

        internal static List<string> Bad(GodSim sim) { var l = new List<string>(); DivinityModule.Check(sim.State, l); return l; }

        internal static MythicTrace TraceOf(GodSim sim) => MythicTrace.Start(sim.State.Divinity.Gods.Gods.Select(g => (g.Id, g.Mythic)));

        // ---------------------------------------------------------------- D1-T1 (DP11, G1, G2, G4)

        [Test]
        public void Gods_OnePerFaction_Persist()
        {
            var sim = Game(21, factions: 6, humanGod: 0, npcs: 3);
            var reg = sim.State.Divinity.Gods;
            Assert.AreEqual(6, reg.Gods.Count);
            for (int i = 0; i < 6; i++) { Assert.AreEqual(i + 1, reg.Gods[i].Id); Assert.AreEqual((Faction)i, reg.Gods[i].OriginFaction); Assert.AreEqual(GodSeat.Ai, reg.Gods[i].Seat); Assert.IsFalse(string.IsNullOrEmpty(reg.Gods[i].Name)); }
            var probe = new DivineProbe(sim);
            probe.Step(1);
            Assert.AreEqual(6, probe.Count("GodCreated"), "G4: one GodCreated per god on the first divinity tick");
            Assert.AreEqual(6, probe.Count("GodAssociated"));
            // the whole population of faction F2 dies by script (god 3's people)
            var victims = sim.State.Population.Npcs.Where(n => Bodies.IsLivingPerson(n) && n.Faction == Faction.F2).ToList();
            Assert.Greater(victims.Count, 0);
            var ev = new EventBuffer();
            foreach (var n in victims) PopulationModule.Kill(sim.State, ev, n, DeathCause.Violence);
            int before = sim.State.Population.Npcs.Count;
            var nextId = sim.State.Population.NextNpcId;
            probe.Step(5 * Day);
            Assert.AreEqual(6, reg.Gods.Count, "G1: never shrinks");
            Assert.AreEqual(0, sim.State.Population.Npcs.Count(n => Bodies.IsLivingPerson(n) && n.Faction == Faction.F2 && victims.Any(v => v.Id == n.Id)), "the dead stay dead");
            var credited3 = probe.Of<MythicCredited>().Where(e => e.God == 3).ToList();
            Assert.AreEqual(5, credited3.Count, "the extinct faction's god keeps getting MythicCredited daily");
            Assert.AreEqual(0, probe.Count("GodCreated") - 6, "GodCreated only once");
            // no person is created by any divine code: every new NpcId comes with a birth or immigration event
            int born = probe.Probe.Events.Count(e => e is Population.Households.NpcBorn || e is Population.Households.NpcImmigrated);
            Assert.AreEqual(sim.State.Population.NextNpcId - nextId, born, "every new person is a birth or an immigrant");
            CollectionAssert.IsEmpty(Bad(sim));
        }

        // ---------------------------------------------------------------- D1-T2 (MM1)

        [Test]
        public void Mythic_RegenExact()
        {
            var sim = Game(22, tune: t => { t.Divinity.Mythic.Start = 0; t.Divinity.Mythic.Max = 1_000_000; });
            var probe = new DivineProbe(sim);
            probe.Step(10 * Day);
            var cr = probe.Of<MythicCredited>().Where(e => e.God == 1).ToList();
            Assert.AreEqual(10, cr.Count);
            Assert.AreEqual(200_000, cr.Sum(e => e.Amount), "credited exactly 200 Mythic");
            Assert.AreEqual(200_000, sim.State.Divinity.Gods.Of(1).Mythic);
            Assert.AreEqual(10, probe.Of<MythicBalanceDay>().Count(e => e.God == 1));

            var capped = Game(22, tune: t => { t.Divinity.Mythic.Start = 0; t.Divinity.Mythic.Max = 100_000; });
            var p2 = new DivineProbe(capped);
            long excess = 0;
            for (int d = 0; d < 10; d++)
            {
                p2.Step(Day);
                Assert.LessOrEqual(capped.State.Divinity.Gods.Of(1).Mythic, 100_000, "never above max");
            }
            var c2 = p2.Of<MythicCredited>().Where(e => e.God == 1).ToList();
            excess = c2.Sum(e => e.Capped);
            Assert.AreEqual(100_000, c2.Sum(e => e.Amount));
            Assert.AreEqual(100_000, excess, "the excess is reported in MythicCredited");
            Assert.AreEqual(100_000, capped.State.Divinity.Gods.Of(1).Mythic);
        }

        // ---------------------------------------------------------------- D1-T3 (MM2)

        [Test]
        public void Mythic_SpendRefuse()
        {
            var sim = Game(23, tune: t => { t.Divinity.Mythic.Start = 3_000; });
            var trace = TraceOf(sim);
            var probe = new DivineProbe(sim);
            ulong before = sim.StateHash();
            var r = sim.Submit(new DivineTestSpend { God = 1, Kind = InterventionKind.Compulsion, Cost = 10_000 });
            Assert.AreEqual(DivineReasons.MythicShort, r.Reason);
            Assert.AreEqual(before, sim.StateHash(), "no state change");
            sim.State.Divinity.Gods.Of(1).Mythic = 12_000; // test-only state write
            trace = TraceOf(sim);
            Assert.IsTrue(sim.Submit(new DivineTestSpend { God = 1, Kind = InterventionKind.Compulsion, Cost = 10_000 }).Ok);
            probe.Step(1);
            Assert.AreEqual(2_000, sim.State.Divinity.Gods.Of(1).Mythic, "balance falls by exactly 10 000 milli");
            var spent = probe.Of<MythicSpent>().Single();
            Assert.AreEqual(10_000, spent.Amount); Assert.IsFalse(spent.Waived); Assert.AreEqual(1, spent.Intervention);
            var acc = probe.Of<DivineActionAccepted>().Single();
            Assert.AreEqual(1, acc.Intervention); Assert.AreEqual(InterventionKind.Compulsion, acc.Kind); Assert.AreEqual(10_000, acc.Cost);
            Assert.AreEqual(2, sim.State.Divinity.NextInterventionId);
            Assert.IsTrue(Interventions.TryGet(sim.State, 1, out var rec)); Assert.AreEqual(1, rec.God);
            // a second spend falls short; several days of regeneration later the trace reconstructs every balance
            Assert.AreEqual(DivineReasons.MythicShort, sim.Submit(new DivineTestSpend { God = 1, Kind = InterventionKind.Miracle, Cost = 5_000 }).Reason);
            probe.Step(3 * Day);
            Assert.IsTrue(sim.Submit(new DivineTestSpend { God = 1, Kind = InterventionKind.Miracle, Cost = 5_000 }).Ok);
            probe.Step(Day);
            trace.AddRange(probe.Probe.Events);
            foreach (var g in sim.State.Divinity.Gods.Gods) Assert.AreEqual(g.Mythic, trace.Balance(g.Id), "M1 trace for god " + g.Id);
            CollectionAssert.IsEmpty(Bad(sim));
        }

        [Test]
        public void Spend_Cooldown_And_Gate()
        {
            var sim = Game(24, tune: t => { t.Divinity.Mythic.Cooldown.MiracleTicks = 100; });
            Assert.IsTrue(sim.Submit(new DivineTestSpend { God = 1, Kind = InterventionKind.Miracle, Cost = 1_000 }).Ok);
            sim.Step();
            Assert.AreEqual(1 + 100, sim.State.Divinity.Gods.Of(1).CooldownUntil[(int)InterventionKind.Miracle]);
            Assert.AreEqual(DivineReasons.Cooldown, sim.Submit(new DivineTestSpend { God = 1, Kind = InterventionKind.Miracle, Cost = 1_000 }).Reason);
            Assert.IsTrue(sim.Submit(new DivineTestSpend { God = 1, Kind = InterventionKind.Command, Cost = 1_000 }).Ok, "cooldowns are per kind");
            // D1-T8 External_NotYourGod: god 2 is AI-seated; the F1 gate refuses before the handler
            Assert.AreEqual(DivineReasons.NotYourGod, sim.Submit(new DivineTestSpend { God = 2, Kind = InterventionKind.Command, Cost = 1_000 }).Reason);
            Assert.AreEqual(DivineReasons.NotYourGod, sim.Submit(new SetMythicMode { God = 2, Mode = DivinityMode.Unlimited }).Reason);
            Assert.IsTrue(DivinityModule.HoldsHumanSeat(sim.State, 1));
            Assert.IsFalse(DivinityModule.HoldsHumanSeat(sim.State, 2));
        }

        // ---------------------------------------------------------------- D1-T4 (MM4)

        [Test]
        public void Unlimited_Records_NoCharge()
        {
            var sim = Game(25, mode: DivinityMode.Unlimited, tune: t => { t.Divinity.Mythic.Start = 3_000; });
            var g = sim.State.Divinity.Gods.Of(1);
            Assert.AreEqual(g.MythicMax, g.Mythic, "Unlimited starts at max");
            var trace = TraceOf(sim);
            var probe = new DivineProbe(sim);
            Assert.IsTrue(sim.Submit(new DivineTestSpend { God = 1, Kind = InterventionKind.Compulsion, Cost = 10_000 }).Ok);
            Assert.IsTrue(sim.Submit(new DivineTestSpend { God = 1, Kind = InterventionKind.Miracle, Cost = 500_000 }).Ok, "more than max: still accepted");
            probe.Step(Day + 1);
            Assert.AreEqual(g.MythicMax, g.Mythic, "balance held at max");
            var spends = probe.Of<MythicSpent>().ToList();
            Assert.AreEqual(2, spends.Count);
            CollectionAssert.AreEqual(new long[] { 10_000, 500_000 }, spends.Select(e => e.Nominal).ToArray(), "nominal costs recorded");
            Assert.IsTrue(spends.All(e => e.Waived && e.Amount == 0));
            Assert.AreEqual(510_000, probe.Of<MythicBalanceDay>().Single(e => e.God == 1).Waived);
            Assert.AreEqual(0, probe.Of<DivineActionAccepted>().Sum(e => e.Charged));
            trace.AddRange(probe.Probe.Events);
            Assert.AreEqual(g.Mythic, trace.Balance(1));
            // switching to Standard and back: the top-up to max is credited, so the trace still holds
            Assert.IsTrue(sim.Submit(new SetMythicMode { God = 1, Mode = DivinityMode.Standard }).Ok);
            Assert.IsTrue(sim.Submit(new DivineTestSpend { God = 1, Kind = InterventionKind.Command, Cost = 30_000 }).Ok);
            probe.Step(1);
            Assert.AreEqual(g.MythicMax - 30_000, g.Mythic);
            Assert.IsTrue(sim.Submit(new SetMythicMode { God = 1, Mode = DivinityMode.Unlimited }).Ok);
            probe.Step(1);
            Assert.AreEqual(g.MythicMax, g.Mythic);
            Assert.AreEqual(2, probe.Count("MythicConfigChanged"));
            var t2 = TraceOf(Game(25, mode: DivinityMode.Unlimited, tune: t => { t.Divinity.Mythic.Start = 3_000; }));
            t2.AddRange(probe.Probe.Events);
            Assert.AreEqual(g.Mythic, t2.Balance(1), "M1 across mode changes");
        }

        // ---------------------------------------------------------------- D1-T5 (MM5) unit part (the scripted god is in D1DirectorTests)

        [Test]
        public void Disabled_RefusesActions_ConfigStillWorks()
        {
            var sim = Game(26, mode: DivinityMode.Disabled);
            ulong before = sim.StateHash();
            Assert.AreEqual(DivineReasons.DivinityDisabled, sim.Submit(new DivineTestSpend { God = 1, Kind = InterventionKind.Miracle, Cost = 1 }).Reason);
            Assert.AreEqual(before, sim.StateHash(), "state untouched");
            Assert.AreEqual(2, sim.State.Divinity.Gods.Gods.Count, "gods and records exist");
            Assert.IsTrue(sim.Submit(new SetMythicMode { God = 1, Mode = DivinityMode.Standard }).Ok, "configuration is not affected by the mode");
        }

        // ---------------------------------------------------------------- D1-T13 (MM3 precondition)

        [Test]
        public void Upkeep_ShortfallSignal()
        {
            var sim = Game(27, tune: t => { t.Divinity.Mythic.Upkeep.PossessionPerTick = 50; });
            var s = sim.State;
            s.Divinity.Gods.Of(1).Mythic = 1_000; // test-only state write
            var trace = TraceOf(sim);
            var body = s.Population.Npcs.First(n => Bodies.IsLivingPerson(n));
            // a test spell (D4 makes the real one): a possession entry opened through the F1 control API
            Assert.IsTrue(Control.TryAcquire(s, new EventBuffer(), new ControlEntry { Kind = ControlKind.Possessed, God = 1, Body = body.Id, Intervention = 1 }, out var why), why);
            var probe = new DivineProbe(sim);
            for (int t = 1; t <= 19; t++) { probe.Step(1); Assert.AreEqual(ControlKind.Possessed, Control.Of(s, body.Id), "tick " + t); }
            Assert.AreEqual(50, s.Divinity.Gods.Of(1).Mythic);
            probe.Step(1);
            Assert.AreEqual(20, s.Tick);
            Assert.AreEqual(ControlKind.Self, Control.Of(s, body.Id), "shortfall exactly at tick 20: the spell ends that tick");
            Assert.AreEqual(0, s.Divinity.Gods.Of(1).Mythic);
            probe.Step(2 * Day);
            var up = probe.Of<MythicUpkeepCharged>().ToList();
            Assert.AreEqual(1, up.Count, "one aggregated MythicUpkeepCharged per god per day");
            Assert.AreEqual(1, up[0].God); Assert.AreEqual(InterventionKind.Possession, up[0].Kind); Assert.AreEqual(20, up[0].Ticks); Assert.AreEqual(1_000, up[0].Amount);
            trace.AddRange(probe.Probe.Events);
            Assert.AreEqual(s.Divinity.Gods.Of(1).Mythic, trace.Balance(1), "M1 with upkeep");
            CollectionAssert.IsEmpty(Bad(sim));
        }

        // ---------------------------------------------------------------- neutrality basics

        [Test]
        public void Off_NoState_NoSystem()
        {
            var setup = new GameSetup(28, GameSetup.NoHuman, 2) { Population = true, StartNpcsPerFaction = 3 };
            var sim = GodSim.Create(setup, new SimTuning());
            Assert.IsNull(sim.State.Divinity);
            Assert.IsFalse(sim.Systems.Any(x => x is DivinitySystem));
            Assert.IsFalse(sim.HasHandler(typeof(SwitchGod)));
            Assert.IsNull(SimBuilder.AttachGods(sim));
        }

        [Test]
        public void Config_RefusedOutsideSandbox()
        {
            var t = new SimTuning(); t.Divinity.Enabled = 1;
            var setup = new GameSetup(29, GameSetup.NoHuman, 2) { Population = true, StartNpcsPerFaction = 2, HumanGod = 1 };
            var sim = GodSim.Create(setup, t);
            Assert.AreEqual(DivineReasons.NotSandbox, sim.Submit(new SwitchGod { God = 1, From = 1, To = 2 }).Reason);
            Assert.AreEqual(DivineReasons.NotSandbox, sim.Submit(new SetMythicMode { God = 1, Mode = DivinityMode.Unlimited }).Reason);
            Assert.AreEqual(DivineReasons.NotSandbox, sim.Submit(new SetGodPolicy { God = 1, Policy = GodPolicies.Idle }).Reason);
            Assert.AreEqual(DivineReasons.NotSandbox, sim.Submit(new SetDivineProfile { God = 1, Profile = new Information.DivineObservationProfile() }).Reason);
        }
    }
}
