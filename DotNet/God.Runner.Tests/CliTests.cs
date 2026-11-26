using System;
using System.IO;
using System.Linq;
using God.Recording;
using God.Sim;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace God.Runner.Tests
{
    /// <summary>M0.9: every exit code from a constructed case; strict scenario parsing.</summary>
    public class CliTests
    {
        private static string WriteScenario(string dir, string json)
        {
            string p = Path.Combine(dir, "s.scenario.json");
            File.WriteAllText(p, json);
            return p;
        }

        [Test]
        public void ExitCode2_ForBadFlags_AndUnknownCommands()
        {
            Assert.AreEqual(ExitCodes.Usage, TestUtil.Cli(out _, out var e1, "run", "--scenario", "watch6", "--bogus", "1"));
            StringAssert.Contains("--bogus", e1);
            Assert.AreEqual(ExitCodes.Usage, TestUtil.Cli(out _, out _, "frobnicate"));
            Assert.AreEqual(ExitCodes.Usage, TestUtil.Cli(out _, out _, "run", "--scenario", "watch6", "--ticks", "abc"));
            Assert.AreEqual(ExitCodes.Usage, TestUtil.Cli(out _, out _, "batch", "--scenario", "watch6", "--seeds", "9-3"));
            Assert.AreEqual(ExitCodes.Usage, TestUtil.Cli(out _, out _, "verify", "a.godr", "b.godr"), "one replay file at a time");
            Assert.AreEqual(ExitCodes.Usage, TestUtil.Cli(out _, out _));
            Assert.AreEqual(ExitCodes.Ok, TestUtil.Cli(out var help, out _, "--help"));
            StringAssert.Contains("exit codes", help);
        }

        [TestCase("{\"scenarioVersion\":1,\"name\":\"x\",\"setpu\":{}}", "setpu")]
        [TestCase("{\"scenarioVersion\":1,\"name\":\"x\",\"setup\":{\"factionCont\":4}}", "factionCont")]
        [TestCase("{\"scenarioVersion\":1,\"name\":\"x\",\"tuning\":{\"Military.CritPc\":3}}", "CritPc")]
        [TestCase("{\"scenarioVersion\":1,\"name\":\"x\",\"tuning\":{\"Military.Seed\":3}}", "seed")]
        [TestCase("{\"scenarioVersion\":2,\"name\":\"x\"}", "scenarioVersion")]
        [TestCase("{\"scenarioVersion\":1,\"name\":\"x\",\"setup\":{\"victory\":[{\"kind\":\"conquest\"}]}}", "conquest")]
        [TestCase("{\"scenarioVersion\":1,\"name\":\"x\",\"setup\":{\"victory\":[]}}", "non-empty")]
        [TestCase("{\"scenarioVersion\":1,\"name\":\"x\",\"setup\":{\"population\":true}}", "M3")]
        [TestCase("{\"scenarioVersion\":1,\"name\":\"x\",\"policies\":{\"civilian\":{\"default\":\"utility.v1\"}}}", "M3")]
        [TestCase("{\"scenarioVersion\":1,\"name\":\"x\",\"metrics\":[\"gini_wealth\"]}", "gini_wealth")]
        [TestCase("{\"scenarioVersion\":1,\"name\":\"x\",\"record\":{\"level\":\"experiment\"}}", "M13")]
        [TestCase("{\"scenarioVersion\":1,\"name\":\"x\",\"commands\":[{\"tick\":5,\"type\":\"DeclareWar\",\"args\":{\"Agianst\":2}}]}", "Agianst")]
        [TestCase("{\"scenarioVersion\":1,\"name\":\"x\",\"commands\":[{\"tick\":5,\"type\":\"Teleport\"}]}", "Teleport")]
        [TestCase("{\"scenarioVersion\":1,\"name\":\"x\",\"stop\":{\"maxYears\":1,\"untill\":3}}", "untill")]
        [TestCase("not json", "JSON")]
        public void ExitCode3_ForInvalidScenarios(string json, string mentioned)
        {
            string dir = TestUtil.TempDir("strict");
            try
            {
                int code = TestUtil.Cli(out _, out var err, "run", "--scenario", WriteScenario(dir, json), "--seed", "1", "--no-out");
                Assert.AreEqual(ExitCodes.InvalidInput, code, err);
                StringAssert.Contains(mentioned, err);
            }
            finally { TestUtil.Cleanup(dir); }
        }

        [Test]
        public void ExitCode3_ForSeedOverrides_AndUnknownTuningPaths()
        {
            Assert.AreEqual(ExitCodes.InvalidInput, TestUtil.Cli(out _, out var e1, "run", "--scenario", "watch6", "--set", "setup.seed=4", "--no-out"));
            StringAssert.Contains("seed", e1);
            Assert.AreEqual(ExitCodes.InvalidInput, TestUtil.Cli(out _, out var e2, "run", "--scenario", "watch6", "--set", "tuning.Military.Nope=4", "--no-out"));
            StringAssert.Contains("Military.Nope", e2);
            Assert.AreEqual(ExitCodes.InvalidInput, TestUtil.Cli(out _, out _, "run", "--scenario", "no_such_scenario", "--no-out"));
            Assert.AreEqual(ExitCodes.InvalidInput, TestUtil.Cli(out _, out _, "verify", Path.Combine(TestUtil.TempDir("x"), "missing.godr")));
        }

        [Test]
        public void ResolvedScenario_RoundTrips_AndOverridesApply()
        {
            var sc = TestUtil.Load("watch6", "tuning.Military.CritPct=12", "setup.lifeScale=50", "stop.maxYears=2");
            Assert.AreEqual(12, sc.Tuning["Military.CritPct"]); Assert.AreEqual(50, sc.LifeScale); Assert.AreEqual(2, sc.MaxYears);
            Assert.AreEqual(12, sc.ToTuning().Military.CritPct);
            var again = Scenario.FromJson(sc.ToJson());
            Assert.AreEqual(sc.ToJson().ToString(), again.ToJson().ToString());
            Assert.AreEqual(sc.Hash(), again.Hash());
            Assert.AreNotEqual(sc.Hash(), TestUtil.Load("watch6").Hash());
            var setup = sc.ToSetup(9);
            Assert.AreEqual(9UL, setup.Seed); Assert.AreEqual(GameSetup.NoHuman, setup.Human); Assert.AreEqual(VictoryKind.VictoryPoints, setup.Victory[0].Kind);
        }

        [Test]
        public void ExitCode4_ForADivergence()
        {
            // a replay file whose checkpoint hash was changed by hand
            string dir = TestUtil.TempDir("div");
            try
            {
                var spec = TestUtil.Spec("watch6", 2, 3000); spec.OutRoot = dir; spec.WriteReplay = true;
                var r = RunEngine.Run(spec);
                string file = Path.Combine(r.RunDir, "replay.godr");
                var rf = ReplayFile.Read(file);
                var lines = new System.Collections.Generic.List<string> { rf.Header.ToJson().ToString(Newtonsoft.Json.Formatting.None) };
                using (var w = new ReplayWriter(Path.Combine(dir, "bad.godr")))
                {
                    w.Header(rf.Header);
                    foreach (var e in rf.Entries)
                    {
                        if (e.Kind == ReplayEntryKind.Command) w.Command(e.Tick, e.ToCommand(), e.Origin);
                        else if (e.Kind == ReplayEntryKind.Checkpoint) { var p = (ulong[])e.Parts.Clone(); if (e.Tick == 2400) p[2] ^= 1; w.Checkpoint(e.Tick, e.Tick == 2400 ? GodSim.Fold(p) : e.Hash, p); }
                        else if (e.Kind == ReplayEntryKind.End) w.End(e.Tick, e.Hash, e.Reason);
                    }
                }
                Assert.AreEqual(ExitCodes.Divergence, TestUtil.Cli(out var o, out var err, "verify", Path.Combine(dir, "bad.godr")), o + err);
                StringAssert.Contains("first bad checkpoint at tick 2400", o);
                StringAssert.Contains("military", o);
            }
            finally { TestUtil.Cleanup(dir); }
        }

        [Test]
        public void ExitCode5_ForABrokenInvariant()
        {
            var spec = TestUtil.Spec("watch6", 1, 1200);
            spec.TestSystems.Add(() => new BreakInvariantSystem(400));
            var r = RunEngine.Run(spec);
            Assert.AreEqual(ExitCodes.InvariantFailed, r.ExitCode, string.Join("; ", r.Messages));
            Assert.IsTrue(r.Messages.Any(m => m.Contains("negative count")));
        }

        [Test]
        public void ExitCode6_ForAnExceptionInTheSim()
        {
            var spec = TestUtil.Spec("watch6", 1, 1200);
            spec.TestSystems.Add(() => new ThrowSystem(77));
            var r = RunEngine.Run(spec);
            Assert.AreEqual(ExitCodes.SimException, r.ExitCode);
            Assert.AreEqual("exception", r.StopReason);
            StringAssert.Contains("test fault at tick 77", r.Error);
        }

        [Test]
        public void ExitCode7_ForAMissedBenchmark()
        {
            string dir = TestUtil.TempDir("bench7");
            try
            {
                string baseline = Path.Combine(dir, "baseline.json");
                File.WriteAllText(baseline, "{\"ticksPerSecond\": 1e12}");
                int code = TestUtil.Cli(out var o, out var e, "bench", "--ticks", "300", "--parallel-ticks", "100", "--workers", "1", "--compare", baseline, "--strict");
                Assert.AreEqual(ExitCodes.PerformanceMissed, code, o + e);
                StringAssert.Contains("below the baseline", o);
                Assert.AreEqual(ExitCodes.Ok, TestUtil.Cli(out o, out e, "bench", "--ticks", "300", "--parallel-ticks", "100", "--workers", "1", "--compare", baseline), "off the reference machine it only warns");
                StringAssert.Contains("WARNING", o);
            }
            finally { TestUtil.Cleanup(dir); }
        }

        [Test]
        public void ExitCode8_ForAnOutputDirectoryThatCannotBeWritten()
        {
            string dir = TestUtil.TempDir("io");
            try
            {
                string notADir = Path.Combine(dir, "file");
                File.WriteAllText(notADir, "x");
                int code = TestUtil.Cli(out var o, out var e, "run", "--scenario", "watch6", "--seed", "1", "--ticks", "600", "--out", notADir, "--quiet");
                Assert.AreEqual(ExitCodes.IoError, code, o + e);
            }
            finally { TestUtil.Cleanup(dir); }
        }

        [TestCase("lab", "M2")]
        [TestCase("experiment", "M14")]
        [TestCase("serve", "M15")]
        [TestCase("dataset", "M15")]
        [TestCase("verify-model", "M15")]
        [TestCase("learn-dump", "M3")]
        [TestCase("export", "M13")]
        public void LaterCommands_SayWhenTheyArrive(string cmd, string milestone)
        {
            Assert.AreEqual(ExitCodes.NotImplemented, TestUtil.Cli(out _, out var e, cmd, "all", "--check"));
            StringAssert.Contains("not implemented until " + milestone, e);
        }

        [Test]
        public void ExitCode1_ForAFailCondition()
        {
            var spec = TestUtil.Spec("watch6", 1, 3600);
            spec.Scenario.Fail.Add(new MetricCondition { Metric = "buildings_total", Above = 3 });
            var r = RunEngine.Run(spec);
            Assert.AreEqual(ExitCodes.CheckFailed, r.ExitCode);
            Assert.AreEqual("fail:buildings_total", r.StopReason);
        }

        [Test]
        public void RunJson_AndTheRunId_AreReproducible()
        {
            string dir = TestUtil.TempDir("runid");
            try
            {
                Assert.AreEqual(ExitCodes.Ok, TestUtil.Cli(out var o1, out _, "run", "--scenario", "sandbox_watch6", "--seed", "4", "--ticks", "1200", "--out", dir, "--json", "--record", "metrics"));
                Assert.AreEqual(ExitCodes.Ok, TestUtil.Cli(out var o2, out _, "run", "--scenario", "sandbox_watch6", "--seed", "4", "--ticks", "1200", "--out", dir, "--json", "--record", "metrics"));
                var a = JObject.Parse(o1.Trim()); var b = JObject.Parse(o2.Trim());
                Assert.AreEqual((string)a["runId"], (string)b["runId"]);
                Assert.AreEqual((string)a["finalHash"], (string)b["finalHash"]);
                string runDir = (string)a["runDir"];
                foreach (var f in new[] { "run.json", "summary.json", "scenario.resolved.json", "replay.godr", "metrics.jsonl" })
                    Assert.IsTrue(File.Exists(Path.Combine(runDir, f)), f);
                Assert.IsFalse(File.ReadAllText(Path.Combine(runDir, "summary.json")).Contains("wall"), "no wall clock in summary.json");
                StringAssert.Contains("wallSeconds", File.ReadAllText(Path.Combine(runDir, "run.json")));
            }
            finally { TestUtil.Cleanup(dir); }
        }
    }
}
