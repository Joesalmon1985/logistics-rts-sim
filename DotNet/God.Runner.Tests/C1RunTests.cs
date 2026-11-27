using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using God.Recording;
using God.Runner.Warehouse;
using God.Sim;
using God.Sim.Civ;
using God.Sim.Comms;
using God.Sim.Learning;
using God.Sim.Population;
using God.Sim.Social;
using Microsoft.Data.Sqlite;
using NUnit.Framework;

namespace God.Runner.Tests
{
    /// <summary>
    /// C1 at run level (docs/civdiv/DEVELOPMENT_PACKAGES/C1.md section 13): the idle pin (T1), M5 trade with Civ on (T9), the
    /// civ_assets scenario (T10: people build and work productive assets from what they gather, nothing scripted) and the
    /// warehouse reconciliation (T12).
    /// </summary>
    [Parallelizable(ParallelScope.None)]
    public class C1RunTests
    {
        private string _dir;
        private EndState _end;
        [SetUp] public void SetUp() { _dir = TestUtil.TempDir("c1"); }
        [TearDown] public void TearDown() { SqliteConnection.ClearAllPools(); TestUtil.Cleanup(_dir); }

        /// <summary>C1-T1: Civ on, asset building off (nothing to found, sense or work): every part except civ equals the P8 base digest's parts.</summary>
        [Test]
        public void Civ_Idle_Neutral()
        {
            for (ulong seed = 1; seed <= 3; seed++)
            {
                var off = F1NeutralityTests.Spec("ibs_families", seed);
                var on = TestUtil.Spec("ibs_families", seed, F1NeutralityTests.Ticks, "tuning.Divinity.Enabled=0", "tuning.Orgs.Enabled=0", "tuning.Civ.Enabled=1", "tuning.Civ.C1.BuildEnabled=0");
                on.Scenario.Checks.Remove("smoke");
                on.CheckpointEvery = F1NeutralityTests.Every;
                var r = TestUtil.RunAll(new[] { off, on });
                Assert.AreEqual(0, r[0].ExitCode, r[0].Error); Assert.AreEqual(0, r[1].ExitCode, r[1].Error);
                Assert.AreEqual(F1NeutralityTests.Base["ibs_families/" + seed], F1NeutralityTests.Digest(r[0]), "the off run is P8");
                Assert.AreEqual(r[0].FinalParts.Length + 1, r[1].FinalParts.Length, "the civ part is appended");
                int civ = Array.IndexOf(GodSim.AllHashPartNames.ToArray(), "civ");
                for (int c = 0; c < r[0].Checkpoints.Count; c++)
                    for (int i = 0; i < r[0].Checkpoints[c].Parts.Length; i++)
                        if (GodSim.AllHashPartNames[i] != "core")
                            Assert.AreEqual(r[0].Checkpoints[c].Parts[i], r[1].Checkpoints[c].Parts[i], "seed " + seed + " checkpoint " + c + " part " + GodSim.AllHashPartNames[i]);
                Assert.Greater(civ, 0);
            }
        }

        /// <summary>C1-T9: M5's honest-pair test (accepted deals complete, multi-unit deals traded incrementally) holds with Civ on.</summary>
        [Test]
        public void Scenario2_Trade_WithCiv()
        {
            int accepted = 0, completed = 0, multi = 0;
            for (ulong seed = 1; seed <= 20; seed++)
            {
                var lab = new M5SocialLab(seed, t => t.Civ.Enabled = 1);
                Assert.IsNotNull(lab.Sim.State.Civ);
                var a = lab.Add(lab.Glade, null, LearningMode.On, fed: false, (Ware.Fish, 4));
                var b = lab.Add(lab.At(0, 2), null, LearningMode.On, fed: false, (Ware.Berries, 4));
                lab.Step(1200 * 3);
                var agreed = lab.Of<GestureMade>().Where(g => g.Act == GestureAct.Accept && (g.Speaker == a.Id || g.Speaker == b.Id)).Select(g => g.DealId).Distinct().ToList();
                var closed = lab.Of<ExchangeClosed>().ToDictionary(c => c.DealId, c => c);
                var opened = lab.Of<ExchangeOpened>().ToDictionary(c => c.DealId, c => c);
                foreach (int d in agreed)
                {
                    if (!opened.TryGetValue(d, out var o) || o.Kind != DealKind.Spot || !closed.TryGetValue(d, out var c)) continue;
                    accepted++;
                    if (c.Outcome == DealOutcome.Completed) completed++;
                    if (o.ACount + o.BCount > 2) multi++;
                }
                Assert.IsEmpty(lab.Of<TheftCommitted>(), "honest trade is no theft");
                Assert.IsEmpty(SimInvariants.Check(lab.Sim.State));
            }
            TestContext.WriteLine("Civ on: accepted " + accepted + ", completed " + completed + ", multi-unit " + multi);
            Assert.Greater(accepted, 20, "the pair trades");
            Assert.GreaterOrEqual(completed * 10, accepted * 9, "at least 90% of accepted deals complete");
        }

