using System.Collections;
using System.IO;
using System.Linq;
using God.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace God.Game.Tests
{
    public class EconomyViewTests
    {
        /// <summary>
        /// Runs the F2 chain script at speed in the real scene and checks the view shows buildings and walkers while the
        /// sim stays the only thing that mutates state. Also writes a screenshot to LocalData/p2_economy.png (not committed).
        /// </summary>
        [UnityTest]
        public IEnumerator ChainScript_ShowsBuildingsCarriersAndStock()
        {
            yield return SceneManager.LoadSceneAsync("Assets/God/Scenes/Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var boot = GodBootstrap.Instance;
            Assert.IsNotNull(boot.Economy, "the scene builds the economy view");
            boot.Sim.State.Military.WavesEnabled = false; // this test measures the economy view, not the war
            boot.DebugSample();
            boot.Speed = 48f;
            Time.captureFramerate = 30; // fixed 1/30 s per frame, so the sim speed does not depend on how fast the machine renders
            int maxWalkers = 0, maxMarkers = 0;
            int frames = 0;
            while (boot.Sim.State.Tick < 5400 && frames++ < 4000)
            {
                yield return null;
                maxWalkers = Mathf.Max(maxWalkers, boot.Economy.WalkerCount);
                maxMarkers = Mathf.Max(maxMarkers, boot.Economy.BuildingMarkerCount);
            }
            Assert.GreaterOrEqual(boot.Sim.State.Tick, 5400);
            Assert.Greater(maxWalkers, 0, "carriers were drawn");
            Assert.Greater(maxMarkers, 0, "sites or buildings were drawn");
            Assert.Greater(boot.Sim.State.Economy.Buildings.Count(b => b.Built && !b.IsHq), 3);

            Time.captureFramerate = 0;
            // Frame the player's corner for the picture.
            boot.Running = false;
            var cam = boot.GodCamera;
            var s = boot.Sim.State; var g = s.Territory.Grid;
            var pts = s.Economy.Buildings.Where(b => b.Faction == Faction.Player && !b.IsHq).Select(b => new Vector3((float)g.X(b.Node), 0f, (float)g.Z(b.Node))).ToList();
            // Zoom on the densest cluster of buildings.
            var best = pts.OrderByDescending(q => pts.Count(p => (p - q).magnitude < 55f)).First();
            pts = pts.Where(p => (p - best).magnitude < 55f).ToList();
            var c = pts.Aggregate(Vector3.zero, (a, p) => a + p) / pts.Count;
            float spread = pts.Max(p => (p - c).magnitude);
            c += new Vector3(0f, 0f, 10f);
            var dir = new Vector3(0f, Mathf.Sin(IslandCamera.Pitch * Mathf.Deg2Rad), -Mathf.Cos(IslandCamera.Pitch * Mathf.Deg2Rad));
            cam.transform.position = c + dir * (spread * 3.2f + 40f);
            cam.transform.rotation = Quaternion.LookRotation(-dir, Vector3.up);
            for (int i = 0; i < 3; i++) yield return null;
            string dirPath = Path.Combine(Application.dataPath, "..", "LocalData");
            Directory.CreateDirectory(dirPath);
            string file = Path.GetFullPath(Path.Combine(dirPath, "p2_economy.png"));
            // Render the camera into a texture: works in batch mode too, where there is no game view to capture.
            var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
            var prevTarget = cam.targetTexture; var prevActive = RenderTexture.active;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            File.WriteAllBytes(file, tex.EncodeToPNG());
            cam.targetTexture = prevTarget; RenderTexture.active = prevActive;
            Object.Destroy(rt); Object.Destroy(tex);
            Debug.Log("P2 screenshot: " + file + " tick " + s.Tick + " units " + s.Economy.ProducedLog.Count);
        }
    }
}
