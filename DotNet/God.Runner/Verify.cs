using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using God.Recording;
using God.Sim;

namespace God.Runner
{
    /// <summary>
    /// Determinism checks (HEADLESS_SIMULATION 4.5): compare checkpoint sequences, and on a difference re-run the last
    /// good interval of both runs with a hash every tick to name the first bad tick, then write both states at that tick
    /// as saves (<c>diverge_a.json</c>, <c>diverge_b.json</c>) for a text diff.
    /// </summary>
    public static class Verify
    {
        /// <summary>The first checkpoint where two runs differ (null when equal at every common checkpoint and at the end).</summary>
        public static Divergence Compare(RunResult a, RunResult b)
        {
            var names = GodSim.HashPartNames;
            var bt = b.Checkpoints.ToDictionary(c => c.Tick);
            long lastGood = 0;
            foreach (var ca in a.Checkpoints)
            {
                if (!bt.TryGetValue(ca.Tick, out var cb)) continue;
                if (ca.Hash != cb.Hash) return new Divergence { LastGoodTick = lastGood, BadCheckpointTick = ca.Tick, Modules = RunEngine.DiffModules(ca.Parts, cb.Parts, names) };
                lastGood = ca.Tick;
            }
            if (a.Ticks != b.Ticks || a.FinalHash != b.FinalHash)
                return new Divergence
                {
                    LastGoodTick = lastGood, BadCheckpointTick = Math.Min(a.Ticks, b.Ticks), Modules = RunEngine.DiffModules(a.FinalParts, b.FinalParts, names),
                    Note = "the ends differ: tick " + a.Ticks + " vs " + b.Ticks,
                };
            return null;
        }

        /// <summary>
        /// Steps fresh copies of both runs to the last good checkpoint, then tick by tick to the bad one, hashing every tick,
        /// and fills in the first bad tick and its modules. Writes the two states at that tick when <paramref name="outDir"/> is given.
        /// </summary>
        public static void Bisect(RunSpec a, RunSpec b, Divergence d, string outDir)
        {
            var sa = new SimSession(a.Scenario, a.Seed, a.FromSave, a.Replay, a.TestSystems);
            var sb = new SimSession(b.Scenario, b.Seed, b.FromSave, b.Replay, b.TestSystems);
            while (sa.Tick < d.LastGoodTick && !sa.Sim.State.IsGameOver) sa.StepQuiet();
            while (sb.Tick < d.LastGoodTick && !sb.Sim.State.IsGameOver) sb.StepQuiet();
            if (sa.Sim.StateHash() != sb.Sim.StateHash()) { d.Note = "already different at the last good checkpoint (state outside the checkpoints?)"; return; }
            while (sa.Tick < d.BadCheckpointTick && !(sa.Sim.State.IsGameOver && sb.Sim.State.IsGameOver))
            {
                sa.StepQuiet(); sb.StepQuiet();
                var pa = sa.Sim.StateHashParts(); var pb = sb.Sim.StateHashParts();
                if (GodSim.Fold(pa) == GodSim.Fold(pb)) continue;
                d.FirstBadTick = sa.Tick;
                d.Modules = RunEngine.DiffModules(pa, pb, GodSim.HashPartNames);
                if (outDir != null)
                {
                    Directory.CreateDirectory(outDir);
                    File.WriteAllBytes(Path.Combine(outDir, "diverge_a.json"), RunEngine.SaveBytes(sa.Sim));
                    File.WriteAllBytes(Path.Combine(outDir, "diverge_b.json"), RunEngine.SaveBytes(sb.Sim));
                }
                return;
            }
            d.Note = (d.Note != null ? d.Note + "; " : "") + "bisect found no single bad tick";
        }

        /// <summary>Runs both specs (in parallel when asked), compares, and bisects a difference.</summary>
        public static Divergence CompareRuns(RunSpec a, RunSpec b, bool parallel, string outDir, out RunResult ra, out RunResult rb)
        {
            if (parallel)
            {
                RunResult x = null, y = null;
                Parallel.Invoke(() => x = RunEngine.Run(a), () => y = RunEngine.Run(b));
                ra = x; rb = y;
            }
            else { ra = RunEngine.Run(a); rb = RunEngine.Run(b); }
            var d = Compare(ra, rb);
            if (d != null) Bisect(a, b, d, outDir);
            return d;
        }

