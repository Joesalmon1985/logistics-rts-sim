using System.Collections.Generic;

namespace God.Sim
{
    /// <summary>
    /// A small battle between two groups on a real island, run by the real military tick (Q4 balancing): a two-faction
    /// game at war, a straight run of open land far from both HQs, each side's melee soldiers in a front line and its
    /// ranged soldiers one lattice step (5 m) behind them (on the front line when the side has no melee). Front lines start <see cref="FrontGapSteps"/> steps apart, so
    /// the ranged soldiers see the enemy front from where they stand. The battle ends when one side has nobody left.
    /// For balancing and tests; the game itself never calls it.
    /// </summary>
    public static class GroupBattle
    {
        public enum Winner { Draw = 0, A = 1, B = 2 }

        public struct Result
        {
            public Winner Winner;
            public int LeftA, LeftB, Ticks;
        }

        /// <summary>Lattice steps between the two front lines (15 m).</summary>
        public const int FrontGapSteps = 3;
        public const int MaxTicks = 3000;
        /// <summary>The battle line keeps at least this far (mm) from every HQ, so nobody turns on a building.</summary>
        public const int ClearOfHqsMm = 45000;

        /// <summary>Fights one battle on the island of <paramref name="seed"/>; the rolls come from that seed too.</summary>
        public static Result Fight(Kit[] a, Kit[] b, ulong seed)
        {
            var sim = GodSim.Create(new GameSetup(seed, GameSetup.NoHuman, 2));
            var s = sim.State; var g = s.Territory.Grid;
            if (!FindLine(s, 2 + FrontGapSteps + 2, out int start, out int dir)) return new Result { Winner = Winner.Draw };
            sim.Submit(new DeclareWar(Faction.F0, Faction.F1));
            sim.Step();
            int Along(int k) { int n = start; for (int i = 0; i < k; i++) n = g.Neighbour(n, dir); return n; }
            // A: ranged at step 0, melee at step 1. B: melee at 1 + gap, ranged one step further.
            Place(sim, Faction.F0, a, Along(1), Along(0));
            Place(sim, Faction.F1, b, Along(1 + FrontGapSteps), Along(2 + FrontGapSteps));
            sim.Step();
            var m = s.Military;
            for (int t = 0; t < MaxTicks; t++)
            {
                int na = m.CountUnits(Faction.F0), nb = m.CountUnits(Faction.F1);
                if (na == 0 || nb == 0)
                    return new Result { Winner = na > 0 ? Winner.A : nb > 0 ? Winner.B : Winner.Draw, LeftA = na, LeftB = nb, Ticks = t };
                sim.Step();
                sim.DrainEvents();
            }
            return new Result { Winner = Winner.Draw, LeftA = m.CountUnits(Faction.F0), LeftB = m.CountUnits(Faction.F1), Ticks = MaxTicks };
        }

        private static void Place(GodSim sim, Faction f, Kit[] kits, int meleeNode, int rangedNode)
        {
            // A side with no melee at all stands its ranged soldiers on the front line.
            bool anyMelee = false;
            foreach (var k in kits) if (!Gear.IsRanged(MilitaryTuning.Normalize(k).Weapon)) anyMelee = true;
            if (!anyMelee) rangedNode = meleeNode;
            foreach (var k in kits)
                sim.Submit(DevSpawnUnit.WithKit(f, k, Gear.IsRanged(MilitaryTuning.Normalize(k).Weapon) ? rangedNode : meleeNode));
        }

        /// <summary>A straight run of land nodes, all far from every HQ (lowest start node and direction first).</summary>
        private static bool FindLine(GodState s, int steps, out int start, out int dir)
        {
            var g = s.Territory.Grid;
            var hqs = new List<(int x, int z)>();
            foreach (var h in s.Territory.Hqs) hqs.Add((Pathing.NodeMmX(g, h.Node), Pathing.NodeMmZ(g, h.Node)));
            long clear2 = (long)ClearOfHqsMm * ClearOfHqsMm;
            bool Far(int n)
            {
                int x = Pathing.NodeMmX(g, n), z = Pathing.NodeMmZ(g, n);
                foreach (var h in hqs) if (Pathing.Dist2(x, z, h.x, h.z) < clear2) return false;
                return true;
            }
            for (int n = 0; n < g.Count; n++)
            {
                if (!g.IsLand(n) || !Far(n)) continue;
                for (int d = 0; d < 6; d++)
                {
                    int cur = n; bool ok = true;
                    for (int k = 0; k < steps && ok; k++) { cur = g.Neighbour(cur, d); ok = cur >= 0 && g.IsLand(cur) && Far(cur); }
                    if (ok) { start = n; dir = d; return true; }
                }
            }
            start = -1; dir = 0;
            return false;
        }

        /// <summary>Wins of A, wins of B and draws over <paramref name="count"/> battles on seeds seed, seed+1, ...</summary>
        public static (int aWins, int bWins, int draws, int leftA, int leftB) Series(Kit[] a, Kit[] b, int count, ulong seed)
        {
            int wa = 0, wb = 0, d = 0, la = 0, lb = 0;
            for (int i = 0; i < count; i++)
            {
                var r = Fight(a, b, seed + (ulong)i);
                if (r.Winner == Winner.A) wa++; else if (r.Winner == Winner.B) wb++; else d++;
                la += r.LeftA; lb += r.LeftB;
            }
            return (wa, wb, d, la, lb);
        }
    }
}
