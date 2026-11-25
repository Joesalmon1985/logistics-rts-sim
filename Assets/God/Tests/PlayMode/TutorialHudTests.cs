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
    /// <summary>Stage 3 (Q3): the tutorial cards, the start card's Tutorial button, the tabbed build bar, the grouped stock bar and the new models.</summary>
    public class TutorialHudTests
    {
        private const string Scene = "Assets/God/Scenes/Main.unity";
        private string _dir;

        private GodBootstrap Boot => GodBootstrap.Instance;
        private GodSim Sim => Boot.Sim;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "god_q3_" + System.Guid.NewGuid().ToString("N"));
            SaveStore.Root = _dir;
            GodBootstrap.SkipStartCard = true;
        }

        [TearDown]
        public void TearDown()
        {
            GodBootstrap.SkipStartCard = true;
            GodBootstrap.PendingLoad = null; GodBootstrap.PendingSeed = null; GodBootstrap.PendingSetup = null; GodBootstrap.PendingStartAi = false;
            SaveStore.Root = null;
            try { if (Directory.Exists(_dir)) Directory.Delete(_dir, true); } catch (IOException) { }
        }

        private static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        private IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync(Scene, new LoadSceneParameters(LoadSceneMode.Single));
            yield return Frames(2);
            Assert.IsNotNull(Boot);
        }

        /// <summary>Loads the scene as the start card's Tutorial button does, and waits for the new game.</summary>
        private IEnumerator LoadTutorial()
        {
            yield return Load();
            Boot.Menu.StartTutorial();
            yield return Frames(4);
            Assert.IsNotNull(Boot.Tutorial, "a tutorial game has its cards");
        }

        // ---------------------------------------------------------------- start card

        [UnityTest]
        public IEnumerator StartCard_TutorialButton_StartsTheTutorialIsland()
        {
            yield return LoadTutorial();
            var s = Sim.State;
            Assert.IsTrue(s.Setup.Tutorial);
            Assert.AreEqual(TutorialIsland.Seed, s.Seed);
            Assert.AreEqual(Faction.F0, s.Setup.HumanFaction, "you are Amber");
            Assert.IsFalse(Boot.Hud.Watching);
            Assert.IsTrue(Boot.Tutorial.Guide.Visible);
            Assert.AreEqual("welcome", Boot.Tutorial.ActiveStep.Id);
        }

        [UnityTest]
        public IEnumerator PlainGame_HasNoTutorial()
        {
            yield return Load();
            Assert.IsFalse(Sim.State.Setup.Tutorial);
            Assert.IsNull(Boot.Tutorial);
            Assert.IsFalse(Boot.Menu.HasTutorialTips);
        }

        // ---------------------------------------------------------------- cards

        [UnityTest]
        public IEnumerator Cards_AdvanceWhenTheGoalIsDone_AndSkipAndHideWork()
        {
            yield return LoadTutorial();
            var tc = Boot.Tutorial; var hud = Boot.Hud;
            tc.Guide.HoldTicks = 0;
            Assert.AreEqual(0, tc.Guide.Index);
            tc.Next();
            Assert.AreEqual("woodcutter", tc.ActiveStep.Id);

            // Place a woodcutter through the HUD, as a click would.
            var t = Sim.State.Territory;
            int node = t.BuildableNodes(Faction.F0).First(n => Sim.Check(new PlaceBuilding(Faction.F0, n, BuildingKinds.Woodcutter)).Ok);
            hud.BeginPlace(BuildingKinds.Woodcutter);
            Assert.IsTrue(hud.WorldClick(node, 0).Ok);
            for (float t0 = Time.realtimeSinceStartup; Time.realtimeSinceStartup - t0 < 5f && tc.ActiveStep.Id == "woodcutter";) yield return null;
            Assert.AreEqual("road", tc.ActiveStep.Id, "placing the woodcutter ticks step 2 and moves on (tick " + Sim.State.Tick + ", woodcutters "
                + Sim.State.Economy.Buildings.Count(b => b.Kind == BuildingKinds.Woodcutter) + ", met " + tc.Guide.GoalMet + ", hold " + tc.Guide.HoldTicks + ", running " + Boot.Running + ", menu " + Boot.Menu.Page + ")");

            tc.Next();
            Assert.AreEqual("dice", tc.ActiveStep.Id, "Skip step moves on without the goal");

            tc.HideTips();
            Assert.IsNull(tc.ActiveStep, "no card while hidden");
            Boot.Menu.ToggleTutorialTips();
            Assert.IsNotNull(tc.ActiveStep, "the Esc menu's tips switch brings it back");
            Assert.AreEqual("dice", tc.ActiveStep.Id);
        }

        [UnityTest]
        public IEnumerator Cards_DoNotBlockPlay_AndPointAtTheBuildButton()
        {
            yield return LoadTutorial();
            var tc = Boot.Tutorial; var hud = Boot.Hud;
            tc.Next();
            Assert.AreEqual(BuildingKinds.Woodcutter, hud.TutorialButtonKey, "the woodcutter button glows");
            long tick = Sim.State.Tick;
            for (float t0 = Time.realtimeSinceStartup; Time.realtimeSinceStartup - t0 < 5f && Sim.State.Tick == tick;) yield return null;
            Assert.Greater(Sim.State.Tick, tick, "the game keeps running with a card up");
            Assert.IsFalse(Boot.Menu.Blocking, "a card is not a menu");
        }

        [UnityTest]
        public IEnumerator Tutorial_IsSavedAndLoadedWithTheGame()
        {
            yield return LoadTutorial();
            Boot.Tutorial.Next(); Boot.Tutorial.Next();
            Boot.Tutorial.HideTips();
            Assert.IsTrue(Boot.Menu.Save(1));
            Assert.IsTrue(Boot.Menu.LoadSlot(1));
            yield return Frames(4);
            Assert.IsTrue(Sim.State.Setup.Tutorial, "the setup carries the tutorial flag");
            Assert.IsNotNull(Boot.Tutorial);
            Assert.AreEqual(2, Boot.Tutorial.Guide.Index);
            Assert.IsTrue(Boot.Tutorial.Guide.Hidden);
        }

        // ---------------------------------------------------------------- build bar

        [UnityTest]
        public IEnumerator BuildBar_ListsEveryKindOfTheGroups_OnTheRightTab()
        {
            yield return Load();
            var all = GodHud.BuildTools.Where(t => t.Mode == HudMode.Place).Select(t => t.Kind).ToList();
            foreach (var k in BuildingKinds.Harvesters) { Assert.Contains(k, all); Assert.AreEqual(BuildTab.Gather, GodHud.TabOf(k)); }
            foreach (var k in BuildingKinds.Processors) { Assert.Contains(k, all); Assert.AreEqual(BuildTab.Process, GodHud.TabOf(k)); }
            foreach (var k in BuildingKinds.Armouries) { Assert.Contains(k, all); Assert.AreEqual(BuildTab.Arms, GodHud.TabOf(k)); }
            Assert.AreEqual(5, GodHud.ToolsOf(BuildTab.Gather).Count);
            Assert.AreEqual(10, GodHud.ToolsOf(BuildTab.Process).Count);
            Assert.AreEqual(10, GodHud.ToolsOf(BuildTab.Arms).Count);
            Assert.AreEqual(BuildingKinds.Barracks, GodHud.ToolsOf(BuildTab.Army).Single().Kind);
            var land = GodHud.ToolsOf(BuildTab.Land).Select(t => t.Key).ToArray();
            CollectionAssert.AreEqual(new[] { "hq", "road", "demolish", "trade" }, land);
        }

        [UnityTest]
        public IEnumerator BuildBar_TooltipsShowCostAndRecipe_OrComing_AndTabsCycle()
        {
            yield return Load();
            foreach (var tool in GodHud.BuildTools.Where(t => t.Mode == HudMode.Place))
            {
                string tip = GodHud.ToolTip(tool);
                Assert.IsFalse(string.IsNullOrEmpty(tip), tool.Kind);
                if (EconomyDefs.Get(tool.Kind) == null)
                {
                    StringAssert.Contains("Coming soon", tip, tool.Kind);
                    Assert.IsFalse(GodHud.ToolAvailable(tool), tool.Kind + " is off until its def exists");
                }
                else { StringAssert.Contains("Cost:", tip, tool.Kind); StringAssert.Contains("Takes:", tip, tool.Kind); Assert.IsTrue(GodHud.ToolAvailable(tool)); }
            }
            var hud = Boot.Hud;
            Assert.AreEqual(BuildTab.Gather, hud.Tab);
            hud.CycleTab(1); Assert.AreEqual(BuildTab.Process, hud.Tab);
            hud.CycleTab(-1); hud.CycleTab(-1); Assert.AreEqual(BuildTab.Land, hud.Tab, "cycling wraps");
            hud.SetTab(BuildTab.Arms); Assert.AreEqual(BuildTab.Arms, hud.Tab);
            // A kind with no def cannot be picked.
            string missing = BuildingKinds.Armouries.Concat(BuildingKinds.Processors).FirstOrDefault(k => EconomyDefs.Get(k) == null);
            if (missing != null) { hud.BeginPlace(missing); Assert.AreEqual(HudMode.Normal, hud.Mode); }
        }

        [UnityTest]
        public IEnumerator StockBar_HasThreeGroups_WithOwnColours()
        {
            yield return Load();
            Assert.AreEqual(5, GodHud.StockRaws.Length);
            Assert.AreEqual(10, GodHud.StockProcessed.Length);
            Assert.AreEqual(10, GodHud.StockGear.Length);
            var all = GodHud.StockRaws.Concat(GodHud.StockProcessed).Concat(GodHud.StockGear).ToList();
            foreach (var w in all) Assert.AreNotEqual(Color.magenta, EconomyView.WareColour(w), w + " has a colour");
            Assert.AreEqual(all.Count, all.Select(w => EconomyView.WareColour(w)).Distinct().Count(), "no two wares share a colour");
        }

        [UnityTest]
        public IEnumerator WatchMode_KeepsItsNumberKeys_NoTabKeysThere()
        {
            yield return Load();
            GodBootstrap.StartNew(12, true);
            yield return Frames(4);
            Assert.IsTrue(Boot.Hud.Watching);
            Assert.AreEqual(BuildTab.Gather, Boot.Hud.Tab, "watch mode never switches the build tab");
        }

        // ---------------------------------------------------------------- models

        [UnityTest]
        public IEnumerator Models_ExistForEveryKindOfTheGroups_InBothStyles()
        {
            yield return Load();
            var wanted = BuildingKinds.Harvesters.Concat(BuildingKinds.Processors).Concat(BuildingKinds.Armouries).Append(BuildingKinds.Barracks).ToList();
            foreach (var k in wanted) Assert.Contains(k, BuildingModels.Kinds, k + " is in the model list");
            foreach (var k in BuildingModels.Stage3Kinds())
                foreach (var f in new[] { Faction.F0, Faction.F1 })
                    Assert.Greater(BuildingModels.MeshOf(k, f).vertexCount, 200, k + " " + f);
        }

        [UnityTest]
        public IEnumerator HarvesterModels_GrowWithTheirLevel()
        {
            yield return Load();
            foreach (var k in BuildingKinds.Harvesters)
            {
                int v1 = BuildingModels.MeshOf(k, Faction.F0).vertexCount;
                int v2 = BuildingModels.MeshOf(BuildingModels.KindAtLevel(k, 2), Faction.F0).vertexCount;
                int v3 = BuildingModels.MeshOf(BuildingModels.KindAtLevel(k, 3), Faction.F0).vertexCount;
                Assert.Greater(v2, v1, k + " level 2 has more than level 1");
                Assert.Greater(v3, v2, k + " level 3 has more than level 2");
                Assert.Greater(BuildingModels.RigOf(BuildingModels.KindAtLevel(k, 3), Faction.F0).Height, BuildingModels.RigOf(k, Faction.F0).Height);
            }
            Assert.AreEqual(BuildingKinds.Barracks, BuildingModels.KindAtLevel(BuildingKinds.Barracks, 3), "only harvesters have levels");
        }

        [UnityTest]
        public IEnumerator BuildingLayer_RedrawsAHarvesterWhenItsLevelRises()
        {
            yield return Load();
            Boot.Running = false;
            var hud = Boot.Hud; var t = Sim.State.Territory;
            string kind = new[] { BuildingKinds.Woodcutter, BuildingKinds.ClayPit, BuildingKinds.Quarry, BuildingKinds.Farm, BuildingKinds.Mine }
                .First(k => t.BuildableNodes(Faction.F0).Any(n => Sim.Check(new PlaceBuilding(Faction.F0, n, k)).Ok));
            int node = t.BuildableNodes(Faction.F0).First(n => Sim.Check(new PlaceBuilding(Faction.F0, n, kind)).Ok);
            hud.BeginPlace(kind);
            Assert.IsTrue(hud.WorldClick(node, 0).Ok);
            yield return Frames(3);
            int id = t.BuildingAtNode(node).Id;
            Assert.IsTrue(EconomyModule.CompleteInstantly(Sim.State, id));
            yield return Frames(3);
            var layer = Boot.Economy.Buildings;
            var m1 = layer.ModelOf(id);
            Assert.IsNotNull(m1, "the finished harvester has a model");
            int before = m1.GetComponent<MeshFilter>().sharedMesh.vertexCount;
            Sim.State.Economy.Find(id).Level = 2;
            yield return Frames(3);
            int after = layer.ModelOf(id).GetComponent<MeshFilter>().sharedMesh.vertexCount;
            Assert.Greater(after, before, "level 2 is drawn bigger");
            Sim.State.Economy.Find(id).Level = 3;
            yield return Frames(3);
            Assert.Greater(layer.ModelOf(id).GetComponent<MeshFilter>().sharedMesh.vertexCount, after, "and level 3 bigger still");
        }
    }
}
