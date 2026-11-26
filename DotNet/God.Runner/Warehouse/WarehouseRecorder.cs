using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using God.Recording;
using God.Sim;
using Microsoft.Data.Sqlite;

namespace God.Runner.Warehouse
{
    /// <summary>Host settings for the SQLite warehouse of one run (HEADLESS_SIMULATION: <c>warehouse.sqlite</c> in the run directory).</summary>
    public sealed class WarehouseOptions
    {
        /// <summary>The database file. Null: <c>warehouse.sqlite</c> in the run directory.</summary>
        public string Path;
        /// <summary>PRAGMA foreign_keys=ON while loading (tests). Production loads run with it off and check at the end.</summary>
        public bool ForeignKeys;
        /// <summary>Write on a separate thread through a bounded queue (the default); false writes on the sim thread.</summary>
        public bool Async = true;
        public int QueueDays = 8;
        /// <summary>Create the indexes and run ANALYZE when the run ends (the default).</summary>
        public bool IndexAtEnd = true;
        /// <summary>The event to table mapping; null uses <see cref="EventMapRegistry.CreateDefault"/>.</summary>
        public EventMapRegistry Registry;

        public WarehouseOptions Clone() => (WarehouseOptions)MemberwiseClone();
    }

    /// <summary>
    /// The SQLite recorder (DATA_WAREHOUSE_DESIGN 4.2): maps drained domain events and the daily faction snapshot to
    /// dimensional rows, assigns surrogate keys in order of first appearance (deterministic), and hands one batch per sim day
    /// to the writer, which commits it as one transaction. It only reads events and snapshot rows: it never touches the sim,
    /// never draws from an Rng and keeps no wall-clock value in any column except the ones the content hash excludes.
    /// Levels: Metrics writes dim_run, dim_date, fact_metric_day and fact_run_metric; Summary adds every dimension, the daily
    /// snapshot, the low-volume events and, for high-volume ones, daily aggregates plus a 1-in-sampleEvery sample; Full writes
    /// every event. Per-domain switches (<see cref="RunInfo.Domains"/>) override the level for the events of one domain.
    /// </summary>
    public sealed class WarehouseRecorder : IRunRecorder
    {
        private readonly WarehouseOptions _opt;
        private readonly Scenario _scenario;
        private readonly int _replicate;
        private readonly string _git, _host;
        private readonly MetricsRecorder _metrics;
        private readonly EventMapRegistry _registry;
        private readonly DateTime _wallStart = DateTime.UtcNow;

        private RunInfo _info;
        private WarehouseDb _db;
        private BatchWriter _writer;
        private DayBatch _batch = new DayBatch();
        private MapContext _ctx;
        private SimCalendar _cal;
        private TerritoryState _territory;
        private Board _board;
        private int _factionCount;
        private bool _ended, _finished;
        private int _nextHistKey = 1;

        internal readonly PayloadJson Json = new PayloadJson();
        private readonly HashSet<long> _dates = new HashSet<long>();
        private readonly Dictionary<int, long> _factionKeys = new Dictionary<int, long>();
        private readonly Dictionary<int, long> _factionHist = new Dictionary<int, long>();   // faction id -> open history row key
        private readonly Dictionary<int, long> _factionHistFrom = new Dictionary<int, long>();
        private readonly Dictionary<int, string> _controller = new Dictionary<int, string>();
        private readonly Dictionary<int, BuildingDim> _buildings = new Dictionary<int, BuildingDim>();
        private readonly Dictionary<long, long> _locations = new Dictionary<long, long>();    // (kind<<32 | idx) -> location key
        private readonly Dictionary<int, PersonDim> _persons = new Dictionary<int, PersonDim>();
        private readonly Dictionary<long, long> _parties = new Dictionary<long, long>();     // (kind<<32 | id) -> party key
        private long _nextParty = 1;
        private long _nextLocation = 1, _nextBuilding = 1, _nextPerson = 1;
        private readonly SortedDictionary<string, Aggregate> _aggs = new SortedDictionary<string, Aggregate>(StringComparer.Ordinal);
        private readonly Dictionary<Type, Plan> _plans = new Dictionary<Type, Plan>();
        private readonly SortedDictionary<string, long> _sampledOut = new SortedDictionary<string, long>(StringComparer.Ordinal);

