namespace God.Sim.Lab
{
    /// <summary>A game between two strategy indices; payoffs x1000. Random outcomes are realised from the given stream.</summary>
    public interface IPairGame
    {
        void Play(int sa, int sb, Rng rng, out long payA, out long payB);
    }

    /// <summary>Selection helpers shared by the agent-based experiments.</summary>
    public static class Selection
    {
        /// <summary>
        /// Wright-Fisher: <paramref name="n"/> parents drawn with replacement, each with probability proportional to its
        /// (positive) weight. Cumulative sums and a binary search; one unbiased draw per offspring.
        /// </summary>
        public static void WrightFisher(long[] weights, int count, int n, Rng rng, int[] parents, long[] cumScratch)
        {
            long total = 0;
            for (int i = 0; i < count; i++)
            {
                long w = weights[i] > 0 ? weights[i] : 0;
                total += w;
                cumScratch[i] = total;
            }
            if (total <= 0)
            {
                for (int i = 0; i < n; i++) parents[i] = i % count;
                return;
            }
            for (int k = 0; k < n; k++)
            {
                long u = LabRandom.NextLong(rng, total);
                int lo = 0, hi = count - 1;
                while (lo < hi)
                {
                    int mid = (lo + hi) >> 1;
                    if (cumScratch[mid] > u) hi = mid; else lo = mid + 1;
                }
                parents[k] = lo;
            }
        }

        /// <summary>Soft-selection weights exp(beta × payoff), beta = betaNum / betaDen per payoff unit, payoffs x1000.
        /// Computed relative to the maximum payoff (so the largest weight is 1 in Q30; the ratio is unchanged).</summary>
        public static void SoftWeights(long[] payX1000, int count, long betaNum, long betaDen, long[] weights)
        {
            long max = long.MinValue;
            for (int i = 0; i < count; i++) if (payX1000[i] > max) max = payX1000[i];
            for (int i = 0; i < count; i++)
            {
                long arg = Fx.MulDiv(payX1000[i] - max, Fx.One * betaNum, betaDen * 1000);
                long w = Fx.Exp(arg);
                weights[i] = w > 0 ? w : 1;
            }
        }
    }

    /// <summary>Settings for <see cref="AgentEngine.Run"/>.</summary>
    public sealed class AgentRunSpec
    {
        public int N = 1000;
        public int K = 2;
        /// <summary>Initial agent count per strategy (assigned to ids in ascending order of strategy); must sum to N.</summary>
        public int[] InitialCounts;
        /// <summary>k: pairing rounds per generation (each agent plays at most one game per round).</summary>
        public int Rounds = 10;
        public int Generations = 300;
        public long W0X1000 = 500000;
        public int MutationPermille;
        /// <summary>Stop when one strategy holds every agent and there is no mutation (the rest of the series repeats it).</summary>
        public bool StopAtFixation = true;

        public static int[] Counts(int n, params int[] permille)
        {
            var c = new int[permille.Length];
            int sum = 0;
            for (int i = 0; i < c.Length; i++) { c[i] = (int)((long)n * permille[i] / 1000); sum += c[i]; }
            int big = 0;
            for (int i = 1; i < c.Length; i++) if (c[i] > c[big]) big = i;
            c[big] += n - sum;
            return c;
        }
    }

    public sealed class AgentRunResult
    {
        /// <summary>[gen][strategy] shares, Q30 (gen 0 = the start).</summary>
        public long[][] Shares;
        /// <summary>[gen][strategy] mean payoff per game, x1000 (0 when no agent of that strategy played).</summary>
        public long[][] MeanPayX1000;
        public int GenerationsRun;
    }

    /// <summary>
    /// The agent-based engine (4.5): N agents with a strategy index; each generation k rounds of random pairing (a
    /// Fisher-Yates shuffle on the keyed stream <c>pair/gen/round</c>, neighbours paired), one game per pair with realised
    /// random outcomes (stream <c>game/gen/round</c>), fitness max(1, w0 + sum of payoffs), Wright-Fisher sampling
    /// (stream <c>sel/gen</c>), then mutation to a uniformly chosen other strategy (stream <c>mut/gen</c>).
    /// </summary>
    public static class AgentEngine
    {
        public static AgentRunResult Run(AgentRunSpec s, IPairGame game, Rng root)
        {
            int n = s.N, k = s.K;
            var strat = new int[n];
            int id = 0;
            for (int i = 0; i < k; i++) for (int c = 0; c < s.InitialCounts[i]; c++) strat[id++] = i;
            if (id != n) throw new System.ArgumentException("initial counts must sum to N");
            var perm = new int[n];
            var pay = new long[n];
            var fit = new long[n];
            var cum = new long[n];
            var parents = new int[n];
            var next = new int[n];
            var sumPay = new long[k];
            var games = new long[k];
            var counts = new int[k];
            var res = new AgentRunResult { Shares = new long[s.Generations + 1][], MeanPayX1000 = new long[s.Generations + 1][] };
            int gen = 0;
            for (; gen <= s.Generations; gen++)
            {
                System.Array.Clear(counts, 0, k);
                for (int a = 0; a < n; a++) counts[strat[a]]++;
                var sh = new long[k];
                for (int i = 0; i < k; i++) sh[i] = Fx.MulDivFloor(counts[i], Fx.One, n);
                Replicator.Normalise(sh);
                res.Shares[gen] = sh;
                if (gen == s.Generations) { res.MeanPayX1000[gen] = new long[k]; break; }

                System.Array.Clear(pay, 0, n);
                System.Array.Clear(sumPay, 0, k);
                System.Array.Clear(games, 0, k);
                for (int r = 0; r < s.Rounds; r++)
                {
                    var pr = root.Fork("pair", gen, r);
                    var gr = root.Fork("game", gen, r);
                    LabRandom.Permutation(pr, perm, n);
                    for (int p = 0; p + 1 < n; p += 2)
                    {
                        int a = perm[p], b = perm[p + 1];
                        game.Play(strat[a], strat[b], gr, out long pa, out long pb);
                        pay[a] += pa; pay[b] += pb;
                        sumPay[strat[a]] += pa; games[strat[a]]++;
                        sumPay[strat[b]] += pb; games[strat[b]]++;
                    }
                }
                var mp = new long[k];
                for (int i = 0; i < k; i++) mp[i] = games[i] > 0 ? sumPay[i] / games[i] : 0;
                res.MeanPayX1000[gen] = mp;

                bool fixedNow = false;
                for (int i = 0; i < k; i++) if (counts[i] == n) fixedNow = true;
                if (fixedNow && s.MutationPermille == 0 && s.StopAtFixation)
                {
                    for (int g = gen + 1; g <= s.Generations; g++) { res.Shares[g] = (long[])sh.Clone(); res.MeanPayX1000[g] = (long[])mp.Clone(); }
                    res.GenerationsRun = gen;
                    return res;
                }

                for (int a = 0; a < n; a++) { long f = s.W0X1000 + pay[a]; fit[a] = f < 1 ? 1 : f; }
                Selection.WrightFisher(fit, n, n, root.Fork("sel", gen), parents, cum);
                var mr = root.Fork("mut", gen);
                for (int a = 0; a < n; a++)
                {
                    int st = strat[parents[a]];
                    if (s.MutationPermille > 0 && k > 1 && LabRandom.Permille(mr, s.MutationPermille))
                    {
                        int o = mr.NextInt(k - 1);
                        st = o >= st ? o + 1 : o;
                    }
                    next[a] = st;
                }
                System.Array.Copy(next, strat, n);
            }
            res.GenerationsRun = s.Generations;
            return res;
        }
    }
}
