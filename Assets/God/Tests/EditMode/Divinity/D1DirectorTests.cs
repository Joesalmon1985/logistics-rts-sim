using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using God.Sim.Divinity;
using God.Sim.Information;
using God.Sim.Policy;
using God.Sim.Population;
using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>
    /// D1b (D1 spec 14): the god director, ScheduleGod, the configuration commands and the omniscient divine view.
    /// D1-T5 (MM5), T6, T7 (DP10), T8, T9, T10 (I-D5-3, I-D5-6), T11 (I-D5-1, I-D5-2), save/load parity. OWNER: D1.
    /// </summary>
    public class D1DirectorTests
    {
        private const int Day = D1MythicTests.Day;

        /// <summary>A built game (directors attached) with god 2 running ScheduleGod; the slots set by <paramref name="schedule"/>.</summary>
        internal static BuiltSim Built(ulong seed, DivinityMode mode, Action<GodSchedule> schedule, int think = 1, int humanGod = 1, int divinity = 1, Action<SimTuning> tune = null)
        {
            var t = new SimTuning();
            t.Divinity.Enabled = divinity; t.Divinity.Mode = mode; t.Divinity.Mythic.TestCommand = 1; t.Divinity.Mythic.GodThinkTicks = think;
            schedule?.Invoke(t.Divinity.Mythic.Schedule);
            tune?.Invoke(t);
            var setup = new GameSetup(seed, GameSetup.NoHuman, 2) { Population = true, StartNpcsPerFaction = 3, Victory = VictorySpec.SandboxOnly(), HumanGod = humanGod };
            var pol = PolicyAssignment.Stage3Default();
            pol.SetDefault(PolicyLevel.Civilian, Population.Policies.Associative);
            pol.Set(PolicyLevel.God, 2, GodPolicies.Schedule);
            return SimBuilder.Create(setup, t, true, pol);
        }

        private static ulong[] PartsExcept(GodSim sim, params string[] except)
        {
            var names = GodSim.PartNamesOf(sim.State);
            var parts = sim.StateHashParts();
            var l = new List<ulong>();
            for (int i = 0; i < parts.Length; i++) if (!except.Contains(names[i])) l.Add(parts[i]);
            return l.ToArray();
        }

        // ---------------------------------------------------------------- D1-T5 (MM5, P3 small)

        [Test]
        public void Disabled_RefusesAll()
        {
            var b = Built(31, DivinityMode.Disabled, s => { s.S1.Tick = 10; s.S1.Every = Day; s.S1.Template = ScheduleTemplate.TestSpend; s.S1.A = (int)InterventionKind.Miracle; s.S1.B = 1_000; });
            var results = new List<CommandResult>();
            b.Gods.Submitted += (g, tick, c, r) => results.Add(r);
            var off = Built(31, DivinityMode.Standard, null, divinity: 0);
            Assert.IsNull(off.Gods);
            for (int d = 0; d < 5; d++)
            {
                b.Sim.Step(Day); off.Sim.Step(Day);
                CollectionAssert.AreEqual(PartsExcept(off.Sim, "core", "pending"), PartsExcept(b.Sim, "core", "divine", "pending"), "day " + d + ": every part except core (tuning), divine and pending equals the off run");
            }
            Assert.AreEqual(5, results.Count, "one scripted command per day");
            Assert.IsTrue(results.All(r => r.Reason == DivineReasons.DivinityDisabled), string.Join(",", results.Select(r => r.Reason)));
            Assert.AreEqual(5, b.Gods.Refused);
            Assert.AreEqual(1, b.Sim.State.Divinity.NextInterventionId, "nothing opened");
        }

        // ---------------------------------------------------------------- D1-T6

        [Test]
        public void ScheduleGod_ActsInsideStep()
        {
            var b = Built(32, DivinityMode.Standard, s =>
            {
                s.S1.Tick = 7; s.S1.Template = ScheduleTemplate.TestSpend; s.S1.A = (int)InterventionKind.Miracle; s.S1.B = 1_000;
                s.S2.Tick = 50; s.S2.Template = ScheduleTemplate.TestSpend; s.S2.A = (int)InterventionKind.Command; s.S2.B = 2_000;
                s.S3.Tick = 333; s.S3.Template = ScheduleTemplate.TestSpend; s.S3.A = (int)InterventionKind.Suggestion; s.S3.B = 3_000;
            });
            var at = new List<(int god, long tick, bool inStep)>();
            b.Gods.Submitted += (g, tick, c, r) => { Assert.IsTrue(r.Ok, r.ToString()); at.Add((g, tick, b.Sim.InStep)); };
            int external = 0;
            b.Sim.ExternalCommandAccepted += c => external++;
            var probe = new DivineProbe(b.Sim);
            probe.Step(400);
            CollectionAssert.AreEqual(new[] { (2, 7L, true), (2, 50L, true), (2, 333L, true) }, at, "each submitted inside Step at its tick");
            Assert.AreEqual(0, external, "never logged as external");
            var acc = probe.Of<DivineActionAccepted>().ToList();
            CollectionAssert.AreEqual(new long[] { 8, 51, 334 }, acc.Select(e => e.Tick).ToArray(), "applied at the next tick head");
            Assert.IsTrue(acc.All(e => e.God == 2));
            // a staggered think interval finds the same commands one think later, never twice
            var b20 = Built(32, DivinityMode.Standard, s => { s.S1.Tick = 7; s.S1.Every = 13; s.S1.Until = 300; s.S1.Template = ScheduleTemplate.TestSpend; s.S1.A = 6; s.S1.B = 1; }, think: 20);
            int n = 0; b20.Gods.Submitted += (g, t, c, r) => n++;
            b20.Sim.Step(400);
            Assert.AreEqual(Enumerable.Range(0, 400).Count(k => 7 + 13L * k <= 300), n);
            // replay: the same build with no external command reproduces the hash (in-step commands are recomputed)
            var again = Built(32, DivinityMode.Standard, s =>
            {
                s.S1.Tick = 7; s.S1.Template = ScheduleTemplate.TestSpend; s.S1.A = (int)InterventionKind.Miracle; s.S1.B = 1_000;
                s.S2.Tick = 50; s.S2.Template = ScheduleTemplate.TestSpend; s.S2.A = (int)InterventionKind.Command; s.S2.B = 2_000;
                s.S3.Tick = 333; s.S3.Template = ScheduleTemplate.TestSpend; s.S3.A = (int)InterventionKind.Suggestion; s.S3.B = 3_000;
            });
            again.Sim.Step(400);
            Assert.AreEqual(b.Sim.StateHash(), again.Sim.StateHash());
        }

        // ---------------------------------------------------------------- D1-T7 (DP10)

        private sealed class ForeignGod : IGodPolicy
        {
            public string Name => "ForeignGod";
            public PolicyLevel Level => PolicyLevel.God;
            public void Decide(in IDivineView view, ref Rng rng, List<Command> outCommands)
            {
                outCommands.Add(new DivineTestSpend { God = 3, Kind = InterventionKind.Miracle, Cost = 1 });   // another god
                outCommands.Add(new SetMythicMode { God = view.God, Mode = DivinityMode.Unlimited });       // configuration: external only
                outCommands.Add(new DivineTestSpend { God = view.God, Kind = InterventionKind.Miracle, Cost = 1 }); // its own: admitted
            }
        }

        [Test]
        public void GodDirector_DropsForeignCommand()
        {
            var t = new SimTuning(); t.Divinity.Enabled = 1; t.Divinity.Mythic.TestCommand = 1; t.Divinity.Mythic.GodThinkTicks = 1;
            var setup = new GameSetup(33, GameSetup.NoHuman, 3) { Population = true, StartNpcsPerFaction = 2, Victory = VictorySpec.SandboxOnly(), HumanGod = 1 };
            var sim = GodSim.Create(setup, t);
            var reg = new PolicyRegistry();
            GodPolicies.Register(reg);
            reg.Register(PolicyLevel.God, "ForeignGod", ctx => new ForeignGod(), "test", testOnly: true);
            var a = PolicyAssignment.Stage3Default();
            a.Set(PolicyLevel.God, 2, "ForeignGod");
            var d = GodDirector.Attach(sim, a, reg);
            var probe = new DivineProbe(sim);
            probe.Step(1);
            Assert.IsInstanceOf<IdleGod>(d.PolicyOf(3));
            Assert.AreEqual(2, d.Inadmissible, "the foreign and the configuration command are dropped and counted");
            probe.Step(1);
            Assert.AreEqual(DivinityMode.Standard, sim.State.Divinity.Gods.Mode, "never applied");
            Assert.IsTrue(probe.Of<DivineActionAccepted>().All(e => e.God == 2));
            Assert.AreEqual(1, probe.Of<DivineActionAccepted>().Count());
            // the command types a god policy may emit equal the human's divine action set (no second vocabulary)
            var all = F1FoundationTests.DivineCommandTypes();
            var actions = DivineCommandSet.Types();
            CollectionAssert.AreEquivalent(all.Where(x => !DivineCommandSet.IsConfiguration(x)), actions);
            foreach (var x in actions) Assert.IsTrue(DivineCommandSet.Admissible(Make(x, 2), 2), x.Name);
            foreach (var x in all.Where(DivineCommandSet.IsConfiguration)) Assert.IsFalse(DivineCommandSet.Admissible(Make(x, 2), 2), x.Name);
            Assert.IsFalse(DivineCommandSet.Admissible(new DeclareWar(Faction.F1, Faction.F0), 2));
        }

        private static Command Make(Type t, int god) { var c = (DivineCommand)Activator.CreateInstance(t); c.God = god; return c; }


        // ---------------------------------------------------------------- D1-T8

        [Test]
        public void External_NotYourGod()
        {
            var b = Built(34, DivinityMode.Standard, null);
            ulong h = b.Sim.StateHash();
            foreach (var c in new DivineCommand[] { new DivineTestSpend { God = 2, Kind = InterventionKind.Miracle, Cost = 1 }, new SwitchGod { God = 2, From = 2, To = 1 }, new SetGodPolicy { God = 2, Policy = GodPolicies.Idle } })
                Assert.AreEqual(DivineReasons.NotYourGod, b.Sim.Submit(c).Reason, c.GetType().Name);
            Assert.AreEqual(h, b.Sim.StateHash(), "the D1 handler never ran");
            Assert.AreEqual(0, b.Sim.PendingCommands.Count);
        }

        // ---------------------------------------------------------------- D1-T9

        [Test]
        public void SwitchGod_MovesSeatOnly()
        {
            var b = Built(35, DivinityMode.Standard, null);
            var reg = b.Sim.State.Divinity.Gods;
            string Snap(GodRecord g) => g.Name + "|" + g.OriginFaction + "|" + g.PolicyName + "|" + g.Mythic + "|" + g.MythicMax + "|" + string.Join(",", g.CooldownUntil) + "|" + g.AvatarBody + "|" + g.Profile.Default + "|" + g.CreatedTick;
            b.Sim.Step(5);
            var before = reg.Gods.Select(Snap).ToList();
            var probe = new DivineProbe(b.Sim);
            Assert.IsTrue(b.Sim.Submit(new SwitchGod { God = 1, From = 1, To = 2 }).Ok);
            probe.Step(1);
            Assert.AreEqual(GodSeat.Ai, reg.Of(1).Seat); Assert.AreEqual(GodSeat.Human, reg.Of(2).Seat);
            CollectionAssert.AreEqual(before, reg.Gods.Select(Snap).ToList(), "no other GodRecord field changes");
            Assert.AreEqual(2, probe.Of<GodPolicyChanged>().Count());
            Assert.AreEqual(2, Gods.HumanSeatHolder(b.Sim.State));
            Assert.AreEqual(DivineReasons.NotYourGod, b.Sim.Submit(new DivineTestSpend { God = 1, Kind = InterventionKind.Miracle, Cost = 1 }).Reason, "god 1 is no longer the human's");
            Assert.IsTrue(b.Sim.Submit(new DivineTestSpend { God = 2, Kind = InterventionKind.Miracle, Cost = 1 }).Ok);
            // god 1 is now AI-seated (IdleGod) and god 2 (ScheduleGod) human: the director follows the state
            probe.Step(3);
            Assert.IsNull(b.Gods.PolicyOf(1) as ScheduleGod);
            // policy: set on the human's own god, then seat it back
            Assert.IsTrue(b.Sim.Submit(new SetGodPolicy { God = 2, Policy = GodPolicies.Schedule }).Ok);
            Assert.AreEqual(DivineReasons.BadTarget, b.Sim.Submit(new SetGodPolicy { God = 2, Policy = "NoSuchGod" }).Reason);
            Assert.AreEqual(DivineReasons.ActUnavailable, b.Sim.Submit(new SetDivineProfile { God = 2, Profile = new DivineObservationProfile { Default = DivinePerception.Perceived } }).Reason);
            Assert.IsTrue(b.Sim.Submit(new SetDivineProfile { God = 2, Profile = new DivineObservationProfile() }).Ok);
            probe.Step(1);
            Assert.AreEqual(4, probe.Of<GodPolicyChanged>().Count(), "two for the switch, one each for the policy and the profile");
            // non-sandbox: refused
            var t = new SimTuning(); t.Divinity.Enabled = 1;
            var ns = SimBuilder.Create(new GameSetup(35, GameSetup.NoHuman, 2) { Population = true, StartNpcsPerFaction = 3, HumanGod = 1 }, t);
            Assert.AreEqual(DivineReasons.NotSandbox, ns.Sim.Submit(new SwitchGod { God = 1, From = 1, To = 2 }).Reason);
            // replay of the switch: the same external command at the same tick reproduces the hash
            var r1 = Built(35, DivinityMode.Standard, null); var r2 = Built(35, DivinityMode.Standard, null);
            foreach (var x in new[] { r1, r2 }) { x.Sim.Step(5); x.Sim.Submit(new SwitchGod { God = 1, From = 1, To = 2 }); x.Sim.Step(2 * Day); }
            Assert.AreEqual(r1.Sim.StateHash(), r2.Sim.StateHash());
        }

        // ---------------------------------------------------------------- D1-T10 (I-D5-3, I-D5-6)

        [Test]
        public void DivineView_Omniscient_NoHashChange()
        {
            var a = Built(36, DivinityMode.Standard, s => { s.S1.Tick = 100; s.S1.Every = 600; s.S1.Template = ScheduleTemplate.TestSpend; s.S1.A = 6; s.S1.B = 500; });
            var b = Built(36, DivinityMode.Standard, s => { s.S1.Tick = 100; s.S1.Every = 600; s.S1.Template = ScheduleTemplate.TestSpend; s.S1.A = 6; s.S1.B = 500; });
            var views = Enumerable.Range(1, 2).Select(g => DivineViewFiller.For(b.Sim.State, g)).ToList();
            var gods = new List<DivineGodView>(); var people = new List<DivinePersonView>();
            int seen = 0;
            for (int tick = 0; tick < 5 * Day; tick++)
            {
                a.Sim.Step(); b.Sim.Step();
                foreach (var v in views)
                {
                    v.Gods(gods); v.People(people);
                    Assert.AreEqual(2, gods.Count); Assert.AreEqual(b.Sim.State.Tick, v.AsOfTick);
                    foreach (var p in people) { Assert.IsTrue(v.TryPerson(p.Npc, out var q)); Assert.AreEqual(KnowledgeLayer.DivineObservation, q.Layer); seen++; }
                    Assert.AreEqual(DivinePerception.Omniscient, v.Profile.Default);
                }
                if (tick % 600 == 0) Assert.AreEqual(a.Sim.StateHash(), b.Sim.StateHash(), "tick " + tick);
            }
            Assert.AreEqual(a.Sim.StateHash(), b.Sim.StateHash());
            Assert.Greater(seen, 0);
            Assert.IsTrue(views[0].TryGod(2, out var g2));
            Assert.AreEqual(b.Sim.State.Divinity.Gods.Of(2).Mythic, g2.Mythic);
            Assert.AreEqual(GodSeat.Ai, g2.Seat); Assert.AreEqual(GodPolicies.Schedule, g2.PolicyName);
            Assert.AreEqual(20_000, g2.RegenPerDay);
            // the dead are seen too
            var dead = b.Sim.State.Population.Npcs.FirstOrDefault(n => Bodies.IsPerson(n) && !n.Alive);
            if (dead != null) { Assert.IsTrue(views[0].TryPerson(dead.Id, out var dv)); Assert.IsFalse(dv.Alive); }
        }

        // ---------------------------------------------------------------- D1-T11 (I-D5-1, I-D5-2)

        private static readonly Type[] Forbidden = { typeof(GodState), typeof(PopulationState), typeof(ITrueStateOverlay) };

        private static IEnumerable<Type> Referenced(Type t)
        {
            const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            foreach (var f in t.GetFields(all)) yield return f.FieldType;
            foreach (var p in t.GetProperties(all)) yield return p.PropertyType;
            foreach (var m in t.GetMethods(all)) { yield return m.ReturnType; foreach (var p in m.GetParameters()) yield return p.ParameterType; }
            foreach (var c in t.GetConstructors(all)) foreach (var p in c.GetParameters()) yield return p.ParameterType;
        }

        private static Type Bare(Type t) { while (t.HasElementType) t = t.GetElementType(); return t; }

        [Test]
        public void GodPolicy_SeesOnlyItsView()
        {
            var types = typeof(GodSim).Assembly.GetTypes();
            var gods = types.Where(t => typeof(IGodPolicy).IsAssignableFrom(t) && !t.IsInterface).ToList();
            Assert.GreaterOrEqual(gods.Count, 2);
            foreach (var g in gods)
                foreach (var r in Referenced(g).Select(Bare))
                    Assert.IsFalse(Forbidden.Contains(r), g.Name + " references " + r.Name);
            var others = types.Where(t => !t.IsInterface && (typeof(INpcPolicy).IsAssignableFrom(t) || typeof(IInstitutionPolicy).IsAssignableFrom(t) || typeof(IFactionPolicy).IsAssignableFrom(t) || typeof(IMilitaryPolicy).IsAssignableFrom(t))).ToList();
            Assert.Greater(others.Count, 0);
            foreach (var o in others)
                foreach (var r in Referenced(o).Select(Bare))
                    Assert.IsFalse(typeof(IDivineView).IsAssignableFrom(r) || r == typeof(DivineGodView) || r == typeof(DivinePersonView), o.Name + " references the divine view");
        }

        // ---------------------------------------------------------------- D1-T14 D1_SaveLoadParity

        [Test]
        public void D1_SaveLoadParity()
        {
            Action<GodSchedule> sched = s => { s.S1.Tick = 30; s.S1.Every = 400; s.S1.Template = ScheduleTemplate.TestSpend; s.S1.A = (int)InterventionKind.Miracle; s.S1.B = 2_000; };
            Action<SimTuning> cd = t => t.Divinity.Mythic.Cooldown.MiracleTicks = 900;
            var a = Built(37, DivinityMode.Standard, sched, think: 20, tune: cd);
            var b = Built(37, DivinityMode.Standard, sched, think: 20, tune: cd);
            a.Sim.Step(1700); b.Sim.Step(1700); // mid-day, a cooldown running
            Assert.Greater(b.Sim.State.Divinity.Gods.Of(2).CooldownUntil[(int)InterventionKind.Miracle], b.Sim.State.Tick, "a cooldown is running");
            var json = SaveFile.From(b.Sim).ToJson();
            var loaded = SimBuilder.FromSave(SaveFile.FromJson(json));
            Assert.AreEqual(a.Sim.StateHash(), loaded.Sim.StateHash());
            Assert.AreEqual(GodPolicies.Schedule, loaded.Sim.State.Divinity.Gods.Of(2).PolicyName, "seats come from state");
            for (int k = 0; k < 8; k++)
            {
                a.Sim.Step(600); loaded.Sim.Step(600);
                Assert.AreEqual(a.Sim.StateHash(), loaded.Sim.StateHash(), "checkpoint " + k);
            }
            Assert.Greater(a.Sim.State.Divinity.NextInterventionId, 3);
            var probe = new DivineProbe(loaded.Sim);
            probe.Step(10);
            Assert.AreEqual(0, probe.Count("GodCreated"), "G4: GodCreated is not emitted again after a load");
        }
    }
}
