using System.Collections;
using System.Linq;
using God.Sim;
using God.Sim.Divinity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace God.Game.Tests
{
    /// <summary>U3b PlayMode: divine effects are drawn only from real events, on the real thing named, and the avatar has its own model (U3-T11, T12, T13).</summary>
    public class DivineFxPlayTests
    {
        private const string Scene = "Assets/God/Scenes/Main.unity";

        [TearDown]
        public void TearDown() { DevOverlays.Reset(); Time.captureFramerate = 0; GodBootstrap.SkipStartCard = true; GodBootstrap.PendingSeed = null; GodBootstrap.PendingSetup = null; GodBootstrap.PendingStartAi = false; }

        private static IEnumerator Load(ulong seed)
        {
            GodBootstrap.SkipStartCard = true;
            GodBootstrap.PendingSeed = seed;
            GodBootstrap.PendingSetup = new GameSetup(seed, 0) { Population = true, StartNpcsPerFaction = 20, Victory = VictorySpec.SandboxOnly() };
            GodBootstrap.PendingStartAi = false;
            yield return SceneManager.LoadSceneAsync(Scene, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null; yield return null;
            GodBootstrap.Instance.Running = false;
        }

        [UnityTest]
        public IEnumerator OnlyFromEvents_OneEffectEach_AndARefusedSuggestionPlaysDeliveryThenRefusal()
        {
            yield return Load(3UL);
            var boot = GodBootstrap.Instance; var fx = boot.DivineFx;
            var alive = boot.Sim.State.Population.Npcs.Where(n => n.Alive).OrderBy(n => n.Id).ToList();
            int a = alive[0].Id, b = alive[1].Id;
            yield return null;
            Assert.AreEqual(0, fx.ActiveCount, "no event, no effect");

            PoseLayer.Reset();
            fx.OnEvent(new CommandPerceived { Tick = 5, Npc = a, God = 1, Kind = InterventionKind.Suggestion, Intervention = 1 });
            Assert.AreEqual(1, fx.ActiveCount, "the suggestion's delivery is one halo");
            Assert.AreEqual(FxShape.Halo, fx.Records.Last().Shape);
            boot.SimEventRaised += PoseLayer.OnEvent; // the pump is not running: feed the response as the bootstrap would
            PoseLayer.OnEvent(new CommandResponded { Tick = 6, Npc = a, God = 1, Response = CommandResponse.Refused, Intervention = 1 });
            boot.SimEventRaised -= PoseLayer.OnEvent;
            fx.OnEvent(new CommandResponded { Tick = 6, Npc = a, God = 1, Response = CommandResponse.Refused, Intervention = 1 });
            Assert.AreEqual(1, fx.ActiveCount, "the refusal adds no divine effect");
            var cue = PoseLayer.Cues.Single();
            Assert.AreEqual(AnimKind.Refusing, cue.Kind); Assert.AreEqual(a, cue.Actor, "the person's own refusal animation");
            yield return null; yield return null;
            var go = fx.ObjectOf(fx.Records.First().Id);
            Assert.IsNotNull(go);
            Assert.IsTrue(boot.People.TryPosition(a, out var at));
            Assert.Less(Vector3.Distance(new Vector3(go.transform.position.x, 0, go.transform.position.z), new Vector3(at.x, 0, at.z)), 0.1f, "the halo stands on the person");

            // one effect per event type, each on the real thing
            int before = fx.ActiveCount;
            fx.OnEvent(new CommandPerceived { Tick = 7, Npc = b, God = 2, Kind = InterventionKind.Command, Intervention = 2 }); Assert.AreEqual(before + 1, fx.ActiveCount);
            fx.OnEvent(new CompulsionStarted { Tick = 8, Npc = b, God = 2, Intervention = 3 }); Assert.AreEqual(before + 2, fx.ActiveCount);
            var tether = fx.Records.Last();
            fx.OnEvent(new CompulsionEnded { Tick = 9, Npc = b, God = 2, Intervention = 3 }); Assert.AreEqual(before + 3, fx.ActiveCount);
            Assert.AreEqual(tether.Id, fx.Records.Last().Closes);
            fx.OnEvent(new LocationConsecrated { Tick = 10, God = 2, Node = 5 }); Assert.AreEqual(before + 4, fx.ActiveCount);
            var lot = boot.Sim.State.Population.Lots.FirstOrDefault();
            if (lot.Lot.Id != 0)
            {
                fx.OnEvent(new ResourceCreated { Tick = 11, God = 2, Ware = lot.Lot.Ware, Count = 2, Lot = lot.Lot.Id, Node = lot.Node });
                var rec = fx.Records.Last();
                Assert.AreEqual(lot.Lot.Id, rec.Lot); Assert.AreEqual(lot.Node, rec.Node);
                yield return null;
                var g = fx.ObjectOf(rec.Id);
                var want = ModelKit.Node(boot.Sim.State.Territory.Grid, lot.Node);
                Assert.Less(Vector3.Distance(new Vector3(g.transform.position.x, 0, g.transform.position.z), new Vector3(want.x, 0, want.z)), 0.2f, "motes on the real lot");
            }
            // distinct gods wear distinct cores
            Assert.AreNotEqual(GodLook.Colour(1).R + GodLook.Colour(1).G, GodLook.Colour(2).R + GodLook.Colour(2).G);
        }

        [UnityTest]
        public IEnumerator Smite_BoltOnCast_CollapseOnlyWhenTheEffectHappened()
        {
            yield return Load(3UL);
            var boot = GodBootstrap.Instance; var fx = boot.DivineFx;
            var hut = boot.Sim.State.Population.Huts.FirstOrDefault();
            Assert.IsNotNull(hut);
            fx.OnEvent(new MiracleCast { Tick = 5, God = 1, Kind = MiracleKind.SmiteBuilding, TargetKind = DivineTargetKind.Hut, TargetId = hut.Id, TargetNode = hut.Node, Intervention = 9 });
            Assert.AreEqual(1, fx.ActiveCount);
            Assert.AreEqual(FxShape.Bolt, fx.Records.Last().Shape, "the bolt plays on the cast");
            Assert.IsFalse(fx.Records.Any(r => r.Shape == FxShape.Collapse), "the hut is not shown falling while nothing happened to it");
            fx.OnEvent(new MiracleEffectApplied { Tick = 5, Intervention = 9, ThingKind = MiracleThing.Hut, ThingId = hut.Id });
            Assert.AreEqual(FxShape.Collapse, fx.Records.Last().Shape, "the collapse follows the real effect");
            Assert.AreEqual(hut.Node, fx.Records.Last().Node);
            // a heal: a spiral, and the healed person's own thanks
            var p = boot.Sim.State.Population.Npcs.First(n => n.Alive);
            fx.OnEvent(new MiracleCast { Tick = 6, God = 1, Kind = MiracleKind.HealInjury, TargetKind = DivineTargetKind.Npc, TargetId = p.Id, Intervention = 10 });
            Assert.AreEqual(FxShape.HealSpiral, fx.Records.Last().Shape);
        }

        [UnityTest]
        public IEnumerator Avatar_HasItsOwnModel_AndCollapsesOnItsEvent()
        {
            yield return Load(3UL);
            var boot = GodBootstrap.Instance; var fx = boot.DivineFx;
            var npc = boot.Sim.State.Population.Npcs.First(n => n.Alive);
            var avatar = DivineFxLayer.ShapeMesh(FxShape.Figure);
            var villager = boot.People.PartsOf(npc.Id).BodyF.sharedMesh;
            Assert.AreNotSame(villager, avatar, "a model of its own");
            Assert.Greater(avatar.bounds.size.y, 3.4f, "taller than the villager figure (2 m before scaling)");
            Assert.AreNotEqual(avatar.vertexCount, villager.vertexCount);
            fx.OnCode(851, 5, 1, npc.Id, -1);
            Assert.AreEqual(FxShape.Figure, fx.Records.Last().Shape);
            var fig = fx.Records.Last();
            fx.OnCode(852, 8, 1, npc.Id, -1);
            Assert.AreEqual(FxShape.Implode, fx.Records.Last().Shape, "the collapse effect on the avatar's destruction");
            Assert.AreEqual(fig.Id, fx.Records.Last().Closes);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Budget_TwentyFourDetailed_TheRestMarkers()
        {
            yield return Load(3UL);
            var boot = GodBootstrap.Instance; var fx = boot.DivineFx;
            var alive = boot.Sim.State.Population.Npcs.Where(n => n.Alive).OrderBy(n => n.Id).Take(30).ToList();
            for (int i = 0; i < alive.Count; i++) fx.OnEvent(new CommandPerceived { Tick = 5, Npc = alive[i].Id, God = 1, Kind = InterventionKind.Command, Intervention = i + 1 });
            Assert.AreEqual(alive.Count, fx.ActiveCount);
            Assert.AreEqual(24, fx.DetailedActive);
            yield return null;
        }
    }
}
