using System;
using System.Collections.Generic;
using God.Sim.Unified;

namespace God.Sim
{
    /// <summary>
    /// UN1 (R85, design 2): the unified board. <c>Board.Radius</c> 4 with <see cref="BoardShape.UnifiedIsland"/> is the 61-hex disc: the
    /// 30-hex six-player layout (rows 3/4/5/6/5/4/3) is kept as land and every other hex of the disc is Sea. Other radii take a land
    /// hexagon of radius <c>Radius - 1</c> (one sea ring). The land is typed from <see cref="UN1Tuning"/>'s pool, shuffled from the
    /// seed; tokens 2-12 go to every non-desert hex, sea included (D9.12). A Sea hex owns no lattice node (<see cref="NodeGrid"/>), so
    /// sea is unwalkable and a coast node is a land node with a non-land neighbour. Used only when the game is unified.
    /// </summary>
    public static class UnifiedBoard
    {
        /// <summary>True when the hex is land on a unified board of this radius.</summary>
        public static bool IsLandHex(HexCoord h, int radius)
        {
            if (radius == 4) return BoardTopology.InShape(h.Q, h.R, 3, BoardShape.SixPlayer);
            return h.Ring <= radius - 1;
        }

        /// <summary>The per-type land counts for <paramref name="landHexes"/> hexes: the configured pool, scaled (largest remainder) when its sum differs.</summary>
        public static int[] PoolCounts(UN1Tuning t, int landHexes)
        {
            // order: Forest, Mountain, Wetland, Pastures, Fields, Desert
            int[] w = { Math.Max(0, t.Forests), Math.Max(0, t.Mountains), Math.Max(0, t.Wetlands), Math.Max(0, t.PasturesHexes), Math.Max(0, t.FieldsHexes), Math.Max(0, t.Deserts) };
            int sum = 0; foreach (int x in w) sum += x;
            if (sum == landHexes) return w;
            if (sum == 0) throw new InvalidOperationException("The land pool is empty.");
            var c = new int[6]; var rem = new long[6]; int used = 0;
            for (int i = 0; i < 6; i++) { long num = (long)w[i] * landHexes; c[i] = (int)(num / sum); rem[i] = num % sum; used += c[i]; }
            while (used < landHexes)
            {
                int best = 0;
                for (int i = 1; i < 6; i++) if (rem[i] > rem[best]) best = i;
                c[best]++; rem[best] = -1; used++;
            }
            return c;
        }

        /// <summary>The unified board for a seed: terrain and tokens dealt, every non-land hex Sea (<see cref="Terrain.Meadow"/>).</summary>
        public static Board Create(ulong seed, UnifiedTuning u)
        {
            int radius = u.Board.Radius;
            var topo = BoardTopology.Get(radius, BoardShape.UnifiedIsland);
            var b = new Board { Radius = radius, Shape = BoardShape.UnifiedIsland, HexTerrain = new Terrain[topo.HexCount], HexNumber = new int[topo.HexCount] };
            var land = new List<int>();
            for (int h = 0; h < topo.HexCount; h++) if (IsLandHex(topo.Hex(h), radius)) land.Add(h);
            var counts = PoolCounts(u.UN1, land.Count);
            var types = new List<Terrain>(land.Count);
            Terrain[] order = { Terrain.Forest, Terrain.Mountain, Terrain.Hills, Terrain.Pasture, Terrain.Fields, Terrain.Desert };
            for (int k = 0; k < 6; k++) for (int i = 0; i < counts[k]; i++) types.Add(order[k]);
            new Rng(seed).Fork("board.unified").Shuffle(types);
            for (int i = 0; i < land.Count; i++) b.HexTerrain[land[i]] = types[i];
            // tokens: every non-desert hex including sea (D9.12), from a keyed stream; the Stage 3 pool repeated, 6s and 8s kept apart when possible
            var withToken = new List<int>();
            for (int h = 0; h < topo.HexCount; h++) if (b.HexTerrain[h] != Terrain.Desert) withToken.Add(h);
            var rng = new Rng(seed).Fork("unified-numbers");
            var tokens = new List<int>(withToken.Count);
            var block = new List<int>();
            while (tokens.Count < withToken.Count)
            {
                block.Clear(); block.AddRange(BoardGenerator.IslandNumberPool);
                rng.Shuffle(block);
                for (int i = 0; i < block.Count && tokens.Count < withToken.Count; i++) tokens.Add(block[i]);
            }
            rng.Shuffle(tokens);
            for (int i = 0; i < withToken.Count; i++) b.HexNumber[withToken[i]] = tokens[i];
            // repair: swap tokens (a swap is kept when it does not raise the count of touching 6s and 8s) until none touch; bounded
            int bad = RedPairs(b, topo), tries = 0;
            while (bad > 0 && tries < 200000)
            {
                tries++;
                int x = withToken[rng.NextInt(withToken.Count)], y = withToken[rng.NextInt(withToken.Count)];
                if (b.HexNumber[x] == b.HexNumber[y]) continue;
                int t0 = b.HexNumber[x]; b.HexNumber[x] = b.HexNumber[y]; b.HexNumber[y] = t0;
                int nb = RedPairs(b, topo);
                if (nb <= bad) bad = nb; else { t0 = b.HexNumber[x]; b.HexNumber[x] = b.HexNumber[y]; b.HexNumber[y] = t0; }
            }
            b.DealTriesUsed = tries + 1;
            return b;
        }

