using System.Collections;
using System.Linq;
using God.Sim;
using God.Sim.Comms;
using God.Sim.Social;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace God.Game.Tests
{
    /// <summary>
    /// U3a (M5.18, D5.18 to D5.20): the decoded message text, the addressee line, the grid and the producing-hex glow are
    /// developer overlays, off by default; none of them changes the sim hash.
    /// </summary>
    public class DevOverlayTests
    {
        private const string Scene = "Assets/God/Scenes/Main.unity";

        [SetUp]
        public void SetUp() { DevOverlays.Reset(); }

        [TearDown]
        public void TearDown() { DevOverlays.Reset(); Time.captureFramerate = 0; GodBootstrap.SkipStartCard = true; GodBootstrap.PendingSeed = null; GodBootstrap.PendingSetup = null; GodBootstrap.PendingStartAi = false; }

        private static IEnumerator Load(ulong seed)
        {
            GodBootstrap.SkipStartCard = true;
            GodBootstrap.PendingSeed = seed;
            GodBootstrap.PendingSetup = new GameSetup(seed, 0) { Population = true, StartNpcsPerFaction = 20, Victory = VictorySpec.SandboxOnly() };
            GodBootstrap.PendingStartAi = true;
            yield return SceneManager.LoadSceneAsync(Scene, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
        }

        private static int Labels(GodBootstrap boot) =>
            boot.People.GetComponentsInChildren<Transform>(true).Count(t => t.name == "GestureLabel" && t.gameObject.activeInHierarchy);
        private static int Lines(GodBootstrap boot) =>
            boot.People.GetComponentsInChildren<Transform>(true).Count(t => t.name == "GestureLine" && t.gameObject.activeInHierarchy);

        [UnityTest]
        public IEnumerator Gesture_TextAndLine_OnlyWithTheDecodeOverlay()
        {
            yield return Load(3UL);
            var boot = GodBootstrap.Instance;
            boot.Running = false;
            for (int i = 0; i < 200; i++) boot.Sim.Step();
            for (int i = 0; i < 3; i++) yield return null;
            var alive = boot.Sim.State.Population.Npcs.Where(n => n.Alive).OrderBy(n => n.Id).ToList();
            var speaker = alive[0]; var other = alive[1];
            var g = new GestureMade { Speaker = speaker.Id, Act = GestureAct.Warn, Target = other.Id };

            // overlay off: the gesture is animated, no text, no line
            GestureLayer.OnEvent(g); PoseLayer.OnEvent(g);
            for (int i = 0; i < 3; i++) yield return null;
            Assert.IsNotNull(GestureLayer.Current(speaker.Id), "the gesture is held for drawing");
            Assert.AreEqual(0, Labels(boot), "no gesture text with the overlay off");
            Assert.AreEqual(0, Lines(boot), "no addressee line with the overlay off");
            // the body animation (U3b vocabulary) is separate from the text: a cue is queued for the speaker even with the overlay off
            Assert.IsTrue(PoseLayer.Cues.Any(c => c.Actor == speaker.Id), "the gesture is animated by the vocabulary");

            // captions only: a short text, still no line
            DevOverlays.Captions = true;
            GestureLayer.OnEvent(g);
            for (int i = 0; i < 3; i++) yield return null;
            Assert.AreEqual(1, Labels(boot), "caption shown");
            Assert.AreEqual(0, Lines(boot), "captions never draw the addressee line");
            var cap = boot.People.GetComponentsInChildren<TextMesh>(true).First(t => t.name == "GestureLabel").text;
            Assert.AreEqual(GestureLayer.Caption(g), cap);
            DevOverlays.Captions = false;
            for (int i = 0; i < 2; i++) yield return null;
            Assert.AreEqual(0, Labels(boot), "caption hidden again");

            // decode overlay on: text and line
            DevOverlays.DecodedMessages = true;
            GestureLayer.OnEvent(g);
            for (int i = 0; i < 3; i++) yield return null;
            Assert.AreEqual(1, Labels(boot), "decoded text drawn");
            Assert.AreEqual(1, Lines(boot), "addressee line drawn");
            Assert.AreEqual(GestureLayer.Words(g), boot.People.GetComponentsInChildren<TextMesh>(true).First(t => t.name == "GestureLabel").text);
        }

        [UnityTest]
        public IEnumerator GridOverlay_IsOffByDefault_AndHexGlowStaysQuiet()
        {
            yield return Load(5UL);
            var boot = GodBootstrap.Instance;
            Assert.IsFalse(DevOverlays.GridOverlay);
            Assert.IsFalse(DevOverlays.DecodedMessages);
            Assert.IsFalse(DevOverlays.Captions);
            Assert.IsNull(boot.LegacyIsland, "the old hex look is not built");
            boot.Speed = 32f;
            Time.captureFramerate = 30;
            int frames = 0;
            while (boot.Sim.State.Tick < 1200 && frames++ < 2500)
            {
                yield return null;
                Assert.IsFalse(boot.Territory.ShowDebugOverlay, "node lattice off");
                Assert.AreEqual(0, boot.Island.PulsingNow.Count, "no producing-hex glow in normal play");
            }
            Time.captureFramerate = 0;
            boot.Running = false;
            DevOverlays.GridOverlay = true;
            yield return null; yield return null;
            Assert.IsTrue(boot.Territory.ShowDebugOverlay, "the grid overlay switches the lattice on");
            DevOverlays.GridOverlay = false;
            yield return null; yield return null;
            Assert.IsFalse(boot.Territory.ShowDebugOverlay);
        }

        private static IEnumerator HashAfter(bool overlays, ulong[] result)
        {
            DevOverlays.Reset();
            yield return Load(7UL);
            var boot = GodBootstrap.Instance;
            boot.Running = false;
            DevOverlays.DecodedMessages = overlays; DevOverlays.Captions = overlays; DevOverlays.GridOverlay = overlays;
            for (int chunk = 0; chunk < 10; chunk++)
            {
                boot.Sim.Step(60);
                yield return null;
            }
            result[0] = boot.Sim.StateHash();
        }

        [UnityTest]
        public IEnumerator StateHash_IsIdentical_WithOverlaysOnOrOff()
        {
            var off = new ulong[1]; var on = new ulong[1];
            yield return HashAfter(false, off);
            yield return HashAfter(true, on);
            Assert.AreNotEqual(0UL, off[0]);
            Assert.AreEqual(off[0], on[0], "overlays are view-only");
        }
    }
}
