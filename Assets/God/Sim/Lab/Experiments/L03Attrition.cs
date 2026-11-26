using System.Collections.Generic;

namespace God.Sim.Lab
{
    /// <summary>
    /// L3. War of attrition (PDF 99, 100). STRATEGY_BENCHMARKS 4.6 L3; ACCEPTANCE M2.5. Times and payoffs are Q30 in units
    /// of the prize V (V = 1). Both sides pay 1 per unit time until one quits; the more persistent side wins V.
    /// </summary>
    public sealed class L03Attrition : LabExperiment
    {
        public override string Id => "L3";
        public override string Title => "War of attrition";
        public override string Source => "PDF 99-100";
        public override ReproStatus Status => ReproStatus.Invented;
        public override string StatusText => "Invented parameters, theory check: prize, cost rate, selection and mutation are ours; the checks test the model's known mathematics (mean persistence V, fixed limits invadable, the value of a tell), not a figure from the book";
        public override string ExpectedSummary => "mean m = V +/- 0.2 V; CV of persistence 1.0-1.5; every fixed limit L in [1, V-2] invaded by L+1; a tell gains 0.368 V +/- 0.01 V over 100,000 contests; readers spread above 0.9 in 100 generations; a hidden tell gains 0 +/- 0.01 V";

        private const long V = Fx.One;

        /// <summary>Payoffs of a contest with quitting times ta, tb (Q30, units of V); ties split the prize.</summary>
        public static void Contest(long ta, long tb, long v, out long pa, out long pb)
        {
            long t = ta < tb ? ta : tb;
            if (ta > tb) { pa = v - t; pb = -t; }
            else if (tb > ta) { pa = -t; pb = v - t; }
            else { pa = v / 2 - t; pb = v / 2 - t; }
        }

        protected override void Execute(LabConfig cfg, LabResult res, CheckList ck, List<string> notes)
        {
            int n = cfg.AgentsOr(1000), k = cfg.ContestsOr(20), gens = cfg.GenerationsOr(400);
            var root = LabRandom.Root(cfg.Seed, Id, "evolve");
            var m = new long[n];
            var init = root.Fork("init");
            for (int a = 0; a < n; a++) m[a] = Fx.FromPermille(200) + Fx.Mul(LabRandom.Unit(init), Fx.FromPermille(2800)); // uniform [0.2 V, 3 V]
            var perm = new int[n];
            var pay = new long[n];
            var w = new long[n];
            var cum = new long[n];
            var parents = new int[n];
            var next = new long[n];
            var series = new List<long[]>();
            long lastSum = 0; int lastCount = 0;
            long tSum = 0, tSq = 0; long tN = 0; // realised persistence, last 100 generations (Q30 sums, sq in Q30 too)
            for (int gen = 0; gen < gens; gen++)
            {
                long mean = 0;
                for (int a = 0; a < n; a++) mean += m[a];
                mean /= n;
                series.Add(new[] { mean });
                bool last = gen >= gens - 100;
                if (last) { lastSum += mean; lastCount++; }
                System.Array.Clear(pay, 0, n);
                for (int r = 0; r < k; r++)
                {
                    LabRandom.Permutation(root.Fork("pair", gen, r), perm, n);
                    var gr = root.Fork("game", gen, r);
                    for (int i = 0; i + 1 < n; i += 2)
                    {
                        int a = perm[i], b = perm[i + 1];
                        long ta = LabRandom.Exponential(gr, m[a]), tb = LabRandom.Exponential(gr, m[b]);
                        Contest(ta, tb, V, out long pa, out long pb);
                        pay[a] += pa; pay[b] += pb;
                        if (last) { tSum += ta + tb; tSq += Sq(ta) + Sq(tb); tN += 2; }
                    }
                }
                // soft selection exp(f / (0.3 k V)); payoffs here are Q30, so f / (0.3 k) = f * 10 / (3 k)
                long max = long.MinValue;
                for (int a = 0; a < n; a++) if (pay[a] > max) max = pay[a];
                for (int a = 0; a < n; a++) { long x = Fx.MulDiv(pay[a] - max, 10, 3L * k); long e = Fx.Exp(x); w[a] = e > 0 ? e : 1; }
                Selection.WrightFisher(w, n, n, root.Fork("sel", gen), parents, cum);
                var mr = root.Fork("mut", gen);
                for (int a = 0; a < n; a++)
                {
                    // log-normal factor with a spread of 5 per cent
                    long f = Fx.Exp(LabRandom.Gaussian(mr) / 20);
                    long v = Fx.Mul(m[parents[a]], f);
                    if (v < Fx.One / 100) v = Fx.One / 100;
                    if (v > 50 * Fx.One) v = 50 * Fx.One;
                    next[a] = v;
                }
                System.Array.Copy(next, m, n);
            }
            res.Strategies = new[] { "mean_m_over_V" };
            res.SharesPerGeneration = series.ToArray();
            res.SeriesTitle = "mean heritable persistence m / V per generation, N=" + n + ", k=" + k;
            long meanLast = lastSum / lastCount;
            ck.Near("mean m over the last 100 generations (units of V)", LabMath.Micro(meanLast), 1000000, 200000, note: "proto 1.105 to 1.129 V in 3 seeds");
            long tMean = tSum / tN;
            long tVar = tSq / tN - Sq(tMean);
            long cv = Fx.Div(Fx.Sqrt(tVar), tMean);
            ck.Between("coefficient of variation of realised persistence", LabMath.Micro(cv), 1000000, 1500000, note: "calibration target from the spec ('not yet seen')");

            // Fixed limits, V = 100 ticks: L + 1 invades L for every L in [1, V - 2].
            int fails = 0;
            for (int L = 1; L <= 98; L++)
            {
                Contest(L, L, 100, out long res0, out _);            // resident against resident
                Contest(L + 1, L, 100, out long inv, out _);         // invader against resident
                if (inv <= res0) fails++;
            }
            ck.Add("fixed limits L in [1, 98] (V = 100) not invaded by L + 1", fails * 1000000L, "0", fails == 0, decimals: 0);

            // The tell: a reader sees the opponent's quitting time t; if t < V it stays just longer and wins, else quits at once.
            int nTell = cfg.ParamInt("tell", 100000);
            var tr = LabRandom.Root(cfg.Seed, Id, "tell");
            long readerSum = 0, baseSum = 0, hiddenSum = 0, hiddenBase = 0;
            for (int i = 0; i < nTell; i++)
            {
                long t = LabRandom.Exponential(tr, V);      // opponent's quitting time, mean V
                long own = LabRandom.Exponential(tr, V);    // a normal player's own time (and the hidden reader's false reading)
                Contest(Reader(t), t, V, out long pr, out _);
                Contest(own, t, V, out long pbse, out _);
                readerSum += pr; baseSum += pbse;
            }
            long gain = (readerSum - baseSum) / nTell;
            ck.Near("tell: reader's gain per contest against exponential opponents of mean V", LabMath.Micro(gain), 367879, 10000, note: "V/e = 0.368 V");
            int nHidden = cfg.ParamInt("hidden", 400000);
            var hr = LabRandom.Root(cfg.Seed, Id, "hidden");
            for (int i = 0; i < nHidden; i++)
            {
                long t = LabRandom.Exponential(hr, V);
                long seen = LabRandom.Exponential(hr, V); // the tell is hidden: what the reader "sees" is independent of t
                Contest(Reader(seen), t, V, out long ph, out _);
                Contest(seen, t, V, out long pb2, out _);  // common random numbers: the normal player uses the same draw as its time
                hiddenSum += ph; hiddenBase += pb2;
            }
            long hiddenGain = (hiddenSum - hiddenBase) / nHidden;
            ck.Near("hidden tell: reader's gain per contest (" + nHidden + " contests, common random numbers)", LabMath.Micro(hiddenGain), 0, 10000);

            // Readers spread when the tell is readable.
            long readerShare = ReaderSpread(cfg.Seed, n, k);
            // Published: reader share above 0.9 within 100 generations. Amended at M2: the opponent of a reader gets
            // V(3/e - 1) = 0.104 V (it wins whenever the reader quits at once), so a reader's edge 1/e - p(4/e - 1) vanishes at
            // p* = 1/(4 - e) = 0.780; readers spread to that mixed equilibrium, not to fixation.
            ck.Near("readable tell: reader share after 100 generations (start 0.1), against p* = 1/(4 - e)", LabMath.Micro(readerShare), 779800, 50000, kind: CheckKind.Amended,
                note: "Amended at M2. Published: above 0.9 within 100 generations. A reader's opponent earns V(3/e - 1) = 0.104 V, so readers have an edge only while their share is below p* = 1/(4 - e) = 0.780 (two readers play as normal players); measured 0.74-0.78 in seeds 1-5. Corrected expectation: readers spread from 0.1 to 0.78 +/- 0.05.",
                source: "L3");

            notes.Add("Times are Q30 fixed point in units of V; exponential draws use -m ln U with a fixed-point logarithm.");
            notes.Add("Two readers facing each other cannot exploit each other's tell and play their own exponential times (our choice).");
        }

