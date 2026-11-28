using System;
using System.Collections.Generic;

namespace God.Sim
{
    /// <summary>
    /// The one global fine lattice of nodes over the island: the triangular <see cref="Lattice"/> (5 m, so 12 nodes per
    /// hex edge and 6 across each build circle radius). Hex corners fall exactly on nodes. Node ids are plain ints,
    /// row-major over a bounding box: id = (i - IMin) * Height + (j - JMin). A node is LAND when it lies inside (or on
    /// the border of) an on-board hex; a node on a shared border belongs to the lowest hex id. Immutable, derived from
    /// the topology, never stored in state: get it with <see cref="Get"/>.
    /// </summary>
    public sealed class NodeGrid
    {
        /// <summary>The six steps, in the order (0,1), (1,0), (1,-1), (0,-1), (-1,0), (-1,1).</summary>
        public static readonly ReadOnlyArray<int> DI = new ReadOnlyArray<int>(0, 1, 1, 0, -1, -1);
        public static readonly ReadOnlyArray<int> DJ = new ReadOnlyArray<int>(1, 0, -1, -1, 0, 1);

        /// <summary>Direction index of the south-east neighbour (X up, Z down): where a building's flag stands.</summary>
        public const int SouthEast = 2;

        private static readonly object Lock = new object();
        private static readonly Dictionary<(int, BoardShape), NodeGrid> Cache = new Dictionary<(int, BoardShape), NodeGrid>();

        public int Radius { get; }
        public BoardShape Shape { get; }
        public BoardTopology Topology { get; }
        public int IMin { get; }
        public int JMin { get; }
        public int Width { get; }
        public int Height { get; }
        public int Count => Width * Height;

        /// <summary>Lattice steps along one hex edge (12).</summary>
        public int NodesPerHexEdge { get; }

        /// <summary>Radius of a build circle in lattice steps (6): half a hex edge.</summary>
        public int CircleRadiusSteps => NodesPerHexEdge / 2;

        /// <summary>Radius of a build circle in metres (30).</summary>
        public double CircleRadius => BoardTopology.HexEdgeLength * 0.5;

        private readonly int[] _hexOf;
        private readonly int[] _cornerNode;
        private readonly int[] _hexCentreNode;

        public static NodeGrid Get(int radius, BoardShape shape = BoardShape.Hexagon)
        {
            lock (Lock)
            {
                if (!Cache.TryGetValue((radius, shape), out var g)) Cache[(radius, shape)] = g = new NodeGrid(radius, shape);
                return g;
            }
        }

        private NodeGrid(int radius, BoardShape shape)
        {
            Radius = radius;
            Shape = shape;
            Topology = BoardTopology.Get(radius, shape);
            double per = BoardTopology.HexEdgeLength / Lattice.Step;
            NodesPerHexEdge = (int)Math.Round(per);
            if (Math.Abs(per - NodesPerHexEdge) > 1e-9 || NodesPerHexEdge % 2 != 0)
                throw new InvalidOperationException("The node spacing must divide the hex edge into an even number of steps.");

            var t = Topology;
            int iLo = int.MaxValue, iHi = int.MinValue, jLo = int.MaxValue, jHi = int.MinValue;
            var cI = new int[t.HexCount];
            var cJ = new int[t.HexCount];
            _hexCentreNode = new int[t.HexCount];
            for (int h = 0; h < t.HexCount; h++)
            {
                Lattice.CellOf(t.HexX(h), t.HexZ(h), out cI[h], out cJ[h]);
                iLo = Math.Min(iLo, cI[h] - NodesPerHexEdge); iHi = Math.Max(iHi, cI[h] + NodesPerHexEdge);
                jLo = Math.Min(jLo, cJ[h] - NodesPerHexEdge); jHi = Math.Max(jHi, cJ[h] + NodesPerHexEdge);
            }
            IMin = iLo - 1; JMin = jLo - 1;
            Width = iHi + 1 - IMin + 1;
            Height = jHi + 1 - JMin + 1;

            _hexOf = new int[Count];
            for (int n = 0; n < _hexOf.Length; n++) _hexOf[n] = -1;
            for (int h = 0; h < t.HexCount; h++)
            {
                _hexCentreNode[h] = Id(cI[h], cJ[h]);
                if (shape == BoardShape.UnifiedIsland && !UnifiedBoard.IsLandHex(t.Hex(h), radius)) continue; // UN1: a Sea hex owns no node (water)
                for (int di = -NodesPerHexEdge; di <= NodesPerHexEdge; di++)
                    for (int dj = -NodesPerHexEdge; dj <= NodesPerHexEdge; dj++)
                    {
                        if (Lattice.Distance(0, 0, di, dj) > NodesPerHexEdge) continue;
                        int id = Id(cI[h] + di, cJ[h] + dj);
                        if (_hexOf[id] < 0) _hexOf[id] = h;
                    }
            }

            _cornerNode = new int[t.CornerCount];
            for (int c = 0; c < t.CornerCount; c++)
            {
                Lattice.CellOf(t.VertexX(c), t.VertexZ(c), out int i, out int j);
                _cornerNode[c] = Id(i, j);
            }
        }

        /// <summary>Node id of lattice cell (i, j), or -1 outside the bounding box.</summary>
        public int Id(int i, int j)
        {
            int a = i - IMin, b = j - JMin;
            if (a < 0 || b < 0 || a >= Width || b >= Height) return -1;
            return a * Height + b;
        }

        public int I(int node) => node / Height + IMin;
        public int J(int node) => node % Height + JMin;
        public double X(int node) => Lattice.X(I(node), J(node));
        public double Z(int node) => Lattice.Z(I(node), J(node));

        public bool IsValid(int node) => node >= 0 && node < Count;

        /// <summary>Hex the node lies in, or -1 (water).</summary>
        public int HexOf(int node) => _hexOf[node];

        public bool IsLand(int node) => node >= 0 && node < _hexOf.Length && _hexOf[node] >= 0;

        /// <summary>The neighbour of a node in direction 0..5, or -1 outside the bounding box. It may be water.</summary>
        public int Neighbour(int node, int dir) => Id(I(node) + DI[dir], J(node) + DJ[dir]);

        /// <summary>The south-east neighbour: where the flag of a building on this node stands.</summary>
        public int FlagNodeOf(int node) => Neighbour(node, SouthEast);

        /// <summary>Steps between two nodes.</summary>
        public int Distance(int a, int b) => Lattice.Distance(I(a), J(a), I(b), J(b));

        public bool Adjacent(int a, int b) => Distance(a, b) == 1;

        /// <summary>The node a hex corner sits on (exact).</summary>
        public int CornerNode(int corner) => _cornerNode[corner];

        public int HexCentreNode(int hex) => _hexCentreNode[hex];

        /// <summary>True when node lies inside or on the build circle centred on the given node.</summary>
        public bool InCircle(int centreNode, int node)
        {
            int di = I(node) - I(centreNode), dj = J(node) - J(centreNode);
            int r = CircleRadiusSteps;
            return di * di + di * dj + dj * dj <= r * r;
        }

        /// <summary>All nodes inside the circle round a centre node, ascending id.</summary>
        public List<int> CircleNodes(int centreNode)
        {
            var list = new List<int>();
            int ci = I(centreNode), cj = J(centreNode), r = CircleRadiusSteps;
            for (int di = -r; di <= r; di++)
                for (int dj = -r; dj <= r; dj++)
                {
                    if (di * di + di * dj + dj * dj > r * r) continue;
                    int id = Id(ci + di, cj + dj);
                    if (id >= 0) list.Add(id);
                }
            list.Sort();
            return list;
        }
    }
}
