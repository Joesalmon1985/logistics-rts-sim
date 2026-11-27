using System.Collections;
using System.Collections.Generic;
using System.Linq;
using God.Sim;
using God.Sim.Divinity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace God.Game.Tests
{
    /// <summary>
    /// U2a PlayMode tests (batchmode, no OnGUI): the god-only Living island with gods, the Mythic meter, the gating, the catalogue's ghost, sending a
    /// command, the refusal display, pending chips while paused, the Divine tab, the cues. D2 and D3 handlers are not on this branch, so the
    /// real instruct and miracle flows are refused by the gate (shown gracefully); a test-only spend command stands in to exercise the accept path.
    /// </summary>
    public class DivineHudPlayTests
    {
        private sealed class SpendFactory : IDivineCommandFactory
        {
            public Command Create(DivineRequest r) => new DivineTestSpend { God = r.God, Kind = r.Act == DivineActs.Miracle ? InterventionKind.Miracle : InterventionKind.Command, Cost = 4000 };
        }

        [SetUp] public void SetUp() { U1.Reset(); }

        [TearDown]
        public void TearDown() { U1.Reset(); GodBootstrap.ForceGodOnly = false; GameMenu.LivingIslandGodOnly = true; }

        /// <summary>The god-only Living island as the menu deals it (control god-only, divinity on, the first faction's god holds the seat).</summary>
        private static IEnumerator LoadGod(ulong seed, DivinityMode mode = DivinityMode.Standard, bool testSpend = true)
        {
            GodBootstrap.SkipStartCard = true; GodBootstrap.StartRunning = false;
            GodBootstrap.PendingSeed = seed;
            GodBootstrap.PendingSetup = new GameSetup(seed, GameSetup.NoHuman)
            {
                Population = true, StartNpcsPerFaction = 20, Victory = VictorySpec.SandboxOnly(), Control = PlayerControl.GodOnly, HumanGod = 1,
            };
            var tuning = new SimTuning();
            tuning.Divinity.Enabled = 1; tuning.Divinity.Mode = mode;
            if (testSpend) tuning.Divinity.Mythic.TestCommand = 1;
            GodBootstrap.PendingTuning = tuning;
            GodBootstrap.PendingStartAi = true;
            yield return SceneManager.LoadSceneAsync(U1.Scene, new LoadSceneParameters(LoadSceneMode.Single));
            for (int i = 0; i < 4; i++) yield return null;
            GodBootstrap.Instance.Running = false;
        }

        private static int OtherCommunityPerson(GodBootstrap boot)
        {
            foreach (var n in boot.Sim.State.Population.Npcs)
                if (n.Alive && n.Faction != Faction.F0 && boot.People.TryPosition(n.Id, out _)) return n.Id;
            return 0;
        }

        [UnityTest]
        public IEnumerator GodOnly_HidesFactionPaths_AndStage3StillHasThem()
        {
            yield return LoadGod(11);
            var boot = GodBootstrap.Instance; var hud = boot.Hud;
            Assert.IsTrue(hud.DivineOn); Assert.IsFalse(hud.FactionControlsOn); Assert.IsFalse(hud.CanAct);
            hud.BeginPlace("woodcutter"); hud.BeginHq(); hud.BeginRoad(); hud.BeginDemolish();
            Assert.AreEqual(HudMode.Normal, hud.Mode, "no build tool can be picked up");
            int pending = boot.PendingCommandCount;
            var r = hud.DeclareWarOn(Faction.F1);
            Assert.IsFalse(r.Ok); Assert.AreEqual("god_only", r.Reason);
            Assert.IsFalse(hud.OfferPeaceTo(Faction.F1).Ok); Assert.IsFalse(hud.SetGuardsAt(5, 2).Ok);
            Assert.AreEqual(pending, boot.PendingCommandCount, "nothing faction-side reached the gate's queue");
            var ids = hud.PersonTabIds();
            Assert.Contains("divine", ids);

            yield return U1.Load(12, godOnly: false, human: true);
            var h2 = GodBootstrap.Instance.Hud;
            Assert.IsTrue(h2.FactionControlsOn, "the stage 3 game keeps its faction controls");
            Assert.IsFalse(h2.DivineOn);
            Assert.IsTrue(h2.CanAct);
        }

        [UnityTest]
        public IEnumerator Meter_ShowsBalanceRegenAndTimeToFull_FromTheDivineView()
        {
            yield return LoadGod(13);
            var hud = GodBootstrap.Instance.Hud;
            Assert.IsTrue(hud.TryGod(out var g));
            Assert.AreEqual(50_000, g.Mythic); Assert.AreEqual(100_000, g.MythicMax); Assert.AreEqual(20_000, g.RegenPerDay);
            StringAssert.Contains("Mythic 50 / 100", hud.MeterText());
            StringAssert.Contains("+20 a day", hud.MeterText());
            StringAssert.Contains("full in 2.5 days", hud.MeterText());
            hud.Arm("command");
            Assert.AreEqual(4_000, hud.ArmedCostMilli() * 1 / 1, "the ghost segment is the armed power's cost from the sim's table");
            hud.CancelArmed();
            Assert.IsNull(hud.Armed);
        }

        [UnityTest]
        public IEnumerator MythicModes_UnlimitedShowsInfinity_DisabledHidesPowersAndTheGateRefuses()
        {
            yield return LoadGod(14, DivinityMode.Unlimited);
            var boot = GodBootstrap.Instance; var hud = boot.Hud;
            StringAssert.Contains("∞", hud.MeterText());
            boot.Player.Builder.Factory = new SpendFactory();
            int p = OtherCommunityPerson(boot);
            boot.Selection.SetPerson(p);
            var res = hud.SendRequest(PlayerCommandBuilder.Request(1, new DivineIntent(DivineActs.Command, (int)DivineAct.Gather, 100), boot.Selection.Snapshot()), "Command");
            Assert.IsTrue(res.Ok, res.Message);
            boot.StepTicks(1);
            Assert.IsTrue(hud.TryGod(out var g));
            Assert.AreEqual(g.MythicMax, g.Mythic, "Unlimited: the balance is unchanged by a spend");
            Assert.IsTrue(boot.Divine.Feed.LastWaived); Assert.AreEqual(4_000, boot.Divine.Feed.LastNominal, "the spend is still shown");

            yield return LoadGod(15, DivinityMode.Disabled);
            boot = GodBootstrap.Instance; hud = boot.Hud;
            Assert.AreEqual(new[] { "inspect" }, hud.VisibleSlots().Select(s => s.Id).ToArray(), "Disabled hides the power slots");
            Assert.IsFalse(hud.Arm("command"));
            StringAssert.Contains("disabled", hud.MeterText());
            boot.Selection.SetPerson(OtherCommunityPerson(boot));
            var gate = boot.Gate.Check(new DivineTestSpend { God = 1, Kind = InterventionKind.Command, Cost = 1000 });
            Assert.IsFalse(gate.Ok); Assert.AreEqual("divinity_disabled", gate.Reason);
        }

        [UnityTest]
        public IEnumerator Card_ForAnyone_HasADivineTab_WithLayerTaggedRows()
        {
            yield return LoadGod(16);
            var boot = GodBootstrap.Instance; var hud = boot.Hud;
            int p = OtherCommunityPerson(boot);
            Assert.AreNotEqual(0, p);
            hud.SelectNpc(p);
            Assert.IsNotNull(hud.PersonCard, "a card for a person of another community");
            boot.Player.Builder.Factory = new SpendFactory();
            hud.SendRequest(PlayerCommandBuilder.Request(1, new DivineIntent(DivineActs.Command, (int)DivineAct.Gather, 100), PersonSel(p)), "Command");
            boot.StepTicks(1);
            var rows = hud.DivineRowsOf(hud.PersonCard);
            Assert.IsTrue(rows.Count >= 1, "the issued command is a row");
            foreach (var r in rows) Assert.AreNotEqual(0, (int)r.Layer, r.Head);
            Assert.IsTrue(rows.Any(r => r.Head == "Issued"));
        }

        private static SelectionSnapshot PersonSel(int id) { var m = new SelectionModel(); m.SetPerson(id); return m.Snapshot(); }

        [UnityTest]
        public IEnumerator Ghost_EqualsGateCheck_ForFiftyTargets()
        {
            yield return LoadGod(17);
            var boot = GodBootstrap.Instance; var hud = boot.Hud;
            boot.Player.Builder.Factory = new SpendFactory();
            var people = boot.Sim.State.Population.Npcs.Where(n => n.Alive).Select(n => n.Id).Take(50).ToList();
            Assert.GreaterOrEqual(people.Count, 50);
            foreach (var slotId in new[] { "command", "heal" })
            {
                var slot = DivineCatalog.Find(slotId);
                foreach (int id in people)
                {
                    var ghost = hud.EvaluateGhost(slot, id, -1);
                    var req = PlayerCommandBuilder.Request(1, DivineCatalog.Intent(slot, DivineCatalog.InstructActs[0].Act, 1200), PersonSel(id));
                    var cmd = boot.Player.Builder.Factory.Create(req);
                    Assert.AreEqual(boot.Gate.Check(cmd).Ok, ghost.Valid, slotId + " person " + id);
                }
            }
            // and with the real factory (D2/D3 handlers may or may not be merged: the ghost still equals the gate)
            boot.Player.Builder.Factory = new DivineCommandFactory();
            foreach (int id in people.Take(20))
            {
                var slot = DivineCatalog.Find("smite");
                var ghost = hud.EvaluateGhost(slot, id, -1);
                var cmd = boot.Player.Builder.Factory.Create(PlayerCommandBuilder.Request(1, DivineCatalog.Intent(slot, DivineAct.None, 0), PersonSel(id)));
                Assert.AreEqual(boot.Gate.Check(cmd).Ok, ghost.Valid);
            }
        }

        [UnityTest]
        public IEnumerator Instruct_Paused_PendingChip_ThenAcceptedAtTheNextTick()
        {
            yield return LoadGod(18);
            var boot = GodBootstrap.Instance; var hud = boot.Hud;
            boot.Player.Builder.Factory = new SpendFactory();
            int p = OtherCommunityPerson(boot);
            boot.Selection.SetPerson(p);
            hud.OpenPopover("command", DivineAct.Gather, TargetScope.Person);
            long tick = boot.Sim.State.Tick;
            var res = hud.SendPopover();
            Assert.IsTrue(res.Ok, res.Message);
            Assert.AreEqual(1, hud.Chips.Count); Assert.IsTrue(hud.Chips[0].Pending, "a pending chip");
            Assert.AreEqual(tick, boot.Sim.State.Tick, "paused: no tick advanced");
            Assert.AreEqual(1, boot.PendingCommandCount);
            Assert.AreEqual(1, hud.Chips.Count(c => c.Label.StartsWith("Command")), "exactly one command submitted");
            boot.StepTicks(1);
            Assert.IsFalse(hud.Chips[0].Pending, "resolved after the next tick");
            Assert.AreEqual(1, hud.Chips[0].Accepted);
            Assert.AreEqual(4_000, hud.Chips[0].Charged, "the real spend");
            Assert.IsTrue(hud.TryGod(out var g)); Assert.AreEqual(46_000, g.Mythic, "the meter shows the real balance");
            Assert.IsFalse(hud.PopoverOpen);
        }

        [UnityTest]
        public IEnumerator RealInstructAndMiracle_ShowTheGatesAnswerGracefully()
        {
            yield return LoadGod(19);
            var boot = GodBootstrap.Instance; var hud = boot.Hud;
            int p = OtherCommunityPerson(boot);
            boot.Selection.SetPerson(p);
            hud.OpenPopover("suggest", DivineAct.Eat, TargetScope.Person);
            var s = hud.SendPopover();
            if (!s.Ok) { Assert.IsFalse(string.IsNullOrEmpty(hud.RefusalText), "a refusal is shown, not swallowed"); StringAssert.Contains("Suggest", hud.RefusalText); }
            else Assert.AreEqual(1, hud.Chips.Count);
            var m = hud.CastOnSelection("heal");
            Assert.IsNotNull(m);
            if (!m.Ok) StringAssert.Contains("Heal", hud.RefusalText);
            Assert.AreEqual(m.Ok ? 1 : 0, m.Sent, "one command or none");
            // a compulsion on a group is refused (it forces one body) and nothing is queued
            var sel = new SelectionModel(); sel.SetGroup(new[] { p, p + 1 });
            Assert.IsNull(boot.Player.Builder.Factory.Create(new DivineRequest { God = 1, Act = DivineActs.Compel, Scope = TargetScope.Group, Persons = new[] { p, p + 1 }, Node = -1 }));
        }

        [UnityTest]
        public IEnumerator Cues_AppearForSyntheticDivineEvents()
        {
            yield return LoadGod(20);
            var boot = GodBootstrap.Instance;
            int p = OtherCommunityPerson(boot);
            var feed = boot.Divine.Feed;
            feed.Accept(new DivineActionAccepted { God = 1, Intervention = 1, Kind = InterventionKind.Suggestion, TargetKind = DivineTargetKind.Npc, TargetId = p });
            feed.Accept(new DivineActionAccepted { God = 1, Intervention = 2, Kind = InterventionKind.Command, TargetKind = DivineTargetKind.Npc, TargetId = p });
            feed.Accept(new DivineActionAccepted { God = 1, Intervention = 3, Kind = InterventionKind.Compulsion, TargetKind = DivineTargetKind.Npc, TargetId = p });
            var cues = boot.Divine.Cues;
            Assert.AreEqual(1, cues.CountOf(CueKind.Suggest)); Assert.AreEqual(1, cues.CountOf(CueKind.Command)); Assert.AreEqual(1, cues.CountOf(CueKind.Compel));
            yield return null; yield return null; // the HUD draws them in its overlay without error
            Assert.Greater(boot.Hud.Overlay.DynamicVertices, 0, "the cues are drawn");
        }

        [UnityTest]
        public IEnumerator Menu_LivingIsland_StartsTheGodOnlyGame()
        {
            yield return LoadGod(21);
            var boot = GodBootstrap.Instance;
            GameMenu.LivingIslandGodOnly = true;
            boot.Menu.MythicChoice = DivinityMode.Unlimited;
            boot.Menu.StartLivingIsland();
            for (int i = 0; i < 6; i++) yield return null;
            boot = GodBootstrap.Instance;
            var setup = boot.Sim.State.Setup;
            Assert.AreEqual(PlayerControl.GodOnly, setup.Control);
            Assert.AreEqual(1, boot.Sim.State.Tuning.Divinity.Enabled);
            Assert.AreEqual(GameSetup.NoHuman, setup.Human);
            Assert.AreEqual(1, setup.HumanGod, "the god of the first faction");
            Assert.AreEqual(1, Gods.HumanSeatHolder(boot.Sim.State));
            Assert.AreEqual(DivinityMode.Unlimited, boot.Hud.MythicMode, "the start card's Mythic choice");
            Assert.IsTrue(boot.GodOnly);
        }

        [UnityTest]
        public IEnumerator Template_Reveal_CarriesRulesNotText()
        {
            yield return LoadGod(22);
            var boot = GodBootstrap.Instance; var hud = boot.Hud;
            hud.PickTemplate(0, 2);
            var rules = hud.TemplateRules();
            Assert.AreEqual(CommandmentText.Render(rules), hud.TemplateSentence());
            var r = hud.RevealTemplate();
            // D6's handler is not on this branch: the gate refuses and the refusal is shown
            if (!r.Ok) Assert.IsFalse(string.IsNullOrEmpty(hud.RefusalText));
        }
    }
}
