using System.Collections.Generic;

namespace God.Sim.Lab
{
    /// <summary>L1. Hawk/Dove (PDF 93 to 96). STRATEGY_BENCHMARKS 4.6 L1; ACCEPTANCE M2.3, M2.15.</summary>
    public sealed class L01HawkDove : LabExperiment
    {
        public override string Id => "L1";
        public override string Title => "Hawk/Dove";
        public override string Source => "PDF 93-96";
        public override ReproStatus Status => ReproStatus.Exact;
        public override string StatusText => "Exact: book payoffs and ESS (7/12 Hawks, mean payoff 6.25); group optimum and phase reader derived by us from the same payoffs";
        public override string ExpectedSummary => "replicator 583 +/- 3 permille from 0.01/0.5/0.99; agents N=1000 0.583 +/- 0.03 (N=100 +/- 0.05); ESS payoff 6.25 +/- 0.5 < 0.5 x optimum 16.67; one Hawk invades; mixed mean 0.583 +/- 0.03; phase reader gains >= 10; N=10000 agents within 0.03 of the replicator; w0=100 fails";

        public static long[,] MatrixX1000() => new long[,] { { -25000, 50000 }, { 0, 15000 } };

        public static ContestKind[] Kinds() => new[] { ContestKind.Hawk, ContestKind.Dove };

