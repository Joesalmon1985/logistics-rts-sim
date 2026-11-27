using System;
using System.Collections.Generic;

namespace God.Sim.Population
{
    /// <summary>
    /// Walking paths for NPCs on the node lattice (5 m a node): A* over land nodes with the step cost of the time a step
    /// takes (roads faster), ties by the lower f, then h, then node id, so equal inputs give the same path. Search arrays
    /// are pooled and stamped, so a search allocates only its result. Bounded by an expansion limit; beyond it the path is
    /// the best straight walk over land. Not state: a cache owned by the population system.
    /// </summary>
    public sealed class NodePather
    {
        private readonly NodeGrid _g;
        private readonly int[] _gScore, _prev, _stamp;
        private readonly bool[] _closed;
        private int _gen;
        private readonly List<long> _heap = new List<long>(1024);
        private readonly List<int> _out = new List<int>(128);

        public int Searches, Expanded;

        public NodePather(NodeGrid g)
        {
            _g = g;
            _gScore = new int[g.Count]; _prev = new int[g.Count]; _stamp = new int[g.Count]; _closed = new bool[g.Count];
        }

        /// <summary>A path of nodes after <paramref name="from"/> up to and including <paramref name="to"/>; empty when already there; null when unreachable.</summary>
        public int[] Find(int from, int to, int[] nodeRoad, int roadCost, int offCost, int maxExpand)
        {
            if (from == to) return Array.Empty<int>();
            if (!_g.IsLand(from) || !_g.IsLand(to)) return null;
            Searches++;
            _gen++;
            if (_gen == int.MaxValue) { Array.Clear(_stamp, 0, _stamp.Length); _gen = 1; }
            _heap.Clear();
            int minCost = Math.Min(roadCost, offCost);
            Set(from, 0, -1);
            Push(_g.Distance(from, to) * minCost, _g.Distance(from, to), from);
            int expanded = 0;
            bool found = false;
            while (_heap.Count > 0)
            {
                long top = Pop();
                int u = (int)(top & 0xFFFFFF);
                if (_closed[u] && _stamp[u] == _gen) continue;
                _closed[u] = true;
                if (u == to) { found = true; break; }
                if (++expanded > maxExpand) break;
                int gu = _gScore[u];
                for (int d = 0; d < 6; d++)
                {
                    int v = _g.Neighbour(u, d);
                    if (v < 0 || !_g.IsLand(v)) continue;
                    if (_stamp[v] == _gen && _closed[v]) continue;
                    int step = nodeRoad != null && nodeRoad[v] != 0 ? roadCost : offCost;
                    int ng = gu + step;
                    if (_stamp[v] == _gen && _gScore[v] <= ng) continue;
                    Set(v, ng, u);
                    int h = _g.Distance(v, to);
                    Push(ng + h * minCost, h, v);
                }
            }
            Expanded += expanded;
            if (!found) return Straight(from, to);
            _out.Clear();
            for (int c = to; c != from; c = _prev[c]) _out.Add(c);
            _out.Reverse();
            return _out.ToArray();
        }

        private void Set(int n, int g, int prev)
        {
            if (_stamp[n] != _gen) { _stamp[n] = _gen; _closed[n] = false; }
            _gScore[n] = g; _prev[n] = prev;
        }

        // heap entries: f (20 bits) | h (12 bits) | node (24 bits), smallest first
        private void Push(int f, int h, int node)
        {
            long key = ((long)Math.Min(f, 0xFFFFF) << 36) | ((long)Math.Min(h, 0xFFF) << 24) | (uint)node;
            _heap.Add(key);
            int i = _heap.Count - 1;
            while (i > 0)
            {
                int p = (i - 1) >> 1;
                if (_heap[p] <= key) break;
                _heap[i] = _heap[p]; i = p;
            }
            _heap[i] = key;
        }

        private long Pop()
        {
            long top = _heap[0];
            long last = _heap[_heap.Count - 1];
            _heap.RemoveAt(_heap.Count - 1);
            int n = _heap.Count;
            if (n > 0)
            {
                int i = 0;
                while (true)
                {
                    int l = 2 * i + 1;
                    if (l >= n) break;
                    int r = l + 1;
                    int m = r < n && _heap[r] < _heap[l] ? r : l;
                    if (_heap[m] >= last) break;
                    _heap[i] = _heap[m]; i = m;
                }
                _heap[i] = last;
            }
            return top;
        }

        /// <summary>A greedy straight walk over land (each step to the land neighbour nearest the goal, ties by lower id); stops when stuck.</summary>
        public int[] Straight(int from, int to)
        {
            _out.Clear();
            int cur = from, guard = 0;
            while (cur != to && guard++ < 400)
            {
                int best = -1, bestD = _g.Distance(cur, to);
                for (int d = 0; d < 6; d++)
                {
                    int v = _g.Neighbour(cur, d);
                    if (v < 0 || !_g.IsLand(v)) continue;
                    int dv = _g.Distance(v, to);
                    if (dv < bestD) { bestD = dv; best = v; }
                }
                if (best < 0) break;
                _out.Add(best);
                cur = best;
            }
            return _out.Count == 0 ? null : _out.ToArray();
        }
    }
}
