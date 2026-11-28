namespace God.Sim.Unified
{
    /// <summary>What a spatial query looks for (flags).</summary>
    [System.Flags]
    public enum SpatialKind : ushort
    {
        None = 0, Npc = 1, Lot = 2, Building = 4, Site = 8, RawNode = 16, ForagePlace = 32, WaterPoint = 64, Fixture = 128, All = 0xFF,
    }

    /// <summary>One result of a spatial query: kind, id, node and lattice distance from the query node.</summary>
    public struct SpatialHit
    {
        public SpatialKind Kind;
        public int Id;
        public int Node;
        public int Distance;
    }

    /// <summary>
    /// UN0 (contracts 23.6, D10.3): the one way the unified encoder and world queries find things near a node, so no caller scans
    /// all pairs. UN2 ships a correct simple implementation; UNP may replace it behind this interface. Results are deterministic:
    /// sorted by distance, then kind, then id (nodes: distance, then id). An index is a cache rebuilt from the state (never saved,
    /// never hashed).
    /// </summary>
    public interface ISpatialIndex
    {
        /// <summary>Rebuilds from the state (call when the state changed since the last query).</summary>
        void Rebuild(GodState s);

        /// <summary>The things of <paramref name="kinds"/> within <paramref name="radius"/> lattice steps of <paramref name="node"/> (a sight radius), nearest first, at most <c>into.Length</c>; returns how many were written.</summary>
        int Nearest(GodState s, int node, int radius, SpatialKind kinds, SpatialHit[] into);

        /// <summary>The nodes within <paramref name="radius"/> of <paramref name="node"/> (itself included), nearest first.</summary>
        int NodesWithin(GodState s, int node, int radius, int[] into);

        /// <summary>The things of <paramref name="kinds"/> at <paramref name="node"/>, by kind then id.</summary>
        int At(GodState s, int node, SpatialKind kinds, SpatialHit[] into);

        /// <summary>True when a body may stand on <paramref name="node"/> (land, on the board).</summary>
        bool Walkable(GodState s, int node);
    }

    /// <summary>
    /// UN0: a correct index of an empty unified world, for compile and for tests of a world with nothing in it: node queries and
    /// walkability are exact (from the territory grid); entity queries find nothing. UN2's <c>SimpleSpatialIndex</c> is the real one.
    /// </summary>
    public sealed class NullSpatialIndex : ISpatialIndex
    {
        public void Rebuild(GodState s) { }

        public int Nearest(GodState s, int node, int radius, SpatialKind kinds, SpatialHit[] into) => 0;

        public int At(GodState s, int node, SpatialKind kinds, SpatialHit[] into) => 0;

        public bool Walkable(GodState s, int node) { var g = s.Territory.Grid; return node >= 0 && node < g.Count && g.IsLand(node); }

        public int NodesWithin(GodState s, int node, int radius, int[] into)
        {
            var g = s.Territory.Grid;
            int n = 0;
            for (int d = 0; d <= radius && n < into.Length; d++)
                for (int v = 0; v < g.Count && n < into.Length; v++)
                    if (g.Distance(node, v) == d) into[n++] = v;
            return n;
        }
    }
}
