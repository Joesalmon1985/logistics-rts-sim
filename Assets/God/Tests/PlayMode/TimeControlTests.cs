using System.Collections;
using System.Collections.Generic;
using System.Linq;
using God.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace God.Game.Tests
{
    /// <summary>U1-T5, U1-T6: pause with queued commands (god-only), the speed sets, step and accelerated.</summary>
    public class TimeControlTests
    {
        [TearDown]
        public void TearDown() { U1.Reset(); }

        [UnityTest]
        public IEnumerator PausedCommand_Queued_InGodOnly_AppliedAtTheNextTick()
        {
            yield return U1.Load(3, true);
            var boot = GodBootstrap.Instance; var player = boot.Player;
            var f = new U1.StandInFactory(); player.Builder.Factory = f;
            var events = new List<SimEvent>(); boot.SimEventRaised += events.Add;
            Assert.IsFalse(boot.Running, "paused");
            long t0 = boot.Sim.State.Tick;
            boot.Selection.SetPerson(U1.SomeDrawnPerson(boot));
            var r = player.Issue("suggest", 1);
            Assert.IsTrue(r.Submitted, r.Result.ToString());
            yield return U1.Frames(8);
            Assert.AreEqual(t0, boot.Sim.State.Tick, "god-only: a command sent while paused does not take a tick");
            Assert.AreEqual(1, boot.Sim.PendingCommands.Count, "it is pending");
            Assert.AreEqual(1, boot.Hud.PendingCommandCount);
            Assert.IsFalse(events.Any(e => e is WarDeclared));
            boot.Hud.StepTicks(1);
            Assert.AreEqual(t0 + 1, boot.Sim.State.Tick);
            Assert.AreEqual(0, boot.Sim.PendingCommands.Count);
            Assert.IsTrue(events.Any(e => e is WarDeclared), "its event is raised at the next tick that runs");
        }

        [UnityTest]
        public IEnumerator PausedCommand_Stage3Mode_StillTakesItsOneTick()
        {
            yield return U1.Load(3, false);
            var boot = GodBootstrap.Instance;
            Assert.IsFalse(boot.GodOnly);
            boot.Running = false;
            long t0 = boot.Sim.State.Tick;
            Assert.IsTrue(boot.Gate.Submit(new DeclareWar(Faction.F0, Faction.F1)).Ok);
            yield return U1.Frames(4);
            Assert.AreEqual(t0 + 1, boot.Sim.State.Tick, "stage 3: the M1 rule, one ordinary tick when a command is accepted");
            Assert.AreEqual(0, boot.Sim.PendingCommands.Count);
        }

        [UnityTest]
        public IEnumerator Speeds_GodOnly_SetAndRate()
        {
            yield return U1.Load(3, true);
            var boot = GodBootstrap.Instance; var hud = boot.Hud;
            CollectionAssert.AreEqual(new[] { 1f, 2f, 5f, 10f }, hud.Speeds);
            Time.captureFramerate = 30;
            hud.SetSpeed(5f);
            Assert.IsTrue(boot.Running); Assert.AreEqual(5f, boot.Speed);
            long t0 = boot.Sim.State.Tick;
            yield return U1.Frames(100);
            long steps = boot.Sim.State.Tick - t0;
            float expect = 100f / 30f * 5f * GodSim.TicksPerSecond;
            Assert.AreEqual(expect, steps, expect * 0.05f, "within 5% of 5x");
            // + and - walk the set
            hud.StepSpeed(1); Assert.AreEqual(10f, boot.Speed);
            hud.StepSpeed(1); Assert.AreEqual(10f, boot.Speed);
            hud.StepSpeed(-1); hud.StepSpeed(-1); hud.StepSpeed(-1); Assert.AreEqual(1f, boot.Speed);
        }

        [UnityTest]
        public IEnumerator Speeds_Stage3AndWatchSets_Unchanged()
        {
            yield return U1.Load(3, false, human: true);
            var hud = GodBootstrap.Instance.Hud;
            Assert.IsFalse(hud.Watching);
            CollectionAssert.AreEqual(new[] { 1f, 2f, 4f }, hud.Speeds);
            U1.Reset();
            yield return U1.Load(3, false, human: false);
            hud = GodBootstrap.Instance.Hud;
            Assert.IsTrue(hud.Watching);
            CollectionAssert.AreEqual(new[] { 1f, 2f, 4f, 8f, 16f }, hud.Speeds);
        }

        [UnityTest]
        public IEnumerator StepKey_AndAcceleratedMode()
        {
            yield return U1.Load(3, true);
            var boot = GodBootstrap.Instance; var hud = boot.Hud; var player = boot.Player;
            var input = new ScriptedInput(); player.Source = input; player.InputEnabled = false;
            long t0 = boot.Sim.State.Tick;
            input.Press(PlayerKeys.StepTick); player.Step(0.02f);
            Assert.AreEqual(t0 + 1, boot.Sim.State.Tick, ". steps one tick while paused");
            input.Shift = true; input.Press(PlayerKeys.StepTick); player.Step(0.02f); input.Shift = false;
            Assert.AreEqual(t0 + 11, boot.Sim.State.Tick, "Shift+. steps one second");
            input.Press(PlayerKeys.TogglePause); player.Step(0.02f);
            Assert.IsTrue(boot.Running, "Space resumes");
            input.Press(PlayerKeys.SpeedUp); player.Step(0.02f);
            Assert.AreEqual(2f, boot.Speed);
            Time.captureFramerate = 30;
            boot.Speed = 1f;
            long a0 = boot.Sim.State.Tick;
            hud.ToggleAccelerated();
            yield return U1.Frames(30);
            long fast = boot.Sim.State.Tick - a0;
            Assert.Greater(fast, 90, "accelerated runs well above 1x (1x would be 10 ticks a second: " + fast + " in one second)");
            Assert.Greater(hud.AchievedSpeed, 2f);
            hud.SetSpeed(1f);
            Assert.IsFalse(hud.Accelerated, "choosing a speed leaves accelerated mode");
        }

        [UnityTest]
        public IEnumerator GodOnlyFlag_BootsTheLivingIsland_WithNoHumanFaction()
        {
            // what -godonly does: GodOnly mode, no human faction, a population
            yield return U1.Load(3, true);
            var boot = GodBootstrap.Instance;
            Assert.IsTrue(boot.GodOnly);
            Assert.IsFalse(boot.Hud.CanAct, "no faction tools in god-only mode");
            Assert.IsNotNull(boot.Sim.State.Population);
            Assert.IsTrue(boot.Director.Driving);
        }
    }
}
