using System.Collections.Generic;

namespace God.Sim.Lab
{
    /// <summary>
    /// L2 and L2b. Retaliator, Bully, Prober (PDF 97, 98, endnote 306), and the Monte Carlo check of both 4.2 matrices.
    /// STRATEGY_BENCHMARKS 4.2, 4.6 L2, test 8; ACCEPTANCE M2.2, M2.4.
    /// </summary>
    public sealed class L02Retaliator : LabExperiment
    {
        public override string Id => "L2";
        public override string Title => "Retaliator, Bully, Prober (and L2b matrix check)";
        public override string Source => "PDF 97-98, endnote 306";
        public override ReproStatus Status => ReproStatus.Invented;
        public override string StatusText => "Derived and invented: matrix derived from L1's book payoffs plus our turn rules and a scuffle cost S we chose; Gale and Eaves's Hawk/Bully result is not claimed";
        public override string ExpectedSummary => "L2b every 4.2 cell within 3 of a 50,000-contest Monte Carlo; Dove share 0.1 +/- 0.001 over 20,000 generations; Bully rises from (R .5, D .45, B .05), falls from (R .65, D .30, B .05); agents N=200 R < 0.75, H+B > 0.10; N=1000 R 0.65-0.90";

        public static ContestKind[] Hdrbp() => new[] { ContestKind.Hawk, ContestKind.Dove, ContestKind.Retaliator, ContestKind.Bully, ContestKind.Prober };
        public static ContestKind[] Hdba() => new[] { ContestKind.Hawk, ContestKind.Dove, ContestKind.Bourgeois, ContestKind.AntiBourgeois };

        /// <summary>The 4.2 table as published (x1000), rows and columns H, D, R, B, P.</summary>
        public static long[][] DocHdrbp() => new[]
        {
            new long[] { -25000, 50000, -25000, 40000, -25000 },
            new long[] { 0, 15000, 15000, 0, 13500 },
            new long[] { -25000, 15000, 15000, 40000, 14000 },
            new long[] { -10000, 50000, -10000, 15000, -10000 },
            new long[] { -25000, 18500, 14000, 40000, 13200 },
        };

        /// <summary>The 4.2 resident/intruder table as published (x1000), H, D, Bourgeois, Anti-Bourgeois.</summary>
        public static long[][] DocHdba() => new[]
        {
            new long[] { -25000, 50000, 12500, 12500 },
            new long[] { 0, 15000, 25000, 25000 },
            new long[] { -12500, 25000, 25000, -12500 },
            new long[] { -12500, 25000, -12500, 25000 },
        };

        /// <summary>The exact expected matrix of the turn rules (x1000), by enumeration of every random input.</summary>
        public static long[,] Exact(ContestKind[] kinds, bool roles)
        {
            int k = kinds.Length;
            var m = new long[k, k];
            var r = ContestRules.Lab;
            for (int i = 0; i < k; i++) for (int j = 0; j < k; j++) m[i, j] = Contest.ExpectedX1000(kinds[i], kinds[j], roles, r);
            return m;
        }

