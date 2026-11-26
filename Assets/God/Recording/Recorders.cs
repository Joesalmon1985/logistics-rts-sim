using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using God.Sim;

namespace God.Recording
{
    /// <summary>
    /// The Metrics recorder (DATA_WAREHOUSE_DESIGN 9: SmokeRun's event counting becomes its counters): counts every event
    /// by type, and at each day end computes the M0 metrics from the snapshot rows and the counters. Writes
    /// <c>metrics.jsonl</c> (one line a sim day, the requested metrics in the requested order) when it has a directory.
    /// </summary>
    public sealed class MetricsRecorder : IRunRecorder
    {
        /// <summary>The M0 metric names (runner counters and daily faction aggregates). Scenario files may only name these.</summary>
        public static IReadOnlyList<string> Catalogue => new[]
        {
            "factions_alive", "vp_max", "vp_total", "hqs_total", "citadels_total", "buildings_total", "buildings_built",
            "units_total", "walkers_total", "stock_total", "wars_active", "envoys_out",
            "wars_declared", "peace_made", "captures", "eliminations", "units_produced", "units_died", "buildings_completed",
            "wares_produced", "trades_offered", "trades_accepted", "trades_refused", "envoys_killed", "commands_applied",
            "commands_rejected", "events_total", "invariant_failures",
        };

        private readonly List<string> _metrics;
        private readonly SortedDictionary<string, long> _counters = new SortedDictionary<string, long>(StringComparer.Ordinal);
        private RunInfo _info;
        private StreamWriter _out;
        private long _bytes, _lines;

        /// <summary>The metrics of the last day end (empty before the first).</summary>
        public readonly Dictionary<string, long> Latest = new Dictionary<string, long>();
        /// <summary>Counted outside this recorder (the host's invariant checks) but reported with the metrics.</summary>
        public long InvariantFailures;

        public MetricsRecorder(IEnumerable<string> metrics = null)
        {
            _metrics = metrics != null ? new List<string>(metrics) : new List<string>(Catalogue);
            if (_metrics.Count == 0) _metrics.AddRange(Catalogue);
        }

        public IReadOnlyDictionary<string, long> Counters => _counters;

        public void Begin(RunInfo info, GodSim sim)
        {
            _info = info;
            if (info.Dir != null && info.Level >= RecordingLevel.Metrics)
                _out = new StreamWriter(Path.Combine(info.Dir, "metrics.jsonl"), false, new UTF8Encoding(false)) { NewLine = "\n" };
        }

        private void Count(string key, long n = 1) { _counters.TryGetValue(key, out long v); _counters[key] = v + n; }

        public void OnTick(GodSim sim, IReadOnlyList<SimEvent> events)
        {
            foreach (var e in events)
            {
                Count("events_total");
                Count("events." + e.GetType().Name);
                switch (e)
                {
                    case WarDeclared _: Count("wars_declared"); break;
                    case PeaceMade _: Count("peace_made"); break;
                    case HqCaptured _: Count("captures"); break;
                    case FactionEliminated _: Count("eliminations"); break;
                    case UnitProduced _: Count("units_produced"); break;
                    case UnitDied _: Count("units_died"); break;
                    case BuildingCompleted _: Count("buildings_completed"); break;
                    case WaresProduced wp: Count("wares_produced", wp.Count); break;
                    case TradeOffered _: Count("trades_offered"); break;
                    case TradeAccepted _: Count("trades_accepted"); break;
                    case TradeRefused _: Count("trades_refused"); break;
                    case EnvoyKilled _: Count("envoys_killed"); break;
                    case CommandApplied _: Count("commands_applied"); break;
                    case CommandRejected _: Count("commands_rejected"); break;
                }
            }
        }

        public void OnDayEnd(GodSim sim, FactionDayRow[] rows)
        {
            Latest.Clear();
            long alive = 0, vpMax = 0, vpTotal = 0, hqs = 0, cit = 0, blds = 0, built = 0, units = 0, walkers = 0, stock = 0, wars = 0, envoys = 0;
            foreach (var r in rows)
            {
                if (!r.Eliminated) alive++;
                vpMax = Math.Max(vpMax, r.VictoryPoints); vpTotal += r.VictoryPoints; hqs += r.Hqs; cit += r.Citadels;
                blds += r.Buildings; built += r.BuildingsBuilt; units += r.Units; walkers += r.Walkers; stock += r.StockTotal;
                wars += r.WarsActive; envoys += r.EnvoysOut;
            }
            Latest["factions_alive"] = alive; Latest["vp_max"] = vpMax; Latest["vp_total"] = vpTotal; Latest["hqs_total"] = hqs;
            Latest["citadels_total"] = cit; Latest["buildings_total"] = blds; Latest["buildings_built"] = built; Latest["units_total"] = units;
            Latest["walkers_total"] = walkers; Latest["stock_total"] = stock; Latest["wars_active"] = wars / 2; Latest["envoys_out"] = envoys;
            foreach (var k in new[] { "wars_declared", "peace_made", "captures", "eliminations", "units_produced", "units_died", "buildings_completed",
                "wares_produced", "trades_offered", "trades_accepted", "trades_refused", "envoys_killed", "commands_applied", "commands_rejected", "events_total" })
                Latest[k] = _counters.TryGetValue(k, out long v) ? v : 0;
            Latest["invariant_failures"] = InvariantFailures;
            if (_out == null) return;
            var sb = new StringBuilder();
            sb.Append("{\"day\":").Append(rows.Length > 0 ? rows[0].Day : 0).Append(",\"t\":").Append(sim.State.Tick);
            foreach (var m in _metrics) sb.Append(",\"").Append(m).Append("\":").Append(Latest.TryGetValue(m, out long v) ? v : 0);
            sb.Append('}');
            _out.WriteLine(sb.ToString());
            _bytes += sb.Length + 1; _lines++;
        }

