using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using God.Recording;
using God.Runner.Warehouse;
using God.Sim;
using Microsoft.Data.Sqlite;
using NUnit.Framework;

namespace God.Runner.Tests
{
    /// <summary>A test recorder that keeps every event it is given (to compare the warehouse with the stream it was built from).</summary>
    internal sealed class CaptureRecorder : IRunRecorder
    {
        public readonly List<SimEvent> Events = new List<SimEvent>();
        public FactionDayRow[] LastRows;
        public long Days;
        public void Begin(RunInfo info, GodSim sim) { }
        public void OnTick(GodSim sim, IReadOnlyList<SimEvent> events) { Events.AddRange(events); }
        public void OnDayEnd(GodSim sim, FactionDayRow[] rows) { LastRows = rows; Days++; }
        public void End(GodSim sim, IDictionary<string, long> counters) { }
        public void Dispose() { }
    }

    /// <summary>M13a: the SQLite warehouse (DATA_WAREHOUSE_DESIGN 10.1-10.14 for the tables today's events fill).</summary>
    [TestFixture]
    public class WarehouseTests
    {
        private string _dir;

        [SetUp] public void SetUp() { _dir = TestUtil.TempDir("wh"); }
        [TearDown] public void TearDown() { SqliteConnection.ClearAllPools(); TestUtil.Cleanup(_dir); }

        /// <summary>A run that writes <c>warehouse.sqlite</c> into its own folder; the capture recorder holds the event stream.</summary>
        private (RunResult res, string db, CaptureRecorder cap) RunWh(string tag, ulong seed, long ticks, RecordingLevel level, Action<RunSpec> tweak = null, bool fk = true, bool async = true)
        {
            var spec = TestUtil.Spec("watch6", seed, ticks);
            spec.Level = level;
            spec.Sqlite = new WarehouseOptions { ForeignKeys = fk, Async = async };
            spec.RunDir = Path.Combine(_dir, tag); spec.OutRoot = _dir;
            spec.SkipJsonl = true;
            var cap = new CaptureRecorder();
            spec.ExtraRecorders.Add(() => cap);
            tweak?.Invoke(spec);
            var res = RunEngine.Run(spec);
            Assert.IsNull(res.Error, res.Error);
            Assert.IsNotNull(res.WarehousePath, "no warehouse written");
            Assert.IsTrue(File.Exists(res.WarehousePath), res.WarehousePath);
            return (res, res.WarehousePath, cap);
        }

        private static long Q(string db, string sql)
        {
            using (var d = WarehouseDb.OpenRead(db)) return Convert.ToInt64(d.Scalar(sql) ?? 0L);
        }

        // ------------------------------------------------------------------ schema

        [Test]
        public void Schema_IsCreated_AtTheCurrentVersion_WithTheCoreTables()
        {
            var (res, db, _) = RunWh("schema", 1, 2400, RecordingLevel.Summary);
            using (var d = WarehouseDb.OpenRead(db))
            {
                Assert.AreEqual(WarehouseDb.CurrentVersion, d.SchemaVersion());
                // M5: one row per migration that exists (a branch may carry 0006 without M4's 0005, SHARED_CONTRACTS 8)
                Assert.AreEqual(WarehouseDb.Migrations().Count, (long)d.Scalar("SELECT COUNT(*) FROM meta_schema_version"), "one row per applied migration");
                Assert.GreaterOrEqual(WarehouseDb.CurrentVersion, 4, "v4 = the M3 population tables, the M3b decision trace and F0's people and goods seams; later milestones add theirs");
                var tables = d.TableNames();
                foreach (var t in new[] { "dim_run", "dim_date", "dim_faction", "dim_faction_history", "dim_ware", "dim_building", "dim_location", "dim_code", "dim_kpi",
                    "fact_faction_day", "fact_production", "fact_production_day", "fact_combat", "fact_command", "fact_policy_change", "fact_kpi", "fact_event",
                    "fact_run_metric", "fact_metric_day", "dim_person", "fact_death", "fact_person_day", "fact_learning", "fact_decision", "dim_party", "fact_ownership", "fact_social_day", "meta_table_grain", "meta_event_type", "meta_event_map", "meta_load", "meta_schema_version" })
                    CollectionAssert.Contains(tables, t);
                Assert.AreEqual(1, (long)d.Scalar("SELECT COUNT(*) FROM dim_run"));
                Assert.AreEqual(res.RunId, (string)d.Scalar("SELECT run_id FROM dim_run"));
                Assert.AreEqual(Hex.Of(res.FinalHash), (string)d.Scalar("SELECT final_hash FROM dim_run"));
                Assert.AreEqual(res.Ticks, (long)d.Scalar("SELECT final_tick FROM dim_run"));
                Assert.AreEqual("summary", (string)d.Scalar("SELECT recorder_level FROM dim_run"));
                Assert.AreEqual("delete", ((string)d.Scalar("PRAGMA journal_mode")).ToLowerInvariant(), "the finished file is a single self-contained database");
                Assert.IsFalse(File.Exists(db + "-wal"));
                // the stable views answer
                Assert.GreaterOrEqual(Q(db, "SELECT COUNT(*) FROM v_faction_day"), 1);
            }
        }

        [Test]
        public void Schema_RefusesAFileOfANewerVersion()
        {
            var (_, db, _) = RunWh("newer", 1, 1200, RecordingLevel.Metrics);
            using (var c = new SqliteConnection("Data Source=" + db + ";Pooling=False"))
            {
                c.Open();
                using (var cmd = c.CreateCommand()) { cmd.CommandText = "INSERT INTO meta_schema_version VALUES (99, 'x', 'a future version')"; cmd.ExecuteNonQuery(); }
            }
            Assert.Throws<InvalidDataException>(() => WarehouseDb.OpenRead(db));
            Assert.AreEqual(ExitCodes.InvalidInput, TestUtil.Cli(out _, out var err, "export", "--from", db, "--out", Path.Combine(_dir, "x")), err);
        }

