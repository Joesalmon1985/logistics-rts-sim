using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using God.Recording;
using God.Runner.Warehouse;
using God.Sim;
using God.Sim.Learning;
using God.Sim.Population;
using Microsoft.Data.Sqlite;
using NUnit.Framework;

namespace God.Runner.Tests
{
    /// <summary>
    /// M3.29 and M13a.5 for the population tables: dim_person, fact_death, fact_person_day, fact_production (personal goods) and
    /// fact_learning reconcile exactly with the in-memory event stream on the 6 x 20 island, and recording changes no hash.
    /// </summary>
    [TestFixture]
    public class PopulationWarehouseTests
    {
        private string _dir;
        [SetUp] public void SetUp() { _dir = TestUtil.TempDir("popwh"); }
        [TearDown] public void TearDown() { SqliteConnection.ClearAllPools(); TestUtil.Cleanup(_dir); }

        private (RunResult res, string db, CaptureRecorder cap) Run(string tag, ulong seed, long ticks, RecordingLevel level, bool sqlite = true, Action<RunSpec> tweak = null)
        {
            var spec = TestUtil.Spec("band_6x20", seed, ticks);
            spec.Scenario.Checks.Remove("smoke");
            spec.Level = level; tweak?.Invoke(spec);
            if (sqlite) spec.Sqlite = new WarehouseOptions { ForeignKeys = true };
            spec.RunDir = Path.Combine(_dir, tag); spec.OutRoot = _dir; spec.SkipJsonl = true;
            var cap = new CaptureRecorder();
            spec.ExtraRecorders.Add(() => cap);
            var res = RunEngine.Run(spec);
            Assert.IsNull(res.Error, res.Error);
            Assert.AreEqual(0, res.ExitCode, string.Join("; ", res.Messages));
            return (res, res.WarehousePath, cap);
        }

        private static long Q(string db, string sql) { using (var d = WarehouseDb.OpenRead(db)) return Convert.ToInt64(d.Scalar(sql) ?? 0L); }

        private const long Ticks = 3 * 1200 + 5;   // three full sim days and a little
        // NpcDay v2 (F0): care (M4) and social (M5) ticks are part of the day
        private const string AllTicks = "ticks_idle + ticks_eat + ticks_forage + ticks_sleep + ticks_rest + ticks_explore + ticks_gather + ticks_build + ticks_flee + ticks_carry + ticks_other + ticks_care + ticks_social";

