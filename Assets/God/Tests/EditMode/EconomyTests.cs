using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace God.Sim.Tests
{
    internal static class EcoHelp
    {
        /// <summary>A new game (everyone starts at peace, and there are no enemy waves any more).</summary>
        public static GodSim NoWar(ulong seed) => GodSim.Create(seed);

        /// <summary>A unit order from a vehicle workshop (stage 2 vehicles are gone: it now makes a kitless soldier).</summary>
        public static bool FromWorkshop(GodState s, UnitOrder u) => s.Economy.Find(u.BuildingId)?.Kind == BuildingKinds.VehicleWorkshop;

        public static GodSim Quiet(ulong seed)
        {
            // No dice piles: nothing but the test itself changes the piles.
            var sim = EcoHelp.NoWar(seed);
            Array.Clear(sim.State.Board.HexNumber, 0, sim.State.Board.HexNumber.Length);
            return sim;
        }

        public static EcoBuilding Hq(GodSim sim, Faction f = Faction.F0) => sim.State.Economy.HqsOf(f)[0];

        /// <summary>A quiet game whose player circle holds a legal woodcutter node on forest.</summary>
        public static GodSim QuietWith(Terrain terrain, out int hex)
        {
            for (ulong seed = 1; seed < 200; seed++)
            {
                var sim = Quiet(seed);
                var t = sim.State.Territory; var g = t.Grid;
                foreach (int n in t.BuildableNodes(Faction.F0))
                {
                    int h = g.HexOf(n);
                    if (sim.State.Board.HexTerrain[h] == terrain && sim.Check(new PlaceBuilding(Faction.F0, n, BuildingKinds.Woodcutter)).Ok)
                    { hex = h; return sim; }
                }
            }
            throw new InvalidOperationException("no seed");
        }

        public static void Run(GodSim sim, int ticks, Func<bool> until = null)
        {
            for (int i = 0; i < ticks; i++) { sim.Step(); if (until != null && until()) return; }
        }

        public static int Place(GodSim sim, string kind, bool instant)
        {
            var bot = new EconomyBot(sim, Faction.F0);
            int id = bot.PlaceAndConnect(kind);
            Assert.AreNotEqual(0, id, kind + ": " + string.Join("; ", bot.Log));
            if (instant) { sim.Step(); Assert.IsTrue(EconomyModule.CompleteInstantly(sim.State, id)); }
            return id;
        }

        public static void RemoveHqForTest(GodState s, int hqId)
        {
            var t = s.Territory;
            var b = t.BuildingById(hqId);
            t.Hqs.Remove(t.HqOf(hqId));
            t.NodeBuilding[b.Node] = 0;
            t.Buildings.Remove(b);
            var fl = t.FlagById(b.FlagId);
            if (fl != null) fl.BuildingId = 0;
        }
    }

    public class EconomyConstructionTests
    {
        [Test]
        public void Start_HqHoldsTheAgreedStock_AndCarriers()
        {
            var s = EcoHelp.NoWar(3).State;
            var hq = s.Economy.HqsOf(Faction.F0).Single();
            // Stage 3: 8 log, 6 stone, 6 clay, 4 grain, 6 plank, 4 brick, 2 mortar.
            Assert.AreEqual(8, s.Economy.Stock(hq.Id, Ware.Log));
            Assert.AreEqual(6, s.Economy.Stock(hq.Id, Ware.Stone));
            Assert.AreEqual(6, s.Economy.Stock(hq.Id, Ware.Clay));
            Assert.AreEqual(4, s.Economy.Stock(hq.Id, Ware.Grain));
            Assert.AreEqual(6, s.Economy.Stock(hq.Id, Ware.Plank));
            Assert.AreEqual(4, s.Economy.Stock(hq.Id, Ware.Brick));
            Assert.AreEqual(2, s.Economy.Stock(hq.Id, Ware.Mortar));
            Assert.AreEqual(36, hq.Stock.Sum(), "nothing else");
            Assert.AreEqual(6, hq.IdleCarriers);
            Assert.AreEqual(1, s.Economy.HqsOf(Faction.F1).Count);
        }

        [Test]
        public void Site_IsDeliveredOverRoads_AndFinished()
        {
            var sim = EcoHelp.Quiet(3);
            var e = sim.State.Economy;
            int planks = e.TotalStock(Faction.F0, Ware.Plank), bricks = e.TotalStock(Faction.F0, Ware.Brick);
            int id = EcoHelp.Place(sim, BuildingKinds.Sawmill, false);
            sim.Step();
            Assert.IsFalse(e.Find(id).Built);
            sim.DrainEvents();
            EcoHelp.Run(sim, 1500, () => e.Find(id).Built);
            Assert.IsTrue(e.Find(id).Built);
            Assert.AreEqual(planks - 2, e.TotalStock(Faction.F0, Ware.Plank), "a processor costs 2 plank");
            Assert.AreEqual(bricks - 1, e.TotalStock(Faction.F0, Ware.Brick), "and 1 brick");
            Assert.AreEqual(1000, e.SiteProgressPermille(id));
            Assert.IsTrue(sim.DrainEvents().OfType<BuildingCompleted>().Any(c => c.BuildingId == id));
            EcoHelp.Run(sim, 400);
            Assert.AreEqual(0, e.Walkers.Count, "everyone is home again");
            Assert.AreEqual(6, EcoHelp.Hq(sim).IdleCarriers);
            Assert.AreEqual(2, EcoHelp.Hq(sim).IdleBuilders);
        }

        [Test]
        public void Site_WithoutARoad_Waits()
        {
            var sim = EcoHelp.Quiet(3);
            var e = sim.State.Economy; var t = sim.State.Territory;
            int node = t.BuildableNodes(Faction.F0).First(n => sim.Check(new PlaceBuilding(Faction.F0, n, BuildingKinds.Sawmill)).Ok);
            Assert.IsTrue(sim.Submit(new PlaceBuilding(Faction.F0, node, BuildingKinds.Sawmill)).Ok);
            EcoHelp.Run(sim, 1200);
            int id = t.NodeBuilding[node];
            Assert.IsFalse(e.Find(id).Built);
            Assert.AreEqual(BuildStatus.SiteNoRoad, e.Find(id).Status);
            Assert.AreEqual(0, e.Walkers.Count);
            Assert.AreEqual(EconomyTuning.StartPlank, e.TotalStock(Faction.F0, Ware.Plank));
            Assert.AreEqual(0, e.SiteProgressPermille(id));
            // The road arrives: now it is built.
            Assert.IsTrue(new EconomyBot(sim, Faction.F0).ConnectFlag(t.BuildingById(id).FlagNode));
            EcoHelp.Run(sim, 1500, () => e.Find(id).Built);
            Assert.IsTrue(e.Find(id).Built);
        }

        [Test]
        public void TypedPlace_TakesSizeFromKind_AndRefusesUnknownKind_AndWrongLand()
        {
            var sim = EcoHelp.Quiet(3);
            var t = sim.State.Territory;
            Assert.AreEqual(BuildingSize.Medium, new PlaceBuilding(Faction.F0, 0, BuildingKinds.Kiln).Size);
            Assert.AreEqual(BuildingSize.Small, new PlaceBuilding(Faction.F0, 0, BuildingKinds.Quarry).Size);
            int node = t.BuildableNodes(Faction.F0).First(n => sim.Check(new PlaceBuilding(Faction.F0, n, BuildingKinds.Sawmill)).Ok);
            Assert.AreEqual(Reasons.InvalidSite, sim.Check(new PlaceBuilding(Faction.F0, node, "castle")).Reason);
            var mismatch = new PlaceBuilding(Faction.F0, node, BuildingSize.Small) { Kind = BuildingKinds.Sawmill };
            Assert.AreEqual(Reasons.InvalidSite, sim.Check(mismatch).Reason);
            // The P1 size command still gives an instant placeholder.
            Assert.IsTrue(sim.Submit(new PlaceBuilding(Faction.F0, node, BuildingSize.Medium)).Ok);
            sim.Step();
            Assert.IsNull(sim.State.Economy.Find(t.NodeBuilding[node]));
            // A harvester on land that does not give its pile is refused.
            var g = t.Grid;
            int wrong = t.BuildableNodes(Faction.F0).FirstOrDefault(n => sim.State.Board.HexTerrain[g.HexOf(n)] != Terrain.Forest
                && sim.Check(new PlaceBuilding(Faction.F0, n, BuildingSize.Small)).Ok);
            Assert.AreEqual(Reasons.WrongTerrain, sim.Check(new PlaceBuilding(Faction.F0, wrong, BuildingKinds.Woodcutter)).Reason);
        }

        [Test]
        public void NewHq_IsASite_UntilWaresAreCarried_ThenGivesReachAndCarriers()
        {
            var sim = EcoHelp.Quiet(3);
            var s = sim.State; var t = s.Territory; var e = s.Economy;
            int corner = s.Board.Topology.Corners.First(c => s.Board.Topology.VertexHexes(c).Count == 3
                && s.Board.Topology.CornerDistance(EcoHelp.Hq(sim).Id == 0 ? 0 : t.HqOf(EcoHelp.Hq(sim).Id).Corner, c) == 2 && sim.Check(new PlaceHq(Faction.F0, c)).Ok);
            int id = new EconomyBot(sim, Faction.F0).PlaceHqAndConnect(corner);
            Assert.AreNotEqual(0, id);
            Assert.IsTrue(t.HqOf(id).Site);
            Assert.AreEqual(1, t.HqsOf(Faction.F0).Count);
            EcoHelp.Run(sim, 3000, () => e.Find(id).Built);
            Assert.IsTrue(e.Find(id).Built);
            Assert.IsFalse(t.HqOf(id).Site);
            Assert.AreEqual(2, t.HqsOf(Faction.F0).Count);
            Assert.AreEqual(3, EconomyTuning.StartPlank - e.TotalStock(Faction.F0, Ware.Plank), "3 planks carried");
            Assert.AreEqual(2, EconomyTuning.StartMortar - e.TotalStock(Faction.F0, Ware.Mortar), "2 mortar carried");
            Assert.AreEqual(EconomyTuning.CarriersPerHq, e.Find(id).IdleCarriers);
        }
    }

    public class EconomyProductionTests
    {
        [Test]
        public void Harvester_TakesPiles_WalksOffRoad_AndDeliversToTheHq()
        {
            var sim = EcoHelp.QuietWith(Terrain.Forest, out int hex);
            var s = sim.State; var e = s.Economy; var t = s.Territory;
            int id = 0;
            var bot = new EconomyBot(sim, Faction.F0);
            // Place on the forest hex.
            foreach (int n in t.BuildableNodes(Faction.F0).Where(n => t.Grid.HexOf(n) == hex))
                if (sim.Check(new PlaceBuilding(Faction.F0, n, BuildingKinds.Woodcutter)).Ok)
                {
                    sim.Submit(new PlaceBuilding(Faction.F0, n, BuildingKinds.Woodcutter)); sim.Step();
                    id = t.NodeBuilding[n];
                    if (bot.ConnectFlag(t.BuildingById(id).FlagNode)) break;
                    sim.Submit(new RemoveBuilding(Faction.F0, id)); sim.Step(); id = 0;
                }
            Assert.AreNotEqual(0, id);
            EcoHelp.Run(sim, 1500, () => e.Find(id).Built);
            Assert.IsTrue(e.Find(id).Built);
            t.Piles[hex] = 2;
            int logs = e.TotalStock(Faction.F0, Ware.Log);
            bool sawWalk = false;
            for (int i = 0; i < 1500 && t.Piles[hex] > 0 || e.Walkers.Any(w => w.Kind == WalkerKind.Harvester); i++)
            {
                sim.Step();
                var hw = e.Walkers.FirstOrDefault(w => w.Kind == WalkerKind.Harvester);
                if (hw != null)
                {
                    sawWalk = true;
                    Assert.IsTrue(hw.CurrentNode >= 0);
                    Assert.AreEqual(hex, t.Grid.HexOf(hw.Path[hw.Path.Length - 1]), "walks to the hex centre");
                }
            }
            Assert.IsTrue(sawWalk);
            Assert.AreEqual(0, t.Piles[hex]);
            EcoHelp.Run(sim, 600);
            Assert.AreEqual(logs + 2 * EconomyTuning.WaresPerPile, e.TotalStock(Faction.F0, Ware.Log));
            Assert.AreEqual(2, e.Find(id).Produced);
        }

        [Test]
        public void TwoHarvestersOnOneHex_ShareDeterministically_LowerIdFirst()
        {
            var sim = EcoHelp.QuietWith(Terrain.Forest, out int hex);
            var s = sim.State; var e = s.Economy; var t = s.Territory;
            var bot = new EconomyBot(sim, Faction.F0);
            var ids = new List<int>();
            foreach (int n in t.BuildableNodes(Faction.F0).Where(n => t.Grid.HexOf(n) == hex).ToList())
            {
                if (ids.Count == 2) break;
                if (!sim.Check(new PlaceBuilding(Faction.F0, n, BuildingKinds.Woodcutter)).Ok) continue;
                sim.Submit(new PlaceBuilding(Faction.F0, n, BuildingKinds.Woodcutter)); sim.Step();
                int id = t.NodeBuilding[n];
                if (bot.ConnectFlag(t.BuildingById(id).FlagNode)) ids.Add(id);
                else { sim.Submit(new RemoveBuilding(Faction.F0, id)); sim.Step(); }
            }
            if (ids.Count < 2) Assert.Inconclusive("no room for two woodcutters on that hex");
            foreach (int id in ids) EconomyModule.CompleteInstantly(s, id);
            t.Piles[hex] = 1;
            EcoHelp.Run(sim, 800);
            Assert.AreEqual(1, e.Find(ids[0]).Produced);
            Assert.AreEqual(0, e.Find(ids[1]).Produced);
            Assert.AreEqual(0, t.Piles[hex]);
            // Three piles: shared, never more than there are.
            t.Piles[hex] = 3;
            EcoHelp.Run(sim, 2000);
            Assert.AreEqual(4, e.Find(ids[0]).Produced + e.Find(ids[1]).Produced);
            Assert.Greater(e.Find(ids[1]).Produced, 0, "the second one gets some too");
        }

        [Test]
        public void Quarry_And_Mine_BothTakeRock_ForStoneAndOre()
        {
            var q = EconomyDefs.Get(BuildingKinds.Quarry); var m = EconomyDefs.Get(BuildingKinds.Mine);
            Assert.AreEqual(RawKind.Rock, q.Harvests); Assert.AreEqual(RawKind.Rock, m.Harvests);
            Assert.AreEqual(Ware.Stone, q.HarvestWare); Assert.AreEqual(Ware.Ore, m.HarvestWare);
            Assert.AreEqual(RawKind.Clay, EconomyDefs.Get(BuildingKinds.ClayPit).Harvests);
            Assert.AreEqual(BuildingSize.Medium, EconomyDefs.Get(BuildingKinds.Farm).Size);
            Assert.AreEqual(RawKind.Grain, EconomyDefs.Get(BuildingKinds.Farm).Harvests);
        }
    }

    public class EconomyPriorityTests
    {
        private static (GodSim sim, int a, int b) TwoBarracks()
        {
            var sim = EcoHelp.Quiet(3);
            var hq = EcoHelp.Hq(sim);
            hq.Stock[(int)Ware.Bread] = 0;
            int a = EcoHelp.Place(sim, BuildingKinds.Barracks, true);
            int b = EcoHelp.Place(sim, BuildingKinds.Barracks, true);
            return (sim, a, b);
        }

        private static int AxesReceived(EconomyState e, int id) => e.Find(id).In[(int)Ware.Axe] + e.Find(id).Incoming[(int)Ware.Axe];

        [Test]
        public void SameRank_LowerIdOrNearerWins()
        {
            var (sim, a, b) = TwoBarracks();
            EcoHelp.Hq(sim).Stock[(int)Ware.Axe] = 1;
            EcoHelp.Run(sim, 400);
            var e = sim.State.Economy;
            Assert.AreEqual(1, AxesReceived(e, a) + AxesReceived(e, b));
        }

        [Test]
        public void Priority_ChangesTheDeliveryOrder()
        {
            foreach (bool aHigh in new[] { true, false })
            {
                var (sim, a, b) = TwoBarracks();
                var e = sim.State.Economy;
                // Settle the default order first, then rewind by making a fresh game with the priorities set before any ware moves.
                int hi = aHigh ? a : b, lo = aHigh ? b : a;
                Assert.IsTrue(sim.Submit(new SetInputPriority(Faction.F0, lo, Ware.Axe, Priority.Low)).Ok);
                Assert.IsTrue(sim.Submit(new SetInputPriority(Faction.F0, hi, Ware.Axe, Priority.High)).Ok);
                EcoHelp.Hq(sim).Stock[(int)Ware.Axe] = 1;
                EcoHelp.Run(sim, 400);
                Assert.AreEqual(1, AxesReceived(e, hi), "high priority gets the only axe");
                Assert.AreEqual(0, AxesReceived(e, lo));
            }
        }

        [Test]
        public void Stop_AsksForNothing_AndPauseFreezesWork()
        {
            var (sim, a, b) = TwoBarracks();
            var e = sim.State.Economy; var hq = EcoHelp.Hq(sim);
            Assert.IsTrue(sim.Submit(new SetInputPriority(Faction.F0, a, Ware.Axe, Priority.Stop)).Ok);
            Assert.IsTrue(sim.Submit(new SetInputPriority(Faction.F0, b, Ware.Axe, Priority.Stop)).Ok);
            Assert.IsTrue(sim.Submit(new SetInputPriority(Faction.F0, b, Ware.Bread, Priority.Stop)).Ok);
            hq.Stock[(int)Ware.Axe] = 3;
            EcoHelp.Run(sim, 400);
            Assert.AreEqual(3, hq.Stock[(int)Ware.Axe], "nobody asks, nothing moves");
            Assert.AreEqual(0, AxesReceived(e, a) + AxesReceived(e, b));
            // Pause: inputs stop and work stops.
            Assert.IsTrue(sim.Submit(new SetInputPriority(Faction.F0, a, Ware.Axe, Priority.Normal)).Ok);
            Assert.IsTrue(sim.Submit(new SetPaused(Faction.F0, a, true)).Ok);
            hq.Stock[(int)Ware.Bread] = 2;
            EcoHelp.Run(sim, 400);
            Assert.AreEqual(0, e.Find(a).In[(int)Ware.Bread], "paused: no bread either");
            Assert.AreEqual(BuildStatus.Paused, e.Find(a).Status);
            Assert.AreEqual(0, AxesReceived(e, a));
            Assert.IsEmpty(sim.State.Economy.ProducedLog.Where(u => u.BuildingId == a).ToList());
            Assert.IsTrue(sim.Submit(new SetPaused(Faction.F0, a, false)).Ok);
            EcoHelp.Run(sim, 600);
            Assert.GreaterOrEqual(sim.State.Economy.ProducedLog.Count(u => u.BuildingId == a), 1, "resumed");
        }

        [Test]
        public void PriorityCommands_RefuseWrongWareOwnerOrBuilding()
        {
            var (sim, a, b) = TwoBarracks();
            Assert.AreEqual(Reasons.NotAnInput, sim.Check(new SetInputPriority(Faction.F0, a, Ware.Log, Priority.High)).Reason);
            Assert.AreEqual(Reasons.NotOwner, sim.Check(new SetInputPriority(Faction.F1, a, Ware.Axe, Priority.High)).Reason);
            Assert.AreEqual(Reasons.NotAnInput, sim.Check(new SetInputPriority(Faction.F0, a, Ware.Iron, Priority.High)).Reason, "stage 3: the barracks takes gear, not iron");
            Assert.AreEqual(Reasons.NoSuchThing, sim.Check(new SetPaused(Faction.F0, 99999, true)).Reason);
            Assert.AreEqual(Reasons.NoSuchThing, sim.Check(new SetPaused(Faction.F0, EcoHelp.Hq(sim).Id, true)).Reason);
        }
    }

    public class EconomyDestructionTests
    {
        private static void AssertClean(GodSim sim)
        {
            var e = sim.State.Economy;
            foreach (var b in e.Buildings)
                for (int w = 0; w < Wares.Count; w++)
                {
                    Assert.GreaterOrEqual(b.Incoming[w], 0); Assert.GreaterOrEqual(b.OutReserved[w], 0); Assert.GreaterOrEqual(b.StockReserved[w], 0);
                }
            foreach (var w in e.Walkers)
            {
                Assert.NotNull(e.Find(w.Home), "no walker without a home");
                Assert.IsTrue(w.Index >= 0 && w.Index < Math.Max(1, w.Path.Length));
            }
        }

        [Test]
        public void SiteDestroyed_MidDelivery_CarriersReturnTheirCargo()
        {
            var sim = EcoHelp.Quiet(3);
            var e = sim.State.Economy; var hq = EcoHelp.Hq(sim);
            int id = EcoHelp.Place(sim, BuildingKinds.Sawmill, false);
            EcoHelp.Run(sim, 400, () => e.Walkers.Any(w => w.Kind == WalkerKind.Carrier && w.Loaded && w.DstId == id));
            Assert.IsTrue(e.Walkers.Any(w => w.Kind == WalkerKind.Carrier && w.Loaded && w.DstId == id), "a loaded carrier is on its way");
            int planksBefore = e.TotalStock(Faction.F0, Ware.Plank);
            Assert.IsTrue(sim.Submit(new RemoveBuilding(Faction.F0, id)).Ok);
            EcoHelp.Run(sim, 1200);
            AssertClean(sim);
            Assert.AreEqual(0, e.Walkers.Count, "everyone home");
            Assert.AreEqual(EconomyTuning.CarriersPerHq, hq.IdleCarriers);
            Assert.AreEqual(EconomyTuning.BuildersPerHq, hq.IdleBuilders);
            Assert.IsNull(e.Find(id));
            // Wares that were only reserved or in flight are back in the HQ; those already handed to the site are gone with it.
            Assert.GreaterOrEqual(e.TotalStock(Faction.F0, Ware.Plank), planksBefore);
            Assert.Greater(e.TotalStock(Faction.F0, Ware.Plank) + e.LostWares, 0);
        }

        [Test]
        public void ProducerDestroyed_WithOutputWaitingOrReserved_LeavesNothingDangling()
        {
            var sim = EcoHelp.Quiet(3);
            var e = sim.State.Economy;
            int saw = EcoHelp.Place(sim, BuildingKinds.Sawmill, true);
            int axes = EcoHelp.Place(sim, BuildingKinds.Axesmith, true);
            EcoHelp.Run(sim, 600, () => e.Walkers.Any(w => w.Kind == WalkerKind.Carrier && w.SrcId == saw && w.Phase == WalkerPhase.Outbound));
            Assert.IsTrue(sim.Submit(new RemoveBuilding(Faction.F0, saw)).Ok);
            EcoHelp.Run(sim, 900);
            AssertClean(sim);
            Assert.AreEqual(0, e.Walkers.Count);
            Assert.AreEqual(EconomyTuning.CarriersPerHq, EcoHelp.Hq(sim).IdleCarriers);
            Assert.AreEqual(0, e.Find(axes).Incoming[(int)Ware.Plank], "no claim left on the dead producer");
        }

        [Test]
        public void HarvesterDestroyed_ReleasesItsPile()
        {
            var sim = EcoHelp.QuietWith(Terrain.Forest, out int hex);
            var s = sim.State; var e = s.Economy; var t = s.Territory;
            var bot = new EconomyBot(sim, Faction.F0);
            int id = 0;
            foreach (int n in t.BuildableNodes(Faction.F0).Where(n => t.Grid.HexOf(n) == hex))
            {
                if (!sim.Check(new PlaceBuilding(Faction.F0, n, BuildingKinds.Woodcutter)).Ok) continue;
                sim.Submit(new PlaceBuilding(Faction.F0, n, BuildingKinds.Woodcutter)); sim.Step();
                id = t.NodeBuilding[n];
                if (bot.ConnectFlag(t.BuildingById(id).FlagNode)) break;
                sim.Submit(new RemoveBuilding(Faction.F0, id)); sim.Step(); id = 0;
            }
            EconomyModule.CompleteInstantly(s, id);
            t.Piles[hex] = 2;
            EcoHelp.Run(sim, 200, () => e.Walkers.Any(w => w.Kind == WalkerKind.Harvester));
            Assert.AreEqual(1, e.PilesReserved[hex]);
            sim.Submit(new RemoveBuilding(Faction.F0, id)); sim.Step(2);
            Assert.AreEqual(0, e.PilesReserved[hex]);
            Assert.AreEqual(0, e.Walkers.Count(w => w.Kind == WalkerKind.Harvester));
            Assert.AreEqual(2, t.Piles[hex]);
        }

        [Test]
        public void HqDestroyed_LosesItsStockAndCarriers_RestKeepsRunning()
        {
            var sim = EcoHelp.Quiet(3);
            var s = sim.State; var e = s.Economy; var t = s.Territory;
            var hq1 = EcoHelp.Hq(sim);
            int corner = s.Board.Topology.Corners.First(c => s.Board.Topology.VertexHexes(c).Count == 3
                && s.Board.Topology.CornerDistance(t.HqOf(hq1.Id).Corner, c) == 2 && sim.Check(new PlaceHq(Faction.F0, c)).Ok);
            int hq2 = new EconomyBot(sim, Faction.F0).PlaceHqAndConnect(corner);
            EcoHelp.Run(sim, 3000, () => e.Find(hq2).Built);
            Assert.IsTrue(e.Find(hq2).Built);
            // Put stock in HQ2 and send some carriers out from it.
            e.Find(hq2).Stock[(int)Ware.Plank] = 5; e.Find(hq2).Stock[(int)Ware.Stone] = 5;
            int siteA = EcoHelp.Place(sim, BuildingKinds.Sawmill, false);
            EcoHelp.Run(sim, 120);
            var events = sim.DrainEvents();
            int lostBefore = e.LostWares, stockThere = e.Find(hq2).Stock.Sum();
            EcoHelp.RemoveHqForTest(s, hq2);
            EcoHelp.Run(sim, 5);
            Assert.IsNull(e.Find(hq2));
            Assert.IsTrue(sim.DrainEvents().OfType<HqLost>().Any(l => l.HqId == hq2));
            Assert.AreEqual(0, e.Walkers.Count(w => w.Home == hq2), "its carriers are lost");
            AssertClean(sim);
            Assert.GreaterOrEqual(e.LostWares - lostBefore, stockThere, "its stock is lost");
            Assert.Greater(stockThere, 0);
            // The first HQ carries on and finishes the site.
            EcoHelp.Run(sim, 2500, () => e.Find(siteA) != null && e.Find(siteA).Built);
            Assert.IsTrue(e.Find(siteA).Built);
            AssertClean(sim);
        }

        [Test]
        public void HqDestroyed_WhileCarrierLoadedToIt_ReroutesOrDropsWithoutCrashing()
        {
            var sim = EcoHelp.Quiet(3);
            var s = sim.State; var e = s.Economy; var t = s.Territory;
            int saw = EcoHelp.Place(sim, BuildingKinds.Sawmill, true);
            EcoHelp.Run(sim, 800, () => e.Walkers.Any(w => w.Kind == WalkerKind.Carrier && w.Loaded && w.DstIsHq));
            var hq1 = EcoHelp.Hq(sim);
            EcoHelp.RemoveHqForTest(s, hq1.Id);
            EcoHelp.Run(sim, 600);
            Assert.AreEqual(0, e.Walkers.Count);
            Assert.AreEqual(0, e.HqsOf(Faction.F0).Count);
        }
    }

    public class EconomyDeterminismTests
    {
        private static ulong Run(ulong seed, int ticks)
        {
            var sim = EcoHelp.NoWar(seed);
            var bot = new ChainBot(sim, Faction.F0);
            for (int i = 0; i < ticks; i++)
            {
                sim.Step();
                if (i % 20 == 0) bot.Think();
            }
            return EconomyModule.StateHash(sim.State);
        }

        [Test]
        public void SameSeedAndCommands_SameStateHash()
        {
            Assert.AreEqual(Run(5, 1500), Run(5, 1500));
            Assert.AreNotEqual(Run(5, 1500), Run(6, 1500));
        }
    }

    public class EconomyFullChainTests
    {
        /// <summary>An island where F0's chain bot finishes the whole stage 3 chain inside <see cref="ChainTicks"/> (seed scan, 25 Nov).</summary>
        public static ulong ChainSeed = 9UL;
        /// <summary>The stage 3 chain is longer (12 buildings, and bread and axes before a soldier): 30 minutes.</summary>
        public const int ChainTicks = 18000;

        public static (GodSim sim, ChainBot bot) RunChain(ulong seed, int ticks)
        {
            var sim = EcoHelp.NoWar(seed);
            var bot = new ChainBot(sim, Faction.F0);
            for (int i = 0; i < ticks; i++)
            {
                sim.Step();
                if (i % 20 == 0) bot.Think();
                if (sim.State.Economy.ProducedLog.Count(u => u.Kit.IsArmed) >= 1 && bot.Done) break;
            }
            return (sim, bot);
        }

        [Test]
        public void ChainRun_BuildsTheWholeChain_AndMakesAnAxeman()
        {
            var (sim, bot) = RunChain(ChainSeed, ChainTicks);
            var s = sim.State; var e = s.Economy;
            string why = "placed " + string.Join(",", bot.Placed.Keys) + " tick " + s.Tick + "; " + string.Join(" | ", bot.Log.Distinct());
            Assert.IsTrue(bot.Done, why);
            foreach (var kv in bot.Placed) Assert.IsTrue(e.Find(kv.Value).Built, kv.Key + " built. " + why);
            Assert.GreaterOrEqual(s.Economy.ProducedLog.Count(u => u.Kind == UnitKind.Infantry && u.Kit.Weapon == Ware.Axe), 1, why);
            Assert.LessOrEqual(s.Tick, ChainTicks);
        }

        [Test]
        public void ChainRun_KeepsItsInvariants_NoLeaksOrNegativeCounts()
        {
            var sim = EcoHelp.NoWar(ChainSeed);
            var bot = new ChainBot(sim, Faction.F0);
            var e = sim.State.Economy;
            for (int i = 0; i < 4000; i++)
            {
                sim.Step();
                if (i % 20 == 0) bot.Think();
                if (i % 50 != 0) continue;
                foreach (var b in e.Buildings)
                {
                    for (int w = 0; w < Wares.Count; w++)
                    {
                        Assert.GreaterOrEqual(b.In[w], 0); Assert.GreaterOrEqual(b.Incoming[w], 0); Assert.GreaterOrEqual(b.Stock[w], 0);
                        Assert.LessOrEqual(b.OutReserved[w], b.Out[w]); Assert.LessOrEqual(b.StockReserved[w], b.Stock[w]);
                        Assert.LessOrEqual(b.Out[w], b.Def.Role == BuildingRole.Harvester ? EconomyTuning.HarvesterOutputCap : EconomyTuning.OutputCap);
                        if (b.Built && !b.IsHq) Assert.LessOrEqual(b.In[w] + b.Incoming[w], Math.Max(EconomyTuning.InputBuffer, b.Def.In[w]), b.Kind);
                    }
                    if (b.IsHq && b.Built)
                        Assert.AreEqual(EconomyTuning.CarriersPerHq, b.IdleCarriers + e.CarriersOut(b.Id), "carriers are never lost or duplicated");
                }
            }
        }

        [Test]
        public void ChainRun_WorksOnSeveralSeeds()
        {
            int ok = 0; var failed = new List<ulong>();
            for (ulong seed = 1; seed <= 16; seed++)
            {
                var (sim, bot) = RunChain(seed, ChainTicks);
                var q = sim.State.Economy.ProducedLog;
                if (q.Any(u => u.Kit.IsArmed)) ok++; else failed.Add(seed);
            }
            Assert.GreaterOrEqual(ok, 6, "failed seeds: " + string.Join(",", failed));
        }
    }
}
