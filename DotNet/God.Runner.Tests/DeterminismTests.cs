using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using God.Recording;
using God.Sim;
using NUnit.Framework;

namespace God.Runner.Tests
{
    /// <summary>The M0 determinism suite (ACCEPTANCE_TESTS M0.1 to M0.7, G2.1 to G2.8).</summary>
    [Parallelizable(ParallelScope.None)]
    public class DeterminismTests
    {
        [Test]
        public void SameSeedSameCommands_SameHashes_AndDifferentSeedsDiffer()
        {
            // M0.1: watch6, seeds 1-8, up to 40,000 ticks, two runs each: equal at every checkpoint; 8 distinct final hashes.
            var specs = new List<RunSpec>();
            for (ulong seed = 1; seed <= 8; seed++) { specs.Add(TestUtil.Spec("watch6", seed, 40000)); specs.Add(TestUtil.Spec("watch6", seed, 40000)); }
            var r = TestUtil.RunAll(specs);
            var finals = new HashSet<ulong>();
            for (int i = 0; i < 8; i++)
            {
                var a = r[2 * i]; var b = r[2 * i + 1];
                Assert.AreEqual(0, a.ExitCode, "seed " + (i + 1) + ": " + a.Error + " " + a.SmokeLine);
                Assert.Greater(a.Checkpoints.Count, 5);
                CollectionAssert.AreEqual(TestUtil.Seq(a), TestUtil.Seq(b), "seed " + (i + 1));
                Assert.AreEqual(a.FinalHash, b.FinalHash);
                Assert.IsNull(Verify.Compare(a, b));
                finals.Add(a.FinalHash);
            }
            Assert.AreEqual(8, finals.Count, "different seeds, different final hashes");
        }

        [Test]
        public void RecordingLevels_AndDomainSwitches_GiveEqualHashes()
        {
            // M0.2: seeds 1-4, 2 sim years (24,000 ticks, sandbox so no seed ends early); Off, Metrics, Summary, Full and each
            // domain alone at Full, writing real files.
            string dir = TestUtil.TempDir("matrix");
            try
            {
                var specs = new List<(ulong seed, string name, RunSpec spec)>();
                for (ulong seed = 1; seed <= 4; seed++)
                {
                    var b = TestUtil.Spec("sandbox_watch6", seed, 24000);
                    b.WriteReplay = true;
                    foreach (var (name, s) in Verify.RecordMatrix(b, Path.Combine(dir, seed.ToString()))) specs.Add((seed, name, s));
                }
                var res = TestUtil.RunAll(specs.Select(x => x.spec).ToList());
                for (ulong seed = 1; seed <= 4; seed++)
                {
                    var group = Enumerable.Range(0, specs.Count).Where(i => specs[i].seed == seed).ToList();
                    var reference = res[group[0]];
                    Assert.AreEqual(24000, reference.Ticks);
                    Assert.AreEqual(20, reference.Checkpoints.Count);
                    foreach (int i in group)
                    {
                        Assert.AreEqual(0, res[i].ExitCode, specs[i].name + ": " + string.Join("; ", res[i].Messages));
                        CollectionAssert.AreEqual(TestUtil.Seq(reference), TestUtil.Seq(res[i]), "seed " + seed + " " + specs[i].name);
                    }
                    // the files exist where the level says
                    var full = res[group.Single(i => specs[i].name == "level full")];
                    Assert.IsTrue(File.Exists(Path.Combine(full.RunDir, "events.jsonl.gz")));
                    Assert.IsTrue(File.Exists(Path.Combine(full.RunDir, "snapshots.jsonl.gz")));
                    Assert.IsTrue(File.Exists(Path.Combine(full.RunDir, "metrics.jsonl")));
                    Assert.Greater(full.Counters["rows.events"], 1000);
                    var off = res[group.Single(i => specs[i].name == "level off")];
                    Assert.IsFalse(File.Exists(Path.Combine(off.RunDir, "events.jsonl.gz")));
                    Assert.IsFalse(File.Exists(Path.Combine(off.RunDir, "metrics.jsonl")));
                    Assert.IsTrue(File.Exists(Path.Combine(off.RunDir, "run.json")) && File.Exists(Path.Combine(off.RunDir, "replay.godr")));
                    var summary = res[group.Single(i => specs[i].name == "level summary")];
                    Assert.Less(summary.Counters["rows.events"], full.Counters["rows.events"], "summary samples the high-volume events");
                    Assert.Greater(summary.Counters["sampled_out"], 0);
                }
            }
            finally { TestUtil.Cleanup(dir); }
        }