        public void End(GodSim sim, IDictionary<string, long> counters)
        {
            foreach (var kv in _counters) counters[kv.Key] = kv.Value;
            if (_out != null) { counters["bytes_written.metrics"] = _bytes; counters["rows.metrics"] = _lines; }
            Dispose();
        }

        public void Dispose() { _out?.Dispose(); _out = null; }
    }

    /// <summary>
    /// The JSONL event recorder (DATA_WAREHOUSE_DESIGN 8, M0). At Full it writes every event to <c>events.jsonl.gz</c> and
    /// every daily snapshot to <c>snapshots.jsonl.gz</c>. At Summary it keeps the low-volume events in full and one
    /// high-volume event (fights, dice, piles, production) in <see cref="RunInfo.SampleEvery"/>, chosen by a hash of
    /// seed, tick and seq (no Rng), and the snapshots. Per-domain switches override the level for one domain.
    /// </summary>
    public sealed class JsonlRecorder : IRunRecorder
    {
        private RunInfo _info;
        private EventJson _json;
        private StreamWriter _events, _snaps;
        private long _eventBytes, _eventRows, _snapRows, _sampledOut;

        public void Begin(RunInfo info, GodSim sim)
        {
            _info = info;
            _json = new EventJson(info.RunId);
            if (info.Dir == null) return;
            if (MaxLevel(info) >= RecordingLevel.Summary) _events = Gz(Path.Combine(info.Dir, "events.jsonl.gz"));
            if (info.Level >= RecordingLevel.Summary) _snaps = Gz(Path.Combine(info.Dir, "snapshots.jsonl.gz"));
        }

        private static RecordingLevel MaxLevel(RunInfo info)
        {
            var l = info.Level;
            foreach (var kv in info.Domains) if (kv.Value > l) l = kv.Value;
            return l;
        }

        private static StreamWriter Gz(string path) =>
            new StreamWriter(new GZipStream(File.Create(path), CompressionLevel.Fastest), new UTF8Encoding(false)) { NewLine = "\n" };

        /// <summary>True when the event is written at this run's level and domain switches.</summary>
        public bool Keeps(SimEvent e)
        {
            var info = EventCatalog.Of(e.GetType());
            string domain = info.Domain.ToString().ToLowerInvariant();
            var level = _info.Domains.TryGetValue(domain, out var l) ? l : _info.Level;
            if (level >= RecordingLevel.Full) return true;
            if (level < RecordingLevel.Summary) return false;
            if (!info.HighVolume) return true;
            int every = Math.Max(1, _info.SampleEvery);
            return Hex.SampleKey(_info.Seed, e.Tick, e.Seq) % (ulong)every == 0;
        }

        public void OnTick(GodSim sim, IReadOnlyList<SimEvent> events)
        {
            if (_events == null) return;
            foreach (var e in events)
            {
                if (!Keeps(e)) { _sampledOut++; continue; }
                var line = _json.Line(e);
                _events.WriteLine(line);
                _eventBytes += line.Length + 1; _eventRows++;
            }
        }

        public void OnDayEnd(GodSim sim, FactionDayRow[] rows)
        {
            if (_snaps == null) return;
            foreach (var r in rows) { _snaps.WriteLine(_json.SnapshotLine(r)); _snapRows++; }
        }

        public void End(GodSim sim, IDictionary<string, long> counters)
        {
            if (_events != null) { counters["rows.events"] = _eventRows; counters["bytes_written.events"] = _eventBytes; counters["sampled_out"] = _sampledOut; }
            if (_snaps != null) counters["rows.snapshots"] = _snapRows;
            Dispose();
        }

        public void Dispose()
        {
            _events?.Dispose(); _events = null;
            _snaps?.Dispose(); _snaps = null;
        }
    }
}
