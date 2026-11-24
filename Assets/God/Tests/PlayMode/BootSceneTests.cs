using System.Collections;
using God.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace God.Game.Tests
{
    public class BootSceneTests
    {
        [UnityTest]
        public IEnumerator MainScene_DealsTheIsland_AndShowsItFromTheGodCamera()
        {
            yield return SceneManager.LoadSceneAsync("Assets/God/Scenes/Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;

            var boot = GodBootstrap.Instance;
            Assert.IsNotNull(boot, "the scene has a GodBootstrap");
            Assert.AreEqual(19, boot.Sim.State.Board.Topology.HexCount);
            Assert.IsNotNull(boot.Island, "the island view was built");
            Assert.AreEqual(19, boot.Island.ItemCounts.Length);

            var cam = boot.GodCamera;
            Assert.IsNotNull(cam);
            Assert.AreEqual(IslandCamera.Pitch, cam.transform.eulerAngles.x, 0.5f, "tilted god camera");

            // Every hex centre projects inside the screen.
            var topo = boot.Sim.State.Board.Topology;
            for (int h = 0; h < topo.HexCount; h++)
            {
                var vp = cam.WorldToViewportPoint(BoardSpace.Hex(topo, h));
                Assert.That(vp.x, Is.InRange(0f, 1f), "hex " + h + " x");
                Assert.That(vp.y, Is.InRange(0f, 1f), "hex " + h + " y");
                Assert.Greater(vp.z, 0f);
            }
        }

        [UnityTest]
        public IEnumerator ViewNeverChangesSimState()
        {
            yield return SceneManager.LoadSceneAsync("Assets/God/Scenes/Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            GodBootstrap.Instance.Running = false; // the sim runs by default now; this test is about a still sim
            var board = GodBootstrap.Instance.Sim.State.Board;
            var terrain = (God.Sim.Terrain[])board.HexTerrain.Clone();
            var numbers = (int[])board.HexNumber.Clone();
            long tick = GodBootstrap.Instance.Sim.State.Tick;
            for (int i = 0; i < 10; i++) yield return null;
            CollectionAssert.AreEqual(terrain, board.HexTerrain);
            CollectionAssert.AreEqual(numbers, board.HexNumber);
            Assert.AreEqual(tick, GodBootstrap.Instance.Sim.State.Tick, "the view does not step the sim");
        }
    }
}
