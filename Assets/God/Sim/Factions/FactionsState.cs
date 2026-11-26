using System.Collections.Generic;

namespace God.Sim
{
    public sealed partial class GodState
    {
        /// <summary>How the game was set up: seed, human faction (or none), faction count. Set by <see cref="GodSim.Create(GameSetup)"/>.</summary>
        public GameSetup Setup;

        /// <summary>War and peace, eliminations and the game result (P7). Null until <see cref="FactionsModule.StartGame"/>.</summary>
        public FactionsState Factions;

        /// <summary>True once <see cref="GameOver"/> has been emitted: the sim stops ticking and refuses commands.</summary>
        public bool IsGameOver => Factions != null && Factions.GameOver;

        /// <summary>Factions in this game (6 unless the setup says otherwise).</summary>
        public int FactionCount => Setup?.FactionCount ?? FactionInfo.Max;
    }

    /// <summary>Why the game ended.</summary>
    public enum GameEndReason
    {
        None = 0,
        /// <summary>A faction reached <see cref="FactionTuning.VictoryPoints"/>.</summary>
        VictoryPoints = 1,
        /// <summary>Every other faction was eliminated.</summary>
        LastStanding = 2,
    }

    /// <summary>The game result as one faction sees it (for the HUD).</summary>
    public enum GameOutcome { Playing = 0, Won = 1, Lost = 2 }

    /// <summary>One line of the standings: VP first, then HQ count, then the faction id.</summary>
    public struct Standing
    {
        public Faction Faction;
        public int VictoryPoints;
        public int Hqs;
        public int Citadels;
        public bool Eliminated;
        /// <summary>1-based place.</summary>
        public int Rank;
        public override string ToString() => Rank + ". " + FactionInfo.Name(Faction) + " " + VictoryPoints + " VP" + (Eliminated ? " (out)" : "");
    }

    /// <summary>
    /// Relations between factions and the game result (P7). Plain data plus read-only queries. Every pair is at peace
    /// at the start. Changed only by <see cref="DeclareWar"/>, <see cref="OfferPeace"/> and the faction system.
    /// Tables are indexed by faction id; pair tables by <c>a * FactionInfo.Max + b</c>.
    /// </summary>
    public sealed class FactionsState
    {
        /// <summary>Symmetric: War[a * Max + b] == War[b * Max + a].</summary>
        public bool[] War = new bool[FactionInfo.Max * FactionInfo.Max];
        /// <summary>Directed: PeaceOffer[from * Max + to] while <c>from</c>'s offer to <c>to</c> stands. Cleared when peace is made or war declared again.</summary>
        public bool[] PeaceOffer = new bool[FactionInfo.Max * FactionInfo.Max];
        public bool[] Eliminated = new bool[FactionInfo.Max];
        /// <summary>Tick each faction was eliminated (0 while in the game).</summary>
        public long[] EliminatedTick = new long[FactionInfo.Max];
        /// <summary>The faction that last captured an HQ of each faction (-1 for none): credited with its elimination.</summary>
        public int[] LastCapturedBy = { -1, -1, -1, -1, -1, -1 };

        public bool GameOver;
        public long GameOverTick;
        /// <summary>Winning faction id, -1 while playing (or when nobody is left).</summary>
        public int Winner = -1;
        public GameEndReason EndReason;
        /// <summary>Standings when the game ended (empty while playing).</summary>
        public List<Standing> FinalStandings = new List<Standing>();

        private static int Pair(Faction a, Faction b) => (int)a * FactionInfo.Max + (int)b;

        // ---------------- snapshot API ----------------

        /// <summary>True when the two factions are at war. A faction is never at war with itself.</summary>
        public bool AtWar(Faction a, Faction b) => a != b && War[Pair(a, b)];

        /// <summary>True while <paramref name="from"/> has offered peace to <paramref name="to"/> and they are still at war.</summary>
        public bool HasOfferedPeace(Faction from, Faction to) => PeaceOffer[Pair(from, to)];

        public bool IsEliminated(Faction f) => Eliminated[(int)f];

        /// <summary>The factions <paramref name="f"/> is at war with, ascending.</summary>
        public List<Faction> Enemies(Faction f)
        {
            var l = new List<Faction>();
            foreach (var o in FactionInfo.All) if (AtWar(f, o)) l.Add(o);
            return l;
        }

        /// <summary>The result for one faction: Playing until the game ends, then Won for the winner and Lost for the rest.</summary>
        public GameOutcome OutcomeFor(Faction f) => !GameOver ? GameOutcome.Playing : Winner == (int)f ? GameOutcome.Won : GameOutcome.Lost;

        // ---------------- internal ----------------

        internal void SetWar(Faction a, Faction b, bool war)
        {
            War[Pair(a, b)] = war; War[Pair(b, a)] = war;
            PeaceOffer[Pair(a, b)] = false; PeaceOffer[Pair(b, a)] = false;
        }

        internal void SetOffer(Faction from, Faction to, bool on) => PeaceOffer[Pair(from, to)] = on;
    }

    // ---------------- events ----------------

    /// <summary><c>By</c> declared war on <c>Against</c>; the pair is at war from this tick.</summary>
    public sealed class WarDeclared : DomainEvent { public Faction By; public Faction Against; }
    /// <summary><c>By</c> offered peace to <c>To</c>. Peace follows once both have offered.</summary>
    public sealed class PeaceOffered : DomainEvent { public Faction By; public Faction To; }
    /// <summary>The pair is at peace again (both offered).</summary>
    public sealed class PeaceMade : DomainEvent { public Faction A; public Faction B; }

    /// <summary>A Citadel upgrade started on a finished HQ: its wares now flow there like a site's.</summary>
    public sealed class CitadelStarted : DomainEvent { public Faction Faction; public int HqId; }
    /// <summary>The HQ is now a Citadel: 2 VP and more health.</summary>
    public sealed class CitadelCompleted : DomainEvent { public Faction Faction; public int HqId; }

    /// <summary>
    /// An HQ or Citadel fell to <c>To</c>'s units and passed to it at half health, with everything listed: the buildings
    /// and sites of its build area (the HQ included), the flags in it and the roads wholly inside it.
    /// </summary>
    public sealed class HqCaptured : DomainEvent
    {
        public int HqId; public Faction From; public Faction To; public bool Citadel;
        public int[] BuildingIds; public int[] FlagIds; public int[] RoadIds;
        /// <summary>Walkers of the old owner removed (and recalled to their home store) because of the capture.</summary>
        public int WalkersRemoved;
    }

    /// <summary>A faction lost its last HQ. <c>By</c> is the faction that captured its last HQ, or -1.</summary>
    public sealed class FactionEliminated : DomainEvent { public Faction Faction; public int By; }

    /// <summary>The game ended: <c>Winner</c> is a faction id (-1 if nobody is left). The sim stops after this tick.</summary>
    public sealed class GameOver : DomainEvent
    {
        public int Winner;
        public GameEndReason Reason;
        public List<Standing> Standings;
    }
}
