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
    /// <summary>
    /// D4 runner tests (D4 spec 12-15): the d4_embodiment demonstration, its warehouse reconciliation (D4-T15 <c>D4_Reconciles</c>:
    /// spells open and close exactly at 845/846 and 850/851/852, rows carry control_code 2/3), recording neutrality, replay of
    /// the input stream and save/load in the middle of it. OWNER: D4.
    /// </summary>
    [TestFixture]
    public class D4RunnerTests
    {
        private string _dir;
        [SetUp] public void SetUp() { _dir = TestUtil.TempDir("d4"); }
        [TearDown] public void TearDown() { SqliteConnection.ClearAllPools(); TestUtil.Cleanup(_dir); }

        private RunSpec Demo(RecordingLevel level, string tag)
        {
            var spec = TestUtil.Spec("d4_embodiment", 1, 3600);
            spec.Scenario.Checks.Remove("smoke");
            spec.CheckpointEvery = 600;
            spec.Level = level;
            if (level != RecordingLevel.Off) { spec.Sqlite = new WarehouseOptions { Async = false }; spec.SkipJsonl = true; }
            spec.OutRoot = _dir; spec.RunDir = Path.Combine(_dir, tag);
            return spec;
        }

        [Test]
        public void D4_Reconciles()
        {
            GodSim sim = null;
            var full = Demo(RecordingLevel.Full, "full");
            full.BeforeStep = x => sim = x;
            var off = Demo(RecordingLevel.Off, "off");
            var a = RunEngine.Run(full); var b = RunEngine.Run(off);
            Assert.IsNull(a.Error, a.Error); Assert.IsNull(b.Error, b.Error);
            CollectionAssert.AreEqual(TestUtil.Seq(b), TestUtil.Seq(a), "recording Off vs Full changes no hash");
            var reg = sim.State.Divinity.Avatars;
            using (var d = WarehouseDb.OpenRead(a.WarehousePath))
            {
                Assert.GreaterOrEqual(d.SchemaVersion(), 11);
                long Ev(int code) => Convert.ToInt64(d.Scalar("SELECT COALESCE(SUM(events), 0) FROM fact_event_day WHERE event_code = " + code));
                long Rows(string sql) => Convert.ToInt64(d.Scalar(sql));
                // possession (845-847)
                Assert.AreEqual(1L, Ev(845), "the scheduled possession began");
                Assert.AreEqual(Ev(845), Rows("SELECT COUNT(*) FROM fact_possession WHERE kind_code = 1"));
                Assert.AreEqual(Ev(846), Rows("SELECT COUNT(*) FROM fact_possession WHERE kind_code = 2"));
                Assert.AreEqual(Ev(847), Rows("SELECT COUNT(*) FROM fact_possession WHERE kind_code = 3"));
                Assert.Greater(Ev(847), 10L, "the god's inputs started acts in the body");
                Assert.AreEqual(Ev(845) - Ev(846), (long)reg.Possessions.Count, "open spells = started - ended");
                Assert.AreEqual(0L, Rows("SELECT COUNT(*) FROM (SELECT intervention_id FROM fact_possession GROUP BY 1 HAVING SUM(kind_code = 1) <> 1 OR SUM(kind_code = 2) > 1)"),
                    "every possession opens once and ends at most once");
                Assert.AreEqual(0L, Rows("SELECT COUNT(*) FROM fact_possession WHERE control_code <> 2"), "possessed rows carry control_code 2");
                // avatar (850-854)
                Assert.AreEqual(Ev(850), Rows("SELECT COUNT(*) FROM fact_avatar_manifestation WHERE kind_code = 1"));
                Assert.AreEqual(Ev(851), Rows("SELECT COUNT(*) FROM fact_avatar_manifestation WHERE kind_code = 2"));
                Assert.AreEqual(Ev(852), Rows("SELECT COUNT(*) FROM fact_avatar_manifestation WHERE kind_code = 3"));
                Assert.AreEqual(Ev(853), Rows("SELECT COUNT(*) FROM fact_avatar_manifestation WHERE kind_code = 4"));
                Assert.AreEqual(Ev(854), Rows("SELECT COUNT(*) FROM fact_avatar_manifestation WHERE kind_code = 5"));
                Assert.AreEqual(Ev(850) - Ev(851) - Ev(852), (long)reg.Manifestations.Count, "open manifestations = manifested - dismissed - destroyed");
                Assert.AreEqual(0L, Rows("SELECT COUNT(*) FROM fact_avatar_manifestation WHERE control_code <> 3"), "avatar rows carry control_code 3");
                Assert.AreEqual(0L, Rows("SELECT COUNT(*) FROM dim_person WHERE npc_id >= " + DivinityIds.AvatarBodyBase), "the avatar never has a dim_person row");
                TestContext.Progress.WriteLine("D4 demo: 845 " + Ev(845) + ", 846 " + Ev(846) + ", 847 " + Ev(847) + ", 850 " + Ev(850) + ", 851 " + Ev(851) + ", 852 "
                    + Ev(852) + ", 854 " + Ev(854));
            }
            var bad = new System.Collections.Generic.List<string>();
            DivinityModule.Check(sim.State, bad); PopulationInvariants.Check(sim.State, bad);
            CollectionAssert.IsEmpty(bad);
        }

        [Test]
        public void Replay_D4()
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
        public void Verify_SaveMid_d4_embodiment()
        {
            int code = TestUtil.Cli(out var o, out var e, "verify", "--scenario", "d4_embodiment", "--seed", "1", "--days", "3", "--save-mid");
            Assert.AreEqual(ExitCodes.Ok, code, o + e);
        }
    }
}
