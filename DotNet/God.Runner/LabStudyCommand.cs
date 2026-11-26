using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using God.Sim.Lab;

namespace God.Runner
{
    /// <summary>
    /// <c>lab invade</c> and <c>lab sweep</c>: the diagnostic runs of the Hawk/Bully/Retaliator investigation
    /// (docs/stage4/research/HAWK_BULLY_RETALIATOR_INVESTIGATION.md). They read <see cref="ContestStudy"/> and the
    /// existing replicator and agent engine; they never change L2. Output is Markdown, deterministic for given options.
    /// </summary>
    public static class LabStudyCommand
    {
        public const string Usage = @"  God.Runner lab invade [--matrix l2|l2p|msp|msp4] [--rule S=10 ...] [--eps 10] [--gens 20000] [--w0 500]
                        [--agents 1000] [--agent-gens 2000] [--k 10] [--seeds 1..5] [--expected] [--workers N]
  God.Runner lab sweep  [--matrix l2] [--dynamics agents|replicator] [--starts uniform,hawk,ret,hb] [--mu 0,2,20]
                        [--agents 200] [--k 10] [--w0 500] [--gens 6000] [--seeds 1..5] [--rule S=10 ...]
                        [--expected] [--basins STEPS] [--workers N]";

        // ---------------- shared options ----------------

