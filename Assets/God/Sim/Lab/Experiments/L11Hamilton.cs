using System.Collections.Generic;

namespace God.Sim.Lab
{
    /// <summary>L11. Hamilton's rule and the mushroom call (PDF 112 to 122). STRATEGY_BENCHMARKS 4.6 L11; ACCEPTANCE M2.12.</summary>
    public sealed class L11Hamilton : LabExperiment
    {
        public override string Id => "L11";
        public override string Title => "Hamilton's rule and the mushroom call";
        public override string Source => "PDF 112-122 (worked example PDF 120)";
        public override ReproStatus Status => ReproStatus.Invented;
        public override string StatusText => "Exact (decision example) and invented (spread test): the decision test uses the book's worked example as recorded in research notes C1 (8 mushrooms worth +6, brother r 1/2, cousin r 1/8: keep 18, call 12 + 6 + 1.5 = 19.5), confirmed against the notes and consistent in its own arithmetic; the spread test uses invented b and c and checks Hamilton's rule itself";
        public override string ExpectedSummary => "the book's example calls (19.5 > 18); without the cousin a tie resolves to Keep; b = 2.6 helper > 0.95, b = 1.4 < 0.05, threshold 2 +/- 15%; with r' = 0.55 r the threshold is c/r'";

        /// <summary>Call iff own_if_call + sum(r_i b_i) &gt; own_if_keep. Values x1000, r as a fraction num/den. A tie keeps quiet.</summary>
        public static bool Call(long ownIfCallX1000, long ownIfKeepX1000, (long num, long den, long benefitX1000)[] relatives, out long totalX1000)
        {
            long t = ownIfCallX1000 * 1000000;
            foreach (var r in relatives) t += r.benefitX1000 * r.num * 1000000 / r.den;
            totalX1000 = t / 1000000;
            return t > ownIfKeepX1000 * 1000000;
        }

