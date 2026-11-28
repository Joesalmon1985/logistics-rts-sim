using System;
using System.Collections.Generic;
using System.Linq;
using God.Sim.Unified;
using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>UN1 (spec section 13): the unified world. UN1a: the board spike, hex types, sea ring, tokens, coast nodes. OWNER: UN1.</summary>
    public class UN1WorldTests
    {
        internal static GodSim Game(ulong seed, int radius = 4, Action<SimTuning> tune = null, int npcs = 3, int factions = 2)
        {
            var t = new SimTuning();
            t.Unification.Enabled = 1;
            t.Unification.Board.Radius = radius;
            tune?.Invoke(t);
            var setup = new GameSetup(seed, GameSetup.NoHuman, factions) { Population = true, StartNpcsPerFaction = npcs, Victory = VictorySpec.SandboxOnly() };
            return GodSim.Create(setup, t);
        }

        static UnifiedTuning Tun(int radius) { var u = new UnifiedTuning(); u.Enabled = 1; u.Board.Radius = radius; return u; }

        [Test]
        public void Spike_Radius4_Is61HexDisc_30Land()
        {
            var b = UnifiedBoard.Create(1, Tun(4));
            var topo = b.Topology;
            Assert.AreEqual(61, topo.HexCount);
            var g = NodeGrid.Get(4, BoardShape.UnifiedIsland);
            int land = 0, sea = 0;
            for (int h = 0; h < topo.HexCount; h++) { if (b.HexTerrain[h] == Terrain.Meadow) sea++; else land++; }
            Assert.AreEqual(30, land); Assert.AreEqual(31, sea);
            int landNodes = 0, coast = 0;
            for (int n = 0; n < g.Count; n++) if (g.IsLand(n)) { landNodes++; if (UnifiedBoard.IsCoast(g, n)) coast++; }
            var legacy = NodeGrid.Get(3, BoardShape.SixPlayer);
            int legacyLand = 0; for (int n = 0; n < legacy.Count; n++) if (legacy.IsLand(n)) legacyLand++;
            Assert.AreEqual(legacyLand, landNodes, "the same land lattice as the Stage 3 island");
            TestContext.Out.WriteLine("land nodes " + landNodes + ", coast nodes " + coast + ", grid " + g.Count);
        }

        static int[] Counts(Board b)
        {
            var c = new int[8];
            foreach (var t in b.HexTerrain) c[(int)HexTypes.OfTerrain(t)]++;
            return c;
        }

        [TestCase(4)]
        [TestCase(6)]
        public void World_SevenTerrainsAndSeaRing(int radius)
        {
            var u = Tun(radius);
            var b = UnifiedBoard.Create(1, u);
            var topo = b.Topology;
            int hexes = 3 * radius * (radius + 1) + 1;
            Assert.AreEqual(hexes, topo.HexCount, "the whole disc is in the topology");
            var c = Counts(b);
            int land = 0; for (int t = 1; t <= 6; t++) land += c[t];
            Assert.AreEqual(hexes - land, c[(int)HexType.Sea]);
            var want = UnifiedBoard.PoolCounts(u.UN1, land);
            Assert.AreEqual(want[0], c[(int)HexType.Forest]); Assert.AreEqual(want[1], c[(int)HexType.Mountain]);
            Assert.AreEqual(want[2], c[(int)HexType.Wetland]); Assert.AreEqual(want[3], c[(int)HexType.Pastures]);
            Assert.AreEqual(want[4], c[(int)HexType.Fields]); Assert.AreEqual(want[5], c[(int)HexType.Desert]);
            if (radius == 4) { Assert.AreEqual(30, land); Assert.AreEqual(new[] { 5, 5, 5, 6, 6, 3 }, want); }
            // a complete ring: every rim hex is sea; every land hex next to the sea (or the rim) is coastal
            var g = NodeGrid.Get(radius, BoardShape.UnifiedIsland);
            int outerLand = 0;
            for (int h = 0; h < topo.HexCount; h++)
            {
                var hc = topo.Hex(h);
                if (hc.Ring == radius) Assert.AreEqual(Terrain.Meadow, b.HexTerrain[h], "the rim is sea");
                if (b.HexTerrain[h] == Terrain.Meadow) continue;
                bool nextToSea = false;
                for (int d = 0; d < 6; d++) { int n = topo.HexIndex(hc.Neighbour(d)); if (n < 0 || b.HexTerrain[n] == Terrain.Meadow) nextToSea = true; }
                if (nextToSea) { outerLand++; Assert.Greater(UnifiedBoard.ShoreNodes(b, g, h).Count, 0, "land hex " + h + " beside the sea has shore nodes"); }
            }
            Assert.Greater(outerLand, 0);
            // sea nodes are unwalkable, land nodes of a sea-facing hex include coast
            for (int n = 0; n < g.Count; n++)
            {
                if (g.IsLand(n)) Assert.AreNotEqual(Terrain.Meadow, b.HexTerrain[g.HexOf(n)]);
                else Assert.IsFalse(UnifiedBoard.IsCoast(g, n));
            }
            // a Sea hex beside land has shore nodes (the access of seaweed and fish)
            int seaWithShore = 0;
            for (int h = 0; h < topo.HexCount; h++) if (b.HexTerrain[h] == Terrain.Meadow && UnifiedBoard.ShoreNodes(b, g, h).Count > 0) seaWithShore++;
            Assert.Greater(seaWithShore, 0);
            // determinism and variety
            var b2 = UnifiedBoard.Create(1, u);
            Assert.AreEqual(b.HexTerrain, b2.HexTerrain); Assert.AreEqual(b.HexNumber, b2.HexNumber);
            bool differs = false;
            for (ulong sd = 2; sd < 6 && !differs; sd++) differs = !UnifiedBoard.Create(sd, u).HexTerrain.SequenceEqual(b.HexTerrain);
            Assert.IsTrue(differs, "different seeds give different boards");
            TestContext.Out.WriteLine("radius " + radius + ": hexes " + hexes + ", land " + land + ", counts F/M/W/P/Fi/D/Sea " + string.Join("/", c.Skip(1)) + ", outer land hexes " + outerLand);
        }

        [Test]
        public void World_NumbersOnNonDesert()
        {
            foreach (int radius in new[] { 4, 6 })
                for (ulong seed = 1; seed <= 3; seed++)
                {
                    var b = UnifiedBoard.Create(seed, Tun(radius));
                    for (int h = 0; h < b.HexTerrain.Length; h++)
                    {
                        if (b.HexTerrain[h] == Terrain.Desert) Assert.AreEqual(0, b.HexNumber[h]);
                        else { Assert.GreaterOrEqual(b.HexNumber[h], 2); Assert.LessOrEqual(b.HexNumber[h], 12); Assert.AreNotEqual(7, b.HexNumber[h]); }
                    }
                    Assert.IsTrue(BoardGenerator.RedsApart(b, b.Topology), "6s and 8s apart (radius " + radius + ", seed " + seed + ")");
                }
            // Stage 3 untouched with the switch off
            var legacy = Game0(7);
            var island = BoardGenerator.CreateIslandBoard(7);
            Assert.AreEqual(island.HexNumber, legacy.State.Board.HexNumber);
            Assert.AreEqual(island.HexTerrain, legacy.State.Board.HexTerrain);
            Assert.AreEqual(BoardShape.SixPlayer, legacy.State.Board.Shape);
            Assert.IsNull(legacy.State.Unification);
        }

        static GodSim Game0(ulong seed)
        {
            var setup = new GameSetup(seed, GameSetup.NoHuman, 2) { Population = true, StartNpcsPerFaction = 3, Victory = VictorySpec.SandboxOnly() };
            return GodSim.Create(setup, new SimTuning());
        }

        [Test]
        public void World_UnifiedGameStartsAndTicks()
        {
            var sim = Game(1);
            var s = sim.State;
            Assert.AreEqual(4, s.Board.Radius); Assert.AreEqual(BoardShape.UnifiedIsland, s.Board.Shape);
            Assert.AreEqual(61, s.Unification.HexKinds.Length);
            Assert.AreEqual(31, s.Unification.HexKinds.Count(k => k == HexType.Sea));
            Assert.AreEqual(0, s.Population.Sources.Count, "no legacy sources in a unified world");
            for (int i = 0; i < 100; i++) sim.Step();
            var bad = new List<string>(); UnifiedInvariants.Check(s, bad); Assert.AreEqual(0, bad.Count, string.Join(";", bad));
        }

        [Test]
        public void World_StartCornersOnLand_Radius4And6()
        {
            foreach (int radius in new[] { 4, 6 })
            {
                var b = UnifiedBoard.Create(3, Tun(radius));
                var topo = b.Topology;
                var land = BoardGenerator.LandCorners(b, topo);
                var starts = StartCorners.Pick(b, 3, 6);
                var g = NodeGrid.Get(radius, BoardShape.UnifiedIsland);
                var set = new HashSet<int>(land);
                foreach (int c in starts) Assert.IsTrue(set.Contains(c) && g.IsLand(g.CornerNode(c)));
                foreach (int c in starts)
                {
                    int near = land.Count(o => topo.CornerDistance(c, o) <= 2);
                    Assert.GreaterOrEqual(near, 3, "at least three settlement-capable corners at each tribe site (radius " + radius + ")");
                }
                TestContext.Out.WriteLine("radius " + radius + ": land corners " + land.Count + ", 6 starts ok");
            }
        }
    }
}