        private sealed class Aggregate { public TableDef Table; public object[] Key, Attrs; public long[] Sums; }
        private sealed class Plan { public EventMapping Map; public EventInfo Info; public RecordingLevel Level; public string Domain; }

        public WarehouseRecorder(WarehouseOptions options, Scenario scenario, int replicate, string git, string host, MetricsRecorder metrics)
        {
            _opt = options; _scenario = scenario; _replicate = replicate; _git = git ?? "unknown"; _host = host ?? "runner"; _metrics = metrics;
            _registry = options.Registry ?? EventMapRegistry.CreateDefault();
        }

        public string DatabasePath => _opt.Path;
        public MapContext Context => _ctx;
        internal SimCalendar Calendar => _cal;

        // ---------------- begin ----------------

        public void Begin(RunInfo info, GodSim sim)
        {
            _info = info;
            if (_opt.Path == null) throw new InvalidOperationException("the warehouse needs a database path");
            var s = sim.State;
            _cal = SimCalendar.Of(s);
            _territory = s.Territory; _board = s.Board; _factionCount = s.FactionCount;
            _ctx = new MapContext(this);
            _db = WarehouseDb.Create(_opt.Path, _opt.ForeignKeys);
            _writer = new BatchWriter(_db, _opt.Async, _opt.QueueDays);

            var setup = s.Setup;
            string victory = setup?.Victory != null ? string.Join(",", setup.Victory.Select(v => v.Kind.ToString())) : "";
            using (var c = _db.Conn.CreateCommand())
            {
                c.CommandText = "INSERT INTO dim_run (run_key, run_id, scenario_name, scenario_hash, seed, replicate, git_commit, sim_version, save_version, schema_version, " +
                    "life_scale, ticks_per_day, days_per_year, faction_count, victory, recorder_level, record_domains, sample_every, host) " +
                    "VALUES (1, @id, @sn, @sh, @seed, @rep, @git, @sv, @save, @schema, @ls, @tpd, @dpy, @fc, @vic, @lvl, @dom, @se, @host)";
                void P(string n, object v) => c.Parameters.AddWithValue(n, v ?? DBNull.Value);
                P("@id", info.RunId); P("@sn", info.ScenarioName); P("@sh", Hex.Of(_scenario.Hash())); P("@seed", (long)info.Seed); P("@rep", (long)_replicate);
                P("@git", _git); P("@sv", ReplayHeader.SimVersion); P("@save", (long)SaveFile.CurrentVersion); P("@schema", (long)WarehouseDb.CurrentVersion);
                P("@ls", (long)_cal.LifeScale); P("@tpd", (long)_cal.TicksPerDay); P("@dpy", (long)_cal.DaysPerYear); P("@fc", (long)_factionCount); P("@vic", victory);
                P("@lvl", RecordingLevels.Name(info.Level));
                P("@dom", string.Join(",", info.Domains.OrderBy(k => k.Key, StringComparer.Ordinal).Select(k => k.Key + "=" + RecordingLevels.Name(k.Value))));
                P("@se", (long)info.SampleEvery); P("@host", _host);
                c.ExecuteNonQuery();
            }

            // the first batch: metadata, global dimensions and the dimensions of this run
            foreach (var e in EventCatalog.All.OrderBy(x => x.Code))
            {
                var m = _registry.Resolve(e.Type);
                AddRow(Tables.MetaEventType, (long)e.Code, (long)e.Version, e.Name, e.Domain.ToString().ToLowerInvariant(), e.Table == "analysis-none" ? "analysis-none" : "active", e.HighVolume ? 1L : 0L);
                AddRow(Tables.MetaEventMap, (long)e.Code, (long)e.Version, m.Table, m.Mapper, m.Note ?? e.Note);
            }
            foreach (var g in PopulationMapping.Grains.Concat(HouseholdMappings.Grains).Concat(SocialMappings.Grains)) AddRow(Tables.MetaTableGrain, g.table, g.grain, g.events);
            WriteCodes();
            foreach (Ware w in Enum.GetValues(typeof(Ware)).Cast<Ware>().OrderBy(x => (int)x))
                AddRow(Tables.DimWare, (long)(int)w, Wares.Name(w), WareCategory(w), Wares.IsRaw(w) || w == Ware.Rock ? 1L : 0L);
            string controller = "ai";
            for (int i = 0; i < _factionCount; i++)
            {
                long key = i + 1; _factionKeys[i] = key;
                bool human = setup != null && setup.Human == i;
                AddRow(Tables.DimFaction, key, 1L, (long)i, FactionInfo.Name((Faction)i), (long)FactionInfo.ColourRgb((Faction)i), human ? 1L : 0L);
                long hk = _nextHistKey++; _factionHist[i] = hk; _factionHistFrom[i] = 0;
                _controller[i] = human ? "human" : controller;
                AddRow(Tables.DimFactionHistory, hk, key, 0L, null, 0L, null, 1L, _controller[i], 0L, "start");
            }
            if (_board != null && _board.HexTerrain != null)
            {
                var topo = _board.Topology;
                for (int h = 0; h < _board.HexTerrain.Length; h++)
                {
                    var hc = topo.Hex(h);
                    long lk = _nextLocation++; _locations[Loc(0, h)] = lk;
                    AddRow(Tables.DimLocation, lk, 1L, "hex", (long)h, (long)h, _board.HexTerrain[h].ToString(), (long)_board.HexNumber[h], (long)hc.Q, (long)hc.R, null, null);
                }
            }
            // what is already on the board when recording starts (the setup's HQs emit no event; a run from a save starts mid-game)
            if (_territory != null)
            {
                long now = s.Tick;
                foreach (var h in _territory.Hqs)
                {
                    var b = BuildingOf(h.Id); b.IsHq = true; b.Kind = "hq"; b.SizeCode = (int)BuildingSize.Hq; b.SetFaction(FactionKeyOf((int)h.Faction), true);
                    b.NodeLocation = NodeLocation(h.Node); b.HexLocation = HexLocationOfNode(h.Node); b.PlacedTick = now; if (!h.Site) b.CompletedTick = now;
                }
                foreach (var tb in _territory.Buildings)
                {
                    if (_buildings.ContainsKey(tb.Id)) continue;
                    var b = BuildingOf(tb.Id); b.IsHq = tb.Size == BuildingSize.Hq; b.Kind = tb.Kind; b.SizeCode = (int)tb.Size; b.SetFaction(FactionKeyOf((int)tb.Faction), true);
                    b.NodeLocation = NodeLocation(tb.Node); b.HexLocation = HexLocationOfNode(tb.Node); b.PlacedTick = now;
                }
                CommitDay();
            }
            else FlushBatch(true);
        }

