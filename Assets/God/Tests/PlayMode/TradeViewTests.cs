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
    /// P8: the trade panel, the offer card and the envoy model in the real scene, driven through the HUD's public methods.
    /// Writes p8_panel.png, p8_offer.png (game view) and p8_envoy.png (camera render) to <c>GOD_SHOTS</c>, else LocalData.
    /// </summary>
    public class TradeViewTests
    {
        private GodBootstrap _boot;
        private GodHud _hud;
        private GodSim Sim => _boot.Sim;

        private IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Assets/God/Scenes/Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            _boot = GodBootstrap.Instance;
            _hud = _boot.Hud;
            Assert.IsNotNull(_hud);
            _boot.Running = false;
            _hud.DismissHelp();
        }

        private static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        private static string ShotDir()
        {
            var env = System.Environment.GetEnvironmentVariable("GOD_SHOTS");
            var dir = !string.IsNullOrEmpty(env) ? env : Path.Combine(Application.dataPath, "..", "LocalData");
            Directory.CreateDirectory(dir);
            return dir;
        }

        /// <summary>A stocked game: every HQ holds brick, plank and log so any offer can be paid.</summary>
        private void Stock()
        {
            foreach (var hq in Sim.State.Economy.Buildings.Where(b => b.IsHq))
            {
                hq.Stock[(int)Ware.Log] = 12; hq.Stock[(int)Ware.Brick] = 12; hq.Stock[(int)Ware.Plank] = 12; hq.Stock[(int)Ware.Clay] = 12;
            }
        }

        private bool RunUntil(int maxTicks, System.Func<bool> done)
        {
            for (int i = 0; i < maxTicks && !done(); i++) Sim.Step();
            return done();
        }

        [UnityTest]
        public IEnumerator TradePanel_SendsAnOffer_ThroughTheHudMethods()
        {
            yield return Load();
            Stock();
            Assert.IsFalse(_hud.TradeOpen);
            _hud.ToggleTrade();
            Assert.IsTrue(_hud.TradeOpen);
            Assert.AreEqual(5, _hud.TradePartners().Count, "every other faction, none at war");
            _hud.SelectTradePartner(Faction.F1);
            Assert.AreEqual(Faction.F1, _hud.TradeTo);

            // The steppers keep to 8 wares a side and one side per ware.
            for (int i = 0; i < 12; i++) _hud.AdjustTrade(true, Ware.Log, 1);
            Assert.AreEqual(8, _hud.TradeGive[(int)Ware.Log]);
            Assert.IsFalse(_hud.AdjustTrade(true, Ware.Plank, 1), "8 is the most each way");
            _hud.AdjustTrade(true, Ware.Log, -5);
            Assert.IsTrue(_hud.AdjustTrade(false, Ware.Brick, 1)); _hud.AdjustTrade(false, Ware.Brick, 1);
            Assert.IsTrue(_hud.AdjustTrade(false, Ware.Log, 1));
            Assert.AreEqual(0, _hud.TradeGive[(int)Ware.Log], "a ware is on one side only");
            _hud.AdjustTrade(false, Ware.Log, -1); _hud.AdjustTrade(true, Ware.Log, 3);
            Assert.AreEqual("3 log for 2 brick", _hud.TradeSummary());

            int logs = Sim.State.Economy.TotalStock(Faction.F0, Ware.Log);
            var r = _hud.SendTrade();
            Assert.IsTrue(r.Ok, r.ToString());
            yield return Frames(3);
            Assert.AreEqual(1, Sim.State.Trade.Envoys.Count, "the envoy is out");
            Assert.AreEqual(logs - 3, Sim.State.Economy.TotalStock(Faction.F0, Ware.Log));
            Assert.AreEqual(0, _hud.TradeGiveTotal, "the amounts are cleared");

            // A refusal shows as a toast: nothing to give.
            _hud.ClearToast();
            var bad = _hud.SendTrade();
            Assert.IsFalse(bad.Ok);
            Assert.IsTrue(_hud.ToastActive);
            Assert.AreEqual(TradeReasons.BadAmount, bad.Reason);

            // At war: no longer a partner, and a command to them is refused.
            _hud.DeclareWarOn(Faction.F2); Sim.Step(2);
            Assert.AreEqual(4, _hud.TradePartners().Count);
            _hud.SelectTradePartner(Faction.F2);
            Assert.AreEqual(Faction.F1, _hud.TradeTo, "a faction at war cannot be picked");
            _hud.ToggleTrade();
            Assert.IsFalse(_hud.TradeOpen);
        }

        [UnityTest]
        public IEnumerator OfferCard_AcceptsAnIncomingOffer()
        {
            yield return Load();
            Stock();
            Assert.IsNull(_hud.CardOffer());
            Assert.IsTrue(Sim.Submit(new OfferTrade(Faction.F1, Faction.F0, new[] { new WareCount(Ware.Log, 3) }, new[] { new WareCount(Ware.Brick, 2) })).Ok);
            Assert.IsTrue(RunUntil(3000, () => Sim.State.Trade.PendingFor(Faction.F0).Count == 1), "the envoy reaches the human");
            yield return Frames(3);
            var offer = _hud.CardOffer();
            Assert.IsNotNull(offer, "the offer card pops up");
            Assert.AreEqual(Faction.F1, offer.From);
            Assert.Greater(offer.TicksLeft, 0);
            Assert.AreEqual("Azure offers 3 log for 2 brick", FactionLook.Name(offer.From) + " offers " + GodHud.WaresText(offer.Give) + " for " + GodHud.WaresText(offer.Want));

            int logs = Sim.State.Economy.TotalStock(Faction.F0, Ware.Log), brick = Sim.State.Economy.TotalStock(Faction.F0, Ware.Brick);
            var r = _hud.AnswerOffer(offer.Id, true);
            Assert.IsTrue(r.Ok, r.ToString());
            Sim.Step(2);
            yield return Frames(3);
            Assert.AreEqual(logs + 3, Sim.State.Economy.TotalStock(Faction.F0, Ware.Log));
            Assert.AreEqual(brick - 2, Sim.State.Economy.TotalStock(Faction.F0, Ware.Brick));
            Assert.IsNull(_hud.CardOffer(), "the card is gone");
            CollectionAssert.Contains(_hud.TradeLogLines.ToList(), "You accepted Azure's offer: 3 log for 2 brick");

            // A refusal, by the card's other button.
            Assert.IsTrue(Sim.Submit(new OfferTrade(Faction.F2, Faction.F0, new[] { new WareCount(Ware.Plank, 1) }, new[] { new WareCount(Ware.Clay, 1) })).Ok);
            Assert.IsTrue(RunUntil(3000, () => Sim.State.Trade.PendingFor(Faction.F0).Count == 1));
            Assert.IsTrue(_hud.AnswerOffer(_hud.CardOffer().Id, false).Ok);
            Sim.Step(2);
            Assert.AreEqual(1, Sim.State.Trade.Refused);
        }

        [UnityTest]
        public IEnumerator EnvoyModel_ExistsWhileItWalks_AndOpensItsCardWhenClicked()
        {
            yield return Load();
            Stock();
            var layer = _boot.Economy.Envoys;
            Assert.IsNotNull(layer);
            Assert.AreEqual(0, layer.Count);
            Assert.IsTrue(Sim.Submit(new OfferTrade(Faction.F0, Faction.F3, new[] { new WareCount(Ware.Log, 4), new WareCount(Ware.Plank, 2) }, new[] { new WareCount(Ware.Brick, 2) })).Ok);
            Sim.Step(15);
            yield return Frames(4);
            var env = Sim.State.Trade.Envoys.Single();
            Assert.AreEqual(1, layer.Count, "an envoy figure is drawn");
            Assert.IsTrue(layer.HasFigure(env.Id));
            Assert.Greater(layer.BundlesNow, 0, "the donkey carries its cargo");
            Assert.IsNotNull(GameObject.Find("Pack donkey"), "with a pack animal");
            Assert.IsNotNull(GameObject.Find("Envoy"));

            // Clicking the envoy opens its card.
            Assert.IsTrue(layer.TryGetPosition(env.Id, out var pos));
            var sp = _boot.GodCamera.WorldToScreenPoint(pos + Vector3.up * 1.6f);
            Assert.AreEqual(env.Id, _hud.PickEnvoy(new Vector2(sp.x, sp.y)));
            _hud.SelectEnvoy(env.Id);
            Assert.AreEqual(env.Id, _hud.SelectedEnvoy);
            _hud.Cancel();
            Assert.AreEqual(0, _hud.SelectedEnvoy);

            // It goes away when the trip is over.
            Assert.IsTrue(RunUntil(5000, () => Sim.State.Trade.Envoys.Count == 0));
            yield return Frames(4);
            Assert.AreEqual(0, layer.Count);
        }

        private static void SetCamera(Camera cam, Vector3 focus, float dist, float yaw = 0f)
        {
            ModelViewTests.Aim(cam, focus, dist, 38f, yaw);
        }

        private IEnumerator Shot(string name)
        {
            for (int i = 0; i < 4; i++) yield return null;
            yield return new WaitForEndOfFrame();
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.GetFullPath(Path.Combine(ShotDir(), name)), tex.EncodeToPNG());
            Object.Destroy(tex);
        }

        [UnityTest]
        public IEnumerator Screenshots()
        {
            yield return Load();
            Stock();
            var cam = _boot.GodCamera; _boot.Rig.InputEnabled = false;
            var g = Sim.State.Territory.Grid;
            var hq = Sim.State.Territory.HqsOf(Faction.F0)[0];
            var hqPos = new Vector3((float)g.X(hq.Node), 0f, (float)g.Z(hq.Node));
            SetCamera(cam, hqPos, 95f);

            // The trade panel with an offer filled in.
            _hud.ToggleTrade(); _hud.SelectTradePartner(Faction.F1);
            _hud.AdjustTrade(true, Ware.Log, 3); _hud.AdjustTrade(true, Ware.Plank, 1); _hud.AdjustTrade(false, Ware.Brick, 2);
            yield return Shot("p8_panel.png");
            _hud.ToggleTrade();

            // An offer card for an envoy that reached you; two of your trades in the log.
            Sim.Submit(new OfferTrade(Faction.F0, Faction.F2, new[] { new WareCount(Ware.Clay, 3) }, new[] { new WareCount(Ware.Plank, 2) }));
            Sim.Submit(new OfferTrade(Faction.F1, Faction.F0, new[] { new WareCount(Ware.Log, 3), new WareCount(Ware.Plank, 1) }, new[] { new WareCount(Ware.Brick, 2) }));
            Assert.IsTrue(RunUntil(3000, () => Sim.State.Trade.PendingFor(Faction.F0).Count == 1 && Sim.State.Trade.Accepted >= 1));
            yield return Frames(3);
            SetCamera(cam, hqPos, 95f);
            yield return Shot("p8_offer.png");

            // A close-up of an envoy walking, with its donkey and bundles.
            Assert.IsTrue(_hud.AnswerOffer(_hud.CardOffer().Id, false).Ok);
            Sim.Step(5);
            Assert.IsTrue(Sim.Submit(new OfferTrade(Faction.F0, Faction.F4, new[] { new WareCount(Ware.Log, 4), new WareCount(Ware.Brick, 2), new WareCount(Ware.Plank, 2) }, new[] { new WareCount(Ware.Clay, 2) })).Ok);
            Sim.Step(1);
            _hud.ToggleTrade(); // keep the panel closed in the render
            if (_hud.TradeOpen) _hud.ToggleTrade();
            var layer = _boot.Economy.Envoys;
            var env = Sim.State.Trade.Envoys.Last();
            Assert.AreEqual(Faction.F0, env.Faction);
            _boot.Speed = 1f; _boot.Running = true;
            for (int i = 0; i < 90; i++)
            {
                if (layer.TryGetPosition(env.Id, out var p)) SetCamera(cam, p, 26f, 25f);
                yield return null;
            }
            Assert.IsTrue(layer.TryGetPosition(env.Id, out var at));
            SetCamera(cam, at, 24f, 25f);
            ModelViewTests.Shot(cam, "p8_envoy.png");
            _boot.Running = false;
        }
    }
}
