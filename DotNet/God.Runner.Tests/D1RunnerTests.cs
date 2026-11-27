using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using God.Recording;
using God.Runner;
using God.Runner.Warehouse;
using God.Sim;
using God.Sim.Policy;
using Microsoft.Data.Sqlite;
using NUnit.Framework;

namespace God.Runner.Tests
{
    /// <summary>
    /// D1 runner tests (D1 spec 14, 15; contracts 16): pins P2 and P3 over the contracts' scenarios and seeds, the d1_gods
    /// demonstration scenario, its warehouse reconciliation (D1_Reconciles), recording neutrality (Off/Full), the replay of
    /// in-step divine commands and verify --save-mid. OWNER: D1.
    /// </summary>
    [TestFixture]
    public class D1RunnerTests
    {
        private string _dir;
        [SetUp] public void SetUp() { _dir = TestUtil.TempDir("d1"); }
        [TearDown] public void TearDown() { SqliteConnection.ClearAllPools(); TestUtil.Cleanup(_dir); }

        private const int CoreIndex = 5;

        /// <summary>
        /// Compares an on-run with the off run, every checkpoint: the on run has the same parts plus <c>divine</c> appended
        /// (with a population); every part except <c>core</c> (it holds the tuning JSON, so the switch itself changes it) and
        /// the listed ones equals the off run.
        /// </summary>
        private static void AssertNeutral(string key, RunResult off, RunResult on, params int[] alsoSkip)
        {
            Assert.AreEqual(0, off.ExitCode, key + ": " + off.Error); Assert.AreEqual(0, on.ExitCode, key + ": " + on.Error);
            Assert.AreEqual(off.Checkpoints.Count, on.Checkpoints.Count, key);
            for (int c = 0; c < off.Checkpoints.Count; c++)
            {
                var a = off.Checkpoints[c].Parts; var b = on.Checkpoints[c].Parts;
                bool pop = a.Length > 7;
                Assert.AreEqual(a.Length + (pop ? 1 : 0), b.Length, key + " checkpoint " + c + ": divine appended");
                for (int i = 0; i < a.Length; i++)
                {
                    if (i == CoreIndex || alsoSkip.Contains(i)) continue;
                    Assert.AreEqual(a[i], b[i], key + " checkpoint " + c + " part " + GodSim.AllHashPartNames[i]);
                }
            }
        }

        private static void Pin(Func<RunSpec, RunSpec> on, params int[] alsoSkip)
        {
            var keys = new List<string>(); var specs = new List<RunSpec>();
            foreach (var sc in F1NeutralityTests.Scenarios)
                for (ulong seed = 1; seed <= 3; seed++)
                {
                    keys.Add(sc + "/" + seed);
                    specs.Add(F1NeutralityTests.Spec(sc, seed));
                    specs.Add(on(F1NeutralityTests.Spec(sc, seed, "tuning.Divinity.Enabled=1")));
                }
            var r = TestUtil.RunAll(specs);
            for (int i = 0; i < keys.Count; i++) AssertNeutral(keys[i], r[2 * i], r[2 * i + 1], alsoSkip);
        }

        /// <summary>P2: Divinity.Enabled = 1, IdleGod for every god, no external command: every part except divine (and core's tuning) equals P1.</summary>
        [Test]
        public void Divinity_Idle_Is_Neutral() => Pin(s => { s.Scenario.Policies.SetDefault(PolicyLevel.God, GodPolicies.Idle); return s; });

        /// <summary>P3: Mode = Disabled, a ScheduleGod commanding every day for every god: all refused; every part except divine and pending equals P1.</summary>
        [Test]
        public void Divinity_Disabled_Mode_Neutral() => Pin(s =>
        {
            foreach (var kv in new Dictionary<string, long>
            {
                ["Divinity.Mode"] = 2, ["Divinity.Mythic.TestCommand"] = 1, ["Divinity.Mythic.Schedule.S1.Tick"] = 100, ["Divinity.Mythic.Schedule.S1.Every"] = 1200,
                ["Divinity.Mythic.Schedule.S1.Template"] = 1, ["Divinity.Mythic.Schedule.S1.A"] = 6, ["Divinity.Mythic.Schedule.S1.B"] = 1000,
            }) s.Scenario.Tuning[kv.Key] = kv.Value;
            s.Scenario.Policies.SetDefault(PolicyLevel.God, GodPolicies.Schedule);
            return s;
        }, 6 /* pending */);

        // ---------------------------------------------------------------- d1_gods

        private RunSpec Demo(RecordingLevel level, string tag, params string[] sets)
        {
            var spec = TestUtil.Spec("d1_gods", 1, 12000, sets);
            spec.Scenario.Checks.Remove("smoke");
            spec.CheckpointEvery = 1200;
            spec.Level = level;
            if (level != RecordingLevel.Off) { spec.Sqlite = new WarehouseOptions { ForeignKeys = true, Async = false }; spec.SkipJsonl = true; }
            spec.OutRoot = _dir; spec.RunDir = Path.Combine(_dir, tag);
            return spec;
        }

