using System.Collections.Generic;

namespace God.Sim.Unified
{
    /// <summary>
    /// UN1 (contracts 23.8, spec 5): the unified world's data and its API. Generated once at the unified start (<see cref="Generate"/>) from
    /// keyed streams, then saved (never regenerated on load): infinite forage places, water points, raw nodes and the per-hex raw books.
    /// Nothing here is an item, a title or a permission. Raw nodes are not lots and cannot be picked up: <see cref="TakeRaw"/> is the only
    /// way a stock falls (UN3b's collection calls it).
    /// </summary>
    public static partial class World
    {
        // ---------------- lookups (ascending ids: binary search) ----------------

        public static ForagePlace ForagePlaceById(UnifiedState u, int id)
        {
            var l = u.ForagePlaces; int lo = 0, hi = l.Count - 1;
            while (lo <= hi) { int m = (lo + hi) >> 1; int v = l[m].Id; if (v == id) return l[m]; if (v < id) lo = m + 1; else hi = m - 1; }
            return null;
        }

        public static WaterPoint WaterPointById(UnifiedState u, int id)
        {
            var l = u.WaterPoints; int lo = 0, hi = l.Count - 1;
            while (lo <= hi) { int m = (lo + hi) >> 1; int v = l[m].Id; if (v == id) return l[m]; if (v < id) lo = m + 1; else hi = m - 1; }
            return null;
        }

        public static RawNode RawNodeById(UnifiedState u, int id)
        {
            var l = u.RawNodes; int lo = 0, hi = l.Count - 1;
            while (lo <= hi) { int m = (lo + hi) >> 1; int v = l[m].Id; if (v == id) return l[m]; if (v < id) lo = m + 1; else hi = m - 1; }
            return null;
        }

        public static HexSupply SupplyOf(UnifiedState u, int hex)
        {
            var l = u.Supply; int lo = 0, hi = l.Count - 1;
            while (lo <= hi) { int m = (lo + hi) >> 1; int v = l[m].Hex; if (v == hex) return l[m]; if (v < hex) lo = m + 1; else hi = m - 1; }
            return null;
        }

        /// <summary>The forage place standing on <paramref name="node"/>, or null (at most one place per node).</summary>
        public static ForagePlace ForageAt(UnifiedState u, int node)
        {
            foreach (var p in u.ForagePlaces) if (p.Node == node) return p;
            return null;
        }

        /// <summary>The water point on <paramref name="node"/>, or null.</summary>
        public static WaterPoint WaterAt(UnifiedState u, int node)
        {
            foreach (var p in u.WaterPoints) if (p.Node == node) return p;
            return null;
        }

        // ---------------- water API (UN3b's Well, UN4's destruction) ----------------

        /// <summary>Adds a water point (a well when <paramref name="building"/> is not 0); ids ascend, so the list stays sorted.</summary>
        public static WaterPoint AddWaterPoint(GodState s, int node, bool fresh, int building = 0)
        {
            var u = s.Unification;
            var w = new WaterPoint { Id = u.NextWaterId++, Node = node, Fresh = fresh, Building = building };
            u.WaterPoints.Add(w);
            return w;
        }

        public static bool RemoveWaterPoint(GodState s, int id)
        {
            var l = s.Unification.WaterPoints;
            for (int i = 0; i < l.Count; i++) if (l[i].Id == id) { l.RemoveAt(i); return true; }
            return false;
        }

        // ---------------- raw API (UN3b's CollectRaw) ----------------

        /// <summary>
        /// Takes up to <paramref name="n"/> units from a raw node (the only way a stock falls): returns how many were taken. The books
        /// (<see cref="HexSupply.CollectedEver"/>) move with it, so <c>Geo-2</c> holds.
        /// </summary>
        public static int TakeRaw(GodState s, int nodeId, int n)
        {
            var u = s.Unification;
            var r = RawNodeById(u, nodeId);
            if (r == null || n <= 0) return 0;
            int take = n < r.Stock ? n : r.Stock;
            if (take <= 0) return 0;
            r.Stock -= take;
            var sup = SupplyOf(u, r.Hex);
            if (sup != null) sup.CollectedEver += take;
            return take;
        }

        // ---------------- generation ----------------

        /// <summary>Picks up to <paramref name="count"/> nodes from <paramref name="cands"/> in a seeded order, at least <paramref name="spacing"/> apart, clear of the hall sites and of nodes already used.</summary>
        static List<int> Pick(NodeGrid g, List<int> cands, Rng rng, int count, int spacing, List<int> halls, int clearance, HashSet<int> used)
        {
            var order = new List<int>(cands);
            rng.Shuffle(order);
            var got = new List<int>();
            foreach (int n in order)
            {
                if (got.Count >= count) break;
                if (used.Contains(n)) continue;
                bool ok = true;
                foreach (int h in halls) if (g.Distance(n, h) <= clearance) { ok = false; break; }
                if (!ok) continue;
                foreach (int o in got) if (g.Distance(n, o) < spacing) { ok = false; break; }
                if (ok) got.Add(n);
            }
            return got;
        }

