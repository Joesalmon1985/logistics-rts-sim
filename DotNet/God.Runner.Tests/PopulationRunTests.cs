using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using God.Recording;
using God.Sim;
using God.Sim.Learning;
using God.Sim.Population;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace God.Runner.Tests
{
    /// <summary>
    /// M3 in the runner (ACCEPTANCE M3.8, M3.9, M3.13, G2 with the population on): the band_6x20 scenario, same seed same
    /// hashes, recording levels equal, parallel equals serial, save at day 2.5 with open traces equals the straight run,
    /// bounds over a long run, learn-dump, populationBelow, and the stage 3 hashes untouched.
    /// </summary>
    [Parallelizable(ParallelScope.None)]
    public class PopulationRunTests
    {
        private static RunSpec Band(ulong seed, long ticks, params string[] sets)
        {
            var s = TestUtil.Spec("band_6x20", seed, ticks, sets);
            s.Scenario.Checks.Remove("smoke");
            return s;
        }

        [Test]
        public void Band6x20_SameSeedSameHashes_DifferentSeedsDiffer_DecisionsIncluded()
        {
            var specs = new List<RunSpec>();
            for (ulong seed = 1; seed <= 4; seed++) { specs.Add(Band(seed, 6000)); specs.Add(Band(seed, 6000)); }
            var r = TestUtil.RunAll(specs);
            var finals = new HashSet<ulong>();
            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(0, r[2 * i].ExitCode, r[2 * i].Error + " " + string.Join("; ", r[2 * i].Messages));
                CollectionAssert.AreEqual(TestUtil.Seq(r[2 * i]), TestUtil.Seq(r[2 * i + 1]), "seed " + (i + 1));
                finals.Add(r[2 * i].FinalHash);
                Assert.AreEqual(10, r[2 * i].Sim.StateHashParts().Length, "population, cognition and learning are hashed");
            }
            Assert.AreEqual(4, finals.Count);
        }

        [Test]
        public void Band6x20_RecordingLevels_GiveEqualHashes()
        {
            string dir = TestUtil.TempDir("popmatrix");
            try
            {
                var specs = new List<(ulong seed, string name, RunSpec spec)>();
                for (ulong seed = 1; seed <= 2; seed++)
                {
                    var b = Band(seed, 4800);
                    b.WriteReplay = true;
                    foreach (var (name, s) in Verify.RecordMatrix(b, Path.Combine(dir, seed.ToString()))) specs.Add((seed, name, s));
                }
                var res = TestUtil.RunAll(specs.Select(x => x.spec).ToList());
                for (ulong seed = 1; seed <= 2; seed++)
                {
                    var group = Enumerable.Range(0, specs.Count).Where(i => specs[i].seed == seed).ToList();
                    var reference = res[group[0]];
                    foreach (int i in group)
                    {
                        Assert.AreEqual(0, res[i].ExitCode, specs[i].name + ": " + string.Join("; ", res[i].Messages));
                        CollectionAssert.AreEqual(TestUtil.Seq(reference), TestUtil.Seq(res[i]), "seed " + seed + " " + specs[i].name);
                    }
                    var full = res[group.Single(i => specs[i].name == "level full")];
                    Assert.Greater(full.Counters["rows.events"], 1000, "population events are recorded at Full");
                }
            }
            finally { TestUtil.Cleanup(dir); }
        }

        [Test]
        public void Band6x20_ParallelEqualsSerial()
        {
            var seeds = Enumerable.Range(1, 6).Select(i => (ulong)i).ToList();
            var serial = seeds.Select(s => RunEngine.Run(Band(s, 3600))).ToList();
            var par = TestUtil.RunAll(seeds.Select(s => Band(s, 3600)).ToList(), 6);
            for (int i = 0; i < seeds.Count; i++) Assert.AreEqual(serial[i].FinalHash, par[i].FinalHash, "seed " + seeds[i]);
        }

        [Test]
        public void Band6x20_SaveAtDay2Point5WithOpenTraces_EqualsStraight()
        {
            // M3.8 / L9: save mid-day (open traces and episodes), load, continue to day 5: every checkpoint equal (every 100 ticks)
            var problems = new List<string>();
            System.Threading.Tasks.Parallel.For(1, 11, seed =>
            {
                var spec = Band((ulong)seed, 6000); spec.CheckpointEvery = 100;
                var p = Verify.SaveLoadMid(spec, new long[] { 3000 }, out var straight);
                lock (problems) { foreach (var x in p) problems.Add("seed " + seed + ": " + x); }
            });
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void Band6x20_BoundsHold_OverTwoSimYears()
        {
            // M3.9 / L11 at the commit tier: 120 NPCs for 2 sim years (24,000 ticks), invariants (store caps, weights within
            // +-4000, sorted stores, conserved goods) checked every sim day. The 50-year 200-NPC run is a NIGHT run.
            var t = new SimTuning();
            var setup = new GameSetup(3, GameSetup.NoHuman, 6) { Population = true, StartNpcsPerFaction = 20, Victory = VictorySpec.SandboxOnly() };
            var sim = SimBuilder.Create(setup, t).Sim;
            int bad = 0; int maxEntries = 0, maxConj = 0, maxRing = 0, maxOpen = 0;
            for (int d = 0; d < 20; d++)
            {
                for (int i = 0; i < 1200; i++) { sim.Step(); sim.DrainEvents(); }
                var b = SimInvariants.Check(sim.State);
                bad += b.Count;
                if (b.Count > 0) TestContext.WriteLine("day " + d + ": " + string.Join("; ", b.Take(3)));
                foreach (var n in sim.State.Population.Npcs.Where(n => n.Mind != null))
                {
                    maxEntries = Math.Max(maxEntries, n.Mind.Store.Count); maxConj = Math.Max(maxConj, n.Mind.Store.ConjUsed);
                    maxRing = Math.Max(maxRing, n.Mind.Store.RingCount); maxOpen = Math.Max(maxOpen, n.Mind.Store.OpenCount);
                }
            }
            TestContext.WriteLine($"2 sim years, 120 NPCs: max entries {maxEntries}/256, conjunctions {maxConj}/64, traces {maxRing}/16, episodes {maxOpen}/8; alive {sim.State.Population.AliveCount}");
            Assert.AreEqual(0, bad);
            Assert.LessOrEqual(maxEntries, 256); Assert.LessOrEqual(maxConj, 64); Assert.LessOrEqual(maxRing, 16); Assert.LessOrEqual(maxOpen, 8);
        }

        [Test]
        public void LearnDump_WritesTheStoreAsJsonLines()
        {
            int code = TestUtil.Cli(out var stdout, out var stderr, "learn-dump", "--scenario", "band_6x20", "--seed", "2", "--days", "1", "--npc", "3");
            Assert.AreEqual(ExitCodes.Ok, code, stderr);
            var lines = stdout.Split('\n').Where(l => l.Trim().Length > 0).ToList();
            var head = JObject.Parse(lines[0]);
            Assert.AreEqual("npc", (string)head["type"]);
            Assert.AreEqual(3, (int)head["npc"]);
            Assert.Greater((int)head["entries"], 0);
            Assert.Greater(lines.Count(l => l.Contains("\"type\":\"entry\"")), 0);
            Assert.IsTrue(lines.Any(l => l.Contains("\"words\"")), "keys are given in words too");
            Assert.AreEqual(ExitCodes.Ok, TestUtil.Cli(out var all, out _, "learn-dump", "--scenario", "band_6x20", "--seed", "2", "--days", "1", "--npc", "all"));
            Assert.AreEqual(120, all.Split('\n').Count(l => l.Contains("\"npc\"")));
            Assert.AreEqual(ExitCodes.InvalidInput, TestUtil.Cli(out _, out var e2, "learn-dump", "--scenario", "watch6", "--seed", "2", "--days", "1", "--npc", "3"));
            StringAssert.Contains("population", e2);
        }

        [Test]
        public void PopulationBelow_StopsARun()
        {
            var spec = Band(1, 6000, "stop.populationBelow=1000");
            var r = RunEngine.Run(spec);
            Assert.AreEqual(1200, r.Ticks, "stopped at the first day end");
            Assert.AreEqual(0, r.ExitCode);
        }

        [Test]
        public void ScenarioKeys_ResolveAndRoundTrip()
        {
            var sc = TestUtil.Load("band_6x20");
            Assert.IsTrue(sc.Population);
            Assert.AreEqual(20, sc.StartNpcsPerFaction);
            Assert.AreEqual(Policies.Associative, sc.Policies.DefaultOf(God.Sim.Policy.PolicyLevel.Civilian));
            var setup = sc.ToSetup(5);
            Assert.IsTrue(setup.Population);
            Assert.AreEqual(20, setup.StartNpcsPerFaction);
            var again = Scenario.Parse(sc.ToJson().ToString(), new List<string>());
            Assert.AreEqual(sc.Hash(), again.Hash());
        }
    }
}