        [Test]
        public void SnapshotBuilders_AreReadOnly()
        {
            // M0.3: calling every snapshot builder every tick vs never: identical hashes over 5,000 ticks.
            var a = TestUtil.Spec("watch6", 3, 5000); a.CheckpointEvery = 100;
            var b = TestUtil.Spec("watch6", 3, 5000); b.CheckpointEvery = 100; b.SnapshotEveryTick = true;
            var r = TestUtil.RunAll(new[] { a, b });
            Assert.AreEqual(50, r[0].Checkpoints.Count);
            CollectionAssert.AreEqual(TestUtil.Seq(r[0]), TestUtil.Seq(r[1]));
        }

        [Test]
        public void Parallel_EqualsSerial_WithThreadsAndProcesses()
        {
            // M0.4: 16 seeds with 8 workers and with processes: the same per-seed hashes and byte-identical summary.json
            // as a serial run (summary.json has no wall-clock field).
            string dir = TestUtil.TempDir("parallel");
            try
            {
                var seeds = Enumerable.Range(1, 16).Select(i => (ulong)i).ToList();
                RunSpec Template(string sub)
                {
                    var t = TestUtil.Spec("watch6", 0, 12000);
                    t.OutRoot = Path.Combine(dir, sub); t.Level = RecordingLevel.Metrics;
                    t.Scenario.Checks.Remove("smoke"); // 12,000 ticks is too short for the smoke verdict
                    return t;
                }
                var serial = Batch.Run(Template("serial"), seeds, 1, false, TextWriter.Null, out int c1);
                var threads = Batch.Run(Template("threads"), seeds, 8, false, TextWriter.Null, out int c2);
                var procs = Batch.Run(Template("procs"), seeds, 8, true, TextWriter.Null, out int c3);
                Assert.AreEqual(0, c1); Assert.AreEqual(0, c2); Assert.AreEqual(0, c3, string.Join("\n", procs.Select(p => p.Error)));
                for (int i = 0; i < seeds.Count; i++)
                {
                    Assert.AreEqual(serial[i].FinalHash, threads[i].FinalHash, "threads, seed " + seeds[i]);
                    Assert.AreEqual(serial[i].FinalHash, procs[i].FinalHash, "processes, seed " + seeds[i]);
                    Assert.AreEqual(serial[i].RunId, procs[i].RunId);
                    string s = File.ReadAllText(Path.Combine(serial[i].RunDir, "summary.json"));
                    Assert.AreEqual(s, File.ReadAllText(Path.Combine(threads[i].RunDir, "summary.json")), "summary.json byte-identical (threads), seed " + seeds[i]);
                    Assert.AreEqual(s, File.ReadAllText(Path.Combine(procs[i].RunDir, "summary.json")), "summary.json byte-identical (processes), seed " + seeds[i]);
                }
                Assert.IsTrue(File.Exists(Path.Combine(dir, "threads", "adhoc", "watch6", "batch.json")));
            }
            finally { TestUtil.Cleanup(dir); }
        }

        [Test]
        public void TwoTunings_InParallel_DoNotLeak()
        {
            // M0.4: one sim with Military.CritPct=0, one default, on two threads at once: each equals its own serial run.
            var crit0 = TestUtil.Spec("watch6", 5, 20000, "tuning.Military.CritPct=0");
            var dflt = TestUtil.Spec("watch6", 5, 20000);
            var serial0 = RunEngine.Run(crit0); var serialD = RunEngine.Run(dflt);
            var par = TestUtil.RunAll(new[] { Verify.Copy(crit0), Verify.Copy(dflt), Verify.Copy(crit0), Verify.Copy(dflt) }, 4);
            CollectionAssert.AreEqual(TestUtil.Seq(serial0), TestUtil.Seq(par[0]));
            CollectionAssert.AreEqual(TestUtil.Seq(serialD), TestUtil.Seq(par[1]));
            CollectionAssert.AreEqual(TestUtil.Seq(serial0), TestUtil.Seq(par[2]));
            CollectionAssert.AreEqual(TestUtil.Seq(serialD), TestUtil.Seq(par[3]));
            Assert.AreNotEqual(serial0.FinalHash, serialD.FinalHash, "the tuning is a different game");
            Assert.AreEqual(0, serial0.Sim.State.Tuning.Military.CritPct);
            Assert.AreEqual(10, serialD.Sim.State.Tuning.Military.CritPct);
        }

