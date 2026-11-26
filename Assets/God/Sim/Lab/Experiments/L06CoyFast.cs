using System.Collections.Generic;

namespace God.Sim.Lab
{
    /// <summary>L6. Coy/fast, faithful/philanderer (PDF 174 to 176, endnote 326). STRATEGY_BENCHMARKS 4.4, 4.6 L6, test 17; ACCEPTANCE M2.8, M2.15.</summary>
    public sealed class L06CoyFast : LabExperiment
    {
        public override string Id => "L6";
        public override string Title => "Coy/fast, faithful/philanderer";
        public override string Source => "PDF 174-176, endnote 326";
        public override ReproStatus Status => ReproStatus.ExactPayoffsCorrectedOrDifferent;
        public override string StatusText => "Exact payoffs, corrected result: the payoffs are the book's (PDF 174); the book's claim that the mix is stable is wrong (its own endnote), so the corrected statement is tested: the system cycles around (5/6, 5/8)";
        public override string ExpectedSummary => "RK4 time average (0.833 +/- 0.01, 0.625 +/- 0.02); SD c > 0.1, SD f > 0.2 (second half); distance to centre > 0.1 (last quarter); H drifts < 0.1%; Euler fails it; agents c 0.83 +/- 0.03, f 0.60 +/- 0.05, SD c > 0.05";

        /// <summary>Partnership payoffs from the book's numbers (PDF 174): child worth 15 to each parent, raising costs 20
        /// (shared when both stay), courtship costs 3 to each partner. [female, male] → (female, male).</summary>
        public static void Payoffs(out long[,] female, out long[,] male)
        {
            const int child = 15, raise = 20, court = 3;
            female = new long[2, 2]; male = new long[2, 2];
            // coy (0) with faithful (0): courtship, both stay
            female[0, 0] = child - raise / 2 - court; male[0, 0] = child - raise / 2 - court;
            // coy with philanderer (1): he will not court, no mating
            female[0, 1] = 0; male[0, 1] = 0;
            // fast (1) with faithful: no courtship, both stay
            female[1, 0] = child - raise / 2; male[1, 0] = child - raise / 2;
            // fast with philanderer: he leaves, she raises alone
            female[1, 1] = child - raise; male[1, 1] = child;
        }

        protected override void Execute(LabConfig cfg, LabResult res, CheckList ck, List<string> notes)
        {
            Payoffs(out var fp, out var mp);
            ck.True("payoff table (PDF 174): coy/faithful 2/2, fast/faithful 5/5, fast/philanderer -5/15, coy/philanderer 0/0",
                fp[0, 0] == 2 && mp[0, 0] == 2 && fp[1, 0] == 5 && mp[1, 0] == 5 && fp[1, 1] == -5 && mp[1, 1] == 15 && fp[0, 1] == 0 && mp[0, 1] == 0, "the book's table", status: ReproStatus.Exact);
            var sys = new TwoPopReplicator(fp, mp, 100);
            ck.True("derived dynamics: dc/dt = c(1-c)(5 - 8f), df/dt = f(1-f)(12c - 10)", sys.C0 == 5 && sys.C1 == -8 && sys.D0 == -10 && sys.D1 == 12, "5, -8, -10, 12", status: ReproStatus.Derived);

            int steps = cfg.ParamInt("time", 4000) * 100;
            var rk = Integrate(sys, steps, true, out long avgC, out long avgF, out long sdC, out long sdF, out long dist, out long drift, out var series);
            res.Strategies = new[] { "coy", "faithful" };
            res.SharesPerGeneration = series;
            res.SeriesTitle = "two-population replicator, RK4 step 0.01 from (0.5, 0.3), sampled every time unit";
            ck.Near("RK4: time average of c (coy share)", LabMath.Micro(avgC), 833333, 10000);
            ck.Near("RK4: time average of f (faithful share)", LabMath.Micro(avgF), 625000, 20000);
            ck.Above("RK4: SD of c over the second half (keeps cycling)", LabMath.Micro(sdC), 100000, note: "proto 0.198");
            ck.Above("RK4: SD of f over the second half", LabMath.Micro(sdF), 200000, note: "proto 0.301");
            ck.Above("RK4: mean distance to (5/6, 5/8) over the last quarter", LabMath.Micro(dist), 100000);
            ck.Below("RK4: drift of the conserved H (relative)", drift, 1000, 6, note: "H = -10 ln c - 2 ln(1-c) - 5 ln f - 3 ln(1-f)");
            Integrate(sys, steps, false, out _, out _, out _, out _, out _, out long eulerDrift, out _);
            ck.Add("negative control: an Euler step fails the H check", eulerDrift, ">= 0.001 (fails)", eulerDrift >= 1000, decimals: 6, source: "L6, test 17");

            // Agents: N per sex, soft selection exp(0.02 × payoff), mutation 1 permille.
            int n = cfg.AgentsOr(1000), gens = cfg.GenerationsOr(20000);
            Agents(cfg.Seed, n, gens, fp, mp, out long aC, out long aF, out long aSdC);
            int from = gens / 4;
            // The deterministic (infinite-N) version of the same agent model: soft selection exp(0.02 payoff) on random
            // partners, mutation 1 permille. Its mutation-selection point is what the agent time averages should approach.
            DeterministicMap(fp, mp, out long detC, out long detF);
            ck.Report("deterministic soft-selection map: mutation-selection point (c, f)", LabMath.Micro(detC), "c " + Fx.Format(detC, 3) + ", f " + Fx.Format(detF, 3) + " (derived in the lab)", status: ReproStatus.Derived);
            const string amend = "Amended at M2. Published: c 0.83 +/- 0.03, f 0.60 +/- 0.05 (proto). The spec's own agent model (random partner, soft selection exp(0.02 payoff), mutation 1 permille) has its deterministic mutation-selection point at about (0.859, 0.559): selection on females is weak, so mutation pulls f well below 5/8. Agents (seeds 1-5) gave c 0.851-0.859, f 0.540-0.553. Corrected expectation: the published tolerances around the derived point.";
            ck.Near("agents N=" + n + " per sex: mean c, generations " + from + "-" + gens + ", against the derived point", LabMath.Micro(aC), LabMath.Micro(detC), 30000, kind: CheckKind.Amended, note: amend);
            ck.Near("agents: mean f, against the derived point", LabMath.Micro(aF), LabMath.Micro(detF), 50000, kind: CheckKind.Amended, note: amend);
            ck.Report("agents: mean c and f against the published proto values (0.83, 0.60)", LabMath.Micro(aF), "c " + Fx.Format(aC, 3) + ", f " + Fx.Format(aF, 3));
            ck.Above("agents: SD of c (the cycle does not vanish)", LabMath.Micro(aSdC), 50000, note: "proto 0.16");
            notes.Add("The book's equilibrium (5/6, 5/8) is the mean of a cycle, not a place the system sits; the RK4 run keeps cycling and an Euler step spirals out.");
            notes.Add("Agent soft selection is risk-sensitive (exp of a realised payoff), which moves the agent mean of f below 5/8 (about 0.60, as the proto).");
            notes.Add("The N = 10,000 agent run (proto SD 0.06-0.07) is not part of lab all (cost); run it with --agents 10000.");
        }

