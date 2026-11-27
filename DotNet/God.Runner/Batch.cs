using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using God.Recording;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace God.Runner
{
    /// <summary>One seed's outcome in a batch (what <c>batch.json</c> lists, in seed order).</summary>
    public sealed class BatchItem
    {
        public ulong Seed;
        public string RunId;
        public int ExitCode;
        public long Ticks;
        public string FinalHash;
        public string StopReason;
        public string RunDir;
        public string Error;
        public string SummaryJson;
    }

    /// <summary>
    /// Many seeds of one scenario (HEADLESS_SIMULATION 4.6): one sim per worker thread, or one child process per run with
    /// <c>--processes</c>. Runs share nothing; results are gathered and written in seed order, so finishing order never
    /// shows in any output. The batch's exit code is the highest of its runs.
    /// </summary>
    public static class Batch
    {
        public static List<BatchItem> Run(RunSpec template, IList<ulong> seeds, int workers, bool processes, TextWriter log, out int exitCode)
        {
            var items = new BatchItem[seeds.Count];
            workers = Math.Max(1, workers);
            if (!processes)
            {
                Parallel.For(0, seeds.Count, new ParallelOptions { MaxDegreeOfParallelism = workers, CancellationToken = CancellationToken.None }, i =>
                {
                    var spec = Verify.Copy(template); spec.Seed = seeds[i]; spec.Quiet = true; spec.Log = null;
                    var r = RunEngine.Run(spec);
                    items[i] = new BatchItem
                    {
                        Seed = seeds[i], RunId = r.RunId, ExitCode = r.ExitCode, Ticks = r.Ticks, FinalHash = Hex.Of(r.FinalHash), StopReason = r.StopReason,
                        RunDir = r.RunDir, Error = r.Error, SummaryJson = RunEngine.SummaryJson(r, spec.Scenario).ToString(Formatting.Indented),
                    };
                    lock (log) log.WriteLine("seed " + seeds[i] + " exit " + r.ExitCode + " tick " + r.Ticks + " " + r.StopReason + " hash " + Hex.Of(r.FinalHash));
                });
            }
            else RunProcesses(template, seeds, workers, items, log);

            exitCode = ExitCodes.Ok;
            foreach (var it in items) exitCode = ExitCodes.Worst(exitCode, it.ExitCode);
            if (template.OutRoot != null)
            {
                var arr = new JArray();
                foreach (var it in items)
                    arr.Add(new JObject { ["seed"] = it.Seed, ["runId"] = it.RunId, ["exitCode"] = it.ExitCode, ["ticks"] = it.Ticks, ["finalHash"] = it.FinalHash, ["stopReason"] = it.StopReason, ["error"] = it.Error });
                var batch = new JObject { ["scenario"] = template.Scenario.Name, ["scenarioHash"] = Hex.Of(template.Scenario.Hash()), ["workers"] = workers, ["processes"] = processes, ["exitCode"] = exitCode, ["runs"] = arr };
                try
                {
                    string dir = Path.Combine(template.OutRoot, template.Group ?? "adhoc", template.Scenario.Name);
                    Directory.CreateDirectory(dir);
                    File.WriteAllText(Path.Combine(dir, "batch.json"), batch.ToString(Formatting.Indented));
                    if (template.Sqlite != null) God.Runner.Warehouse.BatchIndex.Write(Path.Combine(dir, "batch.sqlite"), items.Select(i => i.RunDir).ToList());
                }
                catch (Exception e) { log.WriteLine("I/O error (batch.json): " + e.Message); exitCode = ExitCodes.Worst(exitCode, ExitCodes.IoError); }
            }
            return items.ToList();
        }

        /// <summary>The runner's own entry point for child processes: <c>dotnet God.Runner.dll</c>.</summary>
        public static string RunnerDll => typeof(Batch).Assembly.Location;

        private static void RunProcesses(RunSpec template, IList<ulong> seeds, int workers, BatchItem[] items, TextWriter log)
        {
            string tmp = Path.Combine(template.OutRoot ?? Path.GetTempPath(), "batch_" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".scenario.json");
            Directory.CreateDirectory(Path.GetDirectoryName(tmp));
            File.WriteAllText(tmp, template.Scenario.ToJson().ToString(Formatting.Indented));
            try
            {
                using (var gate = new SemaphoreSlim(workers))
                {
                    var tasks = new List<Task>();
                    for (int i = 0; i < seeds.Count; i++)
                    {
                        int idx = i;
                        gate.Wait();
                        tasks.Add(Task.Run(() =>
                        {
                            try { items[idx] = RunChild(template, tmp, seeds[idx]); }
                            finally { gate.Release(); }
                            lock (log) log.WriteLine("seed " + seeds[idx] + " exit " + items[idx].ExitCode + " (process)");
                        }));
                    }
                    Task.WaitAll(tasks.ToArray());
                }
            }
            finally { try { File.Delete(tmp); } catch { } }
        }

        private static BatchItem RunChild(RunSpec template, string scenarioFile, ulong seed)
        {
            var args = new List<string> { Quote(RunnerDll), "run", "--scenario", Quote(scenarioFile), "--seed", seed.ToString(), "--quiet", "--json" };
            if (template.OutRoot != null) { args.Add("--out"); args.Add(Quote(template.OutRoot)); args.Add("--group"); args.Add(template.Group ?? "adhoc"); }
            else args.Add("--no-out");
            if (template.Sqlite != null) { args.Add("--sqlite"); if (template.SkipJsonl) args.Add("--no-jsonl"); }
            if (template.Level.HasValue) { args.Add("--record"); args.Add(RecordingLevels.Name(template.Level.Value)); }
            if (template.ToTick > 0) { args.Add("--ticks"); args.Add(template.ToTick.ToString()); }
            if (template.CheckSmoke) args.Add("--check-smoke");
            var psi = new ProcessStartInfo("dotnet", string.Join(" ", args)) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
            if (template.GitCommit != null) psi.Environment["GOD_GIT_COMMIT"] = template.GitCommit;
            using (var p = Process.Start(psi))
            {
                var errTask = p.StandardError.ReadToEndAsync();
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                string err = errTask.Result;
                var item = new BatchItem { Seed = seed, ExitCode = p.ExitCode };
                var last = output.Split('\n').Select(x => x.Trim()).LastOrDefault(x => x.StartsWith("{"));
                if (last != null)
                {
                    var o = JObject.Parse(last);
                    item.RunId = (string)o["runId"]; item.Ticks = (long?)o["ticks"] ?? 0; item.FinalHash = (string)o["finalHash"];
                    item.StopReason = (string)o["stopReason"]; item.RunDir = (string)o["runDir"]; item.Error = (string)o["error"];
                    item.SummaryJson = o["summary"]?.ToString(Formatting.Indented);
                }
                else item.Error = "no result line from the child process: " + err;
                return item;
            }
        }

        private static string Quote(string s) => s.Contains(" ") ? "\"" + s + "\"" : s;
    }
}
