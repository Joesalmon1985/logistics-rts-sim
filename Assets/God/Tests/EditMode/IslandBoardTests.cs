using System.Linq;
using NUnit.Framework;

namespace God.Sim.Tests
{
    public class IslandBoardTests
    {
        private static readonly ulong[] Seeds = { 1, 2, 3, 7, 42, 99, 1234, 20261124 };

        [Test]
        public void Island_Has19Hexes_AndOneDesert([ValueSource(nameof(Seeds))] ulong seed)
        {
            var b = BoardGenerator.CreateIslandBoard(seed);
            Assert.AreEqual(19, b.Topology.HexCount);
            Assert.AreEqual(19, b.HexTerrain.Length);
            Assert.AreEqual(1, b.HexTerrain.Count(t => t == Terrain.Desert));
            Assert.AreEqual(5, b.HexTerrain.Count(t => t == Terrain.Forest));
            Assert.AreEqual(5, b.HexTerrain.Count(t => t == Terrain.Fields));
            Assert.AreEqual(4, b.HexTerrain.Count(t => t == Terrain.Hills));
            Assert.AreEqual(4, b.HexTerrain.Count(t => t == Terrain.Mountain));
        }

        [Test]
        public void Numbers_AreTheClassicPool_AndDesertHasNone([ValueSource(nameof(Seeds))] ulong seed)
        {
            var b = BoardGenerator.CreateIslandBoard(seed);
            var dealt = b.HexNumber.Where(n => n != 0).OrderBy(n => n).ToArray();
            CollectionAssert.AreEqual(BoardGenerator.IslandNumberPool.OrderBy(n => n).ToArray(), dealt);
            for (int h = 0; h < 19; h++)
                if (b.HexTerrain[h] == Terrain.Desert) Assert.AreEqual(0, b.HexNumber[h]);
                else Assert.That(b.HexNumber[h], Is.InRange(2, 12).And.Not.EqualTo(7));
        }

        [Test]
        public void RedNumbers_NeverTouch([ValueSource(nameof(Seeds))] ulong seed)
        {
            var b = BoardGenerator.CreateIslandBoard(seed);
            Assert.IsTrue(BoardGenerator.RedsApart(b, b.Topology));
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
            var corners = BoardGenerator.LandCorners(b, b.Topology);
            Assert.AreEqual(24, corners.Count, "a 19-hex Catan island has 24 inner corners");
            foreach (int v in corners) Assert.AreEqual(3, b.Topology.VertexHexes(v).Count);
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