        // ------------------------------------------------------------------ 10.1 every event type maps to a table

        /// <summary>Tables a catalogue event may name before its table exists; such an event falls back to fact_event. Empty for today's events.</summary>
        private static readonly HashSet<string> PendingTables = new HashSet<string>();

        [Test]
        public void EveryEventType_MapsToATable()
        {
            var (_, db, _) = RunWh("map", 1, 1200, RecordingLevel.Summary);
            // every DomainEvent subclass in the sim is in the catalogue (reflection), and every catalogue row is in meta_event_map
            var all = typeof(DomainEvent).Assembly.GetTypes().Where(t => typeof(DomainEvent).IsAssignableFrom(t) && !t.IsAbstract).ToList();
            Assert.GreaterOrEqual(all.Count, 40);
            foreach (var t in all) Assert.IsTrue(EventCatalog.TryOf(t, out _), t.Name + " is not in the EventCatalog");
            Assert.AreEqual(EventCatalog.All.Count, EventCatalog.All.Select(e => e.Code).Distinct().Count(), "codes are unique");
            var reg = EventMapRegistry.CreateDefault();
            using (var d = WarehouseDb.OpenRead(db))
            {
                var tables = d.TableNames();
                Assert.AreEqual(EventCatalog.All.Count, d.Count("meta_event_type"));
                Assert.AreEqual(EventCatalog.All.Count, d.Count("meta_event_map"));
                foreach (var e in EventCatalog.All)
                {
                    var m = reg.Resolve(e.Type);
                    CollectionAssert.Contains(tables, m.Table, e.Name + " maps to " + m.Table);
                    if (PendingTables.Contains(e.Table) && reg.IsGeneric(e.Type)) { Assert.AreEqual(EventMapRegistry.GenericTable, m.Table, e.Name); continue; }
                    Assert.AreEqual(m.Table, (string)d.Scalar("SELECT target_table FROM meta_event_map WHERE code = " + e.Code + " AND version = " + e.Version), e.Name);
                    // the catalogue's own table column must agree with the mapper (generic means no claim, analysis-none needs a note)
                    if (e.Table == "analysis-none") Assert.IsFalse(string.IsNullOrEmpty(e.Note), e.Name + ": analysis-none needs a reason");
                    else if (e.Table != EventCatalog.Generic) Assert.AreEqual(e.Table, m.Table, e.Name + ": catalogue table differs from the mapper");
                }
                Assert.AreEqual(0, d.Count("meta_event_map WHERE target_table NOT IN (SELECT name FROM sqlite_master)"));
            }
            // an explicit mapper exists only for events that are in the catalogue
            foreach (var m in reg.Explicit) Assert.IsTrue(EventCatalog.TryOf(m.EventType, out _), m.EventType.Name);
        }

        [Test]
        public void ARegisteredMapper_ChangesTheMapAndTheRows_WithoutTouchingTheRecorder()
        {
            long seen = 0;
            var reg = EventMapRegistry.CreateDefault();
            reg.Register<DiceRolled>("fact_event", "test_dice", (c, e) => { seen++; c.Generic(e); }, "a later milestone registers like this");
            var (_, db, _) = RunWh("reg", 1, 2400, RecordingLevel.Full, s => s.Sqlite.Registry = reg);
            Assert.Greater(seen, 0);
            using (var d = WarehouseDb.OpenRead(db))
                Assert.AreEqual("test_dice", (string)d.Scalar("SELECT mapper FROM meta_event_map m JOIN meta_event_type t ON t.code = m.code WHERE t.name = 'DiceRolled'"));
            Assert.AreEqual(seen, Q(db, "SELECT COUNT(*) FROM fact_event e JOIN meta_event_type t ON t.code = e.event_code WHERE t.name = 'DiceRolled'"));
        }

        // ------------------------------------------------------------------ 10.2 foreign keys

        [Test]
        public void ForeignKeys_Hold_AtFull_AndAtSummary()
        {
            foreach (var level in new[] { RecordingLevel.Full, RecordingLevel.Summary })
            {
                var (res, db, _) = RunWh("fk_" + level, 2, 24000, level, fk: true);
                using (var d = WarehouseDb.OpenRead(db))
                {
                    Assert.AreEqual(0, WarehouseRecorder.ForeignKeyViolations(d), level.ToString());
                    Assert.AreEqual(0, (long)d.Scalar("SELECT value FROM fact_run_metric WHERE metric = 'foreign_key_violations'"));
                    Assert.Greater(d.Count("fact_event"), 100, "events were recorded");
                }
            }
            // production loads run with foreign_keys OFF and check at the end: the same files must pass
            var (_, db2, _) = RunWh("fk_off", 2, 12000, RecordingLevel.Full, fk: false);
            using (var d = WarehouseDb.OpenRead(db2)) Assert.AreEqual(0, WarehouseRecorder.ForeignKeyViolations(d));
        }

        // ------------------------------------------------------------------ 10.3 counts reconcile

