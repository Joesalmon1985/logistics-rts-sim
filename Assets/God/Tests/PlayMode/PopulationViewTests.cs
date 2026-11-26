using System.Collections;
using System.IO;
using System.Linq;
using God.Sim;
using God.Sim.Learning;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace God.Game.Tests
{
    /// <summary>
    /// M3 views: a living island in the real scene. People are drawn as faction-coloured figures (walking, carrying,
    /// eating, sleeping), sources and huts appear, and clicking a person opens the inspector card read through the
    /// faction knowledge view. Screenshots of a foraging scene and of the inspector go to LocalData (not committed).
    /// </summary>
    public class PopulationViewTests
    {
        private const string Scene = "Assets/God/Scenes/Main.unity";

        [TearDown]
        public void TearDown() { Time.captureFramerate = 0; GodBootstrap.SkipStartCard = true; GodBootstrap.PendingSeed = null; GodBootstrap.PendingSetup = null; GodBootstrap.PendingStartAi = false; }

        private static string Dir()
        {
            string dir = System.Environment.GetEnvironmentVariable("Q3_SHOTS");
            if (string.IsNullOrEmpty(dir)) dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "LocalData"));
            Directory.CreateDirectory(dir);
            return dir;
        }

        [UnityTest]
        public IEnumerator LivingIsland_DrawsPeople_AndTheInspectorShowsTheirOwnView()
        {
            GodBootstrap.SkipStartCard = true;
            GodBootstrap.PendingSeed = 3;
            GodBootstrap.PendingSetup = new GameSetup(3, 0) { Population = true, StartNpcsPerFaction = 20, Victory = VictorySpec.SandboxOnly() };
            GodBootstrap.PendingStartAi = true;
            yield return SceneManager.LoadSceneAsync(Scene, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var boot = GodBootstrap.Instance;
            Assert.IsNotNull(boot.Sim.State.Population, "the living island has a population");
            Assert.IsNotNull(boot.People, "and the scene draws it");
            boot.Speed = 32f;
            Time.captureFramerate = 30;
            int frames = 0, maxSleep = 0, maxCarry = 0;
            while (boot.Sim.State.Tick < 1800 && frames++ < 3000)
            {
                yield return null;
                maxSleep = Mathf.Max(maxSleep, boot.People.SleepingNow);
                maxCarry = Mathf.Max(maxCarry, boot.People.CarryingNow);
            }
            Time.captureFramerate = 0;
            boot.Running = false;
            yield return null;
            Assert.AreEqual(boot.Sim.State.Population.AliveCount, boot.People.Count, "one figure per living person");
            Assert.Greater(boot.People.ThingsNow, 40, "sources, huts and lots are drawn");
            Assert.Greater(maxCarry, 0, "someone carried food or a log");
            Assert.Greater(maxSleep, 0, "someone slept");

            // frame a person of the player's faction who is foraging or eating, and open the inspector on them
            var p = boot.Sim.State.Population;
            var me = boot.View.Faction;
            var person = p.Npcs.Where(n => n.Alive && n.Faction == me && !n.Action.Asleep)
                .OrderBy(n => n.Action.Verb == Verb.ForageAt || n.Action.Verb == Verb.Eat ? 0 : 1).ThenBy(n => n.Id).First();
            var cam = boot.GodCamera;
            boot.People.TryPosition(person.Id, out var at);
            ModelViewTests.Aim(cam, at, 34f, 48f);
            for (int i = 0; i < 4; i++) yield return null;
            var sp = cam.WorldToScreenPoint(at + Vector3.up * 1.2f);
            Assert.AreEqual(person.Id, boot.Hud.PickNpc(new Vector2(sp.x, sp.y)), "clicking the figure picks that person");
            boot.Hud.SelectNpc(person.Id);
            for (int i = 0; i < 3; i++) yield return null;
            var card = boot.Hud.PersonCard;
            Assert.IsNotNull(card, "the card comes from the faction's view");
            Assert.AreEqual(person.Id, card.Id);
            Assert.IsNotEmpty(card.LastDecision, "its last decision");
            Assert.Greater(card.Candidates, 0);
            Assert.Greater(card.Entries, 0, "it has learned associations");
            Assert.IsTrue(card.HabitsPositive.Count + card.HabitsNegative.Count > 0, "its strongest habits are listed");
            Assert.Greater(card.Opportunities.Count, 0, "the options it weighed, with their score terms");
            Assert.AreEqual(1, card.Opportunities.Count(o => o.Chosen));
            Assert.Greater(card.Experiences.Count, 0, "its recent experiences with their rewards");
            Debug.Log("Inspector: " + card.LastDecision + " | why: " + string.Join("; ", card.Why) + " | habits: " + string.Join("; ", card.HabitsPositive)
                + " | options: " + string.Join("; ", card.Opportunities) + " | experiences: " + string.Join("; ", card.Experiences));
            var foreign = p.Npcs.First(n => n.Alive && n.Faction != me);
            Assert.IsNull(boot.View.Population.Inspect(foreign.Id), "another faction's people have no card for this player (Q21)");

            Shot(cam, "m3_foraging.png");
            yield return new WaitForEndOfFrame();
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(Dir(), "m3_inspector.png"), tex.EncodeToPNG());
            Object.Destroy(tex);
            Debug.Log("M3 screenshots: " + Dir());
        }

        private static void Shot(Camera cam, string name)
        {
            var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            var prev = RenderTexture.active; var prevT = cam.targetTexture;
            cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
            var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); tex.Apply();
            File.WriteAllBytes(Path.Combine(Dir(), name), tex.EncodeToPNG());
            cam.targetTexture = prevT; RenderTexture.active = prev;
            Object.Destroy(rt); Object.Destroy(tex);
        }
    }
}