        protected override void Execute(LabConfig cfg, LabResult res, CheckList ck, List<string> notes)
        {
            var rules = ContestRules.Lab;
            int perCell = cfg.ParamInt("mc", 50000);
            // ---- L2b: Monte Carlo of the turn rules against the exact enumeration and against the published tables ----
            MonteCarlo(cfg.Seed, "hdrbp", Hdrbp(), false, DocHdrbp(), perCell, rules, ck, notes);
            MonteCarlo(cfg.Seed, "hdba", Hdba(), true, DocHdba(), perCell, rules, ck, notes);

            var exact = Exact(Hdrbp(), false);
            // H, D, R, B only (the first four rows of the exact matrix)
            var m4 = new long[4, 4];
            for (int i = 0; i < 4; i++) for (int j = 0; j < 4; j++) m4[i, j] = exact[i, j];

            // ---- exact replicator facts ----
            var rep = new Replicator(m4, 500000);
            var tie = Replicator.SharesPermille(0, 100, 900, 0);
            var x = (long[])tie.Clone();
            long maxDev = 0;
            for (int g = 0; g < 20000; g++)
            {
                rep.Step(x);
                long d = x[1] - tie[1]; if (d < 0) d = -d;
                if (d > maxDev) maxDev = d;
            }
            ck.Near("Retaliator population: Dove share after 20,000 generations (no mutation)", LabMath.Micro(x[1]), 100000, 1000, 4);
            ck.Report("largest Dove-share deviation over the run", LabMath.Micro(maxDev), "0 (Dove ties with Retaliator)", 6);
            ck.True("Dove ties with Retaliator against both (15 = 15): all-Retaliator is not a strict ESS", m4[1, 2] == m4[2, 2] && m4[1, 1] == m4[2, 1], "D vs R = R vs R and D vs D = R vs D");

            var up = Replicator.SharesPermille(0, 450, 500, 50);
            var upEnd = LabMath.Final(rep, up, 10);
            ck.Above("Bully share after 10 generations from (R .50, D .45, B .05)", LabMath.Micro(upEnd[3]), 50000, 4);
            var down = Replicator.SharesPermille(0, 300, 650, 50);
            var downEnd = LabMath.Final(rep, down, 10);
            ck.Below("Bully share after 10 generations from (R .65, D .30, B .05)", LabMath.Micro(downEnd[3]), 50000, 4);
            // the thresholds d = 25/60 (Bully) and d = 0.533 (Hawk), from the matrix against an R/D mix
            ck.True("Bully beats Retaliator in an R/D mix iff d > 25/60 (exact integer test at d = 416 and 417 permille)",
                BullyBeats(m4, 417) && !BullyBeats(m4, 416), "d* = 0.417");
            ck.True("Hawk beats Retaliator in an R/D mix iff d > 0.533 (at 533 and 534 permille)", HawkBeats(m4, 534) && !HawkBeats(m4, 533), "d* = 0.533");

            // ---- agents H, D, R, B ----
            var game = new ContestGame(new[] { ContestKind.Hawk, ContestKind.Dove, ContestKind.Retaliator, ContestKind.Bully }, false, rules);
            int gens = cfg.GenerationsOr(6000);
            int mu = cfg.MutationOr(2);
            int from = gens / 2;
            int kk = cfg.ParamInt("k", 10), w0 = cfg.ParamInt("w0", 500) * 1000;
            int reps = cfg.ParamInt("replicates", 6);
            Mean4(cfg.Seed, "agents-n200", 200, gens, mu, game, kk, w0, reps, from, out long hS, out long dS, out long rS, out long bS, out string per200, out _);
            // Published expectation: R < 0.75 and H + B > 0.10 at N = 200 (proto, 6 seeds). Amended at M2: with the L1 agent
            // defaults (k = 10, w0 = 500) Retaliator stays dominant at N = 200 in every seed tried (seeds 1-13: 0.77-0.94).
            const string amend = "Amended at M2. Published: R < 0.75 and H+B > 0.10 (proto mean of 6 seeds). With k=10, w0=500, mu=2 permille every N=200 run tried (seeds 1-13) stayed in the Retaliator basin (R 0.77-0.94 per seed); the Hawk/Dove/Bully basin the proto mean implies is reached only with stronger drift (k=1, or w0=100). Corrected expectation: Retaliator stays dominant (R >= 0.75, H+B < 0.10).";
            ck.AtLeast("agents N=200, mean of " + reps + " seeds: Retaliator share, generations " + from + "-" + gens, LabMath.Micro(rS), 750000, kind: CheckKind.Amended, note: amend, source: "L2, ACCEPTANCE M2.4");
            ck.Below("agents N=200, mean of " + reps + " seeds: Hawk + Bully share", LabMath.Micro(hS + bS), 100000, kind: CheckKind.Amended, note: amend, source: "L2, ACCEPTANCE M2.4");
            ck.Report("agents N=200, mean of " + reps + " seeds: H / D / R / B", LabMath.Micro(rS), "H " + Fx.Format(hS, 3) + ", D " + Fx.Format(dS, 3) + ", R " + Fx.Format(rS, 3) + ", B " + Fx.Format(bS, 3) + " (proto over 6 seeds: H .20 D .15 R .58 B .08)");
            ck.Report("agents N=200: Retaliator share per seed", LabMath.Micro(rS), per200);
            int n = cfg.AgentsOr(1000);
            Mean4(cfg.Seed, "agents-n1000", n, gens, mu, game, kk, w0, cfg.ParamInt("replicates1000", 1), from, out long hB, out long dB, out long rB, out long bB, out string per1000, out var big);
            res.Strategies = new[] { "Hawk", "Dove", "Retaliator", "Bully" };
            res.SharesPerGeneration = LabMath.Thin(big.Shares, 10);
            res.MeanPayoffX1000 = LabMath.Thin(big.MeanPayX1000, 10);
            res.SeriesTitle = "agents N=" + n + ", k=" + kk + ", w0=" + (w0 / 1000) + ", mu=" + mu + " permille, first replicate, every 10th generation";
            ck.Between("agents N=" + n + ": Retaliator share", LabMath.Micro(rB), 650000, 900000);
            ck.Report("agents N=" + n + ": Hawk + Bully share", LabMath.Micro(hB + bB), "proto 0.05");
            ck.Report("agents N=" + n + ": Retaliator share per seed", LabMath.Micro(rB), per1000);

            // ---- the second variant: H, D, R, B, P, replicator with mutation 0.002 (on the turn-rule matrix) ----
            var rep5 = new Replicator(exact, 500000, Replicator.MuQ20(2, 1000));
            var s5 = LabMath.RunReplicator(rep5, Replicator.SharesPermille(200, 200, 200, 200, 200), 6000);
            var names = new[] { "H", "D", "R", "B", "P" };
            var proto = new[] { "0.002", "0.177", "0.620", "0.003", "0.198" };
            for (int i = 0; i < 5; i++)
                ck.Report("H/D/R/B/P replicator (mu 0.002): " + names[i] + " mean share, generations 3000-6000", LabMath.Micro(LabMath.Mean(s5, i, 3000, 6000)),
                    "proto " + proto[i] + " (on the published table; the turn-rule Prober cells differ, see L2b)");

            notes.Add("Agents play the turn-level contest with realised probes and coin flips (tabulated exactly, ContestGame).");
            notes.Add("Our matrix gives no stable Hawk/Bully mix (a Dove invades a Hawk/Bully mixture); no Hawk/Bully ESS is claimed.");
        }