        [Test]
        public void Counts_Reconcile_AtFull_ExactlyWithTheEventStreamAndTheMetrics()
        {
            var (res, db, cap) = RunWh("full", 3, 36000, RecordingLevel.Full);
            var byName = cap.Events.GroupBy(e => e.GetType().Name).ToDictionary(g => g.Key, g => (long)g.Count());
            Assert.Greater(byName.Count, 8, "the run produced a variety of events");

            // every event is in fact_event_day exactly, by code; and every event is either a dedicated row or a fact_event row
            foreach (var g in cap.Events.GroupBy(e => EventCatalog.Of(e.GetType()).Code))
                Assert.AreEqual(g.Count(), Q(db, "SELECT COALESCE(SUM(events), 0) FROM fact_event_day WHERE event_code = " + g.Key), "fact_event_day code " + g.Key);
            Assert.AreEqual(cap.Events.Count, Q(db, "SELECT SUM(events) FROM fact_event_day"));
            Assert.AreEqual(res.Counters["events_total"], Q(db, "SELECT SUM(events) FROM fact_event_day"));
            var dedicated = new HashSet<string> { nameof(WaresProduced), nameof(UnitFired), nameof(UnitDied), nameof(BuildingDestroyed), nameof(CommandApplied), nameof(CommandRejected) };
            long expectGeneric = cap.Events.Count(e => !dedicated.Contains(e.GetType().Name)) + byName.GetValueOrDefault(nameof(BuildingDestroyed));
            // BuildingDestroyed is dedicated (fact_combat) and has no fact_event row; HqCaptured etc. have both a dim effect and a row
            expectGeneric -= byName.GetValueOrDefault(nameof(BuildingDestroyed));
            Assert.AreEqual(expectGeneric, Q(db, "SELECT COUNT(*) FROM fact_event"), "fact_event rows");

            // events by type equal the Metrics counters
            foreach (var kv in byName) Assert.AreEqual(kv.Value, res.Counters["events." + kv.Key], kv.Key);
            Assert.AreEqual(res.Counters.GetValueOrDefault("wares_produced"), Q(db, "SELECT COALESCE(SUM(quantity), 0) FROM fact_production"));
            Assert.AreEqual(byName.GetValueOrDefault(nameof(WaresProduced)), Q(db, "SELECT COUNT(*) FROM fact_production"));
            Assert.AreEqual(res.Counters.GetValueOrDefault("wares_produced"), Q(db, "SELECT COALESCE(SUM(quantity), 0) FROM fact_production_day"));
            Assert.AreEqual(res.Counters.GetValueOrDefault("units_died"), Q(db, "SELECT COUNT(*) FROM fact_combat WHERE kind_code = 2"));
            Assert.AreEqual(byName.GetValueOrDefault(nameof(UnitFired)), Q(db, "SELECT COUNT(*) FROM fact_combat WHERE kind_code = 1"));
            Assert.AreEqual(byName.GetValueOrDefault(nameof(BuildingDestroyed)), Q(db, "SELECT COUNT(*) FROM fact_combat WHERE kind_code = 3"));
            Assert.AreEqual(res.Counters.GetValueOrDefault("commands_applied") + res.Counters.GetValueOrDefault("commands_rejected"), Q(db, "SELECT COUNT(*) FROM fact_command"));
            Assert.AreEqual(res.Counters.GetValueOrDefault("buildings_completed"), Q(db, "SELECT COUNT(*) FROM dim_building WHERE completed_tick > 0"));   // the setup HQs are marked completed at tick 0
            Assert.AreEqual(res.Counters.GetValueOrDefault("buildings_completed"), Q(db, "SELECT COUNT(*) FROM fact_event e JOIN meta_event_type t ON t.code = e.event_code WHERE t.name = 'BuildingCompleted'"));
            // goods per ware and faction: the rows equal the aggregate exactly
            Assert.AreEqual(0, Q(db, "SELECT COUNT(*) FROM (SELECT date_key, building_key, ware_key, SUM(quantity) q, COUNT(*) n FROM fact_production GROUP BY 1, 2, 3 " +
                "EXCEPT SELECT date_key, building_key, ware_key, SUM(quantity), SUM(cycles) FROM fact_production_day GROUP BY 1, 2, 3)"));
            // every row carries weight 1 at Full
            Assert.AreEqual(0, Q(db, "SELECT COUNT(*) FROM fact_event WHERE sample_weight <> 1") + Q(db, "SELECT COUNT(*) FROM fact_combat WHERE sample_weight <> 1"));
            // the warehouse's own row counters equal what is in the tables
            foreach (var t in new[] { "fact_event", "fact_production", "fact_combat", "fact_command", "fact_faction_day", "dim_date" })
                Assert.AreEqual(Q(db, "SELECT COUNT(*) FROM " + t), Q(db, "SELECT value FROM fact_run_metric WHERE metric = 'rows_" + t + "'"), t);
            Assert.AreEqual(Q(db, "SELECT COUNT(*) FROM meta_load"), Q(db, "SELECT value FROM fact_run_metric WHERE metric = 'rows_meta_load'") - 0);
        }

        [Test]
        public void Counts_Reconcile_AtSummary_AggregatesExact_SamplesWithinTenPercent()
        {
            var (res, db, cap) = RunWh("summary", 3, 36000, RecordingLevel.Summary, s => s.Scenario.SampleEvery = 16);
            long events = cap.Events.Count;
            Assert.AreEqual(events, Q(db, "SELECT SUM(events) FROM fact_event_day"), "daily event aggregates are exact");
            Assert.AreEqual(res.Counters.GetValueOrDefault("wares_produced"), Q(db, "SELECT SUM(quantity) FROM fact_production_day"), "goods per day are exact");
            Assert.AreEqual(res.Counters.GetValueOrDefault("units_died"), Q(db, "SELECT SUM(events) FROM fact_combat_day WHERE kind_code = 2"));
            // the sample: rows x weight within 10% for every high-volume series with at least 1000 events
            var series = new List<(string what, long exact, long estimate)>
            {
                ("production events", cap.Events.Count(e => e is WaresProduced), Q(db, "SELECT COALESCE(SUM(sample_weight), 0) FROM fact_production")),
                ("attacks", cap.Events.Count(e => e is UnitFired), Q(db, "SELECT COALESCE(SUM(sample_weight), 0) FROM fact_combat WHERE kind_code = 1")),
                ("dice", cap.Events.Count(e => e is DiceRolled), Q(db, "SELECT COALESCE(SUM(sample_weight), 0) FROM fact_event e JOIN meta_event_type t ON t.code = e.event_code WHERE t.name = 'DiceRolled'")),
                ("piles", cap.Events.Count(e => e is PileAdded), Q(db, "SELECT COALESCE(SUM(sample_weight), 0) FROM fact_event e JOIN meta_event_type t ON t.code = e.event_code WHERE t.name = 'PileAdded'")),
            };
            int checkedSeries = 0;
            foreach (var (what, exact, est) in series)
            {
                TestContext.WriteLine(what + ": exact " + exact + ", estimate " + est);
                if (exact < 1000) continue;
                checkedSeries++;
                Assert.LessOrEqual(Math.Abs(est - exact), exact / 10.0, what);
            }
            Assert.GreaterOrEqual(checkedSeries, 1, "at least one series is large enough to test");
            // low-volume events are all there, weight 1
            long lowVolume = cap.Events.Count(e => !EventCatalog.Of(e.GetType()).HighVolume && !(e is CommandApplied) && !(e is CommandRejected) && !(e is BuildingDestroyed) && !(e is UnitDied));
            Assert.AreEqual(lowVolume, Q(db, "SELECT COUNT(*) FROM fact_event e JOIN meta_event_type t ON t.code = e.event_code WHERE t.high_volume = 0"));
        }

