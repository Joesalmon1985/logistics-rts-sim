using System;
using System.Collections.Generic;

namespace God.Sim
{
    /// <summary>
    /// The island deal. A six-player Catan island of 30 hexes in rows of 3/4/5/6/5/4/3 (radius 3, <see
    /// cref="BoardShape.SixPlayer"/>): 8 Forest, 6 Hills, 7 Fields, 7 Mountain and 2 Desert, dealt with the seed; the 28
    /// number tokens of the five-to-six-player game (2 and 12 twice; 3 to 6 and 8 to 11 three times) are dealt on the other
    /// hexes, reshuffled until no 6 or 8 touches another. Pure and deterministic from the seed. The numbers stay hidden
    /// from the player: the view shows richness only.
    /// </summary>
    public static partial class BoardGenerator
    {
        /// <summary>Radius of the island's bounding hexagon.</summary>
        public const int IslandRadius = 3;

        /// <summary>Shape of the island inside that radius.</summary>
        public const BoardShape IslandShape = BoardShape.SixPlayer;

        /// <summary>Land hexes of the island.</summary>
        public const int IslandHexCount = 30;

        /// <summary>The island's topology.</summary>
        public static BoardTopology IslandTopology => BoardTopology.Get(IslandRadius, IslandShape);

        /// <summary>Most number reshuffles per deal before giving up on the 6/8 rule (never reached in practice).</summary>
        public const int NumberShuffleLimit = 100000;

        /// <summary>Terrain dealt over the 30 hexes.</summary>
        public static readonly ReadOnlyArray<Terrain> IslandTerrainPool = Pool(
            (Terrain.Forest, 8), (Terrain.Hills, 6), (Terrain.Fields, 7), (Terrain.Mountain, 7), (Terrain.Desert, 2));

        /// <summary>Numbers dealt over the 28 resource hexes: 2 and 12 twice, every other number but 7 three times.</summary>
        public static readonly ReadOnlyArray<int> IslandNumberPool = new ReadOnlyArray<int>(
            2, 2, 3, 3, 3, 4, 4, 4, 5, 5, 5, 6, 6, 6, 8, 8, 8, 9, 9, 9, 10, 10, 10, 11, 11, 11, 12, 12);

        private static ReadOnlyArray<Terrain> Pool(params (Terrain t, int n)[] parts)
        {
            var l = new List<Terrain>();
            foreach (var (t, n) in parts) for (int i = 0; i < n; i++) l.Add(t);
            return new ReadOnlyArray<Terrain>(l);
        }

        /// <summary>Pips of a number (2/12 = 1 ... 6/8 = 5; 0 for anything else).</summary>
        public static int Pips(int number) => number < 2 || number > 12 || number == 7 ? 0 : 6 - Math.Abs(7 - number);

        /// <summary>True for a 6 or an 8.</summary>
        public static bool IsRed(int number) => number == 6 || number == 8;

        /// <summary>
        /// UN1 (R85): the board of a game: the unified 61-hex disc (<see cref="UnifiedBoard"/>) when the tuning asks for a unified game,
        /// else the Stage 3 island exactly as before. The one flagged hunk of this file.
        /// </summary>
        public static Board CreateBoard(ulong seed, SimTuning tuning) =>
            God.Sim.Unified.Unified.Requested(tuning) ? UnifiedBoard.Create(seed, tuning.Unification) : CreateIslandBoard(seed);

        /// <summary>The island for a seed: 30 hexes, terrain and hidden numbers dealt.</summary>
        public static Board CreateIslandBoard(ulong seed)
        {
            var topo = IslandTopology;
            var rng = new Rng(seed).Fork("board.island");
            var b = new Board
            {
                Radius = IslandRadius,
                Shape = IslandShape,
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
            if (numbers.Count != resource.Count) throw new InvalidOperationException("The number pool must cover the resource hexes.");
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
            foreach (int v in topo.Corners)
            {
                if (topo.VertexHexes(v).Count != 3) continue;
                if (b.Shape == BoardShape.UnifiedIsland) // UN1: a corner of the unified disc is a land corner only when all three hexes are land
                {
                    bool sea = false;
                    foreach (int h in topo.VertexHexes(v)) if (b.HexTerrain[h] == Terrain.Meadow) sea = true;
                    if (sea) continue;
                }
                list.Add(v);
            }
            return list;
        }
    }
}
