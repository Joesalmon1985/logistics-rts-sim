namespace God.Sim.Lab
{
    /// <summary>The exchange benchmarks of STRATEGY_BENCHMARKS 4.3, plus the L8b variant TFT-End.</summary>
    public enum RepeatedKind : byte
    {
        AllD, AllC, Random, Tft, Tf2t, Stft, Grudger, NaiveProber, RemorsefulProber, Pavlov, Tester, Sneak,
        /// <summary>L8b: Tit for Tat that defects on the final round when the length is known (<see cref="PairMemory.KnownLength"/>).</summary>
        TftEnd,
    }

    /// <summary>
    /// The per-partner memory of a repeated-game benchmark. One struct covers every strategy; each strategy reads only the
    /// fields its row in the 4.3 table allows (the "memory" column), which the unit tests pin with golden sequences.
    /// </summary>
    public struct PairMemory
    {
        /// <summary>Rounds already played with this partner.</summary>
        public int Round;
        public Move TheirLast, TheirLast2, MyLast;
        /// <summary>Grudger: the partner has ever defected.</summary>
        public bool Grudge;
        /// <summary>Probers: my last move was a probe (a defection where the base rule said cooperate).</summary>
        public bool LastWasProbe;
        /// <summary>Tester: the partner defected on round 2 (retaliated for my round-1 defection).</summary>
        public bool Retaliated;
        /// <summary>TFT-End: the announced match length (0 = unknown, so it never knows the final round).</summary>
        public int KnownLength;
    }

    /// <summary>The state machines of 4.3 (exact; see the table). Random draws only where the table says, from the given stream.</summary>
    public static class RepeatedRules
    {
        public const int DefaultProbePermille = 100;
        public const int DefaultRandomPermille = 500;

        public static string Key(RepeatedKind k)
        {
            switch (k)
            {
                case RepeatedKind.AllD: return "bench.alld";
                case RepeatedKind.AllC: return "bench.allc";
                case RepeatedKind.Random: return "bench.random";
                case RepeatedKind.Tft: return "bench.tft";
                case RepeatedKind.Tf2t: return "bench.tf2t";
                case RepeatedKind.Stft: return "bench.stft";
                case RepeatedKind.Grudger: return "bench.grudger";
                case RepeatedKind.NaiveProber: return "bench.naive_prober";
                case RepeatedKind.RemorsefulProber: return "bench.remorseful_prober";
                case RepeatedKind.Pavlov: return "bench.pavlov";
                case RepeatedKind.Tester: return "bench.tester";
                case RepeatedKind.Sneak: return "bench.sneak";
                default: return "lab.tft_end";
            }
        }

        public static string Name(RepeatedKind k)
        {
            switch (k)
            {
                case RepeatedKind.AllD: return "Always Defect";
                case RepeatedKind.AllC: return "Always Cooperate";
                case RepeatedKind.Random: return "Random";
                case RepeatedKind.Tft: return "Tit for Tat";
                case RepeatedKind.Tf2t: return "Tit for Two Tats";
                case RepeatedKind.Stft: return "Suspicious TFT";
                case RepeatedKind.Grudger: return "Grudger";
                case RepeatedKind.NaiveProber: return "Naive Prober";
                case RepeatedKind.RemorsefulProber: return "Remorseful Prober";
                case RepeatedKind.Pavlov: return "Pavlov";
                case RepeatedKind.Tester: return "Tester";
                case RepeatedKind.Sneak: return "Sneak";
                default: return "TFT-End";
            }
        }

        /// <summary>Memory per partner in bits (4.3 table; Sneak keeps a round counter, reported as -1).</summary>
        public static int MemoryBits(RepeatedKind k)
        {
            switch (k)
            {
                case RepeatedKind.AllD: case RepeatedKind.AllC: case RepeatedKind.Random: return 0;
                case RepeatedKind.Tft: case RepeatedKind.Stft: case RepeatedKind.Grudger: case RepeatedKind.NaiveProber: return 1;
                case RepeatedKind.Tf2t: case RepeatedKind.RemorsefulProber: case RepeatedKind.Pavlov: case RepeatedKind.Tester: return 2;
                default: return -1;
            }
        }

        /// <summary>Nice = never the first to defect (4.3 "Class"; probers, Tester, Sneak and STFT are nasty; Random is neither).</summary>
        public static bool IsNice(RepeatedKind k) =>
            k == RepeatedKind.AllC || k == RepeatedKind.Tft || k == RepeatedKind.Tf2t || k == RepeatedKind.Grudger || k == RepeatedKind.Pavlov;

        /// <summary>True when the strategy never draws a random number (its moves depend only on the history).</summary>
        public static bool IsDeterministic(RepeatedKind k) =>
            k != RepeatedKind.Random && k != RepeatedKind.NaiveProber && k != RepeatedKind.RemorsefulProber;

        /// <summary>The move for the next round (round <c>m.Round + 1</c>). <paramref name="p"/> is the probe or cooperation
        /// probability in permille for the random strategies (ignored otherwise).</summary>
        public static Move Decide(RepeatedKind k, ref PairMemory m, int p, Rng rng)
        {
            int n = m.Round + 1;
            switch (k)
            {
                case RepeatedKind.AllD: return Move.Defect;
                case RepeatedKind.AllC: return Move.Cooperate;
                case RepeatedKind.Random: return LabRandom.Permille(rng, p) ? Move.Cooperate : Move.Defect;
                case RepeatedKind.Tft: return n == 1 ? Move.Cooperate : m.TheirLast;
                case RepeatedKind.TftEnd:
                    if (m.KnownLength > 0 && n == m.KnownLength) return Move.Defect;
                    return n == 1 ? Move.Cooperate : m.TheirLast;
                case RepeatedKind.Tf2t:
                    return n >= 3 && m.TheirLast == Move.Defect && m.TheirLast2 == Move.Defect ? Move.Defect : Move.Cooperate;
                case RepeatedKind.Stft: return n == 1 ? Move.Defect : m.TheirLast;
                case RepeatedKind.Grudger: return m.Grudge ? Move.Defect : Move.Cooperate;
                case RepeatedKind.NaiveProber: return Prober(ref m, n, p, rng, false);
                case RepeatedKind.RemorsefulProber: return Prober(ref m, n, p, rng, true);
                case RepeatedKind.Pavlov:
                    if (n == 1) return Move.Cooperate;
                    return m.TheirLast == Move.Cooperate ? m.MyLast : Flip(m.MyLast);
                case RepeatedKind.Tester:
                    if (n == 1) return Move.Defect;
                    if (n == 2) return Move.Cooperate; // the apology round in the 4.3 table
                    if (m.Retaliated) return m.TheirLast;
                    return (n & 1) == 0 ? Move.Defect : Move.Cooperate;
                case RepeatedKind.Sneak:
                    if (n == 1) return Move.Cooperate;
                    if (n == 50 || n == 100 || n == 150) return Move.Defect;
                    return m.TheirLast;
                default: throw new System.ArgumentOutOfRangeException(nameof(k));
            }
        }

        private static Move Prober(ref PairMemory m, int n, int q, Rng rng, bool remorseful)
        {
            if (n == 1) { m.LastWasProbe = false; return Move.Cooperate; }
            if (remorseful && m.LastWasProbe && m.TheirLast == Move.Defect)
            {
                m.LastWasProbe = false; // take the hit once, do not echo
                return Move.Cooperate;
            }
            if (m.TheirLast == Move.Defect) { m.LastWasProbe = false; return Move.Defect; }
            if (LabRandom.Permille(rng, q)) { m.LastWasProbe = true; return Move.Defect; }
            m.LastWasProbe = false;
            return Move.Cooperate;
        }

        public static Move Flip(Move m) => m == Move.Cooperate ? Move.Defect : Move.Cooperate;

        /// <summary>After each round, with both moves.</summary>
        public static void Observe(ref PairMemory m, Move mine, Move theirs)
        {
            m.Round++;
            if (m.Round == 2 && theirs == Move.Defect) m.Retaliated = true;
            if (theirs == Move.Defect) m.Grudge = true;
            m.TheirLast2 = m.TheirLast;
            m.TheirLast = theirs;
            m.MyLast = mine;
        }

        /// <summary>The default random parameter of a strategy (probe q or Random's p), permille.</summary>
        public static int DefaultParam(RepeatedKind k) =>
            k == RepeatedKind.Random ? DefaultRandomPermille : (k == RepeatedKind.NaiveProber || k == RepeatedKind.RemorsefulProber ? DefaultProbePermille : 0);
    }

    // ---- the spec's interface form (STRATEGY_BENCHMARKS 3), a thin wrapper over the rules above ----

    public interface IRepeatedStrategy
    {
        string Key { get; }
        int MemoryBits { get; }
        IRepeatedState NewPair();
    }

    /// <summary>Per-partner state. <c>Rng</c> is a class in this code base, so it is passed by reference already
    /// (the spec's <c>ref Rng</c>).</summary>
    public interface IRepeatedState
    {
        Move Decide(Rng rng);
        void Observe(Move mine, Move theirs);
    }

    public sealed class BenchRepeated : IRepeatedStrategy
    {
        public readonly RepeatedKind Kind;
        public readonly int ParamPermille;
        public readonly int KnownLength;

        public BenchRepeated(RepeatedKind kind, int paramPermille = -1, int knownLength = 0)
        {
            Kind = kind;
            ParamPermille = paramPermille >= 0 ? paramPermille : RepeatedRules.DefaultParam(kind);
            KnownLength = knownLength;
        }

        public string Key => RepeatedRules.Key(Kind);
        public int MemoryBits => RepeatedRules.MemoryBits(Kind);
        public IRepeatedState NewPair() => new State(this);

        private sealed class State : IRepeatedState
        {
            private readonly BenchRepeated _s;
            private PairMemory _m;
            public State(BenchRepeated s) { _s = s; _m.KnownLength = s.KnownLength; }
            public Move Decide(Rng rng) => RepeatedRules.Decide(_s.Kind, ref _m, _s.ParamPermille, rng);
            public void Observe(Move mine, Move theirs) => RepeatedRules.Observe(ref _m, mine, theirs);
        }
    }

    /// <summary>Prisoner's Dilemma payoffs (temptation, reward, punishment, sucker), in whole units.</summary>
    public readonly struct PdPayoffs
    {
        public readonly long T, R, P, S;
        public PdPayoffs(long t, long r, long p, long s) { T = t; R = r; P = p; S = s; }

        /// <summary>Axelrod scoring, PDF 231: 5, 3, 1, 0.</summary>
        public static PdPayoffs Axelrod => new PdPayoffs(5, 3, 1, 0);
        /// <summary>The dollar example, PDF 226: +500, +300, -10, -100.</summary>
        public static PdPayoffs Dollars => new PdPayoffs(500, 300, -10, -100);

        public long Row(Move mine, Move theirs)
        {
            if (mine == Move.Cooperate) return theirs == Move.Cooperate ? R : S;
            return theirs == Move.Cooperate ? T : P;
        }

        /// <summary>T &gt; R &gt; P &gt; S and (T + S)/2 &lt; R (written without division: T + S &lt; 2R).</summary>
        public bool IsPrisonersDilemma => T > R && R > P && P > S && T + S < 2 * R;
    }

    /// <summary>Plays repeated matches between two state machines.</summary>
    public static class RepeatedMatch
    {
        /// <summary>One match of <paramref name="rounds"/> rounds. Each side draws from its own stream.</summary>
        public static void Play(RepeatedKind a, int pa, RepeatedKind b, int pb, int rounds, PdPayoffs pay, Rng rngA, Rng rngB,
            out long scoreA, out long scoreB, int knownLengthA = 0, int knownLengthB = 0)
        {
            var ma = new PairMemory { KnownLength = knownLengthA };
            var mb = new PairMemory { KnownLength = knownLengthB };
            scoreA = 0; scoreB = 0;
            for (int r = 0; r < rounds; r++)
            {
                Move x = RepeatedRules.Decide(a, ref ma, pa, rngA);
                Move y = RepeatedRules.Decide(b, ref mb, pb, rngB);
                scoreA += pay.Row(x, y);
                scoreB += pay.Row(y, x);
                RepeatedRules.Observe(ref ma, x, y);
                RepeatedRules.Observe(ref mb, y, x);
            }
        }
    }

    /// <summary>
    /// Cumulative payoffs of a pair of deterministic strategies, by round, extended on demand. Strategies that never draw
    /// and never see the match length play the same moves whatever the length, so a match of length L is a lookup. This
    /// is exact (the same numbers as playing the match), only faster.
    /// </summary>
    public sealed class DeterministicMatchCache
    {
        private readonly RepeatedKind _a, _b;
        private readonly PdPayoffs _pay;
        private PairMemory _ma, _mb;
        private long[] _cumA = new long[64], _cumB = new long[64];
        private int _len;

        public DeterministicMatchCache(RepeatedKind a, RepeatedKind b, PdPayoffs pay)
        {
            if (!RepeatedRules.IsDeterministic(a) || !RepeatedRules.IsDeterministic(b)) throw new System.ArgumentException("both strategies must be deterministic");
            _a = a; _b = b; _pay = pay;
        }

        public void Scores(int rounds, out long a, out long b)
        {
            while (_len < rounds) Extend();
            a = rounds == 0 ? 0 : _cumA[rounds - 1];
            b = rounds == 0 ? 0 : _cumB[rounds - 1];
        }

        private void Extend()
        {
            if (_len == _cumA.Length)
            {
                System.Array.Resize(ref _cumA, _len * 2);
                System.Array.Resize(ref _cumB, _len * 2);
            }
            Move x = RepeatedRules.Decide(_a, ref _ma, 0, null);
            Move y = RepeatedRules.Decide(_b, ref _mb, 0, null);
            long pa = _pay.Row(x, y), pb = _pay.Row(y, x);
            _cumA[_len] = (_len > 0 ? _cumA[_len - 1] : 0) + pa;
            _cumB[_len] = (_len > 0 ? _cumB[_len - 1] : 0) + pb;
            RepeatedRules.Observe(ref _ma, x, y);
            RepeatedRules.Observe(ref _mb, y, x);
            _len++;
        }
    }
}
