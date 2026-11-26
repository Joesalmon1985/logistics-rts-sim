using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using God.Recording;
using God.Sim;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace God.Runner
{
    /// <summary>
    /// The benchmark (HEADLESS_SIMULATION 4.10): the stage 3 game with no NPCs (<c>bench_stage3_watch6</c>, a sandbox so it
    /// never ends early), recording Off. Measures ticks per second, sim years per real second, the tick-time spread, the
    /// allocation rate, memory, save size and save/load time, and the throughput of 1, 4 and N parallel sims.
    /// </summary>
    public static class Bench
    {
        public const double MinimumTicksPerSecond = 2000;
        public const double RegressionTolerance = 0.15;

        public sealed class Options
        {
            public Scenario Scenario;
            public ulong Seed = 1;
            public long Ticks = 40000;
            /// <summary>Ticks each parallel sim runs.</summary>
            public long ParallelTicks = 20000;
            public int[] Workers;
            public TextWriter Log = TextWriter.Null;
        }

        public static JObject Run(Options o)
        {
            var sc = o.Scenario;
            var setup = sc.ToSetup(o.Seed);
            var tuning = sc.ToTuning();
            // warm-up: JIT and the shared topology caches
            {
                var w = SimBuilder.Create(sc.ToSetup(o.Seed + 1000), tuning).Sim;
                for (int i = 0; i < 12000; i++) { w.Step(); w.DrainEvents(); } // past tiered JIT's first tiers
            }
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            long memBefore = GC.GetTotalMemory(true);
            var built = SimBuilder.Create(setup, tuning);
            var sim = built.Sim;
            long memAfterBuild = GC.GetTotalMemory(true);
            var times = new double[o.Ticks];
            var events = new List<SimEvent>(256);
            long alloc0 = GC.GetAllocatedBytesForCurrentThread();
            double tickToMs = 1000.0 / Stopwatch.Frequency;
            var total = Stopwatch.StartNew();
            long done = 0;
            for (; done < o.Ticks && !sim.State.IsGameOver; done++)
            {
                long t0 = Stopwatch.GetTimestamp();
                sim.Step();
                events.Clear(); sim.DrainInto(events);
                times[done] = (Stopwatch.GetTimestamp() - t0) * tickToMs;
            }
            total.Stop();
            long alloc = GC.GetAllocatedBytesForCurrentThread() - alloc0;
            long memEnd = GC.GetTotalMemory(false);
            long memEndCollected = GC.GetTotalMemory(true);
            var ms = times.Take((int)done).OrderBy(x => x).ToArray();
            double tps = done / total.Elapsed.TotalSeconds;
            double ticksPerYear = (double)setup.TicksPerDay * setup.DaysPerYear;

            // save size and times
            var sw = Stopwatch.StartNew();
            var bytes = RunEngine.SaveBytes(sim);
            double saveMs = sw.Elapsed.TotalMilliseconds;
            sw.Restart();
            var loaded = SimBuilder.FromSave(SaveFile.FromJson(Encoding.UTF8.GetString(bytes)));
            double loadMs = sw.Elapsed.TotalMilliseconds;
            bool loadedSame = loaded.Sim.StateHash() == sim.StateHash();

            var result = new JObject
            {
                ["case"] = sc.Name, ["seed"] = o.Seed, ["ticks"] = done,
                ["ticksPerSecond"] = Math.Round(tps, 1),
                ["realTimeMultiple"] = Math.Round(tps / GodSim.TicksPerSecond, 1),
                ["simYearsPerRealSecond"] = Math.Round(tps / ticksPerYear, 4),
                ["msPerTickMean"] = Math.Round(ms.Average(), 4),
                ["msPerTickP50"] = Math.Round(Pct(ms, 0.50), 4),
                ["msPerTickP99"] = Math.Round(Pct(ms, 0.99), 4),
                ["msPerTickMax"] = Math.Round(ms.Last(), 3),
                ["allocBytesPerTick"] = done > 0 ? alloc / done : 0,
                ["memoryBuildBytes"] = memAfterBuild - memBefore,
                ["memoryEndBytes"] = memEndCollected - memBefore,
                ["memoryEndUncollectedBytes"] = memEnd - memBefore,
                ["processWorkingSetBytes"] = Process.GetCurrentProcess().WorkingSet64,
                ["saveBytes"] = bytes.Length, ["saveMs"] = Math.Round(saveMs, 1), ["loadMs"] = Math.Round(loadMs, 1), ["loadSameHash"] = loadedSame,
                ["machine"] = Environment.MachineName.Length > 0 ? "cores " + Environment.ProcessorCount : "", ["runtime"] = Environment.Version.ToString(),
            };
            if (sim.State.Population != null) AddPopulationColumns(o, sc, sim, result, done, total.Elapsed.TotalSeconds, memEndCollected);
            o.Log.WriteLine("single: " + done + " ticks, " + tps.ToString("0") + " ticks/s, mean " + ms.Average().ToString("0.000") + " ms, p99 " + Pct(ms, 0.99).ToString("0.000") + " ms, max " + ms.Last().ToString("0.0") + " ms");

            // parallel throughput
            var par = new JArray();
            double single = 0;
            foreach (int w in (o.Workers ?? new[] { 1, 4, Environment.ProcessorCount }).Distinct())
            {
                var watch = Stopwatch.StartNew();
                var counts = new long[w];
                Parallel.For(0, w, new ParallelOptions { MaxDegreeOfParallelism = w }, i =>
                {
                    // every worker plays the same seed, so the scaling measures interference between sims, not different games
                    var s = SimBuilder.Create(sc.ToSetup(o.Seed), sc.ToTuning()).Sim;
                    var ev = new List<SimEvent>(256);
                    long n = 0;
                    for (; n < o.ParallelTicks && !s.State.IsGameOver; n++) { s.Step(); ev.Clear(); s.DrainInto(ev); }
                    counts[i] = n;
                });
                watch.Stop();
                double agg = counts.Sum() / watch.Elapsed.TotalSeconds;
                if (w == 1) single = agg;
                double eff = single > 0 ? agg / (w * single) : 0;
                par.Add(new JObject { ["workers"] = w, ["aggregateTicksPerSecond"] = Math.Round(agg, 0), ["perSimTicksPerSecond"] = Math.Round(agg / w, 0), ["scaling"] = Math.Round(eff, 3) });
                o.Log.WriteLine("parallel " + w + ": " + agg.ToString("0") + " ticks/s in total, " + (agg / w).ToString("0") + " per sim, scaling " + eff.ToString("0.00"));
            }
            result["parallel"] = par;
            return result;
        }

        /// <summary>
        /// The owner's NPC columns (HEADLESS 4.10, ACCEPTANCE M3.24): NPC decisions per second; memory per NPC (the run's
        /// collected heap minus the same setup without a population after the same ticks, over the NPCs alive); the store's
        /// fixed bytes per NPC; learning cost per think (the same run with learning Off for the same ticks: the time
        /// difference over the thinks); path searches per tick; consolidation runs (sleep starts) per sim day.
        /// </summary>
        private static void AddPopulationColumns(Options o, Scenario sc, GodSim sim, JObject result, long ticks, double seconds, long memEnd)
        {
            var pop = SimBuilder.PopulationOf(sim);
            var p = sim.State.Population;
            int alive = p.AliveCount;
            result["npcs"] = p.Npcs.Count; result["npcsAlive"] = alive;
            result["npcDecisionsPerSecond"] = Math.Round(pop.Thinks / seconds, 0);
            result["npcThinks"] = pop.Thinks;
            result["pathSearchesPerTick"] = Math.Round((double)pop.Pather.Searches / Math.Max(1, ticks), 3);
            result["pathNodesExpandedPerSearch"] = pop.Pather.Searches > 0 ? Math.Round((double)pop.Pather.Expanded / pop.Pather.Searches, 1) : 0;
            result["learningStoreBytesPerNpc"] = p.Npcs.First(n => n.Mind != null).Mind.Store.Bytes;
            result["sleepsPerSimDay"] = Math.Round(p.Stats.Sleeps * (double)sc.TicksPerDay / Math.Max(1, ticks), 1);

            // memory per NPC: the same setup without a population, after the same ticks
            var setup0 = sc.ToSetup(o.Seed); setup0.Population = false;
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            long m0 = GC.GetTotalMemory(true);
            var plain = SimBuilder.Create(setup0, sc.ToTuning()).Sim;
            for (long i = 0; i < ticks; i++) { plain.Step(); plain.DrainEvents(); }
            long plainBytes = GC.GetTotalMemory(true) - m0;
            GC.KeepAlive(plain);
            result["memoryPerNpcBytes"] = alive > 0 ? (memEnd - plainBytes) / alive : 0;

            // learning cost per think: the same game with learning Off, timed over the same ticks
            var tOff = sc.ToTuning(); tOff.Learning.DefaultMode = God.Sim.Learning.LearningMode.Off;
            var off = SimBuilder.Create(sc.ToSetup(o.Seed), tOff).Sim;
            var ev = new List<SimEvent>(256);
            var sw = Stopwatch.StartNew();
            for (long i = 0; i < ticks; i++) { off.Step(); ev.Clear(); off.DrainInto(ev); }
            sw.Stop();
            long offThinks = SimBuilder.PopulationOf(off).Thinks;
            double usOn = seconds * 1e6 / Math.Max(1, pop.Thinks), usOff = sw.Elapsed.TotalSeconds * 1e6 / Math.Max(1, offThinks);
            result["microsPerThinkOn"] = Math.Round(usOn, 2);
            result["microsPerThinkOff"] = Math.Round(usOff, 2);
            result["learningMicrosPerThink"] = Math.Round(usOn - usOff, 2);
            result["learningShareOfThink"] = usOn > 0 ? Math.Round((usOn - usOff) / usOn, 3) : 0;
            o.Log.WriteLine("npcs " + alive + ": " + pop.Thinks / seconds + " decisions/s, memory/NPC " + result["memoryPerNpcBytes"] + " B, learning " + (usOn - usOff).ToString("0.00") + " us/think of " + usOn.ToString("0.00"));

            // M3b: the population update timed by phase (host profiler), On and Off over the same ticks. Learning is the
            // learning-only phases (credit and close, the update, consolidation) plus the extra cognition and decision time
            // On spends over Off per think (conjunction matching, Q lookups); the share is of the NPC update time.
            var phOn = PhaseRun(sc.ToSetup(o.Seed), sc.ToTuning(), ticks, out long thinksOn);
            var phOff = PhaseRun(sc.ToSetup(o.Seed), tOff, ticks, out long thinksOff);
            double Per(double[] ph, long th, int k) => ph[k] * 1e6 / Math.Max(1, th);
            var phases = new JObject();
            for (int k = 0; k < God.Sim.Population.PopPhase.Count; k++)
                phases[God.Sim.Population.PopPhase.Names[k]] = new JObject { ["usPerThinkOn"] = Math.Round(Per(phOn, thinksOn, k), 3), ["usPerThinkOff"] = Math.Round(Per(phOff, thinksOff, k), 3) };
            result["populationPhases"] = phases;
            double npcUpdate = phOn.Sum() * 1e6 / Math.Max(1, thinksOn);
            int[] learnOnly = { God.Sim.Population.PopPhase.Credit, God.Sim.Population.PopPhase.LearnUpdate, God.Sim.Population.PopPhase.Consolidate };
            int[] mixed = { God.Sim.Population.PopPhase.Cognition, God.Sim.Population.PopPhase.Decide };
            double learn = learnOnly.Sum(k => Per(phOn, thinksOn, k)) + Math.Max(0, mixed.Sum(k => Per(phOn, thinksOn, k) - Per(phOff, thinksOff, k)));
            result["npcUpdateMicrosPerThink"] = Math.Round(npcUpdate, 2);
            result["learningMicrosPerThinkByPhase"] = Math.Round(learn, 2);
            result["learningShareOfNpcUpdate"] = npcUpdate > 0 ? Math.Round(learn / npcUpdate, 3) : 0;
            result["consolidationMsPerSimDay"] = Math.Round(phOn[God.Sim.Population.PopPhase.Consolidate] * 1000 * sc.TicksPerDay / Math.Max(1, ticks), 3);
            o.Log.WriteLine("population update " + npcUpdate.ToString("0.00") + " us/think; learning " + learn.ToString("0.00") + " us/think (" + (npcUpdate > 0 ? 100 * learn / npcUpdate : 0).ToString("0.0") + "%)");
        }

        private sealed class PhaseTimer : God.Sim.Population.IPopulationProfiler
        {
            public readonly long[] Ticks = new long[God.Sim.Population.PopPhase.Count];
            private long _last;
            public void Begin() => _last = Stopwatch.GetTimestamp();
            public void Mark(int phase) { long now = Stopwatch.GetTimestamp(); Ticks[phase] += now - _last; _last = now; }
        }

        /// <summary>Runs a game for <paramref name="ticks"/> ticks with the phase profiler; seconds spent per phase.</summary>
        private static double[] PhaseRun(GameSetup setup, SimTuning tuning, long ticks, out long thinks)
        {
            var sim = SimBuilder.Create(setup, tuning).Sim;
            var pop = SimBuilder.PopulationOf(sim);
            var timer = new PhaseTimer();
            pop.Profiler = timer;
            var ev = new List<SimEvent>(256);
            for (long i = 0; i < ticks; i++) { sim.Step(); ev.Clear(); sim.DrainInto(ev); }
            thinks = pop.Thinks;
            return timer.Ticks.Select(t => (double)t / Stopwatch.Frequency).ToArray();
        }

        private static double Pct(double[] sorted, double p)
        {
            if (sorted.Length == 0) return 0;
            int i = (int)Math.Ceiling(p * sorted.Length) - 1;
            return sorted[Math.Max(0, Math.Min(sorted.Length - 1, i))];
        }

        /// <summary>
        /// The regression gate: fails (exit 7) when ticks per second fall more than 15% below the baseline or below the
        /// 2,000 ticks/s minimum. Enforced only on the reference machine (<c>GOD_REFERENCE_MACHINE=1</c>) or with <c>--strict</c>;
        /// elsewhere it prints a warning and passes.
        /// </summary>
        public static int Compare(JObject result, JObject baseline, bool strict, TextWriter log)
        {
            double now = (double)result["ticksPerSecond"];
            double was = (double?)baseline?["ticksPerSecond"] ?? 0;
            var problems = new List<string>();
            if (now < MinimumTicksPerSecond) problems.Add("below the minimum: " + now.ToString("0") + " < " + MinimumTicksPerSecond + " ticks/s");
            if (was > 0 && now < was * (1 - RegressionTolerance)) problems.Add("more than 15% below the baseline: " + now.ToString("0") + " vs " + was.ToString("0") + " ticks/s");
            if (problems.Count == 0) { log.WriteLine("bench: within budget (" + now.ToString("0") + " ticks/s, baseline " + was.ToString("0") + ")"); return ExitCodes.Ok; }
            bool enforce = strict || Environment.GetEnvironmentVariable("GOD_REFERENCE_MACHINE") == "1";
            foreach (var p in problems) log.WriteLine((enforce ? "bench FAIL: " : "bench WARNING (not the reference machine): ") + p);
            return enforce ? ExitCodes.PerformanceMissed : ExitCodes.Ok;
        }
    }
}
