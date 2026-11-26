namespace God.Sim.Lab
{
    /// <summary>
    /// The fixed-point discrete replicator (STRATEGY_BENCHMARKS 4.5). Shares are Q30 (2^30 = 1) and always sum to exactly
    /// 2^30; payoffs are x1000. No random numbers.
    /// </summary>
    public sealed class Replicator
    {
        private readonly long[,] _a;
        private readonly long _w0;
        private readonly long _muQ20;
        public readonly int K;
        private readonly long[] _f;

        /// <param name="matrixX1000">A[i,j]: payoff of strategy i against j, x1000.</param>
        /// <param name="w0X1000">Baseline fitness w0, x1000.</param>
        /// <param name="mutationQ20">Mutation mu in 2^-20 units (0 = none): x' = (1 - mu) x' + mu / K.</param>
        public Replicator(long[,] matrixX1000, long w0X1000, long mutationQ20 = 0)
        {
            _a = matrixX1000;
            _w0 = w0X1000;
            _muQ20 = mutationQ20;
            K = matrixX1000.GetLength(0);
            _f = new long[K];
        }

        /// <summary>Mutation rate mu given as a ratio num/den, in 2^-20 units.</summary>
        public static long MuQ20(long num, long den) => Fx.MulDiv(1L << 20, num, den);

        /// <summary>The fitness f_i = w0 + sum_j A_ij x_j (x1000) of the last step.</summary>
        public long Fitness(int i) => _f[i];

        /// <summary>Fitness of each strategy against shares x (x1000), without changing anything.</summary>
        public long[] FitnessAt(long[] x)
        {
            var f = new long[K];
            for (int i = 0; i < K; i++)
            {
                long s = 0;
                for (int j = 0; j < K; j++) s += Fx.MulDiv(_a[i, j], x[j], Fx.One);
                f[i] = _w0 + s;
            }
            return f;
        }

        /// <summary>The payoff part only (sum_j A_ij x_j, x1000).</summary>
        public long PayoffAt(long[] x, int i)
        {
            long s = 0;
            for (int j = 0; j < K; j++) s += Fx.MulDiv(_a[i, j], x[j], Fx.One);
            return s;
        }

        /// <summary>One generation, in place.</summary>
        public void Step(long[] x)
        {
            long den = 0;
            for (int i = 0; i < K; i++)
            {
                long s = 0;
                for (int j = 0; j < K; j++) s += _a[i, j] * x[j]; // |A| < 2^20, x <= 2^30, K small: fits
                long f = _w0 + (s >> Fx.Bits);
                if (f <= 0) f = 1; // fitness floor (never reached by the lab's presets)
                _f[i] = f;
                den += x[i] / 1024 * f + (x[i] % 1024) * f / 1024; // sum x f / 2^10 without overflow
            }
            if (den <= 0) return;
            for (int i = 0; i < K; i++)
            {
                long num = x[i] / 1024 * _f[i] + (x[i] % 1024) * _f[i] / 1024;
                x[i] = Fx.MulDivFloor(num, Fx.One, den);
            }
            Normalise(x);
            if (_muQ20 > 0)
            {
                long add = (Fx.One * _muQ20 >> 20) / K;
                for (int i = 0; i < K; i++) x[i] = x[i] - (x[i] * _muQ20 >> 20) + add;
                Normalise(x);
            }
        }

        /// <summary>Remainder-to-largest correction so the shares sum to exactly 2^30 (ties: the lowest index).</summary>
        public static void Normalise(long[] x)
        {
            long sum = 0;
            int big = 0;
            for (int i = 0; i < x.Length; i++)
            {
                sum += x[i];
                if (x[i] > x[big]) big = i;
            }
            x[big] += Fx.One - sum;
        }

        /// <summary>Shares from permille values (normalised to 2^30).</summary>
        public static long[] SharesPermille(params int[] permille)
        {
            var x = new long[permille.Length];
            for (int i = 0; i < x.Length; i++) x[i] = Fx.FromPermille(permille[i]);
            Normalise(x);
            return x;
        }

        public static long[] SharesFx(params long[] fx)
        {
            var x = (long[])fx.Clone();
            Normalise(x);
            return x;
        }
    }

    /// <summary>
    /// Two-population replicator for a 2x2 bimatrix game (coy/fast against faithful/philanderer), continuous time with a
    /// fixed-point RK4 step (or Euler, for the negative control). State (c, f) = share of the first strategy in each
    /// population, Q30. dc/dt = c(1-c) Dc(f), df/dt = f(1-f) Df(c), with Dc and Df linear and derived from the payoffs.
    /// </summary>
    public sealed class TwoPopReplicator
    {
        // Dc(f) = c0 + c1 f ; Df(c) = d0 + d1 c   (whole payoff units)
        public readonly long C0, C1, D0, D1;
        private readonly int _stepDen;

        /// <param name="femalePay">[female strategy, male strategy] female payoff; index 0 = the tracked strategy (Coy).</param>
        /// <param name="malePay">[female strategy, male strategy] male payoff; male index 0 = the tracked strategy (Faithful).</param>
        /// <param name="stepDen">The step is 1 / stepDen time units (100 = the spec's 0.01).</param>
        public TwoPopReplicator(long[,] femalePay, long[,] malePay, int stepDen)
        {
            // Female advantage of Coy over Fast against a male population with faithful share f:
            // [f F(c,fa) + (1-f) F(c,ph)] - [f F(fa_st,fa) + (1-f) F(fast,ph)]
            long coyPh = femalePay[0, 1], coyFa = femalePay[0, 0], fastPh = femalePay[1, 1], fastFa = femalePay[1, 0];
            C0 = coyPh - fastPh;
            C1 = (coyFa - coyPh) - (fastFa - fastPh);
            // Male advantage of Faithful over Philanderer against females with coy share c.
            long faFast = malePay[1, 0], faCoy = malePay[0, 0], phFast = malePay[1, 1], phCoy = malePay[0, 1];
            D0 = faFast - phFast;
            D1 = (faCoy - faFast) - (phCoy - phFast);
            _stepDen = stepDen;
        }

        private void Deriv(long c, long f, out long dc, out long df)
        {
            long gc = Fx.Mul(c, Fx.One - c);
            long gf = Fx.Mul(f, Fx.One - f);
            dc = Fx.Mul(gc, C0 * Fx.One + C1 * f);
            df = Fx.Mul(gf, D0 * Fx.One + D1 * c);
        }

        public void StepRk4(ref long c, ref long f)
        {
            int n = _stepDen;
            Deriv(c, f, out long k1c, out long k1f);
            Deriv(c + Div(k1c, 2 * n), f + Div(k1f, 2 * n), out long k2c, out long k2f);
            Deriv(c + Div(k2c, 2 * n), f + Div(k2f, 2 * n), out long k3c, out long k3f);
            Deriv(c + Div(k3c, n), f + Div(k3f, n), out long k4c, out long k4f);
            c += Div(k1c + 2 * k2c + 2 * k3c + k4c, 6 * n);
            f += Div(k1f + 2 * k2f + 2 * k3f + k4f, 6 * n);
        }

        public void StepEuler(ref long c, ref long f)
        {
            Deriv(c, f, out long dc, out long df);
            c += Div(dc, _stepDen);
            f += Div(df, _stepDen);
        }

        private static long Div(long v, long d) => v >= 0 ? (v + d / 2) / d : -((-v + d / 2) / d);
    }
}
