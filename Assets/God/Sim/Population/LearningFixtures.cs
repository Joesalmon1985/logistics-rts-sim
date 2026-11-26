using System;
using God.Sim.Learning;

namespace God.Sim.Population
{
    /// <summary>
    /// Controlled learning scenarios (NPC_LEARNING 4.17, section 10): a real game (the whole sim ticks) with a population
    /// but no founders, no automatic wild sources, no AI and no hand gathering of dice piles, so a test places exactly the
    /// NPCs, sources, huts and goods it needs in a quiet spot of the island. Nothing scripts an NPC's choices: the NPC
    /// perceives, decides and learns exactly as on the island.
    /// </summary>
    public static class LearningFixtures
    {
        /// <summary>A quiet game: 2 factions, population on, nothing placed. <paramref name="tune"/> may change the tuning first.</summary>
        public static GodSim Empty(ulong seed, Action<SimTuning> tune = null)
        {
            var t = new SimTuning();
            t.Population.HandGatherPiles = false;
            t.Population.FounderAxePermille = 0;
            t.Ecology.BerriesPerForest = 0; t.Ecology.BerriesPerMeadow = 0; t.Ecology.RootsPerHills = 0; t.Ecology.RootsPerFields = 0;
            t.Ecology.WoodPerForest = 0; t.Ecology.FishPerCoast = 0; t.Ecology.NightshadePerForest = 0;
            tune?.Invoke(t);
            var setup = new GameSetup(seed, GameSetup.NoHuman, 2) { Population = true, StartNpcsPerFaction = 0, Victory = VictorySpec.SandboxOnly() };
            return GodSim.Create(setup, t);
        }

        /// <summary>
        /// The centre of a "glade": a land hex far from every HQ (the hex centre farthest from the nearest HQ), with all land
        /// around it to at least <paramref name="radius"/> nodes. The same board always gives the same glade.
        /// </summary>
        public static int Glade(GodSim sim, int radius = 8)
        {
            var s = sim.State;
            var g = s.Territory.Grid;
            int best = -1, bestD = -1;
            for (int h = 0; h < s.Territory.Topology.HexCount; h++)
            {
                int c = g.HexCentreNode(h);
                if (!g.IsLand(c) || !AllLand(g, c, radius)) continue;
                int near = int.MaxValue;
                foreach (var hq in s.Territory.Hqs) near = Math.Min(near, g.Distance(hq.Node, c));
                if (near > bestD || near == bestD && c < best) { bestD = near; best = c; }
            }
            if (best < 0) throw new InvalidOperationException("no glade on this board");
            return best;
        }

        public static bool AllLand(NodeGrid g, int centre, int radius)
        {
            int ci = g.I(centre), cj = g.J(centre);
            for (int di = -radius; di <= radius; di++)
                for (int dj = -radius; dj <= radius; dj++)
                {
                    if (Lattice.Distance(0, 0, di, dj) > radius) continue;
                    int n = g.Id(ci + di, cj + dj);
                    if (n < 0 || !g.IsLand(n)) return false;
                    if (PopulationModule.IsCoast(g, n)) return false;
                }
            return true;
        }

        /// <summary>The node <paramref name="steps"/> steps from a node in one of the six lattice directions (land assumed).</summary>
        public static int Step(GodSim sim, int node, int dir, int steps)
        {
            var g = sim.State.Territory.Grid;
            int cur = node;
            for (int k = 0; k < steps; k++) { int v = g.Neighbour(cur, dir); if (v < 0) break; cur = v; }
            return cur;
        }

        /// <summary>A naive or default NPC of faction 0 at a node, in a learning mode, with a neutral nature (all traits 0, rates 1000).</summary>
        public static Npc Npc(GodSim sim, int node, InstinctSet instincts, LearningMode mode, int hunger = 200, int fatigue = 150)
        {
            var s = sim.State;
            var npc = PopulationModule.AddNpc(s, Faction.F0, node, instincts, mode, false);
            PopulationModule.SetNature(npc, new int[Trait.Count], 1000, 1000, 1000);
            npc.Drives[Drive.Hunger] = hunger * 1000;
            npc.Drives[Drive.Fatigue] = fatigue * 1000;
            Array.Copy(npc.Drives, npc.RewardSnap, Drive.Count);
            for (int i = 0; i < npc.Carry.Length; i++) npc.Carry[i] = default;
            return npc;
        }

        /// <summary>A source that never runs out in the scenario's time (a large stock that regrows).</summary>
        public static ResourceSource Bush(GodSim sim, SourceKind kind, int node, int stock = 400, int capacity = 400, int regrow = 400) =>
            PopulationModule.AddSource(sim.State, kind, node, stock, capacity, regrow);
    }
}
