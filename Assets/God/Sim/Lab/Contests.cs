namespace God.Sim.Lab
{
    /// <summary>The contest benchmarks of STRATEGY_BENCHMARKS 4.2.</summary>
    public enum ContestKind : byte { Hawk, Dove, Retaliator, Bully, Prober, Bourgeois, AntiBourgeois }

    /// <summary>What a contestant sees on its turn (4.2 / section 3).</summary>
    public struct ContestView
    {
        public int Turn;
        public Role MyRole;
        public CMove OppLast;
        public CMove MyLast;
        public bool OppEscalatedEver;
        public bool MutualExchangeHappened;
        public int MyHealthPermille;
    }

    /// <summary>Contest parameters. Lab values: V 50, I 100, T 10, S 10, MaxTurns 8, probe q 100 permille.</summary>
    public readonly struct ContestRules
    {
        public readonly int V, I, T, S, MaxTurns, ProbePermille;
        /// <summary>
        /// Optional first-blow cost (lab variant, D3; 0 in L2 and every preset, so they are unchanged). A contestant that
        /// plays Display on turn 1 while its opponent plays Escalate on turn 1 pays this once, on top of whatever else the
        /// contest gives it, whatever it does afterwards and however the contest ends. Both sides can pay (never both:
        /// a side that displays is struck only by an escalating opponent). Dove, Retaliator and a non-probing Prober
        /// open with Display, so they pay against an opening Hawk, Bully or probing Prober; it is not charged to Hawk or Bully.
        /// </summary>
        public readonly int FirstBlow;
        public ContestRules(int v, int i, int t, int s, int maxTurns, int probePermille) : this(v, i, t, s, maxTurns, probePermille, 0) { }
        public ContestRules(int v, int i, int t, int s, int maxTurns, int probePermille, int firstBlow)
        {
            V = v; I = i; T = t; S = s; MaxTurns = maxTurns; ProbePermille = probePermille; FirstBlow = firstBlow;
        }
        /// <summary>The same rules with a first-blow cost.</summary>
        public ContestRules WithFirstBlow(int cost) => new ContestRules(V, I, T, S, MaxTurns, ProbePermille, cost);
        public static ContestRules Lab => new ContestRules(50, 100, 10, 10, 8, 100);
    }

    /// <summary>One contestant's state for one contest (Prober's probe-mode bit is the only memory).</summary>
    public struct ContestState
    {
        public ContestKind Kind;
        public bool ProbeMode;

        public CMove Decide(in ContestView v)
        {
            switch (Kind)
            {
                case ContestKind.Hawk: return CMove.Escalate;
                case ContestKind.Dove: return v.Turn > 1 && v.OppLast == CMove.Escalate ? CMove.Withdraw : CMove.Display;
                case ContestKind.Retaliator: return v.OppLast == CMove.Escalate ? CMove.Escalate : CMove.Display;
                case ContestKind.Bully:
                    if (v.Turn == 1) return CMove.Escalate;
                    return v.OppLast == CMove.Escalate ? CMove.Withdraw : CMove.Escalate;
                case ContestKind.Prober:
                    if (v.Turn == 1) return ProbeMode ? CMove.Escalate : CMove.Display;
                    if (ProbeMode)
                    {
                        if (v.OppLast == CMove.Display) return CMove.Escalate;
                        ProbeMode = false; // the probe failed: Retaliator rules for the rest of the contest
                    }
                    return v.OppLast == CMove.Escalate ? CMove.Escalate : CMove.Display;
                case ContestKind.Bourgeois: return v.MyRole == Role.Resident ? CMove.Escalate : CMove.Withdraw;
                case ContestKind.AntiBourgeois: return v.MyRole == Role.Intruder ? CMove.Escalate : CMove.Withdraw;
                default: throw new System.ArgumentOutOfRangeException();
            }
        }
    }

    /// <summary>The outcome of one contest.</summary>
    public struct ContestOutcome
    {
        public int PayA, PayB;
        public int Turns;
        public bool Fight;
        public bool MutualExchange;
        /// <summary>The move sequences, for golden tests (index = turn - 1); filled when the caller asks.</summary>
        public CMove[] MovesA, MovesB;
    }

    /// <summary>
    /// The turn-level contest (4.2). All randomness enters as inputs: each side's probe decision (drawn once at the start,
    /// only for a Prober) and one coin that decides any coin flip (true = A wins). So a contest is a pure function, and
    /// its expected payoff can be enumerated exactly.
    ///
    /// Rules, as written in 4.2, with two points the text leaves open settled here (recorded in the M2 report):
    /// - a display block (both Display) ends the contest: each pays T and the coin gives V to one side;
    /// - both Withdraw in a contest with no mutual exchange: nobody gets the prize and nobody pays. This is what the
    ///   4.2 resident/intruder matrix needs (Bourgeois as intruder against Anti-Bourgeois as resident: both flee, 0 each,
    ///   which gives the table's -12.5).
    /// </summary>
    public static class Contest
    {
        public static string Name(ContestKind k)
        {
            switch (k)
            {
                case ContestKind.Hawk: return "Hawk";
                case ContestKind.Dove: return "Dove";
                case ContestKind.Retaliator: return "Retaliator";
                case ContestKind.Bully: return "Bully";
                case ContestKind.Prober: return "Prober";
                case ContestKind.Bourgeois: return "Bourgeois";
                default: return "Anti-Bourgeois";
            }
        }

        public static string Key(ContestKind k)
        {
            switch (k)
            {
                case ContestKind.Hawk: return "bench.hawk";
                case ContestKind.Dove: return "bench.dove";
                case ContestKind.Retaliator: return "bench.retaliator";
                case ContestKind.Bully: return "bench.bully";
                case ContestKind.Prober: return "bench.prober";
                case ContestKind.Bourgeois: return "bench.resident_rule";
                default: return "bench.intruder_rule";
            }
        }

        public static ContestOutcome Resolve(ContestKind a, ContestKind b, Role roleA, Role roleB, bool probeA, bool probeB, bool coinA,
            in ContestRules r, bool recordMoves = false)
        {
            var o = ResolveTurns(a, b, roleA, roleB, probeA, probeB, coinA, r, recordMoves, out bool fbA, out bool fbB);
            if (r.FirstBlow != 0) // the first-blow cost of the D3 variant (see ContestRules.FirstBlow); absent in L2
            {
                if (fbA) o.PayA -= r.FirstBlow;
                if (fbB) o.PayB -= r.FirstBlow;
            }
            return o;
        }

        private static ContestOutcome ResolveTurns(ContestKind a, ContestKind b, Role roleA, Role roleB, bool probeA, bool probeB, bool coinA,
            in ContestRules r, bool recordMoves, out bool firstBlowA, out bool firstBlowB)
        {
            firstBlowA = firstBlowB = false;
            var sa = new ContestState { Kind = a, ProbeMode = a == ContestKind.Prober && probeA };
            var sb = new ContestState { Kind = b, ProbeMode = b == ContestKind.Prober && probeB };
            var o = new ContestOutcome();
            if (recordMoves) { o.MovesA = new CMove[r.MaxTurns]; o.MovesB = new CMove[r.MaxTurns]; }
            CMove lastA = CMove.Display, lastB = CMove.Display;
            bool mutualPrev = false, mutualEver = false, escA = false, escB = false;
            int winV = r.V;
            for (int turn = 1; turn <= r.MaxTurns; turn++)
            {
                var va = new ContestView { Turn = turn, MyRole = roleA, OppLast = lastB, MyLast = lastA, OppEscalatedEver = escB, MutualExchangeHappened = mutualEver, MyHealthPermille = 1000 };
                var vb = new ContestView { Turn = turn, MyRole = roleB, OppLast = lastA, MyLast = lastB, OppEscalatedEver = escA, MutualExchangeHappened = mutualEver, MyHealthPermille = 1000 };
                CMove ma = sa.Decide(va), mb = sb.Decide(vb);
                if (recordMoves) { o.MovesA[turn - 1] = ma; o.MovesB[turn - 1] = mb; }
                o.Turns = turn;
                if (turn == 1)
                {
                    firstBlowA = ma == CMove.Display && mb == CMove.Escalate;
                    firstBlowB = mb == CMove.Display && ma == CMove.Escalate;
                }
                if (ma == CMove.Withdraw || mb == CMove.Withdraw)
                {
                    int cost = mutualEver ? r.S : 0;
                    if (ma == CMove.Withdraw && mb == CMove.Withdraw)
                    {
                        if (mutualEver) { o.PayA = (coinA ? winV : 0) - cost; o.PayB = (coinA ? 0 : winV) - cost; }
                        else { o.PayA = 0; o.PayB = 0; }
                    }
                    else if (ma == CMove.Withdraw) { o.PayA = -cost; o.PayB = winV - cost; }
                    else { o.PayA = winV - cost; o.PayB = -cost; }
                    o.MutualExchange = mutualEver;
                    return o;
                }
                if (ma == CMove.Escalate && mb == CMove.Escalate)
                {
                    if (mutualPrev)
                    {
                        o.Fight = true; o.MutualExchange = true;
                        o.PayA = coinA ? winV : -r.I;
                        o.PayB = coinA ? -r.I : winV;
                        return o;
                    }
                    mutualPrev = true; mutualEver = true;
                }
                else
                {
                    mutualPrev = false;
                    if (ma == CMove.Display && mb == CMove.Display) return DisplayBlock(ref o, coinA, r, mutualEver);
                }
                if (ma == CMove.Escalate) escA = true;
                if (mb == CMove.Escalate) escB = true;
                lastA = ma; lastB = mb;
            }
            return DisplayBlock(ref o, coinA, r, mutualEver); // MaxTurns reached: treated as a display block
        }

        private static ContestOutcome DisplayBlock(ref ContestOutcome o, bool coinA, in ContestRules r, bool mutual)
        {
            o.PayA = (coinA ? r.V : 0) - r.T;
            o.PayB = (coinA ? 0 : r.V) - r.T;
            o.MutualExchange = mutual;
            return o;
        }

        /// <summary>A contest with all random inputs drawn from <paramref name="rng"/>: probes (only for a Prober), roles
        /// (when <paramref name="randomRoles"/>: A is resident with probability 1/2, B gets the other role), then the coin.</summary>
        public static ContestOutcome Play(ContestKind a, ContestKind b, bool randomRoles, Rng rng, in ContestRules r)
        {
            bool pa = a == ContestKind.Prober && LabRandom.Permille(rng, r.ProbePermille);
            bool pb = b == ContestKind.Prober && LabRandom.Permille(rng, r.ProbePermille);
            Role ra = Role.None, rb = Role.None;
            if (randomRoles)
            {
                bool aRes = rng.NextInt(2) == 0;
                ra = aRes ? Role.Resident : Role.Intruder;
                rb = aRes ? Role.Intruder : Role.Resident;
            }
            bool coin = rng.NextInt(2) == 0;
            return Resolve(a, b, ra, rb, pa, pb, coin, r);
        }

        /// <summary>
        /// The exact expected payoff of A against B, x1000, by enumerating every random input with its probability
        /// (probe decisions at q, roles 1/2 when random, coin 1/2). Exact rational arithmetic in integers.
        /// </summary>
        public static long ExpectedX1000(ContestKind a, ContestKind b, bool randomRoles, in ContestRules r)
        {
            long q = r.ProbePermille;
            long sum = 0; // in units of 1 / (1000 * 1000 * 2 * 2) of a payoff point
            for (int ia = 0; ia < 2; ia++)
            for (int ib = 0; ib < 2; ib++)
            {
                long wa = a == ContestKind.Prober ? (ia == 1 ? q : 1000 - q) : (ia == 0 ? 1000 : 0);
                long wb = b == ContestKind.Prober ? (ib == 1 ? q : 1000 - q) : (ib == 0 ? 1000 : 0);
                if (wa == 0 || wb == 0) continue;
                for (int role = 0; role < 2; role++)
                {
                    long wr = randomRoles ? 1 : (role == 0 ? 2 : 0);
                    if (wr == 0) continue;
                    Role ra = !randomRoles ? Role.None : (role == 0 ? Role.Resident : Role.Intruder);
                    Role rb = !randomRoles ? Role.None : (role == 0 ? Role.Intruder : Role.Resident);
                    for (int coin = 0; coin < 2; coin++)
                    {
                        var o = Resolve(a, b, ra, rb, ia == 1, ib == 1, coin == 0, r);
                        sum += wa * wb * wr * o.PayA;
                    }
                }
            }
            // sum / (1000 * 1000 * 2 * 2) payoff points; x1000 → divide by 4000, rounded
            return sum >= 0 ? (sum + 2000) / 4000 : -((-sum + 2000) / 4000);
        }
    }
}
