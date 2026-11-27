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
    /// <summary>Package D6 runner tests (D6 spec 13-15): the d6_commandment demonstration, its warehouse reconciliation (D6-T15), recording neutrality, replay and save/load (D6-T11). OWNER: D6.</summary>
    [TestFixture]
    public class D6RunnerTests
    {
        private string _dir;
        [SetUp] public void SetUp() { _dir = TestUtil.TempDir("d6"); }
        [TearDown] public void TearDown() { SqliteConnection.ClearAllPools(); TestUtil.Cleanup(_dir); }

        private RunSpec Demo(RecordingLevel level, string tag, int days = 10)
        {
            var spec = TestUtil.Spec("d6_commandment", 1, days * 1200);
            spec.Scenario.Checks.Remove("smoke");
            spec.CheckpointEvery = 1200;
            spec.Level = level;
            if (level != RecordingLevel.Off) { spec.Sqlite = new WarehouseOptions { Async = false }; spec.SkipJsonl = true; }
            spec.OutRoot = _dir; spec.RunDir = Path.Combine(_dir, tag);
            return spec;
        }

        [Test]
        public void D6_Reconciles()
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
                Assert.GreaterOrEqual(d.SchemaVersion(), 14);
                long Ev(int code) => Convert.ToInt64(d.Scalar("SELECT COALESCE(SUM(events), 0) FROM fact_event_day WHERE event_code = " + code));
                long Rows(string sql) => Convert.ToInt64(d.Scalar(sql));
                Assert.AreEqual(1L, Ev(860), "one revelation");
                Assert.AreEqual(1L, Ev(861), "one revision");
                Assert.AreEqual(1L, Ev(862), "one revocation");
                Assert.AreEqual(Ev(860), Rows("SELECT COUNT(*) FROM fact_commandment"));
                Assert.AreEqual(Ev(861), Rows("SELECT COUNT(*) FROM fact_commandment_revision WHERE kind_code = 1"));
                Assert.AreEqual(Ev(862), Rows("SELECT COUNT(*) FROM fact_commandment_revision WHERE kind_code = 2"));
                Assert.AreEqual(Ev(863), Rows("SELECT COUNT(*) FROM fact_commandment_knowledge WHERE kind_code = 1"), "learned rows = 863");
                Assert.AreEqual(Ev(864), Rows("SELECT COUNT(*) FROM fact_commandment_knowledge WHERE kind_code = 2"), "stance rows = 864");
                Assert.AreEqual(Ev(865), Rows("SELECT COUNT(*) FROM fact_commandment_knowledge WHERE kind_code = 3"), "forgotten rows = 865");
                Assert.AreEqual(Ev(867), Rows("SELECT COUNT(*) FROM fact_religion"));
                Assert.AreEqual(Ev(868), Rows("SELECT COUNT(*) FROM fact_affiliation"));
                Assert.Greater(Ev(863), 8L, "people learned it");
                // knowledge spells: each person's first row is a learning (no stance or forgetting before it)
                Assert.AreEqual(0L, Rows("SELECT COUNT(*) FROM (SELECT person_key, commandment_id, MIN(tick * 100000 + seq) AS f FROM fact_commandment_knowledge GROUP BY 1, 2) x " +
                    "JOIN fact_commandment_knowledge k ON k.person_key = x.person_key AND k.commandment_id = x.commandment_id AND k.tick * 100000 + k.seq = x.f WHERE k.kind_code <> 1"));
                // affiliation spells: never more closes than opens per person and religion
                Assert.AreEqual(0L, Rows("SELECT COUNT(*) FROM (SELECT person_key, religion_id FROM fact_affiliation GROUP BY 1, 2 HAVING SUM(opened = 0) > SUM(opened = 1))"));
                Assert.AreEqual(0L, Rows("SELECT COUNT(*) FROM fact_affiliation WHERE community_code <> citizenship_code"), "citizenship stand-in follows the community, never the religion");
            }
            var bad = new System.Collections.Generic.List<string>();
            DivinityModule.Check(sim.State, bad); PopulationInvariants.Check(sim.State, bad);
            CollectionAssert.IsEmpty(bad);
        }

        [Test]
        public void Replay_Doctrine()
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
        public void Verify_SaveMid_d6_commandment()
        {
            int code = TestUtil.Cli(out var o, out var e, "verify", "--scenario", "d6_commandment", "--seed", "1", "--days", "10", "--save-mid");
            Assert.AreEqual(ExitCodes.Ok, code, o + e);
        }
    }
}