        /// <summary>Counts of one run of civ_assets (events kept by the capture recorder).</summary>
        private static (int built, int used, int workers, int stored, int thefts, int claims, RunResult res) Assets(ulong seed, long ticks = 12000)
        {
            var spec = TestUtil.Spec("civ_assets", seed, ticks);
            spec.Scenario.Checks.Remove("smoke");
            var cap = new CaptureRecorder();
            spec.ExtraRecorders.Add(() => cap);
            var res = RunEngine.Run(spec);
            Assert.IsNull(res.Error, res.Error);
            Assert.AreEqual(0, res.ExitCode, string.Join("; ", res.Messages));
            var e = cap.Events;
            var used = e.OfType<AssetUsed>().ToList();
            var builtIds = new HashSet<int>(e.OfType<AssetBuilt>().Select(b => b.Asset));
            int stored = e.OfType<Produced>().Count(p => p.Reason == ProduceReason.Crafted || p.Reason == ProduceReason.HandGathered && p.OwnerKind != OwnerKind.None && used.Any(u => u.Tick == p.Tick && u.Worker == p.Npc));
            return (builtIds.Count, used.Count(u => builtIds.Contains(u.Asset)), used.Select(u => u.Worker).Distinct().Count(), stored, e.OfType<TheftCommitted>().Count(), e.OfType<SourceClaimed>().Count(), res);
        }

        /// <summary>C1-T10: civ_assets, 10 seeds x 10 days: on at least 7 seeds someone builds an asset and works it, the output stored (a report, not a tuned target).</summary>
        [Test]
        public void Scenario_OperatesProductiveAsset()
        {
            int good = 0;
            for (ulong seed = 1; seed <= 10; seed++)
            {
                var (built, used, workers, stored, thefts, claims, _) = Assets(seed);
                bool ok = built > 0 && used > 0 && stored > 0;
                if (ok) good++;
                TestContext.WriteLine("seed " + seed + ": assets built " + built + ", work units " + used + " by " + workers + " workers, outputs stored " + stored
                    + ", thefts classified " + thefts + ", source claims " + claims + (ok ? "" : "  (none operated)"));
            }
            TestContext.WriteLine("seeds where an NPC built and worked an asset: " + good + "/10");
            Assert.GreaterOrEqual(good, 7);
        }

        private (RunResult res, string db, CaptureRecorder cap) Recorded(string tag, RecordingLevel level, ulong seed = 3, long ticks = 12000)
        {
            var spec = TestUtil.Spec("civ_assets", seed, ticks);
            spec.Scenario.Checks.Remove("smoke");
            spec.Level = level;
            spec.Sqlite = new WarehouseOptions { ForeignKeys = true, Async = false };
            spec.RunDir = Path.Combine(_dir, tag); spec.OutRoot = _dir; spec.SkipJsonl = true;
            var cap = new CaptureRecorder();
            spec.ExtraRecorders.Add(() => cap);
            spec.ExtraRecorders.Add(() => _end = new EndState());
            var res = RunEngine.Run(spec);
            Assert.IsNull(res.Error, res.Error);
            Assert.AreEqual(0, res.ExitCode, string.Join("; ", res.Messages));
            return (res, res.WarehousePath, cap);
        }

        private static long Q(string db, string sql) { using (var d = WarehouseDb.OpenRead(db)) return Convert.ToInt64(d.Scalar(sql) ?? 0L); }