        [Test]
        public void SaveLoadMid_EqualsStraight()
        {
            // M0.6: seeds 1-4, save at T/3 and 2T/3 (T = 2 sim years, sandbox), load in a fresh sim, continue: checkpoints equal
            // the straight run; a second save of a loaded game is byte-identical.
            var problems = new List<string>();
            System.Threading.Tasks.Parallel.For(1, 5, seed =>
            {
                var spec = TestUtil.Spec("sandbox_watch6", (ulong)seed, 24000);
                var p = Verify.SaveLoadMid(spec, new long[] { 8000, 16000 }, out var straight);
                lock (problems) { foreach (var x in p) problems.Add("seed " + seed + ": " + x); if (straight.Ticks != 24000) problems.Add("seed " + seed + " ended at " + straight.Ticks); }
            });
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void PendingCommands_AreInTheHash_AndSurviveASave()
        {
            var a = GodSim.Create(GameSetup.Watch(6));
            var b = GodSim.Create(GameSetup.Watch(6));
            a.Submit(new DeclareWar(Faction.F0, Faction.F3));
            Assert.AreNotEqual(a.StateHash(), b.StateHash(), "the queue is in the hash");
            Assert.AreEqual(GodSim.StateHashOf(a.State), GodSim.StateHashOf(b.State), "the state alone is the same");
            var loaded = SaveFile.FromJson(SaveFile.From(a).ToJson()).ToSim();
            Assert.AreEqual(1, loaded.PendingCommands.Count);
            Assert.AreEqual(a.StateHash(), loaded.StateHash(), "and it survives a save");
            // and in a real run: the brains' commands queued at a save tick are carried over (SaveLoadMid covers the hashes)
            var spec = TestUtil.Spec("watch6", 6, 2000); spec.KeepSaveBytes = true; spec.SaveAtTick = 1900;
            var r = RunEngine.Run(spec);
            Assert.AreEqual(1, r.Saves.Count);
        }

        [Test]
        public void Divergence_Bisect_FindsTheExactTick()
        {
            // M0.7: an injected nondeterminism (test-only hook) in run B is found at the exact tick, and the module named.
            var a = TestUtil.Spec("watch6", 7, 6000);
            var b = TestUtil.Spec("watch6", 7, 6000);
            b.TestSystems.Add(() => new PokeSystem(3333));
            string dir = TestUtil.TempDir("bisect");
            try
            {
                var d = Verify.CompareRuns(a, b, false, dir, out _, out _);
                Assert.IsNotNull(d);
                Assert.AreEqual(2400, d.LastGoodTick);
                Assert.AreEqual(3600, d.BadCheckpointTick);
                Assert.AreEqual(3333, d.FirstBadTick);
                CollectionAssert.Contains(d.Modules, "territory");
                Assert.IsTrue(File.Exists(Path.Combine(dir, "diverge_a.json")) && File.Exists(Path.Combine(dir, "diverge_b.json")));
                var sa = SaveFile.FromJson(File.ReadAllText(Path.Combine(dir, "diverge_a.json")));
                Assert.AreEqual(3333, sa.State.Tick);
            }
            finally { TestUtil.Cleanup(dir); }
        }

        [Test]
        public void StaticAudit_NoMutableStatics()
        {
            // G2.7 from the runner suite: the same reflection audit as the EditMode suite.
            new God.Sim.Tests.StaticAuditTests().StaticAudit_NoMutableStatics();
        }
    }
}