        private static long[][] Integrate(TwoPopReplicator sys, int steps, bool rk4, out long avgC, out long avgF, out long sdC, out long sdF, out long dist, out long driftMicro, out long[][] series)
        {
            long c = Fx.FromPermille(500), f = Fx.FromPermille(300);
            long h0 = H(c, f);
            long maxDrift = 0;
            long sumC = 0, sumF = 0;
            long sumC2 = 0, sumF2 = 0, sumC2h = 0, sumF2h = 0; long nh = 0;
            long sumDist = 0; long nq = 0;
            long cs = Fx.FromRatio(5, 6), fs = Fx.FromRatio(5, 8);
            var s = new List<long[]>();
            bool blown = false;
            for (int i = 0; i < steps; i++)
            {
                if (i % 100 == 0) s.Add(new[] { c, f });
                if (rk4) sys.StepRk4(ref c, ref f); else sys.StepEuler(ref c, ref f);
                if (c <= 0 || c >= Fx.One || f <= 0 || f >= Fx.One) { blown = true; break; }
                sumC += c; sumF += f;
                if (i >= steps / 2) { sumC2h += c; sumF2h += f; sumC2 += Fx.Mul(c, c); sumF2 += Fx.Mul(f, f); nh++; }
                if (i >= steps * 3 / 4)
                {
                    long dc = c - cs, df = f - fs;
                    sumDist += Fx.Sqrt(Fx.Mul(dc, dc) + Fx.Mul(df, df)); nq++;
                }
                if (i % 100 == 99)
                {
                    long d = H(c, f) - h0; if (d < 0) d = -d;
                    if (d > maxDrift) maxDrift = d;
                }
            }
            series = s.ToArray();
            if (blown)
            {
                avgC = avgF = sdC = sdF = dist = 0;
                driftMicro = 1000000000; // left the simplex: the check fails by any measure
                return series;
            }
            avgC = sumC / steps; avgF = sumF / steps;
            long mc = sumC2h / nh, mf = sumF2h / nh;
            sdC = Fx.Sqrt(System.Math.Max(0, sumC2 / nh - Fx.Mul(mc, mc)));
            sdF = Fx.Sqrt(System.Math.Max(0, sumF2 / nh - Fx.Mul(mf, mf)));
            dist = sumDist / nq;
            driftMicro = Fx.MulDiv(maxDrift, 1000000, h0 < 0 ? -h0 : h0);
            return series;
        }

        /// <summary>The conserved quantity H = -10 ln c - 2 ln(1-c) - 5 ln f - 3 ln(1-f) (Q30).</summary>
        public static long H(long c, long f) => -10 * Fx.Ln(c) - 2 * Fx.Ln(Fx.One - c) - 5 * Fx.Ln(f) - 3 * Fx.Ln(Fx.One - f);

