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
    /// <summary>The menu, save slots, start card, watch mode, standings, game-over card and the six-faction look (P10).</summary>
    public class MenuTests
    {
        private const string Scene = "Assets/God/Scenes/Main.unity";
        private string _dir;
        private int _quits;

        private GodBootstrap Boot => GodBootstrap.Instance;
        private GodSim Sim => Boot.Sim;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "god_p10_" + System.Guid.NewGuid().ToString("N"));
            SaveStore.Root = _dir;
            GodBootstrap.SkipStartCard = true;
            _quits = 0;
            GameMenu.QuitHandler = () => _quits++;
        }

        [TearDown]
        public void TearDown()
        {
            GameMenu.QuitHandler = null;
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

        // ---------------------------------------------------------------- menu

        [UnityTest]
        public IEnumerator Esc_PutsTheToolDownFirst_ThenOpensTheMenu_WhichPausesTheGame()
        {
            yield return Load();
            var hud = Boot.Hud;
            Boot.Running = true;
            string kind = BuildingKinds.Woodcutter;
            hud.BeginPlace(kind);
            Assert.AreEqual(HudMode.Place, hud.Mode);
            hud.PressEscape();
            Assert.AreEqual(HudMode.Normal, hud.Mode, "the first Esc only puts the tool down");
            Assert.IsFalse(Boot.Menu.IsOpen);
            hud.PressEscape();
            Assert.IsTrue(Boot.Menu.IsOpen, "with no tool in hand Esc opens the menu");
            Assert.AreEqual(MenuPage.Main, Boot.Menu.Page);

            long tick = Sim.State.Tick;
            yield return Frames(12);
            Assert.AreEqual(tick, Sim.State.Tick, "the game is held while the menu is open");
            Assert.IsTrue(hud.IsOverUi(new Vector2(5, 5)), "the HUD takes no clicks under the menu");

            hud.PressEscape();
            Assert.IsFalse(Boot.Menu.IsOpen, "Esc in the menu closes it");
            yield return new WaitForSeconds(0.6f);
            Assert.Greater(Sim.State.Tick, tick, "the game goes on");
        }

        [UnityTest]
        public IEnumerator Menu_BacksOutOfTheSlotPages_AndResumes()
        {
            yield return Load();
            var m = Boot.Menu;
            m.Open(); m.ShowSave();
            Assert.AreEqual(MenuPage.Save, m.Page);
            m.Back();
            Assert.AreEqual(MenuPage.Main, m.Page);
            m.ShowLoad(); m.Back(); m.Resume();
            Assert.IsFalse(m.IsOpen);
        }

        [UnityTest]
        public IEnumerator Save_ToASlot_ThenLoad_GivesTheSameTickAndHash()
        {
            yield return Load();
            Boot.StartAi();
            Boot.Running = false;
            for (int i = 0; i < 900; i++) { Sim.Step(); if (i % 20 == 0) Boot.ThinkChains(); }
            Sim.DrainEvents();
            long tick = Sim.State.Tick; ulong hash = Sim.StateHash(); ulong seed = Boot.Seed;
            var placed = Boot.Chains.Select(c => c.Placed.Count).ToArray();
            Assert.Greater(placed.Sum(), 0, "the bots have built something");

            Boot.Menu.Open();
            Assert.IsTrue(Boot.Menu.Save(2));
            Assert.IsTrue(File.Exists(Path.Combine(_dir, "slot2.json")));
            var h = SaveStore.ReadHeader(2);
            Assert.AreEqual(tick, h.Tick); Assert.AreEqual(seed, h.Seed);
            Assert.IsNull(SaveStore.ReadHeader(1), "an empty slot has no header");

            Assert.IsTrue(Boot.Menu.LoadSlot(2));
            yield return Frames(3);
            Assert.AreEqual(tick, Sim.State.Tick, "the same tick after the load");
            Assert.AreEqual(hash, Sim.StateHash(), "the same hash after the load");
            Assert.AreEqual(seed, Boot.Seed);
            Assert.IsFalse(Boot.Running, "a loaded game waits for the player");
            Assert.IsFalse(Boot.Menu.IsOpen);
            Assert.AreEqual(5, Boot.Chains.Count, "the AIs of the other five factions are restarted by StartAi");
            CollectionAssert.AreEqual(placed, Boot.Chains.Select(c => c.Placed.Count).ToArray(), "and know what they built");
        }

        [UnityTest]
        public IEnumerator ACorruptSlot_IsRefused_AndTheGameGoesOn()
        {
            yield return Load();
            Directory.CreateDirectory(_dir);
            File.WriteAllText(Path.Combine(_dir, "slot3.json"), "{ this is not a save");
            long tick = Sim.State.Tick;
            Assert.IsFalse(Boot.Menu.LoadSlot(3));
            Assert.IsTrue(Boot.Menu.MessageIsError);
            Assert.IsFalse(Boot.Menu.LoadSlot(1), "an empty slot too");
            yield return Frames(2);
            Assert.IsNotNull(Boot);
            Assert.GreaterOrEqual(Sim.State.Tick, tick, "still the same game");
        }

        [UnityTest]
        public IEnumerator NewIsland_ChangesTheSeed_AndKeepsTheMode()
        {
            yield return Load();
            ulong seed = Boot.Seed;
            Boot.Menu.Open();
            Boot.Menu.NewIsland();
            yield return Frames(3);
            Assert.AreNotEqual(seed, Boot.Seed, "another island");
            Assert.IsFalse(Boot.Hud.Watching, "still playing");
            Assert.IsFalse(Boot.Menu.IsOpen);

            Boot.Menu.WatchAis();
            yield return Frames(3);
            Assert.IsTrue(Boot.Hud.Watching, "Watch AIs removes the human");
            ulong watchSeed = Boot.Seed;
            Boot.Menu.Open();
            Boot.Menu.NewIsland();
            yield return Frames(3);
            Assert.AreNotEqual(watchSeed, Boot.Seed);
            Assert.IsTrue(Boot.Hud.Watching, "a new island keeps the watch mode");
        }

        [UnityTest]
        public IEnumerator Quit_CallsTheQuitHandler()
        {
            yield return Load();
            Boot.Menu.Open();
            Boot.Menu.Quit();
            Assert.AreEqual(1, _quits);
        }

        // ---------------------------------------------------------------- start card

        [UnityTest]
        public IEnumerator StartCard_ShowsAndHoldsTheGame_UnlessSkipped()
        {
            GodBootstrap.SkipStartCard = false;
            yield return Load();
            Assert.IsTrue(Boot.ShowStartCard);
            Assert.AreEqual(MenuPage.Start, Boot.Menu.Page);
            Assert.AreEqual(Boot.Seed.ToString(), Boot.Menu.SeedText, "the field shows the current seed");
            long tick = Sim.State.Tick;
            yield return Frames(10);
            Assert.AreEqual(tick, Sim.State.Tick, "nothing runs behind the card");
            Boot.Hud.PressEscape();
            Assert.AreEqual(MenuPage.Start, Boot.Menu.Page, "Esc does not dismiss the start card");

            Boot.Menu.RollSeed();
            Assert.AreNotEqual(Boot.Seed.ToString(), Boot.Menu.SeedText, "the dice give another seed");
            Boot.Menu.SeedText = Boot.Seed.ToString();
            Boot.Menu.StartPlay();
            Assert.IsFalse(Boot.Menu.Blocking, "Play with the same seed just starts");
            Assert.AreEqual(5, Boot.Chains.Count, "five AIs");
            yield return new WaitForSeconds(0.5f);
            Assert.Greater(Sim.State.Tick, tick);

            GodBootstrap.SkipStartCard = true;
            yield return Load();
            Assert.IsFalse(Boot.ShowStartCard, "skipped when asked to");
            Assert.IsFalse(Boot.Menu.Blocking);
        }

        [UnityTest]
        public IEnumerator StartCard_Watch_WithANewSeed_RestartsInWatchMode()
        {
            GodBootstrap.SkipStartCard = false;
            yield return Load();
            Boot.Menu.SeedText = "4242";
            Boot.Menu.StartWatch();
            yield return Frames(3);
            Assert.AreEqual(4242UL, Boot.Seed);
            Assert.IsTrue(Boot.Hud.Watching);
            Assert.IsFalse(Boot.ShowStartCard, "a restart does not ask again");
            Assert.AreEqual(6, Boot.Chains.Count);
        }

        // ---------------------------------------------------------------- watch mode

        [UnityTest]
        public IEnumerator WatchMode_HidesTheTools_OffersFastSpeeds_AndFliesToAFactionsHq()
        {
            yield return Load();
            GodBootstrap.StartNew(Boot.Seed, true);
            yield return Frames(3);
            var hud = Boot.Hud;
            Assert.IsTrue(hud.Watching);
            Assert.IsFalse(hud.CanAct);
            hud.BeginPlace(BuildingKinds.Sawmill); Assert.AreEqual(HudMode.Normal, hud.Mode, "no build tools");
            hud.BeginHq(); Assert.AreEqual(HudMode.Normal, hud.Mode);
            hud.BeginRoad(); Assert.AreEqual(HudMode.Normal, hud.Mode);
            CollectionAssert.AreEqual(new[] { 1f, 2f, 4f, 8f, 16f }, hud.Speeds);
            hud.SetSpeed(16f);
            Assert.AreEqual(16f, Boot.Speed);

            Boot.Running = false;
            Boot.Rig.InputEnabled = false;
            var t = Sim.State.Territory; var g = t.Grid;
            foreach (var f in new[] { Faction.F0, Faction.F3, Faction.F5 })
            {
                Assert.IsTrue(Boot.WatchCam.JumpToFaction(f, true));
                var hq = t.HqsOf(f)[0];
                Assert.IsTrue(Boot.Rig.GroundPoint(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f), out var p));
                Assert.Less(Vector2.Distance(new Vector2(p.x, p.z), new Vector2((float)g.X(hq.Node), (float)g.Z(hq.Node))), 25f, "the view centres on " + f);
            }
        }

        // ---------------------------------------------------------------- standings

        [UnityTest]
        public IEnumerator Standings_HaveSixRows_WithVpHqsAndWars()
        {
            yield return Load();
            var rows = Boot.Hud.StandingRows();
            Assert.AreEqual(6, rows.Count);
            foreach (var r in rows) { Assert.AreEqual(1, r.Hqs); Assert.AreEqual(1, r.Vp); Assert.AreEqual(0, r.Citadels); Assert.IsFalse(r.Eliminated); Assert.AreEqual(0, r.AtWarWith.Count); }
            Sim.Submit(new DeclareWar(Faction.F0, Faction.F2)); Sim.Step();
            rows = Boot.Hud.StandingRows();
            CollectionAssert.AreEqual(new[] { Faction.F2 }, rows.First(r => r.Faction == Faction.F0).AtWarWith);
            CollectionAssert.AreEqual(new[] { Faction.F0 }, rows.First(r => r.Faction == Faction.F2).AtWarWith);
            Assert.AreEqual(6, rows.Select(r => r.Faction).Distinct().Count());
        }

        // ---------------------------------------------------------------- toasts

        [UnityTest]
        public IEnumerator BigEvents_MakeToasts_InTheFactionColour()
        {
            yield return Load();
            Boot.Running = false;
            Sim.Submit(new DeclareWar(Faction.F1, Faction.F3)); Sim.Step();
            Boot.PumpEvents();
            Assert.AreEqual(1, Boot.Toasts.ActiveCount);
            var it = Boot.Toasts.Items[0];
            StringAssert.Contains("Azure declares war on Jade", it.Text);
            Assert.AreEqual(FactionLook.Colour(Faction.F1), it.Colour);
            Sim.Submit(new OfferPeace(Faction.F1, Faction.F3)); Sim.Step();
            Sim.Submit(new OfferPeace(Faction.F3, Faction.F1)); Sim.Step();
            Boot.PumpEvents();
            StringAssert.Contains("make peace", Boot.Toasts.Items.Last().Text);
        }

        // ---------------------------------------------------------------- game over

        [UnityTest]
        public IEnumerator GameOver_ShowsTheEndCard_WithTheStandingsAndNoMenu()
        {
            yield return Load();
            Boot.Running = false;
            var s = Sim.State; var t = s.Territory;
            foreach (var h in t.Hqs.ToList())
                if (h.Faction != Faction.F0) MilitaryRules.Destroy(s, s.Military.BuildingById(h.Id), new EventBuffer());
            Sim.Step(2);
            Assert.IsTrue(s.IsGameOver);
            yield return Frames(3);
            var hud = Boot.Hud;
            Assert.AreNotEqual(GameOutcome.Playing, hud.Outcome, "the end card shows");
            Assert.AreEqual("Victory", hud.EndTitle());
            StringAssert.Contains("last faction standing", hud.EndReasonText());
            Assert.AreEqual(6, s.Factions.FinalStandings.Count);
            hud.PressEscape();
            Assert.IsFalse(Boot.Menu.IsOpen, "no menu over the end card");
        }

        // ---------------------------------------------------------------- the six-faction look

        [UnityTest]
        public IEnumerator EveryHq_FliesAStandard_ThatGrowsWithTheCameraHeight()
        {
            yield return Load();
            yield return Frames(2);
            Assert.AreEqual(6, Boot.Standards.Count);
            Assert.AreEqual(1f, StandardLayer.ScaleFor(60f));
            Assert.Greater(StandardLayer.ScaleFor(400f), 2.5f);
            Assert.AreEqual(StandardLayer.MaxScale, StandardLayer.ScaleFor(5000f));
            Assert.Greater(Boot.Standards.CurrentScale, 1.5f, "the home view is far out");
        }

        [UnityTest]
        public IEnumerator Citadel_IsItsOwnModel_InBothStyles_AndTheFactionsLookApart()
        {
            yield return Load();
            foreach (var f in new[] { Faction.F0, Faction.F1 })
            {
                var hqMesh = BuildingModels.MeshOf(TerritoryTuning.HqKind, f);
                var citMesh = BuildingModels.MeshOf(BuildingModels.CitadelKind, f);
                Assert.Greater(citMesh.vertexCount, hqMesh.vertexCount * 2, "a fortress, not a scaled HQ (" + f + ")");
                Assert.Greater(BuildingModels.RigOf(BuildingModels.CitadelKind, f).Radius, BuildingModels.RigOf(TerritoryTuning.HqKind, f).Radius);
            }
            // Same family, different faction: the roof and trim differ.
            var a = ModelStyle.Of(Faction.F0); var c = ModelStyle.Of(Faction.F2);
            Assert.IsFalse(a.Stone || c.Stone);
            Assert.AreNotEqual(a.Roof, c.Roof);
            Assert.AreNotEqual(a.RoofEdge, c.RoofEdge);
            Assert.AreNotEqual(ModelStyle.Of(Faction.F1).Roof, ModelStyle.Of(Faction.F3).Roof);
            // The upgraded HQ is drawn with it.
            var hq = Sim.State.Territory.HqsOf(Faction.F1)[0];
            Boot.Running = false;
            hq.Citadel = true; // test set-up: as if upgraded
            yield return Frames(3);
            Assert.IsNotNull(Boot.Economy.Buildings.ModelOf(hq.Id));
            Assert.AreEqual(Vector3.one, Boot.Economy.Buildings.ModelOf(hq.Id).transform.localScale, "not a scaled HQ");
        }
    }
}