        protected override void Execute(LabConfig cfg, LabResult res, CheckList ck, List<string> notes)
        {
            var A = MatrixX1000();
            int w0 = cfg.BaselineOr(500000);
            int n = cfg.AgentsOr(1000);
            int gens = cfg.GenerationsOr(300);
            int mu = cfg.MutationOr(10);
            int k = cfg.ContestsOr(10);
            var game = new ContestGame(Kinds(), false, ContestRules.Lab);
            res.Strategies = new[] { "Hawk", "Dove" };

            // The contest rules give the book's matrix exactly.
            var r = ContestRules.Lab;
            bool same = Contest.ExpectedX1000(ContestKind.Hawk, ContestKind.Hawk, false, r) == A[0, 0] && Contest.ExpectedX1000(ContestKind.Hawk, ContestKind.Dove, false, r) == A[0, 1]
                && Contest.ExpectedX1000(ContestKind.Dove, ContestKind.Hawk, false, r) == A[1, 0] && Contest.ExpectedX1000(ContestKind.Dove, ContestKind.Dove, false, r) == A[1, 1];
            ck.True("turn rules give the book matrix (HH -25, HD 50, DH 0, DD 15)", same, "exact");

            // Replicator fixed point from three starts (test 10).
            var rep = new Replicator(A, 500000);
            long fixedFx = 0;
            foreach (int start in new[] { 10, 500, 990 })
            {
                var x = LabMath.Final(rep, Replicator.SharesPermille(start, 1000 - start), 3000);
                if (start == 500) fixedFx = x[0];
                ck.Near("replicator Hawk share from " + Fx.FormatScaled(start, 3), LabMath.Micro(x[0]), 583000, 3000, source: "L1, test 10");
            }
            // Mean payoff at the ESS, from the replicator fixed point.
            var xs = new[] { fixedFx, Fx.One - fixedFx };
            long payEss = MeanPayoffX1000(A, xs);
            ck.Near("mean payoff at the ESS (replicator)", LabMath.MicroX1000(payEss), 6250000, 500000, 3);
            // Group optimum: scan p in permille, exact integer mean payoff (x1000 x 10^6).
            long bestPay = long.MinValue; int bestP = 0;
            for (int p = 0; p <= 1000; p++)
            {
                long[] pp = { p, 1000 - p };
                long pay = 0;
                for (int i = 0; i < 2; i++) for (int j = 0; j < 2; j++) pay += A[i, j] * pp[i] * pp[j];
                if (pay > bestPay) { bestPay = pay; bestP = p; }
            }
            bestPay /= 1000000; // x1000
            ck.Near("group optimum Hawk share (derived)", bestP * 1000L, 167000, 1000, 3, status: ReproStatus.Derived);
            ck.Near("group optimum mean payoff (derived)", LabMath.MicroX1000(bestPay), 16667000, 10000, 2, status: ReproStatus.Derived);
            ck.Add("ESS payoff < 0.5 x optimum (derived)", LabMath.MicroX1000(payEss), "< " + Fx.FormatScaled(bestPay / 2, 3), payEss * 2 < bestPay, decimals: 3, status: ReproStatus.Derived);

            // Agents, N=1000 (the main series) and N=100.
            int from = gens / 3, to = gens;
            var main = RunAgents(cfg.Seed, "agents", n, k, gens, w0, mu, game, 500);
            res.SharesPerGeneration = main.Shares;
            res.MeanPayoffX1000 = main.MeanPayX1000;
            res.SeriesTitle = "agents N=" + n + ", k=" + k + ", w0=" + (w0 / 1000) + ", mu=" + mu + " permille";
            long hawkN1000 = LabMath.Mean(main.Shares, 0, from, to);
            ck.Near("agents N=" + n + " Hawk share, generations " + from + "-" + to, LabMath.Micro(hawkN1000), 583000, 30000);
            long payAg = MeanAgentPayoff(main, from, to);
            ck.Report("agents mean payoff per contest, generations " + from + "-" + to, LabMath.MicroX1000(payAg), "near 6.25; it rises as the Hawk share drifts below 7/12 (dW/dp = -50 at the ESS)", 3);
            var small = RunAgents(cfg.Seed, "agents-n100", 100, k, gens, w0, mu, game, 500);
            ck.Near("agents N=100 Hawk share", LabMath.Micro(LabMath.Mean(small.Shares, 0, from, to)), 583000, 50000);

            // A conspiracy of Doves is invaded: one Hawk among 999 Doves.
            var inv = new AgentRunSpec { N = 1000, K = 2, InitialCounts = new[] { 1, 999 }, Rounds = 10, Generations = 30, W0X1000 = 500000, MutationPermille = 10 };
            var invR = AgentEngine.Run(inv, game, LabRandom.Root(cfg.Seed, Id, "one-hawk"));
            long hawkPay0 = invR.MeanPayX1000[0][0], dovePay0 = invR.MeanPayX1000[0][1];
            ck.Add("one Hawk among 999 Doves earns more (Hawk vs Dove payoff, generation 0)", LabMath.MicroX1000(hawkPay0), "50 against " + Fx.FormatScaled(dovePay0, 3), hawkPay0 == 50000 && dovePay0 > 14000 && dovePay0 <= 15000, decimals: 3);
            int pass03 = -1;
            for (int g = 0; g < invR.Shares.Length; g++) if (invR.Shares[g][0] > Fx.FromPermille(300)) { pass03 = g; break; }
            ck.Add("generation at which Hawk share passes 0.3 (from one Hawk, mu=10 permille)", pass03 * 1000000L, "<= 30", pass03 >= 0 && pass03 <= 30, decimals: 0,
                note: "With the L1 mutation rate (10 permille) mutation also supplies about 5 Hawks a generation; the no-mutation case is reported next.");
            int invaded = 0;
            for (int i = 0; i < 20; i++)
            {
                var s0 = new AgentRunSpec { N = 1000, K = 2, InitialCounts = new[] { 1, 999 }, Rounds = 10, Generations = 30, W0X1000 = 500000, MutationPermille = 0 };
                var r0 = AgentEngine.Run(s0, game, LabRandom.Root(cfg.Seed + (ulong)i, Id, "one-hawk-nomut"));
                if (r0.Shares[30][0] > Fx.FromPermille(300)) invaded++;
            }
            ck.Report("one Hawk, no mutation: share of 20 seeds where Hawks pass 0.3 within 30 generations", invaded * 50000L, "most (a single mutant can be lost to drift)", 2);

            // Mixed strategy: each agent plays Hawk with its own heritable probability.
            long mixed = MixedStrategyMean(cfg.Seed, n, k, gens, w0, game, from, to);
            ck.Near("per-agent mixed strategy: population mean Hawk probability", LabMath.Micro(mixed), 583000, 30000);

            // A predictable player (7 Hawk then 5 Dove) against a reader that knows the phase.
            long readerSum = 0;
            for (int t = 0; t < 1200; t++)
            {
                int opp = t % 12 < 7 ? 0 : 1;
                int best = A[0, opp] >= A[1, opp] ? 0 : 1;
                readerSum += A[best, opp];
            }
            long reader = readerSum / 1200;
            ck.Add("phase reader payoff per contest (derived)", LabMath.MicroX1000(reader), "20.833 (5/12 x 50)", reader == 20833, decimals: 3, status: ReproStatus.Derived);
            ck.Add("phase reader gain over the random mix (derived)", LabMath.MicroX1000(reader - payEss), ">= 10", reader - payEss >= 10000, decimals: 3, status: ReproStatus.Derived);

            // M2.15: N=10000 agents against the replicator.
            var big = RunAgents(cfg.Seed, "agents-n10000", 10000, k, gens, w0, mu, game, 500);
            long hawkBig = LabMath.Mean(big.Shares, 0, from, to);
            ck.Near("agents N=10000 Hawk share against the replicator value " + Fx.Format(fixedFx, 3), LabMath.Micro(hawkBig), LabMath.Micro(fixedFx), 30000, source: "L1, test 16");

            // Negative control (test 17): w0 = 100 clips unlucky Hawks and biases the share.
            var biased = RunAgents(cfg.Seed, "agents-w0-100", n, k, gens, 100000, mu, game, 500);
            long hawkBiased = LabMath.Mean(biased.Shares, 0, from, to);
            long dev = LabMath.Micro(hawkBiased) - 583000;
            ck.Add("negative control: w0=100 misses the Hawk tolerance (clipping bias)", LabMath.Micro(hawkBiased), "outside 0.583 +/- 0.03 (proto 0.64)", dev > 30000 || dev < -30000, source: "L1, test 17");

            notes.Add("The contest turn rules of 4.2 give the book's Hawk/Dove matrix exactly; agent games use realised coin flips.");
            notes.Add("Status Exact: the 7/12 ESS and the 6.25 mean payoff are the book's own numbers, and the run reproduces them; the group optimum and the phase reader are derived.");
        }

