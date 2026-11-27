using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>The 30-hex six-player island (P7).</summary>
    public class IslandBoardTests
    {
        private static readonly ulong[] Seeds = { 1, 2, 3, 7, 42, 99, 1234, 20261124 };

        private static IEnumerable<ulong> ManySeeds() { for (ulong s = 1; s <= 60; s++) yield return s; }

        [Test]
        public void Island_Has30Hexes_InRowsOf3To6_WithTheSixPlayerTerrain()
        {
            foreach (ulong seed in ManySeeds())
            {
                var b = BoardGenerator.CreateIslandBoard(seed);
                var topo = b.Topology;
                Assert.AreEqual(30, topo.HexCount);
                Assert.AreEqual(30, b.HexTerrain.Length);
                var rows = Enumerable.Range(0, topo.HexCount).GroupBy(h => topo.Hex(h).R).OrderBy(gr => gr.Key).Select(gr => gr.Count()).ToArray();
                CollectionAssert.AreEqual(new[] { 3, 4, 5, 6, 5, 4, 3 }, rows, "rows of 3/4/5/6/5/4/3");
                Assert.AreEqual(8, b.HexTerrain.Count(t => t == Terrain.Forest));
                Assert.AreEqual(6, b.HexTerrain.Count(t => t == Terrain.Hills));
                Assert.AreEqual(7, b.HexTerrain.Count(t => t == Terrain.Fields));
                Assert.AreEqual(7, b.HexTerrain.Count(t => t == Terrain.Mountain));
                Assert.AreEqual(2, b.HexTerrain.Count(t => t == Terrain.Desert));
            }
        }

        [Test]
        public void Island_RowsAreCentred_AndConnected()
        {
            var topo = BoardGenerator.IslandTopology;
            // Each row is centred on the same x (half a hex east of the origin).
            foreach (var row in Enumerable.Range(0, topo.HexCount).GroupBy(h => topo.Hex(h).R))
            {
                double mid = row.Average(h => topo.HexX(h));
                Assert.AreEqual(BoardTopology.Sqrt3 * 0.5 * BoardTopology.HexEdgeLength, mid, 1e-6, "row " + row.Key);
            }
            // Every hex touches another; the land is one piece.
            var seen = new HashSet<int> { 0 }; var q = new Queue<int>(); q.Enqueue(0);
            while (q.Count > 0)
            {
                int h = q.Dequeue();
                for (int d = 0; d < 6; d++) { int n = topo.HexIndex(topo.Hex(h).Neighbour(d)); if (n >= 0 && seen.Add(n)) q.Enqueue(n); }
            }
            Assert.AreEqual(30, seen.Count);
        }

        [Test]
        public void Numbers_AreThe28Tokens_AndDesertsHaveNone()
        {
            Assert.AreEqual(28, BoardGenerator.IslandNumberPool.Length);
            foreach (ulong seed in ManySeeds())
            {
                var b = BoardGenerator.CreateIslandBoard(seed);
                var dealt = b.HexNumber.Where(n => n != 0).OrderBy(n => n).ToArray();
                CollectionAssert.AreEqual(BoardGenerator.IslandNumberPool.OrderBy(n => n).ToArray(), dealt);
                for (int h = 0; h < 30; h++)
                    if (b.HexTerrain[h] == Terrain.Desert) Assert.AreEqual(0, b.HexNumber[h]);
                    else Assert.That(b.HexNumber[h], Is.InRange(2, 12).And.Not.EqualTo(7));
                Assert.AreEqual(2, BoardGenerator.IslandNumberPool.Count(n => n == 2));
                Assert.AreEqual(3, BoardGenerator.IslandNumberPool.Count(n => n == 6));
            }
        }

        [Test]
        public void RedNumbers_NeverTouch_OverManySeeds()
        {
            foreach (ulong seed in ManySeeds())
            {
                var b = BoardGenerator.CreateIslandBoard(seed);
                Assert.IsTrue(BoardGenerator.RedsApart(b, b.Topology), "seed " + seed);
                Assert.Less(b.DealTriesUsed, BoardGenerator.NumberShuffleLimit);
            }
        }

        [Test]
        public void SameSeed_SameIsland_DifferentSeed_Differs()
        {
            var a = BoardGenerator.CreateIslandBoard(77);
            var c = BoardGenerator.CreateIslandBoard(77);
            CollectionAssert.AreEqual(a.HexTerrain, c.HexTerrain);
            CollectionAssert.AreEqual(a.HexNumber, c.HexNumber);
            var seen = Seeds.Select(s => string.Join(",", BoardGenerator.CreateIslandBoard(s).HexTerrain)).Distinct().Count();
            Assert.Greater(seen, 4, "different seeds should deal different islands");
        }

        [Test]
        public void LandCorners_AreWhereThreeLandHexesMeet()
        {
            var b = BoardGenerator.CreateIslandBoard(5);
            var topo = b.Topology;
            var corners = BoardGenerator.LandCorners(b, topo);
            int expected = topo.Corners.Count(v => topo.VertexHexes(v).Count == 3);
            Assert.AreEqual(expected, corners.Count);
            Assert.AreEqual(42, corners.Count, "a 30-hex 3/4/5/6/5/4/3 island has 42 inner corners");
            foreach (int v in corners) Assert.AreEqual(3, topo.VertexHexes(v).Count);
        }

        [Test]
        public void Pips_FollowTheClassicTable()
        {
            Assert.AreEqual(5, BoardGenerator.Pips(6));
            Assert.AreEqual(5, BoardGenerator.Pips(8));
            Assert.AreEqual(1, BoardGenerator.Pips(2));
            Assert.AreEqual(1, BoardGenerator.Pips(12));
            Assert.AreEqual(0, BoardGenerator.Pips(7));
            Assert.AreEqual(0, BoardGenerator.Pips(0));
        }
    }
}
