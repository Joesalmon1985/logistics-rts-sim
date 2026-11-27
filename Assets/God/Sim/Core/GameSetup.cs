using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;

namespace God.Sim
{
    /// <summary>How hard disease bites (POPULATION_DESIGN "Disease"; owner question 3). Unused until the population milestones (M3/M4).</summary>
    public enum DiseaseSetting { On = 0, Mild = 1, Off = 2 }

    /// <summary>
    /// Contracts 2.1: what the person at the seat controls. <see cref="Stage3Faction"/> (default): a faction with the stage 3 HUD
    /// (every existing scenario). <see cref="GodOnly"/>: a god only; every faction is seated by a faction policy, external
    /// faction commands are refused <c>god_only</c>, <c>Divinity.Enabled</c> must be 1 and <c>Human</c> must be <c>NoHuman</c>.
    /// </summary>
    public enum PlayerControl : byte { Stage3Faction = 0, GodOnly = 1 }

    /// <summary>
    /// How a game was set up. Stored in <see cref="GodState.Setup"/>; fixed once the game starts. The human faction is
    /// only a hint for views, the HUD and bots (which faction to leave alone): the sim rules never read it.
    /// </summary>
    public sealed class GameSetup
    {
        /// <summary><see cref="Human"/> value for watch mode: every faction is a bot.</summary>
        public const int NoHuman = -1;

        public ulong Seed;

        /// <summary>Faction id the person plays, 0 by default, or <see cref="NoHuman"/> in watch mode.</summary>
        public int Human;

        /// <summary>Factions in the game, 2 to <see cref="FactionInfo.Max"/> (6 by default). Ids run 0 to FactionCount - 1.</summary>
        public int FactionCount = FactionInfo.Max;

        /// <summary>Stage 3 tutorial game: the AIs leave the human alone (no war on them) until the tutorial ends or <c>FactionTuning</c> says otherwise.</summary>
        public bool Tutorial;

        // ---------------- M0: calendar, life scale, victory ----------------

        /// <summary>Ticks in a sim day (1200 = 2 minutes at 1x). See <see cref="SimCalendar"/>.</summary>
        public int TicksPerDay = SimCalendar.DefaultTicksPerDay;
        /// <summary>Days in a sim year (10).</summary>
        public int DaysPerYear = SimCalendar.DefaultDaysPerYear;
        /// <summary>Life speed in percent (100 default; higher = faster lives). Scales only life-cycle durations.</summary>
        public int LifeScale = SimCalendar.DefaultLifeScale;
        /// <summary>Whether per-day forgetting (social memory, associations) keeps pace with LifeScale. Default false (owner question 1).</summary>
        public bool DecayFollowsLifeScale;
        /// <summary>
        /// M3: the game has a living population (NPCs, wild food, huts). Off: the stage 3 game, unchanged (G6.1). Written to the
        /// setup's JSON only when set, so every stage 3 hash is unchanged.
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool Population;
        /// <summary>Founders per faction when <see cref="Population"/> is on (the owner's first scenario: about 20 in each of 6 factions).</summary>
        [DefaultValue(20)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public int StartNpcsPerFaction = 20;
        /// <summary>Disease setting for the population milestones (unused before M3/M4). On by default; the tutorial uses Mild.</summary>
        public DiseaseSetting Disease = DiseaseSetting.On;
        /// <summary>The faction knowledge view is filled almost completely (stage 4: information separation built, not enforced). Unused before M1.</summary>
        public bool FullInformation = true;
        /// <summary>
        /// What the game is for (Q22, M1): competitive, sandbox, developer, lab or observation. A host setting, not sim state:
        /// it is neither hashed nor saved (a loaded game gets it from its host again), so it never changes a replay.
        /// </summary>
        [JsonIgnore] public Information.ScenarioPurpose Purpose = Information.ScenarioPurpose.Competitive;
        /// <summary>
        /// Whether the debug true-state overlay may be shown (Q22; set through <see cref="Information.TrueStateOverlayRules"/>).
        /// False in competitive play. Like <see cref="Purpose"/> it is neither hashed nor saved.
        /// </summary>
        [JsonIgnore] public bool AllowTrueStateOverlay;
        /// <summary>
        /// The ways to win, checked in order every tick (<see cref="IVictoryCondition"/>). A plain setup keeps the stage 3
        /// rules (10 VP, then last standing) so existing tests keep their meaning; scenario files choose (a sandbox lists
        /// only <see cref="VictoryKind.Sandbox"/>).
        /// </summary>
        public List<VictorySpec> Victory = VictorySpec.Stage3Default();

        // ---------------- F1 (contracts 2.1, 20.5): the player seat. Written only when set, so every older hash is unchanged. ----------------

        /// <summary>What the seat controls (default <see cref="PlayerControl.Stage3Faction"/>).</summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public PlayerControl Control;
        /// <summary>The god whose seat is <c>Human</c> at the start (GodId; 0 = none / watch).</summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int HumanGod;

        /// <summary>The calendar these settings give.</summary>
        [JsonIgnore] public SimCalendar Calendar => SimCalendar.Of(this);

        public GameSetup() { }

        public GameSetup(ulong seed, int human = 0, int factionCount = FactionInfo.Max)
        {
            Seed = seed; Human = human; FactionCount = factionCount;
        }

        /// <summary>Watch mode: six bots, no human.</summary>
        public static GameSetup Watch(ulong seed) => new GameSetup(seed, NoHuman);

        [JsonIgnore] public bool HasHuman => Human != NoHuman;

        /// <summary>The human faction. Only valid when <see cref="HasHuman"/>.</summary>
        [JsonIgnore] public Faction HumanFaction => (Faction)Human;

        /// <summary>True for an id that takes part in this game (0 to FactionCount - 1).</summary>
        public bool InGame(int id) => id >= 0 && id < FactionCount;

        /// <summary>The factions of this game, ascending.</summary>
        [JsonIgnore]
        public Faction[] Factions
        {
            get
            {
                var a = new Faction[FactionCount];
                for (int i = 0; i < a.Length; i++) a[i] = (Faction)i;
                return a;
            }
        }
    }
}
