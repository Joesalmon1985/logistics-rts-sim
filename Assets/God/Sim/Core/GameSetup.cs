using Newtonsoft.Json;

namespace God.Sim
{
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
