using System.Collections.Generic;

namespace God.Sim
{
    /// <summary>
    /// The fixed island of the tutorial: a seed where Amber's first HQ area has forest, hills and mountain (log, clay, stone
    /// and ore) and a second HQ can legally stand on a corner that touches fields (grain). A corner touches three hexes at
    /// most, so one area never holds all four terrains: the missing one is "next to the first area", which the tutorial's
    /// second-HQ step reaches. The seed is a constant; <see cref="Find"/> is the deterministic search that chose it (a test
    /// checks they agree).
    /// <para>M0: none of those hexes may carry a 2 or a 12 (<see cref="RareNumber"/>), so the tutorial never waits on the
    /// rarest rolls. The old island (seed 40) had its only mountain on a 2 and played through only with the stage 3 dice
    /// stream; with keyed dice streams its learner starved of stone (see the M0 report).</para>
    /// </summary>
    public static class TutorialIsland
    {
        /// <summary>The tutorial seed: the first seed from 1 that <see cref="Qualifies(ulong)"/>.</summary>
        public const ulong Seed = 111;

        /// <summary>A tutorial game setup: Amber is the human, six factions, <see cref="GameSetup.Tutorial"/> set.</summary>
        public static GameSetup Setup() => new GameSetup(Seed, 0) { Tutorial = true, Disease = DiseaseSetting.Mild };

        /// <summary>The terrains the build area of a faction's HQs reaches (the hexes of its buildable nodes).</summary>
        public static HashSet<Terrain> TerrainsInArea(GodState s, Faction f)
        {
            var set = new HashSet<Terrain>();
            var t = s.Territory; var g = t.Grid;
            foreach (int n in t.BuildableNodes(f))
            {
                int hex = g.HexOf(n);
                if (hex >= 0) set.Add(s.Board.HexTerrain[hex]);
            }
            return set;
        }

        /// <summary>The corner of a legal second HQ whose hexes touch the given terrain, or -1 (lowest id first).</summary>
        public static int SecondHqCornerTouching(GodSim sim, Faction f, Terrain terrain)
        {
            var s = sim.State; var topo = s.Territory.Topology;
            for (int c = 0; c < topo.CornerCount; c++)
            {
                bool touches = false;
                foreach (int hex in topo.VertexHexes(c)) if (s.Board.HexTerrain[hex] == terrain) touches = true;
                if (touches && sim.Check(new PlaceHq(f, c)).Ok) return c;
            }
            return -1;
        }

        /// <summary>
        /// First area with forest, hills and mountain, and a legal second HQ corner touching fields; no hex of the first area
        /// and no fields hex of that corner carries a rare number (2 or 12).
        /// </summary>
        public static bool Qualifies(GodSim sim, Faction f)
        {
            var s = sim.State;
            var have = TerrainsInArea(s, f);
            if (!have.Contains(Terrain.Forest) || !have.Contains(Terrain.Hills) || !have.Contains(Terrain.Mountain)) return false;
            var g = s.Territory.Grid;
            foreach (int n in s.Territory.BuildableNodes(f))
            {
                int hex = g.HexOf(n);
                if (hex >= 0 && RareNumber(s.Board.HexNumber[hex])) return false;
            }
            int corner = SecondHqCornerTouching(sim, f, Terrain.Fields);
            if (corner < 0) return false;
            foreach (int hex in s.Territory.Topology.VertexHexes(corner))
                if (s.Board.HexTerrain[hex] == Terrain.Fields && RareNumber(s.Board.HexNumber[hex])) return false;
            return true;
        }

        /// <summary>The two rarest dice totals (1 in 36 each).</summary>
        public static bool RareNumber(int n) => n == 2 || n == 12;

        /// <summary>True when the island dealt from this seed suits the tutorial for Amber.</summary>
        public static bool Qualifies(ulong seed) => Qualifies(GodSim.Create(new GameSetup(seed, 0)), Faction.F0);

        /// <summary>The first qualifying seed from <paramref name="from"/> (at most <paramref name="max"/> tried), or 0.</summary>
        public static ulong Find(ulong from = 1, int max = 400)
        {
            for (ulong i = 0; i < (ulong)max; i++) if (Qualifies(from + i)) return from + i;
            return 0;
        }
    }
}
