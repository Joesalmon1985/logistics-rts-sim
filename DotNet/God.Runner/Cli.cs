using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using God.Recording;
using God.Runner.Warehouse;
using God.Sim;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace God.Runner
{
    /// <summary>
    /// <c>god-runner &lt;command&gt; [options]</c> (HEADLESS_SIMULATION 4.11). M0 commands: run, batch, verify, replay, bench.
    /// The commands of later milestones answer with exit code <see cref="ExitCodes.NotImplemented"/> and the milestone
    /// that brings them; they never pretend to work.
    /// </summary>
    public static class Cli
    {
        private static readonly string[] Flags =
        {
            "quiet", "json", "no-out", "no-replay", "print-events", "check-smoke", "parallel", "save-mid", "record-matrix",
            "processes", "strict", "update-baseline", "help", "sqlite", "no-index", "fk", "no-jsonl",
        };

        private static readonly Dictionary<string, string> Later = new Dictionary<string, string>
        {
            ["experiment"] = "M14 (balance sweeps; the full policy suite in stage 6)",
            ["serve"] = "M15 (network readiness)", ["dataset"] = "M15 (network readiness)", ["verify-model"] = "M15 (network readiness)",
        };

        public const string Usage = @"God.Runner - the headless God of the Island simulation (stage 4 M0)

usage: God.Runner <command> [options]

commands:
  run      one run         --scenario FILE|NAME --seed N [--ticks T|--days D|--years Y] [--set path=value]...
                           [--record off|metrics|summary|full] [--sqlite [--no-jsonl]] [--out DIR|--no-out] [--group NAME] [--every N]
                           [--from-save FILE] [--save-at TICK] [--save-every-days N] [--check smoke] [--no-replay]
                           [--print-events] [--quiet] [--json]
  batch    many seeds      --scenario S --seeds 1-64 [--workers N] [--processes] (and run's options)
  verify   determinism     FILE | --against-unity FILE | --scenario S --seed N --ticks T [--parallel] [--save-mid]
                           [--record-matrix] [--every N] [--out DIR]
  replay   re-run a replay FILE [--to TICK] [--record LEVEL] [--save-at TICK] [--print-events] [--out DIR]
  bench    benchmark       [--cases bench_stage3_watch6] [--ticks N] [--parallel-ticks N] [--workers 1,4,16]
                           [--compare FILE] [--update-baseline] [--baseline FILE] [--strict] [--out DIR]
  lab      game-theory lab list | run L1 [--seed N] | all --check [--seeds 1..5] | report DIR | hash L1 (M2) | invade | sweep
  export   warehouse out  --from FILE|DIR [--to csv|parquet] --out DIR [--tables a,b]   (SQLite to CSV or a Parquet lake)
  learn-dump  one NPC's association store as JSON lines --scenario FILE|NAME --seed N --days D --npc ID|all [--out FILE]
                           or --from-save FILE --npc ID|all
  experiment, serve, dataset, verify-model: later milestones (exit code 9)

exit codes: 0 ok, 1 check failed, 2 usage, 3 invalid input, 4 divergence, 5 invariant, 6 sim exception,
            7 performance, 8 I/O, 9 not implemented until a later milestone, 130 interrupted";


        public static int Run(string[] argv, TextWriter output, TextWriter error, CancellationToken cancel)
        {
            if (argv.Length == 0 || argv[0] == "help" || argv[0] == "--help" || argv[0] == "-h") { output.WriteLine(Usage); return argv.Length == 0 ? ExitCodes.Usage : ExitCodes.Ok; }
            string cmd = argv[0];
            try
            {
                if (cmd == "lab") return LabCommand.Run(argv, output, error, cancel);
                if (Later.TryGetValue(cmd, out var when))
                {
                    error.WriteLine("'" + cmd + "' is not implemented until " + when + ".");
                    return ExitCodes.NotImplemented;
                }
                var a = new Args(argv, 1, Flags);
                switch (cmd)
                {
                    case "run": return RunCmd(a, output, error, cancel);
                    case "batch": return BatchCmd(a, output, error, cancel);
                    case "verify": return VerifyCmd(a, output, error, cancel);
                    case "replay": return ReplayCmd(a, output, error, cancel);
                    case "bench": return BenchCmd(a, output, error);
                    case "export": return ExportCmd(a, output, error);
                    case "learn-dump": return LearnDump.Run(a, output, error, cancel);
                    default: error.WriteLine("unknown command '" + cmd + "'\n\n" + Usage); return ExitCodes.Usage;
                }
            }
            catch (UsageException e) { error.WriteLine("usage error: " + e.Message); return ExitCodes.Usage; }
            catch (ScenarioException e) { error.WriteLine("invalid scenario: " + e.Message); return ExitCodes.InvalidInput; }
            catch (ReplayException e) { error.WriteLine("invalid replay: " + e.Message); return ExitCodes.InvalidInput; }
            catch (SaveException e) { error.WriteLine("invalid save: " + e.Message); return ExitCodes.InvalidInput; }
            catch (IOException e) { error.WriteLine("I/O error: " + e.Message); return ExitCodes.IoError; }
            catch (UnauthorizedAccessException e) { error.WriteLine("I/O error: " + e.Message); return ExitCodes.IoError; }
        }

        // ---------------- scenario and common options ----------------

        /// <summary>A scenario file path, or the name of one of the runner's own scenarios (<c>watch6</c>).</summary>
        public static string FindScenario(string nameOrPath)
        {
            if (nameOrPath == null) throw new UsageException("--scenario is required");
            if (File.Exists(nameOrPath)) return nameOrPath;
            foreach (var c in new[] { nameOrPath + ".scenario.json", Path.Combine(AppContext.BaseDirectory, "Scenarios", nameOrPath + ".scenario.json"), Path.Combine(AppContext.BaseDirectory, "Scenarios", nameOrPath) })
                if (File.Exists(c)) return c;
            throw new ScenarioException("no scenario file '" + nameOrPath + "'");
        }

        public static Scenario LoadScenario(Args a)
        {
            string path = FindScenario(a.Str("scenario"));
            string json;
            try { json = File.ReadAllText(path); }
            catch (Exception e) { throw new ScenarioException("cannot read " + path + ": " + e.Message); }
            var sc = Scenario.Parse(json, a.All("set"));
            long ticks = a.Long("ticks", 0), days = a.Long("days", 0), years = a.Long("years", 0);
            if (ticks < 0 || days < 0 || years < 0) throw new UsageException("--ticks, --days and --years must not be negative");
            if (ticks + days + years > 0) { sc.MaxTicks = ticks; sc.MaxDays = days; sc.MaxYears = years; }
            return sc;
        }

        private static RunSpec CommonSpec(Args a, Scenario sc, TextWriter log, CancellationToken cancel)
        {
            var spec = new RunSpec { Scenario = sc, Seed = a.ULong("seed") ?? 1, Log = log, Cancel = cancel, Quiet = a.Flag("quiet") };
            string rec = a.Str("record");
            if (rec != null)
            {
                if (rec == "experiment") throw new UsageException("--record experiment is not implemented until M13");
                if (!RecordingLevels.TryParse(rec, out var lv)) throw new UsageException("--record must be off, metrics, summary or full");
                spec.Level = lv;
            }
            bool noOut = a.Flag("no-out");
            spec.OutRoot = noOut ? null : a.Str("out", "out");
            spec.Group = a.Str("group", "adhoc");
            spec.WriteReplay = !a.Flag("no-replay");
            spec.CheckpointEvery = a.Long("every", 0);
            if (spec.CheckpointEvery < 0) throw new UsageException("--every must be positive");
            string check = a.Str("check");
            if (check != null && check != "smoke") throw new UsageException("--check takes smoke");
            spec.CheckSmoke = check == "smoke" | a.Flag("check-smoke");
            spec.PrintEvents = a.Flag("print-events");
            if (a.Flag("sqlite"))
            {
                if (noOut) throw new UsageException("--sqlite writes warehouse.sqlite into the run directory, so it cannot be combined with --no-out");
                spec.Sqlite = new WarehouseOptions { IndexAtEnd = !a.Flag("no-index"), ForeignKeys = a.Flag("fk") };
            }
            else { a.Flag("no-index"); a.Flag("fk"); }
            spec.SkipJsonl = a.Flag("no-jsonl");
            return spec;
        }

        private static void PrintResult(RunResult r, TextWriter output, bool json, Scenario sc)
        {
            if (json)
            {
                var o = new JObject
                {
                    ["runId"] = r.RunId, ["seed"] = r.Seed, ["exitCode"] = r.ExitCode, ["ticks"] = r.Ticks, ["finalHash"] = Hex.Of(r.FinalHash),
                    ["stopReason"] = r.StopReason, ["runDir"] = r.RunDir, ["error"] = r.Error, ["divergence"] = r.Divergence?.ToString(),
                    ["ticksPerSecond"] = Math.Round(r.TicksPerSecond, 1), ["summary"] = RunEngine.SummaryJson(r, sc),
                };
                output.WriteLine(o.ToString(Formatting.None));
            }
            else
            {
                if (r.SmokeLine != null) output.WriteLine(r.SmokeLine);
                output.WriteLine("run " + r.RunId + " seed " + r.Seed + ": tick " + r.Ticks + ", stop " + r.StopReason + ", hash " + Hex.Of(r.FinalHash) + ", exit " + r.ExitCode
                    + ", " + r.TicksPerSecond.ToString("0") + " ticks/s" + (r.RunDir != null ? ", " + r.RunDir : ""));
                if (r.Divergence != null) output.WriteLine("DIVERGENCE " + r.Divergence);
                if (r.Error != null) output.WriteLine("error: " + r.Error);
                foreach (var m in r.Messages.Take(10)) output.WriteLine("  " + m);
            }
        }

        // ---------------- run ----------------

        private static int RunCmd(Args a, TextWriter output, TextWriter error, CancellationToken cancel)
        {
            var sc = LoadScenario(a);
            bool json = a.Flag("json");
            var spec = CommonSpec(a, sc, json ? TextWriter.Null : output, cancel);
            spec.SaveAtTick = a.Long("save-at", 0);
            spec.SaveEveryDays = (int)a.Long("save-every-days", 0);
            string from = a.Str("from-save");
            if (from != null)
            {
                string text;
                try { text = File.ReadAllText(from); } catch (Exception e) { throw new SaveException("cannot read the save " + from + ": " + e.Message); }
                spec.FromSave = SaveFile.FromJson(text);
                spec.FromSavePath = from;
            }
            a.CheckAllUsed(0);
            var r = RunEngine.Run(spec);
            if (r.Error != null && r.ExitCode == ExitCodes.InvalidInput) { error.WriteLine("invalid input: " + r.Error); return r.ExitCode; }
            PrintResult(r, output, json, sc);
            return r.ExitCode;
        }

        // ---------------- batch ----------------

        /// <summary>One worker per physical core: half the logical processors (assumes two-way SMT, as on the reference machine).</summary>
        public static int DefaultWorkers => Math.Max(1, Environment.ProcessorCount / 2);

        private static int BatchCmd(Args a, TextWriter output, TextWriter error, CancellationToken cancel)
        {
            var sc = LoadScenario(a);
            var seeds = Args.ParseSeeds(a.Str("seeds") ?? throw new UsageException("--seeds is required (1-64, 1..8 or 1,3,5)"));
            int workers = (int)a.Long("workers", DefaultWorkers);
            bool processes = a.Flag("processes");
            bool json = a.Flag("json");
            var spec = CommonSpec(a, sc, TextWriter.Null, cancel);
            if (a.ULong("seed") != null) throw new UsageException("batch takes --seeds, not --seed");
            a.CheckAllUsed(0);
            spec.Quiet = true;
            var items = Batch.Run(spec, seeds, workers, processes, json ? TextWriter.Null : output, out int code);
            if (json) output.WriteLine(new JObject { ["exitCode"] = code, ["runs"] = new JArray(items.Select(i => new JObject { ["seed"] = i.Seed, ["exitCode"] = i.ExitCode, ["finalHash"] = i.FinalHash, ["ticks"] = i.Ticks })) }.ToString(Formatting.None));
            else output.WriteLine("batch " + sc.Name + ": " + items.Count + " runs, " + items.Count(i => i.ExitCode == 0) + " ok, exit " + code);
            return code;
        }

        // ---------------- verify ----------------

        private static int VerifyCmd(Args a, TextWriter output, TextWriter error, CancellationToken cancel)
        {
            string unity = a.Str("against-unity");
            string file = unity ?? (a.Positional.Count > 0 ? a.Positional[0] : null);
            string outDir = a.Str("out");
            if (file != null)
            {
                long every = a.Long("every", 0);
                a.CheckAllUsed(unity != null ? 0 : 1);
                if (every > 0) throw new UsageException("--every applies to two fresh runs; a replay file has the checkpoints it was written with");
                return VerifyReplay(file, outDir, output, cancel, out _);
            }

            var sc = LoadScenario(a);
            var spec = CommonSpec(a, sc, TextWriter.Null, cancel);
            bool parallel = a.Flag("parallel"), saveMid = a.Flag("save-mid"), matrix = a.Flag("record-matrix");
            a.CheckAllUsed(0);
            if (sc.LimitTicks <= 0) throw new UsageException("verify needs a length: --ticks, --days or --years (or a scenario stop limit)");
            spec.Quiet = true; spec.OutRoot = null; spec.WriteReplay = false;
            string work = outDir ?? Path.Combine(Path.GetTempPath(), "god-verify-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            int code = ExitCodes.Ok;

            var d = Verify.CompareRuns(spec, Verify.Copy(spec), parallel, work, out var ra, out var rb);
            output.WriteLine("verify " + sc.Name + " seed " + spec.Seed + ": two " + (parallel ? "parallel" : "sequential") + " runs to tick " + ra.Ticks + ", "
                + ra.Checkpoints.Count + " checkpoints: " + (d == null ? "equal (" + Hex.Of(ra.FinalHash) + ")" : "DIVERGENCE " + d));
            if (d != null) code = ExitCodes.Divergence;
            code = ExitCodes.Worst(code, Severe(ra.ExitCode));

            if (saveMid)
            {
                long t = sc.LimitTicks;
                var problems = Verify.SaveLoadMid(spec, new[] { t / 3, 2 * t / 3 }, out _);
                output.WriteLine("verify save/load at " + (t / 3) + " and " + (2 * t / 3) + ": " + (problems.Count == 0 ? "equal to the straight run, second saves byte-identical" : "DIVERGENCE " + string.Join("; ", problems)));
                if (problems.Count > 0) code = ExitCodes.Worst(code, ExitCodes.Divergence);
            }
            if (matrix)
            {
                foreach (var (name, ms) in Verify.RecordMatrix(spec, Path.Combine(work, "matrix")))
                {
                    var r = RunEngine.Run(ms);
                    var md = Verify.Compare(ra, r);
                    output.WriteLine("verify recording " + name + ": " + (md == null ? "equal" : "DIVERGENCE " + md));
                    if (md != null) code = ExitCodes.Worst(code, ExitCodes.Divergence);
                    if (r.ExitCode == ExitCodes.IoError) code = ExitCodes.Worst(code, ExitCodes.IoError);
                }
            }
            if (outDir == null) { try { Directory.Delete(work, true); } catch { } }
            output.WriteLine("verify exit " + code);
            return code;
        }

        /// <summary>Exit codes of a run that matter to verify (exceptions and invariants; a smoke verdict does not).</summary>
        private static int Severe(int code) => code == ExitCodes.SimException || code == ExitCodes.InvariantFailed || code == ExitCodes.IoError ? code : ExitCodes.Ok;

        /// <summary><c>verify FILE</c>: re-run a replay and compare every checkpoint and the end. Exit 4 names the first bad checkpoint and its modules.</summary>
        public static int VerifyReplay(string file, string outDir, TextWriter output, CancellationToken cancel, out RunResult result)
        {
            var spec = ReplaySpec(file, cancel);
            spec.OutRoot = null; spec.RunDir = outDir != null ? Path.Combine(outDir, "verify") : null; spec.WriteReplay = false;
            result = RunEngine.Run(spec);
            if (result.ExitCode == ExitCodes.InvalidInput) { output.WriteLine("invalid input: " + result.Error); return result.ExitCode; }
            int checks = spec.Replay.Of(ReplayEntryKind.Checkpoint).Count();
            if (result.Divergence != null)
            {
                output.WriteLine("verify " + file + ": DIVERGENCE " + result.Divergence);
                return ExitCodes.Divergence;
            }
            output.WriteLine("verify " + file + ": " + checks + " checkpoints and the end (tick " + result.Ticks + ", " + Hex.Of(result.FinalHash) + ") match");
            return ExitCodes.Worst(ExitCodes.Ok, Severe(result.ExitCode));
        }

        public static RunSpec ReplaySpec(string file, CancellationToken cancel)
        {
            var rf = ReplayFile.Read(file);
            var sc = Scenario.FromJson(rf.Header.Scenario);
            var spec = new RunSpec { Scenario = sc, Seed = rf.Header.Seed, Replay = rf, Cancel = cancel, Quiet = true, CheckpointEvery = rf.Header.CheckpointEvery };
            var end = rf.Of(ReplayEntryKind.End).FirstOrDefault();
            if (end != null) spec.ToTick = end.Tick;
            if (rf.Header.StartKind == "save")
            {
                string path = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(file)) ?? ".", rf.Header.StartFile ?? "");
                if (!File.Exists(path)) throw new ReplayException("the replay starts from a save that is missing: " + path);
                var bytes = File.ReadAllBytes(path);
                if (RunEngine.Sha256(bytes) != rf.Header.StartSha256) throw new ReplayException("the start save does not match the replay's SHA-256: " + path);
                spec.FromSave = SaveFile.FromJson(System.Text.Encoding.UTF8.GetString(bytes));
                spec.FromSavePath = path;
            }
            return spec;
        }

        // ---------------- replay ----------------

        private static int ReplayCmd(Args a, TextWriter output, TextWriter error, CancellationToken cancel)
        {
            if (a.Positional.Count < 1) throw new UsageException("replay needs a FILE");
            var spec = ReplaySpec(a.Positional[0], cancel);
            long to = a.Long("to", 0);
            if (to > 0) spec.ToTick = to;
            string rec = a.Str("record");
            if (rec != null) { if (!RecordingLevels.TryParse(rec, out var lv)) throw new UsageException("--record must be off, metrics, summary or full"); spec.Level = lv; }
            spec.SaveAtTick = a.Long("save-at", 0);
            spec.PrintEvents = a.Flag("print-events");
            spec.Log = output;
            spec.Quiet = a.Flag("quiet");
            string outDir = a.Str("out");
            a.CheckAllUsed(1);
            spec.OutRoot = outDir; spec.Group = "replay";
            spec.WriteReplay = outDir != null;
            var r = RunEngine.Run(spec);
            PrintResult(r, output, false, spec.Scenario);
            return r.ExitCode;
        }

        // ---------------- export ----------------

        private static int ExportCmd(Args a, TextWriter output, TextWriter error)
        {
            string from = a.Str("from") ?? (a.Positional.Count > 0 ? a.Positional[0] : null) ?? throw new UsageException("export needs --from FILE_OR_DIR (a warehouse.sqlite, a run directory or a folder of runs)");
            string to = a.Str("to", "csv");
            string outDir = a.Str("out") ?? throw new UsageException("export needs --out DIR");
            var only = (a.Str("tables") ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            a.CheckAllUsed(1);
            if (to == "sqlite") { error.WriteLine("'export --to sqlite' (JSONL to SQLite) is not implemented; record the run again with 'replay FILE --record full --sqlite' (same mapper code, DATA_WAREHOUSE 4.6)."); return ExitCodes.NotImplemented; }
            if (to != "csv" && to != "parquet") throw new UsageException("--to takes csv or parquet");
            var dbs = new List<string>();
            if (File.Exists(from)) dbs.Add(from);
            else if (Directory.Exists(from)) dbs.AddRange(Directory.GetFiles(from, "warehouse.sqlite", SearchOption.AllDirectories).OrderBy(x => x, StringComparer.Ordinal));
            if (dbs.Count == 0) { error.WriteLine("no warehouse.sqlite under " + from); return ExitCodes.InvalidInput; }
            foreach (var db in dbs)
            {
                try
                {
                    if (to == "csv")
                    {
                        string runId; using (var d = WarehouseDb.OpenRead(db)) runId = (string)d.Scalar("SELECT run_id FROM dim_run LIMIT 1");
                        var counts = WarehouseExport.ToCsv(db, dbs.Count > 1 ? Path.Combine(outDir, runId) : outDir, only);
                        output.WriteLine("export " + db + ": " + counts.Count + " tables, " + counts.Values.Sum() + " rows to CSV");
                    }
                    else
                    {
                        var files = WarehouseExport.ToParquet(db, outDir, only);
                        output.WriteLine("export " + db + ": " + files.Count + " Parquet files under " + outDir);
                    }
                }
                catch (InvalidDataException e) { error.WriteLine("invalid warehouse: " + e.Message); return ExitCodes.InvalidInput; }
                catch (Microsoft.Data.Sqlite.SqliteException e) { error.WriteLine("invalid warehouse " + db + ": " + e.Message); return ExitCodes.InvalidInput; }
            }
            return ExitCodes.Ok;
        }

        // ---------------- bench ----------------

        private static int BenchCmd(Args a, TextWriter output, TextWriter error)
        {
            string cases = a.Str("cases", "bench_stage3_watch6");
            if (cases == "recording")
            {
                long rt = a.Long("ticks", 24000); string rout = a.Str("out");
                a.CheckAllUsed(0);
                var rb = RecordingBench.Run(Scenario.Parse(File.ReadAllText(FindScenario("bench_stage3_watch6")), new List<string>()), rt, a.ULong("seed") ?? 1, output);
                output.WriteLine(rb.ToString(Formatting.Indented));
                if (rout != null) { Directory.CreateDirectory(rout); File.WriteAllText(Path.Combine(rout, "bench_recording.json"), rb.ToString(Formatting.Indented)); }
                return ExitCodes.Ok;
            }
            var names = cases.Split(',');
            foreach (var n in names)
                if (n != "bench_stage3_watch6" && !n.StartsWith("bench_npc_")) throw new UsageException("bench cases: bench_stage3_watch6, bench_npc_120, bench_npc_300, bench_npc_500 (comma-separated)");
            long ticks = a.Long("ticks", 0), parTicks = a.Long("parallel-ticks", 0);
            ulong seed = a.ULong("seed") ?? 1;
            string workers = a.Str("workers");
            int[] w = workers?.Split(',').Select(x => int.TryParse(x, out int k) && k > 0 ? k : throw new UsageException("--workers takes a list like 1,4,16")).ToArray();
            var sets = a.All("set");
            if (names.Length > 1)
            {
                string outAll = a.Str("out");
                a.CheckAllUsed(0);
                var all = new JArray();
                foreach (var n in names)
                {
                    var scn = Scenario.Parse(File.ReadAllText(FindScenario(n)), sets);
                    bool npc = scn.Population;
                    var oo = new Bench.Options { Scenario = scn, Ticks = ticks > 0 ? ticks : npc ? 6000 : 40000, ParallelTicks = parTicks > 0 ? parTicks : npc ? 3000 : 20000, Log = output, Seed = seed, Workers = w ?? (npc ? new[] { 1, 4 } : null) };
                    var r = Bench.Run(oo);
                    r["gitCommit"] = GitInfo.Commit(); r["date"] = DateTime.UtcNow.ToString("yyyy-MM-dd");
                    all.Add(r);
                }
                output.WriteLine(all.ToString(Formatting.Indented));
                if (outAll != null) { Directory.CreateDirectory(outAll); File.WriteAllText(Path.Combine(outAll, "bench.json"), all.ToString(Formatting.Indented)); }
                return ExitCodes.Ok;
            }
            var sc = Scenario.Parse(File.ReadAllText(FindScenario(cases)), sets);
            var o = new Bench.Options { Scenario = sc, Ticks = ticks > 0 ? ticks : sc.Population ? 6000 : 40000, ParallelTicks = parTicks > 0 ? parTicks : sc.Population ? 3000 : 20000, Log = output, Seed = seed };
            if (w != null) o.Workers = w; else if (sc.Population) o.Workers = new[] { 1, 4 };
            string compare = a.Str("compare");
            bool update = a.Flag("update-baseline"), strict = a.Flag("strict");
            string baselinePath = a.Str("baseline", Path.Combine("DotNet", "God.Runner", "Bench", "baseline.json"));
            string outDir = a.Str("out");
            a.CheckAllUsed(0);
            JObject baseline = null;
            if (compare != null)
            {
                try { baseline = JObject.Parse(File.ReadAllText(compare)); }
                catch (Exception e) { throw new ScenarioException("cannot read the baseline " + compare + ": " + e.Message); }
            }
            var result = Bench.Run(o);
            result["gitCommit"] = GitInfo.Commit();
            result["date"] = DateTime.UtcNow.ToString("yyyy-MM-dd");
            output.WriteLine(result.ToString(Formatting.Indented));
            if (outDir != null) { Directory.CreateDirectory(outDir); File.WriteAllText(Path.Combine(outDir, "bench.json"), result.ToString(Formatting.Indented)); }
            if (update) { Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(baselinePath))); File.WriteAllText(baselinePath, result.ToString(Formatting.Indented)); output.WriteLine("baseline written to " + baselinePath); }
            return compare != null ? Bench.Compare(result, baseline, strict, output) : ExitCodes.Ok;
        }
    }
}
