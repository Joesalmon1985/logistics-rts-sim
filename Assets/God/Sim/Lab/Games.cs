namespace God.Sim.Lab
{
    /// <summary>
    /// A contest game between strategy indices, realised turn by turn. Because a contest is a pure function of its random
    /// inputs (<see cref="Contest.Resolve"/>), the outcomes for every input combination are tabulated once; a game then
    /// draws the inputs (probes, roles, coin) exactly as <see cref="Contest.Play"/> does and looks the result up.
    /// </summary>
    public sealed class ContestGame : IPairGame
    {
        private readonly ContestKind[] _kinds;
        private readonly bool _roles;
        private readonly ContestRules _r;
        // [a, b, probeA, probeB, aResident, coin] → payA, payB (whole units)
        private readonly int[] _payA, _payB;
        private readonly int _k;

        public ContestGame(ContestKind[] kinds, bool randomRoles, ContestRules rules)
        {
            _kinds = kinds; _roles = randomRoles; _r = rules; _k = kinds.Length;
            _payA = new int[_k * _k * 16];
            _payB = new int[_k * _k * 16];
            for (int a = 0; a < _k; a++)
            for (int b = 0; b < _k; b++)
            for (int pa = 0; pa < 2; pa++)
            for (int pb = 0; pb < 2; pb++)
            for (int res = 0; res < 2; res++)
            for (int coin = 0; coin < 2; coin++)
            {
                Role ra = !_roles ? Role.None : (res == 1 ? Role.Resident : Role.Intruder);
                Role rb = !_roles ? Role.None : (res == 1 ? Role.Intruder : Role.Resident);
                var o = Contest.Resolve(kinds[a], kinds[b], ra, rb, pa == 1, pb == 1, coin == 1, rules);
                int idx = Index(a, b, pa, pb, res, coin);
                _payA[idx] = o.PayA; _payB[idx] = o.PayB;
            }
        }

        private int Index(int a, int b, int pa, int pb, int res, int coin) => ((a * _k + b) * 16) + (pa << 3) + (pb << 2) + (res << 1) + coin;

        public void Play(int sa, int sb, Rng rng, out long payA, out long payB)
        {
            int pa = _kinds[sa] == ContestKind.Prober && LabRandom.Permille(rng, _r.ProbePermille) ? 1 : 0;
            int pb = _kinds[sb] == ContestKind.Prober && LabRandom.Permille(rng, _r.ProbePermille) ? 1 : 0;
            int res = 0;
            if (_roles) res = rng.NextInt(2) == 0 ? 1 : 0;
            int coin = rng.NextInt(2) == 0 ? 1 : 0;
            int idx = Index(sa, sb, pa, pb, res, coin);
            payA = _payA[idx] * 1000L;
            payB = _payB[idx] * 1000L;
        }
    }

    /// <summary>A fixed expected-payoff matrix as a game (no randomness), x1000.</summary>
    public sealed class MatrixGame : IPairGame
    {
        private readonly long[,] _a;
        public MatrixGame(long[,] matrixX1000) { _a = matrixX1000; }
        public void Play(int sa, int sb, Rng rng, out long payA, out long payB) { payA = _a[sa, sb]; payB = _a[sb, sa]; }
    }

    /// <summary>
    /// The cumulative distribution of a geometric match length (round 1 always, each further round with probability w),
    /// tabulated in Q30 so one uniform draw and a binary search give the length. The tail beyond the table (probability
    /// below 2^-30) is drawn round by round, so the distribution is exact to the precision of the draw.
    /// </summary>
    public sealed class GeometricLength
    {
        private readonly int _w;
        private readonly long[] _survive; // _survive[n] = P(L > n+1) in Q30, decreasing

        public GeometricLength(int continuePermille)
        {
            _w = continuePermille;
            var l = new System.Collections.Generic.List<long>();
            long s = Fx.One;
            while (true)
            {
                s = Fx.MulDiv(s, continuePermille, 1000);
                if (s <= 0 || l.Count > 100000) break;
                l.Add(s);
            }
            _survive = l.ToArray();
        }

        public int Draw(Rng rng)
        {
            long u = LabRandom.Unit(rng);
            // L = 1 + number of n with u < survive[n-1]
            int lo = 0, hi = _survive.Length;
            while (lo < hi)
            {
                int mid = (lo + hi) >> 1;
                if (u < _survive[mid]) lo = mid + 1; else hi = mid;
            }
            int len = 1 + lo;
            if (lo == _survive.Length) while (rng.NextInt(1000) < _w) len++;
            return len;
        }
    }

    /// <summary>
    /// Repeated games between strategy indices: each pairing plays one match of <c>MatchRounds</c> rounds, or of a
    /// geometric length with continuation w. Deterministic pairs use <see cref="DeterministicMatchCache"/> (exact).
    /// Payoffs are the match totals, x1000.
    /// </summary>
    public sealed class RepeatedGame : IPairGame
    {
        private readonly RepeatedKind[] _kinds;
        private readonly int[] _params;
        private readonly PdPayoffs _pay;
        private readonly int _rounds;
        private readonly GeometricLength _geo;
        private readonly DeterministicMatchCache[,] _cache;

        public RepeatedGame(RepeatedKind[] kinds, PdPayoffs pay, int matchRounds, int continuePermille)
        {
            _kinds = kinds; _pay = pay; _rounds = matchRounds;
            _params = new int[kinds.Length];
            for (int i = 0; i < kinds.Length; i++) _params[i] = RepeatedRules.DefaultParam(kinds[i]);
            if (matchRounds <= 0) _geo = new GeometricLength(continuePermille);
            _cache = new DeterministicMatchCache[kinds.Length, kinds.Length];
            for (int a = 0; a < kinds.Length; a++)
            for (int b = 0; b < kinds.Length; b++)
                if (RepeatedRules.IsDeterministic(kinds[a]) && RepeatedRules.IsDeterministic(kinds[b]) && kinds[a] != RepeatedKind.TftEnd && kinds[b] != RepeatedKind.TftEnd)
                    _cache[a, b] = new DeterministicMatchCache(kinds[a], kinds[b], pay);
        }

        public void Play(int sa, int sb, Rng rng, out long payA, out long payB)
        {
            int len = _rounds > 0 ? _rounds : _geo.Draw(rng);
            var c = _cache[sa, sb];
            long a, b;
            if (c != null) c.Scores(len, out a, out b);
            else RepeatedMatch.Play(_kinds[sa], _params[sa], _kinds[sb], _params[sb], len, _pay, rng, rng, out a, out b);
            payA = a * 1000; payB = b * 1000;
        }
    }
}
