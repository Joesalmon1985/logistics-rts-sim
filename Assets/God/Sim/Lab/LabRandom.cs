namespace God.Sim.Lab
{
    /// <summary>
    /// Integer random draws for the lab, all on a caller-given <see cref="Rng"/> (always a keyed fork; STRATEGY_BENCHMARKS
    /// 4.5, conventions section 3). No floating point.
    /// </summary>
    public static class LabRandom
    {
        /// <summary>The lab's root stream for one experiment part: <c>Rng(seed).Fork("lab").Fork(experiment + "/" + part)</c>.
        /// Each part has its own label, so changing one part's size never changes another part's draws (test 13).</summary>
        public static Rng Root(ulong seed, string experiment, string part) => new Rng(seed).Fork("lab").Fork(experiment + "/" + part);

        /// <summary>Uniform in [0, 2^30) as a Q30 value.</summary>
        public static long Unit(Rng rng) => (long)(rng.NextULong() >> 34);

        /// <summary>Uniform in (0, 1] as a Q30 value (never zero, safe for a logarithm).</summary>
        public static long UnitPositive(Rng rng) => (long)(rng.NextULong() >> 34) + 1;

        /// <summary>True with probability p (Q30).</summary>
        public static bool ChanceFx(Rng rng, long p) => Unit(rng) < p;

        /// <summary>True with probability p / 1000.</summary>
        public static bool Permille(Rng rng, int p) => p >= 1000 || (p > 0 && rng.NextInt(1000) < p);

        /// <summary>Unbiased uniform integer in [0, n) for a positive long n.</summary>
        public static long NextLong(Rng rng, long n)
        {
            if (n <= 0) throw new System.ArgumentOutOfRangeException(nameof(n));
            ulong bound = (ulong)n;
            ulong limit = ulong.MaxValue - (ulong.MaxValue % bound);
            ulong x;
            do { x = rng.NextULong(); } while (x >= limit);
            return (long)(x % bound);
        }

        /// <summary>Approximately standard normal (Irwin-Hall: the sum of 12 uniforms minus 6), Q30.</summary>
        public static long Gaussian(Rng rng)
        {
            long s = 0;
            for (int i = 0; i < 12; i++) s += Unit(rng);
            return s - 6 * Fx.One;
        }

        /// <summary>Exponential with the given mean (Q30): -mean × ln U.</summary>
        public static long Exponential(Rng rng, long mean) => Fx.Mul(mean, -Fx.Ln(UnitPositive(rng)));

        /// <summary>A geometric match length: round 1 always, then each further round with probability w / 1000.</summary>
        public static int Geometric(Rng rng, int continuePermille)
        {
            int n = 1;
            while (rng.NextInt(1000) < continuePermille) n++;
            return n;
        }

        /// <summary>Fisher-Yates shuffle of 0..n-1 written into perm (ascending ids as the base order).</summary>
        public static void Permutation(Rng rng, int[] perm, int n)
        {
            for (int i = 0; i < n; i++) perm[i] = i;
            for (int i = n - 1; i > 0; i--)
            {
                int j = rng.NextInt(i + 1);
                int t = perm[i]; perm[i] = perm[j]; perm[j] = t;
            }
        }
    }
}