        [Test]
        public void Snapshots_Reconcile_WithState_OnTheLastDay()
        {
            var (res, db, cap) = RunWh("snap", 4, 24000, RecordingLevel.Summary);
            var last = cap.LastRows;
            Assert.IsNotNull(last);
            long day = last[0].Day;
            Assert.AreEqual(day + 1, Q(db, "SELECT COUNT(DISTINCT date_key) FROM fact_faction_day"));
            Assert.AreEqual(last.Length, Q(db, "SELECT COUNT(*) FROM fact_faction_day WHERE date_key = " + day));
            var s = res.Sim.State;
            var rows = SnapshotBuilder.FactionDay(s); // the end state is the end of the last day (24000 = 20 days)
            foreach (var r in rows)
            {
                string w = "date_key = " + r.Day + " AND faction_key = (SELECT faction_key FROM dim_faction WHERE faction_id = " + (int)r.Faction + ")";
                Assert.AreEqual(r.VictoryPoints, Q(db, "SELECT victory_points FROM fact_faction_day WHERE " + w));
                Assert.AreEqual(r.Hqs, Q(db, "SELECT hqs FROM fact_faction_day WHERE " + w));
                Assert.AreEqual(r.Units, Q(db, "SELECT units FROM fact_faction_day WHERE " + w));
                Assert.AreEqual(r.StockTotal, Q(db, "SELECT stock_total FROM fact_faction_day WHERE " + w));
                Assert.AreEqual(r.StockTotal, Q(db, "SELECT COALESCE(SUM(quantity), 0) FROM fact_faction_day_stock WHERE " + w));
                Assert.AreEqual(r.Eliminated ? 1 : 0, Q(db, "SELECT eliminated FROM fact_faction_day WHERE " + w));
            }
            // every building row matches the state's territory
            using (var dd = WarehouseDb.OpenRead(db))
            using (var cc = dd.Conn.CreateCommand())
            {
                cc.CommandText = "SELECT building_id, kind, size_code, placed_tick, completed_tick FROM dim_building WHERE is_hq = 0 AND destroyed_tick IS NULL AND removed_tick IS NULL";
                var alive = new List<string>(); var inState = new HashSet<int>(s.Territory.Buildings.Select(b => b.Id));
                using (var r = cc.ExecuteReader()) while (r.Read()) if (!inState.Contains((int)r.GetInt64(0))) alive.Add(r.GetInt64(0) + " " + r.GetValue(1) + " size " + r.GetValue(2) + " placed " + r.GetValue(3) + " done " + r.GetValue(4));
                Assert.IsEmpty(alive, "in the warehouse as alive but not in the state");
            }
            var stateIds = new HashSet<int>(s.Territory.Buildings.Where(b => b.Size != BuildingSize.Hq).Select(b => b.Id));
            using (var dd = WarehouseDb.OpenRead(db))
            using (var cc = dd.Conn.CreateCommand())
            {
                cc.CommandText = "SELECT building_id, kind, size_code FROM dim_building WHERE is_hq = 0 AND destroyed_tick IS NULL AND removed_tick IS NULL";
                var extra = new List<string>();
                using (var r = cc.ExecuteReader()) while (r.Read()) if (!stateIds.Remove((int)r.GetInt64(0))) extra.Add(r.GetInt64(0) + " " + r.GetValue(1) + " " + r.GetValue(2));
                Assert.IsEmpty(extra, "alive in dim_building but not a non-HQ building of the state");
                Assert.IsEmpty(stateIds, "non-HQ buildings of the state missing from dim_building");
            }
            foreach (var b in s.Territory.Buildings.Where(b => b.Size != BuildingSize.Hq))
                Assert.AreEqual(1, Q(db, "SELECT COUNT(*) FROM dim_building d JOIN dim_faction f ON f.faction_key = d.faction_key WHERE d.building_id = " + b.Id + " AND f.faction_id = " + (int)b.Faction), "building " + b.Id + " owner");
        }

        // ------------------------------------------------------------------ 10.5 recording never changes the hash