        /// <summary>Iterates c' = mu-mix(c Wc / mean W), f' likewise, in Q30, to its fixed point (20,000 steps).</summary>
        private static void DeterministicMap(long[,] fp, long[,] mp, out long c, out long f)
        {
            var wf = new long[2, 2]; var wm = new long[2, 2];
            for (int a = 0; a < 2; a++) for (int b = 0; b < 2; b++)
            {
                wf[a, b] = Fx.Exp(Fx.MulDiv(fp[a, b], Fx.One * 2, 100));
                wm[a, b] = Fx.Exp(Fx.MulDiv(mp[a, b], Fx.One * 2, 100));
            }
            c = Fx.Half; f = Fx.FromPermille(300);
            long mu = Fx.FromPermille(1);
            for (int i = 0; i < 20000; i++)
            {
                long wCoy = Fx.Mul(f, wf[0, 0]) + Fx.Mul(Fx.One - f, wf[0, 1]);
                long wFast = Fx.Mul(f, wf[1, 0]) + Fx.Mul(Fx.One - f, wf[1, 1]);
                long wFa = Fx.Mul(c, wm[0, 0]) + Fx.Mul(Fx.One - c, wm[1, 0]);
                long wPh = Fx.Mul(c, wm[0, 1]) + Fx.Mul(Fx.One - c, wm[1, 1]);
                long c2 = Fx.Div(Fx.Mul(c, wCoy), Fx.Mul(c, wCoy) + Fx.Mul(Fx.One - c, wFast));
                long f2 = Fx.Div(Fx.Mul(f, wFa), Fx.Mul(f, wFa) + Fx.Mul(Fx.One - f, wPh));
                c = c2 + Fx.Mul(mu, Fx.One - 2 * c2);
                f = f2 + Fx.Mul(mu, Fx.One - 2 * f2);
            }
        }

        private static void Agents(ulong seed, int n, int gens, long[,] fp, long[,] mp, out long meanC, out long meanF, out long sdC)
        {
            var root = LabRandom.Root(seed, "L6", "agents");
            var fem = new byte[n]; var mal = new byte[n];
            for (int i = 0; i < n; i++) { fem[i] = (byte)(i < n / 2 ? 0 : 1); mal[i] = (byte)(i < n * 3 / 10 ? 0 : 1); }
            // weights exp(0.02 × payoff) for each payoff value that occurs, relative to the largest (15)
            var wf = new long[2, 2]; var wm = new long[2, 2];
            for (int a = 0; a < 2; a++) for (int b = 0; b < 2; b++)
            {
                wf[a, b] = Fx.Exp(Fx.MulDiv(fp[a, b] - 15, Fx.One * 2, 100));
                wm[a, b] = Fx.Exp(Fx.MulDiv(mp[a, b] - 15, Fx.One * 2, 100));
            }
            var perm = new int[n]; var w1 = new long[n]; var w2 = new long[n]; var cum = new long[n]; var par = new int[n];
            var nf = new byte[n]; var nm = new byte[n];
            int from = gens / 4;
            long sumC = 0, sumF = 0, sumC2 = 0; long cnt = 0;
            for (int gen = 0; gen < gens; gen++)
            {
                int coy = 0, faith = 0;
                for (int i = 0; i < n; i++) { if (fem[i] == 0) coy++; if (mal[i] == 0) faith++; }
                if (gen >= from)
                {
                    long c = Fx.FromRatio(coy, n), f = Fx.FromRatio(faith, n);
                    sumC += c; sumF += f; sumC2 += Fx.Mul(c, c); cnt++;
                }
                LabRandom.Permutation(root.Fork("pair", gen, 0), perm, n); // female i meets male perm[i]
                for (int i = 0; i < n; i++)
                {
                    int m = perm[i];
                    w1[i] = wf[fem[i], mal[m]];
                    w2[m] = wm[fem[i], mal[m]];
                }
                Selection.WrightFisher(w1, n, n, root.Fork("sel", gen), par, cum);
                for (int i = 0; i < n; i++) nf[i] = fem[par[i]];
                Selection.WrightFisher(w2, n, n, root.Fork("selm", gen), par, cum);
                for (int i = 0; i < n; i++) nm[i] = mal[par[i]];
                var mr = root.Fork("mut", gen);
                for (int i = 0; i < n; i++)
                {
                    if (LabRandom.Permille(mr, 1)) nf[i] ^= 1;
                    if (LabRandom.Permille(mr, 1)) nm[i] ^= 1;
                }
                System.Array.Copy(nf, fem, n); System.Array.Copy(nm, mal, n);
            }
            meanC = sumC / cnt; meanF = sumF / cnt;
            sdC = Fx.Sqrt(System.Math.Max(0, sumC2 / cnt - Fx.Mul(meanC, meanC)));
        }
    }
}