        private static long Reader(long opponentTime) => opponentTime < V ? opponentTime + 1 : 0;

        private static long Sq(long v) => Fx.Mul(v, v);

        /// <summary>Readers (heritable flag) against normal exponential(V) players; soft selection as above, no mutation.</summary>
        private static long ReaderSpread(ulong seed, int n, int k)
        {
            var root = LabRandom.Root(seed, "L3", "readers");
            var reader = new bool[n];
            for (int a = 0; a < n / 10; a++) reader[a] = true;
            var perm = new int[n]; var pay = new long[n]; var w = new long[n]; var cum = new long[n]; var parents = new int[n]; var next = new bool[n];
            for (int gen = 0; gen < 100; gen++)
            {
                System.Array.Clear(pay, 0, n);
                for (int r = 0; r < k; r++)
                {
                    LabRandom.Permutation(root.Fork("pair", gen, r), perm, n);
                    var gr = root.Fork("game", gen, r);
                    for (int i = 0; i + 1 < n; i += 2)
                    {
                        int a = perm[i], b = perm[i + 1];
                        long ta = LabRandom.Exponential(gr, V), tb = LabRandom.Exponential(gr, V);
                        if (reader[a] && !reader[b]) ta = Reader(tb);
                        else if (reader[b] && !reader[a]) tb = Reader(ta);
                        Contest(ta, tb, V, out long pa, out long pb);
                        pay[a] += pa; pay[b] += pb;
                    }
                }
                long max = long.MinValue;
                for (int a = 0; a < n; a++) if (pay[a] > max) max = pay[a];
                for (int a = 0; a < n; a++) { long e = Fx.Exp(Fx.MulDiv(pay[a] - max, 10, 3L * k)); w[a] = e > 0 ? e : 1; }
                Selection.WrightFisher(w, n, n, root.Fork("sel", gen), parents, cum);
                for (int a = 0; a < n; a++) next[a] = reader[parents[a]];
                System.Array.Copy(next, reader, n);
            }
            int c = 0;
            for (int a = 0; a < n; a++) if (reader[a]) c++;
            return Fx.FromRatio(c, n);
        }
    }
}
