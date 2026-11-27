using System.Collections.Generic;

namespace God.Sim.Lab
{
    /// <summary>L12. The selfish herd (PDF 190 to 192). STRATEGY_BENCHMARKS 4.6 L12; ACCEPTANCE M2.12.</summary>
    public sealed class L12SelfishHerd : LabExperiment
    {
        public override string Id => "L12";
        public override string Title => "The selfish herd";
        public override string Source => "PDF 190-192";
        public override ReproStatus Status => ReproStatus.Invented;
        public override string StatusText => "Invented model: the ring, prey count and predator rule are ours (the book describes Hamilton's argument without a model to copy); only the qualitative claim (prey bunch) is tested";
        public override string ExpectedSummary => "mean nearest-neighbour distance falls below 0.2 of its random-start value (50 runs; proto 0.006); edge prey attacked more than centre prey (reported)";

        private const long Unit = 1 << 16;          // positions in 1/65536 of a unit
        private const long Ring = 1000 * Unit;
        private const int Prey = 20, Steps = 300;

        protected override void Execute(LabConfig cfg, LabResult res, CheckList ck, List<string> notes)
        {
            int runs = cfg.ParamInt("runs", 50);
            long startSum = 0, endSum = 0;
            long edgeAttacks = 0, edgeExposure = 0, centreAttacks = 0, centreExposure = 0;
            var curve = new long[Steps + 1];
            for (int run = 0; run < runs; run++)
            {
                var rng = LabRandom.Root(cfg.Seed, Id, "run").Fork("run", run);
                var x = new long[Prey];
                for (int i = 0; i < Prey; i++) x[i] = LabRandom.NextLong(rng, Ring);
                var nx = new long[Prey];
                long d0 = MeanNn(x);
                startSum += d0;
                curve[0] += d0;
                for (int step = 1; step <= Steps; step++)
                {
                    for (int i = 0; i < Prey; i++)
                    {
                        long d = NearestSigned(x, i);
                        long move = d > 0 ? System.Math.Min(Unit, d) : -System.Math.Min(Unit, -d);
                        nx[i] = Wrap(x[i] + move);
                    }
                    System.Array.Copy(nx, x, Prey);
                    // the predator appears at a random place and takes (attacks) the nearest prey; the prey is not removed
                    long p = LabRandom.NextLong(rng, Ring);
                    int victim = 0; long best = long.MaxValue;
                    for (int i = 0; i < Prey; i++) { long d = Dist(p, x[i]); if (d < best) { best = d; victim = i; } }
                    for (int i = 0; i < Prey; i++)
                    {
                        bool edge = IsEdge(x, i);
                        if (edge) { edgeExposure++; if (i == victim) edgeAttacks++; }
                        else { centreExposure++; if (i == victim) centreAttacks++; }
                    }
                    curve[step] += MeanNn(x);
                }
                endSum += MeanNn(x);
            }
            long ratio = Fx.MulDiv(1000000, endSum, startSum);
            ck.Report("mean nearest-neighbour distance at the random start (units)", Fx.MulDiv(startSum, 1000000, runs * Unit), "proto 24.8", 2);
            ck.Report("mean nearest-neighbour distance after 300 steps (units)", Fx.MulDiv(endSum, 1000000, runs * Unit), "proto 0.14", 3);
            ck.Below("end / start mean nearest-neighbour distance", ratio, 200000, note: "proto 0.006");
            long edgeRate = edgeExposure > 0 ? Fx.MulDiv(1000000, edgeAttacks, edgeExposure) : 0;
            long centreRate = centreExposure > 0 ? Fx.MulDiv(1000000, centreAttacks, centreExposure) : 0;
            ck.Report("attacks per prey-step: edge prey (a gap above 2 units on one side)", edgeRate, "calibration, not yet seen: above the centre rate", 4);
            ck.Report("attacks per prey-step: centre prey", centreRate, "reported", 4);
            res.Strategies = new[] { "mean_nn_distance_over_start" };
            var series = new long[Steps + 1][];
            for (int s = 0; s <= Steps; s++) series[s] = new[] { Fx.MulDiv(Fx.One, curve[s], curve[0]) };
            res.SharesPerGeneration = series;
            res.SeriesTitle = "mean nearest-neighbour distance / its start value, per step, pooled over " + runs + " runs";
            notes.Add("Each step every prey moves up to 1 unit towards its nearest neighbour (all at once; at most the distance to it, so close pairs can swap and keep a small gap).");
            notes.Add("The predator's attack is counted but the prey stays (our choice: with removal 20 prey cannot last 300 steps).");
        }

        private static long Wrap(long v) { v %= Ring; return v < 0 ? v + Ring : v; }

        private static long Dist(long a, long b) { long d = a - b; if (d < 0) d = -d; return d > Ring - d ? Ring - d : d; }

        /// <summary>The signed shortest displacement from prey i to its nearest neighbour (ties: lowest index).</summary>
        private static long NearestSigned(long[] x, int i)
        {
            long best = long.MaxValue, bestSigned = 0;
            for (int j = 0; j < x.Length; j++)
            {
                if (j == i) continue;
                long d = Wrap(x[j] - x[i]); // forward distance
                long signed = d <= Ring / 2 ? d : d - Ring;
                long abs = signed < 0 ? -signed : signed;
                if (abs < best) { best = abs; bestSigned = signed; }
            }
            return bestSigned;
        }

        private static long MeanNn(long[] x)
        {
            long sum = 0;
            for (int i = 0; i < x.Length; i++) { long d = NearestSigned(x, i); sum += d < 0 ? -d : d; }
            return sum / x.Length;
        }

        /// <summary>Edge of a group: the gap to the nearest prey ahead or behind (on the ring) is above 2 units.</summary>
        private static bool IsEdge(long[] x, int i)
        {
            long ahead = long.MaxValue, behind = long.MaxValue;
            for (int j = 0; j < x.Length; j++)
            {
                if (j == i) continue;
                long f = Wrap(x[j] - x[i]);
                long b = Wrap(x[i] - x[j]);
                if (f < ahead) ahead = f;
                if (b < behind) behind = b;
            }
            return ahead > 2 * Unit || behind > 2 * Unit;
        }
    }
}
