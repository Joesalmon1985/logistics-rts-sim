using System;
using System.Collections.Generic;

namespace God.Sim
{
    /// <summary>
    /// Free movement over land on the node lattice. A point is land when its nearest lattice node is land. Straight
    /// lines are tried first; otherwise a breadth-balanced A* over land nodes (unit step cost, ties by node id) gives
    /// waypoints that are then smoothed by line of sight. Integer millimetres in, integer out.
    /// </summary>
    public static class Pathing
    {
        private const int SampleMm = 500;

        public static int NodeMmX(NodeGrid g, int node) => (int)Math.Round(g.X(node) * 1000.0);
        public static int NodeMmZ(NodeGrid g, int node) => (int)Math.Round(g.Z(node) * 1000.0);

        public static long Dist2(int x0, int z0, int x1, int z1)
        {
            long dx = x1 - x0, dz = z1 - z0;
            return dx * dx + dz * dz;
        }

        public static int ISqrt(long v)
        {
            if (v <= 0) return 0;
            long r = (long)Math.Sqrt(v);
            while (r * r > v) r--;
            while ((r + 1) * (r + 1) <= v) r++;
            return (int)r;
        }

        public static int Dist(int x0, int z0, int x1, int z1) => ISqrt(Dist2(x0, z0, x1, z1));

        /// <summary>The lattice node nearest to a point (may be water or outside the grid, then -1).</summary>
        public static int NodeAt(NodeGrid g, int x, int z)
        {
            Lattice.CellOf(x / 1000.0, z / 1000.0, out int i, out int j);
            return g.Id(i, j);
        }

        public static bool IsLandPoint(NodeGrid g, int x, int z) => g.IsLand(NodeAt(g, x, z));

        /// <summary>The land node nearest to a point: its own node when land, else the closest within a few rings (ties: lowest id). -1 when none.</summary>
        public static int NearestLandNode(NodeGrid g, int x, int z)
        {
            int n = NodeAt(g, x, z);
            if (n >= 0 && g.IsLand(n)) return n;
            if (n < 0) return -1;
            int best = -1; long bestD = long.MaxValue;
            int ci = g.I(n), cj = g.J(n);
            const int R = 12;
            for (int di = -R; di <= R; di++)
                for (int dj = -R; dj <= R; dj++)
                {
                    if (Lattice.Distance(0, 0, di, dj) > R) continue;
                    int id = g.Id(ci + di, cj + dj);
                    if (id < 0 || !g.IsLand(id)) continue;
                    long d = Dist2(x, z, NodeMmX(g, id), NodeMmZ(g, id));
                    if (d < bestD || (d == bestD && id < best)) { bestD = d; best = id; }
                }
            return best;
        }

        /// <summary>True when every sampled point on the segment is land.</summary>
        public static bool LineClear(NodeGrid g, int x0, int z0, int x1, int z1)
        {
            int len = Dist(x0, z0, x1, z1);
            int steps = Math.Max(1, (len + SampleMm - 1) / SampleMm);
            for (int s = 0; s <= steps; s++)
            {
                int x = x0 + (int)((long)(x1 - x0) * s / steps);
                int z = z0 + (int)((long)(z1 - z0) * s / steps);
                if (!IsLandPoint(g, x, z)) return false;
            }
            return true;
        }

        /// <summary>A* over land nodes from one node to another; null when unreachable. Deterministic.</summary>
        public static int[] FindNodePath(NodeGrid g, int from, int to)
        {
            if (!g.IsLand(from) || !g.IsLand(to)) return null;
            if (from == to) return new[] { from };
            var gScore = new Dictionary<int, int> { [from] = 0 };
            var prev = new Dictionary<int, int> { [from] = -1 };
            var open = new SortedSet<(int f, int h, int id)>();
            int h0 = g.Distance(from, to);
            open.Add((h0, h0, from));
            var closed = new HashSet<int>();
            while (open.Count > 0)
            {
                var cur = open.Min;
                open.Remove(cur);
                int u = cur.id;
                if (u == to) break;
                if (!closed.Add(u)) continue;
                int gu = gScore[u];
                for (int d = 0; d < 6; d++)
                {
                    int v = g.Neighbour(u, d);
                    if (v < 0 || !g.IsLand(v) || closed.Contains(v)) continue;
                    int ng = gu + 1;
                    if (gScore.TryGetValue(v, out int old) && old <= ng) continue;
                    gScore[v] = ng; prev[v] = u;
                    int h = g.Distance(v, to);
                    open.Add((ng + h, h, v));
                }
            }
            if (!prev.ContainsKey(to)) return null;
            var path = new List<int>();
            for (int c = to; c != -1; c = prev[c]) path.Add(c);
            path.Reverse();
            return path.ToArray();
        }

