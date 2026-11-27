using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using God.Sim;

namespace God.Recording
{
    /// <summary>
    /// The stage 3 smoke judgement (HEADLESS_SIMULATION 9.1), moved out of the Unity <c>SmokeRun</c> so the player and the
    /// runner share it. Fed with the events and the state after every step: it counts wars, captures, eliminations and
    /// trades, tracks each faction's most HQs (every 200 ticks), checks the invariants every 500 ticks and prints the
    /// standings every 1000. At the end the game passes when no exception was seen, the invariants held, and either someone
    /// won or the game was healthy (at least 3 factions expanded, at least one war, at least one capture). Read-only.
    /// </summary>
    public sealed class SmokeVerdict
    {
        public int Exceptions, Wars, Captures, Eliminations, InvariantFailures, Offered, Accepted, Refused, Killed;
        private readonly int[] _maxHqs = new int[FactionInfo.Max];
        private string _invariantNote = "ok";

        /// <summary>The last standings line (every 1000 ticks) and, once finished, the end line.</summary>
        public string LastLine { get; private set; }
        /// <summary>Null while running; the verdict once <see cref="Finish"/> ran.</summary>
        public bool? Ok { get; private set; }
        public string InvariantNote => _invariantNote;

        public void CountException() => Exceptions++;

        public void OnEvents(IEnumerable<SimEvent> events)
        {
            foreach (var ev in events)
            {
                if (ev is WarDeclared) Wars++;
                else if (ev is HqCaptured) Captures++;
                else if (ev is FactionEliminated) Eliminations++;
                else if (ev is TradeOffered) Offered++;
                else if (ev is TradeAccepted) Accepted++;
                else if (ev is TradeRefused) Refused++;
                else if (ev is EnvoyKilled) Killed++;
            }
        }

        /// <summary>
        /// The per-tick bookkeeping after a step. Returns what to log: <paramref name="info"/> is the standings line
        /// ("SMOKE ..."), <paramref name="warning"/> an invariant failure; both null on most ticks.
        /// </summary>
        public void AfterTick(GodState s, out string info, out string warning)
        {
            info = null; warning = null;
            if (s.Tick % 200 == 0) foreach (var f in s.Setup.Factions) _maxHqs[(int)f] = System.Math.Max(_maxHqs[(int)f], s.Territory.HqsOf(f).Count);
            if (s.Tick % 500 == 0) warning = CheckInvariants(s);
            if (s.Tick % 1000 == 0) info = LastLine = "SMOKE " + Summary(s);
        }

        private string CheckInvariants(GodState s)
        {
            var bad = SimInvariants.Check(s);
            if (bad.Count == 0) return null;
            InvariantFailures++;
            _invariantNote = "tick " + s.Tick + ": " + string.Join("; ", bad);
            return "SMOKE invariants " + _invariantNote;
        }

        public string Summary(GodState s)
        {
            var parts = FactionRules.Standings(s).Select(st => FactionInfo.Name(st.Faction) + " " + st.VictoryPoints + " VP " + st.Hqs + " HQ "
                + st.Citadels + " Cit " + s.Military.CountUnits(st.Faction) + " units"
                + (st.Eliminated ? " (out)" : s.Factions.Enemies(st.Faction).Count > 0 ? " war " + string.Join("/", s.Factions.Enemies(st.Faction).Select(FactionInfo.Name)) : ""));
            return "tick " + s.Tick + " min " + (s.Tick / 600.0).ToString("0.0", CultureInfo.InvariantCulture) + " | " + string.Join(" | ", parts)
                + " | soldiers made " + s.Economy.UnitsProduced + " harvester upgrades " + s.Economy.Buildings.Count(b => b.Level > 1)
                + " | wars " + Wars + " captures " + Captures + " eliminations " + Eliminations
                + " | trades offered " + Offered + " accepted " + Accepted + " refused " + Refused + " killed " + Killed;
        }

        /// <summary>
        /// Judges the game. Returns the game-over line ("SMOKE GAME OVER ..." or "SMOKE no game over ..."), an invariant
        /// warning (or null), and sets <see cref="LastLine"/> to the end line and <see cref="Ok"/> to the verdict.
        /// </summary>
        public string Finish(GodState s, out string warning)
        {
            foreach (var f in s.Setup.Factions) _maxHqs[(int)f] = System.Math.Max(_maxHqs[(int)f], s.Territory.HqsOf(f).Count);
            warning = CheckInvariants(s);
            int expanded = s.Setup.Factions.Count(f => _maxHqs[(int)f] >= 2);
            bool won = s.IsGameOver && s.Factions.Winner >= 0;
            bool healthy = expanded >= 3 && Wars >= 1 && Captures >= 1;
            bool ok = Exceptions == 0 && InvariantFailures == 0 && (won || healthy);
            string over = s.IsGameOver
                ? "GAME OVER at tick " + s.Factions.GameOverTick + " (minute " + (s.Factions.GameOverTick / 600.0).ToString("0.0", CultureInfo.InvariantCulture) + "): "
                  + (s.Factions.Winner >= 0 ? FactionInfo.Name((Faction)s.Factions.Winner) : "nobody") + " wins by " + s.Factions.EndReason
                : "no game over by tick " + s.Tick;
            LastLine = "SMOKE END " + Summary(s) + " | " + over + " | factions expanded " + expanded + " | invariants " + _invariantNote
                + " | exceptions " + Exceptions + " | " + (ok ? "OK" : "FAIL");
            Ok = ok;
            return "SMOKE " + over;
        }

        /// <summary>Counters for the run summary (same names as the old SmokeRun's fields).</summary>
        public void AddCounters(IDictionary<string, long> into)
        {
            into["smoke.wars"] = Wars; into["smoke.captures"] = Captures; into["smoke.eliminations"] = Eliminations;
            into["smoke.trades_offered"] = Offered; into["smoke.trades_accepted"] = Accepted; into["smoke.trades_refused"] = Refused;
            into["smoke.envoys_killed"] = Killed; into["smoke.invariant_failures"] = InvariantFailures; into["smoke.exceptions"] = Exceptions;
            into["smoke.ok"] = Ok == true ? 1 : 0;
        }
    }
}
