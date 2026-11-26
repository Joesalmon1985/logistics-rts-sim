using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using God.Recording;
using God.Sim;
using God.Sim.Lab;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace God.Runner.Tests
{
    /// <summary>
    /// M2 lab tests run through the runner (ACCEPTANCE M2.2 to M2.15, M2.17, M2.18, M2.20; STRATEGY_BENCHMARKS section 10
    /// tests 8, 11, 15 to 17, 24). One serial <c>lab all --check</c> over seeds 1-5 is shared by the experiment tests.
    /// </summary>
    [Parallelizable(ParallelScope.None)]
    public class LabRunnerTests
    {
        private static readonly object Gate = new object();
        private static List<LabResult> _serial;
        private static double _serialSeconds;

        /// <summary>Every experiment for seeds 1-5, serially on one thread (the lab all --check workload).</summary>
        private static List<LabResult> Serial()
        {
            lock (Gate)
            {
                if (_serial != null) return _serial;
                var sw = Stopwatch.StartNew();
                var l = new List<LabResult>();
                foreach (var e in LabRegistry.All())
                    for (ulong s = 1; s <= 5; s++) l.Add(e.Run(new LabConfig { Experiment = e.Id, Seed = s }));
                _serialSeconds = sw.Elapsed.TotalSeconds;
                _serial = l;
                return l;
            }
        }

        private static IEnumerable<LabResult> Of(string id) => Serial().Where(r => r.Experiment == id);

        private static void AssertPasses(string id)
        {
            var runs = Of(id).ToList();
            Assert.AreEqual(5, runs.Count);
            var fails = runs.SelectMany(r => r.Checks.Where(c => c.Fails).Select(c => "seed " + r.Seed + ": " + c.Name + " = " + c.Measured + " (expected " + c.Expected + ")")).ToList();
            Assert.IsEmpty(fails, id + " failed checks:\n" + string.Join("\n", fails));
            Assert.IsTrue(runs.All(r => r.Checks.Length > 0));
        }

        // ---- M2.2 to M2.13 (test 15): each experiment passes its checks for seeds 1 to 5 ----

        [Test] public void L1_HawkDove_M2_3() => AssertPasses("L1");
        [Test] public void L2_RetaliatorBullyProber_AndL2bMatrices_M2_2_M2_4() => AssertPasses("L2");
        [Test] public void L3_WarOfAttrition_M2_5() => AssertPasses("L3");
        [Test] public void L4_ResidentIntruder_M2_6() => AssertPasses("L4");
        [Test] public void L5_PeckingOrder_M2_7() => AssertPasses("L5");
        [Test] public void L6_CoyFast_M2_8() => AssertPasses("L6");
        [Test] public void L7_SuckerCheatGrudger_M2_9() => AssertPasses("L7");
        [Test] public void L8_RoundRobin_AndL8b_M2_10() => AssertPasses("L8");
        [Test] public void L9_Ecological_M2_11() => AssertPasses("L9");
        [Test] public void L9b_TftAllD_Clustering_M2_11_M2_15() => AssertPasses("L9b");
        [Test] public void L10_DiggerWasps_M2_12() => AssertPasses("L10");
        [Test] public void L11_Hamilton_M2_12() => AssertPasses("L11");
        [Test] public void L12_SelfishHerd_M2_12() => AssertPasses("L12");

        [Test]
        public void L13_VampireBats_RunsAndReports_M2_13()
        {
            AssertPasses("L13");
            foreach (var r in Of("L13"))
                Assert.IsTrue(r.Checks.Where(c => c.Kind != CheckKind.Report).All(c => c.Name == "L13 runs and reports"), "L13 claims no numeric expectation");
        }

        [Test]
        public void L2b_EveryMatrixCell_WithinThreeOfTheMonteCarlo_Test8()
        {
            foreach (var r in Of("L2"))
            {
                var cells = r.Checks.Where(c => c.Name.StartsWith("L2b ") && c.Name.Contains("Monte Carlo against the exact")).ToList();
                Assert.AreEqual(25 + 16, cells.Count, "every cell of both 4.2 matrices");
                Assert.IsTrue(cells.All(c => c.Pass));
                // the published table differs from the turn rules in exactly the three Prober cells (recorded as amended)
                var amended = r.Checks.Where(c => c.Name.StartsWith("L2b ") && c.Kind == CheckKind.Amended).Select(c => c.Name).ToList();
                CollectionAssert.AreEquivalent(new[]
                {
                    "L2b Retaliator vs Prober: published 4.2 cell corrected to the turn-rule value",
                    "L2b Prober vs Retaliator: published 4.2 cell corrected to the turn-rule value",
                    "L2b Prober vs Prober: published 4.2 cell corrected to the turn-rule value",
                }, amended);
            }
        }

        [Test]
        public void NegativeControls_FailAsDesigned_Test17()
        {
            // an Euler step fails L6's conserved-quantity check; w0 = 100 fails L1's Hawk tolerance
            foreach (var r in Of("L6")) Assert.IsTrue(r.Checks.Single(c => c.Name.StartsWith("negative control")).Pass);
            foreach (var r in Of("L1")) Assert.IsTrue(r.Checks.Single(c => c.Name.StartsWith("negative control")).Pass);
        }

        // ---- M2.17: lab all --check in under 60 s on one core; exit 0 on a pass, 1 on a miss ----

        [Test]
        public void LabAll_UnderSixtySecondsOnOneCore_M2_17()
        {
            Serial();
            Assert.Less(_serialSeconds, 60.0, "lab all (14 experiments x seeds 1-5) on one thread");
            TestContext.Progress.WriteLine("lab all, seeds 1-5, one thread: " + _serialSeconds.ToString("0.0") + " s");
            Assert.IsTrue(Serial().All(r => r.Pass), string.Join("\n", Serial().Where(r => !r.Pass).Select(r => r.Experiment + " seed " + r.Seed)));
        }

        [Test]
        public void LabCommand_ExitCodes()
        {
            Assert.AreEqual(ExitCodes.Ok, TestUtil.Cli(out var o, out var e, "lab", "run", "L7", "--no-out"), o + e);
            StringAssert.Contains("PASS", o);
            // a miss: 5 generations are far too few for L1's agent checks
            Assert.AreEqual(ExitCodes.CheckFailed, TestUtil.Cli(out o, out e, "lab", "run", "L1", "--gens", "5", "--no-out"), o + e);
            StringAssert.Contains("[FAIL]", o);
            Assert.AreEqual(ExitCodes.Usage, TestUtil.Cli(out _, out _, "lab", "run", "L99"));
            Assert.AreEqual(ExitCodes.Usage, TestUtil.Cli(out _, out _, "lab", "frobnicate"));
            Assert.AreEqual(ExitCodes.Usage, TestUtil.Cli(out _, out _, "lab", "run", "L7", "--format", "xml"));
            Assert.AreEqual(ExitCodes.Usage, TestUtil.Cli(out _, out _, "lab", "run", "L7", "--bogus", "1"));
            Assert.AreEqual(ExitCodes.Ok, TestUtil.Cli(out o, out _, "lab", "list"));
            foreach (var x in LabRegistry.All()) StringAssert.Contains(x.Id + "  " + x.Title, o);
            Assert.AreEqual(ExitCodes.Ok, TestUtil.Cli(out o, out _, "lab", "list", "--json"));
            var arr = JArray.Parse(o);
            Assert.AreEqual(LabRegistry.All().Length, arr.Count);
            Assert.IsTrue(arr.All(t => !string.IsNullOrEmpty((string)t["status"]) && !string.IsNullOrEmpty((string)t["statusLabel"])));
        }

        [Test]
        public void LabAll_Check_ExitCode_OnASubset()
        {
            // the dispatcher, the table and the exit code (the full run is timed in-process above)
            Assert.AreEqual(ExitCodes.Ok, TestUtil.Cli(out var o, out var e, "lab", "all", "--check", "--seeds", "1"), o + e);
            StringAssert.Contains("| L13 |", o);
            StringAssert.Contains("70 pass".Replace("70", LabRegistry.All().Length.ToString()), o);
        }

        // ---- outputs: JSONL and CSV, report, hash ----

        [Test]
        public void LabRun_WritesJsonlCsvChecksAndSummary_AndReportReadsThem()
        {
            string dir = TestUtil.TempDir("lab");
            try
            {
                Assert.AreEqual(0, TestUtil.Cli(out var o, out var e, "lab", "run", "L7", "--seeds", "1..2", "--out", dir), o + e);
                Assert.AreEqual(0, TestUtil.Cli(out o, out e, "lab", "run", "L4", "--out", dir, "--format", "csv"), o + e);
                string l7 = Path.Combine(dir, "lab", "L7", "1");
                foreach (var f in new[] { "result.jsonl", "checks.json", "summary.txt" }) Assert.IsTrue(File.Exists(Path.Combine(l7, f)), f);
                var lines = File.ReadAllLines(Path.Combine(l7, "result.jsonl"));
                Assert.AreEqual("LabRunStarted", (string)JObject.Parse(lines[0])["record"]);
                Assert.AreEqual("LabRunEnded", (string)JObject.Parse(lines[lines.Length - 1])["record"]);
                Assert.AreEqual(1001, lines.Count(l => l.Contains("\"LabGeneration\"")), "one row per generation (0..1000)");
                var checks = JObject.Parse(File.ReadAllText(Path.Combine(l7, "checks.json")));
                Assert.AreEqual("invented", (string)checks["status"]);
                Assert.IsTrue((bool)checks["pass"]);
                string l4 = Path.Combine(dir, "lab", "L4", "1");
                Assert.IsTrue(File.Exists(Path.Combine(l4, "result.csv")));
                Assert.IsTrue(File.ReadAllLines(Path.Combine(l4, "result.csv"))[0].StartsWith("gen,share_bourgeois,share_anti_bourgeois"));
                Assert.IsTrue(File.ReadAllLines(Path.Combine(l4, "checks.csv")).Length > 5);
                Assert.AreEqual(0, TestUtil.Cli(out o, out e, "lab", "report", dir), o + e);
                StringAssert.Contains("| L7 | 2 |", o);
                StringAssert.Contains("| L4 | 1 | Derived |", o);
            }
            finally { TestUtil.Cleanup(dir); }
        }

        // ---- M2.14 / test 11: determinism ----

        /// <summary>Golden result hashes (seed 1), stored after the first accepted M2 run.</summary>
        private static readonly Dictionary<string, string> Golden = new Dictionary<string, string>
        {
            ["L1"] = "7a06f9b04a5c3780",
            ["L2"] = "815ddf13e8116bc9",
            ["L3"] = "b44661d0c0d2772f",
            ["L4"] = "e0ccb19ea42f22e5",
            ["L5"] = "356f0085c5fee617",
            ["L6"] = "c4c5d73593e25059",
            ["L7"] = "8b0a021d75fb4ff3",
            ["L8"] = "c9560aea8c74c513",
            ["L9"] = "070e7303257d941f",
            ["L9b"] = "598e2f5b7efe847a",
            ["L10"] = "d067a02c4428321a",
            ["L11"] = "e67657473b3bd07f",
            ["L12"] = "340f006459d2754c",
            ["L13"] = "08f21a814b7de2b1",
        };

        [Test]
        public void SameConfigAndSeed_SameHash_InProcess_InAnotherProcess_AndGolden_M2_14()
        {
            foreach (var r in Of("L7").Concat(Of("L4")).Concat(Of("L12")))
            {
                var again = LabRegistry.Find(r.Experiment).Run(new LabConfig { Experiment = r.Experiment, Seed = r.Seed });
                Assert.AreEqual(r.ResultHash, again.ResultHash, r.Experiment + " seed " + r.Seed);
            }
            // another process
            string dll = typeof(God.Runner.Cli).Assembly.Location;
            var psi = new ProcessStartInfo("dotnet", "\"" + dll + "\" lab hash L7 --seeds 1..3") { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
            string output;
            using (var p = Process.Start(psi)) { output = p.StandardOutput.ReadToEnd(); p.WaitForExit(); Assert.AreEqual(0, p.ExitCode); }
            var lines = output.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
            Assert.AreEqual(3, lines.Count, output);
            for (int i = 0; i < 3; i++)
            {
                var r = Of("L7").Single(x => x.Seed == (ulong)(i + 1));
                Assert.AreEqual("L7 " + (i + 1) + " " + LabHash.Hex(r.ResultHash), lines[i]);
            }
            // golden hashes, seed 1
            foreach (var r in Serial().Where(x => x.Seed == 1))
                if (Golden.TryGetValue(r.Experiment, out var g)) Assert.AreEqual(g, LabHash.Hex(r.ResultHash), r.Experiment + ": the golden hash changed");
            Assert.AreEqual(LabRegistry.All().Length, Golden.Count, "every experiment has a golden hash");
            // different seeds differ
            Assert.AreEqual(5, Of("L2").Select(r => r.ResultHash).Distinct().Count());
        }

        [Test]
        public void ParallelEqualsSerial_M2_14()
        {
            var serial = Serial();
            var jobs = serial.Select(r => (r.Experiment, r.Seed)).ToList();
            var par = new LabResult[jobs.Count];
            System.Threading.Tasks.Parallel.For(0, jobs.Count, new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = Math.Max(2, Environment.ProcessorCount / 2) },
                i => par[i] = LabRegistry.Find(jobs[i].Experiment).Run(new LabConfig { Experiment = jobs[i].Experiment, Seed = jobs[i].Seed }));
            for (int i = 0; i < jobs.Count; i++) Assert.AreEqual(serial[i].ResultHash, par[i].ResultHash, jobs[i].Experiment + " seed " + jobs[i].Seed);
            // and through the CLI with workers
            Assert.AreEqual(0, TestUtil.Cli(out var o, out var e, "lab", "all", "--check", "--seeds", "1..2", "--workers", "4", "--json"), o + e);
            var j = JObject.Parse(o.Trim().Split('\n').Last());
            foreach (var run in (JArray)j["runs"])
            {
                var s = serial.Single(r => r.Experiment == (string)run["experiment"] && r.Seed == (ulong)run["seed"]);
                Assert.AreEqual(LabHash.Hex(s.ResultHash), (string)run["resultHash"]);
            }
        }

        // ---- M2.20 / test 24: honest labels in the generated reports ----

        [Test]
        public void Reports_CarryTheStatus_AndNonExactNeverCallsItselfAReproduction_M2_20()
        {
            var rx = new Regex("(?i)reproduc");
            foreach (var r in Serial())
            {
                var exp = LabRegistry.Find(r.Experiment);
                var json = LabCommand.ChecksJson(r, exp);
                Assert.AreEqual(ReproStatusText.Code(exp.Status), (string)json["status"]);
                Assert.IsTrue(((JArray)json["checks"]).All(c => !string.IsNullOrEmpty((string)c["status"])));
                if (exp.Status == ReproStatus.Exact) continue;
                string dir = TestUtil.TempDir("lab-label");
                try
                {
                    LabCommand.Write(r, dir, "jsonl", exp);
                    foreach (var f in Directory.GetFiles(dir))
                        Assert.IsFalse(rx.IsMatch(File.ReadAllText(f)), r.Experiment + " (" + ReproStatusText.Label(exp.Status) + "): " + Path.GetFileName(f) + " uses the word reproduction");
                }
                finally { TestUtil.Cleanup(dir); }
            }
            var md = LabCommand.ReportMarkdown(Serial().Where(r => r.Experiment != "L1").Select(r => (r, LabRegistry.Find(r.Experiment))).ToList());
            Assert.IsFalse(rx.IsMatch(md));
        }

        // ---- M2.18: a game with no benchmark assigned is unchanged ----

        [Test]
        public void IslandGame_HashUnchangedByTheLab_M2_18()
        {
            // watch6 seed 1, 3000 ticks: the final hash recorded on unity/poc 3281480 (before Sim/Lab existed)
            var r = RunEngine.Run(TestUtil.Spec("watch6", 1, 3000));
            Assert.IsNull(r.Error);
            Assert.AreEqual(IslandGolden, Hex.Of(r.FinalHash));
        }

        private const string IslandGolden = "0b989c3fea159f2b";
    }
}
