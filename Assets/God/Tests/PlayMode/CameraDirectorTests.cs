using System.Collections;
using System.Linq;
using God.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace God.Game.Tests
{
    /// <summary>U1-T1, U1-T2 and the live-while-paused rule: the free overhead camera in the real scene.</summary>
    public class CameraDirectorTests
    {
        [TearDown]
        public void TearDown() { U1.Reset(); }

        [UnityTest]
        public IEnumerator Camera_OrbitClampZoom_AtEightYaws()
        {
            yield return U1.Load(3, true);
            var boot = GodBootstrap.Instance; var d = boot.Director; var cam = boot.GodCamera;
            Assert.IsTrue(d.Driving, "god-only mode: the director places the camera");
            // the home view is the old view: yaw 0, pitch 50
            Assert.AreEqual(0f, Mathf.DeltaAngle(0f, d.Yaw), 0.5f);
            Assert.AreEqual(50f, d.Pitch, 0.5f);
            float home = d.MaxDistance;
            int person = U1.SomeDrawnPerson(boot);
            for (int k = 0; k < 8; k++)
            {
                float yaw = k * 45f;
                d.Home(true);
                d.SetOrbit(yaw, 50f);
                d.Settle();
                // keyboard pan follows the yaw: 'right' moves along the camera's right in the ground plane
                d.ZoomToBand(ZoomBand.Settlement); d.Settle();
                var before = d.TargetFocus;
                d.Pan(new Vector2(1f, 0f), 0.1f);
                var delta = d.TargetFocus - before; delta.y = 0f;
                var right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
                Assert.Greater(Vector3.Dot(delta.normalized, right), 0.98f, "pan right at yaw " + yaw);
                // zoom and pan far: the focus stays over the island
                d.Zoom(8f, new Vector2(Screen.width * 0.7f, Screen.height * 0.3f));
                d.PanBy(new Vector3(5000f, 0f, -5000f));
                d.Settle();
                var c = boot.Rig.IslandCentre;
                float off = new Vector2(d.Focus.x - c.x, d.Focus.z - c.z).magnitude;
                Assert.LessOrEqual(off, boot.Rig.IslandRadius, "focus over the island at yaw " + yaw);
                Assert.GreaterOrEqual(d.Distance, d.MinDistance - 0.01f);
                Assert.LessOrEqual(d.Distance, home + 0.01f);
                // the picker works at any yaw: close on one person, their figure is more than a quarter of the screen high
                Assert.IsTrue(d.FocusOnPerson(person, true));
                d.ZoomTo(d.MinDistance); d.Settle();
                Assert.IsTrue(boot.Hud.TryDrawnPoint(person, out var chest));
                var feet = chest - Vector3.up * 1.2f; var head = feet + Vector3.up * 1.9f;
                float h = Mathf.Abs(cam.WorldToScreenPoint(head).y - cam.WorldToScreenPoint(feet).y) / cam.pixelHeight;
                Assert.Greater(h, 0.25f, "one person fills more than 25% of the screen height at yaw " + yaw);
                Assert.AreEqual(person, boot.Hud.PickNpc(U1.ScreenOf(boot, person)), "clicking the figure picks them at yaw " + yaw);
            }
            // pitch is clamped 25..80
            d.SetOrbit(0f, 5f); Assert.AreEqual(25f, d.TargetPitch, 0.001f);
            d.SetOrbit(0f, 120f); Assert.AreEqual(80f, d.TargetPitch, 0.001f);
            // the zoom bands
            d.Home(true);
            Assert.AreEqual(ZoomBand.Island, d.Band);
            d.ZoomToBand(ZoomBand.Region); d.Settle(); Assert.AreEqual(ZoomBand.Region, d.Band);
            d.ZoomToBand(ZoomBand.Settlement); d.Settle(); Assert.AreEqual(ZoomBand.Settlement, d.Band);
            d.ZoomToBand(ZoomBand.Individual); d.Settle(); Assert.AreEqual(ZoomBand.Individual, d.Band);
        }

        [UnityTest]
        public IEnumerator Camera_FollowsWalkingPerson_AndPanDetaches()
        {
            yield return U1.Load(3, true);
            var boot = GodBootstrap.Instance; var d = boot.Director;
            Time.captureFramerate = 30;
            boot.Speed = 4f; boot.Running = true;
            // find someone who is walking: the figure that moved furthest over 30 frames
            var start = boot.Sim.State.Population.Npcs.Where(n => n.Alive).Select(n => n.Id).Take(40).ToDictionary(id => id, id => { boot.People.TryPosition(id, out var p); return p; });
            yield return U1.Frames(30);
            int walker = 0; float best = 0f;
            foreach (var kv in start) if (boot.People.TryPosition(kv.Key, out var p)) { float m = (p - kv.Value).magnitude; if (m > best) { best = m; walker = kv.Key; } }
            Assert.Greater(walker, 0); Assert.Greater(best, 1f, "someone is walking");
            boot.Speed = 1f; // the lag of a smoothed follow is speed times the smoothing time: judged at the ordinary pace
            d.Home(true);
            Assert.IsTrue(d.FocusOnPerson(walker, true)); // start near them: frames in a headless run are short, so a far start has not converged
            Assert.IsTrue(d.Follow(walker), "following a drawn person");
            d.ZoomToBand(ZoomBand.Settlement);
            boot.People.TryPosition(walker, out var p0);
            float travelled = 0f; Vector3 last = p0;
            for (int i = 0; i < 200; i++)
            {
                yield return null;
                d.Advance(1f / 30f); // a headless frame lasts a few milliseconds of unscaled time but 1/30 s of figure time: advance the spring at the fixed rate
                if (boot.People.TryPosition(walker, out var p)) { travelled += (p - last).magnitude; last = p; }
            }
            Assert.IsTrue(boot.People.TryPosition(walker, out var now), "still drawn (alive)");
            Assert.IsTrue(d.Following);
            var f = d.Focus;
            Assert.LessOrEqual(new Vector2(f.x - now.x, f.z - now.z).magnitude, 1.5f, "focus within 1.5 m of the drawn position");
            Assert.Greater(travelled, 1f, "the person really walked meanwhile (" + travelled + " m)");
            d.PanBy(new Vector3(10f, 0f, 0f));
            Assert.IsFalse(d.Following, "a pan detaches the follow camera");
        }

        [UnityTest]
        public IEnumerator Camera_StaysLiveWhilePaused_AndFollowEndsInPlaceWhenThePersonIsGone()
        {
            yield return U1.Load(3, true);
            var boot = GodBootstrap.Instance; var d = boot.Director; var player = boot.Player;
            var input = new ScriptedInput(); player.Source = input; player.InputEnabled = false;
            Assert.IsFalse(boot.Running);
            long tick = boot.Sim.State.Tick;
            var p0 = boot.GodCamera.transform.position; var r0 = boot.GodCamera.transform.rotation;
            input.Rotate = 1f; input.Move = new Vector2(0f, 1f);
            for (int i = 0; i < 20; i++) { player.Step(1f / 30f); d.Advance(1f / 30f); }
            input.Rotate = 0f; input.Move = Vector2.zero;
            Assert.AreNotEqual(p0, boot.GodCamera.transform.position, "the camera moved while the game was paused");
            Assert.Greater(Quaternion.Angle(r0, boot.GodCamera.transform.rotation), 20f, "and turned");
            Assert.AreEqual(tick, boot.Sim.State.Tick, "paused: the sim did not move");
            // Home returns to the framed island, north up
            player.Step(1f / 30f);
            input.Press(PlayerKeys.Home); player.Step(1f / 30f);
            for (int i = 0; i < 60; i++) d.Advance(1f / 30f);
            Assert.AreEqual(0f, Mathf.DeltaAngle(0f, d.Yaw), 1f);
            Assert.AreEqual(d.MaxDistance, d.Distance, d.MaxDistance * 0.02f);
            // a follow target that vanishes leaves the camera where it is
            var gone = new VanishingTarget { At = new Vector3(d.Focus.x, 0f, d.Focus.z) };
            Assert.IsTrue(d.Follow(gone));
            bool ended = false; d.FollowEnded += () => ended = true;
            gone.Alive = false;
            var held = d.Focus;
            d.Advance(0.05f);
            Assert.IsTrue(ended); Assert.IsFalse(d.Following);
            Assert.Less((held - d.Focus).magnitude, 0.01f, "the camera stays in place");
        }

        private sealed class VanishingTarget : IFollowTarget
        {
            public Vector3 At; public bool Alive = true;
            public int Id => 999;
            public bool TryGetPosition(out Vector3 w) { w = At; return Alive; }
        }
    }
}
