using System.Collections;
using System.Linq;
using God.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace God.Game.Tests
{
    /// <summary>
    /// P9: the AI in the real scene. The headless smoke (<c>-godsmoke</c>) plays a six-brain watch game to its end, and a
    /// watch game is pictured mid-game (the whole island) and at a war front. Screenshots go where <c>GOD_SHOTS</c> says,
    /// else to LocalData (not committed).
    /// </summary>
    public class AiPlayTests
    {
        private static IEnumerator LoadWatch(ulong seed)
        {
            GodBootstrap.PendingSeed = seed;
            GodBootstrap.PendingSetup = GameSetup.Watch(seed);
            yield return SceneManager.LoadSceneAsync("Assets/God/Scenes/Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayMode_NormalGame_GivesEveryOtherFactionABrain()
        {
            yield return SceneManager.LoadSceneAsync("Assets/God/Scenes/Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var boot = GodBootstrap.Instance;
            var ai = AiHost.Director;
            Assert.IsNotNull(ai);
            Assert.AreSame(boot.Sim, ai.Sim);
            CollectionAssert.AreEqual(new[] { Faction.F1, Faction.F2, Faction.F3, Faction.F4, Faction.F5 }, ai.Brains.Select(b => b.Side).ToArray());
            boot.DebugSample();
            Assert.AreEqual(1, boot.Chains.Count, "F2 drives only the human faction");
            Assert.AreEqual(Faction.F0, boot.Chains[0].Side);
            boot.Running = false;
            for (int i = 0; i < 2000; i++) { boot.Sim.Step(); boot.Sim.DrainEvents(); }
            yield return null;
            foreach (var f in ai.Brains.Select(b => b.Side))
                Assert.Greater(boot.Sim.State.Territory.Buildings.Count(b => b.Faction == f), 1, FactionInfo.Name(f) + " builds");
        }

        [UnityTest]
        public IEnumerator Paused_TheBrainsWait_OnlyAPersonsCommandSteps()
        {
            yield return LoadWatch(3);
            var boot = GodBootstrap.Instance; var s = boot.Sim.State;
            boot.Speed = 8f;
            Time.captureFramerate = 30;
            while (s.Tick < 1500) yield return null; // brains busy, commands in flight
            boot.Running = false;
            for (int i = 0; i < 3; i++) yield return null; // the commands queued before the pause apply
            long tick = s.Tick;
            for (int i = 0; i < 120; i++) yield return null;
            Time.captureFramerate = 0;
            Assert.AreEqual(tick, s.Tick, "a paused game stays paused: the brains send nothing while held");
            Assert.AreEqual(0, boot.Sim.PendingCommands.Count);
        }

        [UnityTest]
        public IEnumerator Smoke_SixBrainWatchGame_EndsWell()
        {
            yield return LoadWatch(1);
            var boot = GodBootstrap.Instance;
            Assert.IsFalse(boot.Sim.State.Setup.HasHuman);
            Assert.AreEqual(6, AiHost.Director.Brains.Count, "watch mode: six brains");
            var run = SmokeRun.Begin(boot, 54000, false);
            int frames = 0;
            while (!run.Ok.HasValue && frames++ < 20000) yield return null;
            Debug.Log("P9 smoke: " + run.LastLine);
            Assert.IsTrue(run.Ok.HasValue, "the smoke finished");
            Assert.IsTrue(run.Ok.Value, run.LastLine);
            Assert.IsTrue(boot.Sim.State.IsGameOver, "the game ended");
            Assert.Greater(run.Wars, 0);
        }

        [UnityTest]
        public IEnumerator Screenshots_MidGameIsland_AndAWarFront()
        {
            yield return LoadWatch(1);
            var boot = GodBootstrap.Instance;
            var sim = boot.Sim; var s = sim.State; var g = s.Territory.Grid;
            var cam = boot.GodCamera;
            boot.Running = false;

            // Mid-game: towns have grown.
            while (s.Tick < 10800 && !s.IsGameOver)
            {
                for (int k = 0; k < 300; k++) { sim.Step(); sim.DrainEvents(); }
                yield return null;
            }
            Assert.Greater(s.Economy.Buildings.Count(b => b.Built && !b.IsHq), 40, "grown towns");
            IslandCamera.Frame(cam, BoardSpace.FrameRadius + 40f, BoardSpace.BoardCentre);
            for (int i = 0; i < 3; i++) yield return null;
            ModelViewTests.Shot(cam, "p9_midgame.png", 1920, 1080);

            // A war front: the first moment many shots fly between two factions in one place.
            bool shot = false;
            while (!shot && !s.IsGameOver && s.Tick < 54000)
            {
                var fired = new System.Collections.Generic.List<UnitFired>();
                for (int k = 0; k < 10; k++) { sim.Step(); fired.AddRange(sim.DrainEvents().OfType<UnitFired>()); }
                if (fired.Count >= 8 && fired.Select(f => f.Faction).Distinct().Count() >= 2)
                {
                    var c = new Vector3((float)fired.Average(f => f.FromX) / 1000f, 0f, (float)fired.Average(f => f.FromZ) / 1000f);
                    int near = fired.Count(f => (new Vector3(f.FromX / 1000f, 0f, f.FromZ / 1000f) - c).magnitude < 25f);
                    if (near >= 6)
                    {
                        yield return null;
                        ModelViewTests.Aim(cam, c, 70f, 50f);
                        yield return null;
                        ModelViewTests.Shot(cam, "p9_war.png");
                        shot = true;
                        Debug.Log("P9 war front at tick " + s.Tick + ": " + fired.Count + " shots by " + string.Join("/", fired.Select(f => FactionInfo.Name(f.Faction)).Distinct()));
                    }
                }
                if (s.Tick % 300 == 0) yield return null;
            }
            Assert.IsTrue(shot, "a war front was found");
        }
    }
}