        [Test]
        public void SqliteRecording_NeverChangesTheHashes_AtAnyLevelDomainOrSampleRate()
        {
            foreach (ulong seed in new ulong[] { 1, 2 })
            {
                var off = TestUtil.Spec("watch6", seed, 12000); off.Level = RecordingLevel.Off;
                var baseline = RunEngine.Run(off);
                var specs = new List<(string, RunSpec)>();
                foreach (var lv in new[] { RecordingLevel.Metrics, RecordingLevel.Summary, RecordingLevel.Full })
                    specs.Add(("level " + lv, Mk(seed, lv, "lv" + lv + seed, s => { })));
                foreach (int every in new[] { 1, 16, 64 })
                    specs.Add(("sampleEvery " + every, Mk(seed, RecordingLevel.Summary, "se" + every + seed, s => s.Scenario.SampleEvery = every)));
                foreach (var dom in Enum.GetValues(typeof(TelemetryDomain)).Cast<TelemetryDomain>().Where(d => (int)d <= (int)TelemetryDomain.State))
                    specs.Add(("domain " + dom, Mk(seed, RecordingLevel.Off, "dm" + dom + seed, s => s.Scenario.RecordDomains[dom.ToString().ToLowerInvariant()] = RecordingLevel.Full)));
                specs.Add(("sync writer", Mk(seed, RecordingLevel.Full, "sync" + seed, s => s.Sqlite.Async = false)));
                var results = TestUtil.RunAll(specs.Select(x => x.Item2).ToList(), 4);
                for (int i = 0; i < specs.Count; i++)
                {
                    Assert.IsNull(results[i].Error);
                    CollectionAssert.AreEqual(TestUtil.Seq(baseline), TestUtil.Seq(results[i]), specs[i].Item1 + " seed " + seed);
                    Assert.AreEqual(baseline.FinalHash, results[i].FinalHash, specs[i].Item1);
                    Assert.IsTrue(File.Exists(results[i].WarehousePath), specs[i].Item1 + ": no database");
                }
            }
        }

        private RunSpec Mk(ulong seed, RecordingLevel lv, string tag, Action<RunSpec> tweak)
        {
            var spec = TestUtil.Spec("watch6", seed, 12000);
            spec.Scenario = spec.Scenario.Clone();
            spec.Level = lv; spec.Sqlite = new WarehouseOptions { ForeignKeys = true }; spec.SkipJsonl = true;
            spec.RunDir = Path.Combine(_dir, tag); spec.OutRoot = _dir;
            tweak(spec);
            return spec;
        }

        [Test]
        public void SqliteRecording_MakesNoSimCalls_AndTheStateAfterEqualsTheStateWithoutIt()
        {
            // the full final state, not only the checkpoint hashes: the saved bytes are equal with and without the warehouse
            var a = TestUtil.Spec("watch6", 5, 9600); a.Level = RecordingLevel.Off; a.KeepSaveBytes = true; a.SaveAtTick = 9600;
            var b = TestUtil.Spec("watch6", 5, 9600); b.Level = RecordingLevel.Full; b.KeepSaveBytes = true; b.SaveAtTick = 9600;
            b.Sqlite = new WarehouseOptions(); b.RunDir = Path.Combine(_dir, "bytes"); b.OutRoot = _dir;
            var ra = RunEngine.Run(a); var rb = RunEngine.Run(b);
            Assert.AreEqual(1, ra.Saves.Count);
            CollectionAssert.AreEqual(ra.Saves[0].bytes, rb.Saves[0].bytes);
        }

        // ------------------------------------------------------------------ 10.8 deterministic content

        [Test]
        public void Warehouse_IsDeterministic_SameSeedSameContentHash()
        {
            var (_, db1, _) = RunWh("det1", 6, 24000, RecordingLevel.Full);
            var (_, db2, _) = RunWh("det2", 6, 24000, RecordingLevel.Full, async: false);
            var (_, db3, _) = RunWh("det3", 7, 24000, RecordingLevel.Full);
            string h1 = WarehouseExport.ContentHash(db1), h2 = WarehouseExport.ContentHash(db2), h3 = WarehouseExport.ContentHash(db3);
            Assert.AreEqual(h1, h2, "same seed, async and sync writers: " + Diff(db1, db2));
            Assert.AreNotEqual(h1, h3, "a different seed gives different content");
            // Summary samples by a hash of seed, tick and seq: the sample is the same every time
            var (_, s1, _) = RunWh("dets1", 6, 24000, RecordingLevel.Summary);
            var (_, s2, _) = RunWh("dets2", 6, 24000, RecordingLevel.Summary);
            Assert.AreEqual(WarehouseExport.ContentHash(s1), WarehouseExport.ContentHash(s2), Diff(s1, s2));
        }

        private static string Diff(string a, string b)
        {
            var x = WarehouseExport.TableHashes(a); var y = WarehouseExport.TableHashes(b);
            return string.Join(",", x.Where(kv => !y.TryGetValue(kv.Key, out var v) || v != kv.Value).Select(kv => kv.Key));
        }

        [Test]
        public void ReRecording_AReplay_GivesTheSameContentAsTheLiveRecord()
        {
            // live: a run with commands on a schedule, recorded at Full, with its replay file
            var live = TestUtil.Spec("watch6", 8, 14400);
            live.Level = RecordingLevel.Full; live.Sqlite = new WarehouseOptions(); live.SkipJsonl = true; live.WriteReplay = true;
            live.RunDir = Path.Combine(_dir, "live"); live.OutRoot = _dir; live.Host = "runner";
            var rl = RunEngine.Run(live);
            Assert.IsNull(rl.Error);
            // re-record: the same run replayed at Off in the first place is the cheap case; here replay it with --record full and the warehouse
            var spec = Cli.ReplaySpec(Path.Combine(rl.RunDir, "replay.godr"), default);
            spec.Level = RecordingLevel.Full; spec.Sqlite = new WarehouseOptions(); spec.SkipJsonl = true;
            spec.RunDir = Path.Combine(_dir, "again"); spec.OutRoot = _dir; spec.WriteReplay = false; spec.GitCommit = live.GitCommit;
            var rr = RunEngine.Run(spec);
            Assert.IsNull(rr.Divergence);
            Assert.AreEqual(rl.FinalHash, rr.FinalHash);
            using (var da = WarehouseDb.OpenRead(rl.WarehousePath)) using (var dbb = WarehouseDb.OpenRead(rr.WarehousePath))
                foreach (var col in new[] { "run_id", "scenario_hash", "git_commit", "host", "recorder_level", "record_domains", "sample_every", "exit_code", "final_tick" })
                    Assert.AreEqual(da.Scalar("SELECT " + col + " FROM dim_run"), dbb.Scalar("SELECT " + col + " FROM dim_run"), "dim_run." + col);
            // dim_run records how the run was stopped ("maxTicks" live, "toTick" in a replay): the one column that names the host's way of stopping
            var ha = WarehouseExport.TableHashes(rl.WarehousePath); var hb = WarehouseExport.TableHashes(rr.WarehousePath);
            ha.Remove("dim_run"); hb.Remove("dim_run");
            CollectionAssert.AreEqual(ha, hb, Diff(rl.WarehousePath, rr.WarehousePath));
        }

