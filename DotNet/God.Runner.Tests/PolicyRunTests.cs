using System;
using System.IO;
using System.Linq;
using God.Recording;
using God.Sim;
using God.Sim.Information;
using God.Sim.Policy;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace God.Runner.Tests
{
    /// <summary>
    /// M1 in whole runs: the default policies behind the new seams replay the M0 baseline exactly (M1.4), the scenario
    /// <c>policies</c> block selects registry names (M1.6), the true-state overlay never changes the sim (M1.5) and the
    /// view never shows foreign truth during a game (M1.9).
    /// </summary>
    public class PolicyRunTests
    {
        /// <summary>
        /// watch6 seeds 1-12 at M0 (commit 3281480, recorded before any M1 change): end tick, winner and final state hash.
        /// End ticks and winners are GameRulesRunTests.M0Baseline; the hashes were printed by <c>God.Runner run --scenario
        /// watch6 --seed N</c> on that commit.
        /// </summary>
        private static readonly (ulong seed, long endTick, string winner, string hash)[] M0 =
        {
            (1, 17534, "Violet", "12be0f64cefc59f1"), (2, 20161, "Crimson", "4bd54b271857a803"), (3, 26560, "Ivory", "0a66415b389d7a87"),
            (4, 24444, "Crimson", "1dfae552c855597b"), (5, 15968, "Amber", "14e55429e7ac286c"), (6, 20509, "Azure", "041da015993cd59a"),
            (7, 20627, "Ivory", "ce69d4a21a9c7b68"), (8, 20661, "Amber", "937e9fae37ca0117"), (9, 17020, "Ivory", "86622d757d7f9096"),
            (10, 30346, "Violet", "44b65041b57dd1eb"), (11, 20616, "Ivory", "d2fe348f86b1c2e0"), (12, 21970, "Jade", "eb9e0065ae98d1fc"),
        };

        [Test]
        public void DefaultPolicies_ReplayTheM0Baseline_Seeds1To12_SameEndTicksWinnersAndHashes()
        {
            // M1.4: AiBrain behind AiBrainFactionPolicy and the soldiers behind Stage3MilitaryPolicy change nothing.
            var runs = TestUtil.RunAll(M0.Select(b => TestUtil.Spec("watch6", b.seed, 40000)).ToList());
            for (int i = 0; i < runs.Length; i++)
            {
                var (seed, end, winner, hash) = M0[i];
                var s = runs[i].Sim.State;
                Assert.AreEqual(end, s.Factions.GameOverTick, "seed " + seed + " end tick");
                Assert.AreEqual(winner, FactionInfo.Name((Faction)s.Factions.Winner), "seed " + seed + " winner");
                Assert.AreEqual(hash, Hex.Of(runs[i].FinalHash), "seed " + seed + " final hash");
            }
        }

        [Test]
        public void TheDefaultSeams_AreReallyInUse()
        {
            var sp = TestUtil.Spec("watch6", 2, 3000);
            var r = RunEngine.Run(sp);
            var sim = r.Sim;
            var mil = SimBuilder.MilitaryOf(sim);
            foreach (var f in sim.State.Setup.Factions) Assert.IsInstanceOf<Stage3MilitaryPolicy>(mil.PolicyOf(f));
            Assert.IsNull(r.Error);
        }

        // ---------------------------------------------------------------- the policies block (M1.6)

        private static Scenario Parse(string policies) =>
            Scenario.Parse("{\"scenarioVersion\":1,\"name\":\"p\",\"setup\":{\"human\":-1},\"policies\":" + policies + "}");

        [Test]
        public void PoliciesBlock_SelectsRegistryNamesPerFactionAndLevel()
        {
            var sc = Parse("{\"faction\":{\"default\":\"AiBrain\",\"2\":\"none\",\"5\":\"none\"},\"military\":{\"default\":\"Stage3MilitaryPolicy\",\"1\":\"Stage3MilitaryPolicy\"}}");
            Assert.AreEqual("none", sc.Policies.NameFor(PolicyLevel.Faction, 2));
            Assert.AreEqual("AiBrain", sc.Policies.NameFor(PolicyLevel.Faction, 3));
            var resolved = sc.ToJson()["policies"];
            Assert.AreEqual("none", (string)resolved["faction"]["5"]);
            Assert.AreEqual("none", (string)resolved["civilian"]["default"]);
            Assert.AreEqual(sc.Hash(), Scenario.FromJson(sc.ToJson()).Hash(), "the resolved form round-trips");

            var session = new SimSession(sc, 3, null, null);
            CollectionAssert.AreEqual(new[] { Faction.F0, Faction.F1, Faction.F3, Faction.F4 }, session.Director.Seats.Select(s => s.Side).ToArray());
            for (int i = 0; i < 3000; i++) session.StepQuiet();
            Assert.AreEqual(1, session.Sim.State.Territory.Buildings.Count(b => b.Faction == Faction.F2), "faction 2 plays no policy");
            Assert.Greater(session.Sim.State.Territory.Buildings.Count(b => b.Faction == Faction.F3), 1);
        }

        [TestCase("{\"faction\":{\"default\":\"AiBrian\"}}", "unknown policy 'AiBrian'")]
        [TestCase("{\"faction\":{\"default\":\"external\"}}", "M15")]
        [TestCase("{\"faction\":{\"7\":\"AiBrain\"}}", "faction id")]
        [TestCase("{\"military\":{\"default\":\"none\"}}", "soldiers must have a policy")]
        [TestCase("{\"military\":{\"0\":\"AiBrain\"}}", "unknown policy 'AiBrain'")]
        [TestCase("{\"institution\":{\"default\":\"scripted.v1\"}}", "M12")]
        [TestCase("{\"civilian\":{\"sample\":{\"policy\":\"benchmark.TitForTat\",\"permille\":0}}}", "M5")]
        [TestCase("{\"diplomat\":{\"default\":\"x\"}}", "policy level")]
        [TestCase("{\"institution\":{\"3\":\"none\"}}", "per-faction")]
        public void PoliciesBlock_RefusesUnusableNamesAtLoad(string block, string mentioned)
        {
            var e = Assert.Throws<ScenarioException>(() => Parse(block));
            StringAssert.Contains(mentioned, e.Message);
        }

        [Test]
        public void PoliciesBlock_AnUnknownName_IsExitCode3()
        {
            string dir = TestUtil.TempDir("policy");
            try
            {
                string path = Path.Combine(dir, "x.scenario.json");
                File.WriteAllText(path, "{\"scenarioVersion\":1,\"name\":\"x\",\"policies\":{\"faction\":{\"1\":\"Nobody\"}}}");
                Assert.AreEqual(ExitCodes.InvalidInput, TestUtil.Cli(out _, out var err, "run", "--scenario", path, "--seed", "1", "--no-out"));
                StringAssert.Contains("Nobody", err);
            }
            finally { TestUtil.Cleanup(dir); }
        }

        [Test]
        public void AnExplicitDefaultBlock_GivesTheSameRunAsNone()
        {
            var a = RunEngine.Run(TestUtil.Spec("watch6", 4, 6000));
            var b = RunEngine.Run(TestUtil.Spec("watch6", 4, 6000, "policies.faction.default=AiBrain", "policies.military.default=Stage3MilitaryPolicy", "policies.military.3=Stage3MilitaryPolicy"));
            CollectionAssert.AreEqual(TestUtil.Seq(a), TestUtil.Seq(b));
        }

        [Test]
        public void RunManifest_NamesThePolicies()
        {
            string dir = TestUtil.TempDir("manifest");
            try
            {
                var sp = TestUtil.Spec("watch6", 1, 1200, "policies.faction.4=none");
                sp.OutRoot = dir;
                var r = RunEngine.Run(sp);
                var run = JObject.Parse(File.ReadAllText(Path.Combine(r.RunDir, "run.json")));
                Assert.AreEqual("none", (string)run["policies"]["faction"]["4"]);
                Assert.AreEqual("AiBrain", (string)run["policies"]["faction"]["default"]);
                Assert.AreEqual("observation", (string)run["purpose"]);
            }
            finally { TestUtil.Cleanup(dir); }
        }

        // ---------------------------------------------------------------- Q22 in scenarios

        [Test]
        public void ScenarioPurpose_DecidesTheOverlay()
        {
            Assert.IsFalse(Scenario.Parse("{\"scenarioVersion\":1,\"name\":\"c\",\"setup\":{\"human\":0}}").ToSetup(1).AllowTrueStateOverlay, "competitive VP game");
            StringAssert.Contains("competitive", Assert.Throws<ScenarioException>(() => Scenario.Parse("{\"scenarioVersion\":1,\"name\":\"c\",\"setup\":{\"human\":0,\"trueStateOverlay\":true}}")).Message);
            Assert.IsFalse(TestUtil.Load("sandbox_watch6").ToSetup(1).AllowTrueStateOverlay, "sandbox: off unless enabled");
            Assert.IsTrue(TestUtil.Load("sandbox_watch6", "setup.trueStateOverlay=true").ToSetup(1).AllowTrueStateOverlay);
            Assert.IsTrue(TestUtil.Load("watch6").ToSetup(1).AllowTrueStateOverlay, "watch mode is an observation scenario");
            Assert.IsTrue(TestUtil.Load("watch6", "setup.purpose=lab").ToSetup(1).AllowTrueStateOverlay);
            Assert.IsTrue(Scenario.Parse("{\"scenarioVersion\":1,\"name\":\"d\",\"setup\":{\"human\":0,\"purpose\":\"developer\"}}").ToSetup(1).AllowTrueStateOverlay);
            StringAssert.Contains("purpose", Assert.Throws<ScenarioException>(() => TestUtil.Load("watch6", "setup.purpose=ranked")).Message);
        }

        [Test]
        public void TogglingTheOverlayEveryDay_LeavesEveryCheckpointHashIdentical()
        {
            // M1.5 / I11: two runs, same seed; in one the overlay is built and read, toggled on and off, at every sim day.
            ITrueStateOverlay ov = null; long reads = 0;
            var plain = RunEngine.Run(TestUtil.Spec("watch6", 6, 40000));
            var sp = TestUtil.Spec("watch6", 6, 40000);
            sp.BeforeStep = sim =>
            {
                if (sim.State.Tick % sim.State.Setup.TicksPerDay != 0) return;
                ov = ov == null ? ViewFiller.Overlay(sim, Faction.F0) : null;
                if (ov != null) foreach (var f in FactionInfo.All) reads += ov.TrueStockAll(f) + ov.TrueSoldiers(f) + ov.TrueBuildings(f) + ov.TrueWaresInTransit(f);
            };
            var toggled = RunEngine.Run(sp);
            Assert.Greater(reads, 0, "the overlay was really read");
            Assert.Greater(plain.Checkpoints.Count, 10);
            CollectionAssert.AreEqual(TestUtil.Seq(plain), TestUtil.Seq(toggled));
            Assert.AreEqual(plain.FinalHash, toggled.FinalHash);
        }

        [Test]
        public void NoForeignTruth_DuringAWholeGame()
        {
            // M1.9 / I19 in a real run: at every sim day, adding wares to every other faction's stores (and taking them back)
            // leaves the viewer's whole view digest unchanged, while the viewer's own stores are in it.
            var sp = TestUtil.Spec("watch6", 3, 24000);
            int checkedDays = 0;
            sp.BeforeStep = sim =>
            {
                var s = sim.State;
                if (s.Tick == 0 || s.Tick % s.Setup.TicksPerDay != 0) return;
                var view = ViewFiller.For(sim, Faction.F1);
                ulong d0 = view.Digest();
                foreach (var b in s.Economy.Buildings) if (b.Faction != Faction.F1 && b.IsHq) b.Stock[(int)Ware.Bread] += 3;
                ulong d1 = view.Digest();
                foreach (var b in s.Economy.Buildings) if (b.Faction != Faction.F1 && b.IsHq) b.Stock[(int)Ware.Bread] -= 3;
                Assert.AreEqual(d0, d1, "tick " + s.Tick + ": foreign stores leaked into the view");
                var mine = s.Economy.HqsOf(Faction.F1);
                if (mine.Count > 0)
                {
                    mine[0].Stock[(int)Ware.Bread] += 1;
                    Assert.AreNotEqual(d0, view.Digest(), "tick " + s.Tick + ": own stores are in the view");
                    mine[0].Stock[(int)Ware.Bread] -= 1;
                }
                checkedDays++;
            };
            var r = RunEngine.Run(sp);
            Assert.Greater(checkedDays, 10);
            Assert.AreEqual(0, r.ExitCode, r.Error);
        }

        // ---------------------------------------------------------------- StepHeld removed

        [Test]
        public void ReplayHoldLines_AreRefused()
        {
            var header = new ReplayHeader { Scenario = TestUtil.Load("watch6").ToJson(), Seed = 1 }.ToJson().ToString(Newtonsoft.Json.Formatting.None);
            var bytes = System.Text.Encoding.UTF8.GetBytes(header + "\n{\"t\":5,\"hold\":\"ai\"}\n");
            var e = Assert.Throws<ReplayException>(() => ReplayFile.Read(bytes));
            StringAssert.Contains("M1", e.Message);
        }
    }
}
