using System.Collections.Generic;

namespace God.Sim.Lab
{
    /// <summary>How a rare mutant fares against a resident state (Maynard Smith's two conditions).</summary>
    public enum InvasionVerdict : byte
    {
        /// <summary>First order: the mutant earns less than the residents against the resident state.</summary>
        Resists,
        /// <summary>First order tie; second order: the mutant does worse against itself than the residents do against it.</summary>
        ResistsSecondOrder,
        /// <summary>Ties at both orders: the mutant share drifts (no selection either way).</summary>
        Neutral,
        /// <summary>The mutant gains, at first order or (after a first-order tie) at second order.</summary>
        Invades,
    }

    /// <summary>One mutant's result against a candidate state.</summary>
    public sealed class InvasionCell
    {
        public int Mutant;
        /// <summary>Mutant's payoff against the resident state minus the residents' mean payoff (first order).</summary>
        public Rational First;
        /// <summary>Mutant against itself minus the resident state against the mutant (decides after a first-order tie).</summary>
        public Rational Second;
        public InvasionVerdict Verdict;
    }

    /// <summary>A candidate equilibrium with its exact invasion analysis.</summary>
    public sealed class Candidate
    {
        public string Label;
        public Rational[] X;
        /// <summary>The residents' mean payoff x·Ax.</summary>
        public Rational Value;
        /// <summary>Every strategy in the support earns the same (so the replicator leaves the state where it is).</summary>
        public bool IsRestPoint;
        /// <summary>The symmetric part of A is negative definite on the support's tangent space (always true for a pure state).</summary>
        public bool StableInsideSupport;
        /// <summary>Negative semidefinite on the support's tangent space: no self-reinforcing direction, some may be neutral.</summary>
        public bool NeutralInsideSupport;
        /// <summary>Strategies outside the support, in index order.</summary>
        public InvasionCell[] Mutants;

        public string Verdict(string[] names)
        {
            if (!IsRestPoint) return "not a rest point";
            var inv = new List<string>();
            var neu = new List<string>();
            foreach (var m in Mutants)
            {
                if (m.Verdict == InvasionVerdict.Invades) inv.Add(names[m.Mutant]);
                else if (m.Verdict == InvasionVerdict.Neutral) neu.Add(names[m.Mutant]);
            }
            if (inv.Count > 0) return "not stable: invaded by " + string.Join(", ", inv);
            if (!StableInsideSupport && !NeutralInsideSupport) return "not an ESS: unstable inside its support";
            if (!StableInsideSupport) neu.Add("shares inside the support");
            if (neu.Count > 0) return "neutrally stable only: " + string.Join(", ", neu) + " drift in";
            return "ESS (every mutant resists)";
        }
    }

    /// <summary>
    /// The Hawk/Bully/Retaliator investigation (docs/stage4/research/HAWK_BULLY_RETALIATOR_INVESTIGATION.md): payoff
    /// matrices, exact rest points and invasion analysis, and helpers for invasion and sweep runs on the existing
    /// replicator and agent engine. Analysis only: it does not change L2 or any preset.
    /// Strategy order everywhere: Hawk, Dove (Maynard Smith and Price's "Mouse"), Retaliator, Bully, Prober(-Retaliator).
    /// </summary>
    public static class ContestStudy
    {
        public static string[] Short(int k) => k == 4 ? new[] { "H", "D", "R", "B" } : new[] { "H", "D", "R", "B", "P" };

        /// <summary>The matrices the study knows: l2 (L2's turn-rule H/D/R/B, what L2's agents play), l2p (with the
        /// Prober), msp (Maynard Smith and Price 1973, Table 1, H/D/R/B/P), msp4 (its H/D/R/B block).</summary>
        public static string[] MatrixNames() => new[] { "l2", "l2p", "msp", "msp4" };

        /// <summary>
        /// Maynard Smith and Price (1973), Nature 246:15-18, Table 1: mean payoff of the row strategy against the column
        /// strategy over 2,000 simulated contests per pairing, x1000. The paper's order is Mouse, Hawk, Bully, Retaliator,
        /// Prober-Retaliator; here it is reordered to H, D (= Mouse), R, B, P.
        /// </summary>
        public static long[,] Msp1973X1000()
        {
            // paper rows (Mouse, Hawk, Bully, Retaliator, Prober-Retaliator) x paper columns, x10
            var paper = new long[,]
            {
                { 290, 195, 195, 290, 172 },
                { 800, -195, 746, -181, -189 },
                { 800, 49, 415, 119, 112 },
                { 290, -223, 571, 290, 231 },
                { 567, -201, 594, 269, 219 },
            };
            var order = new[] { 1, 0, 3, 2, 4 }; // ours H, D, R, B, P → paper index
            var m = new long[5, 5];
            for (int i = 0; i < 5; i++) for (int j = 0; j < 5; j++) m[i, j] = paper[order[i], order[j]] * 100;
            return m;
        }

