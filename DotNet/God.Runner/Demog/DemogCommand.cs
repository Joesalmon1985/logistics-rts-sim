using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using God.Recording;
using Newtonsoft.Json.Linq;

namespace God.Runner.Demog
{
    /// <summary>
    /// <c>demog report</c> (owner D4 Ã‚Â§10, M4.45): the multi-seed demographic report of one or more arms. Each arm is a scenario
    /// with <c>--set</c> overrides; every run is an ordinary <see cref="RunEngine"/> run (recording off unless asked) with a
    /// <see cref="DemogRecorder"/> beside it. Arms come from <c>--arm "name: path=value path=value"</c> (repeatable) or a plan
    /// file (<c>--plan FILE</c>: <c>{ "title", "scenario", "years", "seeds", "set": [..], "arms": [ { "name", "scenario"?, "set": [..] } ] }</c>).
    /// The report never changes a run: the same seeds give the same hashes with or without it.
    /// </summary>
    public static class DemogCommand
    {
        public const string Usage = @"demog report --scenario S [--seeds 1-20] [--years Y|--days D] [--set path=value]... [--arm ""name: path=value ...""]...
             [--plan FILE] [--workers N] [--out DIR] [--title T]";

        public sealed class ArmSpec
        {
            public string Name, Scenario;
            public List<string> Sets = new List<string>();
        }

        public static int Run(string[] argv, TextWriter output, TextWriter error, CancellationToken cancel)
        {
            if (argv.Length < 2 || argv[1] != "report") { error.WriteLine(Usage); return ExitCodes.Usage; }
            var a = new Args(argv, 2, new[] { "quiet" });
            string plan = a.Str("plan");
            string scenario = a.Str("scenario");
            string seedsArg = a.Str("seeds");
            long years = a.Long("years", 0), days = a.Long("days", 0);
            var baseSets = a.All("set").ToList();
            var armArgs = a.All("arm");
            int workers = (int)a.Long("workers", Math.Max(1, Environment.ProcessorCount - 1));
            string outDir = a.Str("out", Path.Combine("out", "demog"));
            string title = a.Str("title");
            bool quiet = a.Flag("quiet");
            a.CheckAllUsed(0);
            var arms = new List<ArmSpec>();
            if (plan != null)
            {
                var j = JObject.Parse(File.ReadAllText(plan));
                scenario = scenario ?? (string)j["scenario"];
                seedsArg = seedsArg ?? (string)j["seeds"];
                if (years == 0 && j["years"] != null) years = (long)j["years"];
                title = title ?? (string)j["title"];
                if (j["set"] is JArray bs) baseSets.InsertRange(0, bs.Select(x => (string)x));
                foreach (JObject arm in (JArray)j["arms"])
                {
                    var spec = new ArmSpec { Name = (string)arm["name"], Scenario = (string)arm["scenario"] };
                    if (arm["set"] is JArray s) spec.Sets.AddRange(s.Select(x => (string)x));
                    arms.Add(spec);
                }
            }
            foreach (string x in armArgs)
            {
                int colon = x.IndexOf(':');
                var spec = new ArmSpec { Name = colon < 0 ? x.Trim() : x.Substring(0, colon).Trim() };
                if (colon >= 0) spec.Sets.AddRange(x.Substring(colon + 1).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
                arms.Add(spec);
            }
            if (arms.Count == 0) arms.Add(new ArmSpec { Name = scenario ?? "default" });
            var seeds = Args.ParseSeeds(seedsArg ?? "1-10");
            title = title ?? ("demographic report: " + string.Join(", ", arms.Select(x => x.Name)));

            var jobs = new List<(int arm, ulong seed)>();
            for (int i = 0; i < arms.Count; i++) foreach (var s in seeds) jobs.Add((i, s));
            var results = new DemogRunSummary[jobs.Count];
            var scenarios = new Scenario[arms.Count];
            for (int i = 0; i < arms.Count; i++)
            {
                string path = Cli.FindScenario(arms[i].Scenario ?? scenario);
                var sc = Scenario.Parse(File.ReadAllText(path), baseSets.Concat(arms[i].Sets).ToList());
                if (years > 0) { sc.MaxTicks = 0; sc.MaxDays = 0; sc.MaxYears = years; }
                else if (days > 0) { sc.MaxTicks = 0; sc.MaxDays = days; sc.MaxYears = 0; }
                scenarios[i] = sc;
            }
            var log = TextWriter.Synchronized(output);
            Parallel.For(0, jobs.Count, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, workers) }, k =>
            {
                if (cancel.IsCancellationRequested) return;
                var (ai, seed) = jobs[k];
                var rec = new DemogRecorder();
                var spec = new RunSpec { Scenario = scenarios[ai], Seed = seed, OutRoot = null, WriteReplay = false, Quiet = true, Cancel = cancel, Level = RecordingLevel.Off };
                spec.ExtraRecorders.Add(() => rec);
                var r = RunEngine.Run(spec);
                long inv = r.Counters.TryGetValue("invariant_failures", out long v) ? v : 0;
                results[k] = DemogReport.Summarise(arms[ai].Name, seed, rec, r.ExitCode, (int)inv);
                if (!quiet) log.WriteLine($"{arms[ai].Name} seed {seed}: exit {r.ExitCode} day {rec.FinalDay} start {rec.Start} end {rec.Final} births {rec.Births} deaths {rec.Deaths} ({results[k].Outcome}) {r.WallSeconds:F0}s");
            });
            var grouped = new List<List<DemogRunSummary>>();
            for (int i = 0; i < arms.Count; i++) grouped.Add(results.Where((x, k) => x != null && jobs[k].arm == i).OrderBy(x => x.Seed).ToList());
            string text = DemogReport.Write(outDir, title, grouped);
            output.WriteLine(text);
            output.WriteLine("written: " + Path.GetFullPath(outDir));
            int code = ExitCodes.Ok;
            foreach (var x in results) if (x != null) code = ExitCodes.Worst(code, x.ExitCode);
            return code;
        }
    }
}
