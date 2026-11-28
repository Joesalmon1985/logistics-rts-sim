namespace God.Sim.Unified
{
    /// <summary>UN1 (design 2.1): the seven abstract hex types of the unified board. Sea is the ring and every non-land hex of the disc.</summary>
    public enum HexType : byte
    {
        Forest = 1, Mountain = 2, Wetland = 3, Pastures = 4, Fields = 5, Desert = 6, Sea = 7,
    }

    /// <summary>UN1 (design 2.2): the migration from the Stage 3 <see cref="Terrain"/> kinds, and the facts each type carries.</summary>
    public static class HexTypes
    {
        public const int Count = 7;

        /// <summary>The pure migration: Hills become Wetland, Pasture Pastures, the Meadow rim Sea.</summary>
        public static HexType OfTerrain(Terrain t)
        {
            switch (t)
            {
                case Terrain.Forest: return HexType.Forest;
                case Terrain.Mountain: return HexType.Mountain;
                case Terrain.Hills: return HexType.Wetland;
                case Terrain.Fields: return HexType.Fields;
                case Terrain.Pasture: return HexType.Pastures;
                case Terrain.Desert: return HexType.Desert;
                default: return HexType.Sea;
            }
        }

        /// <summary>The Stage 3 terrain a type is stored as in <c>Board.HexTerrain</c>.</summary>
        public static Terrain TerrainOf(HexType h)
        {
            switch (h)
            {
                case HexType.Forest: return Terrain.Forest;
                case HexType.Mountain: return Terrain.Mountain;
                case HexType.Wetland: return Terrain.Hills;
                case HexType.Fields: return Terrain.Fields;
                case HexType.Pastures: return Terrain.Pasture;
                case HexType.Desert: return Terrain.Desert;
                default: return Terrain.Meadow;
            }
        }

        /// <summary>The raw ware a hex of this type yields through the dice allowance and a collection building (Desert: none).</summary>
        public static Ware RawOf(HexType h)
        {
            switch (h)
            {
                case HexType.Forest: return Ware.Log;
                case HexType.Mountain: return Ware.Stone;
                case HexType.Wetland: return Ware.Clay;
                case HexType.Pastures: return Ware.Sheep;
                case HexType.Fields: return Ware.Hay;
                case HexType.Sea: return Ware.Fish;
                default: return Ware.None;
            }
        }

        /// <summary>The forage food of this type (design 3.2).</summary>
        public static Ware ForageOf(HexType h)
        {
            switch (h)
            {
                case HexType.Forest: return Ware.Mushroom;
                case HexType.Mountain: return Ware.Roots;
                case HexType.Wetland: return Ware.WildEgg;
                case HexType.Pastures: return Ware.Rabbit;
                case HexType.Fields: return Ware.WildVeg;
                case HexType.Desert: return Ware.CactusFruit;
                default: return Ware.Seaweed;
            }
        }

        /// <summary>Index of a forage food in the <c>UN1Tuning.Forage*</c> arrays (Mushroom, Roots, WildEgg, Rabbit, WildVeg, CactusFruit, Seaweed, Herbs), or -1.</summary>
        public static int ForageIndex(Ware w)
        {
            switch (w)
            {
                case Ware.Mushroom: return 0; case Ware.Roots: return 1; case Ware.WildEgg: return 2; case Ware.Rabbit: return 3;
                case Ware.WildVeg: return 4; case Ware.CactusFruit: return 5; case Ware.Seaweed: return 6; case Ware.Herbs: return 7;
                default: return -1;
            }
        }
    }
}