        // ------------------------------------------------------------------ KPI reconciliation: SQL equals the in-memory metric exactly

        [Test]
        public void Kpis_FromSql_EqualTheInMemoryMetrics_Exactly()
        {
            var (res, db, cap) = RunWh("kpi", 3, 36000, RecordingLevel.Summary);
            // KPI 1: goods produced per faction per day. Expected values from the raw event stream, grouped in C#.
            var cal = new SimCalendar(1200, 10, 100);
            var expected = new SortedDictionary<(long day, int faction, int ware), long>();
            foreach (var e in cap.Events.OfType<WaresProduced>())
            {
                var k = ((e.Tick - 1) / 1200, (int)e.Faction, (int)e.Ware);
                expected.TryGetValue(k, out long v); expected[k] = v + e.Count;
            }
            Assert.Greater(expected.Count, 10);
            var actual = new SortedDictionary<(long, int, int), long>();
            using (var d = WarehouseDb.OpenRead(db))
            using (var c = d.Conn.CreateCommand())
            {
                c.CommandText = "SELECT p.date_key, f.faction_id, p.ware_key, SUM(p.quantity) FROM fact_production_day p JOIN dim_faction f ON f.faction_key = p.faction_key GROUP BY 1, 2, 3";
                using (var r = c.ExecuteReader()) while (r.Read()) actual[(r.GetInt64(0), (int)r.GetInt64(1), (int)r.GetInt64(2))] = r.GetInt64(3);
            }
            CollectionAssert.AreEqual(expected, actual, "goods produced per faction, ware and day");
            // and the grand total is the Metrics counter
            Assert.AreEqual(res.Counters["wares_produced"], actual.Values.Sum());
            // the last day's fact_metric_day equals the in-memory metrics of that day
            foreach (var kv in res.FinalMetrics)
                Assert.AreEqual(kv.Value, Q(db, "SELECT value FROM fact_metric_day WHERE metric = '" + kv.Key + "' AND date_key = (SELECT MAX(date_key) FROM fact_metric_day)"), kv.Key);

            // KPI 2: soldiers lost in combat (not disbanded), per faction, and in total, against the unit events
            var died = cap.Events.OfType<UnitDied>().Where(u => !u.Disbanded).GroupBy(u => (int)u.Faction).ToDictionary(g => g.Key, g => (long)g.Count());
            foreach (var f in Enumerable.Range(0, 6))
                Assert.AreEqual(died.GetValueOrDefault(f), Q(db, "SELECT COALESCE(SUM(c.events), 0) FROM fact_combat_day c JOIN dim_faction x ON x.faction_key = c.faction_key WHERE c.kind_code = 2 AND c.outcome_code = 0 AND x.faction_id = " + f), "deaths in combat, faction " + f);
            Assert.AreEqual(res.Counters.GetValueOrDefault("units_died"), Q(db, "SELECT COALESCE(SUM(events), 0) FROM fact_combat_day WHERE kind_code = 2"));

            // KPI 3: buildings completed per faction, against the events
            var done = cap.Events.OfType<BuildingCompleted>().GroupBy(b => (int)b.Faction).ToDictionary(g => g.Key, g => (long)g.Count());
            Assert.AreEqual(res.Counters.GetValueOrDefault("buildings_completed"), done.Values.Sum());
            long fromSql = 0;
            foreach (var f in Enumerable.Range(0, 6))
            {
                long n = Q(db, "SELECT COUNT(*) FROM fact_event e JOIN meta_event_type t ON t.code = e.event_code JOIN dim_faction x ON x.faction_key = e.faction_key WHERE t.name = 'BuildingCompleted' AND x.faction_id = " + f);
                Assert.AreEqual(done.GetValueOrDefault(f), n, "buildings completed, faction " + f);
                fromSql += n;
            }
            Assert.AreEqual(res.Counters.GetValueOrDefault("buildings_completed"), fromSql);
            TestContext.WriteLine("KPI reconciliation: " + expected.Count + " (day, faction, ware) cells, " + actual.Values.Sum() + " goods, "
                + died.Values.Sum() + " combat deaths, " + fromSql + " buildings completed: SQL equals memory.");
        }

        // ------------------------------------------------------------------ levels

        [Test]
        public void Levels_WriteWhatTheyPromise()
        {
            var (_, m, _) = RunWh("lv_metrics", 1, 12000, RecordingLevel.Metrics);
            Assert.AreEqual(0, Q(m, "SELECT COUNT(*) FROM fact_event") + Q(m, "SELECT COUNT(*) FROM fact_faction_day") + Q(m, "SELECT COUNT(*) FROM fact_production"));
            Assert.AreEqual(10, Q(m, "SELECT COUNT(DISTINCT date_key) FROM fact_metric_day"));
            Assert.Greater(Q(m, "SELECT COUNT(*) FROM fact_run_metric"), 10);
            var (_, f, _) = RunWh("lv_full", 1, 12000, RecordingLevel.Full);
            Assert.Greater(Q(f, "SELECT COUNT(*) FROM fact_event"), 0);
            // a domain switched off records none of its events even at Full
            var (_, o, _) = RunWh("lv_off", 1, 12000, RecordingLevel.Full, s => s.Scenario.RecordDomains["production"] = RecordingLevel.Off);
            Assert.AreEqual(0, Q(o, "SELECT COUNT(*) FROM fact_production") + Q(o, "SELECT COUNT(*) FROM fact_production_day"));
            Assert.Greater(Q(o, "SELECT COUNT(*) FROM fact_event"), 0);
        }