        [Test]
        public void D1_Reconciles()
        {
            GodSim sim = null;
            var full = Demo(RecordingLevel.Full, "full");
            full.BeforeStep = x => sim = x;
            var off = Demo(RecordingLevel.Off, "off");
            var a = RunEngine.Run(full); var b = RunEngine.Run(off);
            Assert.IsNull(a.Error, a.Error); Assert.IsNull(b.Error, b.Error);
            CollectionAssert.AreEqual(TestUtil.Seq(b), TestUtil.Seq(a), "recording Off vs Full changes no hash");
            var reg = sim.State.Divinity.Gods;
            Assert.Greater(sim.State.Divinity.NextInterventionId, 5, "the scheduled god acted");
            using (var d = WarehouseDb.OpenRead(a.WarehousePath))
            {
                Assert.AreEqual(WarehouseDb.CurrentVersion, d.SchemaVersion());
                long Ev(int code) => Convert.ToInt64(d.Scalar("SELECT COALESCE(SUM(events), 0) FROM fact_event_day WHERE event_code = " + code));
                long Rows(string sql) => Convert.ToInt64(d.Scalar(sql));
                Assert.AreEqual(6L, Rows("SELECT COUNT(*) FROM dim_god"));
                Assert.AreEqual(Ev(800), Rows("SELECT COUNT(*) FROM dim_god"));
                Assert.AreEqual(Ev(801), Rows("SELECT COUNT(*) FROM fact_god_faction"));
                Assert.AreEqual(Ev(800) + Ev(802) + 6 * Ev(803), Rows("SELECT COUNT(*) FROM dim_god_history"));
                Assert.AreEqual(Ev(800) + Ev(804) + Ev(805) + Ev(806), Rows("SELECT COUNT(*) FROM fact_mythic_ledger"));
                Assert.AreEqual(Ev(805), Rows("SELECT COUNT(*) FROM fact_mythic_ledger WHERE event_code = 805"));
                Assert.AreEqual(Ev(807), Rows("SELECT COUNT(*) FROM fact_divine_action"));
                Assert.AreEqual(Ev(809), Rows("SELECT COUNT(*) FROM fact_mythic_balance_day"));
                Assert.AreEqual(0L, Rows("SELECT COUNT(*) FROM fact_divine_observation"));
                Assert.AreEqual(60L, Ev(809), "6 gods x 10 finished days");
                Assert.Greater(Ev(807), 5L);
                // M1: the ledger sums reproduce every god's balance
                foreach (var g in reg.Gods)
                    Assert.AreEqual(g.Mythic, Rows("SELECT SUM(amount) FROM fact_mythic_ledger WHERE god_key = " + g.Id), "god " + g.Id);
                Assert.AreEqual(Rows("SELECT -SUM(amount) FROM fact_mythic_ledger WHERE event_code = 805"), Rows("SELECT SUM(charged) FROM fact_divine_action"));
            }
        }

        [Test]
        public void D1_Disabled_RefusesEverything()
        {
            GodSim sim = null;
            var spec = Demo(RecordingLevel.Off, "disabled", "tuning.Divinity.Mode=2");
            spec.BeforeStep = x => sim = x;
            var r = RunEngine.Run(spec);
            Assert.IsNull(r.Error, r.Error);
            Assert.AreEqual(1, sim.State.Divinity.NextInterventionId, "nothing accepted");
            Assert.AreEqual(6, sim.State.Divinity.Gods.Gods.Count, "gods exist");
        }

        /// <summary>D1-T6 (runner half): the scheduled god's commands are in-step: none is logged, and the replay reproduces the hash.</summary>
        [Test]
        public void Replay_ScheduleGod_NoCommandsLogged()
        {
            var spec = Demo(RecordingLevel.Off, "replay");
            spec.WriteReplay = true;
            var r = RunEngine.Run(spec);
            Assert.IsNull(r.Error, r.Error);
            string file = Path.Combine(r.RunDir, "replay.godr");
            var rf = ReplayFile.Read(file);
            Assert.AreEqual(0, rf.Of(ReplayEntryKind.Command).Count(), "no command in the replay body");
            var again = RunEngine.Run(Cli.ReplaySpec(file, default));
            Assert.IsNull(again.Divergence, again.Divergence?.ToString());
            Assert.AreEqual(r.FinalHash, again.FinalHash);
        }

        [Test]
        public void Verify_SaveMid_d1_gods()
        {
            int code = TestUtil.Cli(out var o, out var e, "verify", "--scenario", "d1_gods", "--seed", "1", "--days", "10", "--save-mid");
            Assert.AreEqual(ExitCodes.Ok, code, o + e);
        }

        [Test]
        public void Scenario_GodPoliciesAndSetup_RoundTrip()
        {
            var sc = TestUtil.Load("d1_gods");
            Assert.AreEqual(GodPolicies.Schedule, sc.Policies.NameFor(PolicyLevel.God, 1));
            Assert.AreEqual(GodPolicies.Idle, sc.Policies.NameFor(PolicyLevel.God, 2));
            Assert.AreEqual(PlayerControl.GodOnly, sc.Control);
            var again = Scenario.Parse(sc.ToJson().ToString());
            Assert.AreEqual(sc.ToJson().ToString(), again.ToJson().ToString());
            Assert.Throws<ScenarioException>(() => Scenario.Parse("{\"scenarioVersion\":1,\"name\":\"x\",\"setup\":{},\"policies\":{\"god\":{\"0\":\"IdleGod\"}}}"));
        }
    }
}
