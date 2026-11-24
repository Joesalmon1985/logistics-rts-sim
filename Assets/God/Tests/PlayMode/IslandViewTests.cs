using System.Collections;
using God.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace God.Game.Tests
{
    /// <summary>P4a: the organic island look is deterministic from the seed and clears its dressing round what is built.</summary>
    public class IslandViewTests
    {
        private GameObject _root;

        [SetUp] public void SetUp() { _root = new GameObject("IslandViewTests"); }
        [TearDown] public void TearDown() { Object.Destroy(_root); }

        [UnityTest]
        public IEnumerator SameSeed_GivesTheSameDressing_AnotherSeedDoesNot()
        {
            const ulong seed = 4242UL;
            var a = IslandView.Create(GodSim.Create(seed).State.Board, seed, null, _root.transform);
            int countA = a.Items.Count; long hashA = a.PlacementHash; var perHexA = (int[])a.ItemCounts.Clone();
            Object.Destroy(a.gameObject);
            yield return null;

            var b = IslandView.Create(GodSim.Create(seed).State.Board, seed, null, _root.transform);
            Assert.Greater(countA, 1000, "the island is dressed");
            Assert.AreEqual(countA, b.Items.Count, "same seed, same item count");
            Assert.AreEqual(hashA, b.PlacementHash, "same seed, same placement");
            CollectionAssert.AreEqual(perHexA, b.ItemCounts);
            Object.Destroy(b.gameObject);
            yield return null;

            const ulong other = 777UL;
            var c = IslandView.Create(GodSim.Create(other).State.Board, other, null, _root.transform);
            Assert.AreNotEqual(hashA, c.PlacementHash, "another seed gives another island");
            yield return null;
        }

        [UnityTest]
        public IEnumerator RicherHexes_AreDressedMoreDensely_AndBigger()
        {
            foreach (ulong seed in new[] { GodBootstrap.DefaultSeed, 4242UL, 777UL })
            {
                var board = GodSim.Create(seed).State.Board;
                var v = IslandView.Create(board, seed, null, _root.transform);
                var topo = board.Topology;
                // Items within 45 m of each hex centre, and their mean size, per hex.
                var near = new int[topo.HexCount]; var main = new int[topo.HexCount]; var size = new float[topo.HexCount];
                foreach (var it in v.Items)
                {
                    var c = BoardSpace.Hex(topo, it.Hex);
                    if (new Vector2(it.Pos.x - c.x, it.Pos.z - c.z).magnitude > 45f) continue;
                    near[it.Hex]++;
                    if (IsMain(it.Kind, board.HexTerrain[it.Hex])) { main[it.Hex]++; size[it.Hex] += it.Scale; }
                }
                // Per terrain, the mean item count of the rich hexes (4-5 pips) over the poor ones (1-2 pips).
                double logSum = 0; int terrains = 0;
                for (int t = 1; t <= 4; t++)
                {
                    float richN = 0, poorN = 0; int rich = 0, poor = 0;
                    for (int h = 0; h < topo.HexCount; h++)
                    {
                        if ((int)board.HexTerrain[h] != t) continue;
                        int p = BoardGenerator.Pips(board.HexNumber[h]);
                        if (p >= 4) { richN += near[h]; rich++; }
                        else if (p <= 2) { poorN += near[h]; poor++; }
                    }
                    if (rich == 0 || poor == 0) continue;
                    float ratio = (richN / rich) / Mathf.Max(1f, poorN / poor);
                    Debug.Log("seed " + seed + " " + (God.Sim.Terrain)t + " rich/poor density " + ratio);
                    Assert.Greater(ratio, 1.05f, "seed " + seed + ": rich " + (God.Sim.Terrain)t + " is denser than poor");
                    logSum += System.Math.Log(ratio); terrains++;
                }
                Assert.Greater(terrains, 0);
                Assert.Greater(System.Math.Exp(logSum / terrains), 1.3, "seed " + seed + ": rich hexes are clearly denser overall");

                // Within each terrain, a hex with 3+ more pips has bigger props on average.
                for (int t = 1; t <= 4; t++)
                    for (int a = 0; a < topo.HexCount; a++)
                        for (int b = 0; b < topo.HexCount; b++)
                        {
                            if ((int)board.HexTerrain[a] != t || (int)board.HexTerrain[b] != t || main[a] < 5 || main[b] < 5) continue;
                            if (BoardGenerator.Pips(board.HexNumber[a]) - BoardGenerator.Pips(board.HexNumber[b]) < 3) continue;
                            Assert.Greater(size[a] / main[a], size[b] / main[b], "seed " + seed + ": hex " + a + " is richer than hex " + b);
                        }
                Object.Destroy(v.gameObject);
                yield return null;
            }
        }

        /// <summary>The props whose size carries the richness: trees, crops, mounds and pits, peaks.</summary>
        private static bool IsMain(IslandItem.Kinds k, God.Sim.Terrain t)
        {
            switch (t)
            {
                case God.Sim.Terrain.Forest: return k == IslandItem.Kinds.Conifer || k == IslandItem.Kinds.Broadleaf;
                case God.Sim.Terrain.Fields: return k == IslandItem.Kinds.Crop;
                case God.Sim.Terrain.Hills: return k == IslandItem.Kinds.Mound || k == IslandItem.Kinds.ClayPit;
                case God.Sim.Terrain.Mountain: return k == IslandItem.Kinds.Peak;
                default: return false;
            }
        }

        [UnityTest]
        public IEnumerator PlacingABuilding_CullsTheDressingAroundIt()
        {
            const ulong seed = GodBootstrap.DefaultSeed;
            var sim = GodSim.Create(seed);
            var v = IslandView.Create(sim.State.Board, seed, sim, _root.transform);
            var t = sim.State.Territory; var g = t.Grid;

            // A free buildable node with a visible item right on it.
            int node = -1, item = -1;
            foreach (int n in t.BuildableNodes(Faction.Player))
            {
                if (!t.NodeAt(n).IsFree || !sim.Check(new PlaceBuilding(Faction.Player, n, BuildingSize.Small)).Ok) continue;
                float x = (float)g.X(n), z = (float)g.Z(n);
                for (int i = 0; i < v.Items.Count && item < 0; i++)
                {
                    if (v.IsHidden(i)) continue;
                    float dx = v.Items[i].Pos.x - x, dz = v.Items[i].Pos.z - z;
                    if (dx * dx + dz * dz < 2.5f * 2.5f) item = i;
                }
                if (item >= 0) { node = n; break; }
            }
            Assert.GreaterOrEqual(node, 0, "found a buildable node with dressing on it");
            int visibleBefore = v.VisibleCount;
            int refreshes = v.Refreshes;

            Assert.IsTrue(sim.Submit(new PlaceBuilding(Faction.Player, node, BuildingSize.Small)).Ok);
            sim.Step();
            yield return null; // LateUpdate sees the new territory and culls
            yield return null;

            Assert.Greater(v.Refreshes, refreshes, "the view refreshed on the territory change");
            Assert.IsTrue(v.IsHidden(item), "the item under the building is culled");
            Assert.Less(v.VisibleCount, visibleBefore);
            float bx = (float)g.X(node), bz = (float)g.Z(node);
            for (int i = 0; i < v.Items.Count; i++)
            {
                if (v.IsHidden(i)) continue;
                float dx = v.Items[i].Pos.x - bx, dz = v.Items[i].Pos.z - bz;
                Assert.GreaterOrEqual(Mathf.Sqrt(dx * dx + dz * dz), 3.8f, "no visible dressing stands on the building (item " + i + ")");
            }
        }

        [UnityTest]
        public IEnumerator Pulse_GlowsTheHex_AndFadesOut()
        {
            const ulong seed = GodBootstrap.DefaultSeed;
            var v = IslandView.Create(GodSim.Create(seed).State.Board, seed, null, _root.transform);
            HexDressingView.Pulse(new[] { 3, 99 });
            CollectionAssert.AreEquivalent(new[] { 3 }, HexDressingView.PulsingNow, "the old entry point reaches the new view");
            float end = Time.realtimeSinceStartup + IslandView.PulseSeconds + 0.5f;
            while (Time.realtimeSinceStartup < end) yield return null;
            Assert.IsEmpty(v.PulsingNow);
        }
    }
}