        private static AgentRunResult RunAgents(ulong seed, string part, int n, int k, int gens, int w0, int mu, IPairGame game, int startPermille)
        {
            var s = new AgentRunSpec { N = n, K = 2, InitialCounts = AgentRunSpec.Counts(n, startPermille, 1000 - startPermille), Rounds = k, Generations = gens, W0X1000 = w0, MutationPermille = mu };
            return AgentEngine.Run(s, game, LabRandom.Root(seed, "L1", part));
        }

        public static long MeanPayoffX1000(long[,] a, long[] x)
        {
            int k = x.Length;
            long total = 0;
            for (int i = 0; i < k; i++)
                for (int j = 0; j < k; j++)
                    total += Fx.MulDiv(Fx.Mul(x[i], x[j]), a[i, j], Fx.One);
            return total;
        }

        private static long MeanAgentPayoff(AgentRunResult r, int from, int to)
        {
            long sum = 0; int n = 0;
            for (int g = from; g < to && g < r.MeanPayX1000.Length; g++)
            {
                long pg = 0;
                for (int i = 0; i < 2; i++) pg += Fx.MulDiv(r.Shares[g][i], r.MeanPayX1000[g][i], Fx.One);
                sum += pg; n++;
            }
            return n > 0 ? sum / n : 0;
        }

        /// <summary>Each agent has a heritable Hawk probability p (Q30), uniform at the start; each contest it plays Hawk with
        /// probability p; offspring copy p and, with probability 10 permille, add Gaussian-like noise of 0.05 (clamped to [0, 1]).</summary>
        private static long MixedStrategyMean(ulong seed, int n, int k, int gens, int w0, ContestGame game, int from, int to)
        {
            var root = LabRandom.Root(seed, "L1", "mixed");
            var p = new long[n];
            var init = root.Fork("init");
            for (int a = 0; a < n; a++) p[a] = LabRandom.Unit(init);
            var perm = new int[n];
            var pay = new long[n];
            var fit = new long[n];
            var cum = new long[n];
            var parents = new int[n];
            var next = new long[n];
            long sumMean = 0; int count = 0;
            for (int gen = 0; gen < gens; gen++)
            {
                long mean = 0;
                for (int a = 0; a < n; a++) mean += p[a];
                mean /= n;
                if (gen >= from && gen <= to) { sumMean += mean; count++; }
                System.Array.Clear(pay, 0, n);
                for (int r = 0; r < k; r++)
                {
                    LabRandom.Permutation(root.Fork("pair", gen, r), perm, n);
                    var gr = root.Fork("game", gen, r);
                    for (int i = 0; i + 1 < n; i += 2)
                    {
                        int a = perm[i], b = perm[i + 1];
                        int sa = LabRandom.ChanceFx(gr, p[a]) ? 0 : 1;
                        int sb = LabRandom.ChanceFx(gr, p[b]) ? 0 : 1;
                        game.Play(sa, sb, gr, out long pa, out long pb);
                        pay[a] += pa; pay[b] += pb;
                    }
                }
                for (int a = 0; a < n; a++) { long f = w0 + pay[a]; fit[a] = f < 1 ? 1 : f; }
                Selection.WrightFisher(fit, n, n, root.Fork("sel", gen), parents, cum);
                var mr = root.Fork("mut", gen);
                for (int a = 0; a < n; a++)
                {
                    long v = p[parents[a]];
                    if (LabRandom.Permille(mr, 10))
                    {
                        v += LabRandom.Gaussian(mr) / 20;
                        if (v < 0) v = 0;
                        if (v > Fx.One) v = Fx.One;
                    }
                    next[a] = v;
                }
                System.Array.Copy(next, p, n);
            }
            return count > 0 ? sumMean / count : 0;
        }
    }
}