        // ------------------------------------------------------------------ example queries

        [Test]
        public void ExampleQueries_Run_AndAnswer()
        {
            var (_, db, _) = RunWh("sql", 3, 36000, RecordingLevel.Summary);
            string sql = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Warehouse", "ExampleQueries.sql"));
            var text = string.Join("\n", sql.Split('\n').Where(l => !l.TrimStart().StartsWith("--", StringComparison.Ordinal)));
            var stmts = text.Split(';').Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
            Assert.GreaterOrEqual(stmts.Count, 10);
            int nonEmpty = 0;
            using (var d = WarehouseDb.OpenRead(db))
                foreach (var st in stmts)
                    using (var c = d.Conn.CreateCommand())
                    {
                        c.CommandText = st;
                        int rows = 0;
                        using (var r = c.ExecuteReader()) while (r.Read()) rows++;
                        if (rows > 0) nonEmpty++;
                    }
            Assert.GreaterOrEqual(nonEmpty, 10, "most example queries return rows on today's data");
        }

        // ------------------------------------------------------------------ runner: batch, export, CLI

        [Test]
        public void Cli_RunWithSqlite_WritesTheWarehouse_AndExportWritesCsvAndParquet()
        {
            string o = Path.Combine(_dir, "cli");
            int code = TestUtil.Cli(out var so, out var se, "run", "--scenario", "watch6", "--seed", "2", "--ticks", "7200", "--record", "summary", "--sqlite", "--no-jsonl", "--out", o, "--quiet");
            Assert.AreNotEqual(ExitCodes.Usage, code, so + se);
            var dbs = Directory.GetFiles(o, "warehouse.sqlite", SearchOption.AllDirectories);
            Assert.AreEqual(1, dbs.Length);
            Assert.AreEqual(0, Directory.GetFiles(o, "events.jsonl.gz", SearchOption.AllDirectories).Length, "--no-jsonl");

            // CSV
            string csv = Path.Combine(_dir, "csv");
            Assert.AreEqual(0, TestUtil.Cli(out so, out se, "export", "--from", Path.GetDirectoryName(dbs[0]), "--to", "csv", "--out", csv), se);
            using (var d = WarehouseDb.OpenRead(dbs[0]))
                foreach (var t in new[] { "fact_event", "fact_production_day", "dim_faction", "dim_run" })
                {
                    var lines = File.ReadAllLines(Path.Combine(csv, t + ".csv"));
                    Assert.AreEqual(d.Count(t) + 1, lines.Length, t + " rows + header");
                    Assert.AreEqual(string.Join(",", WarehouseExport.Columns(d, t).Select(c => c.Name)), lines[0]);
                }

            // Parquet round trip: row counts and the sum of every integer column equal SQLite
            string lake = Path.Combine(_dir, "lake");
            Assert.AreEqual(0, TestUtil.Cli(out so, out se, "export", "--from", dbs[0], "--to", "parquet", "--out", lake), se);
            using (var d = WarehouseDb.OpenRead(dbs[0]))
            {
                int compared = 0;
                foreach (var t in d.TableNames())
                {
                    var files = Directory.GetFiles(Path.Combine(lake, WarehouseExport.GlobalTables.Contains(t) ? Path.Combine("_global", t) : t), "*.parquet", SearchOption.AllDirectories);
                    Assert.AreEqual(1, files.Length, t);
                    if (!WarehouseExport.GlobalTables.Contains(t)) StringAssert.Contains("experiment=adhoc", files[0]);
                    var (rows, sums) = WarehouseExport.ReadParquetTotals(files[0]);
                    Assert.AreEqual(d.Count(t), rows, t + " row count");
                    foreach (var c in WarehouseExport.Columns(d, t).Where(x => x.IsInteger))
                    {
                        long sql = Convert.ToInt64(d.Scalar("SELECT COALESCE(SUM(" + c.Name + "), 0) FROM " + t) ?? 0L);
                        Assert.AreEqual(sql, sums.GetValueOrDefault(c.Name), t + "." + c.Name);
                    }
                    compared++;
                }
                Assert.Greater(compared, 20);
            }
            Assert.AreEqual(ExitCodes.InvalidInput, TestUtil.Cli(out _, out se, "export", "--from", Path.Combine(_dir, "nothing"), "--out", csv));
            Assert.AreEqual(ExitCodes.NotImplemented, TestUtil.Cli(out _, out se, "export", "--from", dbs[0], "--to", "sqlite", "--out", csv));
            Assert.AreEqual(ExitCodes.Usage, TestUtil.Cli(out _, out se, "run", "--scenario", "watch6", "--ticks", "100", "--sqlite", "--no-out"));
        }

        [Test]
        public void Batch_WritesOneWarehousePerRun_AndABatchIndex()
        {
            var spec = TestUtil.Spec("watch6", 1, 6000);
            spec.Level = RecordingLevel.Summary; spec.Sqlite = new WarehouseOptions(); spec.SkipJsonl = true; spec.OutRoot = _dir; spec.Group = "b";
            var items = Batch.Run(spec, new ulong[] { 3, 1, 2 }, 3, false, TextWriter.Null, out int code);
            Assert.AreEqual(3, items.Count);
            foreach (var it in items) Assert.IsTrue(File.Exists(Path.Combine(it.RunDir, "warehouse.sqlite")), it.RunDir);
            string idx = Path.Combine(_dir, "b", "watch6", "batch.sqlite");
            Assert.IsTrue(File.Exists(idx));
            using (var d = WarehouseDb.OpenRead(idx))
            {
                Assert.AreEqual(3, d.Count("dim_run"));
                // in the order of the seeds as given, never the order the runs finished
                Assert.AreEqual("3,1,2", string.Join(",", Enumerable.Range(1, 3).Select(i => d.Scalar("SELECT seed FROM dim_run WHERE run_key = " + i))));
                Assert.AreEqual(3, (long)d.Scalar("SELECT COUNT(*) FROM fact_run_metric WHERE metric = 'ticks'"));
            }
        }

