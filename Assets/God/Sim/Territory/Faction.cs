namespace God.Sim
{
    /// <summary>
    /// One of the six factions, by id 0 to 5. A command's <see cref="Command.PlayerId"/> holds the faction as an int.
    /// Who is human is a game setting (<see cref="GameSetup.Human"/>), never a faction id: the sim treats every faction
    /// alike. Names and colours are in <see cref="FactionInfo"/>.
    /// </summary>
    public enum Faction
    {
        F0 = 0,
        F1 = 1,
        F2 = 2,
        F3 = 3,
        F4 = 4,
        F5 = 5,
    }

    /// <summary>Static facts about the factions: count, names, colours.</summary>
    public static class FactionInfo
    {
        /// <summary>Most factions a game can have (the size of per-faction tables).</summary>
        public const int Max = 6;

        /// <summary>Every faction id, ascending.</summary>
        public static readonly ReadOnlyArray<Faction> All = new ReadOnlyArray<Faction>(Faction.F0, Faction.F1, Faction.F2, Faction.F3, Faction.F4, Faction.F5);

        private static readonly ReadOnlyArray<string> Names = new ReadOnlyArray<string>("Amber", "Azure", "Crimson", "Jade", "Violet", "Ivory");

        /// <summary>Colours as 0xRRGGBB, matching <see cref="Name"/>. Views turn them into their own colour type.</summary>
        private static readonly ReadOnlyArray<int> Rgb = new ReadOnlyArray<int>(0xE0A030, 0x3378E8, 0xD23A34, 0x2FA868, 0x8A55C8, 0xE8E2D0);

        public static bool IsValid(int id) => id >= 0 && id < Max;

        public static string Name(Faction f) => IsValid((int)f) ? Names[(int)f] : "None";

        /// <summary>The faction colour as 0xRRGGBB.</summary>
        public static int ColourRgb(Faction f) => IsValid((int)f) ? Rgb[(int)f] : 0x808080;
    }

    /// <summary>Raw goods that a pile holds, by the terrain that produced it. Mountain gives rock: a quarry later turns it into stone, a mine into ore.</summary>
    public enum RawKind
    {
        None = 0,
        Log = 1,
        Clay = 2,
        Grain = 3,
        Rock = 4,
    }

    /// <summary>Size class of a building on the node lattice. Hq stands on a corner node and is placed by <see cref="PlaceHq"/> only.</summary>
    public enum BuildingSize
    {
        Small = 0,
        Medium = 1,
        Hq = 2,
    }

    public static class RawKinds
    {
        /// <summary>The pile a hex of this terrain produces (None for Desert, Meadow and Pasture).</summary>
        public static RawKind OfTerrain(Terrain t)
        {
            switch (t)
            {
                case Terrain.Forest: return RawKind.Log;
                case Terrain.Hills: return RawKind.Clay;
                case Terrain.Fields: return RawKind.Grain;
                case Terrain.Mountain: return RawKind.Rock;
                default: return RawKind.None;
            }
        }
    }
}
