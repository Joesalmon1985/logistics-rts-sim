using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using God.Recording;
using Newtonsoft.Json.Linq;

namespace God.Runner.Warehouse
{
    /// <summary>
    /// The telemetry overhead bench (DATA_WAREHOUSE 10.10, ACCEPTANCE G5): the same sim run with recording Off, Metrics, and
    /// Summary and Full to JSONL and to SQLite, reporting ticks per second, the overhead against Off and the database size
    /// per sim day. Each mode runs twice after a warm-up run and keeps the faster one, so a single slow run does not decide.
    /// The numbers are wall-clock and machine-dependent: a report, not a pass/fail test.
    /// </summary>
    public static class RecordingBench
    {
        public static JObject Run(Scenario sc, long ticks, ulong seed, TextWriter log)
        {
            string root = Path.Combine(Path.GetTempPath(), "god-bench-recording-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(root);
            try
            {
                var modes = new List<(string name, RecordingLevel level, bool jsonl, bool sqlite, bool async)>
                {
                    ("off", RecordingLevel.Off, false, false, true),
                    ("metrics", RecordingLevel.Metrics, false, false, true),
                    ("sqlite_metrics", RecordingLevel.Metrics, false, true, true),
                    ("jsonl_summary", RecordingLevel.Summary, true, false, true),
                    ("sqlite_summary", RecordingLevel.Summary, false, true, true),
                    ("sqlite_summary_sync", RecordingLevel.Summary, false, true, false),
                    ("jsonl_full", RecordingLevel.Full, true, false, true),
                    ("sqlite_full", RecordingLevel.Full, false, true, true),
                };
                RunOne(sc, ticks / 4, seed + 77, RecordingLevel.Off, false, false, true, root, "warmup");
                var rows = new JArray(); double offTps = 0;
                foreach (var m in modes)
                {
                    double best = 0; long dbBytes = 0, jsonlBytes = 0; long days = 0; string hash = null;
                    for (int rep = 0; rep < 2; rep++)
                    {
                        var r = RunOne(sc, ticks, seed, m.level, m.jsonl, m.sqlite, m.async, root, m.name + rep);
                        if (r.TicksPerSecond > best)
                        {
                            best = r.TicksPerSecond; hash = Hex.Of(r.FinalHash); days = r.Ticks / 1200;
                            dbBytes = r.WarehousePath != null && File.Exists(r.WarehousePath) ? new FileInfo(r.WarehousePath).Length : 0;
                            jsonlBytes = r.RunDir == null ? 0 : new[] { "events.jsonl.gz", "snapshots.jsonl.gz" }.Select(f => Path.Combine(r.RunDir, f)).Where(File.Exists).Sum(f => new FileInfo(f).Length);
                        }
                    }
                    if (m.name == "off") offTps = best;
                    double overhead = offTps > 0 && best > 0 ? Math.Round((offTps / best - 1) * 100, 1) : 0;
                    var o = new JObject { ["mode"] = m.name, ["ticksPerSecond"] = Math.Round(best, 0), ["overheadPercent"] = overhead, ["finalHash"] = hash };
                    if (m.sqlite) { o["dbBytes"] = dbBytes; o["dbBytesPerSimDay"] = days > 0 ? dbBytes / days : 0; }
                    if (m.jsonl) { o["jsonlBytes"] = jsonlBytes; o["jsonlBytesPerSimDay"] = days > 0 ? jsonlBytes / days : 0; }
                    rows.Add(o);
                    log?.WriteLine("bench recording " + m.name + ": " + o["ticksPerSecond"] + " ticks/s, overhead " + overhead + "%");
                }
                var hashes = rows.Select(r => (string)r["finalHash"]).Distinct().Count();
                return new JObject { ["scenario"] = sc.Name, ["ticks"] = ticks, ["seed"] = seed, ["simDays"] = ticks / 1200, ["allHashesEqual"] = hashes == 1, ["modes"] = rows, ["date"] = DateTime.UtcNow.ToString("yyyy-MM-dd") };
            }
            finally { try { Directory.Delete(root, true); } catch { } }
        }

        private static RunResult RunOne(Scenario sc, long ticks, ulong seed, RecordingLevel level, bool jsonl, bool sqlite, bool async, string root, string tag)
        {
            var s = sc.Clone(); s.MaxTicks = ticks; s.MaxDays = 0; s.MaxYears = 0;
            s.Checks.Clear();
            var spec = new RunSpec
            {
                Scenario = s, Seed = seed, Quiet = true, GitCommit = "bench", WriteReplay = false, Level = level,
                OutRoot = level == RecordingLevel.Off && !sqlite ? null : root, RunDir = level == RecordingLevel.Off && !sqlite ? null : Path.Combine(root, tag),
            };
            if (sqlite) spec.Sqlite = new WarehouseOptions { Async = async };
            if (!jsonl && level >= RecordingLevel.Summary)
            {
                // the JSONL recorder is added by the engine for Summary and Full; "sqlite only" modes skip it by naming the level on the warehouse alone
                spec.SkipJsonl = true;
            }
            return RunEngine.Run(spec);
        }
    }
}
