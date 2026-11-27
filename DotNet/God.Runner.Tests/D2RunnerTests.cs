using System;
using System.IO;
using God.Recording;
using God.Runner;
using God.Runner.Warehouse;
using God.Sim;
using God.Sim.Divinity;
using God.Sim.Population;
using Microsoft.Data.Sqlite;
using NUnit.Framework;

namespace God.Runner.Tests
{
    /// <summary>D2 runner tests (D2 spec 13-15): the d2_command demonstration, its warehouse reconciliation (D2-T14), recording neutrality, replay and save/load (D2-T13). OWNER: D2.</summary>
    [TestFixture]
    public class D2RunnerTests
    {
        private string _dir;
        [SetUp] public void SetUp() { _dir = TestUtil.TempDir("d2"); }
        [TearDown] public void TearDown() { SqliteConnection.ClearAllPools(); TestUtil.Cleanup(_dir); }

        private RunSpec Demo(RecordingLevel level, string tag)
        {
            var spec = TestUtil.Spec("d2_command", 1, 7200);
            spec.Scenario.Checks.Remove("smoke");
            spec.CheckpointEvery = 1200;
            spec.Level = level;
            if (level != RecordingLevel.Off) { spec.Sqlite = new WarehouseOptions { Async = false }; spec.SkipJsonl = true; }
            spec.OutRoot = _dir; spec.RunDir = Path.Combine(_dir, tag);
            return spec;
        }

        [Test]
        public void D2_Reconciles()
        {
            GodSim sim = null;
            var full = Demo(RecordingLevel.Full, "full");
            full.BeforeStep = x => sim = x;
            var off = Demo(RecordingLevel.Off, "off");
            var a = RunEngine.Run(full); var b = RunEngine.Run(off);
            Assert.IsNull(a.Error, a.Error); Assert.IsNull(b.Error, b.Error);
            CollectionAssert.AreEqual(TestUtil.Seq(b), TestUtil.Seq(a), "recording Off vs Full changes no hash");
            using (var d = WarehouseDb.OpenRead(a.WarehousePath))
            {
                Assert.GreaterOrEqual(d.SchemaVersion(), 9);
                long Ev(int code) => Convert.ToInt64(d.Scalar("SELECT COALESCE(SUM(events), 0) FROM fact_event_day WHERE event_code = " + code));
                long Rows(string sql) => Convert.ToInt64(d.Scalar(sql));
                Assert.AreEqual(4L, Ev(810) + Ev(811), "the four scheduled instructions were accepted");
                Assert.AreEqual(Ev(810) + Ev(811), Rows("SELECT COUNT(*) FROM fact_divine_command"));
                Assert.AreEqual(Ev(813), Rows("SELECT COUNT(*) FROM fact_command_perception WHERE kind_code = 1"), "perceptions = 813 count");
                Assert.AreEqual(Ev(819), Rows("SELECT COUNT(*) FROM fact_command_perception WHERE kind_code = 2"));
                Assert.AreEqual(Ev(814), Rows("SELECT COUNT(*) FROM fact_command_response"), "responses = 814 count");
                Assert.AreEqual(Ev(813), Ev(814), "one response per perceiver");
                Assert.AreEqual(0L, Rows("SELECT COUNT(*) FROM (SELECT intervention_id, person_key FROM fact_command_response GROUP BY 1, 2 HAVING COUNT(*) > 1)"));
                Assert.AreEqual(2L, Ev(815));
                Assert.AreEqual(Ev(815), Rows("SELECT COUNT(*) FROM fact_compulsion WHERE kind_code = 1"));
                Assert.AreEqual(Ev(816), Rows("SELECT COUNT(*) FROM fact_compulsion WHERE kind_code = 2"));
                Assert.AreEqual(Ev(817), Rows("SELECT COUNT(*) FROM fact_compulsion WHERE kind_code = 3"));
                Assert.AreEqual(0L, Rows("SELECT COUNT(*) FROM (SELECT intervention_id FROM fact_compulsion GROUP BY 1 HAVING SUM(kind_code = 1) <> 1 OR SUM(kind_code = 2) <> 1)"),
                    "every compulsion spell opens and closes exactly once");
            }
            var bad = new System.Collections.Generic.List<string>();
            DivinityModule.Check(sim.State, bad); PopulationInvariants.Check(sim.State, bad);
            CollectionAssert.IsEmpty(bad);
            Assert.IsTrue(sim.State.Divinity.Messages == null || sim.State.Divinity.Messages.Items.Count == 0, "every instruction closed by day 6");
        }

        [Test]
        public void Replay_D2()
        {
            var spec = Demo(RecordingLevel.Off, "replay");
            spec.WriteReplay = true;
            var r = RunEngine.Run(spec);
            Assert.IsNull(r.Error, r.Error);
            var again = RunEngine.Run(Cli.ReplaySpec(Path.Combine(r.RunDir, "replay.godr"), default));
            Assert.IsNull(again.Divergence, again.Divergence?.ToString());
            Assert.AreEqual(r.FinalHash, again.FinalHash);
        }

        [Test]
        public void Verify_SaveMid_d2_command()
        {
            int code = TestUtil.Cli(out var o, out var e, "verify", "--scenario", "d2_command", "--seed", "1", "--days", "6", "--save-mid");
            Assert.AreEqual(ExitCodes.Ok, code, o + e);
        }
    }
}