        [Test]
        public void Full_Reconciles_PersonsDeathsPersonDaysProductionAndLearning_WithTheEventStream()
        {
            var (res, db, cap) = Run("full", 3, Ticks, RecordingLevel.Full);
            var ev = cap.Events;
            var spawned = ev.OfType<NpcSpawned>().ToList(); var died = ev.OfType<NpcDied>().ToList(); var days = ev.OfType<NpcDay>().ToList();
            Assert.AreEqual(120, spawned.Count);
            Assert.Greater(days.Count, 200, "person-days were produced");

            // persons (M4: every person has one origin event, a founder's NpcSpawned or a birth's NpcBorn)
            var born = ev.OfType<God.Sim.Population.Households.NpcBorn>().ToList();
            Assert.AreEqual(spawned.Count + born.Count, Q(db, "SELECT COUNT(*) FROM dim_person")); // no npc 0 row since the I-BS fix (12.35)
            Assert.AreEqual(spawned.Count, Q(db, "SELECT COUNT(*) FROM dim_person WHERE is_founder = 1 AND spawn_tick IS NOT NULL"));
            Assert.AreEqual(spawned.Select(s => s.Npc).Concat(born.Select(b => b.Npc)).Distinct().Count(), Q(db, "SELECT COUNT(DISTINCT npc_id) FROM dim_person"));
            foreach (var f in Enumerable.Range(0, 6))
                Assert.AreEqual(spawned.Count(s => (int)s.Faction == f) + born.Count(b => (int)b.Faction == f), Q(db, "SELECT COUNT(*) FROM dim_person p JOIN dim_faction x ON x.faction_key = p.faction_key WHERE x.faction_id = " + f), "faction " + f);

            // deaths, by cause, and the person dimension agrees
            Assert.AreEqual(died.Count, Q(db, "SELECT COUNT(*) FROM fact_death"));
            Assert.AreEqual(died.Count, Q(db, "SELECT COUNT(*) FROM dim_person WHERE death_tick IS NOT NULL"));
            foreach (var g in died.GroupBy(d => (int)d.Cause))
            {
                Assert.AreEqual(g.Count(), Q(db, "SELECT COUNT(*) FROM fact_death WHERE cause_code = " + g.Key), "cause " + (DeathCause)g.Key);
                Assert.AreEqual(g.Count(), Q(db, "SELECT COUNT(*) FROM dim_person WHERE death_cause_code = " + g.Key));
            }
            Assert.AreEqual(died.Sum(d => d.AgeTicks), Q(db, "SELECT COALESCE(SUM(age_ticks), 0) FROM fact_death"));
            Assert.AreEqual(0, Q(db, "SELECT COUNT(*) FROM fact_death f JOIN dim_person p ON p.person_key = f.person_key WHERE p.death_tick <> f.tick"));

            // person-days: one row per NpcDay, one per person per day, ticks add up
            Assert.AreEqual(days.Count, Q(db, "SELECT COUNT(*) FROM fact_person_day"));
            Assert.AreEqual(0, Q(db, "SELECT COUNT(*) FROM (SELECT date_key, person_key FROM fact_person_day GROUP BY 1, 2 HAVING COUNT(*) > 1)"));
            foreach (var g in days.GroupBy(d => d.Day))
                Assert.AreEqual(g.Count(), Q(db, "SELECT COUNT(*) FROM fact_person_day WHERE date_key = " + g.Key), "day " + g.Key);
            long ticksLived = days.Sum(d => d.Ticks.Sum(x => (long)x));
            Assert.AreEqual(ticksLived, Q(db, "SELECT SUM(" + AllTicks + ") FROM fact_person_day"));
            Assert.AreEqual(days.Sum(d => (long)d.Ticks[Activity.Sleep]), Q(db, "SELECT SUM(ticks_sleep) FROM fact_person_day"));
            Assert.AreEqual(days.Sum(d => (long)d.Ticks[Activity.Forage]), Q(db, "SELECT SUM(ticks_forage) FROM fact_person_day"));
            Assert.AreEqual(days.Sum(d => (long)d.Meals), Q(db, "SELECT SUM(meals) FROM fact_person_day"));
            Assert.AreEqual(days.Sum(d => (long)d.Decisions), Q(db, "SELECT SUM(decisions) FROM fact_person_day"));
            Assert.AreEqual(days.Sum(d => (long)d.RewardSum), Q(db, "SELECT SUM(reward_sum) FROM fact_person_day"));
            Assert.AreEqual(days.Sum(d => (long)d.PunishSum), Q(db, "SELECT SUM(punish_sum) FROM fact_person_day"));
            Assert.AreEqual(days.Sum(d => (long)d.Health), Q(db, "SELECT SUM(health) FROM fact_person_day"));
            Assert.AreEqual(days.Count(d => d.HasBed), Q(db, "SELECT COUNT(*) FROM fact_person_day WHERE has_bed = 1"));
            // activity ticks equal the ticks lived: every row's allocation equals the same person's own NpcDay total
            Assert.AreEqual(0, Q(db, "SELECT COUNT(*) FROM fact_person_day WHERE " + AllTicks + " <= 0"));
            // no row for a person after the day it died
            Assert.AreEqual(0, Q(db, "SELECT COUNT(*) FROM fact_person_day f JOIN dim_person p ON p.person_key = f.person_key WHERE p.death_date_key IS NOT NULL AND f.date_key > p.death_date_key"));

            // personal goods: one row per event, by direction and reason, quantities equal
            var prod = ev.OfType<Produced>().ToList(); var cons = ev.OfType<Consumed>().ToList();
            Assert.Greater(prod.Count, 50);
            Assert.AreEqual(prod.Count, Q(db, "SELECT COUNT(*) FROM fact_production WHERE lot_id IS NOT NULL AND direction = 1"));
            Assert.AreEqual(cons.Count, Q(db, "SELECT COUNT(*) FROM fact_production WHERE lot_id IS NOT NULL AND direction = -1"));
            // the sim's own removals (npc 0) are rows with no person, and no npc 0 person exists (I-BS fix, 12.35)
            Assert.AreEqual(cons.Count(c => c.Npc == 0), Q(db, "SELECT COUNT(*) FROM fact_production WHERE lot_id IS NOT NULL AND direction = -1 AND person_key IS NULL"));
            Assert.AreEqual(0, Q(db, "SELECT COUNT(*) FROM dim_person WHERE npc_id = 0"));
            foreach (var g in prod.GroupBy(p => (int)p.Reason))
                Assert.AreEqual(g.Sum(p => (long)p.Count), Q(db, "SELECT COALESCE(SUM(quantity), 0) FROM fact_production WHERE direction = 1 AND lot_id IS NOT NULL AND reason_code = " + g.Key), "produced " + (ProduceReason)g.Key);
            foreach (var g in cons.GroupBy(p => (int)p.Reason))
                Assert.AreEqual(g.Count(), Q(db, "SELECT COUNT(*) FROM fact_production WHERE direction = -1 AND reason_code = " + g.Key), "consumed " + (ConsumeReason)g.Key);
            foreach (var g in prod.GroupBy(p => (int)p.Ware))
                Assert.AreEqual(g.Sum(p => (long)p.Count) - cons.Where(c => (int)c.Ware == g.Key).Sum(c => (long)c.Count),
                    Q(db, "SELECT COALESCE(SUM(quantity * direction), 0) FROM fact_production WHERE lot_id IS NOT NULL AND ware_key = " + g.Key), "ledger of ware " + g.Key);
            // the stage 3 rows are untouched: still one per WaresProduced, with a building
            Assert.AreEqual(ev.OfType<WaresProduced>().Count(), Q(db, "SELECT COUNT(*) FROM fact_production WHERE person_key IS NULL AND building_key IS NOT NULL"));

            // learning, per kind
            var upd = ev.OfType<AssociationUpdated>().Count(); var rec = ev.OfType<ConjunctionRecruited>().Count();
            var fre = ev.OfType<ConjunctionFreed>().Count(); var epi = ev.OfType<EpisodeClosed>().Count();
            Assert.Greater(upd, 100);
            Assert.AreEqual(upd, Q(db, "SELECT COUNT(*) FROM fact_learning WHERE kind_code = 1"));
            Assert.AreEqual(rec, Q(db, "SELECT COUNT(*) FROM fact_learning WHERE kind_code = 2"));
            Assert.AreEqual(fre, Q(db, "SELECT COUNT(*) FROM fact_learning WHERE kind_code = 3"));
            Assert.AreEqual(epi, Q(db, "SELECT COUNT(*) FROM fact_learning WHERE kind_code = 4"));
            Assert.AreEqual(ev.OfType<AssociationUpdated>().Sum(a => (long)a.Delta), Q(db, "SELECT COALESCE(SUM(delta), 0) FROM fact_learning WHERE kind_code = 1"));
            Assert.AreEqual(ev.OfType<EpisodeClosed>().Sum(a => (long)a.Return), Q(db, "SELECT COALESCE(SUM(return_value), 0) FROM fact_learning WHERE kind_code = 4"));

            // the decision trace (M3b, Full only): one row per DecisionMade, with its terms
            var dec = ev.OfType<DecisionMade>().ToList();
            Assert.Greater(dec.Count, 1000, "Full recording traces every decision");
            Assert.AreEqual(dec.Count, Q(db, "SELECT COUNT(*) FROM fact_decision"));
            Assert.AreEqual(dec.Sum(x => (long)x.Total), Q(db, "SELECT COALESCE(SUM(total), 0) FROM fact_decision"));
            Assert.AreEqual(dec.Sum(x => (long)x.Learned), Q(db, "SELECT COALESCE(SUM(learned), 0) FROM fact_decision"));
            Assert.AreEqual(dec.Count(x => x.ExploreWon), Q(db, "SELECT COUNT(*) FROM fact_decision WHERE explore_won = 1"));
            foreach (var g in dec.GroupBy(x => x.Verb))
                Assert.AreEqual(g.Count(), Q(db, "SELECT COUNT(*) FROM fact_decision WHERE verb = " + g.Key), "decisions of verb " + g.Key);
            Assert.AreEqual(0, Q(db, "SELECT COUNT(*) FROM fact_decision WHERE total <> instinct + learned + social + culture + personality - cost + explore"));

            // F0.14: one fact_ownership row per OwnershipChanged; the parties are in dim_party; the lots of fact_production
            var own = ev.OfType<OwnershipChanged>().ToList();
            Assert.Greater(own.Count, 0, "pick-ups, drops and death spills change owners");
            Assert.AreEqual(own.Count, Q(db, "SELECT COUNT(*) FROM fact_ownership"));
            foreach (var g in own.GroupBy(o => (int)o.Basis))
                Assert.AreEqual(g.Count(), Q(db, "SELECT COUNT(*) FROM fact_ownership WHERE basis_code = " + g.Key), "basis " + (OwnershipBasis)g.Key);
            Assert.AreEqual(own.Count(o => o.FromKind == OwnerKind.None), Q(db, "SELECT COUNT(*) FROM fact_ownership WHERE from_party_key IS NULL"));
            Assert.AreEqual(own.Count(o => o.ToKind == OwnerKind.None), Q(db, "SELECT COUNT(*) FROM fact_ownership WHERE to_party_key IS NULL"));
            Assert.AreEqual(own.Where(o => o.ToKind == OwnerKind.Npc).Select(o => o.ToId).Concat(own.Where(o => o.FromKind == OwnerKind.Npc).Select(o => o.FromId)).Distinct().Count(),
                Q(db, "SELECT COUNT(*) FROM dim_party WHERE party_kind_code = 1 AND person_key IS NOT NULL"));
            Assert.AreEqual(prod.Sum(p => (long)p.Lot), Q(db, "SELECT COALESCE(SUM(lot_id), 0) FROM fact_production WHERE direction = 1 AND lot_id IS NOT NULL"));
            Assert.AreEqual(cons.Sum(p => (long)p.Lot), Q(db, "SELECT COALESCE(SUM(lot_id), 0) FROM fact_production WHERE direction = -1"));
            // M4 care acts (ChildFed 134, CareGiven 136) and M5 gifts (213) fill the social day aggregate, exactly, before any sampling
            Assert.AreEqual(ev.OfType<God.Sim.Population.Households.ChildFed>().Count() + ev.OfType<God.Sim.Population.Households.CareGiven>().Count(),
                Q(db, "SELECT COALESCE(SUM(events), 0) FROM fact_social_day WHERE kind_code IN (134, 136)"), "the social day holds exactly the care acts");
            Assert.AreEqual(0, Q(db, "SELECT COUNT(*) FROM fact_social_day WHERE kind_code NOT IN (134, 136, 213)"), "nothing else fills the social day (M5WarehouseTests reconciles the gifts)");

            // none of these events also lands in fact_event; counts by code agree with fact_event_day
            Assert.AreEqual(0, Q(db, "SELECT COUNT(*) FROM fact_event WHERE event_code IN (100, 101, 104, 200, 201, 206, 330, 331, 332, 333, 334)"));
            foreach (var g in ev.GroupBy(e => EventCatalog.Of(e.GetType()).Code))
                Assert.AreEqual(g.Count(), Q(db, "SELECT COALESCE(SUM(events), 0) FROM fact_event_day WHERE event_code = " + g.Key), "fact_event_day " + g.Key);
            using (var d = WarehouseDb.OpenRead(db))
            {
                Assert.AreEqual(0, WarehouseRecorder.ForeignKeyViolations(d));
                // M4 and M5 add their own grains (HouseholdMappings.Grains, SocialMappings.Grains)
                Assert.AreEqual(9 + HouseholdMappings.Grains.Length + SocialMappings.Grains.Length, d.Count("meta_table_grain"));
                foreach (var t in new[] { "dim_person", "fact_death", "fact_person_day", "fact_production", "fact_learning", "fact_decision", "dim_party", "fact_ownership", "fact_social_day" })
                    Assert.AreEqual(1, (long)d.Scalar("SELECT COUNT(*) FROM meta_table_grain WHERE table_name = '" + t + "'"), t);
                Assert.AreEqual("fact_person_day", (string)d.Scalar("SELECT target_table FROM meta_event_map WHERE code = 104"));
                Assert.AreEqual("dim_person", (string)d.Scalar("SELECT target_table FROM meta_event_map WHERE code = 100"));
            }
            TestContext.WriteLine("population warehouse: " + spawned.Count + " persons, " + died.Count + " deaths, " + days.Count + " person-days, " + prod.Count + " produced, "
                + cons.Count + " consumed, " + upd + "/" + rec + "/" + fre + "/" + epi + " learning events, " + dec.Count + " decisions.");
        }