        private static string WareCategory(Ware w) =>
            Wares.IsRaw(w) || w == Ware.Rock ? "raw" : Wares.IsProcessed(w) ? "processed" : Wares.IsGear(w) ? "gear" : w == Ware.None ? "none" : "other";

        private void WriteCodes()
        {
            void Enum<T>(string set) where T : struct, System.Enum
            {
                foreach (var v in System.Enum.GetValues(typeof(T)).Cast<T>().OrderBy(x => Convert.ToInt32(x)))
                    AddRow(Tables.DimCode, set, (long)Convert.ToInt32(v), v.ToString(), null);
            }
            Enum<UnitKind>("unit_kind"); Enum<AttackOutcome>("attack_outcome"); Enum<TargetKind>("target_kind"); Enum<BuildingSize>("building_size");
            Enum<GameEndReason>("game_end_reason"); Enum<RefusalCause>("refusal_cause"); Enum<EnvoyDeathCause>("envoy_death_cause"); Enum<Terrain>("terrain");
            Enum<God.Sim.Population.DeathCause>("death_cause"); Enum<God.Sim.Population.ProduceReason>("produce_reason"); Enum<God.Sim.Population.ConsumeReason>("consume_reason");
            Enum<God.Sim.Population.OwnerKind>("owner_kind"); Enum<God.Sim.Population.OwnershipBasis>("ownership_basis"); Enum<God.Sim.Population.MoveReason>("move_reason");
            Enum<God.Sim.Population.KinBand>("kin_band"); Enum<God.Sim.LifeStage>("life_stage");
            Enum<God.Sim.Population.LearningModeCode>("learning_mode"); Enum<God.Sim.Learning.Origin>("learning_origin");
            for (int a = 0; a < God.Sim.Population.Activity.Count; a++) AddRow(Tables.DimCode, "activity", (long)a, God.Sim.Population.Activity.Names[a], "one of the eleven ticks_* columns of fact_person_day, in order");
            AddRow(Tables.DimCode, "learning_kind", 1L, "association_updated", "AssociationUpdated (sampled below Full)");
            AddRow(Tables.DimCode, "learning_kind", 2L, "conjunction_recruited", "ConjunctionRecruited");
            AddRow(Tables.DimCode, "learning_kind", 3L, "conjunction_freed", "ConjunctionFreed");
            AddRow(Tables.DimCode, "learning_kind", 4L, "episode_closed", "EpisodeClosed");
            AddRow(Tables.DimCode, "combat_kind", 1L, "attack", "one swing or shot (UnitFired); outcome_code = attack_outcome");
            AddRow(Tables.DimCode, "combat_kind", 2L, "unit_died", "a soldier is gone (UnitDied); outcome_code 0 killed, 1 disbanded with its eliminated faction");
            AddRow(Tables.DimCode, "combat_kind", 3L, "building_destroyed", "a building fell (BuildingDestroyed)");
        }