        /// <summary>
        /// C1-T12: rows equal events for 270-275; open ownership spells equal the owners in state at the end; spells per
        /// asset half-open and non-overlapping (W-D5-6), each asset's open spell 1000 permille; theft only at Full. Recording
        /// changes no hash.
        /// </summary>
        [Test]
        public void C1_Warehouse_Reconciles()
        {
            var (full, db, cap) = Recorded("full", RecordingLevel.Full);
            var endFull = _end;
            var ev = cap.Events;
            int built = ev.OfType<AssetBuilt>().Count(), transferred = ev.OfType<AssetTransferred>().Count();
            Assert.Greater(built, 0, "assets were built in civ_assets seed 3");
            Assert.AreEqual(built + transferred, Q(db, "SELECT COUNT(*) FROM fact_ownership_spell"), "one spell per AssetBuilt and AssetTransferred");
            Assert.AreEqual(ev.OfType<AssetUsed>().Count(), Q(db, "SELECT COUNT(*) FROM fact_asset_use"), "one row per AssetUsed at Full");
            Assert.AreEqual(ev.OfType<UseRightGranted>().Count(), Q(db, "SELECT COUNT(*) FROM fact_use_right"));
            Assert.AreEqual(ev.OfType<UseRightEnded>().Count(), Q(db, "SELECT COUNT(*) FROM fact_use_right WHERE to_tick IS NOT NULL"));
            Assert.AreEqual(ev.OfType<TheftCommitted>().Count(), Q(db, "SELECT COUNT(*) FROM fact_theft"), "one row per TheftCommitted at Full");
            Assert.AreEqual(ev.OfType<SourceClaimed>().Count(), Q(db, "SELECT COUNT(*) FROM fact_source_claim"));
            Assert.Greater(ev.OfType<SourceClaimed>().Count(), 0, "first arrivals claimed sources");
            // open spells = the finished assets' owners in state at the end
            var finished = endFull.State.Civ.Ownership.Assets.Where(a => a.IsFinished).ToList();
            {
                Assert.AreEqual(finished.Count, Q(db, "SELECT COUNT(*) FROM fact_ownership_spell WHERE to_tick IS NULL"));
                foreach (var a in finished)
                    Assert.AreEqual(1L, Q(db, "SELECT COUNT(*) FROM fact_ownership_spell WHERE to_tick IS NULL AND asset_id = " + a.Id + " AND owner_kind_code = " + (int)a.Owner.Kind + " AND owner_id = " + a.Owner.Id));
            }
            Assert.AreEqual(0L, Q(db, "SELECT COUNT(*) FROM fact_ownership_spell a JOIN fact_ownership_spell b ON a.asset_id = b.asset_id AND a.spell_key < b.spell_key "
                + "WHERE a.from_tick < COALESCE(b.to_tick, 9223372036854775807) AND b.from_tick < COALESCE(a.to_tick, 9223372036854775807)"), "spells never overlap");
            Assert.AreEqual(0L, Q(db, "SELECT COUNT(*) FROM fact_ownership_spell WHERE to_tick IS NOT NULL AND to_tick < from_tick"), "half-open spells");
            Assert.AreEqual(0L, Q(db, "SELECT COUNT(*) FROM (SELECT asset_id, SUM(shares_permille) s FROM fact_ownership_spell WHERE to_tick IS NULL GROUP BY asset_id) WHERE s <> 1000"), "every open spell set sums to 1000 permille");
            TestContext.WriteLine("civ_assets seed 3: built " + built + ", spells " + Q(db, "SELECT COUNT(*) FROM fact_ownership_spell") + ", work units " + Q(db, "SELECT COUNT(*) FROM fact_asset_use")
                + ", thefts " + Q(db, "SELECT COUNT(*) FROM fact_theft") + ", claims " + Q(db, "SELECT COUNT(*) FROM fact_source_claim"));
            // at Summary: no theft row (truth), and recording changes no hash
            var (sum, db2, cap2) = Recorded("summary", RecordingLevel.Summary);
            Assert.AreEqual(0L, Q(db2, "SELECT COUNT(*) FROM fact_theft"), "theft is analytics-only truth: no row below Full");
            var plain = TestUtil.Spec("civ_assets", 3, 12000);
            plain.Scenario.Checks.Remove("smoke");
            var p = RunEngine.Run(plain);
            Assert.AreEqual(p.FinalHash, full.FinalHash, "recording at Full changes no hash");
            Assert.AreEqual(p.FinalHash, sum.FinalHash, "recording at Summary changes no hash");
        }

        /// <summary>Keeps the run's final state (read only).</summary>
        private sealed class EndState : IRunRecorder
        {
            public GodState State;
            public void Begin(RunInfo info, GodSim sim) { }
            public void OnTick(GodSim sim, IReadOnlyList<SimEvent> events) { }
            public void OnDayEnd(GodSim sim, FactionDayRow[] rows) { }
            public void End(GodSim sim, IDictionary<string, long> counters) { State = sim.State; }
            public void Dispose() { }
        }
    }
}
