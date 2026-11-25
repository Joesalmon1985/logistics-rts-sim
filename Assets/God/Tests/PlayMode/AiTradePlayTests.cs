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
    /// <summary>
    /// P11: AI trade in the real scene. A normal game (you and five trading AIs) is saved with envoys walking and an offer
    /// waiting for you, loaded, and goes on exactly as the unbroken game; screenshots of envoys between AI towns and of a
    /// watch game's end card go where <c>P11_SHOTS</c> says, else to LocalData (not committed).
    /// </summary>
    public class AiTradePlayTests
    {
        private const string Scene = "Assets/God/Scenes/Main.unity";
        private string _dir;
        private GodBootstrap Boot => GodBootstrap.Instance;

        private static string ShotDir()
        {
            string dir = System.Environment.GetEnvironmentVariable("P11_SHOTS");
            if (string.IsNullOrEmpty(dir)) dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "LocalData"));
            Directory.CreateDirectory(dir);
            return dir;
        }

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "god_p11_" + System.Guid.NewGuid().ToString("N"));
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

        private static IEnumerator LoadScene(ulong seed, bool watch)
        {
            GodBootstrap.PendingSeed = seed;
            GodBootstrap.PendingSetup = watch ? GameSetup.Watch(seed) : new GameSetup(seed, 0);
            yield return SceneManager.LoadSceneAsync(Scene, new LoadSceneParameters(LoadSceneMode.Single));
            yield return Frames(2);
        }

        [UnityTest]
        public IEnumerator SaveLoad_NormalGameWithTradingAis_GoesOnLikeTheUnbrokenGame()
        {
            yield return LoadScene(5, false);
            var sim = Boot.Sim; var s = sim.State;
            Boot.Running = false;
            Assert.AreEqual(5, AiHost.Director.Brains.Count, "five AIs");
            Assert.IsInstanceOf<AiTradeJudge>(s.Trade.Judge, "AI targets answer by their brains");
            Assert.IsInstanceOf<AiTrader>(AiHost.Director.Brains[0].Trader);

            // On until AIs trade with each other, then an AI (Azure) offers the human a deal, as AIs may.
            int aiOffers = 0;
            while (aiOffers == 0 && s.Tick < 12000) { sim.Step(); aiOffers += sim.DrainEvents().OfType<TradeOffered>().Count(o => o.From != Faction.F0); }
            Assert.Greater(aiOffers, 0, "the AIs trade");
            // The first AI (by id) with a ware free in one HQ and an envoy to spare offers 1 of it for 1 of the human's.
            bool sent = false;
            foreach (var f in new[] { Faction.F1, Faction.F2, Faction.F3, Faction.F4, Faction.F5 })
            {
                foreach (var hq in s.Economy.HqsOf(f))
                    for (int w = 1; w < Wares.Count && !sent; w++)
                    {
                        if (!TradeTuning.IsTradable((Ware)w) || TradeRules.FreeStock(hq, (Ware)w) < 1) continue;
                        for (int v = 1; v < Wares.Count && !sent; v++)
                            if (v != w && TradeTuning.IsTradable((Ware)v) && TradeRules.FreeStock(s, Faction.F0, (Ware)v) >= 1)
                                sent = sim.Submit(new OfferTrade(f, Faction.F0, new[] { new WareCount((Ware)w, 1) }, new[] { new WareCount((Ware)v, 1) }, hq.Id)).Ok;
                    }
                if (sent) break;
            }
            Assert.IsTrue(sent, "an AI offers the human a deal");
            while (s.Trade.PendingFor(Faction.F0).Count == 0 && s.Tick < 20000) { sim.Step(); sim.DrainEvents(); }
            Assert.AreEqual(1, s.Trade.PendingFor(Faction.F0).Count, "an offer waits for the human");
            yield return Frames(2);
            Assert.AreEqual(1, Boot.Hud.PendingOffers().Count, "the HUD offer card has it");
            while (s.Trade.ActiveEnvoys.Count(e => e.Stage != EnvoyStage.Waiting) == 0 && s.Tick < 20000) { sim.Step(); sim.DrainEvents(); }
            Assert.Greater(s.Trade.ActiveEnvoys.Count(e => e.Stage != EnvoyStage.Waiting), 0, "envoys are walking");

            long tick = s.Tick; ulong hash = sim.StateHash();
            Boot.Menu.Open();
            Assert.IsTrue(Boot.Menu.Save(1));
            Boot.Menu.Resume(); Boot.Running = false;
            const int More = 400;
            int tradesAfter = 0;
            for (int i = 0; i < More; i++) { sim.Step(); tradesAfter += sim.DrainEvents().Count(e => e is TradeOffered || e is TradeAccepted || e is TradeRefused || e is EnvoyHome); }
            ulong straight = sim.StateHash();
            Debug.Log("P11 save at tick " + tick + ", " + s.Trade.ActiveEnvoys.Count + " envoys; " + tradesAfter + " trade events in the next " + More + " ticks");

            Assert.IsTrue(Boot.Menu.LoadSlot(1));
            yield return Frames(3);
            var loaded = Boot.Sim;
            Assert.AreNotSame(sim, loaded);
            Assert.AreEqual(tick, loaded.State.Tick);
            Assert.AreEqual(hash, loaded.StateHash(), "the same hash after the load");
            Assert.AreSame(loaded, AiHost.Director.Sim, "the AIs start again on the loaded game");
            Assert.IsInstanceOf<AiTradeJudge>(loaded.State.Trade.Judge, "the AI judge is set again after the load");
            Assert.AreEqual(1, loaded.State.Trade.PendingFor(Faction.F0).Count, "the offer still waits for the human");
            Assert.AreEqual(1, Boot.Hud.PendingOffers().Count);
            Boot.Running = false;
            for (int i = 0; i < More; i++) { loaded.Step(); loaded.DrainEvents(); }
            Assert.AreEqual(straight, loaded.StateHash(), More + " ticks on, the loaded game is the unbroken game");
            Assert.IsEmpty(SimInvariants.Check(loaded.State));
        }

        [UnityTest]
        public IEnumerator Screenshot_EnvoysWalkingBetweenAiTowns()
        {
            yield return LoadScene(5, true);
            var sim = Boot.Sim; var s = sim.State;
            Boot.Running = false;
            // Mid-game, at a moment with several envoys on their way.
            while (!s.IsGameOver && s.Tick < 30000 && !(s.Tick > 6000 && s.Trade.ActiveEnvoys.Count(e => e.Stage != EnvoyStage.Waiting) >= 3))
            { sim.Step(); sim.DrainEvents(); }
            var walking = s.Trade.ActiveEnvoys.Where(e => e.Stage != EnvoyStage.Waiting).ToList();
            Assert.GreaterOrEqual(walking.Count, 2, "envoys on the way");
            yield return Frames(3);
            // The envoy whose two towns lie closest together: both towns and the envoy fit one picture.
            Vector3 HqAt(int id) { var b = s.Economy.Find(id); return b == null ? Vector3.zero : new Vector3(TradeRules.HqX(s, b) / 1000f, 0f, TradeRules.HqZ(s, b) / 1000f); }
            var e0 = walking.OrderBy(e => (HqAt(e.HomeHq) - HqAt(e.TargetHq)).magnitude).ThenBy(e => e.Id).First();
            Vector3 home = HqAt(e0.HomeHq), there = HqAt(e0.TargetHq);
            var at = new Vector3((float)e0.PosX, 0f, (float)e0.PosZ);
            var cam = Boot.GodCamera;
            Boot.Rig.InputEnabled = false;
            var prev = System.Environment.GetEnvironmentVariable("GOD_SHOTS");
            System.Environment.SetEnvironmentVariable("GOD_SHOTS", ShotDir());
            try
            {
                ModelViewTests.Aim(cam, at, 55f, 50f);
                ModelViewTests.Shot(cam, "p11_trade_close.png", 1600, 900);
                var mid = (home + there) * 0.5f;
                ModelViewTests.Aim(cam, mid, Mathf.Max(150f, (home - there).magnitude * 1.9f), 58f);
                Debug.Log("P11 trade picture: towns " + home + " and " + there + ", envoy at " + at);
                ModelViewTests.Shot(cam, "p11_trade_midgame.png", 1920, 1080);
                IslandCamera.Frame(cam, BoardSpace.FrameRadius + 40f, BoardSpace.BoardCentre);
                ModelViewTests.Shot(cam, "p11_trade_island.png", 1920, 1080);
            }
            finally { System.Environment.SetEnvironmentVariable("GOD_SHOTS", prev); }
            Debug.Log("P11 envoys at tick " + s.Tick + " (pictured " + FactionInfo.Name(e0.Faction) + "->" + FactionInfo.Name(e0.Target) + "): " + string.Join(", ", walking.Select(e => FactionInfo.Name(e.Faction) + "->" + FactionInfo.Name(e.Target) + " " + e.Stage)));
        }

        [UnityTest]
        public IEnumerator Screenshot_WatchGameEndCard()
        {
            yield return LoadScene(2, true);
            var sim = Boot.Sim; var s = sim.State;
            Boot.Running = false;
            int offered = 0, accepted = 0;
            while (!s.IsGameOver && s.Tick < 90 * 600)
            {
                for (int k = 0; k < 500 && !s.IsGameOver; k++)
                {
                    sim.Step();
                    foreach (var ev in sim.DrainEvents()) { if (ev is TradeOffered) offered++; else if (ev is TradeAccepted) accepted++; }
                }
                yield return null;
            }
            Assert.IsTrue(s.IsGameOver, "the watch game ends");
            Assert.Greater(offered, 0); Assert.Greater(accepted, 0);
            Boot.Rig.Reframe(BoardSpace.FrameRadius + 40f, BoardSpace.BoardCentre);
            for (int i = 0; i < 6; i++) yield return null;
            Assert.AreNotEqual(GameOutcome.Playing, Boot.Hud.Outcome, "the end card shows");
            yield return new WaitForEndOfFrame();
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            string file = Path.Combine(ShotDir(), "p11_watch_end.png");
            File.WriteAllBytes(file, tex.EncodeToPNG());
            Object.Destroy(tex);
            Debug.Log("P11 watch end at minute " + (s.Factions.GameOverTick / 600.0).ToString("0.0") + ": " + FactionInfo.Name((Faction)s.Factions.Winner)
                + " wins; trades offered " + offered + ", accepted " + accepted + "; screenshot " + file);
        }
    }
}