        // ---------------- keys and dimension rows used by mappers ----------------

        internal long DayOf(long tick) => tick <= 0 ? 0 : (tick - 1) / _cal.TicksPerDay;

        internal long EnsureDate(long day)
        {
            if (_dates.Add(day))
            {
                long start = day * _cal.TicksPerDay + 1, end = (day + 1) * _cal.TicksPerDay;
                AddRow(Tables.DimDate, day, 1L, day / _cal.DaysPerYear, day % _cal.DaysPerYear, start, end, day % _cal.DaysPerYear == 0 ? 1L : 0L);
            }
            if (day < _batch.FirstDay) _batch.FirstDay = day;
            if (day > _batch.LastDay) _batch.LastDay = day;
            return day;
        }

        internal long? FactionKeyOf(int id) => _factionKeys.TryGetValue(id, out long k) ? k : (long?)null;

        internal BuildingDim BuildingOf(int id)
        {
            if (_buildings.TryGetValue(id, out var b)) return b;
            b = new BuildingDim { Key = _nextBuilding++, Id = id, Dirty = true };
            _buildings[id] = b;
            return b;
        }

        /// <summary>The dim_party row of an owner (F0.14), written once on first sight; null for no owner. Person and estate parties point at the person.</summary>
        internal long? PartyOf(God.Sim.Population.OwnerKind kind, int id)
        {
            if (kind == God.Sim.Population.OwnerKind.None) return null;
            long k = ((long)(int)kind << 32) | (uint)id;
            if (_parties.TryGetValue(k, out long key)) return key;
            key = _nextParty++; _parties[k] = key;
            bool person = kind == God.Sim.Population.OwnerKind.Npc || kind == God.Sim.Population.OwnerKind.Estate;
            object personKey = person ? (object)PersonOf(id).Key : null;
            object household = kind == God.Sim.Population.OwnerKind.Household ? (object)(long)id : null;
            object faction = kind == God.Sim.Population.OwnerKind.State || kind == God.Sim.Population.OwnerKind.Group ? FactionKeyOf(id) : null;
            AddRow(Tables.DimParty, key, 1L, (long)(int)kind, (long)id, personKey, household, faction);
            return key;
        }

