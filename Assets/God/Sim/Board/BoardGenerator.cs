using System;
using System.Collections.Generic;

namespace God.Sim
{
    /// <summary>
    /// The island deal. A Catan-shaped island of radius 2 (19 hexes): 5 Forest, 5 Fields, 4 Hills, 4 Mountain and one
    /// Desert, dealt with the seed; the 18 numbers of the classic game (2, 12 once; 3 to 6 and 8 to 11 twice) are dealt
    /// on the other hexes, reshuffled until no 6 or 8 touches another. Pure and deterministic from the seed. The numbers
    /// stay hidden from the player: the view shows richness only.
    /// </summary>
    public static partial class BoardGenerator
    {
        /// <summary>Radius of the island (19 hexes).</summary>
        public const int IslandRadius = 2;

        /// <summary>Most number reshuffles per deal before giving up on the 6/8 rule (never reached in practice).</summary>
        public const int NumberShuffleLimit = 100000;

        /// <summary>Terrain dealt over the 19 hexes.</summary>
        public static readonly Terrain[] IslandTerrainPool =
        {
            Terrain.Forest, Terrain.Forest, Terrain.Forest, Terrain.Forest, Terrain.Forest,
            Terrain.Fields, Terrain.Fields, Terrain.Fields, Terrain.Fields, Terrain.Fields,
            Terrain.Hills, Terrain.Hills, Terrain.Hills, Terrain.Hills,
            Terrain.Mountain, Terrain.Mountain, Terrain.Mountain, Terrain.Mountain,
            Terrain.Desert,
        };

        /// <summary>Numbers dealt over the 18 resource hexes (one 2, one 12, two of every other number but 7).</summary>
        public static readonly int[] IslandNumberPool =
        {
            2, 3, 3, 4, 4, 5, 5, 6, 6, 8, 8, 9, 9, 10, 10, 11, 11, 12,
        };

        /// <summary>Pips of a number (2/12 = 1 ... 6/8 = 5; 0 for anything else).</summary>
        public static int Pips(int number) => number < 2 || number > 12 || number == 7 ? 0 : 6 - Math.Abs(7 - number);

        /// <summary>True for a 6 or an 8.</summary>
        public static bool IsRed(int number) => number == 6 || number == 8;

        /// <summary>The island for a seed: 19 hexes, terrain and hidden numbers dealt.</summary>
        public static Board CreateIslandBoard(ulong seed)
        {
            var topo = BoardTopology.Get(IslandRadius);
            var rng = new Rng(seed).Fork("board.island");
            var b = new Board
            {
                Radius = IslandRadius,
                HexTerrain = new Terrain[topo.HexCount],
                HexNumber = new int[topo.HexCount],
            };

            var terrains = new List<Terrain>(IslandTerrainPool);
            if (terrains.Count != topo.HexCount) throw new InvalidOperationException("The terrain pool must fill the island.");
            rng.Shuffle(terrains);
            for (int h = 0; h < topo.HexCount; h++) b.HexTerrain[h] = terrains[h];

            var resource = new List<int>();
            for (int h = 0; h < topo.HexCount; h++) if (TerrainInfo.IsResource(b.HexTerrain[h])) resource.Add(h);
            var numbers = new List<int>(IslandNumberPool);
            for (int n = 0; n < NumberShuffleLimit; n++)
            {
                rng.Shuffle(numbers);
                for (int i = 0; i < resource.Count; i++) b.HexNumber[resource[i]] = numbers[i];
                b.DealTriesUsed = n + 1;
                if (RedsApart(b, topo)) return b;
            }
            throw new InvalidOperationException("No number deal keeps the 6s and 8s apart.");
        }

        /// <summary>No 6 or 8 next to another 6 or 8.</summary>
        public static bool RedsApart(Board b, BoardTopology topo)
        {
            for (int h = 0; h < topo.HexCount; h++)
            {
                if (!IsRed(b.HexNumber[h])) continue;
                var c = topo.Hex(h);
                for (int d = 0; d < 6; d++)
                {
                    int n = topo.HexIndex(c.Neighbour(d));
                    if (n >= 0 && IsRed(b.HexNumber[n])) return false;
                }
            }
            return true;
        }

        /// <summary>Summed pips of the hexes around a corner.</summary>
        public static int CornerPips(Board b, BoardTopology topo, int corner)
        {
            int sum = 0;
            foreach (int h in topo.VertexHexes(corner)) sum += Pips(b.HexNumber[h]);
            return sum;
        }

        /// <summary>Corners where three land hexes meet (where an HQ may stand).</summary>
        public static List<int> LandCorners(Board b, BoardTopology topo)
        {
            var list = new List<int>();
            foreach (int v in topo.Corners) if (topo.VertexHexes(v).Count == 3) list.Add(v);
            return list;
        }
    }
}