        protected override void Execute(LabConfig cfg, LabResult res, CheckList ck, List<string> notes)
        {
            // ---- the book's worked example (PDF 120, notes C1) ----
            // 8 mushrooms worth +6 each; calling shares them among four (12 each); keeping quiet scores 18 (the notes' figure).
            long own = 8 * 6 * 1000 / 4;       // 12
            long keep = 18000;
            var family = new[] { (1L, 2L, own), (1L, 8L, own), (0L, 1L, own) }; // brother, cousin, stranger
            bool call = Call(own, keep, family, out long total);
            ck.Add("mushroom call (notes C1): 12 + 1/2 x 12 + 1/8 x 12 + 0 = 19.5 against keep 18: Call", LabMath.MicroX1000(total), "19.5, Call", call && total == 19500, decimals: 1, status: ReproStatus.Exact);
            var noCousin = new[] { (1L, 2L, own), (0L, 1L, own), (0L, 1L, own) }; // the cousin replaced by a stranger
            bool call2 = Call(own, keep, noCousin, out long total2);
            ck.Add("without the cousin: 18.0 against 18, a tie resolves to Keep", LabMath.MicroX1000(total2), "18.0, Keep", !call2 && total2 == 18000, decimals: 1, status: ReproStatus.Exact);
            bool strangerAdds = Call(0, 0, new[] { (0L, 1L, 1000000L) }, out long t3);
            ck.True("a stranger (r = 0) never adds anything", !strangerAdds && t3 == 0, "0", status: ReproStatus.Exact);
            // a brother needs a benefit above cost / r = 12 at a cost of 6
            bool at12 = 12000 * 1 / 2 > 6000, at13 = 13000 * 1 / 2 > 6000;
            ck.True("a brother at cost 6: benefit 12 is a tie (no help), 13 helps (threshold cost / r = 12)", !at12 && at13, "b* = 12", status: ReproStatus.Derived);

            // ---- spread test (invented b and c) ----
            int n = cfg.AgentsOr(1000), gens = cfg.GenerationsOr(300);
            var hiRun = Spread(cfg.Seed, "b2.6", n, gens, 2600, 500, out var series);
            res.Strategies = new[] { "helper" };
            res.SharesPerGeneration = series;
            res.SeriesTitle = "helper allele share, r = 0.5, b = 2.6, c = 1, N=" + n;
            ck.Above("spread: b = 2.6 (r b / c = 1.3), helper share after " + gens + " generations", LabMath.Micro(hiRun), 950000, status: ReproStatus.Invented);
            var loRun = Spread(cfg.Seed, "b1.4", n, gens, 1400, 500, out _);
            ck.Below("spread: b = 1.4 (0.7), helper share", LabMath.Micro(loRun), 50000, status: ReproStatus.Invented);
            // threshold: final share against b on a grid, mean of 3 seeds, linear interpolation at 0.5
            long prevB = 0, prevS = 0, thr = -1;
            for (int b = 1500; b <= 2500; b += 100)
            {
                long sh = 0;
                for (int s = 0; s < 3; s++) sh += Spread(cfg.Seed + (ulong)s, "grid" + b, n, gens, b, 500, out _);
                sh /= 3;
                if (thr < 0 && b > 1500 && prevS < Fx.Half && sh >= Fx.Half)
                    thr = prevB + Fx.MulDiv(100, Fx.Half - prevS, sh - prevS);
                prevB = b; prevS = sh;
            }
            ck.Near("spread: threshold b where the helper share crosses 0.5 (r = 0.5, c = 1; Hamilton b* = c/r = 2)", thr * 1000, 2000000, 300000, 2, status: ReproStatus.Invented);
            // estimated relatedness: helping iff r' b > c with r' = 0.55 r = 0.275
            long rEst = 275; // permille
            int first = -1;
            for (int b = 0; b <= 10000; b++) if (rEst * b > 1000L * 1000) { first = b; break; } // b in thousandths, c = 1
            ck.Near("estimated r' = 0.55 r = 0.275: the decision threshold in b is c / r'", first * 1000L, Fx.MulDiv(1000000, 1000, rEst), 1000, 3, status: ReproStatus.Derived);
            notes.Add("L11 label: the worked example matches research notes C1 exactly (12 + 6 + 1.5 = 19.5 against 18; it follows from 8 x 6 shared four ways, and the notes' keep score of 18). It was checked against the notes, not against a copy of the book (none is in the repository).");
            notes.Add("Spread model: every helper pays c and gives b to one recipient: with probability r an agent carrying the helper allele, otherwise any agent. The edge is r b - c whatever the frequency. Fitness 10 + payoff (c = 1), Wright-Fisher, start 0.5.");
        }

        /// <summary>Helper allele share after the run. b and r in thousandths, c = 1 (1000).</summary>
        private static long Spread(ulong seed, string part, int n, int gens, int bX1000, int rPermille, out long[][] series)
        {
            var root = LabRandom.Root(seed, "L11", part);
            var helper = new bool[n];
            for (int i = 0; i < n; i++) helper[i] = i < n / 2;
            var pay = new long[n]; var fit = new long[n]; var cum = new long[n]; var par = new int[n]; var next = new bool[n];
            var helpers = new int[n];
            var s = new long[gens + 1][];
            for (int g = 0; g <= gens; g++)
            {
                int h = 0;
                for (int i = 0; i < n; i++) if (helper[i]) helpers[h++] = i;
                s[g] = new[] { Fx.FromRatio(h, n) };
                if (g == gens || h == 0 || h == n) { for (int g2 = g + 1; g2 <= gens; g2++) s[g2] = s[g]; break; }
                System.Array.Clear(pay, 0, n);
                var gr = root.Fork("help", g);
                for (int i = 0; i < n; i++)
                {
                    if (!helper[i]) continue;
                    pay[i] -= 1000;
                    int to;
                    if (LabRandom.Permille(gr, rPermille) && h > 1) { do { to = helpers[gr.NextInt(h)]; } while (to == i); }
                    else { do { to = gr.NextInt(n); } while (to == i); }
                    pay[to] += bX1000;
                }
                for (int i = 0; i < n; i++) { long f = 10000 + pay[i]; fit[i] = f < 1 ? 1 : f; }
                Selection.WrightFisher(fit, n, n, root.Fork("sel", g), par, cum);
                for (int i = 0; i < n; i++) next[i] = helper[par[i]];
                System.Array.Copy(next, helper, n);
            }
            series = s;
            return s[gens][0];
        }
    }
}
