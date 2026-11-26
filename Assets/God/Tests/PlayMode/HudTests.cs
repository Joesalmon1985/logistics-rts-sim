using System.Collections;
using System.IO;
using System.Linq;
using God.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace God.Game.Tests
{
    /// <summary>Drives the god HUD through its public methods (what the mouse and keys call) in the real scene.</summary>
    public class HudTests
    {
        private GodBootstrap _boot;
        private GodHud _hud;
        private GodSim Sim => _boot.Sim;
        private TerritoryState T => Sim.State.Territory;

        private IEnumerator Load(bool pause = true)
        {
            yield return SceneManager.LoadSceneAsync("Assets/God/Scenes/Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            _boot = GodBootstrap.Instance;
            _hud = _boot.Hud;
            Assert.IsNotNull(_hud, "the bootstrap builds the HUD");
            _boot.Running = pause ? false : _boot.Running; // paused: a queued command is applied by the bootstrap within one frame
        }

        /// <summary>A small building of F1 beside its HQ, placed through the sim (there is no pre-placed enemy base any more).</summary>
        private Building EnemySmall()
        {
            var hq = T.HqsOf(Faction.F1)[0]; var g = T.Grid;
            int node = T.BuildableNodes(Faction.F1).First(n => g.Distance(n, hq.Node) >= 3 && Sim.Check(new PlaceBuilding(Faction.F1, n, BuildingSize.Small)).Ok);
            Assert.IsTrue(Sim.Submit(new PlaceBuilding(Faction.F1, node, BuildingSize.Small)).Ok);
            Sim.Step();
            return T.BuildingAtNode(node);
        }

        /// <summary>Ends the game with one faction left: every other HQ is destroyed by the sim (test set-up).</summary>
        private void LeaveOnly(Faction survivor)
        {
            var s = Sim.State;
            foreach (var h in T.Hqs.ToList())
                if (h.Faction != survivor) MilitaryRules.Destroy(s, s.Military.BuildingById(h.Id), new EventBuffer());
            Sim.Step(2);
        }

        private int LegalNode(string kind) =>
            T.BuildableNodes(Faction.F0).FirstOrDefault(n => Sim.Check(new PlaceBuilding(Faction.F0, n, kind)).Ok);

        /// <summary>A woodcutter when the start circle touches forest, else the first harvester kind its land allows.</summary>
        private string Harvester()
        {
            foreach (var k in new[] { BuildingKinds.Woodcutter, BuildingKinds.ClayPit, BuildingKinds.Quarry, BuildingKinds.Farm, BuildingKinds.Mine })
                if (LegalNode(k) > 0) return k;
            Assert.Fail("no harvester can stand in the start circle");
            return null;
        }

        private IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        [UnityTest]
        public IEnumerator CloudStrip_ShowsTheSimCalendar_DayAndYear()
        {
            // M0: the strip shows the sim day and year (SimCalendar) with a bar for the day and a pip per day of the year.
            yield return Load(true);
            _boot.Running = false;
            Assert.AreEqual(1200, Sim.State.Setup.TicksPerDay);
            long t0 = Sim.State.Tick;
            Assert.Less(t0, 1200, "a new game starts on its first day");
            Assert.AreEqual("Day 1, year 1", _hud.CalendarText());
            Sim.Step((int)(1800 - t0));
            Sim.DrainEvents();
            Assert.AreEqual("Day 2, year 1", _hud.CalendarText());
            var (frac, day, days) = _hud.CalendarProgress();
            Assert.AreEqual(0.5f, frac, 0.001f, "half way through the day");
            Assert.AreEqual(1, day); Assert.AreEqual(10, days);
            Sim.Step(12000);
            Sim.DrainEvents();
            Assert.AreEqual("Day 2, year 2", _hud.CalendarText());
            yield return Frames(3); // and the strip draws it without errors
        }

        [UnityTest]
        public IEnumerator Game_RunsByDefault_PauseAndSpeedButtonsWork()
        {
            yield return Load(false);
            Assert.IsTrue(_boot.Running, "the sim runs at the start");
            Assert.AreEqual(1f, _boot.Speed);
            for (int i = 0; i < 300 && Sim.State.Tick == 0; i++) yield return null;
            Assert.Greater(Sim.State.Tick, 0, "ticks advance without any key");
            _hud.TogglePause();
            Assert.IsTrue(_hud.Paused);
            long tick = Sim.State.Tick;
            yield return Frames(5);
            Assert.AreEqual(tick, Sim.State.Tick, "paused");
            _hud.SetSpeed(4f);
            Assert.IsTrue(_boot.Running, "choosing a speed resumes");
            Assert.AreEqual(4f, _boot.Speed);
            _hud.StepSpeed(-1); Assert.AreEqual(2f, _boot.Speed);
            _hud.StepSpeed(-1); Assert.AreEqual(1f, _boot.Speed);
            _hud.StepSpeed(-1); Assert.AreEqual(1f, _boot.Speed);
        }

        [UnityTest]
        public IEnumerator PlaceWoodcutter_ThroughTheBuildMenu_ShowsLegalSpots_AndPlaces()
        {
            yield return Load();
            string kind = Harvester();
            _hud.BeginPlace(kind);
            Assert.AreEqual(HudMode.Place, _hud.Mode);
            yield return Frames(2);
            int legal = T.BuildableNodes(Faction.F0).Count(n => Sim.Check(new PlaceBuilding(Faction.F0, n, kind)).Ok);
            Assert.AreEqual(legal, _hud.LegalSpotCount, "the markers are exactly the spots the rules allow");
            Assert.Greater(_hud.Overlay.SpotVertices, 0);
            Assert.IsTrue(_boot.Territory.OverlayVisible == true, "the circles and dots show while placing");

            int before = T.Buildings.Count;
            int node = LegalNode(kind);
            var r = _hud.WorldClick(node, 0);
            Assert.IsTrue(r.Ok, r.ToString());
            Assert.AreEqual(HudMode.Normal, _hud.Mode, "the tool is put down after a placement");
            yield return Frames(3);
            Assert.AreEqual(before + 1, T.Buildings.Count);
            Assert.AreEqual(kind, T.BuildingAtNode(node).Kind);
            Assert.IsFalse(_boot.Territory.OverlayVisible, "the debug overlay is hidden in normal play");
        }

        [UnityTest]
        public IEnumerator Placing_WhilePaused_StillShowsTheBuilding()
        {
            yield return Load();
            Assert.IsTrue(_hud.Paused);
            int node = LegalNode(BuildingKinds.Sawmill);
            _hud.BeginPlace(BuildingKinds.Sawmill);
            Assert.IsTrue(_hud.WorldClick(node, 0).Ok);
            long tick = Sim.State.Tick;
            yield return Frames(3);
            Assert.IsNotNull(T.BuildingAtNode(node), "a queued command is applied even when paused");
            Assert.LessOrEqual(Sim.State.Tick - tick, 2, "by one tick, not by running on");
        }

        [UnityTest]
        public IEnumerator Refusal_ShowsAToastWithTheReason()
        {
            yield return Load();
            _hud.BeginPlace(BuildingKinds.Sawmill);
            var water = Enumerable.Range(0, T.Grid.Count).First(n => !T.Grid.IsLand(n));
            var r = _hud.WorldClick(water, 0);
            Assert.IsFalse(r.Ok);
            Assert.IsTrue(_hud.ToastActive);
            Assert.AreEqual(Reasons.Sentence(Reasons.NotBuildable), _hud.ToastText);
            Assert.AreEqual(HudMode.Place, _hud.Mode, "the tool stays after a refusal");
            _hud.Cancel();
            Assert.AreEqual(HudMode.Normal, _hud.Mode);
        }

        [UnityTest]
        public IEnumerator HqPlacing_MarksTheLegalCorners()
        {
            yield return Load();
            _hud.BeginHq();
            yield return Frames(2);
            int legal = Enumerable.Range(0, T.Topology.CornerCount).Count(c => Sim.Check(new PlaceHq(Faction.F0, c)).Ok);
            Assert.Greater(legal, 0);
            Assert.AreEqual(legal, _hud.LegalSpotCount);
            int corner = Enumerable.Range(0, T.Topology.CornerCount).First(c => Sim.Check(new PlaceHq(Faction.F0, c)).Ok);
            var r = _hud.WorldClick(T.Grid.CornerNode(corner), 0);
            Assert.IsTrue(r.Ok, r.ToString());
            yield return Frames(3);
            Assert.IsTrue(T.Hqs.Any(h => h.Faction == Faction.F0 && h.Corner == corner && h.Site), "an HQ site stands on the corner");
        }

        [UnityTest]
        public IEnumerator PlacingABuilding_StartsARoadAtItsFlag_OneClickOnTheHqFlagJoinsIt()
        {
            yield return Load();
            var hq = T.HqsOf(Faction.F0)[0];
            var hqFlag = T.Buildings.First(b => b.Id == hq.Id).FlagNode;
            int node = LegalNode(BuildingKinds.Sawmill);
            _hud.BeginPlace(BuildingKinds.Sawmill);
            Assert.IsTrue(_hud.WorldClick(node, 0).Ok);
            yield return Frames(3);
            var site = T.BuildingAtNode(node);
            Assert.IsNotNull(site);
            Assert.AreEqual(HudMode.Road, _hud.Mode, "the road tool is picked up by itself");
            Assert.AreEqual(1, _hud.RoadNodes.Count);
            Assert.AreEqual(site.FlagNode, _hud.RoadNodes[0], "starting at the new building's flag");
            Assert.IsFalse(_hud.FlagHasRoad(site.FlagNode));

            var r = _hud.WorldClick(hqFlag, hq.Id);
            Assert.IsTrue(r.Ok, r.ToString());
            yield return Frames(3);
            Assert.IsTrue(_hud.FlagHasRoad(site.FlagNode), "one click on the HQ joins it");
            Assert.AreEqual(0, _hud.RoadNodes.Count);

            // The R key path and the card button both use the same tool.
            _hud.Cancel(); _hud.Cancel();
            _hud.ToggleTool(HudMode.Road);
            Assert.AreEqual(HudMode.Road, _hud.Mode);
            _hud.ToggleTool(HudMode.Road);
            Assert.AreEqual(HudMode.Normal, _hud.Mode);
        }

        [UnityTest]
        public IEnumerator RoadTool_BuildsARoadBetweenTwoFlags_AndPlacesAFlagOnARoad()
        {
            yield return Load();
            var hq = T.HqsOf(Faction.F0)[0];
            var hqFlag = T.Buildings.First(b => b.Id == hq.Id).FlagNode;
            int node = LegalNode(BuildingKinds.Sawmill);
            _hud.BeginPlace(BuildingKinds.Sawmill);
            Assert.IsTrue(_hud.WorldClick(node, 0).Ok);
            yield return Frames(3);
            var site = T.BuildingAtNode(node);
            Assert.IsNotNull(site);

            int roads = T.Roads.Count;
            _hud.BeginRoad();
            Assert.IsTrue(_hud.WorldClick(hqFlag, 0).Ok, "first click: the HQ flag");
            Assert.AreEqual(1, _hud.RoadNodes.Count);
            // A click beside the target, then on the building itself, ends at its flag.
            var r = _hud.WorldClick(node, site.Id);
            Assert.IsTrue(r.Ok, r.ToString());
            yield return Frames(3);
            Assert.AreEqual(roads + 1, T.Roads.Count, "the road is built");
            Assert.AreEqual(0, _hud.RoadNodes.Count);
            var road = T.Roads.Last();
            Assert.IsTrue(road.Nodes.Contains(hqFlag) && road.Nodes.Contains(site.FlagNode));

            // A flag on the new road, three or more steps from the other flags.
            int mid = road.Nodes.Skip(2).Take(road.Nodes.Length - 4).FirstOrDefault(n => Sim.Check(new PlaceFlag(Faction.F0, n)).Ok);
            if (mid > 0)
            {
                int flags = T.Flags.Count;
                Assert.IsTrue(_hud.WorldClick(mid, 0).Ok);
                yield return Frames(3);
                Assert.AreEqual(flags + 1, T.Flags.Count, "clicking a road with the road tool puts a flag on it");
            }
        }

        [UnityTest]
        public IEnumerator RoadTool_ExtendsStepByStep_AndTheFreeEndFinishes()
        {
            yield return Load();
            var hq = T.HqsOf(Faction.F0)[0];
            int hqFlag = T.Buildings.First(b => b.Id == hq.Id).FlagNode;
            var g = T.Grid;
            // Two free nodes a few steps away on land inside the circle.
            var free = T.BuildableNodes(Faction.F0).Where(n => T.NodeAt(n).IsFree && g.Distance(hqFlag, n) >= 3).ToList();
            int far = free.First(n => g.Distance(hqFlag, n) == 4);
            _hud.BeginRoad();
            _hud.WorldClick(hqFlag, 0);
            var r = _hud.WorldClick(far, 0);
            Assert.IsTrue(r.Ok);
            Assert.AreEqual(5, _hud.RoadNodes.Count, "the path was laid by FindRoadPath, one node per step");
            Assert.AreEqual(far, _hud.RoadEnd);
            Assert.AreEqual(0, T.Roads.Count, "nothing is sent until the road is finished");
            r = _hud.WorldClick(far, 0); // the free end again: finish
            Assert.IsTrue(r.Ok, r.ToString());
            yield return Frames(3);
            Assert.AreEqual(1, T.Roads.Count);
            Assert.IsNotNull(T.FlagAtNode(far), "the free end got a flag");

            // Esc undoes a road in progress, keeping the tool; with only its first flag it puts the tool down.
            _hud.BeginRoad();
            _hud.WorldClick(hqFlag, 0);
            Assert.AreEqual(1, _hud.RoadNodes.Count);
            bool clear(int n) => T.NodeFlag[n] == 0 && T.NodeRoad[n] == 0
                && Enumerable.Range(0, 6).Select(d => g.Neighbour(n, d)).All(nb => nb < 0 || (T.NodeFlag[nb] == 0 && T.NodeRoad[nb] == 0));
            int near = T.BuildableNodes(Faction.F0).Where(n => g.Distance(n, hqFlag) >= 3 && clear(n) && _hud.RoadPreview(n) != null)
                .OrderBy(n => g.Distance(n, hqFlag)).First();
            _hud.WorldClick(near, 0);
            Assert.Greater(_hud.RoadNodes.Count, 1);
            _hud.Cancel();
            Assert.AreEqual(0, _hud.RoadNodes.Count);
            Assert.AreEqual(HudMode.Road, _hud.Mode);
            _hud.Cancel();
            Assert.AreEqual(HudMode.Normal, _hud.Mode);
            _hud.BeginRoad();
            _hud.WorldClick(hqFlag, 0);
            _hud.Cancel();
            Assert.AreEqual(HudMode.Normal, _hud.Mode, "a road with only its first flag: one Esc puts the tool down");
        }

        [UnityTest]
        public IEnumerator DemolishTool_RemovesABuilding_ButNotTheHq()
        {
            yield return Load();
            int node = LegalNode(BuildingKinds.Sawmill);
            _hud.BeginPlace(BuildingKinds.Sawmill);
            _hud.WorldClick(node, 0);
            yield return Frames(3);
            _hud.BeginDemolish();
            var hq = T.HqsOf(Faction.F0)[0];
            Assert.IsFalse(_hud.WorldClick(hq.Node, hq.Id).Ok, "the HQ stays");
            Assert.AreEqual(Reasons.Sentence(Reasons.CannotRemove), _hud.ToastText);
            Assert.IsTrue(_hud.WorldClick(node, T.BuildingAtNode(node).Id).Ok);
            yield return Frames(3);
            Assert.IsNull(T.BuildingAtNode(node));
        }

        [UnityTest]
        public IEnumerator Card_SetsPriorityAndPause()
        {
            yield return Load();
            int node = LegalNode(BuildingKinds.Sawmill);
            _hud.BeginPlace(BuildingKinds.Sawmill);
            _hud.WorldClick(node, 0);
            yield return Frames(3);
            var b = T.BuildingAtNode(node);
            Assert.AreEqual(HudMode.Road, _hud.Mode, "placing picks up the road tool");
            _hud.Cancel(); // one right-click drops it
            Assert.AreEqual(HudMode.Normal, _hud.Mode);
            _hud.WorldClick(node, b.Id); // Normal mode: opens its card
            Assert.AreEqual(b.Id, _hud.SelectedBuilding);

            Assert.IsTrue(_hud.SetPriority(b.Id, Ware.Log, Priority.High).Ok);
            Assert.IsTrue(_hud.TogglePaused(b.Id).Ok);
            yield return Frames(3);
            var e = Sim.State.Economy.Find(b.Id);
            Assert.AreEqual(Priority.High, e.Prio[(int)Ware.Log]);
            Assert.IsTrue(e.Paused);
            Assert.IsTrue(_hud.TogglePaused(b.Id).Ok);
            yield return Frames(3);
            Assert.IsFalse(e.Paused);
            Assert.IsFalse(_hud.SetPriority(b.Id, Ware.Iron, Priority.Low).Ok, "a ware the building does not take is refused");
            Assert.IsTrue(_hud.ToastActive);
        }

        [UnityTest]
        public IEnumerator EnemyCard_DeclaresWar_ThenMarksAndUnmarksATarget_ThenMakesPeace()
        {
            yield return Load();
            var m = Sim.State.Military; var fs = Sim.State.Factions; var me = Faction.F0;
            var enemy = EnemySmall();
            _hud.WorldClick(enemy.Node, enemy.Id);
            Assert.AreEqual(enemy.Id, _hud.SelectedBuilding);
            Assert.IsFalse(_hud.ToggleTarget(enemy.Id).Ok, "no target without a war");
            Assert.IsTrue(_hud.DeclareWarOn(Faction.F1).Ok);
            yield return Frames(3);
            Assert.IsTrue(fs.AtWar(me, Faction.F1));
            Assert.IsTrue(_hud.ArmyText().Contains("at war with Azure"), _hud.ArmyText());
            Assert.IsTrue(_hud.ToggleTarget(enemy.Id).Ok);
            yield return Frames(3);
            Assert.AreEqual(1, m.MarkOrder(me, enemy.Id), "marked, with its order number");
            var hqE = T.HqsOf(Faction.F1)[0];
            Assert.IsTrue(_hud.ToggleTarget(hqE.Id).Ok);
            yield return Frames(3);
            Assert.AreEqual(2, m.MarkOrder(me, hqE.Id));
            Assert.IsTrue(_hud.ToggleTarget(enemy.Id).Ok);
            yield return Frames(3);
            Assert.AreEqual(0, m.MarkOrder(me, enemy.Id));
            Assert.AreEqual(1, m.MarkOrder(me, hqE.Id));
            Assert.IsTrue(_hud.OfferPeaceTo(Faction.F1).Ok);
            Sim.Submit(new OfferPeace(Faction.F1, me));
            yield return Frames(3);
            Assert.IsFalse(fs.AtWar(me, Faction.F1), "both offered: peace");
            Assert.AreEqual(0, m.MarkOrder(me, hqE.Id), "peace drops the marks");
            Assert.IsFalse(_hud.ToggleTarget(T.HqsOf(Faction.F0)[0].Id).Ok, "your own buildings cannot be targets");
        }

        [UnityTest]
        public IEnumerator Defend_FlagOnABuilding_AndOnTheGround_WithGuardCounts()
        {
            yield return Load();
            var m = Sim.State.Military;
            var hq = T.HqsOf(Faction.F0)[0];
            Assert.IsTrue(_hud.SetGuards(hq.Id, 3).Ok);
            yield return Frames(3);
            Assert.AreEqual(3, m.DefendAtBuilding(hq.Id).Guards);
            Assert.IsTrue(_hud.SetGuards(hq.Id, 5).Ok);
            yield return Frames(3);
            Assert.AreEqual(5, m.DefendAtBuilding(hq.Id).Guards, "a second order changes the count");
            Assert.IsTrue(_hud.SetGuards(hq.Id, 0).Ok);
            yield return Frames(3);
            Assert.IsNull(m.DefendAtBuilding(hq.Id), "0 guards takes the flag away");

            int node = T.BuildableNodes(Faction.F0).First(n => T.NodeAt(n).IsFree);
            _hud.WorldClick(node, 0);
            Assert.AreEqual(node, _hud.SelectedNode, "clicking ground opens the Defend here card");
            Assert.IsTrue(_hud.SetGuardsAt(node, 2).Ok);
            yield return Frames(3);
            Assert.AreEqual(2, m.DefendAtNode(Faction.F0, node).Guards);
            Assert.IsFalse(_hud.SetGuardsAt(node, 9).Ok);
        }

        [UnityTest]
        public IEnumerator WinCard_AppearsWhenYoureLastStanding_AndPlayAgainKeepsTheSeed()
        {
            yield return Load();
            Assert.AreEqual(GameOutcome.Playing, _hud.Outcome);
            var s = Sim.State;
            LeaveOnly(Faction.F0); // the sim, not the HUD, destroys them
            yield return Frames(5);
            Assert.AreEqual(GameOutcome.Won, _hud.Outcome);
            yield return new WaitForEndOfFrame();
            Assert.IsTrue(_hud.PanelRects.Any(r => r.width >= Screen.width / _hud.Scale - 2f), "the end card dims the whole view");
            long tick = s.Tick;
            yield return Frames(5);
            Assert.AreEqual(tick, s.Tick, "the game stops when it is over");

            Assert.AreEqual(_boot.Seed, _boot.PrepareRestart(false), "Play again keeps the seed");
            Assert.AreEqual(_boot.Seed, GodBootstrap.PendingSeed);
            Assert.AreNotEqual(_boot.Seed, _boot.PrepareRestart(true), "New island changes it");
            GodBootstrap.PendingSeed = null;
        }

        [UnityTest]
        public IEnumerator LoseCard_AppearsWhenYourHqFalls()
        {
            yield return Load();
            var s = Sim.State;
            MilitaryRules.Destroy(s, s.Military.BuildingById(T.HqsOf(Faction.F0)[0].Id), new EventBuffer());
            Sim.Step(2);
            yield return Frames(3);
            Assert.AreEqual(GameOutcome.Playing, _hud.Outcome, "five factions play on");
            Assert.IsFalse(_hud.CanAct, "but you are out");
            LeaveOnly(Faction.F1);
            yield return Frames(5);
            Assert.AreEqual(GameOutcome.Lost, _hud.Outcome);
        }

        [UnityTest]
        public IEnumerator Camera_ZoomsAlongTheRay_Pans_AndStaysOverTheIsland()
        {
            yield return Load();
            yield return Frames(2);
            var rig = _boot.Rig; var cam = _boot.GodCamera;
            rig.InputEnabled = false;
            var centre = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            float pitch = cam.transform.eulerAngles.x;
            float h0 = cam.transform.position.y;
            rig.Zoom(5f, centre);
            Assert.Less(cam.transform.position.y, h0, "the wheel zooms in");
            Assert.GreaterOrEqual(cam.transform.position.y, GodCameraRig.MinHeight - 0.01f);
            Assert.AreEqual(pitch, cam.transform.eulerAngles.x, 0.01f, "the tilt is kept");
            rig.Zoom(-60f, centre);
            Assert.LessOrEqual(cam.transform.position.y, rig.MaxHeight + 0.01f, "cannot zoom out past the framed island");
            rig.Zoom(8f, centre);
            var p0 = cam.transform.position;
            rig.PanBy(new Vector3(40f, 0f, 0f));
            Assert.AreEqual(p0.x + 40f, cam.transform.position.x, 0.5f);
            rig.PanBy(new Vector3(5000f, 0f, 5000f));
            Assert.IsTrue(rig.GroundPoint(centre, out var c));
            Assert.Less(new Vector2(c.x - rig.IslandCentre.x, c.z - rig.IslandCentre.z).magnitude, rig.IslandRadius + 1f, "the island stays in view");
            Assert.IsTrue(rig.GroundPoint(centre, out var gp));
            Assert.IsTrue(_hud.NodeAt(gp) >= -1);
        }

        [UnityTest]
        public IEnumerator Strip_TakesAboutAnEighthOfTheHeight_AndTheTooltipsNameCostTakesGives()
        {
            yield return Load();
            yield return Frames(3);
            yield return new WaitForEndOfFrame();
            var strip = _hud.PanelRects.First(r => r.x == 0f && r.y == 0f);
            float frac = strip.height * _hud.Scale / Screen.height;
            Assert.That(frac, Is.InRange(0.08f, 0.15f), "the strip is a compact band along the top");
            string tip = GodHud.KindTip(EconomyDefs.Get(BuildingKinds.Sawmill));
            StringAssert.Contains("Cost:", tip); StringAssert.Contains("Takes: 1 log", tip); StringAssert.Contains("Gives:", tip);
        }

        /// <summary>Screenshots for the report (game view). Skipped unless run in an editor with a game view.</summary>
        [UnityTest]
        public IEnumerator Screenshots()
        {
            yield return Load();
            string dir = System.Environment.GetEnvironmentVariable("P5_SHOTS");
            if (string.IsNullOrEmpty(dir)) dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "LocalData"));
            Directory.CreateDirectory(dir);
            _boot.Running = false;
            var cam = _boot.GodCamera; _boot.Rig.InputEnabled = false;
            var hq = T.HqsOf(Faction.F0)[0];
            var g = T.Grid;
            var hqPos = new Vector3((float)g.X(hq.Node), 0f, (float)g.Z(hq.Node));

            // Build a small base fast: a sawmill, a woodcutter or quarry, roads.
            string kind = Harvester();
            _hud.BeginPlace(BuildingKinds.Sawmill); _hud.WorldClick(LegalNode(BuildingKinds.Sawmill), 0);
            yield return Frames(2);
            _hud.BeginPlace(kind); _hud.WorldClick(LegalNode(kind), 0);
            yield return Frames(2);
            int hqFlag = T.Buildings.First(b => b.Id == hq.Id).FlagNode;
            foreach (var b in T.Buildings.Where(b => b.Faction == Faction.F0 && b.Id != hq.Id).ToList())
            {
                _hud.BeginRoad(); _hud.WorldClick(hqFlag, 0); _hud.WorldClick(b.Node, b.Id);
                yield return Frames(2);
            }
            _hud.Cancel(); _hud.ClearToast();
            Sim.Step(600); // let the site fill in a little
            yield return Frames(2);
            SetCamera(cam, hqPos, 95f);

            // 1. Placing: the legal spots and the ghost.
            _hud.BeginPlace(BuildingKinds.Kiln);
            var spots = T.BuildableNodes(Faction.F0).Where(n => Sim.Check(new PlaceBuilding(Faction.F0, n, BuildingKinds.Kiln)).Ok).ToList();
            int hover = spots.OrderBy(n => (new Vector3((float)g.X(n), 0, (float)g.Z(n)) - hqPos).magnitude).Skip(spots.Count / 3).First();
            ForceHover(hover);
            yield return Shot(Path.Combine(dir, "p5_build_menu.png"));

            // 2. A building card with priorities.
            _hud.Cancel();
            var kiln = T.Buildings.First(b => b.Faction == Faction.F0 && b.Kind == BuildingKinds.Sawmill);
            _hud.ClearToast();
            _hud.SelectBuilding(kiln.Id);
            Sim.Submit(new SetInputPriority(Faction.F0, kiln.Id, Ware.Log, Priority.High)); Sim.Step(2);
            yield return Shot(Path.Combine(dir, "p5_card.png"));

            // 3. An enemy card with a target.
            _hud.Cancel();
            var enemy = EnemySmall();
            _hud.DeclareWarOn(Faction.F1); Sim.Step(1);
            _hud.ToggleTarget(enemy.Id); Sim.Step(2);
            var ep = new Vector3((float)g.X(enemy.Node), 0f, (float)g.Z(enemy.Node));
            SetCamera(cam, ep, 95f);
            _hud.SelectBuilding(enemy.Id);
            yield return Shot(Path.Combine(dir, "p5_enemy_card.png"));

            // 4. The win card.
            _hud.Cancel();
            LeaveOnly(Faction.F0);
            _boot.Rig.Reframe(BoardSpace.FrameRadius + 40f, BoardSpace.BoardCentre);
            yield return Shot(Path.Combine(dir, "p5_win.png"));
        }

        private void ForceHover(int node)
        {
            // Aim the cursor at the node so the hover logic of the HUD runs on it (no real mouse in tests).
            var g = T.Grid;
            var sp = _boot.GodCamera.WorldToScreenPoint(new Vector3((float)g.X(node), 0f, (float)g.Z(node)));
            _hud.DebugCursor = new Vector2(sp.x, sp.y);
        }

        private static void SetCamera(Camera cam, Vector3 focus, float dist)
        {
            var dir = new Vector3(0f, Mathf.Sin(IslandCamera.Pitch * Mathf.Deg2Rad), -Mathf.Cos(IslandCamera.Pitch * Mathf.Deg2Rad));
            cam.transform.position = focus + dir * dist;
            cam.transform.rotation = Quaternion.LookRotation(-dir, Vector3.up);
        }

        private IEnumerator Shot(string file)
        {
            for (int i = 0; i < 3; i++) yield return null;
            yield return new WaitForEndOfFrame();
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(file, tex.EncodeToPNG());
            Object.Destroy(tex);
            Debug.Log("P5 screenshot: " + file);
        }
    }
}