        private static ContestRules Rules(Args a)
        {
            var r = ContestRules.Lab;
            int v = r.V, i = r.I, t = r.T, s = r.S, mt = r.MaxTurns, q = r.ProbePermille;
            foreach (var kv in a.All("rule"))
            {
                int eq = kv.IndexOf('=');
                if (eq <= 0 || !int.TryParse(kv.Substring(eq + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int val))
                    throw new UsageException("--rule takes KEY=integer (V, I, T, S, MaxTurns, q): " + kv);
                switch (kv.Substring(0, eq))
                {
                    case "V": v = val; break;
                    case "I": i = val; break;
                    case "T": t = val; break;
                    case "S": s = val; break;
                    case "MaxTurns": mt = val; break;
                    case "q": q = val; break;
                    default: throw new UsageException("--rule key must be V, I, T, S, MaxTurns or q: " + kv);
                }
            }
            return new ContestRules(v, i, t, s, mt, q);
        }

        private static string RulesText(ContestRules r) => "V " + r.V + ", I " + r.I + ", T " + r.T + ", S " + r.S + ", MaxTurns " + r.MaxTurns + ", q " + r.ProbePermille + " permille";

        private static List<int> Ints(Args a, string name, string def)
        {
            string s = a.Str(name, def);
            var l = new List<int>();
            foreach (var p in s.Split(','))
            {
                if (!int.TryParse(p.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) || v < 0)
                    throw new UsageException("--" + name + " takes a list of non-negative integers: " + s);
                l.Add(v);
            }
            return l;
        }

        private static List<ulong> Seeds(Args a)
        {
            string s = a.Str("seeds");
            return s == null ? new List<ulong> { 1, 2, 3, 4, 5 } : Args.ParseSeeds(s);
        }

        private static string Matrix(Args a)
        {
            string m = a.Str("matrix", "l2");
            if (!ContestStudy.MatrixNames().Contains(m)) throw new UsageException("--matrix takes l2, l2p, msp or msp4");
            return m;
        }

        private static string F(long q30, int d = 3) => Fx.Format(q30, d);

        private static string Shares(long[] x, string[] names)
        {
            var parts = new List<string>();
            for (int i = 0; i < x.Length; i++) parts.Add(names[i] + " " + F(x[i]));
            return string.Join(", ", parts);
        }

        private static void For(int count, int workers, Action<int> job)
        {
            if (workers <= 1) for (int i = 0; i < count; i++) job(i);
            else Parallel.For(0, count, new ParallelOptions { MaxDegreeOfParallelism = workers }, job);
        }

        public static void PrintMatrix(TextWriter o, long[,] m, string[] names)
        {
            o.WriteLine("| row \\ col | " + string.Join(" | ", names) + " |");
            o.WriteLine("|---|" + string.Concat(names.Select(_ => "---|")));
            for (int i = 0; i < names.Length; i++)
            {
                var cells = new List<string>();
                for (int j = 0; j < names.Length; j++) cells.Add(Fx.FormatScaled(m[i, j], 3).TrimEnd('0').TrimEnd('.'));
                o.WriteLine("| " + names[i] + " | " + string.Join(" | ", cells) + " |");
            }
        }

        // ---------------- invade ----------------

        public static int Invade(Args a, TextWriter o, CancellationToken cancel)
        {
            string matrix = Matrix(a);
            var rules = Rules(a);
            int eps = (int)a.Long("eps", 10);
            int gens = (int)a.Long("gens", 20000);
            long w0 = a.Long("w0", 500) * 1000;
            int n = (int)a.Long("agents", 1000);
            int agentGens = (int)a.Long("agent-gens", 2000);
            int k = (int)a.Long("k", 10);
            var seeds = Seeds(a);
            bool expected = a.Flag("expected");
            int workers = (int)a.Long("workers", 1);
            a.CheckAllUsed(0);
            if (eps < 1 || eps > 500) throw new UsageException("--eps is in permille, 1 to 500");

            var m = ContestStudy.Named(matrix, rules);
            int kk = m.GetLength(0);
            var names = ContestStudy.Short(kk);
            o.WriteLine("# lab invade: matrix " + matrix + (ContestStudy.IsTurnRule(matrix) ? " (" + RulesText(rules) + ")" : " (Maynard Smith and Price 1973, Table 1; D = Mouse)"));
            o.WriteLine();
            PrintMatrix(o, m, names);
            o.WriteLine();

            // candidates: isolated rest points plus the Retaliator/Dove edge (a continuum the support solver skips)
            var cands = ContestStudy.RestPoints(m, names);
            foreach (var p in new[] { new[] { 0, 0, 1000, 0 }, new[] { 0, 100, 900, 0 }, new[] { 0, 400, 600, 0 }, new[] { 0, 450, 550, 0 } })
            {
                var pp = kk == 5 ? new[] { p[0], p[1], p[2], p[3], 0 } : p;
                var x = ContestStudy.StatePermille(pp);
                if (cands.Any(c => Same(c.X, x))) continue;
                cands.Add(ContestStudy.Analyse(m, x, ContestStudy.Label(x, names)));
            }

            o.WriteLine("## Rest points and exact invasion analysis");
            o.WriteLine();
            o.WriteLine("First order: mutant's payoff against the state minus the residents' mean. Second order (used after a first-order tie): mutant against itself minus the residents against the mutant.");
            o.WriteLine();
            o.WriteLine("| # | State | Mean payoff | " + string.Join(" | ", names.Select(x => "mutant " + x)) + " | Verdict |");
            o.WriteLine("|---|---|---|" + string.Concat(names.Select(_ => "---|")) + "---|");
            for (int c = 0; c < cands.Count; c++)
            {
                var cd = cands[c];
                var cells = new List<string>();
                for (int j = 0; j < kk; j++)
                {
                    var cell = cd.Mutants.FirstOrDefault(z => z.Mutant == j);
                    if (cell == null) { cells.Add("(resident)"); continue; }
                    cells.Add(cell.First.Format(3) + (cell.First.IsZero ? " / " + cell.Second.Format(3) : "") + " " + VerdictWord(cell.Verdict));
                }
                o.WriteLine("| " + (c + 1) + " | " + cd.Label + " | " + cd.Value.Format(3) + " | " + string.Join(" | ", cells) + " | " + cd.Verdict(names) + " |");
            }
            o.WriteLine();

            // dynamic replicator invasion: eps of each absent strategy, no mutation
            o.WriteLine("## Replicator invasion runs (no mutation, no drift)");
            o.WriteLine();
            o.WriteLine("Start: (1 - " + Fx.FormatScaled(eps, 3) + ") x state + " + Fx.FormatScaled(eps, 3) + " of the mutant; w0 " + (w0 / 1000) + "; " + gens + " generations. Cell: mutant share at the end, then the basin (R, H/B, mixed) and the largest change of any share from the state.");
            o.WriteLine();
            o.WriteLine("| # | State | " + string.Join(" | ", names.Select(x => "+ " + x)) + " |");
            o.WriteLine("|---|---|" + string.Concat(names.Select(_ => "---|")));
            for (int c = 0; c < cands.Count; c++)
            {
                if (cancel.IsCancellationRequested) return ExitCodes.Interrupted;
                var cd = cands[c];
                var x0 = ContestStudy.ToQ30(cd.X);
                var cells = new string[kk];
                For(kk, workers, j =>
                {
                    if (cd.X[j].Sign > 0) { cells[j] = "(resident)"; return; }
                    var start = ContestStudy.WithMutant(x0, j, eps);
                    var end = ContestStudy.RunReplicator(m, start, gens, w0, 0);
                    long dev = 0;
                    for (int i = 0; i < kk; i++) dev = Math.Max(dev, Math.Abs(end[i] - x0[i]));
                    cells[j] = F(end[j], 4) + ", " + ContestStudy.Region(end) + ", moved " + F(dev, 3);
                });
                o.WriteLine("| " + (c + 1) + " | " + cd.Label + " | " + string.Join(" | ", cells) + " |");
            }
            o.WriteLine();

            if (n > 0)
            {
                var game = ContestStudy.Game(matrix, expected, rules);
                int m0 = Math.Max(1, n * eps / 1000);
                o.WriteLine("## Agent invasion runs (finite population, drift)");
                o.WriteLine();
                o.WriteLine("Only states with no first-order invader. N " + n + " with " + m0 + " mutants, k " + k + ", w0 " + (w0 / 1000) + ", no mutation, " + agentGens + " generations, seeds " + string.Join(",", seeds)
                    + "; " + (ContestStudy.IsTurnRule(matrix) && !expected ? "realised contests (as L2)" : "expected payoffs") + ". Cell: seeds where the mutant ended above its start / lost / fixed; mean final mutant share; final basins (R / H/B / mixed).");
                o.WriteLine();
                o.WriteLine("| # | State | " + string.Join(" | ", names.Select(x => "+ " + x)) + " |");
                o.WriteLine("|---|---|" + string.Concat(names.Select(_ => "---|")));
                for (int c = 0; c < cands.Count; c++)
                {
                    var cd = cands[c];
                    if (!cd.IsRestPoint || cd.Mutants.Length == 0 || cd.Mutants.Any(z => z.First.Sign > 0)) continue;
                    var x0 = ContestStudy.ToQ30(cd.X);
                    var jobs = new List<(int j, ulong seed)>();
                    for (int j = 0; j < kk; j++) if (cd.X[j].Sign <= 0) foreach (var s in seeds) jobs.Add((j, s));
                    var res = new long[jobs.Count][];
                    For(jobs.Count, workers, i =>
                    {
                        if (cancel.IsCancellationRequested) return;
                        var counts = ContestStudy.Counts(x0, n, jobs[i].j, m0);
                        var run = ContestStudy.RunAgents(game, counts, agentGens, k, w0, 0, jobs[i].seed, "invade/" + matrix + "/" + c + "/" + jobs[i].j);
                        res[i] = run.Shares[agentGens];
                    });
                    if (cancel.IsCancellationRequested) return ExitCodes.Interrupted;
                    var cells = new string[kk];
                    long startShare = Fx.FromRatio(m0, n);
                    for (int j = 0; j < kk; j++)
                    {
                        if (cd.X[j].Sign > 0) { cells[j] = "(resident)"; continue; }
                        var mine = jobs.Select((jb, i) => (jb, i)).Where(t => t.jb.j == j).Select(t => res[t.i]).ToList();
                        int up = mine.Count(e => e[j] > startShare), lost = mine.Count(e => e[j] == 0), fixd = mine.Count(e => e[j] == Fx.One);
                        long mean = mine.Sum(e => e[j]) / mine.Count;
                        string basins = mine.Count(e => ContestStudy.Region(e) == "R") + "/" + mine.Count(e => ContestStudy.Region(e) == "H/B") + "/" + mine.Count(e => ContestStudy.Region(e) == "mixed");
                        cells[j] = up + " up, " + lost + " lost, " + fixd + " fixed; mean " + F(mean, 3) + "; " + basins;
                    }
                    o.WriteLine("| " + (c + 1) + " | " + cd.Label + " | " + string.Join(" | ", cells) + " |");
                }
                o.WriteLine();
            }
            return ExitCodes.Ok;
        }

        private static bool Same(Rational[] a, Rational[] b)
        {
            for (int i = 0; i < a.Length; i++) if (!a[i].Equals(b[i])) return false;
            return true;
        }

        private static string VerdictWord(InvasionVerdict v)
        {
            switch (v)
            {
                case InvasionVerdict.Resists: return "resists";
                case InvasionVerdict.ResistsSecondOrder: return "resists (2nd order)";
                case InvasionVerdict.Neutral: return "**neutral**";
                default: return "**invades**";
            }
        }

        // ---------------- sweep ----------------

        public static int Sweep(Args a, TextWriter o, CancellationToken cancel)
        {
            string matrix = Matrix(a);
            var rules = Rules(a);
            string dyn = a.Str("dynamics", "agents");
            if (dyn != "agents" && dyn != "replicator") throw new UsageException("--dynamics takes agents or replicator");
            var starts = a.Str("starts", "uniform,hawk,ret,hb").Split(',').Select(s => s.Trim()).ToList();
            var mus = Ints(a, "mu", "2");
            var ns = Ints(a, "agents", "200");
            var ks = Ints(a, "k", "10");
            var w0s = Ints(a, "w0", "500");
            var gensL = Ints(a, "gens", "6000");
            var seeds = Seeds(a);
            bool expected = a.Flag("expected");
            int basins = (int)a.Long("basins", 0);
            int workers = (int)a.Long("workers", 1);
            a.CheckAllUsed(0);

            var m = ContestStudy.Named(matrix, rules);
            int kk = m.GetLength(0);
            var names = ContestStudy.Short(kk);
            foreach (var s in starts)
            {
                try { ContestStudy.StartPermille(s, kk); }
                catch (ArgumentException e) { throw new UsageException(e.Message); }
            }

            o.WriteLine("# lab sweep: matrix " + matrix + (ContestStudy.IsTurnRule(matrix) ? " (" + RulesText(rules) + ")" : " (Maynard Smith and Price 1973, Table 1)") + ", dynamics " + (basins > 0 ? "replicator basin census" : dyn));
            o.WriteLine();

            if (basins > 0)
            {
                o.WriteLine("Every start on the simplex grid with step 1/" + basins + " and every strategy present; replicator, w0 and mutation as given. Count of starts ending in each basin.");
                o.WriteLine();
                o.WriteLine("| w0 | mu (permille) | gens | starts | R | H/B | mixed |");
                o.WriteLine("|---|---|---|---|---|---|---|");
                var jobs = new List<(int w0, int mu, int g)>();
                foreach (var w in w0s) foreach (var mu in mus) foreach (var g in gensL) jobs.Add((w, mu, g));
                var res = new int[jobs.Count][];
                For(jobs.Count, workers, i => res[i] = ContestStudy.Basins(m, basins, jobs[i].g, jobs[i].w0 * 1000L, jobs[i].mu));
                for (int i = 0; i < jobs.Count; i++)
                {
                    int tot = res[i].Sum();
                    o.WriteLine("| " + jobs[i].w0 + " | " + jobs[i].mu + " | " + jobs[i].g + " | " + tot + " | " + res[i][0] + " | " + res[i][1] + " | " + res[i][2] + " |");
                }
                return ExitCodes.Ok;
            }

            if (dyn == "replicator")
            {
                o.WriteLine("Deterministic replicator (no drift). Mean shares over the second half of the run, and the basin of the final state.");
                o.WriteLine();
                o.WriteLine("| start | w0 | mu (permille) | gens | final | mean, second half | basin |");
                o.WriteLine("|---|---|---|---|---|---|---|");
                foreach (var s in starts) foreach (var w in w0s) foreach (var mu in mus) foreach (var g in gensL)
                {
                    if (cancel.IsCancellationRequested) return ExitCodes.Interrupted;
                    var p = ContestStudy.StartPermille(s, kk);
                    var series = ContestStudy.ReplicatorSeries(m, Replicator.SharesPermille(p), g, w * 1000L, mu);
                    var mean = ContestStudy.MeanShares(series, g / 2, g);
                    o.WriteLine("| " + s + " | " + w + " | " + mu + " | " + g + " | " + Shares(series[g], names) + " | " + Shares(mean, names) + " | " + ContestStudy.Region(series[g]) + " |");
                }
                return ExitCodes.Ok;
            }

            var game = ContestStudy.Game(matrix, expected, rules);
            o.WriteLine("Agent-based (L2's engine: random pairing, k contests per generation, Wright-Fisher selection on w0 + payoffs, uniform mutation). "
                + (ContestStudy.IsTurnRule(matrix) && !expected ? "Realised contests (as L2)." : "Expected payoffs (no outcome noise).")
                + " Seeds " + string.Join(",", seeds) + ". Mean shares over generations gens/2 to gens, averaged over seeds; per-seed Retaliator share; basins of the per-seed window means (R / H/B / mixed).");
            o.WriteLine();
            o.WriteLine("| start | N | k | w0 | mu (permille) | gens | mean shares | R per seed | basins R/HB/mixed |");
            o.WriteLine("|---|---|---|---|---|---|---|---|---|");
            var cfgs = new List<(string s, int n, int k, int w0, int mu, int g)>();
            foreach (var s in starts) foreach (var n in ns) foreach (var k in ks) foreach (var w in w0s) foreach (var mu in mus) foreach (var g in gensL) cfgs.Add((s, n, k, w, mu, g));
            var jobsA = new List<(int cfg, ulong seed)>();
            for (int c = 0; c < cfgs.Count; c++) foreach (var sd in seeds) jobsA.Add((c, sd));
            var means = new long[jobsA.Count][];
            For(jobsA.Count, workers, i =>
            {
                if (cancel.IsCancellationRequested) return;
                var cf = cfgs[jobsA[i].cfg];
                var counts = AgentRunSpec.Counts(cf.n, ContestStudy.StartPermille(cf.s, kk));
                string tag = "sweep/" + matrix + (expected ? "/exp" : "") + "/" + cf.s + "/" + cf.n + "/" + cf.k + "/" + cf.w0 + "/" + cf.mu + "/" + cf.g;
                var run = ContestStudy.RunAgents(game, counts, cf.g, cf.k, cf.w0 * 1000L, cf.mu, jobsA[i].seed, tag);
                means[i] = ContestStudy.MeanShares(run.Shares, cf.g / 2, cf.g);
            });
            if (cancel.IsCancellationRequested) return ExitCodes.Interrupted;
            for (int c = 0; c < cfgs.Count; c++)
            {
                var cf = cfgs[c];
                var mine = jobsA.Select((j, i) => (j, i)).Where(t => t.j.cfg == c).Select(t => means[t.i]).ToList();
                var avg = new long[kk];
                for (int i = 0; i < kk; i++) avg[i] = mine.Sum(x => x[i]) / mine.Count;
                string per = string.Join(" ", mine.Select(x => F(x[2], 2)));
                string bas = mine.Count(x => ContestStudy.Region(x) == "R") + "/" + mine.Count(x => ContestStudy.Region(x) == "H/B") + "/" + mine.Count(x => ContestStudy.Region(x) == "mixed");
                o.WriteLine("| " + cf.s + " | " + cf.n + " | " + cf.k + " | " + cf.w0 + " | " + cf.mu + " | " + cf.g + " | " + Shares(avg, names) + " | " + per + " | " + bas + " |");
            }
            return ExitCodes.Ok;
        }
    }
}
