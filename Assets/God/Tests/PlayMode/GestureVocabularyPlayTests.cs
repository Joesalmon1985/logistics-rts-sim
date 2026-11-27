using System.Collections;
using System.Collections.Generic;
using System.Linq;
using God.Sim;
using God.Sim.Comms;
using God.Sim.Population;
using God.Sim.Social;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace God.Game.Tests
{
    /// <summary>
    /// U3b PlayMode: the gesture vocabulary and carried goods follow real events (U3-T4, T5, T6, T8), characters show their
    /// sim state (T9), and nothing is animated without an event. The animation never changes the sim.
    /// </summary>
    public class GestureVocabularyPlayTests
    {
        private const string Scene = "Assets/God/Scenes/Main.unity";

        [SetUp]
        public void SetUp() { DevOverlays.Reset(); }

        [TearDown]
        public void TearDown() { DevOverlays.Reset(); Time.captureFramerate = 0; GodBootstrap.StartRunning = true; GodBootstrap.SkipStartCard = true; GodBootstrap.PendingSeed = null; GodBootstrap.PendingSetup = null; GodBootstrap.PendingStartAi = false; }

        /// <param name="paused">Checkpoint B: start paused (<c>StartRunning = false</c>) so the frames of the scene load step no
        /// ticks by wall-clock time; the hash comparisons below then compare runs of exactly the same tick count.</param>
        private static IEnumerator Load(ulong seed, bool paused = false)
        {
            GodBootstrap.SkipStartCard = true;
            GodBootstrap.StartRunning = !paused;
            GodBootstrap.PendingSeed = seed;
            GodBootstrap.PendingSetup = new GameSetup(seed, 0) { Population = true, StartNpcsPerFaction = 20, Victory = VictorySpec.SandboxOnly() };
            GodBootstrap.PendingStartAi = true;
            yield return SceneManager.LoadSceneAsync(Scene, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null; yield return null;
        }

        private static string Dump(IEnumerable<AnimationCue> cues) => string.Join("\n", cues.Select(c => c.Kind + ":" + c.Actor + ">" + c.Target + "@" + c.StartTick + "#" + c.SourceEventId + "/" + c.Object.Ware + c.Object.Count));

        [UnityTest]
        public IEnumerator NothingIsAnimated_WithoutEvents()
        {
            yield return Load(3UL);
            var boot = GodBootstrap.Instance;
            boot.Running = false;
            for (int i = 0; i < 20; i++) yield return null;
            Assert.AreEqual(0, PoseLayer.Cues.Count, "no events, no cues");
            Assert.AreEqual(0, boot.DivineFx.ActiveCount, "no events, no divine effect");
            foreach (var t in boot.People.GetComponentsInChildren<Transform>(true))
                if (t.name == "Held" || t.name == "FingersR" || t.name == "FingersL") Assert.IsFalse(t.gameObject.activeSelf, t.name + " hidden");
        }

        [UnityTest]
        public IEnumerator TenDayRun_EveryGestureHasACue_AndGivingFollowsGestureAndDecision()
        {
            yield return Load(3UL);
            var boot = GodBootstrap.Instance;
            boot.Running = false;
            var log = new List<SimEvent>();
            boot.SimEventRaised += log.Add;
            var held = new List<string>();
            for (int chunk = 0; chunk < 120; chunk++)
            {
                boot.Sim.Step(100);
                boot.PumpEvents();
                yield return null;
            }
            var gestures = log.OfType<GestureMade>().ToList();
            Assert.Greater(gestures.Count, 0, "the island talked");
            foreach (var g in gestures)
            {
                long key = GestureAnimationPlanner.EventKey(g);
                var mine = PoseLayer.Cues.Where(c => c.SourceEventId == key && c.Actor == g.Speaker).ToList();
                Assert.GreaterOrEqual(mine.Count, 1, "gesture " + g.Act + " at " + g.Tick + " has a cue");
                Assert.IsTrue(mine.Any(c => c.Kind == GestureAnimationPlanner.FamilyOf(g.Act, g.TopicKind)), "the clip family of " + g.Act);
                Assert.IsTrue(mine.Any(c => c.DurationTicks == g.DurationTicks));
            }
            // every Giving cue comes from an exchange step whose deal had a gesture and a decision before it
            var steps = log.OfType<ExchangeStep>().ToList();
            var giving = PoseLayer.Cues.Where(c => c.Kind == AnimKind.Giving).ToList();
            foreach (var c in giving)
            {
                var step = steps.First(s => GestureAnimationPlanner.EventKey(s) == c.SourceEventId && s.Giver == c.Actor);
                Assert.IsTrue(log.OfType<GestureMade>().Any(g => g.DealId == step.DealId && g.Tick <= step.Tick), "a gesture before the hand-over");
                var msgs = log.OfType<GestureMade>().Where(g => g.DealId == step.DealId).Select(g => g.MessageId).ToList();
                Assert.IsTrue(log.OfType<ResponseChosen>().Any(r => !r.Ignored && msgs.Contains(r.MessageId) && r.Tick <= step.Tick), "a decision before the hand-over");
            }
            Assert.AreEqual(giving.Count, PoseLayer.Cues.Count(c => c.Kind == AnimKind.Receiving), "every Giving has its Receiving");
            Assert.LessOrEqual(giving.Count, steps.Count);
            // care transfers: functional only
            foreach (var c in PoseLayer.Cues.Where(c => c.Kind == AnimKind.Feeding || c.Kind == AnimKind.Tending)) Assert.IsTrue(c.Functional);
            TestContext.WriteLine("gestures " + gestures.Count + ", exchange steps " + steps.Count + ", Giving cues " + giving.Count + ", cues " + PoseLayer.Cues.Count);
        }

        [UnityTest]
        public IEnumerator HeldItem_IsTheEventsLot_AndTheSimAgrees()
        {
            yield return Load(3UL);
            var boot = GodBootstrap.Instance;
            boot.Running = false;
            Time.captureFramerate = 30;
            var p = boot.Sim.State.Population;
            // a real trade or gift if the island makes one soon; otherwise the events are fed for two real people
            ExchangeStep real = null;
            boot.SimEventRaised += e => { if (real == null && e is ExchangeStep s && PoseLayer.Cues.Any(c => c.Kind == AnimKind.Giving && c.SourceEventId == GestureAnimationPlanner.EventKey(s))) real = s; };
            for (int i = 0; i < 12000 && real == null; i++) { boot.Sim.Step(1); boot.PumpEvents(); if (i % 100 == 0) yield return null; }
            bool fed = real == null;
            int giver, receiver; Ware ware; int count;
            if (!fed)
            {
                giver = real.Giver; receiver = real.Receiver; ware = real.Ware; count = real.Count;
                var rn = p.Find(receiver);
                Assert.IsTrue(rn.Carry.Any(l => !l.Empty && l.Ware == ware), "the sim moved the ware into the receiver's carry at the tick");
            }
            else
            {
                var alive = p.Npcs.Where(n => n.Alive && !n.Action.Asleep).OrderBy(n => n.Id).ToList();
                giver = alive[0].Id; receiver = alive[1].Id; ware = Ware.Berries; count = 3;
                PoseLayer.OnEvent(new GestureMade { Tick = 1, MessageId = 900, Speaker = giver, Target = receiver, Act = GestureAct.Offer, DealId = 900, DurationTicks = 3 });
                PoseLayer.OnEvent(new ResponseChosen { Tick = 1, MessageId = 900, Listener = receiver, Speaker = giver });
                PoseLayer.OnEvent(new ExchangeStep { Tick = 2, DealId = 900, Giver = giver, Receiver = receiver, Lot = 4242, Ware = ware, Count = count });
            }
            // the giver holds it first, the receiver takes it at the same moment (the clips' release time), both the event's ware and count
            bool giverHeld = false, recvHeld = false;
            for (int f = 0; f < 120 && !(giverHeld && recvHeld); f++)
            {
                yield return null;
                if (PoseLayer.TryHeld(giver, out var g1) && g1.Ware == ware && g1.Count == count) giverHeld = true;
                if (PoseLayer.TryHeld(receiver, out var r1) && r1.Ware == ware && r1.Count == count) recvHeld = true;
            }
            Time.captureFramerate = 0;
            TestContext.WriteLine("held test used " + (fed ? "fed events" : "a real exchange step") + ": giver " + giverHeld + ", receiver " + recvHeld);
            Assert.IsTrue(giverHeld || recvHeld, "the item named by the event is in someone's hands");
            Assert.IsTrue(giverHeld && recvHeld || !(boot.People.PartsOf(giver) != null && boot.People.PartsOf(giver).Root.gameObject.activeSelf && boot.People.PartsOf(receiver) != null && boot.People.PartsOf(receiver).Root.gameObject.activeSelf),
                "both drawn partners show the item");
        }

        [UnityTest]
        public IEnumerator CharacterLooks_FromSim()
        {
            yield return Load(3UL);
            var boot = GodBootstrap.Instance;
            boot.Running = false;
            var p = boot.Sim.State.Population;
            // someone carrying a load (the island forages quickly)
            for (int i = 0; i < 60 && boot.People.CarryingNow == 0; i++) { boot.Sim.Step(50); yield return null; }
            var adults = p.Npcs.Where(n => n.Alive && n.Stage == LifeStage.Adult && n.Life != null && !n.Action.Asleep).OrderBy(n => n.Id).ToList();
            Assert.GreaterOrEqual(adults.Count, 5);
            var hurt = adults[0]; var sling = adults[1]; var ill = adults[2]; var preg = adults[3]; var well = adults[4];
            hurt.Injury = 100000; sling.Injury = CharacterLook.SlingFrom + 1000; ill.Life.Illness = 1; well.Injury = 0; well.Life.Illness = 0;
            preg.Life.PregFather = hurt.Id; preg.Life.PregStart = boot.Sim.State.Tick - 100; preg.Life.PregDue = boot.Sim.State.Tick + 100;
            yield return null; yield return null;
            var P = boot.People;
            Assert.IsTrue(P.PartsOf(hurt.Id).BandageArm.gameObject.activeSelf && P.PartsOf(hurt.Id).BandageLeg.gameObject.activeSelf, "bandages with an injury");
            Assert.IsFalse(P.PartsOf(hurt.Id).Sling.gameObject.activeSelf, "no sling for a light injury");
            Assert.IsTrue(P.PartsOf(sling.Id).Sling.gameObject.activeSelf, "a sling for a bad one");
            Assert.IsFalse(P.PartsOf(well.Id).BandageArm.gameObject.activeSelf || P.PartsOf(well.Id).BandageLeg.gameObject.activeSelf, "no bandage when well");
            Assert.AreSame(NpcLayer.IllMaterial, P.PartsOf(ill.Id).Skinned[0].sharedMaterial, "ill tint");
            Assert.AreNotSame(NpcLayer.IllMaterial, P.PartsOf(well.Id).Skinned[0].sharedMaterial, "no tint when well");
            Assert.IsTrue(P.PartsOf(preg.Id).Belly.gameObject.activeSelf);
            Assert.AreEqual(CharacterLook.BellyScale(0.5f), P.PartsOf(preg.Id).Belly.localScale.x, 0.02f, "belly by progress");
            Assert.IsFalse(P.PartsOf(well.Id).Belly.gameObject.activeSelf);
            // the carried ware's mesh is the Carry's
            int checkedLoads = 0;
            foreach (var n in p.Npcs.Where(n => n.Alive && !n.Action.Asleep))
            {
                var parts = P.PartsOf(n.Id);
                if (parts == null) continue;
                Ware load = Ware.None;
                foreach (var l in n.Carry) if (!l.Empty && l.Ware != Ware.Axe) { load = l.Ware; break; }
                if (load == Ware.None) { Assert.IsNull(parts.Cargo.sharedMesh, "empty-handed"); continue; }
                Assert.AreSame(NpcLayer.LoadMesh(load), parts.Cargo.sharedMesh, "carried " + load);
                checkedLoads++;
            }
            Assert.Greater(checkedLoads, 0, "someone carried a load");
            // age: a child's head is bigger than an adult's
            var child = p.Npcs.FirstOrDefault(n => n.Alive && n.Stage == LifeStage.Child);
            if (child != null && P.PartsOf(child.Id) != null) Assert.Greater(P.PartsOf(child.Id).Head.localScale.x, 1f);
            // original part names kept
            var names = P.PartsOf(well.Id).Root.GetComponentsInChildren<Transform>(true).Select(t => t.name).ToList();
            foreach (var need in new[] { "Body", "LegL", "LegR", "ArmL", "ArmR", "Cargo", "Tool", "Torso", "Head", "Held", "Symbol", "Aura", "Back", "Hip" }) CollectionAssert.Contains(names, need);
        }

        private static IEnumerator RunHash(ulong seed, float camDist, string[] cues, ulong[] hash, int[] gestureCount)
        {
            yield return Load(seed, paused: true);
            var boot = GodBootstrap.Instance;
            boot.Running = false;
            var cam = Camera.main;
            int gestures = 0;
            boot.SimEventRaised += e => { if (e is GestureMade) gestures++; };
            for (int chunk = 0; chunk < 40; chunk++)
            {
                if (cam != null) cam.transform.position = new Vector3(0, camDist, -camDist);
                boot.Sim.Step(100);
                boot.PumpEvents();
                yield return null;
            }
            cues[0] = Dump(PoseLayer.Cues); hash[0] = boot.Sim.StateHash(); gestureCount[0] = gestures;
        }

        [UnityTest]
        public IEnumerator LOD_Invariance_CamerasDoNotChangeCuesOrHash()
        {
            var c = new string[3][]; var h = new ulong[3][]; var g = new int[3][];
            float[] dist = { 15f, 80f, 400f };
            for (int i = 0; i < 3; i++)
            {
                c[i] = new string[1]; h[i] = new ulong[1]; g[i] = new int[1];
                yield return RunHash(5UL, dist[i], c[i], h[i], g[i]);
            }
            Assert.Greater(g[0][0], 0, "there were gestures to compare");
            Assert.AreEqual(h[0][0], h[1][0]); Assert.AreEqual(h[0][0], h[2][0]);
            Assert.AreEqual(c[0][0], c[1][0], "cue list identical near and mid"); Assert.AreEqual(c[0][0], c[2][0], "and far");
            Assert.IsNotEmpty(c[0][0]);
        }

        [UnityTest]
        public IEnumerator Animation_DoesNotChangeTheSimHash()
        {
            var a = new ulong[1]; var b = new ulong[1];
            yield return Hash(11UL, true, a);
            yield return Hash(11UL, false, b);
            Assert.AreEqual(a[0], b[0], "playing cues (events fed to the layer) versus not drawing them: same hash");
        }

        private static IEnumerator Hash(ulong seed, bool pump, ulong[] r)
        {
            yield return Load(seed, paused: true);
            var boot = GodBootstrap.Instance;
            boot.Running = false;
            for (int chunk = 0; chunk < 30; chunk++) { boot.Sim.Step(100); if (pump) boot.PumpEvents(); else boot.Sim.DrainEvents(); yield return null; }
            r[0] = boot.Sim.StateHash();
        }
    }
}