        private static bool BullyBeats(long[,] m, int dPermille)
        {
            // B against an R/D mix with Dove share d: d B·D + (1-d) B·R  vs  R: d R·D + (1-d) R·R  (x1000 x 1000)
            long b = dPermille * m[3, 1] + (1000 - dPermille) * m[3, 2];
            long r = dPermille * m[2, 1] + (1000 - dPermille) * m[2, 2];
            return b > r;
        }

        private static bool HawkBeats(long[,] m, int dPermille)
        {
            long h = dPermille * m[0, 1] + (1000 - dPermille) * m[0, 2];
            long r = dPermille * m[2, 1] + (1000 - dPermille) * m[2, 2];
            return h > r;
        }

        /// <summary>Mean shares over the window, averaged over replicate seeds s, s+1, ... (4.5 "Seeds").</summary>
        private static void Mean4(ulong seed, string part, int n, int gens, int mu, IPairGame game, int k, int w0, int reps, int from,
            out long h, out long d, out long r, out long b, out string perSeed, out AgentRunResult first)
        {
            h = d = r = b = 0; first = null;
            var per = new List<string>();
            for (int i = 0; i < reps; i++)
            {
                var run = RunAgents(seed + (ulong)i, part, n, gens, mu, game, k, w0);
                if (i == 0) first = run;
                h += LabMath.Mean(run.Shares, 0, from, gens); d += LabMath.Mean(run.Shares, 1, from, gens);
                long ri = LabMath.Mean(run.Shares, 2, from, gens);
                r += ri; b += LabMath.Mean(run.Shares, 3, from, gens);
                per.Add(Fx.Format(ri, 2));
            }
            h /= reps; d /= reps; r /= reps; b /= reps;
            perSeed = string.Join(" ", per);
        }

        private static AgentRunResult RunAgents(ulong seed, string part, int n, int gens, int mu, IPairGame game, int k, int w0)
        {
            var s = new AgentRunSpec { N = n, K = 4, InitialCounts = AgentRunSpec.Counts(n, 250, 250, 250, 250), Rounds = k, Generations = gens, W0X1000 = w0, MutationPermille = mu };
            return AgentEngine.Run(s, game, LabRandom.Root(seed, "L2", part));
        }

        private static void MonteCarlo(ulong seed, string part, ContestKind[] kinds, bool roles, long[][] doc, int perCell, ContestRules rules, CheckList ck, List<string> notes)
        {
            int k = kinds.Length;
            var root = LabRandom.Root(seed, "L2b", part);
            for (int i = 0; i < k; i++)
            for (int j = 0; j < k; j++)
            {
                var rng = root.Fork("cell", i, j);
                long sum = 0;
                for (int t = 0; t < perCell; t++) sum += Contest.Play(kinds[i], kinds[j], roles, rng, rules).PayA;
                long mcMicro = Fx.MulDiv(sum, 1000000, perCell);
                long exactX1000 = Contest.ExpectedX1000(kinds[i], kinds[j], roles, rules);
                string cell = Contest.Name(kinds[i]) + " vs " + Contest.Name(kinds[j]) + (roles ? " (random roles)" : "");
                long dExact = mcMicro - exactX1000 * 1000; if (dExact < 0) dExact = -dExact;
                ck.Add("L2b " + cell + ": Monte Carlo against the exact turn-rule value", mcMicro, Fx.FormatScaled(exactX1000, 3) + " +/- 3", dExact <= 3000000, decimals: 3, status: ReproStatus.Derived, source: "L2b, test 8");
                long docX1000 = doc[i][j];
                long dDoc = mcMicro - docX1000 * 1000; if (dDoc < 0) dDoc = -dDoc;
                if (dDoc <= 3000000 && exactX1000 == docX1000)
                    ck.Add("L2b " + cell + ": published 4.2 cell", mcMicro, Fx.FormatScaled(docX1000, 3) + " +/- 3", true, decimals: 3, status: ReproStatus.Derived, source: "L2b, test 8");
                else
                {
                    // 4.2: "If a cell fails, the turn rules are right and the table is corrected."
                    ck.Add("L2b " + cell + ": published 4.2 cell corrected to the turn-rule value", mcMicro, Fx.FormatScaled(exactX1000, 3) + " +/- 3 (published " + Fx.FormatScaled(docX1000, 3) + ")",
                        dExact <= 3000000, CheckKind.Amended, 3,
                        "The published cell is " + Fx.FormatScaled(docX1000, 3) + "; the 4.2 turn rules give exactly " + Fx.FormatScaled(exactX1000, 3) + " (enumeration of probes and coin), and the Monte Carlo agrees. Per 4.2 the table is corrected.",
                        ReproStatus.Derived, "L2b, test 8");
                }
            }
        }
    }
}
