using System;
using System.IO;
using System.Linq;
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
    /// <summary>D3 runner tests (D3 spec 14, 15): the d3_miracles demonstration, its warehouse reconciliation (D3_Reconciles), recording neutrality, replay and save/load parity (D3-T15, T16). OWNER: D3.</summary>
    [TestFixture]
    public class D3RunnerTests
    {
        private string _dir;
        [SetUp] public void SetUp() { _dir = TestUtil.TempDir("d3"); }
        [TearDown] public void TearDown() { SqliteConnection.ClearAllPools(); TestUtil.Cleanup(_dir); }

        private RunSpec Demo(RecordingLevel level, string tag)
        {
            var spec = TestUtil.Spec("d3_miracles", 1, 7200);
            spec.Scenario.Checks.Remove("smoke");
            spec.CheckpointEvery = 1200;
            spec.Level = level;
            // dim_god is D1's mapper (not on this branch), so foreign keys are not enforced here
            if (level != RecordingLevel.Off) { spec.Sqlite = new WarehouseOptions { ForeignKeys = false, Async = false }; spec.SkipJsonl = true; }
            spec.OutRoot = _dir; spec.RunDir = Path.Combine(_dir, tag);
            return spec;
        }

        [Test]
        public void D3_Reconciles()
        {
            GodSim sim = null;
            var full = Demo(RecordingLevel.Full, "full");
            full.BeforeStep = x => sim = x;
            var off = Demo(RecordingLevel.Off, "off");
            var a = RunEngine.Run(full); var b = RunEngine.Run(off);
            Assert.IsNull(a.Error, a.Error); Assert.IsNull(b.Error, b.Error);
            CollectionAssert.AreEqual(TestUtil.Seq(b), TestUtil.Seq(a), "recording Off vs Full changes no hash");
            var m = sim.State.Divinity.Miracles;
            Assert.IsNotNull(m);
            Assert.AreEqual(7, m.Records.Count, "all seven scheduled miracles were accepted");
            var led = sim.State.Population.Ledger;
            Assert.AreEqual(3, led.ProducedBy(ProduceReason.Miracle, Ware.Fish) + 0 * 1, "3 fish created");
            using (var d = WarehouseDb.OpenRead(a.WarehousePath))
            {
                Assert.GreaterOrEqual(d.SchemaVersion(), 12);
                long Ev(int code) => Convert.ToInt64(d.Scalar("SELECT COALESCE(SUM(events), 0) FROM fact_event_day WHERE event_code = " + code));
                long Rows(string sql) => Convert.ToInt64(d.Scalar(sql));
                Assert.AreEqual(7L, Ev(825)); Assert.AreEqual(Ev(825), Rows("SELECT COUNT(*) FROM fact_miracle"));
                Assert.AreEqual(Ev(826), Rows("SELECT COUNT(*) FROM fact_miracle_effect"));
                Assert.AreEqual(Ev(827) + Ev(828), Rows("SELECT COUNT(*) FROM fact_resource_ledger"));
                Assert.AreEqual(3L, Rows("SELECT COALESCE(SUM(quantity), 0) FROM fact_resource_ledger WHERE direction = 1 AND ware_key = " + (int)Ware.Fish));
                foreach (Ware w in new[] { Ware.Fish, Ware.Berries, Ware.Roots, Ware.Log })
                {
                    Assert.AreEqual(led.ProducedBy(ProduceReason.Miracle, w), Rows("SELECT COALESCE(SUM(quantity), 0) FROM fact_resource_ledger WHERE direction = 1 AND ware_key = " + (int)w), "created " + w);
                    Assert.AreEqual(led.ConsumedBy(ConsumeReason.Miracle, w), Rows("SELECT COALESCE(SUM(quantity), 0) FROM fact_resource_ledger WHERE direction = -1 AND ware_key = " + (int)w), "destroyed " + w);
                }
                Assert.Greater(Rows("SELECT COUNT(*) FROM fact_miracle_effect WHERE before_value <> after_value"), 6L);
                Assert.AreEqual(Rows("SELECT COUNT(*) FROM fact_miracle_effect"), Rows("SELECT COUNT(*) FROM fact_miracle_effect WHERE before_value <> after_value"), "every effect changes something");
            }
            var bad = new System.Collections.Generic.List<string>();
            DivinityModule.Check(sim.State, bad); PopulationInvariants.Check(sim.State, bad);
            CollectionAssert.IsEmpty(bad);
        }

        [Test]
        public void Replay_Miracles()
        {
            var spec = Demo(RecordingLevel.Off, "replay");
            spec.WriteReplay = true;
            var r = RunEngine.Run(spec);
            Assert.IsNull(r.Error, r.Error);
            string file = Path.Combine(r.RunDir, "replay.godr");
            var again = RunEngine.Run(Cli.ReplaySpec(file, default));
            Assert.IsNull(again.Divergence, again.Divergence?.ToString());
            Assert.AreEqual(r.FinalHash, again.FinalHash);
        }

        [Test]
        public void Verify_SaveMid_d3_miracles()
        {
            int code = TestUtil.Cli(out var o, out var e, "verify", "--scenario", "d3_miracles", "--seed", "1", "--days", "6", "--save-mid");
            Assert.AreEqual(ExitCodes.Ok, code, o + e);
        }
    }
}
