using System.Collections;
using God.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace God.Game.Tests
{
    /// <summary>Shared set-up of the U1a PlayMode tests: the Living island in god-only mode (or stage 3 mode), the stand-in divine command factory, fixed-rate frames.</summary>
    internal static class U1
    {
        public const string Scene = "Assets/God/Scenes/Main.unity";

        /// <summary>Stands in for F1a's divine commands: every request becomes a stage 3 war declaration (a command the sim accepts today).</summary>
        public sealed class StandInFactory : IDivineCommandFactory
        {
            public readonly System.Collections.Generic.List<DivineRequest> Requests = new System.Collections.Generic.List<DivineRequest>();
            public Command Create(DivineRequest r) { Requests.Add(r); return new DeclareWar(Faction.F0, Faction.F1); }
        }

        public static void Reset()
        {
            Time.captureFramerate = 0;
            GodBootstrap.SkipStartCard = true;
            GodBootstrap.ForceGodOnly = false; GodBootstrap.StartRunning = true;
            GodBootstrap.PendingSeed = null; GodBootstrap.PendingSetup = null; GodBootstrap.PendingStartAi = false; GodBootstrap.PendingTuning = null;
        }

        /// <summary>Loads the scene and waits for the first framing. godOnly: the Living island with no human faction and the U1a controls.</summary>
        public static IEnumerator Load(ulong seed, bool godOnly, bool human = false)
        {
            GodBootstrap.SkipStartCard = true;
            GodBootstrap.ForceGodOnly = godOnly; GodBootstrap.StartRunning = false;
            GodBootstrap.PendingSeed = seed;
            GodBootstrap.PendingSetup = new GameSetup(seed, godOnly || !human ? GameSetup.NoHuman : 0) { Population = true, StartNpcsPerFaction = 20, Victory = VictorySpec.SandboxOnly() };
            GodBootstrap.PendingStartAi = true;
            yield return SceneManager.LoadSceneAsync(Scene, new LoadSceneParameters(LoadSceneMode.Single));
            for (int i = 0; i < 4; i++) yield return null;
            var boot = GodBootstrap.Instance;
            Assert.IsNotNull(boot.Director, "the bootstrap builds the camera director");
            boot.Running = false;
        }

        public static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        /// <summary>Screen position (pixels) of a drawn person.</summary>
        public static Vector2 ScreenOf(GodBootstrap boot, int id)
        {
            Assert.IsTrue(boot.Hud.DrawnScreenPoint(id, out var sp), "person " + id + " is drawn and in front of the camera");
            return sp;
        }

        /// <summary>A living person of the island that is drawn, preferring one in the middle of the board.</summary>
        public static int SomeDrawnPerson(GodBootstrap boot, int skip = 0)
        {
            var list = new System.Collections.Generic.List<NpcSighting0>();
            foreach (var n in boot.Sim.State.Population.Npcs)
                if (n.Alive && boot.People.TryPosition(n.Id, out var p)) list.Add(new NpcSighting0 { Id = n.Id, D = (p - boot.Rig.IslandCentre).sqrMagnitude });
            list.Sort((a, b) => a.D.CompareTo(b.D));
            return list[Mathf.Min(skip, list.Count - 1)].Id;
        }

        private struct NpcSighting0 { public int Id; public float D; }
    }
}
