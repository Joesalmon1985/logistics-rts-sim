using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>
    /// M0 double audit (HEADLESS 11, conventions 3): the legacy rules that use <c>double</c> are kept, because they use only
    /// + - x / sqrt and rounding, which IEEE 754 makes exact and repeatable on one binary. This test pins their results with a
    /// golden hash, so a run on another runtime (Unity's Mono in EditMode, IL2CPP via the player smoke) or a later change
    /// that alters any of them fails here first. The list of uses is in the M0 report.
    /// </summary>
    public class FloatingPointStabilityTests
    {
        /// <summary>FNV-1a over every double-derived result the rules use (computed under CoreCLR x64, M0).</summary>
        public const ulong Golden = 0x37757BC43F0460FBUL;

        public static ulong Compute()
        {
            var h = new StateHasher();
            foreach (int radius in new[] { 2, 3 })
            {
                var g = NodeGrid.Get(radius);
                var topo = g.Topology;
                // NodeGrid construction (Lattice.CellOf of hex centres and corners) and the land mask it gives.
                h.Add(g.Count); h.Add(g.IMin); h.Add(g.JMin); h.Add(g.NodesPerHexEdge);
                for (int n = 0; n < g.Count; n++) { h.Add(g.HexOf(n)); }
                for (int c = 0; c < topo.CornerCount; c++) h.Add(g.CornerNode(c));
                for (int x = 0; x < topo.HexCount; x++) h.Add(g.HexCentreNode(x));
                // Pathing: node positions in mm and the node at a mm point (Lattice.X/Z, CellOf).
                for (int n = 0; n < g.Count; n += 3)
                {
                    int mx = Pathing.NodeMmX(g, n), mz = Pathing.NodeMmZ(g, n);
                    h.Add(mx); h.Add(mz);
                    h.Add(Pathing.NodeAt(g, mx + 1234, mz - 777));
                    h.Add(Pathing.ISqrt((long)mx * mx + (long)mz * mz));
                }
                // The economy's off-road tie-break (squared distance in double).
                for (int a = 0; a < g.Count; a += 97)
                    for (int b = 0; b < g.Count; b += 131)
                        if (g.IsLand(a) && g.IsLand(b)) foreach (int n in EconomySystem.StraightPath(g, a, b)) h.Add(n);
            }
            // Start corners (the island centre and squared distances in double, then Math.Round).
            for (ulong seed = 1; seed <= 40; seed++)
                foreach (int c in StartCorners.Pick(BoardGenerator.CreateIslandBoard(seed), seed, 6)) h.Add(c);
            return h.Value;
        }

        [Test]
        public void LegacyDoubleRules_GiveTheGoldenResults()
        {
            ulong now = Compute();
            Assert.AreEqual(Compute(), now, "two computations in one run agree");
            Assert.AreEqual(Golden, now, "golden hash of the double-based rules changed: 0x" + now.ToString("X16"));
        }
    }
}