        /// <summary>L2's exact turn-rule matrix (x1000) for the first k of H, D, R, B, P.</summary>
        public static long[,] TurnRuleX1000(int k, ContestRules r)
        {
            var all = L02Retaliator.Hdrbp();
            var kinds = new ContestKind[k];
            System.Array.Copy(all, kinds, k);
            var m = new long[k, k];
            for (int i = 0; i < k; i++) for (int j = 0; j < k; j++) m[i, j] = Contest.ExpectedX1000(kinds[i], kinds[j], false, r);
            return m;
        }

        public static long[,] Named(string name, ContestRules r)
        {
            switch (name)
            {
                case "l2": return TurnRuleX1000(4, r);
                case "l2p": return TurnRuleX1000(5, r);
                case "msp": return Msp1973X1000();
                case "msp4": return Block(Msp1973X1000(), 4);
                default: throw new System.ArgumentException("unknown matrix '" + name + "' (l2, l2p, msp, msp4)");
            }
        }

        /// <summary>True when the matrix comes from the turn rules (so agents can play realised contests).</summary>
        public static bool IsTurnRule(string name) => name == "l2" || name == "l2p";

        public static long[,] Block(long[,] a, int k)
        {
            var m = new long[k, k];
            for (int i = 0; i < k; i++) for (int j = 0; j < k; j++) m[i, j] = a[i, j];
            return m;
        }

        // ---------------- exact analysis ----------------

        /// <summary>A state from permille shares (must sum to 1000).</summary>
        public static Rational[] StatePermille(params int[] permille)
        {
            int sum = 0;
            var x = new Rational[permille.Length];
            for (int i = 0; i < x.Length; i++) { x[i] = Rational.Of(permille[i], 1000); sum += permille[i]; }
            if (sum != 1000) throw new System.ArgumentException("shares must sum to 1000 permille");
            return x;
        }

        /// <summary>Exact invasion analysis of state x (payoffs in whole units, so x1000 entries are divided by 1000).</summary>
        public static Candidate Analyse(long[,] a, Rational[] x, string label)
        {
            int k = x.Length;
            var p = new Rational[k];
            for (int i = 0; i < k; i++)
            {
                var s = Rational.Zero;
                for (int j = 0; j < k; j++) if (!x[j].IsZero) s += A(a, i, j) * x[j];
                p[i] = s;
            }
            var v = Rational.Zero;
            for (int i = 0; i < k; i++) if (!x[i].IsZero) v += x[i] * p[i];
            bool rest = true;
            var support = new List<int>();
            for (int i = 0; i < k; i++) if (x[i].Sign > 0) { support.Add(i); if (!p[i].Equals(v)) rest = false; }
            var mutants = new List<InvasionCell>();
            for (int j = 0; j < k; j++)
            {
                if (x[j].Sign > 0) continue;
                var first = p[j] - v;
                var vsJ = Rational.Zero;
                for (int i = 0; i < k; i++) if (!x[i].IsZero) vsJ += x[i] * A(a, i, j);
                var second = A(a, j, j) - vsJ;
                InvasionVerdict verdict;
                if (first.Sign < 0) verdict = InvasionVerdict.Resists;
                else if (first.Sign > 0) verdict = InvasionVerdict.Invades;
                else if (second.Sign < 0) verdict = InvasionVerdict.ResistsSecondOrder;
                else if (second.Sign > 0) verdict = InvasionVerdict.Invades;
                else verdict = InvasionVerdict.Neutral;
                mutants.Add(new InvasionCell { Mutant = j, First = first, Second = second, Verdict = verdict });
            }
            return new Candidate
            {
                Label = label, X = x, Value = v, IsRestPoint = rest, StableInsideSupport = NegativeDefiniteOnFace(a, support),
                NeutralInsideSupport = NegativeSemidefiniteOnFace(a, support),
                Mutants = mutants.ToArray(),
            };
        }