        internal PersonDim PersonOf(int id)
        {
            if (_persons.TryGetValue(id, out var p)) return p;
            p = new PersonDim { Key = _nextPerson++, NpcId = id, Dirty = true };
            _persons[id] = p;
            return p;
        }

        private static long Loc(int kind, int idx) => ((long)kind << 32) | (uint)idx;

        internal long? HexLocation(int hex) => _locations.TryGetValue(Loc(0, hex), out long k) ? k : (long?)null;

        internal long? HexLocationOfNode(int node)
        {
            if (_territory == null || node < 0 || node >= _territory.Grid.Count) return null;
            int hex = _territory.Grid.HexOf(node);
            return hex >= 0 ? HexLocation(hex) : null;
        }

        internal long NodeLocation(int node)
        {
            if (_locations.TryGetValue(Loc(1, node), out long k)) return k;
            k = _nextLocation++; _locations[Loc(1, node)] = k;
            var g = _territory.Grid;
            bool ok = node >= 0 && node < g.Count;
            AddRow(Tables.DimLocation, k, 1L, "node", (long)node, ok && g.HexOf(node) >= 0 ? (long)g.HexOf(node) : (object)null, null, null, null, null,
                ok ? (long)g.I(node) : (object)null, ok ? (long)g.J(node) : (object)null);
            return k;
        }

        internal void EliminateFaction(int id, long tick)
        {
            if (!_factionHist.TryGetValue(id, out long open)) return;
            long day = DayOf(tick);
            if (_factionHistFrom[id] == tick)
            {
                // a zero-length spell is dropped: the open row becomes the eliminated one (counted in meta_load)
                AddRow(Tables.DimFactionHistoryEliminateInPlace, open, "eliminated");
                _batch.DroppedZeroSpells++;
                return;
            }
            AddRow(Tables.DimFactionHistoryClose, open, tick, day);
            long hk = _nextHistKey++;
            _factionHist[id] = hk; _factionHistFrom[id] = tick;
            AddRow(Tables.DimFactionHistory, hk, _factionKeys[id], tick, null, day, null, 1L, _controller[id], 1L, "eliminated");
        }

        // ---------------- rows ----------------

        internal void AddRow(TableDef t, params object[] values) => _batch.Ops.Add(new Op { Table = t, Values = values });

        internal void AddAggregate(TableDef t, object[] key, object[] attrs, long[] sums)
        {
            string k = t.Name + "|" + string.Join("|", key.Select(x => x is long l ? l.ToString("D14") : Convert.ToString(x)));
            if (_aggs.TryGetValue(k, out var a)) { for (int i = 0; i < sums.Length; i++) a.Sums[i] += sums[i]; if (attrs != null) a.Attrs = attrs; return; }
            _aggs[k] = new Aggregate { Table = t, Key = key, Attrs = attrs ?? Array.Empty<object>(), Sums = (long[])sums.Clone() };
        }

        private void FlushAggregates()
        {
            foreach (var a in _aggs.Values)
            {
                var v = new List<object> { 1L };
                v.AddRange(a.Key); v.AddRange(a.Attrs); foreach (var s in a.Sums) v.Add(s);
                AddRow(a.Table, v.ToArray());
            }
            _aggs.Clear();
        }

        private void FlushBatch(bool sync = false)
        {
            if (_batch.Ops.Count == 0) return;
            FlushAggregates();
            var b = _batch; _batch = new DayBatch();
            _writer.Submit(b);
        }

        // ---------------- the recorder interface ----------------