        /// <summary>
        /// The unified world's start (spec 5): hex types; forage places (one food per hex type, seaweed on coast nodes only; herbs on
        /// Forest and Mountain); water points (every wetland node fresh, one spring per Forest, Mountain, Pastures and Fields hex, salt on
        /// the other coast nodes); raw nodes and the per-hex books. Keyed streams <c>forageplaces</c>, <c>springs</c>, <c>rawnodes</c> per hex.
        /// </summary>
        public static void Generate(GodState s)
        {
            var u = s.Unification; var b = s.Board; var g = s.Territory.Grid; var t = s.Tuning.Unification.UN1;
            int hexes = b.HexTerrain.Length;
            u.HexKinds = UnifiedBoard.TypesOf(b);
            var halls = new List<int>();
            foreach (var hq in s.Territory.Hqs) halls.Add(hq.Node);
            var nodesOf = new List<int>[hexes];
            for (int h = 0; h < hexes; h++) nodesOf[h] = new List<int>();
            for (int n = 0; n < g.Count; n++) { int h = g.HexOf(n); if (h >= 0) nodesOf[h].Add(n); }
            var coast = new bool[g.Count];
            for (int n = 0; n < g.Count; n++) coast[n] = UnifiedBoard.IsCoast(g, n);
            var seaShore = new List<int>[hexes];
            for (int h = 0; h < hexes; h++) if (u.HexKinds[h] == HexType.Sea) seaShore[h] = UnifiedBoard.ShoreNodes(b, g, h);

            var forageUsed = new HashSet<int>();
            // ---- forage places ----
            for (int h = 0; h < hexes; h++)
            {
                var type = u.HexKinds[h];
                var cands = type == HexType.Sea ? seaShore[h] : nodesOf[h];
                if (cands.Count == 0) continue;
                var rng = s.Rng.Fork("forageplaces", h);
                var food = HexTypes.ForageOf(type);
                foreach (int n in Pick(g, cands, rng, t.ForagePlacesPerHex, t.ForageSpacing, halls, t.StartClearance, forageUsed))
                {
                    forageUsed.Add(n);
                    u.ForagePlaces.Add(new ForagePlace { Id = u.NextForageId++, Node = n, Ware = (short)food, Ticks = t.ForageTicks[HexTypes.ForageIndex(food)] });
                }
                if (type == HexType.Forest || type == HexType.Mountain)
                    foreach (int n in Pick(g, cands, rng, t.HerbPlacesPerHex, t.ForageSpacing, halls, t.StartClearance, forageUsed))
                    {
                        forageUsed.Add(n);
                        u.ForagePlaces.Add(new ForagePlace { Id = u.NextForageId++, Node = n, Ware = (short)Ware.Herbs, Ticks = t.ForageTicks[HexTypes.ForageIndex(Ware.Herbs)] });
                    }
            }

            // ---- water points ----
            var hasWater = new bool[g.Count];
            for (int h = 0; h < hexes; h++)
            {
                var type = u.HexKinds[h];
                if (type == HexType.Wetland)
                {
                    foreach (int n in nodesOf[h]) { AddWaterPoint(s, n, true); hasWater[n] = true; }
                }
                else if (type == HexType.Forest || type == HexType.Mountain || type == HexType.Pastures || type == HexType.Fields)
                {
                    var inland = new List<int>();
                    foreach (int n in nodesOf[h]) if (!coast[n]) inland.Add(n);
                    var rng = s.Rng.Fork("springs", h);
                    var one = Pick(g, inland.Count > 0 ? inland : nodesOf[h], rng, 1, 1, halls, t.StartClearance, new HashSet<int>());
                    if (one.Count == 0) one = Pick(g, nodesOf[h], rng, 1, 1, new List<int>(), 0, new HashSet<int>());
                    foreach (int n in one) { AddWaterPoint(s, n, true); hasWater[n] = true; }
                }
            }
            for (int n = 0; n < g.Count; n++) if (coast[n] && !hasWater[n]) AddWaterPoint(s, n, false); // salt: wash only

            // ---- raw nodes and the per-hex books ----
            var rawUsed = new HashSet<int>(forageUsed);
            for (int h = 0; h < hexes; h++)
            {
                var type = u.HexKinds[h];
                var ware = HexTypes.RawOf(type);
                if (ware == Ware.None) continue;
                var cands = type == HexType.Sea ? seaShore[h] : nodesOf[h];
                if (cands.Count == 0) continue;
                var rng = s.Rng.Fork("rawnodes", h);
                var picked = Pick(g, cands, rng, t.NodesPerHex, t.RawNodeSpacing, halls, t.StartClearance, rawUsed);
                if (picked.Count == 0) continue;
                var sup = new HexSupply { Hex = h, Type = (byte)type, NodeFirst = u.NextRawNodeId, NodeCount = picked.Count };
                foreach (int n in picked) { rawUsed.Add(n); u.RawNodes.Add(new RawNode { Id = u.NextRawNodeId++, Hex = h, Node = n, Ware = (short)ware, Stock = 0, Cap = t.RawNodeCap }); }
                u.Supply.Add(sup);
            }
        }
    }
}
