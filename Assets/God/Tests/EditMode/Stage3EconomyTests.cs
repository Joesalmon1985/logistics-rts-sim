using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>Stage 3 economy (Q1): the recipe tables, the barracks kit, the costs and the removal of the old chain.</summary>
    public class RecipeTests
    {
        /// <summary>Every workshop of the stage 3 tables: two inputs, one output (processor or armoury).</summary>
        public static IEnumerable<TestCaseData> Recipes()
        {
            // Processors.
            yield return new TestCaseData(BuildingKinds.Sawmill, Ware.Log, Ware.Stone, Ware.Plank, false);
            yield return new TestCaseData(BuildingKinds.Kiln, Ware.Log, Ware.Clay, Ware.Brick, false);
            yield return new TestCaseData(BuildingKinds.Smelter, Ware.Log, Ware.Ore, Ware.Iron, false);
            yield return new TestCaseData(BuildingKinds.Bakery, Ware.Log, Ware.Grain, Ware.Bread, false);
            yield return new TestCaseData(BuildingKinds.MortarMill, Ware.Clay, Ware.Stone, Ware.Mortar, false);
            yield return new TestCaseData(BuildingKinds.Foundry, Ware.Clay, Ware.Ore, Ware.Bronze, false);
            yield return new TestCaseData(BuildingKinds.Pottery, Ware.Clay, Ware.Grain, Ware.Pottery, false);
            yield return new TestCaseData(BuildingKinds.Forge, Ware.Stone, Ware.Ore, Ware.Steel, false);
            yield return new TestCaseData(BuildingKinds.Tannery, Ware.Grain, Ware.Stone, Ware.Leather, false);
            yield return new TestCaseData(BuildingKinds.Weaver, Ware.Grain, Ware.Ore, Ware.Cloth, false);
            // Armouries.
            yield return new TestCaseData(BuildingKinds.Swordsmith, Ware.Steel, Ware.Leather, Ware.Sword, true);
            yield return new TestCaseData(BuildingKinds.Spearmaker, Ware.Plank, Ware.Bronze, Ware.Spear, true);
            yield return new TestCaseData(BuildingKinds.Axesmith, Ware.Iron, Ware.Plank, Ware.Axe, true);
            yield return new TestCaseData(BuildingKinds.Bowyer, Ware.Plank, Ware.Cloth, Ware.Bow, true);
            yield return new TestCaseData(BuildingKinds.Slingmaker, Ware.Leather, Ware.Pottery, Ware.Sling, true);
            yield return new TestCaseData(BuildingKinds.Shieldwright, Ware.Plank, Ware.Leather, Ware.BigShield, true);
            yield return new TestCaseData(BuildingKinds.Helmsmith, Ware.Iron, Ware.Cloth, Ware.Helmet, true);
            yield return new TestCaseData(BuildingKinds.Bucklersmith, Ware.Bronze, Ware.Leather, Ware.SmallShield, true);
            yield return new TestCaseData(BuildingKinds.Armourer, Ware.Steel, Ware.Cloth, Ware.Breastplate, true);
            yield return new TestCaseData(BuildingKinds.Leatherworker, Ware.Leather, Ware.Cloth, Ware.LightArmour, true);
        }

        [TestCaseSource(nameof(Recipes))]
        public void Recipe_IsInTheDefs(string kind, Ware a, Ware b, Ware make, bool armoury)
        {
            var d = EconomyDefs.Get(kind);
            Assert.NotNull(d, kind);
            Assert.AreEqual(BuildingRole.Workshop, d.Role);
            Assert.AreEqual(armoury, d.Armoury); Assert.AreEqual(!armoury, d.Processor);
            Assert.AreEqual(BuildingSize.Medium, d.Size);
            Assert.AreEqual(make, d.Makes);
            for (int w = 0; w < Wares.Count; w++)
            {
                Assert.AreEqual((Ware)w == a || (Ware)w == b ? 1 : 0, d.In[w], kind + " takes " + (Ware)w);
                Assert.AreEqual((Ware)w == make ? 1 : 0, d.Out[w], kind + " makes " + (Ware)w);
            }
            Assert.AreSame(d, EconomyDefs.MakerOf(make));
            if (armoury) Assert.AreNotEqual(GearSlot.None, Gear.SlotOf(make)); else Assert.IsTrue(Wares.IsProcessed(make));
        }

        [TestCaseSource(nameof(Recipes))]
        public void Recipe_RunsInAGame_TwoInputsMakeOneWare(string kind, Ware a, Ware b, Ware make, bool armoury)
        {
            var sim = EcoHelp.Quiet(3);
            var e = sim.State.Economy; var hq = EcoHelp.Hq(sim);
            int id = EcoHelp.Place(sim, kind, true);
            // Nothing for it yet: let the site's carriers settle, then give exactly two cycles of inputs.
            // (A carrier that set out with a cost ware for the site may hand it in as an input instead.)
            hq.Stock[(int)a] = 0; hq.Stock[(int)b] = 0;
            EcoHelp.Run(sim, 300, () => e.Walkers.Count == 0);
            EcoHelp.Run(sim, 100);
            var r0 = e.Find(id);
            Assert.AreEqual(0, r0.Incoming.Sum() + e.Walkers.Count, "settled");
            int early = r0.Produced;
            hq.Stock[(int)a] = 0; hq.Stock[(int)b] = 0; r0.In[(int)a] = 0; r0.In[(int)b] = 0;
            EcoHelp.Run(sim, 200);
            Assert.AreEqual(early, e.Find(id).Produced, "no inputs, no work");
            r0.Produced = 0;
            hq.Stock[(int)a] = 2; hq.Stock[(int)b] = 2;
            int before = e.TotalStock(Faction.F0, make);
            EcoHelp.Run(sim, 2500, () => e.Find(id).Produced == 2 && e.Walkers.Count == 0 && e.Find(id).Out[(int)make] == 0);
            var rec = e.Find(id);
            Assert.AreEqual(2, rec.Produced, kind);
            Assert.AreEqual(before + 2, e.TotalStock(Faction.F0, make), kind + ": two " + Wares.Name(make) + " in the HQ");
            Assert.AreEqual(0, e.TotalStock(Faction.F0, a) + rec.In[(int)a], "both " + Wares.Name(a) + " used");
            Assert.AreEqual(0, e.TotalStock(Faction.F0, b) + rec.In[(int)b], "both " + Wares.Name(b) + " used");
        }
    }

    public class BarracksTests
    {
        [Test]
        public void Barracks_PutsASoldierWithItsKitOnTheQueue()
        {
            var sim = EcoHelp.Quiet(3);
            var s = sim.State; var e = s.Economy; var hq = EcoHelp.Hq(sim);
            hq.Stock[(int)Ware.Bread] = 1; hq.Stock[(int)Ware.Axe] = 1; hq.Stock[(int)Ware.SmallShield] = 1; hq.Stock[(int)Ware.Helmet] = 1;
            int barracks = EcoHelp.Place(sim, BuildingKinds.Barracks, true);
            sim.DrainEvents();
            EcoHelp.Run(sim, 1200);
            var made = s.Economy.ProducedLog.Where(u => u.BuildingId == barracks).ToList();
            Assert.AreEqual(1, made.Count, "one bread, one soldier");
            Assert.AreEqual(UnitKind.Infantry, made[0].Kind);
            Assert.AreEqual(Faction.F0, made[0].Faction);
            Assert.AreEqual(new Kit(Ware.Axe, Ware.SmallShield, Ware.Helmet), made[0].Kit);
            Assert.AreEqual(made[0].Kit, e.Find(barracks).LastKit, "the card shows the last kit");
            var ev = sim.DrainEvents().OfType<UnitProduced>().Single();
            Assert.AreEqual(made[0].Kit, ev.Kit);
            Assert.AreEqual(1, e.UnitsProduced);
        }

        [Test]
        public void Barracks_WithoutAWeapon_OrWithoutBread_MakesNobody()
        {
            foreach (bool bread in new[] { true, false })
            {
                var sim = EcoHelp.Quiet(3);
                var hq = EcoHelp.Hq(sim);
                if (bread) { hq.Stock[(int)Ware.Bread] = 2; hq.Stock[(int)Ware.Helmet] = 2; hq.Stock[(int)Ware.BigShield] = 2; }
                else hq.Stock[(int)Ware.Sword] = 2;
                int barracks = EcoHelp.Place(sim, BuildingKinds.Barracks, true);
                EcoHelp.Run(sim, 1200);
                Assert.AreEqual(0, sim.State.Economy.ProducedLog.Count, bread ? "defence items are no weapon" : "no bread");
                Assert.AreEqual(BuildStatus.MissingInputs, sim.State.Economy.Find(barracks).Status);
            }
        }

        [Test]
        public void Barracks_InputPriorities_SteerTheWeapon()
        {
            // Without priorities it takes the weapon it holds most of: axes. With axes stopped it only gets the sword.
            foreach (bool stopAxe in new[] { false, true })
            {
                var sim = EcoHelp.Quiet(3);
                var e = sim.State.Economy; var hq = EcoHelp.Hq(sim);
                int barracks = EcoHelp.Place(sim, BuildingKinds.Barracks, true);
                Assert.IsTrue(sim.Submit(new SetInputPriority(Faction.F0, barracks, Ware.Bread, Priority.Stop)).Ok);
                if (stopAxe) Assert.IsTrue(sim.Submit(new SetInputPriority(Faction.F0, barracks, Ware.Axe, Priority.Stop)).Ok);
                hq.Stock[(int)Ware.Axe] = 2; hq.Stock[(int)Ware.Sword] = 1; hq.Stock[(int)Ware.Bread] = 1;
                EcoHelp.Run(sim, 400);
                Assert.IsTrue(sim.Submit(new SetInputPriority(Faction.F0, barracks, Ware.Bread, Priority.Normal)).Ok);
                EcoHelp.Run(sim, 800, () => e.ProducedLog.Count > 0);
                Assert.AreEqual(stopAxe ? Ware.Sword : Ware.Axe, e.ProducedLog.Single().Kit.Weapon);
                Assert.AreEqual(stopAxe ? 2 : 0, hq.Stock[(int)Ware.Axe], "stopped axes stay in the HQ");
            }
        }

        private static int[] Held(params (Ware w, int n)[] items)
        {
            var a = new int[Wares.Count];
            foreach (var (w, n) in items) a[(int)w] += n;
            return a;
        }

        [Test]
        public void Kit_NeedsBreadAndAWeapon()
        {
            Assert.IsFalse(BarracksRules.CanAssemble(Held((Ware.Sword, 3))));
            Assert.IsFalse(BarracksRules.CanAssemble(Held((Ware.Bread, 3), (Ware.Helmet, 1), (Ware.BigShield, 1), (Ware.Breastplate, 1))));
            Assert.IsTrue(BarracksRules.CanAssemble(Held((Ware.Bread, 1), (Ware.Sling, 1))));
            Assert.AreEqual(Ware.None, BarracksRules.Choose(Held((Ware.Bread, 1))).Weapon);
            var held = Held((Ware.Sword, 1), (Ware.Helmet, 1));
            Assert.AreEqual(Ware.None, BarracksRules.Assemble(held).Weapon);
            Assert.AreEqual(1, held[(int)Ware.Sword], "nothing taken when no soldier is made");
        }

        [Test]
        public void Kit_WeaponIsTheOneHeldMost_TiesBySwordSpearAxeBowSling()
        {
            Assert.AreEqual(Ware.Axe, BarracksRules.Choose(Held((Ware.Bread, 1), (Ware.Sword, 1), (Ware.Axe, 2))).Weapon);
            Assert.AreEqual(Ware.Sword, BarracksRules.Choose(Held((Ware.Bread, 1), (Ware.Sword, 1), (Ware.Spear, 1), (Ware.Axe, 1), (Ware.Bow, 1), (Ware.Sling, 1))).Weapon);
            Assert.AreEqual(Ware.Spear, BarracksRules.Choose(Held((Ware.Bread, 1), (Ware.Spear, 2), (Ware.Axe, 2), (Ware.Sling, 2))).Weapon);
            Assert.AreEqual(Ware.Axe, BarracksRules.Choose(Held((Ware.Bread, 1), (Ware.Axe, 1), (Ware.Bow, 1), (Ware.Sling, 1))).Weapon);
            Assert.AreEqual(Ware.Bow, BarracksRules.Choose(Held((Ware.Bread, 1), (Ware.Bow, 1), (Ware.Sling, 1))).Weapon);
            Assert.AreEqual(Ware.Sling, BarracksRules.Choose(Held((Ware.Bread, 1), (Ware.Sling, 1))).Weapon);
        }

        [Test]
        public void Kit_BowTakesNoShield_ButHelmetAndBody()
        {
            var held = Held((Ware.Bread, 1), (Ware.Bow, 1), (Ware.BigShield, 1), (Ware.SmallShield, 1), (Ware.Helmet, 1), (Ware.LightArmour, 1));
            var k = BarracksRules.Assemble(held);
            Assert.AreEqual(new Kit(Ware.Bow, Ware.None, Ware.Helmet, Ware.LightArmour), k);
            Assert.AreEqual("bow + helmet + light armour", k.ToString());
            Assert.AreEqual(1, held[(int)Ware.BigShield]); Assert.AreEqual(1, held[(int)Ware.SmallShield], "the shields stay");
            Assert.AreEqual(0, held[(int)Ware.Bread] + held[(int)Ware.Bow] + held[(int)Ware.Helmet] + held[(int)Ware.LightArmour]);
        }

        [Test]
        public void Kit_OneItemPerSlot_MostHeldThenListOrder()
        {
            var held = Held((Ware.Bread, 2), (Ware.Sword, 1), (Ware.BigShield, 1), (Ware.SmallShield, 2), (Ware.Helmet, 2), (Ware.Breastplate, 1), (Ware.LightArmour, 1));
            var k = BarracksRules.Assemble(held);
            Assert.AreEqual(new Kit(Ware.Sword, Ware.SmallShield, Ware.Helmet, Ware.Breastplate), k, "small shield held most; the body tie goes to the breastplate");
            Assert.IsTrue(k.IsArmed);
            Assert.AreEqual(1, held[(int)Ware.Bread]); Assert.AreEqual(1, held[(int)Ware.SmallShield]); Assert.AreEqual(1, held[(int)Ware.Helmet]);
            Assert.AreEqual(0, held[(int)Ware.Breastplate]); Assert.AreEqual(1, held[(int)Ware.LightArmour]);
            // A shield ties go to the big shield (list order).
            Assert.AreEqual(Ware.BigShield, BarracksRules.Choose(Held((Ware.Bread, 1), (Ware.Spear, 1), (Ware.BigShield, 1), (Ware.SmallShield, 1))).Shield);
            // The second soldier: no weapon left.
            Assert.AreEqual(Ware.None, BarracksRules.Assemble(held).Weapon);
        }

        [Test]
        public void Barracks_TakesBreadAndEveryPieceOfGear_AsInputs()
        {
            var d = EconomyDefs.Get(BuildingKinds.Barracks);
            Assert.AreEqual(BuildingRole.Factory, d.Role);
            Assert.AreEqual(1, d.In[(int)Ware.Bread]);
            foreach (var g in Gear.Attack.Concat(Gear.Defence)) Assert.AreEqual(1, d.In[(int)g], g.ToString());
            Assert.AreEqual(11, d.In.Sum());
        }
    }

    public class EconomyCostTests
    {
        private static int[] Cost(params (Ware w, int n)[] items)
        {
            var a = new int[Wares.Count];
            foreach (var (w, n) in items) a[(int)w] = n;
            return a;
        }

        [Test]
        public void Costs_FollowTheStage3Table()
        {
            var small = Cost((Ware.Log, 2), (Ware.Stone, 1));
            var processor = Cost((Ware.Plank, 2), (Ware.Brick, 1), (Ware.Stone, 1));
            var armoury = Cost((Ware.Plank, 2), (Ware.Brick, 2), (Ware.Mortar, 1));
            foreach (var k in BuildingKinds.Harvesters) CollectionAssert.AreEqual(small, EconomyDefs.Get(k).Cost, k);
            foreach (var k in BuildingKinds.Processors) CollectionAssert.AreEqual(processor, EconomyDefs.Get(k).Cost, k);
            foreach (var k in BuildingKinds.Armouries) CollectionAssert.AreEqual(armoury, EconomyDefs.Get(k).Cost, k);
            CollectionAssert.AreEqual(armoury, EconomyDefs.Get(BuildingKinds.Barracks).Cost, "barracks");
            CollectionAssert.AreEqual(Cost((Ware.Plank, 3), (Ware.Brick, 2), (Ware.Mortar, 2), (Ware.Stone, 2)), EconomyDefs.Hq.Cost, "HQ");
            CollectionAssert.AreEqual(Cost((Ware.Brick, 4), (Ware.Mortar, 4), (Ware.Plank, 3), (Ware.Steel, 2)), EconomyDefs.Citadel.Cost, "Citadel");
            CollectionAssert.AreEqual(Cost((Ware.Plank, 2), (Ware.Brick, 2)), EconomyDefs.HarvesterUpgrade(2).Cost, "level 2");
            CollectionAssert.AreEqual(Cost((Ware.Mortar, 2), (Ware.Iron, 2), (Ware.Bread, 2)), EconomyDefs.HarvesterUpgrade(3).Cost, "level 3");
            Assert.IsNull(EconomyDefs.HarvesterUpgrade(4)); Assert.IsNull(EconomyDefs.HarvesterUpgrade(1));
            Assert.AreEqual(BuildingSize.Medium, EconomyDefs.Get(BuildingKinds.Farm).Size, "the farm keeps its medium plot");
        }

        [Test]
        public void TheOldChainIsGone()
        {
            Assert.IsNull(EconomyDefs.Get(BuildingKinds.VehicleWorkshop), "no vehicle workshop");
            foreach (var d in EconomyDefs.Defs)
            {
                Assert.AreEqual(0, d.In[(int)Ware.Kindling] + d.Out[(int)Ware.Kindling] + d.Cost[(int)Ware.Kindling], d.Kind + ": no kindling");
                Assert.AreEqual(UnitKind.Infantry, d.Unit, d.Kind);
            }
            Assert.AreEqual(5 + 10 + 10 + 1, EconomyDefs.Defs.Count, "5 harvesters, 10 processors, 10 armouries, the barracks");
            CollectionAssert.AreEquivalent(BuildingKinds.Harvesters, EconomyDefs.Defs.Where(d => d.Role == BuildingRole.Harvester).Select(d => d.Kind));
            CollectionAssert.AreEquivalent(BuildingKinds.Processors, EconomyDefs.Defs.Where(d => d.Processor).Select(d => d.Kind));
            CollectionAssert.AreEquivalent(BuildingKinds.Armouries, EconomyDefs.Defs.Where(d => d.Armoury).Select(d => d.Kind));
            // Every processed ware and every piece of gear has exactly one maker.
            foreach (var w in Wares.Processed.Concat(Gear.Attack).Concat(Gear.Defence))
                Assert.AreEqual(1, EconomyDefs.Defs.Count(d => d.Out[(int)w] > 0), w.ToString());
        }

        [Test]
        public void StartStock_PaysForAFirstChain()
        {
            // The start can lay a woodcutter, a quarry, a sawmill and a kiln at once.
            var stock = EcoHelp.Hq(EcoHelp.Quiet(3)).Stock.ToArray();
            foreach (var k in new[] { BuildingKinds.Woodcutter, BuildingKinds.Quarry, BuildingKinds.Sawmill, BuildingKinds.Kiln })
                for (int w = 0; w < Wares.Count; w++) stock[w] -= EconomyDefs.Get(k).Cost[w];
            Assert.IsTrue(stock.All(n => n >= 0), string.Join(",", stock));
        }
    }

    /// <summary>Harvester levels and <see cref="UpgradeBuilding"/>.</summary>
    public class HarvesterUpgradeTests
    {
        /// <summary>A quiet game with a finished woodcutter on a forest hex, joined by road. Returns its id.</summary>
        private static GodSim Woodcutter(out int id, out int hexOut)
        {
            var sim = EcoHelp.QuietWith(Terrain.Forest, out int hex);
            hexOut = hex;
            var t = sim.State.Territory;
            var bot = new EconomyBot(sim, Faction.F0);
            id = 0;
            foreach (int n in t.BuildableNodes(Faction.F0).Where(n => t.Grid.HexOf(n) == hex).ToList())
            {
                if (!sim.Check(new PlaceBuilding(Faction.F0, n, BuildingKinds.Woodcutter)).Ok) continue;
                sim.Submit(new PlaceBuilding(Faction.F0, n, BuildingKinds.Woodcutter)); sim.Step();
                id = t.NodeBuilding[n];
                if (bot.ConnectFlag(t.BuildingById(id).FlagNode)) break;
                sim.Submit(new RemoveBuilding(Faction.F0, id)); sim.Step(); id = 0;
            }
            Assert.AreNotEqual(0, id);
            EconomyModule.CompleteInstantly(sim.State, id);
            return sim;
        }

        [Test]
        public void Upgrade_CarriesTheWares_KeepsWorking_AndRaisesTheLevel_TwiceThenStops()
        {
            var sim = Woodcutter(out int id, out int hex);
            var s = sim.State; var e = s.Economy; var t = s.Territory; var hq = EcoHelp.Hq(sim);
            var b = e.Find(id);
            Assert.AreEqual(1, b.Level);
            EcoHelp.Run(sim, 300, () => e.Walkers.Count == 0);
            int planks = hq.Stock[(int)Ware.Plank], bricks = hq.Stock[(int)Ware.Brick];
            hq.Stock[(int)Ware.Brick] = 0; // the bricks come later, so the harvester works while it waits
            t.Piles[hex] = 3;
            sim.DrainEvents();
            Assert.IsTrue(sim.Submit(new UpgradeBuilding(Faction.F0, id)).Ok);
            sim.Step();
            Assert.IsTrue(b.Upgrading);
            Assert.AreEqual(Reasons.AlreadyUpgrading, sim.Check(new UpgradeBuilding(Faction.F0, id)).Reason);
            Assert.IsTrue(sim.DrainEvents().OfType<BuildingUpgradeStarted>().Any(x => x.BuildingId == id && x.ToLevel == 2));
            EcoHelp.Run(sim, 1500, () => b.Produced >= 2);
            Assert.AreEqual(1, b.Level);
            Assert.IsTrue(b.Upgrading);
            Assert.GreaterOrEqual(b.Produced, 2, "it kept harvesting while the wares came");
            hq.Stock[(int)Ware.Brick] = bricks;
            EcoHelp.Run(sim, 2000, () => b.Level == 2);
            Assert.AreEqual(2, b.Level);
            Assert.IsFalse(b.Upgrading);
            Assert.AreEqual(planks - 2, e.TotalStock(Faction.F0, Ware.Plank), "2 plank carried");
            Assert.AreEqual(bricks - 2, e.TotalStock(Faction.F0, Ware.Brick), "2 brick carried");
            Assert.AreEqual(0, b.In.Sum() + b.Incoming.Sum());
            Assert.IsTrue(sim.DrainEvents().OfType<BuildingUpgraded>().Any(x => x.BuildingId == id && x.Level == 2 && x.Kind == BuildingKinds.Woodcutter));

            // Level 3: 2 mortar, 2 iron, 2 bread.
            hq.Stock[(int)Ware.Iron] = 2; hq.Stock[(int)Ware.Bread] = 2; hq.Stock[(int)Ware.Mortar] = 2;
            Assert.IsTrue(sim.Submit(new UpgradeBuilding(Faction.F0, id)).Ok);
            sim.Step();
            Assert.Greater(e.UpgradeProgressPermille(id), -1);
            EcoHelp.Run(sim, 2000, () => b.Level == 3);
            Assert.AreEqual(3, b.Level);
            Assert.AreEqual(0, hq.Stock[(int)Ware.Iron] + hq.Stock[(int)Ware.Bread] + hq.Stock[(int)Ware.Mortar]);
            Assert.AreEqual(Reasons.MaxLevel, sim.Check(new UpgradeBuilding(Faction.F0, id)).Reason);
            Assert.IsNull(b.UpgradeDef);
        }

        [Test]
        public void Upgrade_WaitsForItsWares_ThenAShortWorkTime()
        {
            var sim = Woodcutter(out int id, out _);
            var e = sim.State.Economy; var hq = EcoHelp.Hq(sim);
            hq.Stock[(int)Ware.Brick] = 0;
            Assert.IsTrue(sim.Submit(new UpgradeBuilding(Faction.F0, id)).Ok);
            EcoHelp.Run(sim, 800);
            var b = e.Find(id);
            Assert.AreEqual(1, b.Level, "no brick, no level");
            Assert.AreEqual(2, b.In[(int)Ware.Plank], "the planks are in");
            Assert.AreEqual(400 * 2 / 4, e.UpgradeProgressPermille(id), "half the wares, no work yet");
            hq.Stock[(int)Ware.Brick] = 2;
            long allIn = -1;
            EcoHelp.Run(sim, 1500, () => { if (allIn < 0 && b.In[(int)Ware.Brick] == 2) allIn = sim.State.Tick; return b.Level == 2; });
            Assert.AreEqual(2, b.Level);
            Assert.That(sim.State.Tick - allIn, Is.InRange(EconomyTuning.UpgradeWorkTicks - 1, EconomyTuning.UpgradeWorkTicks + 1), "the short work time");
        }

        [Test]
        public void Upgrade_IsRefused_ForWrongBuildings()
        {
            var sim = Woodcutter(out int id, out _);
            var e = sim.State.Economy;
            int saw = EcoHelp.Place(sim, BuildingKinds.Sawmill, true);
            int site = 0;
            foreach (var k in BuildingKinds.Harvesters)
                if ((site = new EconomyBot(sim, Faction.F0).PlaceAndConnect(k)) != 0) break;
            Assert.AreNotEqual(0, site, "a harvester site");
            sim.Step();
            Assert.AreEqual(Reasons.NotAHarvester, sim.Check(new UpgradeBuilding(Faction.F0, saw)).Reason);
            Assert.AreEqual(Reasons.NotAHarvester, sim.Check(new UpgradeBuilding(Faction.F0, EcoHelp.Hq(sim).Id)).Reason, "an HQ goes to a Citadel instead");
            Assert.AreEqual(Reasons.NotFinished, sim.Check(new UpgradeBuilding(Faction.F0, site)).Reason);
            Assert.AreEqual(Reasons.NotOwner, sim.Check(new UpgradeBuilding(Faction.F1, id)).Reason);
            Assert.AreEqual(Reasons.NoSuchThing, sim.Check(new UpgradeBuilding(Faction.F0, 99999)).Reason);
            Assert.IsTrue(sim.Check(new UpgradeBuilding(Faction.F0, id)).Ok);
            e.Find(id).Level = 3;
            Assert.AreEqual(Reasons.MaxLevel, sim.Check(new UpgradeBuilding(Faction.F0, id)).Reason);
            Assert.AreEqual("It is already at the top level.", Reasons.Sentence(Reasons.MaxLevel));
        }

        /// <summary>Ticks from piles appearing until the harvester has brought <paramref name="trips"/> loads home, and the wares it made.</summary>
        private static (long ticks, int wares) Trips(int level, int trips)
        {
            var sim = Woodcutter(out int id, out int hex);
            var s = sim.State; var e = s.Economy; var t = s.Territory;
            var b = e.Find(id);
            EcoHelp.Run(sim, 400); // the start-up carriers settle
            b.Level = level;
            int before = e.TotalStock(Faction.F0, Ware.Log);
            t.Piles[hex] = trips;
            long start = s.Tick;
            EcoHelp.Run(sim, 3000, () => b.Produced >= trips);
            long ticks = s.Tick - start;
            EcoHelp.Run(sim, 600, () => b.Out[(int)Ware.Log] == 0 && e.Walkers.Count == 0);
            return (ticks, e.TotalStock(Faction.F0, Ware.Log) - before);
        }

        [Test]
        public void Levels_GiveMoreWaresPerPile_AndGatherFaster()
        {
            var l1 = Trips(1, 2); var l2 = Trips(2, 2); var l3 = Trips(3, 2);
            Assert.AreEqual(2 * 4, l1.wares, "level 1: 4 per pile");
            Assert.AreEqual(2 * 5, l2.wares, "level 2: 5 per pile");
            Assert.AreEqual(2 * 6, l3.wares, "level 3: 6 per pile");
            // A third faster (x3/4 of the time) and twice as fast (x1/2), give or take a tick per step.
            Assert.That(l2.ticks * 4, Is.InRange(l1.ticks * 3 - 16, l1.ticks * 3 + 16), "level 2: " + l2.ticks + " vs " + l1.ticks);
            Assert.That(l3.ticks * 2, Is.InRange(l1.ticks - 8, l1.ticks + 8), "level 3: " + l3.ticks + " vs " + l1.ticks);
            Assert.AreEqual(15, EconomyTuning.GatherTicksAt(2)); Assert.AreEqual(10, EconomyTuning.GatherTicksAt(3));
        }

        [Test]
        public void Level3_MakesOneWareEvery40Seconds_WhenItsHexHasNoPile()
        {
            foreach (int level in new[] { 2, 3 })
            {
                var sim = Woodcutter(out int id, out int hex);
                var s = sim.State; var e = s.Economy;
                e.Find(id).Level = level;
                s.Territory.Piles[hex] = 0;
                EcoHelp.Run(sim, 400);
                int before = e.TotalStock(Faction.F0, Ware.Log) + e.Find(id).Out[(int)Ware.Log];
                EcoHelp.Run(sim, 4 * EconomyTuning.TrickleTicks);
                EcoHelp.Run(sim, 300, () => e.Walkers.Count == 0 && e.Find(id).Out[(int)Ware.Log] == 0);
                int made = e.TotalStock(Faction.F0, Ware.Log) + e.Find(id).Out[(int)Ware.Log] - before;
                Assert.AreEqual(0, e.Find(id).Produced, "no pile was taken");
                if (level == 3) Assert.That(made, Is.InRange(4, 5), "one ware per 40 s (" + made + ")");
                else Assert.AreEqual(0, made, "level 2 stands idle without piles");
            }
        }

        [Test]
        public void Upgrade_StateSurvivesASave()
        {
            var sim = Woodcutter(out int id, out _);
            Assert.IsTrue(sim.Submit(new UpgradeBuilding(Faction.F0, id)).Ok);
            EcoHelp.Run(sim, 60);
            Assert.IsTrue(sim.State.Economy.Find(id).Upgrading);
            var loaded = SaveFile.FromJson(SaveFile.From(sim).ToJson()).ToSim();
            Assert.AreEqual(sim.StateHash(), loaded.StateHash());
            EcoHelp.Run(sim, 1500); EcoHelp.Run(loaded, 1500);
            Assert.AreEqual(2, loaded.State.Economy.Find(id).Level);
            Assert.AreEqual(sim.StateHash(), loaded.StateHash());
        }
    }

    public class Stage3TradeValueTests
    {
        [Test]
        public void EveryStockWare_HasATradeValue_ProcessedIsItsRaws_GearAddsTheWork()
        {
            foreach (var w in Wares.Raws) Assert.That(TradeTuning.ValueOf(w), Is.InRange(2, 3), w.ToString());
            foreach (var w in Wares.Processed)
            {
                var maker = EconomyDefs.MakerOf(w);
                int sum = Enumerable.Range(0, Wares.Count).Sum(i => maker.In[i] * TradeTuning.ValueOf((Ware)i));
                Assert.AreEqual(sum, TradeTuning.ValueOf(w), w.ToString());
                Assert.IsTrue(TradeTuning.IsTradable(w));
            }
            foreach (var g in Gear.Attack.Concat(Gear.Defence))
            {
                var maker = EconomyDefs.MakerOf(g);
                int sum = Enumerable.Range(0, Wares.Count).Sum(i => maker.In[i] * TradeTuning.ValueOf((Ware)i));
                Assert.AreEqual(sum + TradeTuning.GearWorkValue, TradeTuning.ValueOf(g), g.ToString());
                Assert.IsTrue(TradeTuning.IsTradable(g));
            }
            Assert.AreEqual(4, TradeTuning.ValueOf(Ware.Brick)); Assert.AreEqual(5, TradeTuning.ValueOf(Ware.Plank)); Assert.AreEqual(13, TradeTuning.ValueOf(Ware.Sword));
            Assert.IsFalse(TradeTuning.IsTradable(Ware.Kindling)); Assert.IsFalse(TradeTuning.IsTradable(Ware.Rock));
            Assert.AreEqual(0, TradeTuning.ValueOf(Ware.Kindling));
        }

        [Test]
        public void Brain_KnowsWhichLandEachNewWareNeeds()
        {
            // Find a faction whose start area lacks some land, and check processed wares and gear follow their raws.
            var sim = GodSim.Create(GameSetup.Watch(3));
            foreach (var f in sim.State.Setup.Factions)
            {
                var brain = new AiBrain(sim, f);
                brain.WareNeeds(); // refreshes its look
                bool noForest = !brain.HasLand(Terrain.Forest), noMountain = !brain.HasLand(Terrain.Mountain);
                Assert.AreEqual(noForest, brain.LacksLandFor(Ware.Log));
                Assert.AreEqual(noForest || noMountain, brain.LacksLandFor(Ware.Plank), "plank = log + stone");
                Assert.AreEqual(noMountain, brain.LacksLandFor(Ware.Steel), "steel = stone + ore");
                Assert.AreEqual(noForest || noMountain, brain.LacksLandFor(Ware.Axe), "axe = iron + plank");
                Assert.IsFalse(brain.LacksLandFor(Ware.Kindling));
            }
        }
    }
}