        private Plan PlanOf(Type t)
        {
            if (_plans.TryGetValue(t, out var p)) return p;
            var info = EventCatalog.Of(t);
            string domain = info.Domain.ToString().ToLowerInvariant();
            p = new Plan { Info = info, Map = _registry.Resolve(t), Domain = domain, Level = _info.Domains.TryGetValue(domain, out var l) ? l : _info.Level };
            _plans[t] = p;
            return p;
        }

        public void OnTick(GodSim sim, IReadOnlyList<SimEvent> events)
        {
            if (_ended) return;
            foreach (var e in events)
            {
                var p = PlanOf(e.GetType());
                if (p.Level < RecordingLevel.Summary) continue;
                _ctx.Event = e; _ctx.Info = p.Info; _ctx.Full = p.Level >= RecordingLevel.Full;
                long date = _ctx.DateKey();
                AddAggregate(Tables.FactEventDay, new object[] { date, (long)p.Info.Code }, null, new long[] { 1 });
                if (p.Level >= RecordingLevel.Full || !p.Info.HighVolume) { _ctx.RowWanted = true; _ctx.Weight = 1; }
                else
                {
                    int every = Math.Max(1, _info.SampleEvery);
                    _ctx.RowWanted = Hex.SampleKey(_info.Seed, e.Tick, e.Seq) % (ulong)every == 0;
                    _ctx.Weight = every;
                    if (!_ctx.RowWanted) { _sampledOut.TryGetValue(p.Domain, out long n); _sampledOut[p.Domain] = n + 1; }
                }
                p.Map.Map(_ctx, e);
            }
        }

        public void OnDayEnd(GodSim sim, FactionDayRow[] rows)
        {
            if (_ended) return;
            long day = rows.Length > 0 ? rows[0].Day : _cal.Day(sim.State.Tick) - 1;
            long date = EnsureDate(day);
            if (_info.Level >= RecordingLevel.Summary)
            {
                foreach (var r in rows)
                {
                    long fk = FactionKeyOf((int)r.Faction) ?? 0;
                    AddRow(Tables.FactFactionDay, 1L, date, fk, r.Tick, r.Eliminated ? 1L : 0L, (long)r.VictoryPoints, (long)r.Hqs, (long)r.Citadels, (long)r.Buildings,
                        (long)r.BuildingsBuilt, (long)r.Units, (long)r.Walkers, (long)r.StockTotal, (long)r.WarsActive, (long)r.EnvoysOut);
                    for (int w = 0; w < r.Stock.Length; w++) if (r.Stock[w] != 0) AddRow(Tables.FactFactionDayStock, 1L, date, fk, (long)w, (long)r.Stock[w]);
                }
            }
            if (_metrics != null)
                foreach (var kv in _metrics.Latest.OrderBy(k => k.Key, StringComparer.Ordinal)) AddRow(Tables.FactMetricDay, 1L, date, kv.Key, kv.Value);
            CommitDay();
        }

        private void CommitDay()
        {
            foreach (var b in _buildings.Values.Where(x => x.Dirty).OrderBy(x => x.Key))
            {
                AddRow(Tables.DimBuildingUpsert, b.Key, 1L, (long)b.Id, b.FactionKey, b.InitialFactionKey, b.Kind, (object)b.SizeCode, b.IsHq ? 1L : 0L, b.NodeLocation, b.HexLocation,
                    b.PlacedTick, b.CompletedTick, b.DestroyedTick, b.RemovedTick, (object)b.Level);
                b.Dirty = false;
            }
            foreach (var p in _persons.Values.Where(x => x.Dirty).OrderBy(x => x.Key))
            {
                AddRow(Tables.DimPersonUpsert, p.Key, 1L, (long)p.NpcId, p.FactionKey, p.IsFounder ? 1L : 0L, p.SpawnTick, p.SpawnDate, p.SpawnLocation,
                    p.DeathTick, p.DeathDate, (object)p.DeathCause, p.AgeAtDeath);
                p.Dirty = false;
            }
            FlushBatch();
        }