        /// <summary>
        /// Every isolated rest point with full support on its face (support enumeration; each support's equal-payoff
        /// system solved exactly). Supports whose system is singular (a continuum of rest points, such as the
        /// Retaliator/Dove edge) are skipped; analyse such states directly with <see cref="Analyse"/>.
        /// </summary>
        public static List<Candidate> RestPoints(long[,] a, string[] names)
        {
            int k = a.GetLength(0);
            var list = new List<Candidate>();
            for (int mask = 1; mask < (1 << k); mask++)
            {
                var s = new List<int>();
                for (int i = 0; i < k; i++) if ((mask & (1 << i)) != 0) s.Add(i);
                var sol = SolveSupport(a, s);
                if (sol == null) continue;
                bool positive = true;
                foreach (var r in sol) if (r.Sign <= 0) positive = false;
                if (!positive) continue;
                var x = new Rational[k];
                for (int i = 0; i < k; i++) x[i] = Rational.Zero;
                for (int m = 0; m < s.Count; m++) x[s[m]] = sol[m];
                list.Add(Analyse(a, x, Label(x, names)));
            }
            return list;
        }

        public static string Label(Rational[] x, string[] names)
        {
            var parts = new List<string>();
            for (int i = 0; i < x.Length; i++) if (x[i].Sign > 0) parts.Add(names[i] + " " + x[i].Format(4));
            return string.Join(", ", parts);
        }

        private static Rational A(long[,] a, int i, int j) => Rational.Of(a[i, j], 1000);

        /// <summary>Shares on support s with equal payoffs; null when the system is singular.</summary>
        private static Rational[] SolveSupport(long[,] a, List<int> s)
        {
            int n = s.Count;
            if (n == 1) return new[] { Rational.One };
            // unknowns x_s (n) and v; equations: sum_j A_ij x_j - v = 0 for i in s; sum x = 1
            int m = n + 1;
            var mat = new Rational[m, m + 1];
            for (int r = 0; r < n; r++)
            {
                for (int c = 0; c < n; c++) mat[r, c] = A(a, s[r], s[c]);
                mat[r, n] = -Rational.One;
                mat[r, m] = Rational.Zero;
            }
            for (int c = 0; c < n; c++) mat[n, c] = Rational.One;
            mat[n, n] = Rational.Zero;
            mat[n, m] = Rational.One;
            if (!Eliminate(mat, m)) return null;
            var x = new Rational[n];
            for (int i = 0; i < n; i++) x[i] = mat[i, m] / mat[i, i];
            return x;
        }

        /// <summary>Gauss-Jordan in place on an m x (m+1) augmented matrix; false when singular.</summary>
        private static bool Eliminate(Rational[,] mat, int m)
        {
            for (int col = 0; col < m; col++)
            {
                int piv = -1;
                for (int r = col; r < m; r++) if (!mat[r, col].IsZero) { piv = r; break; }
                if (piv < 0) return false;
                if (piv != col) for (int c = 0; c <= m; c++) { var t = mat[col, c]; mat[col, c] = mat[piv, c]; mat[piv, c] = t; }
                for (int r = 0; r < m; r++)
                {
                    if (r == col || mat[r, col].IsZero) continue;
                    var f = mat[r, col] / mat[col, col];
                    for (int c = col; c <= m; c++) mat[r, c] = mat[r, c] - f * mat[col, c];
                }
            }
            return true;
        }

        /// <summary>z·Az &lt; 0 for every nonzero z with sum zero on the support (Sylvester's criterion on the symmetric part).</summary>
        private static bool NegativeDefiniteOnFace(long[,] a, List<int> s)
        {
            int n = s.Count - 1;
            if (n <= 0) return true;
            var b = TangentForm(a, s); // basis z_i = e_si - e_last; b = symmetric part of z_i·A z_j
            for (int d = 1; d <= n; d++)
            {
                var det = Det(b, d);
                // negative definite: leading minors alternate, sign (-1)^d
                int want = d % 2 == 1 ? -1 : 1;
                if (det.Sign != want) return false;
            }
            return true;
        }

        /// <summary>z·Az &lt;= 0 on the support's tangent space (every principal minor of the symmetric part has sign
        /// (-1)^order or is zero): no direction inside the support is self-reinforcing, though some may be neutral.</summary>
        private static bool NegativeSemidefiniteOnFace(long[,] a, List<int> s)
        {
            int n = s.Count - 1;
            if (n <= 0) return true;
            var b = TangentForm(a, s);
            for (int mask = 1; mask < (1 << n); mask++)
            {
                var idx = new List<int>();
                for (int i = 0; i < n; i++) if ((mask & (1 << i)) != 0) idx.Add(i);
                var sub = new Rational[idx.Count, idx.Count];
                for (int i = 0; i < idx.Count; i++) for (int j = 0; j < idx.Count; j++) sub[i, j] = b[idx[i], idx[j]];
                var det = Det(sub, idx.Count);
                int want = idx.Count % 2 == 1 ? -1 : 1;
                if (det.Sign != 0 && det.Sign != want) return false;
            }
            return true;
        }

