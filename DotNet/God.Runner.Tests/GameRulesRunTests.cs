using System;
using System.IO;
using System.Linq;
using God.Recording;
using God.Sim;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace God.Runner.Tests
{
    /// <summary>M0.13 (victory conditions in long runs), the calendar in a real run, and the M0.15 benchmark.</summary>
    public class GameRulesRunTests
    {
        /// <summary>
        /// The M0 baseline of the stage 3 game (watch6, VictoryPoints10 then last standing): end tick and winner for seeds
        /// 1-12, recorded on the first M0 run. G6.5: every later milestone replays these. Before M0 (shared dice stream,
        /// no pending commands in the hash) the results were different; see the M0 report.
        /// </summary>
        private static readonly (ulong seed, long endTick, string winner)[] M0Baseline =
        {
            (1, 17534, "Violet"), (2, 20161, "Crimson"), (3, 26560, "Ivory"), (4, 24444, "Crimson"),
            (5, 15968, "Amber"), (6, 20509, "Azure"), (7, 20627, "Ivory"), (8, 20661, "Amber"),
            (9, 17020, "Ivory"), (10, 30346, "Violet"), (11, 20616, "Ivory"), (12, 21970, "Jade"),
        };

        [Test]
        public void VictoryPoints10_EndsTheStage3Game_AsTheM0Baseline_Seeds1To12()
        {
            var runs = TestUtil.RunAll(M0Baseline.Select(b => TestUtil.Spec("watch6", b.seed, 40000)).ToList());
            for (int i = 0; i < runs.Length; i++)
            {
                var (seed, end, winner) = M0Baseline[i];
                var s = runs[i].Sim.State;
                Assert.IsTrue(s.IsGameOver, "seed " + seed);
                Assert.AreEqual(GameEndReason.VictoryPoints, s.Factions.EndReason, "seed " + seed);
                Assert.AreEqual(end, s.Factions.GameOverTick, "seed " + seed + " end tick");
                Assert.AreEqual(winner, FactionInfo.Name((Faction)s.Factions.Winner), "seed " + seed + " winner");
                Assert.GreaterOrEqual(FactionRules.VictoryPoints(s, (Faction)s.Factions.Winner), 10);
                Assert.AreEqual(true, runs[i].SmokeOk, runs[i].SmokeLine);
            }
        }

        [Test]
        public void Sandbox_TenSimYears_FiveSeeds_NeverEndsWhileTwoFactionsRemain()
        {
            // M0.13 / STATE S-VIC-1
            var runs = TestUtil.RunAll(Enumerable.Range(1, 5).Select(seed =>
            {
                var sp = TestUtil.Spec("sandbox_watch6", (ulong)seed, 0);
                sp.Scenario.MaxYears = 10;
                return sp;
            }).ToList());
            foreach (var r in runs)
            {
                Assert.AreEqual(120000, r.Ticks, "ran the full 10 sim years");
                Assert.AreEqual("maxTicks", r.StopReason);
                Assert.IsFalse(r.Sim.State.IsGameOver);
                Assert.AreEqual(0, r.Counters.TryGetValue("events.GameOver", out long g) ? g : 0);
                Assert.AreEqual(0, r.ExitCode, string.Join("; ", r.Messages) + r.Error);
                TestContext.WriteLine("seed " + r.Seed + ": " + r.LastStandings);
            }
        }

        [Test]
        public void TheCalendar_DrivesDayEnds_AndLifeScaleOnlyScalesAges()
        {
            string dir = TestUtil.TempDir("calendar");
            try
            {
                var spec = TestUtil.Spec("sandbox_watch6", 3, 0, "setup.ticksPerDay=600", "setup.daysPerYear=5", "setup.lifeScale=200", "stop.maxYears=2");
                spec.OutRoot = dir; spec.Level = RecordingLevel.Metrics;
                var r = RunEngine.Run(spec);
                Assert.AreEqual(6000, r.Ticks, "2 years x 5 days x 600 ticks");
                Assert.AreEqual(10, r.Checkpoints.Count, "a checkpoint every sim day by default");
                var lines = File.ReadAllLines(Path.Combine(r.RunDir, "metrics.jsonl"));
                Assert.AreEqual(10, lines.Length, "one metrics line per sim day");
                Assert.AreEqual(9, (long)JObject.Parse(lines[9])["day"]);
                var cal = SimCalendar.Of(r.Sim.State);
                Assert.AreEqual(200, cal.LifeScale);
                Assert.AreEqual(LifeStage.Adult, cal.StageAt(0, 1500), "1 sim year = half of a 3000-tick calendar year at LifeScale 200");
                Assert.AreEqual(LifeStage.Child, cal.StageAt(0, 1499));
                var plain = SimCalendar.Of(new GameSetup(3UL) { TicksPerDay = 600, DaysPerYear = 5 });
                Assert.AreEqual(LifeStage.Adult, plain.StageAt(0, 3000)); Assert.AreEqual(LifeStage.Child, plain.StageAt(0, 2999));
            }
            finally { TestUtil.Cleanup(dir); }
        }

        [Test]
        public void Bench_Stage3_Baseline()
        {
            // M0.15: bench_stage3_watch6 at 2,000 ticks/s or more. A hard gate only on the reference machine; elsewhere a warning.
            var o = new Bench.Options { Scenario = TestUtil.Load("bench_stage3_watch6"), Ticks = 8000, ParallelTicks = 2000, Workers = new[] { 1, 2 } };
            var res = Bench.Run(o);
            double tps = (double)res["ticksPerSecond"];
            TestContext.WriteLine(res.ToString());
            Assert.IsTrue((bool)res["loadSameHash"], "a save and load in the bench keeps the hash");
            Assert.IsTrue(File.Exists(Path.Combine(AppContext.BaseDirectory, "Bench", "baseline.json")), "the baseline is stored with the runner");
            if (Environment.GetEnvironmentVariable("GOD_REFERENCE_MACHINE") == "1") Assert.GreaterOrEqual(tps, Bench.MinimumTicksPerSecond);
            else if (tps < Bench.MinimumTicksPerSecond) Assert.Warn("bench_stage3_watch6 at " + tps.ToString("0") + " ticks/s, below 2,000 (not the reference machine)");
        }
    }
}