        // ------------------------------------------------------------------ the metadata the recorder writes

        [Test]
        public void Dimensions_AreFilled_AndFactionHistoryIsHalfOpen()
        {
            var (_, db, _) = RunWh("dims", 1, 36000, RecordingLevel.Summary);
            Assert.AreEqual(6, Q(db, "SELECT COUNT(*) FROM dim_faction"));
            Assert.AreEqual(Wares.PersonalCount, Q(db, "SELECT COUNT(*) FROM dim_ware"), "every ware id, the personal wares of M3 included");
            Assert.AreEqual(Enum.GetValues(typeof(Ware)).Length, Q(db, "SELECT COUNT(*) FROM dim_ware"));
            Assert.Greater(Q(db, "SELECT COUNT(*) FROM dim_location WHERE kind = 'hex'"), 5);
            Assert.Greater(Q(db, "SELECT COUNT(*) FROM dim_building WHERE is_hq = 1"), 5);
            Assert.AreEqual((Q(db, "SELECT final_tick FROM dim_run") + 1199) / 1200, Q(db, "SELECT COUNT(*) FROM dim_date"));
            Assert.GreaterOrEqual(Q(db, "SELECT COUNT(*) FROM dim_date WHERE is_year_start = 1"), 1);
            // spells: one open row per faction; closed rows end where the next begins; no overlap
            Assert.AreEqual(Q(db, "SELECT COUNT(*) FROM dim_faction"), Q(db, "SELECT COUNT(*) FROM dim_faction_history WHERE is_current = 1"));
            Assert.AreEqual(0, Q(db, "SELECT COUNT(*) FROM dim_faction_history a JOIN dim_faction_history b ON a.faction_key = b.faction_key AND a.faction_hist_key < b.faction_hist_key " +
                "WHERE a.valid_to_tick IS NULL OR a.valid_to_tick > b.valid_from_tick OR a.valid_to_tick < a.valid_from_tick"));
            // day columns agree with ticks through dim_date
            Assert.AreEqual(0, Q(db, "SELECT COUNT(*) FROM fact_event e JOIN dim_date d ON d.date_key = e.date_key WHERE e.tick < d.tick_start OR e.tick > d.tick_end"));
            Assert.AreEqual(0, Q(db, "SELECT COUNT(*) FROM fact_production e JOIN dim_date d ON d.date_key = e.date_key WHERE e.tick < d.tick_start OR e.tick > d.tick_end"));
            // one transaction per sim day: meta_load has one row per committed day plus the first batch and the end
            Assert.GreaterOrEqual(Q(db, "SELECT COUNT(*) FROM meta_load"), Q(db, "SELECT COUNT(*) FROM dim_date"));
        }

        [Test]
        public void FactionElimination_ClosesTheSpell_AndOpensAnEliminatedOne()
        {
            // a long sandbox-less game on the default victory rules: find a seed where somebody falls within the budget
            for (ulong seed = 1; seed <= 12; seed++)
            {
                var (res, db, cap) = RunWh("elim" + seed, seed, 60000, RecordingLevel.Summary);
                if (!cap.Events.OfType<FactionEliminated>().Any()) continue;
                foreach (var e in cap.Events.OfType<FactionEliminated>())
                {
                    string f = "(SELECT faction_key FROM dim_faction WHERE faction_id = " + (int)e.Faction + ")";
                    Assert.AreEqual(2, Q(db, "SELECT COUNT(*) FROM dim_faction_history WHERE faction_key = " + f));
                    Assert.AreEqual(e.Tick, Q(db, "SELECT valid_to_tick FROM dim_faction_history WHERE is_current = 0 AND faction_key = " + f));
                    Assert.AreEqual(e.Tick, Q(db, "SELECT valid_from_tick FROM dim_faction_history WHERE is_current = 1 AND eliminated = 1 AND faction_key = " + f));
                }
                return;
            }
            Assert.Inconclusive("no faction was eliminated on seeds 1-12 within 60000 ticks");
        }

        [Test]
        public void SimAndRecordingAssemblies_DoNotReferenceSqlite()
        {
            // DATA_WAREHOUSE 10.13 / G4.10: the SQLite writer lives only in the runner; Unity's God.Sim and God.Recording never see it
            foreach (var asm in new[] { typeof(GodSim).Assembly, typeof(RunInfo).Assembly })
            {
                var names = asm.GetReferencedAssemblies().Select(a => a.Name).ToList();
                CollectionAssert.DoesNotContain(names, "Microsoft.Data.Sqlite", asm.GetName().Name);
                CollectionAssert.DoesNotContain(names, "Parquet", asm.GetName().Name);
                Assert.IsFalse(names.Any(n => n.StartsWith("SQLitePCL", StringComparison.Ordinal)), asm.GetName().Name);
            }
        }

        // ------------------------------------------------------------------ the bench

        [Test]
        public void RecordingBench_Runs_AndAllModesGiveOneHash()
        {
            var sc = Scenario.Parse(File.ReadAllText(TestUtil.ScenarioPath("bench_stage3_watch6")), new List<string>());
            var o = RecordingBench.Run(sc, 2400, 1, TextWriter.Null);
            Assert.IsTrue((bool)o["allHashesEqual"], o.ToString());
            Assert.AreEqual(8, o["modes"].Count());
            TestContext.WriteLine(o.ToString());
        }
    }
}