        private static Rational[,] TangentForm(long[,] a, List<int> s)
        {
            int n = s.Count - 1;
            int last = s[n];
            var b = new Rational[n, n];
            for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
            {
                Rational q(int u, int w) => A(a, u, w) + A(a, w, u);
                b[i, j] = (q(s[i], s[j]) - q(s[i], last) - q(last, s[j]) + q(last, last)) / Rational.Of(2);
            }
            return b;
        }

        private static Rational Det(Rational[,] b, int d)
        {
            var m = new Rational[d, d];
            for (int i = 0; i < d; i++) for (int j = 0; j < d; j++) m[i, j] = b[i, j];
            var det = Rational.One;
            for (int col = 0; col < d; col++)
            {
                int piv = -1;
                for (int r = col; r < d; r++) if (!m[r, col].IsZero) { piv = r; break; }
                if (piv < 0) return Rational.Zero;
                if (piv != col) { for (int c = 0; c < d; c++) { var t = m[col, c]; m[col, c] = m[piv, c]; m[piv, c] = t; } det = -det; }
                det = det * m[col, col];
                for (int r = col + 1; r < d; r++)
                {
                    if (m[r, col].IsZero) continue;
                    var f = m[r, col] / m[col, col];
                    for (int c = col; c < d; c++) m[r, c] = m[r, c] - f * m[col, c];
                }
            }
            return det;
        }

        // ---------------- states, regions ----------------

        /// <summary>
        /// Named starting distributions (permille) for k = 4 (H, D, R, B) or 5 (H, D, R, B, P): uniform; hawk (Hawk-heavy);
        /// ret (Retaliator-heavy); hb (a Hawk/Bully mix with a 2% fringe of each other strategy); rd (Retaliator/Dove 50:50
        /// with a 2% fringe of the others).
        /// </summary>
        public static int[] StartPermille(string name, int k)
        {
            if (k != 4 && k != 5) throw new System.ArgumentException("k must be 4 or 5");
            switch (name)
            {
                case "uniform": return k == 4 ? new[] { 250, 250, 250, 250 } : new[] { 200, 200, 200, 200, 200 };
                case "hawk": return k == 4 ? new[] { 700, 100, 100, 100 } : new[] { 600, 100, 100, 100, 100 };
                case "ret": return k == 4 ? new[] { 100, 100, 700, 100 } : new[] { 100, 100, 600, 100, 100 };
                case "hb": return k == 4 ? new[] { 580, 20, 20, 380 } : new[] { 570, 20, 20, 370, 20 };
                case "rd": return k == 4 ? new[] { 20, 480, 480, 20 } : new[] { 20, 470, 470, 20, 20 };
                default: throw new System.ArgumentException("unknown start '" + name + "' (uniform, hawk, ret, hb, rd)");
            }
        }

        public static string[] StartNames() => new[] { "uniform", "hawk", "ret", "hb", "rd" };

        /// <summary>
        /// The basin a state is in: "H/B" when Hawk + Bully hold at least 0.8 (the Hawk/Bully attractor, with or without
        /// a small Dove fringe), "R" when Retaliator + Dove (+ Prober) hold at least 0.8 (the Retaliator/Dove edge),
        /// otherwise "mixed" (for example the Hawk/Dove mix, or a mutation-dominated state).
        /// </summary>
        public static string Region(long[] x)
        {
            long hb = x[0] + x[3];
            long rd = x[1] + x[2] + (x.Length > 4 ? x[4] : 0);
            if (hb >= Fx.FromPermille(800)) return "H/B";
            if (rd >= Fx.FromPermille(800)) return "R";
            return "mixed";
        }

        /// <summary>Q30 shares: (1 - eps) x + eps e_j (eps in permille), summing to exactly 2^30.</summary>
        public static long[] WithMutant(long[] x, int j, int epsPermille)
        {
            var y = new long[x.Length];
            long keep = Fx.One - Fx.FromPermille(epsPermille);
            for (int i = 0; i < x.Length; i++) y[i] = Fx.Mul(x[i], keep);
            y[j] += Fx.FromPermille(epsPermille);
            Replicator.Normalise(y);
            return y;
        }

