using System.Collections.Generic;

namespace God.Sim.Lab
{
    /// <summary>L10. Digger wasps, dig or enter (endnote PDF 307, 308). STRATEGY_BENCHMARKS 4.2, 4.6 L10; ACCEPTANCE M2.12.</summary>
    public sealed class L10DiggerWasps : LabExperiment
    {
        public override string Id => "L10";
        public override string Title => "Digger wasps: dig or enter";
        public override string Source => "endnote PDF 307-308";
        public override ReproStatus Status => ReproStatus.Invented;
        public override string StatusText => "Invented payoffs: the book reports the wasp study's conclusion without the benefits and costs; B, Cd, Ce and Cw are ours, chosen so the mixed equilibrium is interior; the test checks our model's own equilibrium, not the study's share";
        public override string ExpectedSummary => "mean x over the last 500 generations 0.28 +/- 0.03 from x = 0.9 and x = 0.05; |payoff(Dig) - payoff(Enter)| < 3; pure types give the same mean +/- 0.03";

        private const int B = 100, Cd = 40, Ce = 5, Cw = 30;

        /// <summary>Expected payoff x1000 of a wasp entering with probability x (Q30) when the population enters at X (Q30).</summary>
        public static long Payoff(long x, long popX)
        {
            long dig = (B - Cd) * 1000L;
            long enter = Fx.Mul(Fx.One - popX, (B - Ce) * 1000L) - Fx.Mul(popX, Cw * 1000L);
            return Fx.Mul(Fx.One - x, dig) + Fx.Mul(x, enter);
        }

        protected override void Execute(LabConfig cfg, LabResult res, CheckList ck, List<string> notes)
        {
            int n = cfg.AgentsOr(500), gens = cfg.GenerationsOr(2000);
            ck.Near("analytic x* = (Cd - Ce)/(B - Ce + Cw) = 35/125", LabMath.MicroRatio(Cd - Ce, B - Ce + Cw), 280000, 0, 3, status: ReproStatus.Derived);
            var hi = Mixed(cfg.Seed, "from-0.9", n, gens, Fx.FromPermille(900), out var series);
            var lo = Mixed(cfg.Seed, "from-0.05", n, gens, Fx.FromPermille(50), out _);
            res.Strategies = new[] { "mean_x" };
            res.SharesPerGeneration = series;
            res.SeriesTitle = "mean entering probability x, heritable per wasp, start 0.9, N=" + n;
            ck.Near("mixed: mean x over the last 500 generations, start 0.9", LabMath.Micro(hi), 280000, 30000, note: "proto 0.278-0.282");
            ck.Near("mixed: mean x over the last 500 generations, start 0.05", LabMath.Micro(lo), 280000, 30000);
            long dig = (B - Cd) * 1000L;
            long enter = Payoff(Fx.One, hi);
            long diff = dig - enter; if (diff < 0) diff = -diff;
            ck.Below("|payoff(Dig) - payoff(Enter)| at the measured mean x", LabMath.MicroX1000(diff), 3000000);
            var pure = Pure(cfg.Seed, n, gens);
            ck.Near("pure types: mean share entering over the last 500 generations", LabMath.Micro(pure), LabMath.Micro(hi), 30000);
            notes.Add("A wasp's payoff is its expected payoff given its own x and the population's mean x (one decision per housing need, averaged). With single realised draws, soft selection exp(0.02 payoff) is risk-sensitive and its equilibrium moves to about 0.55 (e^1.2 = (1-x) e^1.9 + x e^-0.6); the spec's x* = 0.28 is for expected payoffs.");
            notes.Add("Soft selection exp(0.02 payoff); mutation adds Gaussian-like noise of sd 0.01 to every offspring's x, clamped to [0, 1]. Pure types: an offspring switches type with probability 10 permille.");
        }

        private static long Mixed(ulong seed, string part, int n, int gens, long x0, out long[][] series)
        {
            var root = LabRandom.Root(seed, "L10", part);
            var x = new long[n]; var w = new long[n]; var cum = new long[n]; var par = new int[n]; var next = new long[n]; var pay = new long[n];
            for (int i = 0; i < n; i++) x[i] = x0;
            var s = new long[gens + 1][];
            long sum = 0; int cnt = 0;
            for (int g = 0; g <= gens; g++)
            {
                long mean = 0;
                for (int i = 0; i < n; i++) mean += x[i];
                mean /= n;
                s[g] = new[] { mean };
                if (g > gens - 500) { sum += mean; cnt++; }
                if (g == gens) break;
                for (int i = 0; i < n; i++) pay[i] = Payoff(x[i], mean);
                Selection.SoftWeights(pay, n, 2, 100, w);
                Selection.WrightFisher(w, n, n, root.Fork("sel", g), par, cum);
                var mr = root.Fork("mut", g);
                for (int i = 0; i < n; i++)
                {
                    long v = x[par[i]] + LabRandom.Gaussian(mr) / 100;
                    if (v < 0) v = 0;
                    if (v > Fx.One) v = Fx.One;
                    next[i] = v;
                }
                System.Array.Copy(next, x, n);
            }
            series = s;
            return sum / cnt;
        }

        private static long Pure(ulong seed, int n, int gens)
        {
            var root = LabRandom.Root(seed, "L10", "pure");
            var enter = new bool[n]; var w = new long[n]; var cum = new long[n]; var par = new int[n]; var next = new bool[n]; var pay = new long[n];
            for (int i = 0; i < n; i++) enter[i] = i % 2 == 0;
            long sum = 0; int cnt = 0;
            for (int g = 0; g < gens; g++)
            {
                int c = 0;
                for (int i = 0; i < n; i++) if (enter[i]) c++;
                long X = Fx.FromRatio(c, n);
                if (g >= gens - 500) { sum += X; cnt++; }
                for (int i = 0; i < n; i++) pay[i] = Payoff(enter[i] ? Fx.One : 0, X);
                Selection.SoftWeights(pay, n, 2, 100, w);
                Selection.WrightFisher(w, n, n, root.Fork("sel", g), par, cum);
                var mr = root.Fork("mut", g);
                for (int i = 0; i < n; i++) next[i] = LabRandom.Permille(mr, 10) ? !enter[par[i]] : enter[par[i]];
                System.Array.Copy(next, enter, n);
            }
            return sum / cnt;
        }
    }
}
