using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace God.Sim
{
    public sealed class Hq
    {
        /// <summary>Equals the id of the HQ's <see cref="Building"/>.</summary>
        public int Id;
        public Faction Faction;
        public int Corner;
        /// <summary>The node under the HQ: the node a corner sits on.</summary>
        public int Node;
        /// <summary>True while the HQ is only a construction site (P2): it has no build circle and gives no reach yet.</summary>
        public bool Site;
    }

    public sealed class Building
    {
        public int Id;
        public Faction Faction;
        public int Node;
        public BuildingSize Size;
        /// <summary>Placeholder until P2 (building types).</summary>
        public string Kind;
        public int FlagId;
        public int FlagNode;
    }

    public sealed class Flag
    {
        public int Id;
        public Faction Faction;
        public int Node;
        /// <summary>Building this flag belongs to, or 0 for a plain road flag.</summary>
        public int BuildingId;
    }

    /// <summary>A chain of adjacent nodes from flag A to flag B (both end nodes included).</summary>
    public sealed class Road
    {
        public int Id;
        public Faction Faction;
        public int FlagA;
        public int FlagB;
        public int[] Nodes;
        /// <summary>Steps along the road (nodes minus one).</summary>
        [JsonIgnore] public int Length => Nodes.Length - 1;
    }

    /// <summary>What lies on a node. Ids are 0 for none.</summary>
    public readonly struct NodeContent
    {
        public readonly int Node;
        public readonly int Hex;
        public readonly int BuildingId;
        public readonly int FlagId;
        public readonly int RoadId;
        public NodeContent(int node, int hex, int building, int flag, int road)
        { Node = node; Hex = hex; BuildingId = building; FlagId = flag; RoadId = road; }
        public bool Land => Hex >= 0;
        public bool IsFree => BuildingId == 0 && FlagId == 0 && RoadId == 0;
    }

    /// <summary>A build circle: centre and radius in metres, plus its centre node.</summary>
    public readonly struct HqCircle
    {
        public readonly int HqId;
        public readonly Faction Faction;
        public readonly double X, Z, Radius;
        public readonly int CentreNode;
        public HqCircle(int hqId, Faction f, double x, double z, double r, int node)
        { HqId = hqId; Faction = f; X = x; Z = z; Radius = r; CentreNode = node; }
    }

    /// <summary>A route between two flags over built roads.</summary>
    public sealed class FlagRoute
    {
        /// <summary>Flag ids from start to end.</summary>
        public int[] Flags;
        /// <summary>Road ids walked, one fewer than Flags.</summary>
        public int[] Roads;
        /// <summary>Every node walked from the first flag to the last, no repeats at joins.</summary>
        public int[] Nodes;
        /// <summary>Steps walked.</summary>
        public int Length => Nodes.Length - 1;
    }

    /// <summary>
    /// Land, dice and territory state (P1). Plain data plus read-only queries: this is the snapshot API for views.
    /// Mutation happens only through commands (<see cref="TerritoryRules"/>) and the dice system.
    /// </summary>
    public sealed class TerritoryState
    {
        public int Radius;
        public int NextId = 1;

        public List<Hq> Hqs = new List<Hq>();
        public List<Building> Buildings = new List<Building>();
        public List<Flag> Flags = new List<Flag>();
        public List<Road> Roads = new List<Road>();

        /// <summary>Per node: id of the building, flag or road on it, 0 when none.</summary>
        public int[] NodeBuilding;
        public int[] NodeFlag;
        public int[] NodeRoad;

        /// <summary>Piles per hex id (0..PileCap).</summary>
        public int[] Piles;

        /// <summary>Tick of the last roll (0 = none yet), its two dice and total (0 = none yet).</summary>
        public long LastRollTick;
        public int LastDie1, LastDie2, LastTotal;
        public int RollCount;
        /// <summary>Piles dropped because a hex was full.</summary>
        public int WastedPiles;

        [JsonIgnore] public NodeGrid Grid => NodeGrid.Get(Radius);
        [JsonIgnore] public BoardTopology Topology => BoardTopology.Get(Radius);

        public static TerritoryState Create(int radius)
        {
            var t = new TerritoryState { Radius = radius };
            int n = t.Grid.Count;
            t.NodeBuilding = new int[n];
            t.NodeFlag = new int[n];
            t.NodeRoad = new int[n];
            t.Piles = new int[t.Topology.HexCount];
            return t;
        }

        // ---------------- snapshot API ----------------

        public int PilesAt(int hex) => Piles[hex];

        /// <summary>The raw kind a hex piles up on a roll (needs the board for its terrain).</summary>
        public static RawKind PileKind(Board b, int hex) => RawKinds.OfTerrain(b.HexTerrain[hex]);

        public NodeContent NodeAt(int node)
        {
            var g = Grid;
            return new NodeContent(node, g.HexOf(node), NodeBuilding[node], NodeFlag[node], NodeRoad[node]);
        }

        public Hq HqOf(int id) { foreach (var h in Hqs) if (h.Id == id) return h; return null; }
        public Building BuildingById(int id) { foreach (var b in Buildings) if (b.Id == id) return b; return null; }
        public Flag FlagById(int id) { foreach (var f in Flags) if (f.Id == id) return f; return null; }
        public Road RoadById(int id) { foreach (var r in Roads) if (r.Id == id) return r; return null; }

        public Building BuildingAtNode(int node) => NodeBuilding[node] == 0 ? null : BuildingById(NodeBuilding[node]);
        public Flag FlagAtNode(int node) => NodeFlag[node] == 0 ? null : FlagById(NodeFlag[node]);
        public Road RoadAtNode(int node) => NodeRoad[node] == 0 ? null : RoadById(NodeRoad[node]);

        /// <summary>The finished HQs of a faction (an HQ still being built is not listed).</summary>
        public List<Hq> HqsOf(Faction f)
        {
            var l = new List<Hq>();
            foreach (var h in Hqs) if (h.Faction == f && !h.Site) l.Add(h);
            return l;
        }

        /// <summary>The build circle of an HQ.</summary>
        public HqCircle CircleOf(Hq h)
        {
            var g = Grid;
            return new HqCircle(h.Id, h.Faction, g.X(h.Node), g.Z(h.Node), g.CircleRadius, h.Node);
        }

        public List<HqCircle> Circles()
        {
            var l = new List<HqCircle>();
            foreach (var h in Hqs) if (!h.Site) l.Add(CircleOf(h));
            return l;
        }

        /// <summary>A node is buildable for a faction when it is land and inside one of that faction's circles.</summary>
        public bool IsBuildable(Faction f, int node)
        {
            var g = Grid;
            if (!g.IsLand(node)) return false;
            foreach (var h in Hqs) if (h.Faction == f && !h.Site && g.InCircle(h.Node, node)) return true;
            return false;
        }

        /// <summary>Every node buildable for a faction, ascending id.</summary>
        public List<int> BuildableNodes(Faction f)
        {
            var g = Grid;
            var set = new SortedSet<int>();
            foreach (var h in Hqs)
                if (h.Faction == f && !h.Site)
                    foreach (int n in g.CircleNodes(h.Node)) if (g.IsLand(n)) set.Add(n);
            return new List<int>(set);
        }

        /// <summary>True when the corner has a HQ on it or on a corner one hex edge away.</summary>
        public bool HqNearCorner(int corner)
        {
            var topo = Topology;
            foreach (var h in Hqs)
            {
                if (h.Corner == corner) return true;
                foreach (int n in topo.CornerNeighbours(corner)) if (n == h.Corner) return true;
            }
            return false;
        }

        /// <summary>
        /// True when the node touches a Medium building (not its own flag): roads and flags keep clear of a medium
        /// building's six neighbours.
        /// </summary>
        public bool InMediumHalo(int node)
        {
            var g = Grid;
            for (int d = 0; d < 6; d++)
            {
                int nb = g.Neighbour(node, d);
                if (nb < 0 || NodeBuilding[nb] == 0) continue;
                var b = BuildingById(NodeBuilding[nb]);
                if (b != null && b.Size == BuildingSize.Medium && b.FlagNode != node) return true;
            }
            return false;
        }

        /// <summary>True when any of the six neighbours carries a flag.</summary>
        public bool FlagNextTo(int node)
        {
            var g = Grid;
            for (int d = 0; d < 6; d++)
            {
                int nb = g.Neighbour(node, d);
                if (nb >= 0 && NodeFlag[nb] != 0) return true;
            }
            return false;
        }

        /// <summary>
        /// Shortest lattice path for a new road from one node to another (breadth first, six directions in fixed order,
        /// so ties are deterministic). Interior nodes must be free land clear of medium buildings; the ends are not
        /// checked here (use <see cref="TerritoryRules.CheckRoad"/>). Null when there is no way.
        /// </summary>
        public int[] FindRoadPath(int from, int to) => FindRoadPath(from, to, null);

        /// <summary>
        /// As <see cref="FindRoadPath(int,int)"/>, but the path also keeps clear of the nodes in <paramref name="avoid"/>
        /// (the part of a road being laid step by step, so the next stretch cannot cross it). Null avoids nothing.
        /// </summary>
        public int[] FindRoadPath(int from, int to, ICollection<int> avoid)
        {
            var g = Grid;
            if (!g.IsLand(from) || !g.IsLand(to)) return null;
            var prev = new Dictionary<int, int> { [from] = -1 };
            var queue = new Queue<int>();
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                int u = queue.Dequeue();
                if (u == to) break;
                for (int d = 0; d < 6; d++)
                {
                    int v = g.Neighbour(u, d);
                    if (v < 0 || !g.IsLand(v) || prev.ContainsKey(v)) continue;
                    if (v != to && (!NodeAt(v).IsFree || InMediumHalo(v) || (avoid != null && avoid.Contains(v)))) continue;
                    prev[v] = u;
                    queue.Enqueue(v);
                }
            }
            if (!prev.ContainsKey(to)) return null;
            var path = new List<int>();
            for (int c = to; c != -1; c = prev[c]) path.Add(c);
            path.Reverse();
            return path.ToArray();
        }

        /// <summary>
        /// Shortest route over built roads of one faction, in steps walked. Ties are broken deterministically: among
        /// equal-length routes the one reaching each flag from the lower flag id wins, and between parallel roads the
        /// shorter, then the lower road id. Null when there is none.
        /// </summary>
        public FlagRoute RouteBetweenFlags(Faction f, int flagA, int flagB)
        {
            var fa = FlagById(flagA); var fb = FlagById(flagB);
            if (fa == null || fb == null || fa.Faction != f || fb.Faction != f) return null;
            if (flagA == flagB) return new FlagRoute { Flags = new[] { flagA }, Roads = new int[0], Nodes = new[] { fa.Node } };

            int n = Flags.Count;
            var index = new Dictionary<int, int>();
            for (int i = 0; i < n; i++) index[Flags[i].Id] = i;
            var dist = new int[n];
            var prev = new int[n];
            var prevRoad = new int[n];
            var done = new bool[n];
            for (int i = 0; i < n; i++) { dist[i] = int.MaxValue; prev[i] = -1; prevRoad[i] = -1; }
            int src = index[flagA], dst = index[flagB];
            dist[src] = 0;
            while (true)
            {
                int u = -1;
                for (int i = 0; i < n; i++)
                    if (!done[i] && dist[i] != int.MaxValue && (u < 0 || dist[i] < dist[u] || (dist[i] == dist[u] && Flags[i].Id < Flags[u].Id))) u = i;
                if (u < 0 || u == dst) break;
                done[u] = true;
                for (int r = 0; r < Roads.Count; r++)
                {
                    var road = Roads[r];
                    if (road.Faction != f) continue;
                    int other;
                    if (road.FlagA == Flags[u].Id) other = road.FlagB;
                    else if (road.FlagB == Flags[u].Id) other = road.FlagA;
                    else continue;
                    int v = index[other];
                    if (done[v]) continue;
                    int nd = dist[u] + road.Length;
                    bool better = nd < dist[v]
                        || (nd == dist[v] && Flags[u].Id < Flags[prev[v]].Id)
                        || (nd == dist[v] && Flags[u].Id == Flags[prev[v]].Id && road.Length < RoadById(prevRoad[v]).Length)
                        || (nd == dist[v] && Flags[u].Id == Flags[prev[v]].Id && road.Length == RoadById(prevRoad[v]).Length && road.Id < prevRoad[v]);
                    if (better) { dist[v] = nd; prev[v] = u; prevRoad[v] = road.Id; }
                }
            }
            if (dist[dst] == int.MaxValue) return null;

            var flags = new List<int>();
            var roads = new List<int>();
            for (int c = dst; c != -1; c = prev[c])
            {
                flags.Add(Flags[c].Id);
                if (prev[c] != -1) roads.Add(prevRoad[c]);
            }
            flags.Reverse(); roads.Reverse();
            var nodes = new List<int>();
            for (int k = 0; k < roads.Count; k++)
            {
                var road = RoadById(roads[k]);
                bool forward = road.FlagA == flags[k];
                int start = k == 0 ? 0 : 1;
                if (forward) for (int i = start; i < road.Nodes.Length; i++) nodes.Add(road.Nodes[i]);
                else for (int i = road.Nodes.Length - 1 - start; i >= 0; i--) nodes.Add(road.Nodes[i]);
            }
            return new FlagRoute { Flags = flags.ToArray(), Roads = roads.ToArray(), Nodes = nodes.ToArray() };
        }
    }
}