        /// <summary>Waypoints (node ids) from a point to a goal node, smoothed by line of sight. Null when unreachable.</summary>
        public static int[] FindWaypoints(NodeGrid g, int fromX, int fromZ, int goalNode)
        {
            int start = NearestLandNode(g, fromX, fromZ);
            if (start < 0) return null;
            var raw = FindNodePath(g, start, goalNode);
            if (raw == null) return null;
            var outp = new List<int>();
            int cx = fromX, cz = fromZ;
            int i = 0;
            while (i < raw.Length)
            {
                int j = i;
                for (int k = raw.Length - 1; k > i; k--)
                    if (LineClear(g, cx, cz, NodeMmX(g, raw[k]), NodeMmZ(g, raw[k]))) { j = k; break; }
                outp.Add(raw[j]);
                cx = NodeMmX(g, raw[j]); cz = NodeMmZ(g, raw[j]);
                i = j + 1;
            }
            return outp.ToArray();
        }

        /// <summary>
        /// Moves a unit up to its speed towards a goal point, around water. Returns true when it is at the goal.
        /// Updates position, facing and the path cache.
        /// </summary>
        public static bool MoveToward(NodeGrid g, MilUnit u, int goalX, int goalZ) =>
            MoveToward(g, u, goalX, goalZ, MilitaryTuning.Of(u.Kind).SpeedMmPerTick);

        /// <summary>As <see cref="MoveToward(NodeGrid, MilUnit, int, int)"/> with an explicit speed in mm per tick (envoys walk on a body that is not a unit).</summary>
        public static bool MoveToward(NodeGrid g, MilUnit u, int goalX, int goalZ, int speed)
        {
            if (u.X == goalX && u.Z == goalZ) return true;
            int tx, tz;
            if (LineClear(g, u.X, u.Z, goalX, goalZ))
            {
                u.Path = null; u.PathGoalNode = -1;
                tx = goalX; tz = goalZ;
            }
            else
            {
                int goalNode = NearestLandNode(g, goalX, goalZ);
                if (goalNode < 0) return false;
                if (u.Path == null || u.PathGoalNode != goalNode)
                {
                    u.Path = FindWaypoints(g, u.X, u.Z, goalNode);
                    u.PathIdx = 0;
                    u.PathGoalNode = goalNode;
                    if (u.Path == null) return false;
                }
                while (u.PathIdx < u.Path.Length - 1 && Dist(u.X, u.Z, NodeMmX(g, u.Path[u.PathIdx]), NodeMmZ(g, u.Path[u.PathIdx])) <= speed)
                    u.PathIdx++;
                if (u.PathIdx < u.Path.Length)
                {
                    tx = NodeMmX(g, u.Path[u.PathIdx]); tz = NodeMmZ(g, u.Path[u.PathIdx]);
                    if (u.PathIdx == u.Path.Length - 1 && Dist(u.X, u.Z, tx, tz) <= speed)
                    {
                        // Last waypoint reached: finish the way to the goal when it is a straight run, else stop on the node.
                        if (LineClear(g, tx, tz, goalX, goalZ)) { tx = goalX; tz = goalZ; }
                    }
                }
                else { tx = goalX; tz = goalZ; }
            }
            return Step(u, tx, tz, speed) && tx == goalX && tz == goalZ;
        }

        /// <summary>Moves straight towards a point by at most speed. Returns true when it arrived.</summary>
        private static bool Step(MilUnit u, int tx, int tz, int speed)
        {
            long dx = tx - u.X, dz = tz - u.Z;
            int d = ISqrt(dx * dx + dz * dz);
            if (d == 0) return true;
            int nx, nz;
            if (d <= speed) { nx = tx; nz = tz; }
            else { nx = u.X + (int)(dx * speed / d); nz = u.Z + (int)(dz * speed / d); }
            if (nx != u.X || nz != u.Z) { u.FaceX = nx - u.X; u.FaceZ = nz - u.Z; }
            u.X = nx; u.Z = nz;
            return nx == tx && nz == tz;
        }
    }
}
