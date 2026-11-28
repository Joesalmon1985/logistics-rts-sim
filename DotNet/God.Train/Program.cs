using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using God.Sim.Unified;
using God.Train.Artifact;
using God.Train.Curriculum;
using God.Train.Env;
using God.Train.Eval;
using God.Train.Honesty;
using God.Train.Learn;
using God.Train.Net;
using God.Train.Report;

namespace God.Train
{
    public static class Program
    {
        public static int Main(string[] args) => Cli.Run(args, Console.Out, Console.Error);
    }

    /// <summary>
    /// <c>god-train</c>: thresholds-hash, env-check, demo, bc, es, eval, artifact, report. Without <c>--stub</c> every command
    /// targets the real unified world and refuses (exit 3) until UN1/UN2/UN6 have merged. Results never depend on <c>--workers</c>.
    /// </summary>
    public static class Cli
    {
        public const int Ok = 0, Usage = 1, Refused = 2, Unavailable = 3;

        public static int Run(string[] args, TextWriter o, TextWriter e)
        {
            if (args.Length == 0) { e.WriteLine(Help); return Usage; }
            var a = Parse(args);
            try
            {
                switch (args[0])
                {
                    case "thresholds-hash": { var t = Thresholds.Load(Opt(a, "thresholds")); o.WriteLine(t.Hash); return Ok; }
                    case "env-check": return EnvCheck(a, o, e);
                    case "probe": return Probe(a, o, e);
                    case "dagger": return Dagger(a, o, e);
                    case "s1-report": return SliceReportCmd(a, o, e);
                    case "validate": return ValidateCmd(a, o, e);
                    case "demo": return Demo(a, o, e);
                    case "bc": return Bc(a, o, e);
                    case "es": return Es(a, o, e);
                    case "eval": return EvalCmd(a, o, e);
                    case "artifact": return ArtifactCmd(a, o, e);
                    case "report": return ReportCmd(a, o, e);
                    default: e.WriteLine(Help); return Usage;
                }
            }
            catch (HeldOutRefusedException x) { e.WriteLine(x.Message); return Refused; }
            catch (ThresholdsHashMismatchException x) { e.WriteLine(x.Message); return Refused; }
            catch (EvaluationAbortedException x) { e.WriteLine(x.Message); return Refused; }
        }

        public const string Help = "god-train <thresholds-hash|env-check|demo|bc|es|eval|artifact|report> [--stub] [--stage S2] [--seeds 1-8] [--workers N] [--out DIR] [--thresholds-hash H] ...";

        private static Dictionary<string, string> Parse(string[] args)
        {
            var d = new Dictionary<string, string>();
            for (int i = 1; i < args.Length; i++)
            {
                if (!args[i].StartsWith("--")) continue;
                string k = args[i].Substring(2);
                if (i + 1 < args.Length && !args[i + 1].StartsWith("--")) d[k] = args[++i]; else d[k] = "1";
            }
            return d;
        }

        private static string Opt(Dictionary<string, string> a, string k, string def = null) => a.TryGetValue(k, out var v) ? v : def;
        private static int Int(Dictionary<string, string> a, string k, int def) => a.TryGetValue(k, out var v) ? int.Parse(v) : def;
        private static bool Flag(Dictionary<string, string> a, string k) => a.ContainsKey(k);
        private static string OutDir(Dictionary<string, string> a) { var d = Opt(a, "out", "god-train-out"); Directory.CreateDirectory(d); return d; }

        private static bool RealWorldRefused(Dictionary<string, string> a, TextWriter e)
        {
            if (Flag(a, "stub")) return false;
            var why = UnifiedWorld.Unavailable(1);
            if (why == null) return false;
            e.WriteLine("unified world unavailable: " + why + ". Use --stub for the harness self-test (never a result).");
            return true;
        }

