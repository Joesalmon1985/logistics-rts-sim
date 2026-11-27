using System;
using System.Collections;
using System.Linq;
using System.Text;
using God.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace God.Game.Tests
{
    /// <summary>
    /// M1 in the real scene: the HUD reads through its faction's view and shows exactly what it showed when it read the
    /// state directly (M1.7, five seeds), and the true-state overlay is a view-side toggle that exists only where Q22
    /// allows it (M1.8): never in a competitive game, on offer in watch mode and in developer mode.
    /// </summary>
    public class HudKnowledgeTests
    {
        private static IEnumerator Load(ulong seed, bool watch, bool developer = false)
        {
            GodBootstrap.DeveloperMode = developer;
            GodBootstrap.PendingSeed = seed;
            GodBootstrap.PendingSetup = watch ? GameSetup.Watch(seed) : new GameSetup(seed, 0);
            GodBootstrap.PendingStartAi = true;
            yield return SceneManager.LoadSceneAsync("Assets/God/Scenes/Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            GodBootstrap.DeveloperMode = false;
            GodBootstrap.Instance.Running = false;
        }

        /// <summary>The figures the HUD showed before M1, computed from the state directly (the old code paths).</summary>
        private static string DirectFigures(GodState s, Faction me)
        {
            var sb = new StringBuilder();
            sb.Append(SimCalendar.Of(s).Label(s.Tick)).Append('|');
            int melee = 0, ranged = 0, guards = 0;
            foreach (var u in s.Military.Units) if (u.Faction == me) { if (Gear.IsRanged(u.Kit.Weapon)) ranged++; else melee++; }
            foreach (var f in s.Military.Defends) if (f.Faction == me) guards += f.Guards;
            sb.Append(melee + ranged).Append(' ').Append(ranged).Append(' ').Append(guards).Append(' ').Append(string.Join(",", s.Factions.Enemies(me))).Append('|');
            foreach (var st in FactionRules.Standings(s)) sb.Append(st.Faction).Append(st.VictoryPoints).Append(st.Hqs).Append(st.Citadels).Append(st.Eliminated).Append(string.Join(",", s.Factions.Enemies(st.Faction))).Append(';');
            sb.Append('|');
            for (int w = 1; w < Wares.Count; w++)
            {
                int best = 0; foreach (var hq in s.Economy.HqsOf(me)) best = Math.Max(best, TradeRules.FreeStock(hq, (Ware)w));
                sb.Append(s.Economy.TotalStock(me, (Ware)w)).Append('/').Append(best).Append(' ');
            }
            sb.Append('|').Append(s.Trade.PendingFor(me).Count).Append('|').Append(s.Territory.BuildableNodes(me).Count);
            foreach (var b in s.Economy.Buildings.Where(x => x.Faction == me)) sb.Append(b.Id).Append(':').Append(s.Economy.SiteProgressPermille(b.Id)).Append(',');
            return sb.ToString();
        }

        /// <summary>The same figures as the HUD reads them now (its public readouts, and its view for the rest).</summary>
        private static string HudFigures(GodHud hud)
        {
            var v = hud.View; var me = hud.Me;
            var sb = new StringBuilder();
            sb.Append(hud.CalendarText()).Append('|');
            int melee = 0, ranged = 0;
            foreach (var u in v.Military.VisibleUnits) if (u.Faction == me) { if (Gear.IsRanged(u.Kit.Weapon)) ranged++; else melee++; }
            int guards = v.Military.OwnDefends().Sum(f => f.Guards);
            sb.Append(melee + ranged).Append(' ').Append(ranged).Append(' ').Append(guards).Append(' ').Append(string.Join(",", v.Diplomacy.Enemies(me))).Append('|');
            foreach (var row in hud.StandingRows()) sb.Append(row.Faction).Append(row.Vp).Append(row.Hqs).Append(row.Citadels).Append(row.Eliminated).Append(string.Join(",", row.AtWarWith)).Append(';');
            sb.Append('|');
            for (int w = 1; w < Wares.Count; w++) sb.Append(v.Own.TotalStock((Ware)w)).Append('/').Append(hud.MaxGive((Ware)w)).Append(' ');
            sb.Append('|').Append(hud.PendingOffers().Count).Append('|').Append(v.Map.BuildableNodes().Count);
            return sb.ToString();
        }

        [UnityTest]
        public IEnumerator HudFigures_EqualTheDirectReads_FiveSeeds()
        {
            foreach (ulong seed in new ulong[] { 1, 2, 3, 4, 5 })
            {
                yield return Load(seed, false);
                var boot = GodBootstrap.Instance; var sim = boot.Sim; var hud = boot.Hud;
                for (int i = 0; i < 8000 && !sim.State.IsGameOver; i++) { sim.Step(); if (i % 500 == 0) boot.PumpEvents(); }
                boot.PumpEvents();
                yield return null;
                var me = hud.Me;
                Assert.AreEqual(Faction.F0, me);
                string direct = DirectFigures(sim.State, me);
                string viaView = HudFigures(hud);
                // The site progress list is only in the direct string; compare the shared part, then the progress per building.
                Assert.AreEqual(direct.Substring(0, viaView.Length), viaView, "seed " + seed);
                foreach (var b in sim.State.Economy.Buildings.Where(x => x.Faction == me))
                    Assert.AreEqual(sim.State.Economy.SiteProgressPermille(b.Id), hud.View.Own.SiteProgressPermille(b.Id), "seed " + seed);
                Assert.AreEqual("Soldiers " + (sim.State.Military.Units.Count(u => u.Faction == me)), hud.ArmyText().Substring(0, hud.ArmyText().IndexOf(' ', 9)), "seed " + seed);
                var standings = string.Join("   ", FactionRules.Standings(sim.State).Select(st => FactionLook.Name(st.Faction) + " " + (st.Eliminated ? "out" : st.VictoryPoints + " VP")));
                Assert.AreEqual(standings, hud.StandingsText(), "seed " + seed);
                yield return null; // and it draws
            }
        }

        [UnityTest]
        public IEnumerator Overlay_CompetitiveGameHasNoToggle_WatchAndDeveloperModeDo()
        {
            yield return Load(7, false);
            var boot = GodBootstrap.Instance; var hud = boot.Hud;
            Assert.IsFalse(boot.Sim.State.Setup.AllowTrueStateOverlay, "a game against the AIs is competitive");
            Assert.IsNull(boot.TrueState, "no overlay object exists");
            Assert.IsFalse(hud.TrueStateAvailable, "and the HUD has no toggle");
            hud.ToggleTrueState();
            Assert.IsFalse(hud.TrueStateShown);
            Assert.IsEmpty(hud.TruthRows());
            yield return null;

            yield return Load(7, true);
            boot = GodBootstrap.Instance; hud = boot.Hud;
            Assert.IsTrue(hud.TrueStateAvailable, "watch mode is an observation game");
            Assert.IsFalse(hud.TrueStateShown, "off until the player turns it on");
            ulong before = boot.Sim.StateHash();
            hud.ToggleTrueState();
            Assert.IsTrue(hud.TrueStateShown);
            for (int i = 0; i < 3; i++) yield return null;
            var rows = hud.TruthRows();
            Assert.AreEqual(6, rows.Count);
            var mine = rows.First(r => r.Faction == hud.Me);
            Assert.AreEqual(mine.TrueStock, mine.KnownStock, "own stores are known exactly");
            Assert.IsTrue(rows.Where(r => r.Faction != hud.Me).All(r => r.KnownStock == null), "foreign stores are not known (Q21)");
            Assert.AreEqual(before, boot.Sim.StateHash(), "showing the overlay changes nothing");
            hud.ToggleTrueState();
            Assert.IsFalse(hud.TrueStateShown);

            yield return Load(7, false, developer: true);
            Assert.IsTrue(GodBootstrap.Instance.Hud.TrueStateAvailable, "developer mode allows it in any game");
        }

        [UnityTest]
        public IEnumerator Paused_APersonsCommand_TakesOneOrdinaryTick()
        {
            // M1 replaces AiHost.StepHeld: no held ticks. The command shows at once; the brains are not held on that tick.
            yield return Load(3, false);
            var boot = GodBootstrap.Instance; var hud = boot.Hud; var s = boot.Sim.State;
            yield return null;
            long t0 = s.Tick;
            for (int i = 0; i < 10; i++) yield return null;
            Assert.AreEqual(t0, s.Tick, "paused");
            int node = hud.View.Map.BuildableNodes().First(n => hud.Gate.Check(new PlaceBuilding(Faction.F0, n, BuildingKinds.Sawmill)).Ok);
            hud.BeginPlace(BuildingKinds.Sawmill);
            Assert.IsTrue(hud.WorldClick(node, 0).Ok);
            for (int i = 0; i < 3; i++) yield return null;
            Assert.AreEqual(t0 + 1, s.Tick, "one tick for one command");
            Assert.IsNotNull(s.Territory.BuildingAtNode(node), "and the site shows while paused");
            for (int i = 0; i < 10; i++) yield return null;
            Assert.AreEqual(t0 + 1, s.Tick, "then it stays paused");
        }
    }
}
