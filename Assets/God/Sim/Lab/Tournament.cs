namespace God.Sim.Lab
{
    /// <summary>
    /// A round robin of repeated games (STRATEGY_BENCHMARKS 4.6 L8): every strategy plays every other and itself, a fixed
    /// length or a geometric length, averaged over replicate seeds. Each match and side has its own keyed stream
    /// (<c>match/replicate/pair</c>, then <c>a</c> or <c>b</c>), so adding a strategy never changes another pair's draws.
    /// </summary>
    public sealed class Tournament
    {
        public RepeatedKind[] Kinds;
        public int Rounds = 200;
        /// <summary>When Rounds is 0: continuation probability per round, permille.</summary>
        public int ContinuePermille;
        public PdPayoffs Pay = PdPayoffs.Axelrod;
        /// <summary>Execution noise: each move is flipped with this probability (permille); 0 = none.</summary>
        public int NoisePermille;

        /// <summary>[i, j] = mean score of i against j (x1000) over the replicates; self-play scores both seats.</summary>
        public long[,] MeanScoresX1000;
        /// <summary>[i, j] = i's score minus j's score in the match, mean over replicates (x1000).</summary>
        public long[,] MeanLeadX1000;
        /// <summary>[i, j] = the smallest score of i against j in any replicate (x1000).</summary>
        public long[,] MinScoresX1000;
        /// <summary>[i] = mean over j of MeanScoresX1000[i, j].</summary>
        public long[] TotalX1000;

        public void Run(ulong seed, string experiment, int replicates)
        {
            int k = Kinds.Length;
            var sum = new long[k, k];
            var lead = new long[k, k];
            var min = new long[k, k];
            for (int i = 0; i < k; i++) for (int j = 0; j < k; j++) min[i, j] = long.MaxValue;
            var geo = Rounds > 0 ? null : new GeometricLength(ContinuePermille);
            for (int rep = 0; rep < replicates; rep++)
            {
                var root = LabRandom.Root(seed + (ulong)rep, experiment, "round-robin");
                for (int i = 0; i < k; i++)
                for (int j = i; j < k; j++)
                {
                    var mr = root.Fork("match", i, j);
                    int len = Rounds > 0 ? Rounds : geo.Draw(mr.Fork("length"));
                    Play(Kinds[i], Kinds[j], len, mr.Fork("a"), mr.Fork("b"), mr.Fork("noise"), out long a, out long b);
                    if (i == j)
                    {
                        long s = (a + b) * 500; // mean of both seats, x1000
                        sum[i, i] += s;
                        if (s < min[i, i]) min[i, i] = s;
                    }
                    else
                    {
                        sum[i, j] += a * 1000; sum[j, i] += b * 1000;
                        lead[i, j] += (a - b) * 1000; lead[j, i] += (b - a) * 1000;
                        if (a * 1000 < min[i, j]) min[i, j] = a * 1000;
                        if (b * 1000 < min[j, i]) min[j, i] = b * 1000;
                    }
                }
            }
            MeanScoresX1000 = new long[k, k];
            MeanLeadX1000 = new long[k, k];
            MinScoresX1000 = min;
            TotalX1000 = new long[k];
            for (int i = 0; i < k; i++)
            {
                long t = 0;
                for (int j = 0; j < k; j++)
                {
                    MeanScoresX1000[i, j] = sum[i, j] / replicates;
                    MeanLeadX1000[i, j] = lead[i, j] / replicates;
                    t += MeanScoresX1000[i, j];
                }
                TotalX1000[i] = t / k;
            }
        }

        private void Play(RepeatedKind a, RepeatedKind b, int rounds, Rng ra, Rng rb, Rng noise, out long sa, out long sb)
        {
            var ma = new PairMemory { KnownLength = Rounds };
            var mb = new PairMemory { KnownLength = Rounds };
            int pa = RepeatedRules.DefaultParam(a), pb = RepeatedRules.DefaultParam(b);
            sa = 0; sb = 0;
            for (int r = 0; r < rounds; r++)
            {
                Move x = RepeatedRules.Decide(a, ref ma, pa, ra);
                Move y = RepeatedRules.Decide(b, ref mb, pb, rb);
                if (NoisePermille > 0)
                {
                    if (LabRandom.Permille(noise, NoisePermille)) x = RepeatedRules.Flip(x);
                    if (LabRandom.Permille(noise, NoisePermille)) y = RepeatedRules.Flip(y);
                }
                sa += Pay.Row(x, y); sb += Pay.Row(y, x);
                RepeatedRules.Observe(ref ma, x, y);
                RepeatedRules.Observe(ref mb, y, x);
            }
        }

        /// <summary>Strategy indices sorted by total score, best first (ties: lower index first).</summary>
        public int[] Ranking()
        {
            int k = Kinds.Length;
            var idx = new int[k];
            for (int i = 0; i < k; i++) idx[i] = i;
            for (int i = 1; i < k; i++)
            {
                int v = idx[i]; int j = i - 1;
                while (j >= 0 && (TotalX1000[idx[j]] < TotalX1000[v] || (TotalX1000[idx[j]] == TotalX1000[v] && idx[j] > v))) { idx[j + 1] = idx[j]; j--; }
                idx[j + 1] = v;
            }
            return idx;
        }
    }
}