        private static int EnvCheck(Dictionary<string, string> a, TextWriter o, TextWriter e)
        {
            o.WriteLine($"Obs v{ObsSchema.Version} length {ObsSchema.Length} hash {ObsSchema.SchemaHash}");
            o.WriteLine($"Act v{ActSchema.Version} length {ActSchema.Length} hash {ActSchema.SchemaHash}");
            var shape = NetShape.Default;
            o.WriteLine($"network {shape.Text}: {shape.Count} weights, {shape.MacPerDecision} MAC per decision (budget {NetShape.MacBudget})");
            if (RealWorldRefused(a, e)) return Unavailable;
            long seed = Int(a, "seed", 1);
            var st = CurriculumData.Load(Opt(a, "stage", "S2"));
            var w = Pipeline.Stub(st, seed);
            var net = Checkpoint.Untrained(shape, 1, "stub").ToQNet();
            var sw = Stopwatch.StartNew();
            var ep = Rollout.Run(w, new NetActor(net), seed, Pipeline.Ticks(st), evaluation: false);
            double s = sw.Elapsed.TotalSeconds;
            o.WriteLine($"stub world (HARNESS SELF-TEST, not a result): stage {st.Id}, {st.Population} persons, {ep.Ticks} ticks, {ep.Decisions} decisions, {ep.Facts.Accepted}/{ep.Facts.Submissions} accepted, adults alive {ep.AdultsAliveAt(ep.Ticks)}/{ep.AdultsAtStart()}, return {ep.TotalReturn}, hash {ep.FinalHash:x16}");
            o.WriteLine($"rollout {s * 1000:0.0} ms: {ep.Ticks / Math.Max(s, 1e-9):0} ticks/s, {ep.Decisions / Math.Max(s, 1e-9):0} decisions/s (1 worker)");
            return Ok;
        }

        /// <summary>One real-world episode on a training seed (teacher or untrained), summarised per day: a diagnostic, never a result.</summary>
        private static int Probe(Dictionary<string, string> a, TextWriter o, TextWriter e)
        {
            long seed = Int(a, "seed", 1);
            var cfg = new UnifiedConfig { Persons = Int(a, "persons", 1), NeedMask = Convert.ToInt32(Opt(a, "mask", "0x033"), 16), Overlay = Int(a, "overlay", 1), Sample = Int(a, "sample", 1), TemperatureSpread = Int(a, "spread", 300) };
            var w = new UnifiedWorld(cfg, seed, Honesty.Layout.Train, null);
            string which = Opt(a, "actor", "teacher");
            IActor actor = which == "teacher" ? new Teacher.SurvivalTeacher() : which == "game" ? new GameBrainActor()
                : new NetActor((Opt(a, "ckpt") != null ? Checkpoint.Load(Opt(a, "ckpt")) : Checkpoint.Untrained(NetShape.Unified, 1, "unified")).ToQNet());
            int days = Int(a, "days", 3);
            var sw = Stopwatch.StartNew();
            var ep = Rollout.Run(w, actor, seed, (long)days * w.TicksPerDay, evaluation: false);
            double sec = sw.Elapsed.TotalSeconds;
            o.WriteLine($"probe (diagnostic, not a result): seed {seed}, {w.People.Count} persons, actor {which}, {days} days x {w.TicksPerDay} ticks, {sec:0.00} s ({ep.Ticks / Math.Max(sec, 1e-9):0} ticks/s), decisions {ep.Decisions}, accepted {w.Facts.Accepted}/{w.Facts.Submissions}; drinks {w.Drinks}, forages {w.Forages}, sleeps {w.Sleeps}");
            var line = new System.Text.StringBuilder("  applied by control:");
            for (int c = 1; c < 13; c++) if (w.Applied[c] + w.Failed[c] > 0) line.Append($" {(God.Sim.Unified.Prim)c} {w.Applied[c]}/{w.Applied[c] + w.Failed[c]}");
            o.WriteLine(line.ToString());
            o.WriteLine("  refused: " + string.Join(", ", System.Linq.Enumerable.Select(w.Refused, kv => kv.Key + " " + kv.Value)));
            if (actor is Teacher.SurvivalTeacher stt) o.WriteLine($"  teacher: steered by percept/memory {stt.Steered}, explored {stt.Explored}, oracle steps {stt.OracleSteps}");
            foreach (var p in ep.People)
                o.WriteLine($"  person {p.Person}: adult {p.Adult}, death tick {p.DeathTick} ({p.Cause}), cells {p.CellsVisited}, pain(h|t) {p.PainTicksHungerThirst}/{p.PersonTicks}, return {p.Return}");
            return Ok;
        }