        [Test]
        public void Summary_PersonDaysDeathsAndLowVolumeLearning_AreExact_HighVolumeIsSampledWithWeights()
        {
            var (_, db, cap) = Run("summary", 3, Ticks, RecordingLevel.Summary);
            var ev = cap.Events;
            Assert.AreEqual(ev.OfType<NpcSpawned>().Count() + ev.OfType<God.Sim.Population.Households.NpcBorn>().Count(), Q(db, "SELECT COUNT(*) FROM dim_person")); // M4: founders and births (no npc 0 row since the I-BS fix, 12.35)
            Assert.AreEqual(ev.OfType<NpcDied>().Count(), Q(db, "SELECT COUNT(*) FROM fact_death"));
            Assert.AreEqual(ev.OfType<NpcDay>().Count(), Q(db, "SELECT COUNT(*) FROM fact_person_day"));
            Assert.AreEqual(ev.OfType<ConjunctionRecruited>().Count(), Q(db, "SELECT COUNT(*) FROM fact_learning WHERE kind_code = 2"));
            Assert.AreEqual(ev.OfType<EpisodeClosed>().Count(), Q(db, "SELECT COUNT(*) FROM fact_learning WHERE kind_code = 4"));
            // sampled kinds: rows weigh sample_weight, total near the stream
            long upd = ev.OfType<AssociationUpdated>().Count(), prod = ev.OfType<Produced>().Count();
            long w1 = Q(db, "SELECT COALESCE(SUM(sample_weight), 0) FROM fact_learning WHERE kind_code = 1");
            long w2 = Q(db, "SELECT COALESCE(SUM(sample_weight), 0) FROM fact_production WHERE direction = 1 AND lot_id IS NOT NULL");
            Assert.That(w1, Is.InRange(upd * 0.75, upd * 1.25 + 64));
            Assert.That(w2, Is.InRange(prod * 0.75, prod * 1.25 + 64));
            // the decision trace is Full only: no event and no row at Summary
            Assert.AreEqual(0, ev.OfType<DecisionMade>().Count());
            Assert.AreEqual(0, Q(db, "SELECT COUNT(*) FROM fact_decision"));
            using (var d = WarehouseDb.OpenRead(db)) Assert.AreEqual(0, WarehouseRecorder.ForeignKeyViolations(d));
        }

