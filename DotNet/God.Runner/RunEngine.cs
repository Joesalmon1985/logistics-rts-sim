using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using God.Recording;
using God.Runner.Warehouse;
using God.Sim;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace God.Runner
{
    /// <summary>One hash checkpoint: the tick after whose step it was taken, the full hash and the per-module parts.</summary>
    public sealed class Checkpoint
    {
        public long Tick;
        public ulong Hash;
        public ulong[] Parts;
    }

    /// <summary>Where two runs (or a run and its replay file) first disagree.</summary>
    public sealed class Divergence
    {
        public long LastGoodTick;
        public long BadCheckpointTick;
        /// <summary>The exact first tick whose hash differs, when bisected (else -1).</summary>
        public long FirstBadTick = -1;
        public List<string> Modules = new List<string>();
        public string Note;

        public override string ToString() =>
            "first bad checkpoint at tick " + BadCheckpointTick + " (last good " + LastGoodTick + "), modules " + string.Join(",", Modules)
            + (FirstBadTick >= 0 ? "; first bad tick " + FirstBadTick : "") + (Note != null ? "; " + Note : "");
    }

    /// <summary>Everything that decides one run, plus host-only options (output, recording, test hooks).</summary>
    public sealed class RunSpec
    {
        public Scenario Scenario;
        public ulong Seed;
        public int Replicate;
        /// <summary>Overrides the scenario's recording level (<c>--record</c>).</summary>
        public RecordingLevel? Level;
        /// <summary>The output root (<c>--out</c>); null writes no files at all.</summary>
        public string OutRoot;
        /// <summary>"adhoc" or the experiment name: the first folder under the output root.</summary>
        public string Group = "adhoc";
        /// <summary>An exact run directory (replaces out/group/scenario/seed/runid).</summary>
        public string RunDir;
        public bool WriteReplay = true;
        public long SaveAtTick;
        /// <summary>Overrides the scenario's saveEveryDays when positive.</summary>
        public int SaveEveryDays;
        public SaveFile FromSave;
        public string FromSavePath;
        /// <summary>Replay mode: commands come from this file, and its checkpoints are checked.</summary>
        public ReplayFile Replay;
        /// <summary>Stop at this tick (replay <c>--to</c>, verify <c>--ticks</c>); overrides the scenario's length when positive.</summary>
        public long ToTick;
        /// <summary>Checkpoint interval override (<c>--every N</c>); 0 uses the scenario's.</summary>
        public long CheckpointEvery;
        public bool CheckSmoke;
        public bool PrintEvents;
        public bool Quiet = true;
        public TextWriter Log;
        /// <summary>Keep every save's bytes in the result (tests).</summary>
        public bool KeepSaveBytes;
        /// <summary>Test-only systems added to the sim after it is built (injected nondeterminism, a throwing system).</summary>
        public List<Func<ISimSystem>> TestSystems = new List<Func<ISimSystem>>();
        /// <summary>Calls every snapshot builder after every tick (the read-only test); results are thrown away.</summary>
        public bool SnapshotEveryTick;
        /// <summary>
        /// A host hook called before every step, outside <see cref="GodSim.Step()"/>: commands it submits are external (like a
        /// person's or a served agent's) and are logged in the replay with origin "external". Used by the replay tests.
        /// </summary>
        public Action<GodSim> BeforeStep;
        /// <summary>Also record the run in a SQLite warehouse (<c>warehouse.sqlite</c> in the run directory unless a path is set). Null: JSONL only.</summary>
        public WarehouseOptions Sqlite;
        /// <summary>Do not write the JSONL event and snapshot streams even at Summary or Full (SQLite only; <c>--no-jsonl</c>).</summary>
        public bool SkipJsonl;
        /// <summary>Extra recorders built per run (tests: an event capture). They are begun, fed and ended like the runner's own.</summary>
        public List<Func<IRunRecorder>> ExtraRecorders = new List<Func<IRunRecorder>>();
        public string Host = "runner";
        public string GitCommit;
        public CancellationToken Cancel;
    }

    public sealed class RunResult
    {
        public string RunId;
        public ulong Seed;
        public long Ticks;
        public ulong FinalHash;
        public ulong[] FinalParts;
        public List<Checkpoint> Checkpoints = new List<Checkpoint>();
        public string StopReason;
        public int ExitCode;
        public SortedDictionary<string, long> Counters = new SortedDictionary<string, long>(StringComparer.Ordinal);
        public Dictionary<string, long> FinalMetrics = new Dictionary<string, long>();
        public double WallSeconds;
        public double TicksPerSecond;
        public string SmokeLine;
        public bool? SmokeOk;
        public string LastStandings;
        public string RunDir;
        /// <summary>The SQLite warehouse file of this run, when one was written.</summary>
        public string WarehousePath;
        public Divergence Divergence;
        public List<string> Messages = new List<string>();
        public List<(long tick, byte[] bytes)> Saves = new List<(long, byte[])>();
        public GodSim Sim;
        public string Error;
    }

    /// <summary>
    /// One run (HEADLESS_SIMULATION 4.1): build through <see cref="SimSession"/>, then step, drain events into the
    /// recorders, snapshot at day ends, check invariants every 500 ticks, checkpoint every K ticks, save when asked and
    /// stop on the scenario's conditions. Recording, snapshots and hashing only read the sim.
    /// </summary>
    public static class RunEngine
    {
        public const int InvariantEvery = 500;

        public static RunResult Run(RunSpec spec)
        {
            var wall = Stopwatch.StartNew();
            var res = new RunResult { Seed = spec.Seed };
            var sc = spec.Scenario;
            var level = spec.Level ?? sc.RecordLevel;
            bool checkInvariants = sc.Checks.Contains("invariants");
            bool smokeCheck = spec.CheckSmoke || sc.Checks.Contains("smoke");
            string git = spec.GitCommit ?? GitInfo.Commit();
            res.RunId = RunId(sc, spec.Seed, spec.Replicate, git);
            var log = spec.Log ?? TextWriter.Null;

            // ---- outputs ----
            string dir = null;
            if (spec.RunDir != null) dir = spec.RunDir;
            else if (spec.OutRoot != null) dir = Path.Combine(spec.OutRoot, spec.Group ?? "adhoc", sc.Name, spec.Seed.ToString(), res.RunId);
            res.RunDir = dir;
            bool ioFailed = false;
            if (dir != null)
            {
                try
                {
                    Directory.CreateDirectory(dir);
                    File.WriteAllText(Path.Combine(dir, "scenario.resolved.json"), sc.ToJson().ToString(Formatting.Indented));
                }
                catch (Exception e) { ioFailed = true; res.Messages.Add("I/O error: " + e.Message); dir = null; res.RunDir = null; }
            }

            // ---- build ----
            SimSession session;
            try { session = new SimSession(sc, spec.Seed, spec.FromSave, spec.Replay, spec.TestSystems); }
            catch (ScenarioException e) { res.ExitCode = ExitCodes.InvalidInput; res.Error = e.Message; return res; }
            catch (ReplayException e) { res.ExitCode = ExitCodes.InvalidInput; res.Error = e.Message; return res; }
            catch (SaveException e) { res.ExitCode = ExitCodes.InvalidInput; res.Error = e.Message; return res; }
            var sim = session.Sim; var s = sim.State;
            res.Sim = sim;
            var cal = SimCalendar.Of(s);
            long k = spec.CheckpointEvery > 0 ? spec.CheckpointEvery : spec.Replay != null ? spec.Replay.Header.CheckpointEvery : sc.EffectiveCheckpointEvery;
            long limit = spec.ToTick > 0 ? spec.ToTick : sc.LimitTicks;
            int saveEveryDays = spec.SaveEveryDays > 0 ? spec.SaveEveryDays : sc.SaveEveryDays;

            // ---- replay writer ----
            ReplayWriter replay = null;
            if (dir != null && spec.WriteReplay)
            {
                try
                {
                    replay = new ReplayWriter(Path.Combine(dir, "replay.godr"));
                    var h = new ReplayHeader { GitCommit = git, Host = spec.Host, Scenario = sc.ToJson(), Seed = spec.Seed, CheckpointEvery = k };
                    if (spec.FromSave != null)
                    {
                        h.StartKind = "save"; h.StartFile = spec.FromSavePath != null ? Path.GetFileName(spec.FromSavePath) : "start.save.json";
                        var bytes = spec.FromSavePath != null ? File.ReadAllBytes(spec.FromSavePath) : Encoding.UTF8.GetBytes(spec.FromSave.ToJson());
                        h.StartSha256 = Sha256(bytes);
                        if (spec.FromSavePath == null) File.WriteAllBytes(Path.Combine(dir, h.StartFile), bytes);
                        else File.Copy(spec.FromSavePath, Path.Combine(dir, h.StartFile), true);
                    }
                    replay.Header(h);
                }
                catch (Exception e) { ioFailed = true; res.Messages.Add("I/O error (replay): " + e.Message); replay?.Dispose(); replay = null; }
            }
            Action<Command> onExternal = c =>
            {
                try { replay?.Command(s.Tick + 1, c, session.CurrentOrigin); }
                catch (Exception e) { ioFailed = true; res.Messages.Add("I/O error (replay): " + e.Message); }
            };
            sim.ExternalCommandAccepted += onExternal;

            // ---- recorders ----
            var info = new RunInfo
            {
                RunId = res.RunId, Seed = spec.Seed, ScenarioName = sc.Name, Level = level, SampleEvery = sc.SampleEvery, Dir = dir,
                Domains = sc.RecordDomains.ToDictionary(kv => kv.Key, kv => kv.Value),
            };
            // the decision trace (DecisionMade, fact_decision) is built only at Full recording of the learning domain
            var learnLevel = info.Domains.TryGetValue("learning", out var ll) ? ll : level;
            sim.TraceDecisions = learnLevel >= RecordingLevel.Full;
            var metrics = new MetricsRecorder(sc.Metrics);
            var recorders = new List<IRunRecorder> { metrics };
            if (!spec.SkipJsonl && (level >= RecordingLevel.Summary || info.Domains.Values.Any(v => v >= RecordingLevel.Summary))) recorders.Add(new JsonlRecorder());
            WarehouseRecorder wh = null;
            if (spec.Sqlite != null && (level >= RecordingLevel.Metrics || info.Domains.Values.Any(v => v >= RecordingLevel.Summary)))
            {
                var wo = spec.Sqlite.Clone();
                if (wo.Path == null && dir != null) wo.Path = Path.Combine(dir, "warehouse.sqlite");
                if (wo.Path != null) { wh = new WarehouseRecorder(wo, sc, spec.Replicate, git, spec.Host, metrics); recorders.Add(wh); res.WarehousePath = wo.Path; }
            }
            foreach (var f in spec.ExtraRecorders) recorders.Add(f());
            try { foreach (var r in recorders) r.Begin(info, sim); }
            catch (Exception e) { ioFailed = true; res.Messages.Add("I/O error (recorder): " + e.Message); DropRecorders(recorders); }
            var smoke = smokeCheck ? new SmokeVerdict() : null;
            var expected = spec.Replay?.Of(ReplayEntryKind.Checkpoint).ToDictionary(e => e.Tick, e => e);
            var expectedEnd = spec.Replay?.Of(ReplayEntryKind.End).FirstOrDefault();
            var events = new List<SimEvent>(256);
            var eventJson = spec.PrintEvents ? new EventJson(res.RunId) : null;
            long invariantFailures = 0, exceptions = 0;
            string stop = null;
            bool failed = false;
            Checkpoint lastGood = new Checkpoint { Tick = s.Tick };
            if (!spec.Quiet) log.WriteLine("RUN " + sc.Name + " seed " + spec.Seed + " run " + res.RunId + " level " + RecordingLevels.Name(level) + (limit > 0 ? " to tick " + limit : ""));

            // ---- the loop ----
            while (true)
            {
                if (spec.Cancel.IsCancellationRequested) { stop = "interrupted"; break; }
                if (limit > 0 && s.Tick >= limit) { stop = spec.ToTick > 0 ? "toTick" : "maxTicks"; break; }
                if (s.IsGameOver) { stop = "gameOver"; break; }
                spec.BeforeStep?.Invoke(sim);
                try { session.Step(); }
                catch (Exception e)
                {
                    exceptions++; smoke?.CountException();
                    res.Error = "exception at tick " + s.Tick + ": " + e;
                    stop = "exception";
                    break;
                }
                events.Clear();
                sim.DrainInto(events);
                if (smoke != null)
                {
                    smoke.OnEvents(events);
                    smoke.AfterTick(s, out var sInfo, out var sWarn);
                    if (sWarn != null) log.WriteLine(sWarn);
                    if (sInfo != null && !spec.Quiet) log.WriteLine(sInfo);
                }
                try { foreach (var r in recorders) r.OnTick(sim, events); }
                catch (Exception e) { ioFailed = true; res.Messages.Add("I/O error (recorder): " + e.Message); DropRecorders(recorders); }
                if (eventJson != null) foreach (var e in events) log.WriteLine(eventJson.Line(e));
                if (spec.SnapshotEveryTick) SnapshotBuilder.FactionDay(s);

                if (checkInvariants && s.Tick % InvariantEvery == 0)
                {
                    var bad = SimInvariants.Check(s);
                    if (bad.Count > 0) { invariantFailures++; metrics.InvariantFailures = invariantFailures; res.Messages.Add("invariants at tick " + s.Tick + ": " + string.Join("; ", bad)); }
                }

                if (cal.IsDayEnd(s.Tick))
                {
                    var rows = SnapshotBuilder.FactionDay(s);
                    try { foreach (var r in recorders) r.OnDayEnd(sim, rows); }
                    catch (Exception e) { ioFailed = true; res.Messages.Add("I/O error (recorder): " + e.Message); DropRecorders(recorders); }
                    res.LastStandings = Standings(s);
                    if (!spec.Quiet && smoke == null) log.WriteLine(cal.Label(s.Tick) + " | " + res.LastStandings);
                    foreach (var c in sc.Fail) if (metrics.Latest.TryGetValue(c.Metric, out long v) && c.Holds(v)) { failed = true; stop = "fail:" + c.Metric; }
                    if (stop == null) foreach (var c in sc.StopWhen) if (metrics.Latest.TryGetValue(c.Metric, out long v) && c.Holds(v)) stop = "when:" + c.Metric;
                    if (stop == null && sc.PopulationBelow > 0 && s.Population != null && s.Population.AliveCount < sc.PopulationBelow) stop = "populationBelow";
                    if (saveEveryDays > 0 && cal.Day(s.Tick) % saveEveryDays == 0) WriteSave(spec, res, sim, dir, "day_" + cal.Day(s.Tick), ref ioFailed);
                }

                if (s.Tick % k == 0)
                {
                    var parts = sim.StateHashParts();
                    var cp = new Checkpoint { Tick = s.Tick, Hash = GodSim.Fold(parts), Parts = parts };
                    res.Checkpoints.Add(cp);
                    try { replay?.Checkpoint(cp.Tick, cp.Hash, cp.Parts); } catch (Exception e) { ioFailed = true; res.Messages.Add("I/O error (replay): " + e.Message); }
                    if (expected != null && expected.TryGetValue(cp.Tick, out var exp))
                    {
                        if (exp.Hash != cp.Hash)
                        {
                            res.Divergence = new Divergence { LastGoodTick = lastGood.Tick, BadCheckpointTick = cp.Tick, Modules = DiffModules(exp.Parts, cp.Parts, spec.Replay.Header.HashParts) };
                            stop = "divergence";
                        }
                        else lastGood = cp;
                    }
                }
                if (spec.SaveAtTick > 0 && s.Tick == spec.SaveAtTick) WriteSave(spec, res, sim, dir, "tick_" + s.Tick, ref ioFailed);
                if (stop != null) break;
                if (sc.WallSeconds > 0 && wall.Elapsed.TotalSeconds >= sc.WallSeconds) { stop = "wallSeconds"; break; }
            }

            // ---- the end ----
            sim.ExternalCommandAccepted -= onExternal;
            var finalParts = sim.StateHashParts();
            res.FinalParts = finalParts; res.FinalHash = GodSim.Fold(finalParts); res.Ticks = s.Tick; res.StopReason = stop;
            if (expectedEnd != null && res.Divergence == null && stop != "interrupted" && stop != "exception")
            {
                if (expectedEnd.Tick != s.Tick || expectedEnd.Hash != res.FinalHash)
                    res.Divergence = new Divergence { LastGoodTick = lastGood.Tick, BadCheckpointTick = s.Tick, Note = "the end differs: expected tick " + expectedEnd.Tick + " hash " + Hex.Of(expectedEnd.Hash) + ", got tick " + s.Tick + " hash " + Hex.Of(res.FinalHash) };
            }
            try { replay?.End(s.Tick, res.FinalHash, stop); } catch (Exception e) { ioFailed = true; res.Messages.Add("I/O error (replay): " + e.Message); }
            replay?.Dispose();
            if (smoke != null)
            {
                var over = smoke.Finish(s, out var w);
                if (w != null) log.WriteLine(w);
                if (!spec.Quiet) { log.WriteLine(over); log.WriteLine(smoke.LastLine); }
                res.SmokeLine = smoke.LastLine; res.SmokeOk = smoke.Ok;
                smoke.AddCounters(res.Counters);
            }
            foreach (var r in recorders)
            {
                try { r.End(sim, res.Counters); }
                catch (Exception e) { ioFailed = true; res.Messages.Add("I/O error (recorder end): " + e.Message); r.Dispose(); }
            }
            foreach (var kv in metrics.Latest) res.FinalMetrics[kv.Key] = kv.Value;
            res.Counters["ticks"] = s.Tick;
            res.Counters["checkpoints"] = res.Checkpoints.Count;
            res.Counters["invariant_failures"] = invariantFailures;
            res.Counters["exceptions"] = exceptions;
            res.Counters["commands_refused_at_submit"] = session.Refused.Count;
            foreach (var m in session.Refused) res.Messages.Add("refused: " + m);
            wall.Stop();
            res.WallSeconds = wall.Elapsed.TotalSeconds;
            res.TicksPerSecond = res.WallSeconds > 0 ? res.Ticks / res.WallSeconds : 0;

            // exit code: the most serious thing that happened
            int code = ExitCodes.Ok;
            if (failed || (smoke != null && smoke.Ok != true)) code = ExitCodes.CheckFailed;
            if (res.Divergence != null) code = ExitCodes.Worst(code, ExitCodes.Divergence);
            if (checkInvariants && invariantFailures > 0) code = ExitCodes.Worst(code, ExitCodes.InvariantFailed);
            if (exceptions > 0) code = ExitCodes.Worst(code, ExitCodes.SimException);
            if (stop == "interrupted") code = ExitCodes.Interrupted;
            else if (ioFailed) code = ExitCodes.Worst(code, ExitCodes.IoError);
            res.ExitCode = code;

            if (wh != null && recorders.Contains(wh))
            {
                try { wh.Finish(res, sc); }
                catch (Exception e) { res.Messages.Add("I/O error (warehouse): " + e.Message); if (res.ExitCode != ExitCodes.Interrupted) res.ExitCode = ExitCodes.Worst(res.ExitCode, ExitCodes.IoError); wh.Dispose(); }
            }
            if (dir != null) WriteOutputs(res, spec, level, git, dir);
            if (!spec.Quiet)
            {
                log.WriteLine("END " + sc.Name + " seed " + spec.Seed + " tick " + res.Ticks + " stop " + stop + " hash " + Hex.Of(res.FinalHash) + " exit " + res.ExitCode
                    + " (" + res.TicksPerSecond.ToString("0") + " ticks/s)");
                if (res.Divergence != null) log.WriteLine("DIVERGENCE " + res.Divergence);
                if (res.Error != null) log.WriteLine("ERROR " + res.Error);
            }
            return res;
        }

        // ---------------- helpers ----------------

        /// <summary>After a recorder failed: stop and close every recorder except the in-memory metrics (the sim result stays valid).</summary>
        private static void DropRecorders(List<IRunRecorder> recorders)
        {
            foreach (var r in recorders.Where(x => !(x is MetricsRecorder)).ToList()) { try { r.Dispose(); } catch { } }
            recorders.RemoveAll(r => !(r is MetricsRecorder));
        }

        public static string Standings(GodState s) =>
            string.Join(" | ", FactionRules.Standings(s).Select(st => FactionInfo.Name(st.Faction) + " " + st.VictoryPoints + " VP " + st.Hqs + " HQ" + (st.Eliminated ? " (out)" : "")));

        public static List<string> DiffModules(ulong[] a, ulong[] b, IReadOnlyList<string> names)
        {
            var l = new List<string>();
            if (a == null || b == null) { l.Add("unknown"); return l; }
            int n = Math.Max(a.Length, b.Length);
            for (int i = 0; i < n; i++)
            {
                ulong x = i < a.Length ? a[i] : 0, y = i < b.Length ? b[i] : 0;
                if (x != y) l.Add(i < names.Count ? names[i] : "part" + i);
            }
            return l;
        }

        /// <summary>16 hex digits of FNV-1a over the resolved scenario hash, the seed, the replicate and the git commit (no wall time).</summary>
        public static string RunId(Scenario sc, ulong seed, int replicate, string git)
        {
            ulong h = Hex.Fnv(sc.Hash(), 14695981039346656037UL);
            h = Hex.Fnv(seed, h);
            h = Hex.Fnv((ulong)replicate, h);
            h = Hex.Fnv(git ?? "", h);
            return Hex.Of(h);
        }

        public static string Sha256(byte[] bytes)
        {
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>A save of the sim with the AI's tag; deterministic bytes (no wall time in it).</summary>
        public static byte[] SaveBytes(GodSim sim) =>
            Encoding.UTF8.GetBytes(SaveFile.From(sim, null, new ISaveExtra[] { new AiTag() }).ToJson());

        private sealed class AiTag : ISaveExtra
        {
            public string SaveKey => "ai";
            public string SaveState() => AiDirector.SaveTag;
            public void LoadState(string data) { }
        }

        private static void WriteSave(RunSpec spec, RunResult res, GodSim sim, string dir, string name, ref bool ioFailed)
        {
            var bytes = SaveBytes(sim);
            if (spec.KeepSaveBytes) res.Saves.Add((sim.State.Tick, bytes));
            if (dir == null) return;
            try
            {
                Directory.CreateDirectory(Path.Combine(dir, "saves"));
                File.WriteAllBytes(Path.Combine(dir, "saves", name + ".json"), bytes);
            }
            catch (Exception e) { ioFailed = true; res.Messages.Add("I/O error (save): " + e.Message); }
        }

        /// <summary>The run's summary as written to summary.json: no wall-clock field, so equal runs give equal bytes.</summary>
        public static JObject SummaryJson(RunResult res, Scenario sc)
        {
            var counters = new JObject(); foreach (var kv in res.Counters) counters[kv.Key] = kv.Value;
            var metrics = new JObject(); foreach (var kv in res.FinalMetrics.OrderBy(x => x.Key, StringComparer.Ordinal)) metrics[kv.Key] = kv.Value;
            return new JObject
            {
                ["runId"] = res.RunId, ["scenario"] = sc.Name, ["scenarioHash"] = Hex.Of(sc.Hash()), ["seed"] = res.Seed,
                ["ticks"] = res.Ticks, ["stopReason"] = res.StopReason, ["exitCode"] = res.ExitCode,
                ["finalHash"] = Hex.Of(res.FinalHash), ["checkpoints"] = res.Checkpoints.Count,
                ["smoke"] = res.SmokeLine, ["standings"] = res.LastStandings,
                ["counters"] = counters, ["metrics"] = metrics,
            };
        }

        private static void WriteOutputs(RunResult res, RunSpec spec, RecordingLevel level, string git, string dir)
        {
            try
            {
                var sc = spec.Scenario;
                if (level >= RecordingLevel.Metrics) File.WriteAllText(Path.Combine(dir, "summary.json"), SummaryJson(res, sc).ToString(Formatting.Indented));
                var run = new JObject
                {
                    ["runId"] = res.RunId, ["scenario"] = sc.Name, ["scenarioHash"] = Hex.Of(sc.Hash()), ["seed"] = res.Seed, ["replicate"] = spec.Replicate,
                    ["gitCommit"] = git, ["simVersion"] = ReplayHeader.SimVersion, ["saveVersion"] = SaveFile.CurrentVersion, ["host"] = spec.Host,
                    ["wallStart"] = DateTime.UtcNow.AddSeconds(-res.WallSeconds).ToString("o"), ["wallEnd"] = DateTime.UtcNow.ToString("o"),
                    ["ticks"] = res.Ticks, ["ticksPerSecond"] = Math.Round(res.TicksPerSecond, 1), ["wallSeconds"] = Math.Round(res.WallSeconds, 3),
                    ["stopReason"] = res.StopReason, ["finalHash"] = Hex.Of(res.FinalHash), ["exitCode"] = res.ExitCode,
                    ["recordLevel"] = RecordingLevels.Name(level), ["policies"] = Scenario.PoliciesJson(sc.Policies),
                    ["purpose"] = Scenario.PurposeNames[(int)sc.EffectivePurpose], ["divergence"] = res.Divergence?.ToString(), ["error"] = res.Error,
                    ["messages"] = new JArray(res.Messages.Take(50)),
                };
                File.WriteAllText(Path.Combine(dir, "run.json"), run.ToString(Formatting.Indented));
            }
            catch (Exception e)
            {
                res.Messages.Add("I/O error (outputs): " + e.Message);
                if (res.ExitCode != ExitCodes.Interrupted) res.ExitCode = ExitCodes.Worst(res.ExitCode, ExitCodes.IoError);
            }
        }
    }

    /// <summary>The git commit of the working tree (for manifests and run ids), or "unknown". Cached per process.</summary>
    public static class GitInfo
    {
        private static readonly Lazy<string> Cached = new Lazy<string>(Read);

        public static string Commit() => Cached.Value;

        private static string Read()
        {
            string env = Environment.GetEnvironmentVariable("GOD_GIT_COMMIT");
            if (!string.IsNullOrEmpty(env)) return env.Trim();
            try
            {
                var psi = new ProcessStartInfo("git", "rev-parse HEAD") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = AppContext.BaseDirectory };
                using (var p = Process.Start(psi))
                {
                    string o = p.StandardOutput.ReadToEnd().Trim();
                    p.WaitForExit(5000);
                    return p.ExitCode == 0 && o.Length >= 7 ? o : "unknown";
                }
            }
            catch { return "unknown"; }
        }
    }
}