        public void End(GodSim sim, IDictionary<string, long> counters)
        {
            if (_ended) return;
            _ended = true;
            CommitDay();
            _writer.Drain();
            foreach (var kv in _writer.Rows) counters["warehouse.rows." + kv.Key] = kv.Value;
            counters["warehouse.loads"] = _writer.Loads;
            foreach (var kv in _sampledOut) counters["warehouse.sampled_out." + kv.Key] = kv.Value;
        }

        /// <summary>
        /// The last step, after the exit code is known: the run's final fields, the run totals, the indexes, the foreign key
        /// check, and closing the file as a single self-contained database (no -wal file).
        /// </summary>
        public void Finish(RunResult res, Scenario sc)
        {
            if (_finished) return;
            _finished = true;
            if (!_ended) { var tmp = new Dictionary<string, long>(); End(null, tmp); }
            var batch = new DayBatch();
            void Metric(string name, string source, long v, bool det = true) => batch.Ops.Add(new Op { Table = Tables.FactRunMetric, Values = new object[] { 1L, name, source, v, det ? 1L : 0L } });
            var seen = new HashSet<string>();
            foreach (var kv in res.Counters) if (seen.Add(kv.Key)) Metric(kv.Key, "counter", kv.Value);
            foreach (var kv in res.FinalMetrics.OrderBy(k => k.Key, StringComparer.Ordinal)) if (seen.Add(kv.Key)) Metric(kv.Key, "metric", kv.Value);
            foreach (var kv in _writer.Rows) Metric("rows_" + kv.Key, "warehouse", kv.Value);
            foreach (var kv in _sampledOut) Metric("sampled_out_" + kv.Key, "warehouse", kv.Value);
            Metric("queue_full_waits", "warehouse", _writer.QueueFullWaits, false);
            Metric("writer_seconds_ms", "warehouse", (long)(_writer.WriterSeconds * 1000), false);
            _db.Write(batch, (t, n) => { });
            using (var c = _db.Conn.CreateCommand())
            {
                c.CommandText = "UPDATE dim_run SET final_tick = @t, final_hash = @h, stop_reason = @s, exit_code = @x, wall_started = @w, wall_seconds = @ws WHERE run_key = 1";
                c.Parameters.AddWithValue("@t", res.Ticks); c.Parameters.AddWithValue("@h", Hex.Of(res.FinalHash)); c.Parameters.AddWithValue("@s", (object)res.StopReason ?? DBNull.Value);
                c.Parameters.AddWithValue("@x", (long)res.ExitCode); c.Parameters.AddWithValue("@w", _wallStart.ToString("o")); c.Parameters.AddWithValue("@ws", res.WallSeconds);
                c.ExecuteNonQuery();
            }
            if (_opt.IndexAtEnd) _db.PostLoad();
            int bad = ForeignKeyViolations(_db);
            if (bad > 0) res.Messages.Add("warehouse: " + bad + " foreign key violation(s) (PRAGMA foreign_key_check)");
            _db.Exec("INSERT OR REPLACE INTO fact_run_metric (run_key, metric, source, value, deterministic) VALUES (1, 'foreign_key_violations', 'warehouse', " + bad + ", 1)");
            _db.ReleaseCommands();
            _db.Exec("PRAGMA wal_checkpoint(TRUNCATE)");
            _db.Exec("PRAGMA journal_mode=DELETE");
            Dispose();
        }

        public static int ForeignKeyViolations(WarehouseDb db)
        {
            int n = 0;
            using (var c = db.Conn.CreateCommand())
            {
                c.CommandText = "PRAGMA foreign_key_check";
                using (var r = c.ExecuteReader()) while (r.Read()) n++;
            }
            return n;
        }

        public void Dispose()
        {
            _writer?.Dispose(); _writer = null;
            if (_db != null) { _db.ReleaseCommands(); _db.Dispose(); _db = null; }
        }
    }
}