        [Test]
        public void Deaths_OnACrowdedIsland_ReconcileByCause_AndCloseTheirPersonDays()
        {
            var (_, db, cap) = Run("deaths", 1, 36000, RecordingLevel.Summary, true, sp => sp.Scenario.StartNpcsPerFaction = 80);
            var died = cap.Events.OfType<NpcDied>().ToList();
            TestContext.WriteLine("deaths in 30 days (6 x 80): " + died.Count + " " + string.Join(",", died.GroupBy(d => d.Cause).Select(g => g.Key + "=" + g.Count())));
            Assert.Greater(died.Count, 0, "a crowded island has deaths");
            Assert.AreEqual(died.Count, Q(db, "SELECT COUNT(*) FROM fact_death"));
            foreach (var g in died.GroupBy(d => (int)d.Cause)) Assert.AreEqual(g.Count(), Q(db, "SELECT COUNT(*) FROM fact_death WHERE cause_code = " + g.Key), "cause " + g.Key);
            Assert.AreEqual(cap.Events.OfType<NpcDay>().Count(), Q(db, "SELECT COUNT(*) FROM fact_person_day"));
            Assert.AreEqual(0, Q(db, "SELECT COUNT(*) FROM fact_person_day f JOIN dim_person p ON p.person_key = f.person_key WHERE p.death_date_key IS NOT NULL AND f.date_key > p.death_date_key"));
            // M4: the living are the founders and the born (and arrivals) less the dead
            Assert.AreEqual(cap.Events.OfType<NpcSpawned>().Count() + cap.Events.OfType<God.Sim.Population.Households.NpcBorn>().Count()
                + cap.Events.OfType<God.Sim.Population.Households.NpcImmigrated>().Count() - died.Count, Q(db, "SELECT COUNT(*) FROM dim_person WHERE death_tick IS NULL"));
        }

