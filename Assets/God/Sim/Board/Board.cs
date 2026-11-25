using Newtonsoft.Json;

namespace God.Sim
{
    /// <summary>Shape of the land inside the board radius.</summary>
    public enum BoardShape
    {
        /// <summary>Every hex within the radius (19 hexes at radius 2).</summary>
        Hexagon = 0,
        /// <summary>The six-player Catan island: rows of 3/4/5/6/5/4/3 hexes, 30 in all, at radius 3.</summary>
        SixPlayer = 1,
    }

    /// <summary>
    /// Serialisable board data (lives in GodState.Board). Arrays are indexed by hex id of <see cref="Topology"/>.
    /// Corner ids and hex edge ids are those of the same topology. Occupancy (buildings, roads, units) belongs to the
    /// modules, not here. Topology is derived, never stored.
    /// </summary>
    public sealed class Board
    {
        public int Radius;

        /// <summary>Shape of the land within <see cref="Radius"/>.</summary>
        public BoardShape Shape;

        /// <summary>Terrain per hex id.</summary>
        public Terrain[] HexTerrain;

        /// <summary>
        /// Production number 2..12 (never 7) per hex id; 0 for the desert. Hidden from the player: views show richness
        /// only, never the number.
        /// </summary>
        public int[] HexNumber;

        /// <summary>Number of reshuffles the number deal needed (for tests; 0 when not dealt).</summary>
        public int DealTriesUsed;

        [JsonIgnore]
        public BoardTopology Topology => BoardTopology.Get(Radius, Shape);

        public Terrain TerrainAt(int hex) => HexTerrain[hex];
        public int NumberAt(int hex) => HexNumber[hex];
    }
}
