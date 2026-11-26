using System.Collections.Generic;

namespace God.Sim.Lab
{
    /// <summary>L5. Pecking orders (PDF 104, 105). STRATEGY_BENCHMARKS 4.6 L5; ACCEPTANCE M2.7.</summary>
    public sealed class L05PeckingOrder : LabExperiment
    {
        public override string Id => "L5";
        public override string Title => "Pecking orders (hen and cricket rules)";
        public override string Source => "PDF 104-105";
        public override ReproStatus Status => ReproStatus.Invented;
        public override string StatusText => "Invented model: group size, strengths and both rules are ours; only the qualitative claim (memory of past contests reduces fighting) is tested, with wide tolerances";
        public override string ExpectedSummary => "hens: last-100 fight share <= 0.02; crickets: last/first between 0.7 and 1.3; hen last-100 fights < cricket last-100 fights; Landau's h reported only";

        private const int GroupSize = 8, Contests = 400;

        protected override void Execute(LabConfig cfg, LabResult res, CheckList ck, List<string> notes)
        {
            int groups = cfg.ParamInt("groups", 60);
            Run(cfg.Seed, groups, true, out long henFirst, out long henLast, out long henH, out long[] henBlocks);
            Run(cfg.Seed, groups, false, out long crFirst, out long crLast, out long crH, out long[] crBlocks);
            long total = (long)groups * 100;
            ck.Report("hens: fight share in the first 100 contests", LabMath.MicroRatio(henFirst, total), "proto 0.07");
            ck.AtMost("hens: fight share in the last 100 contests", LabMath.MicroRatio(henLast, total), 20000);
            ck.Report("crickets: fight share in the first 100 contests", LabMath.MicroRatio(crFirst, total), "proto 0.25");
            ck.Report("crickets: fight share in the last 100 contests", LabMath.MicroRatio(crLast, total), "proto 0.25");
            ck.Between("crickets: last-100 over first-100 fight share", crFirst > 0 ? LabMath.MicroRatio(crLast, crFirst) : 0, 700000, 1300000);
            ck.Add("hen last-100 fights < cricket last-100 fights", henLast * 1000000L, "< " + crLast, henLast < crLast, decimals: 0);
            ck.Report("Landau's h, hens (mean over groups)", henH, "reported only (proto 0.33)");
            ck.Report("Landau's h, crickets (mean over groups)", crH, "reported only (proto 0.54)");
            res.Strategies = new[] { "hen_fight_share", "cricket_fight_share" };
            var series = new long[henBlocks.Length][];
            for (int i = 0; i < henBlocks.Length; i++) series[i] = new[] { Fx.FromRatio(henBlocks[i], groups * 20L), Fx.FromRatio(crBlocks[i], groups * 20L) };
            res.SharesPerGeneration = series;
            res.SeriesTitle = "fight share per block of 20 contests (pooled over " + groups + " groups)";
            notes.Add("A contest: each side escalates or not; both escalate = a fight won with probability s_i/(s_i+s_j); one escalates = it wins; neither = a coin flip. Every contest has a winner.");
            notes.Add("Cricket streak: s' = s/2 + 1 for a win, - 1 for a loss (permille units), escalation probability 0.5 + 0.1 s, clamped to [0, 1].");
        }

        private static void Run(ulong seed, int groups, bool hens, out long first, out long last, out long hMicro, out long[] blocks)
        {
            first = 0; last = 0; long hSum = 0;
            blocks = new long[Contests / 20];
            for (int g = 0; g < groups; g++)
            {
                var rng = LabRandom.Root(seed, "L5", hens ? "hen" : "cricket").Fork("group", g);
                var str = new int[GroupSize];
                for (int i = 0; i < GroupSize; i++) str[i] = rng.NextRange(10, 100);
                var lost = new bool[GroupSize, GroupSize]; // lost[i, j]: i has lost to j
                var streak = new int[GroupSize];          // permille
                var wins = new int[GroupSize, GroupSize];
                for (int c = 0; c < Contests; c++)
                {
                    int i = rng.NextInt(GroupSize);
                    int j = rng.NextInt(GroupSize - 1);
                    if (j >= i) j++;
                    bool ei = Escalates(hens, i, j, lost, streak, rng), ej = Escalates(hens, j, i, lost, streak, rng);
                    bool fight = ei && ej;
                    bool iWins;
                    if (fight) iWins = rng.NextInt(str[i] + str[j]) < str[i];
                    else if (ei != ej) iWins = ei;
                    else iWins = rng.NextInt(2) == 0;
                    if (fight)
                    {
                        if (c < 100) first++;
                        if (c >= Contests - 100) last++;
                        blocks[c / 20]++;
                    }
                    int w = iWins ? i : j, l = iWins ? j : i;
                    lost[l, w] = true;
                    wins[w, l]++;
                    streak[w] = streak[w] / 2 + 1000;
                    streak[l] = streak[l] / 2 - 1000;
                }
                hSum += Landau(wins);
            }
            hMicro = hSum / groups;
        }

        private static bool Escalates(bool hens, int me, int other, bool[,] lost, int[] streak, Rng rng)
        {
            if (hens) return !lost[me, other] && rng.NextInt(2) == 0;
            int p = 500 + streak[me] / 10;
            if (p < 0) p = 0;
            if (p > 1000) p = 1000;
            return LabRandom.Permille(rng, p);
        }

        /// <summary>Landau's h = 12 / (n^3 - n) × sum (v_i - (n-1)/2)^2, v_i = number of others i dominates (more wins), micro.</summary>
        private static long Landau(int[,] wins)
        {
            int n = GroupSize;
            long sum = 0; // sum of (2 v - (n - 1))^2 = 4 (v - (n-1)/2)^2
            for (int i = 0; i < n; i++)
            {
                int v = 0;
                for (int j = 0; j < n; j++) if (i != j && wins[i, j] > wins[j, i]) v++;
                long d = 2L * v - (n - 1);
                sum += d * d;
            }
            return 12L * sum * 1000000 / (4L * (n * n * n - n));
        }
    }
}