        [Test]
        public void Warehouse_OfAPopulationRun_IsDeterministic()
        {
            var (_, a, _) = Run("det1", 4, Ticks, RecordingLevel.Full);
            var (_, b, _) = Run("det2", 4, Ticks, RecordingLevel.Full);
            Assert.AreEqual(WarehouseExport.ContentHash(a), WarehouseExport.ContentHash(b));
        }

        [Test]
        public void Recording_FullSummaryOff_GiveTheSameFinalStateHash_OnAPopulationScenario()
        {
            foreach (ulong seed in new ulong[] { 1, 2 })
            {
                var (off, _, _) = Run("off" + seed, seed, Ticks, RecordingLevel.Off, sqlite: false);
                var (sum, _, _) = Run("sum" + seed, seed, Ticks, RecordingLevel.Summary);
                var (full, _, _) = Run("full" + seed, seed, Ticks, RecordingLevel.Full);
                Assert.AreEqual(off.FinalHash, sum.FinalHash, "summary, seed " + seed);
                Assert.AreEqual(off.FinalHash, full.FinalHash, "full, seed " + seed);
                CollectionAssert.AreEqual(TestUtil.Seq(off), TestUtil.Seq(full));
            }
        }

        [Test]
        public void PopulationTables_ExportToCsvAndParquet_WithEqualCountsAndSums()
        {
            var (_, db, _) = Run("export", 3, Ticks, RecordingLevel.Full);
            var tables = new[] { "dim_person", "fact_death", "fact_person_day", "fact_production", "fact_learning", "meta_table_grain" };
            string csv = Path.Combine(_dir, "csv"), lake = Path.Combine(_dir, "lake");
            Assert.AreEqual(0, TestUtil.Cli(out _, out var se, "export", "--from", db, "--to", "csv", "--out", csv), se);
            Assert.AreEqual(0, TestUtil.Cli(out _, out se, "export", "--from", db, "--to", "parquet", "--out", lake), se);
            using (var d = WarehouseDb.OpenRead(db))
                foreach (var t in tables)
                {
                    var lines = File.ReadAllLines(Path.Combine(csv, t + ".csv"));
                    Assert.AreEqual(d.Count(t) + 1, lines.Length, t + " csv rows + header");
                    Assert.AreEqual(string.Join(",", WarehouseExport.Columns(d, t).Select(c => c.Name)), lines[0]);
                    var dir = WarehouseExport.GlobalTables.Contains(t) ? Path.Combine(lake, "_global", t) : Path.Combine(lake, t);
                    var files = Directory.GetFiles(dir, "*.parquet", SearchOption.AllDirectories);
                    Assert.AreEqual(1, files.Length, t);
                    var (rows, sums) = WarehouseExport.ReadParquetTotals(files[0]);
                    Assert.AreEqual(d.Count(t), rows, t + " parquet rows");
                    if (t != "fact_death") Assert.Greater(rows, 0, t);
                    foreach (var c in WarehouseExport.Columns(d, t).Where(x => x.IsInteger))
                        Assert.AreEqual(Convert.ToInt64(d.Scalar("SELECT COALESCE(SUM(" + c.Name + "), 0) FROM " + t) ?? 0L), sums.GetValueOrDefault(c.Name), t + "." + c.Name);
                }
        }