        public static long[] ToQ30(Rational[] x)
        {
            var y = new long[x.Length];
            for (int i = 0; i < x.Length; i++) y[i] = x[i].ToQ30();
            Replicator.Normalise(y);
            return y;
        }

        /// <summary>Agent counts for a Q30 state with m extra agents of strategy j (n in total).</summary>
        public static int[] Counts(long[] x, int n, int j, int m)
        {
            int rest = n - m;
            var c = new int[x.Length];
            int sum = 0;
            for (int i = 0; i < x.Length; i++) { c[i] = (int)Fx.MulDivFloor(x[i], rest, Fx.One); sum += c[i]; }
            int big = 0;
            for (int i = 1; i < c.Length; i++) if (c[i] > c[big]) big = i;
            c[big] += rest - sum;
            if (j >= 0) c[j] += m;
            return c;
        }

        // ---------------- runs ----------------

        /// <summary>The replicator from x0 for gens generations; returns the final state.</summary>
        public static long[] RunReplicator(long[,] a, long[] x0, int gens, long w0X1000, int muPermille)
        {
            var rep = new Replicator(a, w0X1000, muPermille > 0 ? Replicator.MuQ20(muPermille, 1000) : 0);
            return LabMath.Final(rep, x0, gens);
        }

        /// <summary>The replicator series (every generation, gen 0 included).</summary>
        public static long[][] ReplicatorSeries(long[,] a, long[] x0, int gens, long w0X1000, int muPermille)
        {
            var rep = new Replicator(a, w0X1000, muPermille > 0 ? Replicator.MuQ20(muPermille, 1000) : 0);
            return LabMath.RunReplicator(rep, x0, gens);
        }

        /// <summary>The game agents play: realised turn-rule contests (as L2) or the expected matrix (no outcome noise).</summary>
        public static IPairGame Game(string matrix, bool expected, ContestRules r)
        {
            if (IsTurnRule(matrix) && !expected)
            {
                int k = matrix == "l2" ? 4 : 5;
                var kinds = new ContestKind[k];
                System.Array.Copy(L02Retaliator.Hdrbp(), kinds, k);
                return new ContestGame(kinds, false, r);
            }
            return new MatrixGame(Named(matrix, r));
        }

        /// <summary>One agent run on the study's own keyed stream (lab/L2s/tag), so it never shares draws with L2.</summary>
        public static AgentRunResult RunAgents(IPairGame game, int[] counts, int gens, int rounds, long w0X1000, int muPermille, ulong seed, string tag)
        {
            int n = 0;
            foreach (var c in counts) n += c;
            var s = new AgentRunSpec
            {
                N = n, K = counts.Length, InitialCounts = counts, Rounds = rounds, Generations = gens, W0X1000 = w0X1000,
                MutationPermille = muPermille,
            };
            return AgentEngine.Run(s, game, LabRandom.Root(seed, "L2s", tag));
        }

        /// <summary>Mean shares over generations [from, to] (Q30).</summary>
        public static long[] MeanShares(long[][] series, int from, int to)
        {
            int k = series[0].Length;
            var m = new long[k];
            for (int i = 0; i < k; i++) m[i] = LabMath.Mean(series, i, from, to);
            return m;
        }

        /// <summary>
        /// Basin census of the no-mutation replicator: every start on the simplex grid with step 1/steps (each strategy
        /// at least one step, so every strategy is present), run for gens generations, then classified by
        /// <see cref="Region"/>. Returns counts for R, H/B, mixed.
        /// </summary>
        public static int[] Basins(long[,] a, int steps, int gens, long w0X1000, int muPermille)
        {
            int k = a.GetLength(0);
            var counts = new int[3];
            var cur = new int[k];
            Grid(a, k, steps, gens, w0X1000, muPermille, cur, 0, steps, counts);
            return counts;
        }

        private static void Grid(long[,] a, int k, int steps, int gens, long w0, int mu, int[] cur, int i, int left, int[] counts)
        {
            if (i == k - 1)
            {
                if (left < 1) return;
                cur[i] = left;
                var x = new long[k];
                for (int t = 0; t < k; t++) x[t] = Fx.FromRatio(cur[t], steps);
                Replicator.Normalise(x);
                var end = RunReplicator(a, x, gens, w0, mu);
                string r = Region(end);
                counts[r == "R" ? 0 : r == "H/B" ? 1 : 2]++;
                return;
            }
            for (int v = 1; v <= left - (k - 1 - i); v++)
            {
                cur[i] = v;
                Grid(a, k, steps, gens, w0, mu, cur, i + 1, left - v, counts);
            }
        }
    }
}
