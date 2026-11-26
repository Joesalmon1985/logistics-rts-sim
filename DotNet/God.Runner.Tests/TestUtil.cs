using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using God.Recording;
using God.Sim;
using NUnit.Framework;

namespace God.Runner.Tests
{
    internal static class TestUtil
    {
        public static string ScenarioPath(string name) => Path.Combine(AppContext.BaseDirectory, "Scenarios", name + ".scenario.json");

        public static Scenario Load(string name, params string[] sets) => Scenario.Parse(File.ReadAllText(ScenarioPath(name)), sets);

        public static RunSpec Spec(string scenario, ulong seed, long ticks, params string[] sets)
        {
            var sc = Load(scenario, sets);
            if (ticks > 0) { sc.MaxTicks = ticks; sc.MaxDays = 0; sc.MaxYears = 0; }
            return new RunSpec { Scenario = sc, Seed = seed, Quiet = true, GitCommit = "test", WriteReplay = false };
        }

        public static string TempDir(string tag)
        {
            string d = Path.Combine(Path.GetTempPath(), "god-runner-tests", tag + "-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(d);
            return d;
        }

        public static void Cleanup(string dir) { try { Directory.Delete(dir, true); } catch { } }

        public static List<(long, ulong)> Seq(RunResult r) => r.Checkpoints.Select(c => (c.Tick, c.Hash)).ToList();

        /// <summary>Runs the specs on a thread pool (one sim per thread) and returns the results in order.</summary>
        public static RunResult[] RunAll(IList<RunSpec> specs, int workers = 0)
        {
            var res = new RunResult[specs.Count];
            System.Threading.Tasks.Parallel.For(0, specs.Count, new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = workers > 0 ? workers : Environment.ProcessorCount }, i => res[i] = RunEngine.Run(specs[i]));
            return res;
        }

        public static int Cli(out string stdout, out string stderr, params string[] args)
        {
            var o = new StringWriter(); var e = new StringWriter();
            int code = God.Runner.Cli.Run(args, o, e, CancellationToken.None);
            stdout = o.ToString(); stderr = e.ToString();
            return code;
        }
    }

    /// <summary>A test-only system: at one tick it changes a pile count, an injected nondeterminism.</summary>
    internal sealed class PokeSystem : ISimSystem
    {
        private readonly long _tick;
        public PokeSystem(long tick) { _tick = tick; }
        public void Tick(GodState s, EventBuffer ev) { if (s.Tick == _tick) s.Territory.Piles[0] = (s.Territory.Piles[0] + 1) % 3; }
    }

    /// <summary>A test-only system that throws at one tick (exit code 6).</summary>
    internal sealed class ThrowSystem : ISimSystem
    {
        private readonly long _tick;
        public ThrowSystem(long tick) { _tick = tick; }
        public void Tick(GodState s, EventBuffer ev) { if (s.Tick == _tick) throw new InvalidOperationException("test fault at tick " + _tick); }
    }

    /// <summary>A test-only system that breaks an invariant (a negative stock) at one tick (exit code 5).</summary>
    internal sealed class BreakInvariantSystem : ISimSystem
    {
        private readonly long _tick;
        public BreakInvariantSystem(long tick) { _tick = tick; }
        public void Tick(GodState s, EventBuffer ev) { if (s.Tick >= _tick) s.Economy.HqsOf(Faction.F0)[0].Stock[(int)Ware.Log] = -5; }
    }
}
