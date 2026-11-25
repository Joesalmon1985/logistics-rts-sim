using System.Collections;
using System.Linq;
using God.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace God.Game.Tests
{
    /// <summary>Stage 3 (Q1): the building card's harvester upgrade and the barracks' last kit, in the real scene.</summary>
    public class EconomyCardTests
    {
        private GodBootstrap _boot;
        private GodHud _hud;
        private GodSim Sim => _boot.Sim;
        private TerritoryState T => Sim.State.Territory;

        private IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Assets/God/Scenes/Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            _boot = GodBootstrap.Instance;
            _hud = _boot.Hud;
            Assert.IsNotNull(_hud);
            _boot.Running = false;
        }

        private IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        private int LegalNode(string kind) =>
            T.BuildableNodes(Faction.F0).FirstOrDefault(n => Sim.Check(new PlaceBuilding(Faction.F0, n, kind)).Ok);

        /// <summary>Places a building joined by road (the scripted bot, as a player would), finishes it at once (test set-up) and opens its card.</summary>
        private IEnumerator PlaceFinished(string kind, System.Action<int> id)
        {
            int bid = new EconomyBot(Sim, Faction.F0).PlaceAndConnect(kind);
            Assert.AreNotEqual(0, bid, "a joined spot for " + kind);
            Assert.IsTrue(EconomyModule.CompleteInstantly(Sim.State, bid));
            yield return Frames(3);
            var b = T.BuildingById(bid);
            _hud.WorldClick(b.Node, b.Id);
            Assert.AreEqual(b.Id, _hud.SelectedBuilding);
            id(b.Id);
        }

        [UnityTest]
        public IEnumerator Card_UpgradesAHarvester_ToLevel2()
        {
            yield return Load();
            string kind = BuildingKinds.Harvesters.FirstOrDefault(k => LegalNode(k) > 0 && EconomyDefs.Get(k).Size == BuildingSize.Small);
            Assert.IsNotNull(kind, "a harvester fits in the start circle");
            int id = 0;
            yield return PlaceFinished(kind, x => id = x);
            yield return Frames(3); // the card draws its level line and the upgrade button
            var e = Sim.State.Economy.Find(id);
            Assert.AreEqual(1, e.Level);
            var hq = Sim.State.Economy.HqsOf(Faction.F0)[0];
            hq.Stock[(int)Ware.Plank] += 2; hq.Stock[(int)Ware.Brick] += 2;
            Assert.IsTrue(_hud.UpgradeHarvester(id).Ok);
            yield return Frames(3);
            Assert.IsTrue(e.Upgrading, "the upgrade started");
            Assert.IsFalse(_hud.UpgradeHarvester(id).Ok, "not twice at once");
            for (int i = 0; i < 60 && e.Level < 2; i++) { Sim.Step(50); yield return null; }
            Assert.AreEqual(2, e.Level, "level 2 once its wares were carried in");
            yield return Frames(3); // the card now offers level 3
            Assert.AreEqual(6, e.UpgradeDef.Cost.Sum(), "the next upgrade costs 2 mortar, 2 iron, 2 bread");
            Assert.AreEqual(2, e.UpgradeDef.Cost[(int)Ware.Iron]);
        }

        [UnityTest]
        public IEnumerator Card_ShowsTheBarracksLastKit()
        {
            yield return Load();
            int id = 0;
            yield return PlaceFinished(BuildingKinds.Barracks, x => id = x);
            var hq = Sim.State.Economy.HqsOf(Faction.F0)[0];
            hq.Stock[(int)Ware.Bread] = 1; hq.Stock[(int)Ware.Bow] = 1; hq.Stock[(int)Ware.BigShield] = 1; hq.Stock[(int)Ware.Helmet] = 1;
            var e = Sim.State.Economy.Find(id);
            for (int i = 0; i < 60 && !Sim.State.Economy.ProducedLog.Any(u => u.BuildingId == id); i++) { Sim.Step(50); yield return null; }
            Assert.AreEqual(1, Sim.State.Economy.ProducedLog.Count(u => u.BuildingId == id), "a soldier came out");
            Assert.AreEqual(new Kit(Ware.Bow, Ware.None, Ware.Helmet), e.LastKit, "a bow takes no shield");
            yield return Frames(3); // the card draws "Last soldier: bow + helmet"
            Assert.AreEqual(id, _hud.SelectedBuilding);
        }
    }
}
