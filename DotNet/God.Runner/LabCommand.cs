using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using God.Recording;
using God.Sim.Lab;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace God.Runner
{
    /// <summary>
    /// <c>god-runner lab ...</c> (HEADLESS_SIMULATION 4.12, STRATEGY_BENCHMARKS 4.8): the maths-only game-theory lab of
    /// <c>Sim/Lab</c>. Subcommands list, run, all, report, hash, and the study runs invade and sweep (<see cref="LabStudyCommand"/>). Exit 0 when every check passes, 1 when a check fails,
    /// 2 for usage errors, 8 for I/O errors.
    /// </summary>
    public static class LabCommand
    {
        private static readonly string[] Flags = { "check", "json", "quiet", "no-out", "help", "expected" };

        public const string Usage = @"usage:
  God.Runner lab list [--json]
  God.Runner lab run L1 [--seed N] [--seeds A..B] [--agents N] [--gens G] [--dynamics replicator|agents]
                        [--param key=value ...] [--out DIR] [--no-out] [--format jsonl|csv] [--json]
  God.Runner lab all [--check] [--seeds 1..5] [--workers N] [--out DIR] [--format jsonl|csv] [--json]
  God.Runner lab report DIR
  God.Runner lab hash L1 [--seed N]
" + LabStudyCommand.Usage;

        public static int Run(string[] argv, TextWriter output, TextWriter error, CancellationToken cancel)
        {
            if (argv.Length < 2 || argv[1] == "help" || argv[1] == "--help") { output.WriteLine(Usage); return argv.Length < 2 ? ExitCodes.Usage : ExitCodes.Ok; }
            string sub = argv[1];
            var a = new Args(argv, 2, Flags);
            switch (sub)
            {
                case "list": return List(a, output);
                case "run": return RunOne(a, output, cancel);
                case "all": return All(a, output, cancel);
                case "report": return Report(a, output);
                case "hash": return Hash(a, output);
                case "invade": return LabStudyCommand.Invade(a, output, cancel);
                case "sweep": return LabStudyCommand.Sweep(a, output, cancel);
                default: error.WriteLine("unknown lab subcommand '" + sub + "'\n" + Usage); return ExitCodes.Usage;
            }
        }

        // ---------------- list ----------------

        private static int List(Args a, TextWriter output)
        {
            bool json = a.Flag("json");
            a.CheckAllUsed(0);
            var all = LabRegistry.All();
            if (json)
            {
                var arr = new JArray(all.Select(e => new JObject
                {
                    ["id"] = e.Id, ["title"] = e.Title, ["source"] = e.Source, ["status"] = ReproStatusText.Code(e.Status),
                    ["statusLabel"] = ReproStatusText.Label(e.Status), ["statusText"] = e.StatusText, ["expected"] = e.ExpectedSummary,
                }));
                output.WriteLine(arr.ToString(Formatting.Indented));
                return ExitCodes.Ok;
            }
            foreach (var e in all)
            {
                output.WriteLine(e.Id + "  " + e.Title + "  (" + e.Source + ")");
                output.WriteLine("    status:   " + ReproStatusText.Label(e.Status) + " - " + e.StatusText);
                output.WriteLine("    expected: " + e.ExpectedSummary);
            }
            return ExitCodes.Ok;
        }

        // ---------------- config ----------------

        private static LabConfig Config(Args a, string id)
        {
            var cfg = new LabConfig { Experiment = id };
            cfg.Agents = (int)a.Long("agents", 0);
            cfg.Generations = (int)a.Long("gens", 0);
            if (cfg.Agents < 0 || cfg.Generations < 0) throw new UsageException("--agents and --gens must be positive");
            string dyn = a.Str("dynamics");
            if (dyn != null && dyn != "replicator" && dyn != "agents") throw new UsageException("--dynamics takes replicator or agents");
            cfg.Dynamics = dyn;
            foreach (var p in a.All("param"))
            {
                int eq = p.IndexOf('=');
                if (eq <= 0) throw new UsageException("--param takes key=value: " + p);
                cfg.Params.Add(new KeyValuePair<string, string>(p.Substring(0, eq), p.Substring(eq + 1)));
            }
            return cfg;
        }

        private static List<ulong> Seeds(Args a, List<ulong> def)
        {
            string seeds = a.Str("seeds");
            ulong? seed = a.ULong("seed");
            if (seeds != null && seed != null) throw new UsageException("give --seed or --seeds, not both");
            if (seeds != null) return Args.ParseSeeds(seeds);
            if (seed != null) return new List<ulong> { seed.Value };
            return def;
        }

        private static LabExperiment Find(string id)
        {
            var e = LabRegistry.Find(id);
            if (e == null) throw new UsageException("unknown experiment '" + id + "' (lab list shows them)");
            return e;
        }

        // ---------------- run ----------------

        private static int RunOne(Args a, TextWriter output, CancellationToken cancel)
        {
            if (a.Positional.Count < 1) throw new UsageException("lab run needs an experiment id (L1 ...)");
            var exp = Find(a.Positional[0]);
            var cfg = Config(a, exp.Id);
            var seeds = Seeds(a, new List<ulong> { 1 });
            string format = Format(a);
            bool noOut = a.Flag("no-out");
            string outRoot = noOut ? null : a.Str("out", "runs");
            bool json = a.Flag("json");
            a.CheckAllUsed(1);
            int code = ExitCodes.Ok;
            foreach (var s in seeds)
            {
                if (cancel.IsCancellationRequested) return ExitCodes.Interrupted;
                var c = cfg.Copy(); c.Seed = s;
                var sw = Stopwatch.StartNew();
                LabResult r;
                try { r = exp.Run(c); }
                catch (ArgumentException e) { throw new UsageException(e.Message); }
                sw.Stop();
                if (outRoot != null) Write(r, Path.Combine(outRoot, "lab", exp.Id, s.ToString(CultureInfo.InvariantCulture)), format, exp);
                if (json) output.WriteLine(ChecksJson(r, exp).ToString(Formatting.None));
                else PrintRun(r, exp, output, sw.Elapsed.TotalSeconds);
                if (!r.Pass) code = ExitCodes.CheckFailed;
            }
            return code;
        }

        private static string Format(Args a)
        {
            string f = a.Str("format", "jsonl");
            if (f != "jsonl" && f != "csv") throw new UsageException("--format takes jsonl or csv");
            return f;
        }

        private static void PrintRun(LabResult r, LabExperiment exp, TextWriter o, double seconds)
        {
            o.WriteLine(exp.Id + " " + exp.Title + " seed " + r.Seed + " [" + ReproStatusText.Label(r.Status) + "]: " + (r.Pass ? "PASS" : "FAIL")
                + ", hash " + LabHash.Hex(r.ResultHash) + ", " + seconds.ToString("0.00", CultureInfo.InvariantCulture) + " s");
            foreach (var c in r.Checks)
                o.WriteLine("  " + Mark(c) + " " + c.Name + ": " + c.Measured + " (expected " + c.Expected + ")" + (c.Note != null ? " - " + c.Note : ""));
        }

        private static string Mark(Check c) => c.Kind == CheckKind.Report ? "[report] " : (c.Pass ? (c.Kind == CheckKind.Amended ? "[amended pass]" : "[pass]") : (c.Kind == CheckKind.Amended ? "[amended FAIL]" : "[FAIL]"));

        // ---------------- all ----------------

        private static int All(Args a, TextWriter output, CancellationToken cancel)
        {
            bool check = a.Flag("check");
            var seeds = Seeds(a, new List<ulong> { 1, 2, 3, 4, 5 });
            int workers = (int)a.Long("workers", 1);
            if (workers < 1) throw new UsageException("--workers must be positive");
            string outRoot = a.Str("out");
            string format = Format(a);
            bool json = a.Flag("json");
            a.CheckAllUsed(0);
            var exps = LabRegistry.All();
            var jobs = new List<(LabExperiment exp, ulong seed)>();
            foreach (var e in exps) foreach (var s in seeds) jobs.Add((e, s));
            var results = new LabResult[jobs.Count];
            var times = new double[jobs.Count];
            var total = Stopwatch.StartNew();
            Action<int> runJob = i =>
            {
                if (cancel.IsCancellationRequested) return;
                var sw = Stopwatch.StartNew();
                results[i] = jobs[i].exp.Run(new LabConfig { Experiment = jobs[i].exp.Id, Seed = jobs[i].seed });
                times[i] = sw.Elapsed.TotalSeconds;
            };
            if (workers == 1) for (int i = 0; i < jobs.Count; i++) runJob(i);
            else Parallel.For(0, jobs.Count, new ParallelOptions { MaxDegreeOfParallelism = workers }, runJob);
            total.Stop();
            if (cancel.IsCancellationRequested) return ExitCodes.Interrupted;

            if (outRoot != null)
            {
                for (int i = 0; i < jobs.Count; i++)
                    Write(results[i], Path.Combine(outRoot, "lab", jobs[i].exp.Id, jobs[i].seed.ToString(CultureInfo.InvariantCulture)), format, jobs[i].exp);
                Directory.CreateDirectory(outRoot);
                File.WriteAllText(Path.Combine(outRoot, "lab", "lab_report.md"), ReportMarkdown(results.Select((r, i) => (r, jobs[i].exp)).ToList()));
            }
            bool pass = results.All(r => r.Pass);
            if (json)
            {
                output.WriteLine(new JObject
                {
                    ["pass"] = pass, ["seconds"] = Math.Round(total.Elapsed.TotalSeconds, 2), ["workers"] = workers,
                    ["runs"] = new JArray(results.Select((r, i) => new JObject { ["experiment"] = r.Experiment, ["seed"] = r.Seed, ["pass"] = r.Pass, ["resultHash"] = LabHash.Hex(r.ResultHash), ["seconds"] = Math.Round(times[i], 3) })),
                }.ToString(Formatting.None));
            }
            else
            {
                output.Write(ReportMarkdown(results.Select((r, i) => (r, jobs[i].exp)).ToList()));
                output.WriteLine();
                output.WriteLine("| Experiment | Seconds (all seeds) |");
                output.WriteLine("|---|---|");
                foreach (var e in exps)
                    output.WriteLine("| " + e.Id + " | " + jobs.Select((j, i) => (j, i)).Where(t => t.j.exp == e).Sum(t => times[t.i]).ToString("0.00", CultureInfo.InvariantCulture) + " |");
                output.WriteLine();
                output.WriteLine("lab all: " + exps.Length + " experiments x " + seeds.Count + " seeds = " + jobs.Count + " runs, "
                    + results.Count(r => r.Pass) + " pass, " + results.Count(r => !r.Pass) + " fail, "
                    + total.Elapsed.TotalSeconds.ToString("0.0", CultureInfo.InvariantCulture) + " s on " + workers + " worker(s)");
            }
            return check && !pass ? ExitCodes.CheckFailed : ExitCodes.Ok;
        }

        /// <summary>A Markdown table: one row per check, measured values over the seeds and the pass count.</summary>
        public static string ReportMarkdown(List<(LabResult r, LabExperiment e)> runs)
        {
            var sb = new StringBuilder();
            sb.AppendLine("| Experiment | Status | Check | Kind | Expected | Actual (per seed) | Result |");
            sb.AppendLine("|---|---|---|---|---|---|---|");
            foreach (var g in runs.GroupBy(x => x.r.Experiment))
            {
                var list = g.ToList();
                var first = list[0].r;
                for (int ci = 0; ci < first.Checks.Length; ci++)
                {
                    var c = first.Checks[ci];
                    var per = list.Select(x => x.r.Checks.Length > ci ? x.r.Checks[ci] : null).Where(x => x != null).ToList();
                    string actual = string.Join(" / ", per.Select(x => x.Measured).Distinct().Count() == 1 ? new[] { per[0].Measured } : per.Select(x => x.Measured).ToArray());
                    int passes = per.Count(x => x.Pass);
                    string result = c.Kind == CheckKind.Report ? "report" : (passes == per.Count ? "pass" : "FAIL") + (per.Count > 1 ? " " + passes + "/" + per.Count : "");
                    if (c.Kind == CheckKind.Amended) result = "amended, " + result;
                    sb.AppendLine("| " + first.Experiment + " | " + ReproStatusText.Label(c.Status) + " | " + Esc(c.Name) + " | " + c.Kind.ToString().ToLowerInvariant()
                        + " | " + Esc(c.Expected) + " | " + actual + " | " + result + " |");
                }
            }
            return sb.ToString();
        }

        private static string Esc(string s) => s == null ? "" : s.Replace("|", "\\|");

        // ---------------- files ----------------

        public static JObject ChecksJson(LabResult r, LabExperiment exp)
        {
            return new JObject
            {
                ["experiment"] = r.Experiment, ["title"] = exp?.Title, ["seed"] = r.Seed, ["status"] = ReproStatusText.Code(r.Status),
                ["statusLabel"] = ReproStatusText.Label(r.Status), ["statusText"] = r.StatusText, ["pass"] = r.Pass,
                ["resultHash"] = LabHash.Hex(r.ResultHash), ["series"] = r.SeriesTitle,
                ["checks"] = new JArray(r.Checks.Select(c => new JObject
                {
                    ["name"] = c.Name, ["kind"] = c.Kind.ToString().ToLowerInvariant(), ["status"] = ReproStatusText.Code(c.Status),
                    ["measured"] = c.Measured, ["measuredMicro"] = c.MeasuredMicro, ["expected"] = c.Expected, ["pass"] = c.Pass,
                    ["source"] = c.Source, ["note"] = c.Note,
                })),
                ["notes"] = new JArray(r.Notes),
            };
        }

        public static void Write(LabResult r, string dir, string format, LabExperiment exp)
        {
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "checks.json"), ChecksJson(r, exp).ToString(Formatting.Indented));
            if (format == "csv")
            {
                var sb = new StringBuilder();
                sb.Append("gen");
                foreach (var s in r.Strategies) sb.Append(",share_" + Col(s));
                if (r.MeanPayoffX1000.Length > 0) foreach (var s in r.Strategies) sb.Append(",payoff_" + Col(s));
                sb.AppendLine();
                for (int g = 0; g < r.SharesPerGeneration.Length; g++)
                {
                    sb.Append(g.ToString(CultureInfo.InvariantCulture));
                    foreach (var v in r.SharesPerGeneration[g]) sb.Append(",").Append(Fx.Format(v, 6));
                    if (g < r.MeanPayoffX1000.Length) foreach (var v in r.MeanPayoffX1000[g]) sb.Append(",").Append(Fx.FormatScaled(v, 3));
                    sb.AppendLine();
                }
                File.WriteAllText(Path.Combine(dir, "result.csv"), sb.ToString());
                var cs = new StringBuilder("experiment,seed,status,check,kind,measured,expected,pass,source\n");
                foreach (var c in r.Checks)
                    cs.Append(r.Experiment).Append(',').Append(r.Seed).Append(',').Append(ReproStatusText.Code(c.Status)).Append(',').Append(Csv(c.Name)).Append(',')
                      .Append(c.Kind.ToString().ToLowerInvariant()).Append(',').Append(c.Measured).Append(',').Append(Csv(c.Expected)).Append(',').Append(c.Pass ? "true" : "false").Append(',').Append(Csv(c.Source)).Append('\n');
                File.WriteAllText(Path.Combine(dir, "checks.csv"), cs.ToString());
            }
            else
            {
                var sb = new StringBuilder();
                sb.AppendLine(new JObject { ["record"] = "LabRunStarted", ["experiment"] = r.Experiment, ["seed"] = r.Seed, ["status"] = ReproStatusText.Code(r.Status), ["series"] = r.SeriesTitle, ["strategies"] = new JArray(r.Strategies) }.ToString(Formatting.None));
                for (int g = 0; g < r.SharesPerGeneration.Length; g++)
                {
                    var row = new JObject { ["record"] = "LabGeneration", ["gen"] = g, ["shares"] = new JArray(r.SharesPerGeneration[g].Select(v => Fx.Format(v, 6))) };
                    if (g < r.MeanPayoffX1000.Length) row["meanPayoff"] = new JArray(r.MeanPayoffX1000[g].Select(v => Fx.FormatScaled(v, 3)));
                    sb.AppendLine(row.ToString(Formatting.None));
                }
                foreach (var c in r.Checks)
                    sb.AppendLine(new JObject { ["record"] = "LabCheck", ["name"] = c.Name, ["measured"] = c.Measured, ["expected"] = c.Expected, ["pass"] = c.Pass, ["kind"] = c.Kind.ToString().ToLowerInvariant() }.ToString(Formatting.None));
                sb.AppendLine(new JObject { ["record"] = "LabRunEnded", ["resultHash"] = LabHash.Hex(r.ResultHash), ["pass"] = r.Pass }.ToString(Formatting.None));
                File.WriteAllText(Path.Combine(dir, "result.jsonl"), sb.ToString());
            }
            var sum = new StringWriter();
            PrintRun(r, exp, sum, 0);
            sum.WriteLine("status: " + ReproStatusText.Label(r.Status) + " - " + r.StatusText);
            foreach (var n in r.Notes) sum.WriteLine("note: " + n);
            File.WriteAllText(Path.Combine(dir, "summary.txt"), sum.ToString());
        }

        private static string Col(string s) => new string(s.Select(ch => char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '_').ToArray());
        private static string Csv(string s) => s == null ? "" : (s.IndexOfAny(new[] { ',', '"', '\n' }) >= 0 ? "\"" + s.Replace("\"", "\"\"") + "\"" : s);

        // ---------------- report ----------------

        private static int Report(Args a, TextWriter output)
        {
            if (a.Positional.Count < 1) throw new UsageException("lab report needs a DIR");
            string dir = a.Positional[0];
            a.CheckAllUsed(1);
            if (!Directory.Exists(dir)) throw new IOException("no directory " + dir);
            var files = Directory.GetFiles(dir, "checks.json", SearchOption.AllDirectories).OrderBy(f => f, StringComparer.Ordinal).ToList();
            if (files.Count == 0) { output.WriteLine("no checks.json under " + dir); return ExitCodes.InvalidInput; }
            var rows = new List<JObject>();
            foreach (var f in files)
            {
                try { rows.Add(JObject.Parse(File.ReadAllText(f))); }
                catch (JsonException e) { throw new ScenarioException("bad lab result " + f + ": " + e.Message); }
            }
            output.WriteLine("| Experiment | Seed | Status | Pass | Failed checks | Result hash |");
            output.WriteLine("|---|---|---|---|---|---|");
            bool all = true;
            foreach (var o in rows.OrderBy(o => (string)o["experiment"], StringComparer.Ordinal).ThenBy(o => (ulong)o["seed"]))
            {
                bool p = (bool)o["pass"];
                all &= p;
                var failed = ((JArray)o["checks"]).Where(c => !(bool)c["pass"] && (string)c["kind"] != "report").Select(c => (string)c["name"] + " = " + (string)c["measured"]).ToList();
                output.WriteLine("| " + o["experiment"] + " | " + o["seed"] + " | " + o["statusLabel"] + " | " + (p ? "pass" : "FAIL") + " | " + Esc(string.Join("; ", failed)) + " | " + o["resultHash"] + " |");
            }
            return all ? ExitCodes.Ok : ExitCodes.CheckFailed;
        }

        // ---------------- hash ----------------

        private static int Hash(Args a, TextWriter output)
        {
            if (a.Positional.Count < 1) throw new UsageException("lab hash needs an experiment id");
            var exp = Find(a.Positional[0]);
            var cfg = Config(a, exp.Id);
            var seeds = Seeds(a, new List<ulong> { 1 });
            a.CheckAllUsed(1);
            foreach (var s in seeds)
            {
                var c = cfg.Copy(); c.Seed = s;
                output.WriteLine(exp.Id + " " + s + " " + LabHash.Hex(exp.Run(c).ResultHash));
            }
            return ExitCodes.Ok;
        }
    }
}
