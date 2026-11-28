using System.Collections.Generic;

namespace God.Sim.Unified
{
    /// <summary>
    /// UN1 (spec 5.7): the reach audit. A breadth-first search over land nodes from a node (tests and generators only: <b>never in a decision
    /// path</b>) that reports whether a fresh water point and how many forage types lie within <c>StartReachMaxEdges</c> steps.
    /// </summary>
    public static partial class World
    {
        public struct Reachable
        {
            public bool FreshWater;
            public int ForageTypes;
            public int FreshDistance;
        }

        public static Reachable ReachAudit(GodState s, int from)
        {
            var u = s.Unification; var g = s.Territory.Grid;
            int max = s.Tuning.Unification.UN1.StartReachMaxEdges;
            var res = new Reachable { FreshDistance = -1 };
            var dist = new Dictionary<int, int>();
            var q = new Queue<int>();
            dist[from] = 0; q.Enqueue(from);
            var foods = new HashSet<int>();
            var placeNodes = new Dictionary<int, int>();
            foreach (var p in u.ForagePlaces) placeNodes[p.Node] = p.Ware;
            var freshNodes = new HashSet<int>();
            foreach (var w in u.WaterPoints) if (w.Fresh) freshNodes.Add(w.Node);
            while (q.Count > 0)
            {
                int n = q.Dequeue(); int d = dist[n];
                if (placeNodes.TryGetValue(n, out int ware)) foods.Add(ware);
                if (!res.FreshWater && freshNodes.Contains(n)) { res.FreshWater = true; res.FreshDistance = d; }
                if (d >= max) continue;
                for (int k = 0; k < 6; k++)
                {
                    int v = g.Neighbour(n, k);
                    if (v < 0 || !g.IsLand(v) || dist.ContainsKey(v)) continue;
                    dist[v] = d + 1; q.Enqueue(v);
                }
            }
            res.ForageTypes = foods.Count;
            return res;
        }
    }
}