        // ------------------------------------------------------------------ owner D3: the decision trace is the decisions

        /// <summary>
        /// D3.1: every DecisionMade is exactly one intent the NPC then started (same NPC, verb and target, in order, tick by
        /// tick), and tracing them changes nothing: the traced and the untraced sim keep the same hash.
        /// </summary>
        [Test]
        public void DecisionTrace_IsExactlyTheIntentsStarted_AndChangesNoHash()
        {
            var setup = new GameSetup(2, GameSetup.NoHuman, 6) { Population = true, StartNpcsPerFaction = 20, Victory = VictorySpec.SandboxOnly() };
            var traced = SimBuilder.Create(setup).Sim; traced.TraceDecisions = true;
            var plain = SimBuilder.Create(setup).Sim;
            var pop = traced.Systems.OfType<PopulationSystem>().Single();
            var started = new List<(int, byte, God.Sim.Policy.IntentTarget, int)>();
            pop.Started = (id, a) => started.Add((id, a.Verb, a.TargetKind, a.TargetKind == God.Sim.Policy.IntentTarget.Node ? a.TargetNode : a.TargetId));
            long decisions = 0;
            for (int t = 1; t <= 2 * setup.TicksPerDay + 5; t++)
            {
                started.Clear();
                traced.Step(); plain.Step();
                var dec = traced.DrainEvents().OfType<DecisionMade>()
                    .Select(d => (d.Npc, d.Verb, d.TargetKind, d.TargetKind == God.Sim.Policy.IntentTarget.Node ? d.TargetNode : d.TargetId)).ToList();
                plain.DrainEvents();
                CollectionAssert.AreEqual(started, dec, "tick " + t + ": decisions traced vs intents started");
                decisions += dec.Count;
                if (t % 600 == 0) Assert.AreEqual(plain.StateHash(), traced.StateHash(), "tick " + t);
            }
            Assert.Greater(decisions, 1000);
            Assert.AreEqual(plain.StateHash(), traced.StateHash());
            TestContext.WriteLine(decisions + " decisions, each one started intent");
        }

        /// <summary>D3.1: recording at Full (with the decision trace and the warehouse) gives the same final hash as recording Off.</summary>
        [Test]
        public void FullRecording_WithTheDecisionTrace_HashesLikeOff()
        {
            var (full, _, cap) = Run("hfull", 2, Ticks, RecordingLevel.Full);
            var (off, _, _) = Run("hoff", 2, Ticks, RecordingLevel.Off, sqlite: false);
            Assert.Greater(cap.Events.OfType<DecisionMade>().Count(), 1000);
            Assert.AreEqual(off.FinalHash, full.FinalHash);
        }
    }
}