        /// <summary>Touching pairs of red (6 or 8) tokens.</summary>
        static int RedPairs(Board b, BoardTopology topo)
        {
            int n = 0;
            for (int h = 0; h < topo.HexCount; h++)
            {
                if (!BoardGenerator.IsRed(b.HexNumber[h])) continue;
                var c = topo.Hex(h);
                for (int d = 0; d < 6; d++) { int o = topo.HexIndex(c.Neighbour(d)); if (o > h && BoardGenerator.IsRed(b.HexNumber[o])) n++; }
            }
            return n;
        }

        /// <summary>The <see cref="HexType"/> of every hex, from <see cref="Board.HexTerrain"/>.</summary>
        public static HexType[] TypesOf(Board b)
        {
            var a = new HexType[b.HexTerrain.Length];
            for (int h = 0; h < a.Length; h++) a[h] = HexTypes.OfTerrain(b.HexTerrain[h]);
            return a;
        }

        /// <summary>True for a coast node: a land node with a neighbour that is not land (sea, or off the lattice).</summary>
        public static bool IsCoast(NodeGrid g, int node)
        {
            if (!g.IsLand(node)) return false;
            for (int d = 0; d < 6; d++) { int v = g.Neighbour(node, d); if (v < 0 || !g.IsLand(v)) return true; }
            return false;
        }

        /// <summary>The coast nodes of a land hex (ascending id): the shore nodes of a land hex, or, for a Sea hex, the coast nodes adjacent to it (its shore).</summary>
        public static List<int> ShoreNodes(Board b, NodeGrid g, int hex)
        {
            var topo = b.Topology;
            var list = new List<int>();
            bool sea = HexTypes.OfTerrain(b.HexTerrain[hex]) == HexType.Sea;
            var c = topo.Hex(hex);
            if (!sea)
            {
                for (int n = 0; n < g.Count; n++) if (g.HexOf(n) == hex && IsCoast(g, n)) list.Add(n);
                return list;
            }
            // a Sea hex: coast nodes of neighbouring land hexes that touch this hex's water (a water node of this hex lies next to them)
            var seen = new HashSet<int>();
            int centre = g.HexCentreNode(hex);
            int reach = g.NodesPerHexEdge + 1;
            int ci = g.I(centre), cj = g.J(centre);
            for (int di = -reach; di <= reach; di++)
                for (int dj = -reach; dj <= reach; dj++)
                {
                    int n = g.Id(ci + di, cj + dj);
                    if (n < 0 || !g.IsLand(n) || !IsCoast(g, n) || Lattice.Distance(0, 0, di, dj) > reach) continue;
                    if (seen.Add(n)) list.Add(n);
                }
            list.Sort();
            return list;
        }
    }
}
