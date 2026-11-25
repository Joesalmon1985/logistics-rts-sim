using System.Collections;
using System.IO;
using God.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace God.Game.Tests
{
    /// <summary>Screenshots of the stage 3 models and the tutorial HUD. Files go to P_SHOTS or LocalData (not committed).</summary>
    public class Q3Shots
    {
        private const string Scene = "Assets/God/Scenes/Main.unity";

        private static string Dir()
        {
            string dir = System.Environment.GetEnvironmentVariable("Q3_SHOTS");
            if (string.IsNullOrEmpty(dir)) dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "LocalData"));
            Directory.CreateDirectory(dir);
            return dir;
        }

        [TearDown]
        public void TearDown() { GodBootstrap.SkipStartCard = true; GodBootstrap.PendingSeed = null; GodBootstrap.PendingSetup = null; GodBootstrap.PendingStartAi = false; }

        [UnityTest]
        public IEnumerator Gallery_NewProcessorsArmouriesAndHarvesterLevels()
        {
            yield return SceneManager.LoadSceneAsync(Scene, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var boot = GodBootstrap.Instance;
            var cam = boot.GodCamera;
            var root = new GameObject("Q3Gallery");
            var origin = new Vector3(0, 0, -520f);
            void Put(string kind, Faction f, Vector3 p)
            {
                var go = ModelKit.Part(kind, root.transform, BuildingModels.MeshOf(kind, f));
                go.transform.position = p; go.transform.rotation = Quaternion.Euler(0, 160f, 0);
                var rig = BuildingModels.RigOf(kind, f);
                var ban = ModelKit.Part("banner", go.transform, BuildingModels.BannerMesh(f));
                ban.transform.localPosition = rig.BannerTop; ban.transform.localRotation = Quaternion.Euler(0, 180f, 0);
                if (rig.HasWheel) { var w = ModelKit.Part("wheel", go.transform, BuildingModels.WheelMesh(kind, f, rig.WheelR)); w.transform.localPosition = rig.Wheel; }
            }
            for (int i = 0; i < BuildingKinds.Processors.Length; i++) Put(BuildingKinds.Processors[i], Faction.F0, origin + new Vector3((i % 5) * 18f - 36f, 0, -(i / 5) * 18f));
            for (int i = 0; i < BuildingKinds.Armouries.Length; i++) Put(BuildingKinds.Armouries[i], Faction.F0, origin + new Vector3((i % 5) * 18f - 36f, 0, 60f - (i / 5) * 18f));
            for (int i = 0; i < BuildingKinds.Harvesters.Length; i++)
                for (int lv = 1; lv <= 3; lv++) Put(BuildingModels.KindAtLevel(BuildingKinds.Harvesters[i], lv), Faction.F0, origin + new Vector3(i * 18f - 36f, 0, 120f - (lv - 1) * 18f));
            for (int i = 0; i < BuildingKinds.Armouries.Length; i++) Put(BuildingKinds.Armouries[i], Faction.F1, origin + new Vector3(200f + (i % 5) * 18f - 36f, 0, 60f - (i / 5) * 18f));
            for (int i = 0; i < 3; i++) yield return null;
            
            ModelViewTests.Aim(cam, origin + new Vector3(0f, 0, -9f), 70f, 50f);
            yield return null; Shot(cam, "q3_processors.png");
            ModelViewTests.Aim(cam, origin + new Vector3(0f, 0, 51f), 70f, 50f);
            yield return null; Shot(cam, "q3_armouries.png");
            ModelViewTests.Aim(cam, origin + new Vector3(0f, 0, 102f), 75f, 50f);
            yield return null; Shot(cam, "q3_harvester_levels.png");
            ModelViewTests.Aim(cam, origin + new Vector3(200f, 0, 51f), 70f, 50f);
            yield return null; Shot(cam, "q3_armouries_stone.png");
            Object.Destroy(root);
        }

        private static void Shot(Camera cam, string name)
        {
            string file = Path.Combine(Dir(), name);
            var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            var prev = RenderTexture.active; var prevT = cam.targetTexture;
            cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
            var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); tex.Apply();
            File.WriteAllBytes(file, tex.EncodeToPNG());
            cam.targetTexture = prevT; RenderTexture.active = prev;
            Object.Destroy(rt); Object.Destroy(tex);
            Debug.Log("Q3 screenshot: " + file);
        }

        [UnityTest]
        public IEnumerator Hud_TutorialCardAndBuildBar()
        {
            GodBootstrap.SkipStartCard = true;
            yield return SceneManager.LoadSceneAsync(Scene, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            GodBootstrap.Instance.Menu.StartTutorial();
            for (int i = 0; i < 6; i++) yield return null;
            var boot = GodBootstrap.Instance;
            boot.Running = false;
            for (int i = 0; i < 4; i++) yield return null;
            yield return new WaitForEndOfFrame();
            Save("q3_hud_welcome.png");
            boot.Tutorial.Next();
            boot.Hud.SetTab(BuildTab.Process); boot.Running = false;
            for (int i = 0; i < 4; i++) yield return null;
            yield return new WaitForEndOfFrame();
            Save("q3_hud_woodcutter_process_tab.png");
            boot.Hud.SetTab(BuildTab.Gather);
            for (int i = 0; i < 4; i++) yield return null;
            yield return new WaitForEndOfFrame();
            Save("q3_hud_woodcutter.png");
        }

        private static void Save(string name)
        {
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(Dir(), name), tex.EncodeToPNG());
            Object.Destroy(tex);
            Debug.Log("Q3 screenshot: " + Path.Combine(Dir(), name));
        }
    }
}
