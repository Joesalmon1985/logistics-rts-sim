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
    /// <summary>
    /// Screenshots for the P10 report (start card, menu, watch mode with standings and banners, a Citadel, the game-over card).
    /// They need a game view; the files go to the folder named by <c>P10_SHOTS</c>, else to LocalData (not committed).
    /// </summary>
    public class MenuShots
    {
        private const string Scene = "Assets/God/Scenes/Main.unity";
        private GodBootstrap Boot => GodBootstrap.Instance;

        private static string Dir()
        {
            string dir = System.Environment.GetEnvironmentVariable("P10_SHOTS");
            if (string.IsNullOrEmpty(dir)) dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "LocalData"));
            Directory.CreateDirectory(dir);
            return dir;
        }

        private IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync(Scene, new LoadSceneParameters(LoadSceneMode.Single));
            for (int i = 0; i < 3; i++) yield return null;
        }

        private IEnumerator Shot(string name)
        {
            for (int i = 0; i < 4; i++) yield return null;
            yield return new WaitForEndOfFrame();
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(Dir(), name), tex.EncodeToPNG());
            Object.Destroy(tex);
            Debug.Log("P10 screenshot: " + Path.Combine(Dir(), name));
        }

        [TearDown]
        public void TearDown() { GodBootstrap.SkipStartCard = true; GodBootstrap.PendingSeed = null; GodBootstrap.PendingSetup = null; GodBootstrap.PendingStartAi = false; }

        [UnityTest]
        public IEnumerator Screenshots()
        {
            // 1. The start card.
            GodBootstrap.SkipStartCard = false;
            yield return Load();
            yield return Shot("p10_start.png");

            // 2. The menu.
            GodBootstrap.SkipStartCard = true;
            yield return Load();
            Boot.Running = false;
            Boot.Menu.Open();
            yield return Shot("p10_menu.png");
            Boot.Menu.ShowSave();
            yield return Shot("p10_menu_save.png");

            // 3. Watch mode: six AIs a while into the game, wars on, two Citadels, the whole island in view.
            GodBootstrap.StartNew(Boot.Seed, true);
            yield return Load2();
            var s = Boot.Sim.State; var sim = Boot.Sim; var t = s.Territory;
            Boot.Running = false;
            for (int i = 0; i < 5000; i++) { sim.Step(); if (i % 20 == 0) Boot.ThinkChains(); }
            sim.DrainEvents();
            foreach (var f in new[] { Faction.F1, Faction.F4 }) t.HqsOf(f)[0].Citadel = true; // set-up for the picture
            sim.Submit(new DeclareWar(Faction.F0, Faction.F1)); sim.Submit(new DeclareWar(Faction.F2, Faction.F3));
            sim.Submit(new DeclareWar(Faction.F0, Faction.F4)); sim.Submit(new DeclareWar(Faction.F5, Faction.F1));
            sim.Step(2);
            Boot.PumpEvents();
            Boot.Hud.SetSpeed(4f); Boot.Running = false;
            yield return Shot("p10_standings.png");

            // 4. A Citadel close up, stone style then timber style.
            Boot.Rig.InputEnabled = false;
            yield return new WaitForSeconds(5f); // the toasts fade
            var g = t.Grid;
            var h1 = t.HqsOf(Faction.F1)[0];
            Boot.Rig.FocusOn(new Vector3((float)g.X(h1.Node), 0f, (float)g.Z(h1.Node)), 62f, true);
            yield return Shot("p10_citadel.png");
            var h4 = t.HqsOf(Faction.F4)[0];
            Boot.Rig.FocusOn(new Vector3((float)g.X(h4.Node), 0f, (float)g.Z(h4.Node)), 62f, true);
            yield return Shot("p10_citadel_b.png");
            var h0 = t.HqsOf(Faction.F0)[0];
            Boot.Rig.FocusOn(new Vector3((float)g.X(h0.Node), 0f, (float)g.Z(h0.Node)), 95f, true);
            yield return Shot("p10_hq_and_toasts.png");

            // 5. The game-over card.
            foreach (var h in t.Hqs.ToList())
                if (h.Faction != Faction.F2) MilitaryRules.Destroy(s, s.Military.BuildingById(h.Id), new EventBuffer());
            sim.Step(2);
            Boot.Rig.Reframe(BoardSpace.FrameRadius + 40f, BoardSpace.BoardCentre);
            Assert.IsTrue(s.IsGameOver);
            yield return Shot("p10_gameover.png");
        }

        private IEnumerator Load2()
        {
            for (int i = 0; i < 6; i++) yield return null;
        }
    }
}