        /// <summary>DAgger (TRAINING ONLY): the network acts on training seeds, the teacher labels; with --beta permille the teacher acts.</summary>
        private static int Dagger(Dictionary<string, string> a, TextWriter o, TextWriter e)
        {
            var seeds = SeedPolicy.ParseSeeds(Opt(a, "seeds", "1-8"));
            SeedPolicy.CheckTraining(seeds);
            if (RealWorldRefused(a, e)) return Unavailable;
            var st = CurriculumData.Load(Opt(a, "stage", "S2"));
            var net = Checkpoint.Load(Opt(a, "from")).ToQNet();
            int beta = Int(a, "beta", 300), workers = Int(a, "workers", 8), persons = Int(a, "persons", st.Population);
            long ticks = Int(a, "days", st.Days) * (long)God.Sim.SimCalendar.DefaultTicksPerDay;
            var per = new List<Sequence>[seeds.Length];
            var stats = new EpisodeStats[seeds.Length];
            System.Threading.Tasks.Parallel.For(0, seeds.Length, new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, workers) }, i =>
            {
                var rec = new List<Sequence>();
                var world = new UnifiedWorld(new UnifiedConfig { Persons = persons, NeedMask = st.NeedMask }, seeds[i], Layout.Train, null);
                stats[i] = Rollout.Run(world, new DaggerActor(net, new Teacher.SurvivalTeacher(), beta), seeds[i], ticks, evaluation: false, record: rec);
                per[i] = rec;
            });
            var all = new List<Sequence>(); foreach (var p in per) all.AddRange(p);
            var path = Path.Combine(OutDir(a), Opt(a, "name", "dagger.bin"));
            DemoFile.Write(path, all);
            long n = 0; foreach (var sq in all) n += sq.Samples.Count;
            long alive = 0, adults = 0, pain = 0, pt = 0;
            foreach (var x in stats) { alive += x.AdultsAliveAt(x.Ticks); adults += x.AdultsAtStart(); foreach (var p in x.People) { pain += p.PainTicksHungerThirst; pt += p.PersonTicks; } }
            o.WriteLine($"dagger (TRAINING ONLY, teacher labels, beta {beta}/1000): {all.Count} sequences, {n} decisions -> {path}; rollouts alive {alive}/{adults}, pain(h|t) {(double)pain / Math.Max(1, pt):0.000}");
            return Ok;
        }

        /// <summary>A stage's criteria on its validation seeds (training-side model selection; never held-out) for a checkpoint.</summary>
        private static int ValidateCmd(Dictionary<string, string> a, TextWriter o, TextWriter e)
        {
            bool stub = Flag(a, "stub");
            if (RealWorldRefused(a, e)) return Unavailable;
            var st = CurriculumData.Load(Opt(a, "stage", "S2"));
            // optional harder validation episodes (same validation seeds): more persons, more days
            if (a.ContainsKey("persons")) st.Population = Int(a, "persons", st.Population);
            if (a.ContainsKey("days")) st.Days = Int(a, "days", st.Days);
            if (a.ContainsKey("vseeds")) st.ValidationSeeds = Int(a, "vseeds", st.ValidationSeeds);
            var net = Checkpoint.Load(Opt(a, "ckpt")).ToQNet();
            var val = Pipeline.Validate(st, net, Int(a, "workers", 8), null, stub);
            o.WriteLine(Pipeline.CriteriaJson(st, CurriculumData.Check(st, val), double.NaN, Pipeline.MeanReturn(val), stub));
            return Ok;
        }

        private static int Demo(Dictionary<string, string> a, TextWriter o, TextWriter e)
        {
            var seeds = SeedPolicy.ParseSeeds(Opt(a, "seeds", "1-8"));
            SeedPolicy.CheckTraining(seeds);
            if (RealWorldRefused(a, e)) return Unavailable;
            var st = CurriculumData.Load(Opt(a, "stage", "S2"));
            bool stub = Flag(a, "stub");
            var demos = Pipeline.Demos(st, seeds, Int(a, "workers", 8), -1, stub);
            var path = Path.Combine(OutDir(a), Opt(a, "name", "demos.bin"));
            DemoFile.Write(path, demos);
            long n = 0; foreach (var s in demos) n += s.Samples.Count;
            o.WriteLine($"teacher demonstrations (TRAINING ONLY, origin Teacher): {demos.Count} sequences, {n} decisions -> {path}");
            return Ok;
        }

        private static int Bc(Dictionary<string, string> a, TextWriter o, TextWriter e)
        {
            var seeds = SeedPolicy.ParseSeeds(Opt(a, "seeds", "1-8"));
            SeedPolicy.CheckTraining(seeds);
            if (RealWorldRefused(a, e)) return Unavailable;
            var st = CurriculumData.Load(Opt(a, "stage", "S2"));
            int workers = Int(a, "workers", 8);
            var dir = OutDir(a);
            bool stub = Flag(a, "stub");
            List<Sequence> demos;
            if (Opt(a, "demos") != null) { demos = new List<Sequence>(); foreach (var f in Opt(a, "demos").Split(',')) demos.AddRange(DemoFile.Read(f)); }
            else demos = Pipeline.Demos(st, seeds, workers, -1, stub);
            var ckPath = Path.Combine(dir, "bc.ckpt");
            var start = Flag(a, "resume") && File.Exists(ckPath) ? Checkpoint.Load(ckPath) : Checkpoint.Untrained(stub ? NetShape.Default : NetShape.Unified, (ulong)Int(a, "init-seed", 1), stub ? "stub" : "unified");
            start.Stage = st.Id; start.SeedsText = "demo seeds " + Opt(a, "seeds", "1-8");
            var cfg = new BcConfig { Epochs = Int(a, "epochs", st.BcEpochs), Workers = workers, Batch = Int(a, "batch", 4), Lr = double.Parse(Opt(a, "lr", "0.003"), System.Globalization.CultureInfo.InvariantCulture) };
            var ck = BehaviourCloning.Train(start, demos, cfg, (start.Kind == "bc" ? start.Step : 0) + cfg.Epochs, (ep, loss) => o.WriteLine($"bc epoch {ep}: loss {loss:0.0000}"));
            var net = ck.ToQNet();
            var pn = new long[13]; var pk = new long[13];
            double agree = BehaviourCloning.Agreement(net, demos, pn, pk);
            o.WriteLine($"  float master agreement {BehaviourCloning.FloatAgreement(ck.NetShape, ck.P, demos):0.0000} (quantisation gap check only)");
            for (int c = 1; c < 13; c++) if (pn[c] > 0) o.WriteLine($"  agreement {(Prim)c}: {pk[c]}/{pn[c]}");
            var val = Pipeline.Validate(st, net, workers, null, stub);
            ck.MetricsJson = Pipeline.CriteriaJson(st, CurriculumData.Check(st, val), agree, Pipeline.MeanReturn(val), stub);
            ck.Save(ckPath);
            o.WriteLine($"bc: agreement {agree:0.0000} (integer network, teacher-forced); validation {ck.MetricsJson}");
            o.WriteLine($"frozen checkpoint -> {ckPath} (weights {net.WeightsSha256()})");
            return Ok;
        }

        private static int Es(Dictionary<string, string> a, TextWriter o, TextWriter e)
        {
            var seeds = SeedPolicy.ParseSeeds(Opt(a, "seeds", "1-64"));
            SeedPolicy.CheckTraining(seeds);
            if (RealWorldRefused(a, e)) return Unavailable;
            var st = CurriculumData.Load(Opt(a, "stage", "S2"));
            int workers = Int(a, "workers", 8);
            var dir = OutDir(a);
            var esPath = Path.Combine(dir, "es.ckpt");
            var from = Flag(a, "resume") && File.Exists(esPath) ? esPath : Opt(a, "from", Path.Combine(dir, "bc.ckpt"));
            var start = Checkpoint.Load(from);
            bool stub = Flag(a, "stub");
            var cfg = new EsConfig { Pairs = Int(a, "pairs", st.EsPairs), EpisodesPerCandidate = Int(a, "episodes", st.EsEpisodes), Workers = workers, Seeds = seeds, Ticks = a.ContainsKey("days") ? Int(a, "days", st.Days) * (long)(stub ? new StubConfig().TicksPerDay : God.Sim.SimCalendar.DefaultTicksPerDay) : Pipeline.Ticks(st, stub),
                Sigma = double.Parse(Opt(a, "sigma", "0.005"), System.Globalization.CultureInfo.InvariantCulture), Lr = double.Parse(Opt(a, "lr", "0.0005"), System.Globalization.CultureInfo.InvariantCulture) };
            int iters = Int(a, "iters", st.EsIterations);
            var sw = Stopwatch.StartNew();
            var ck = EvolutionStrategies.Train(start, s => Pipeline.World(st, s, stub), cfg, (start.Kind == "es" ? start.Step : 0) + iters, (it, mean, best) => o.WriteLine($"es iteration {it}: mean return {mean}, best {best}"));
            ck.Stage = st.Id; ck.SeedsText = start.Kind == "es" ? start.SeedsText : start.SeedsText + "; es seeds " + Opt(a, "seeds", "1-64");
            var net = ck.ToQNet();
            if (!Flag(a, "no-validate"))
            {
                var val = Pipeline.Validate(st, net, workers, null, stub);
                ck.MetricsJson = Pipeline.CriteriaJson(st, CurriculumData.Check(st, val), double.NaN, Pipeline.MeanReturn(val), stub);
            }
            ck.Save(esPath);
            long rollouts = (long)iters * cfg.Pairs * 2 * cfg.EpisodesPerCandidate;
            o.WriteLine($"es: {rollouts} rollouts in {sw.Elapsed.TotalSeconds:0.0} s ({rollouts / Math.Max(sw.Elapsed.TotalSeconds, 1e-9) / Math.Max(1, workers):0.0} rollouts/s per worker); validation {ck.MetricsJson}");
            o.WriteLine($"trained checkpoint -> {esPath} (weights {net.WeightsSha256()})");
            return Ok;
        }

        private static int EvalCmd(Dictionary<string, string> a, TextWriter o, TextWriter e)
        {
            var t = Thresholds.Load(Opt(a, "thresholds"));
            Evaluator.Authorise(t, Opt(a, "thresholds-hash"));
            if (RealWorldRefused(a, e)) return Unavailable;
            var dir = OutDir(a);
            bool stub = Flag(a, "stub");
            var seeds = SeedPolicy.ParseSeeds(Opt(a, "seeds", stub ? $"{t.SelfTestSeeds[0]}-{t.SelfTestSeeds[1]}" : $"{t.HeldOutSeeds[0]}-{t.HeldOutSeeds[1]}"));
            var arms = Opt(a, "arms", "untrained,frozen,trained").Split(',');
            var setup = new EvalSetup { World = stub ? "stub" : "unified", Seeds = seeds, Workers = Int(a, "workers", 8) };
            string stageId = null;
            foreach (var name in arms)
            {
                Checkpoint ck; string brain;
                if (!stub && name == "trained" && Opt(a, "artifact") != null)
                {
                    // the frozen artifact, loaded by the game through UN6's header check (Brain.Kind = 1)
                    int ver = Int(a, "artifact", 1);
                    var art = God.Sim.Unified.ArtifactLoader.LoadVerified(ver);
                    setup.Arms.Add(new ArmSpec { Name = name, Brain = "trained", WeightsSha256 = art.WeightsSha256, ArtifactStatus = "FounderPolicyV" + ver + " " + art.Status + " (game Brain.Kind = 1)",
                        Actor = () => new GameBrainActor(), GameBrainKind = God.Sim.Unified.BrainKind.Trained, GameArtifact = ver });
                    if (stageId == null) stageId = Opt(a, "stage");
                    continue;
                }
                if (name == "untrained") { ck = Checkpoint.Untrained(stub ? NetShape.Default : NetShape.Unified, t.UntrainedInitSeed, stub ? "stub" : "unified"); brain = "untrained"; }
                else if (name == "frozen") { ck = Checkpoint.Load(Opt(a, "frozen", Path.Combine(dir, "bc.ckpt"))); brain = ck.Kind == "bc" ? "frozen" : "not-frozen:" + ck.Kind; }
                else if (name == "trained") { ck = Checkpoint.Load(Opt(a, "trained", Path.Combine(dir, "es.ckpt"))); brain = ck.Kind == "es" ? "trained" : "not-trained:" + ck.Kind; }
                else { e.WriteLine("unknown arm " + name + " (arms: untrained, frozen, trained)"); return Usage; }
                if (ck.Kind != "init" && stageId == null) stageId = ck.Stage;
                var net = ck.ToQNet();
                string label = name == "frozen" ? "FROZEN (behaviour cloning)" : name.ToUpperInvariant();
                string status = name == "untrained" ? "same shape, init seed " + t.UntrainedInitSeed + ", injected" : name == "frozen" ? "last BC checkpoint, injected" : "checkpoint, injected";
                setup.Arms.Add(new ArmSpec { Name = name, Brain = brain, WeightsSha256 = net.WeightsSha256(), ArtifactStatus = status, Actor = () => new NetActor(net) { Label = label } });
            }
            setup.NeedMask = CurriculumData.Load(Opt(a, "stage", stageId ?? "S2")).NeedMask;
            var res = Evaluator.Run(t, Opt(a, "thresholds-hash"), setup);
            var path = Path.Combine(dir, "eval.json");
            res.Save(path);
            o.WriteLine($"{res.Label}: {res.Verdict.Headline}; thresholds {res.ThresholdsHash}; {res.RolloutSecondsTotal:0.0} s, {res.TicksPerSecondPerWorker:0} ticks/s per worker -> {path}");
            foreach (var arm in res.Arms)
            {
                var t1 = arm.Measures.Find(x => x.Id == "T1"); var t2 = arm.Measures.Find(x => x.Id == "T2");
                long ret = 0; foreach (var s in arm.PerSeed) ret += s.Return;
                o.WriteLine($"  {arm.Name}: T1 {t1.Value:0.000} {(t1.Met ? "met" : "not met")}, T2 {t2.Value:0.000} {(t2.Met ? "met" : "not met")}, mean return {ret / Math.Max(1, arm.PerSeed.Count)}");
            }
            return Ok;
        }

        private static int ArtifactCmd(Dictionary<string, string> a, TextWriter o, TextWriter e)
        {
            var ck = Checkpoint.Load(Opt(a, "from"));
            var h = new ArtifactHeader
            {
                Version = Int(a, "version", 1), Status = Opt(a, "status", ck.World == "stub" ? "SELF-TEST" : "INTERIM"),
                CatalogueHash = ArtifactWriter.CurrentCatalogueHash(), TuningHash = Opt(a, "tuning-hash", ck.World == "stub" ? "stub-world" : ""),
                CurriculumId = Opt(a, "curriculum", "founder-v1"), StagesTrained = Opt(a, "stages-trained", ck.Stage), StagesNotTrained = Opt(a, "stages-not-trained", ""),
                TrainingSeeds = ck.SeedsText, TrainerCommit = Opt(a, "trainer-commit", "unknown"), ThresholdsHash = Opt(a, "thresholds-hash", Thresholds.Load(Opt(a, "thresholds")).Hash),
                TrainingMetricsJson = Opt(a, "metrics-file") != null ? File.ReadAllText(Opt(a, "metrics-file")).Trim() : ck.MetricsJson,
            };
            if (ck.World == "stub" && h.Status != "SELF-TEST") { e.WriteLine("refused: a stub-world checkpoint can only produce a SELF-TEST artifact"); return Refused; }
            var text = ArtifactWriter.Write(h, ck.ToQNet());
            var path = Opt(a, "out", ArtifactWriter.FileName(h.Version));
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            File.WriteAllText(path, text);
            o.WriteLine($"artifact {ArtifactWriter.ClassName(h.Version)} ({h.Status}) weights {h.WeightsSha256} -> {path}");
            return Ok;
        }

        /// <summary>The slice-1 headless report (markdown) of an evaluated unified-world results file.</summary>
        private static int SliceReportCmd(Dictionary<string, string> a, TextWriter o, TextWriter e)
        {
            var t = Thresholds.Load(Opt(a, "thresholds"));
            var r = EvalResults.FromJson(File.ReadAllText(Opt(a, "results")));
            if (r.ThresholdsHash != t.Hash) { e.WriteLine($"refused: the results were evaluated against thresholds {r.ThresholdsHash}, the committed file is {t.Hash}"); return Refused; }
            var md = Report.SliceReport.Write(r, t);
            var path = Opt(a, "out", "SLICE1_REPORT.md");
            File.WriteAllText(path, md, new System.Text.UTF8Encoding(false));
            o.WriteLine("slice-1 report -> " + path);
            return Ok;
        }

        private static int ReportCmd(Dictionary<string, string> a, TextWriter o, TextWriter e)
        {
            var t = Thresholds.Load(Opt(a, "thresholds"));
            var r = EvalResults.FromJson(File.ReadAllText(Opt(a, "results")));
            if (r.ThresholdsHash != t.Hash) { e.WriteLine($"refused: the results were evaluated against thresholds {r.ThresholdsHash}, the committed file is {t.Hash}"); return Refused; }
            var md = ReportWriter.Write(r, t);
            var path = Opt(a, "out", "TRAINING_REPORT.md");
            File.WriteAllText(path, md);
            o.WriteLine(md.Split('\n')[0] + " / " + Verdict.Compute(r, t).Headline + " -> " + path);
            return Ok;
        }
    }
}