        /// <summary>
        /// A run that saves at <paramref name="saveTicks"/>, loads each save in a fresh sim and continues: every checkpoint after
        /// the save must equal the straight run's, and a second save of a loaded game must be byte-identical (M0.6).
        /// </summary>
        public static List<string> SaveLoadMid(RunSpec straightSpec, IList<long> saveTicks, out RunResult straight)
        {
            var problems = new List<string>();
            var spec = Copy(straightSpec);
            spec.KeepSaveBytes = true;
            spec.SaveAtTick = 0;
            spec.TestSystems = straightSpec.TestSystems;
            straight = null;
            // one straight run per save tick keeps RunSpec simple (SaveAtTick is a single tick); the straight runs agree anyway
            foreach (long t in saveTicks)
            {
                var sspec = Copy(spec); sspec.SaveAtTick = t;
                var s = RunEngine.Run(sspec);
                straight = s;
                var save = s.Saves.FirstOrDefault(x => x.tick == t);
                if (save.bytes == null) { problems.Add("no save at tick " + t + " (the game ended at " + s.Ticks + ")"); continue; }
                var file = SaveFile.FromJson(Encoding.UTF8.GetString(save.bytes));
                var again = Encoding.UTF8.GetString(RunEngine.SaveBytes(SimBuilder.FromSave(SaveFile.FromJson(Encoding.UTF8.GetString(save.bytes))).Sim));
                if (again != Encoding.UTF8.GetString(save.bytes)) problems.Add("the save at tick " + t + " is not byte-identical after a load and a second save");
                var lspec = Copy(spec); lspec.FromSave = file; lspec.KeepSaveBytes = false;
                var l = RunEngine.Run(lspec);
                var d = Compare(Tail(s, t), l);
                if (d != null) problems.Add("save at tick " + t + ": " + d);
            }
            return problems;
        }

        private static RunResult Tail(RunResult r, long after) => new RunResult
        {
            Checkpoints = r.Checkpoints.Where(c => c.Tick > after).ToList(), Ticks = r.Ticks, FinalHash = r.FinalHash, FinalParts = r.FinalParts,
        };

        public static RunSpec Copy(RunSpec s) => new RunSpec
        {
            Scenario = s.Scenario, Seed = s.Seed, Replicate = s.Replicate, Level = s.Level, OutRoot = s.OutRoot, Group = s.Group, RunDir = s.RunDir,
            WriteReplay = s.WriteReplay, SaveAtTick = s.SaveAtTick, SaveEveryDays = s.SaveEveryDays, FromSave = s.FromSave, FromSavePath = s.FromSavePath,
            Replay = s.Replay, ToTick = s.ToTick, CheckpointEvery = s.CheckpointEvery, CheckSmoke = s.CheckSmoke, PrintEvents = s.PrintEvents,
            Quiet = s.Quiet, Log = s.Log, KeepSaveBytes = s.KeepSaveBytes, TestSystems = new List<Func<ISimSystem>>(s.TestSystems),
            SnapshotEveryTick = s.SnapshotEveryTick, BeforeStep = s.BeforeStep, Host = s.Host, GitCommit = s.GitCommit, Cancel = s.Cancel,
        };

        /// <summary>The recording matrix: Off, Metrics, Summary, Full, and each M0 domain alone at Full (writing real files under <paramref name="outRoot"/>).</summary>
        public static List<(string name, RunSpec spec)> RecordMatrix(RunSpec baseSpec, string outRoot)
        {
            var l = new List<(string, RunSpec)>();
            foreach (var lv in new[] { RecordingLevel.Off, RecordingLevel.Metrics, RecordingLevel.Summary, RecordingLevel.Full })
            {
                var s = Copy(baseSpec); s.Level = lv; s.OutRoot = outRoot; s.RunDir = Path.Combine(outRoot, "matrix_" + RecordingLevels.Name(lv));
                l.Add(("level " + RecordingLevels.Name(lv), s));
            }
            foreach (var dom in new[] { "commands", "territory", "economy", "production", "military", "state" })
            {
                var s = Copy(baseSpec); s.Level = RecordingLevel.Off; s.OutRoot = outRoot; s.RunDir = Path.Combine(outRoot, "matrix_domain_" + dom);
                var sc = baseSpec.Scenario.Clone(); sc.RecordDomains[dom] = RecordingLevel.Full; s.Scenario = sc;
                l.Add(("domain " + dom, s));
            }
            return l;
        }
    }
}
