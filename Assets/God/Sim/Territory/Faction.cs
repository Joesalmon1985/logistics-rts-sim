namespace God.Sim
{
    /// <summary>The two sides. A command's <see cref="Command.PlayerId"/> holds the faction as an int.</summary>
    public enum Faction
    {
        Player = 0,
        Enemy = 1,
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
