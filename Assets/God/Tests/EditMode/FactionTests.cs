using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>Six starts, game setup (P7).</summary>
    public class FactionStartTests
    {
        [Test]
        public void SixStarts_AreLegal_Spread_AndDeterministic()
        {
            var seen = new HashSet<string>();
            for (ulong seed = 1; seed <= 40; seed++)
            {
                var a = GodSim.Create(seed).State;
                var b = GodSim.Create(seed).State;
                var topo = a.Board.Topology; var t = a.Territory; var g = t.Grid;
                var land = BoardGenerator.LandCorners(a.Board, topo);
                Assert.AreEqual(6, t.Hqs.Count);
                var corners = new int[6];
                for (int f = 0; f < 6; f++)
                {
                    var hq = t.HqsOf((Faction)f).Single();
                    corners[f] = hq.Corner;
                    Assert.AreEqual(hq.Corner, b.Territory.HqsOf((Faction)f).Single().Corner, "deterministic");
                    CollectionAssert.Contains(land, hq.Corner, "a land corner");
                    Assert.IsTrue(g.IsLand(g.FlagNodeOf(hq.Node)), "its flag is on land");
                    Assert.IsFalse(hq.Site);
                }
                int min = int.MaxValue;
                for (int i = 0; i < 6; i++)
                    for (int j = i + 1; j < 6; j++) min = Math.Min(min, topo.CornerDistance(corners[i], corners[j]));
                Assert.GreaterOrEqual(min, 3, "spread: every pair at least three hex edges apart, past the distance rule (seed " + seed + ")");
                // Spread round the island: the starts cover every side of the middle.
                double cx = Enumerable.Range(0, topo.HexCount).Average(h => topo.HexX(h)), cz = Enumerable.Range(0, topo.HexCount).Average(h => topo.HexZ(h));
                var sectors = corners.Select(c => (int)Math.Floor((Math.Atan2(topo.VertexZ(c) - cz, topo.VertexX(c) - cx) + Math.PI) / (Math.PI / 2)) % 4).Distinct().Count();
                Assert.GreaterOrEqual(sectors, 3, "the starts face at least three quarters of the island");
                seen.Add(string.Join(",", corners));
            }
            Assert.Greater(seen.Count, 10, "the seed changes the starts");
        }

        [Test]
        public void EveryFaction_StartsWithAnHq_TheStartStock_Carriers_AndPeace()
        {
            var s = GodSim.Create(4).State;
            foreach (var f in FactionInfo.All)
            {
                var hq = s.Economy.HqsOf(f).Single();
                Assert.AreEqual(EconomyTuning.StartPlank, hq.Stock[(int)Ware.Plank]);
                Assert.AreEqual(EconomyTuning.StartStone, hq.Stock[(int)Ware.Stone]);
                Assert.AreEqual(EconomyTuning.StartBrick, hq.Stock[(int)Ware.Brick]);
                Assert.AreEqual(EconomyTuning.CarriersPerHq, hq.IdleCarriers);
                Assert.AreEqual(1, FactionRules.VictoryPoints(s, f));
                foreach (var o in FactionInfo.All) Assert.IsFalse(s.Factions.AtWar(f, o));
            }
            Assert.AreEqual(0, s.Territory.Buildings.Count(b => b.Size != BuildingSize.Hq), "no pre-placed base");
            Assert.AreEqual(0, s.Military.Units.Count);
            Assert.AreEqual(0, s.Setup.Human);
            Assert.AreEqual(6, s.Setup.FactionCount);
        }

        [Test]
        public void Setup_Watch_HasNoHuman_AndFewerFactionsAreAllowed()
        {
            var w = GodSim.Create(GameSetup.Watch(5)).State;
            Assert.IsFalse(w.Setup.HasHuman);
            Assert.AreEqual(6, w.Territory.Hqs.Count);
            var two = GodSim.Create(new GameSetup(5, 1, 2));
            Assert.AreEqual(2, two.State.Territory.Hqs.Count);
            Assert.AreEqual(Faction.F1, two.State.Setup.HumanFaction);
            Assert.IsFalse(two.Submit(new DeclareWar(Faction.F0, Faction.F3)).Ok, "F3 is not in a two-faction game");
            Assert.IsFalse(two.Submit(new PlaceHq(Faction.F4, 0)).Ok, "nor can it send commands");
            Assert.Throws<ArgumentOutOfRangeException>(() => GodSim.Create(new GameSetup(5, 3, 2)));
        }

        [Test]
        public void FactionInfo_HasSixNeutralNames_AndDistinctColours()
        {
            CollectionAssert.AreEqual(new[] { "Amber", "Azure", "Crimson", "Jade", "Violet", "Ivory" }, FactionInfo.All.Select(FactionInfo.Name).ToArray());
            Assert.AreEqual(6, FactionInfo.All.Select(FactionInfo.ColourRgb).Distinct().Count());
        }
    }

    /// <summary>War and peace (P7).</summary>
    public class WarAndPeaceTests
    {
        [Test]
        public void DeclareWar_IsImmediate_AndMutual_WithAnEvent()
        {
            var b = new Bed(3, war: false);
            var fs = b.S.Factions;
            Assert.IsFalse(fs.AtWar(Faction.F0, Faction.F2));
            Assert.IsTrue(b.Do(new DeclareWar(Faction.F2, Faction.F0)).Ok);
            Assert.IsTrue(fs.AtWar(Faction.F0, Faction.F2));
            Assert.IsTrue(fs.AtWar(Faction.F2, Faction.F0));
            Assert.IsFalse(fs.AtWar(Faction.F0, Faction.F1), "other pairs stay at peace");
            var ev = b.Events<WarDeclared>().Single();
            Assert.AreEqual(Faction.F2, ev.By); Assert.AreEqual(Faction.F0, ev.Against);
            CollectionAssert.AreEqual(new[] { Faction.F2 }, fs.Enemies(Faction.F0));

            Assert.AreEqual(FactionReasons.AlreadyAtWar, b.Sim.Check(new DeclareWar(Faction.F0, Faction.F2)).Reason);
            Assert.AreEqual(FactionReasons.SameFaction, b.Sim.Check(new DeclareWar(Faction.F0, Faction.F0)).Reason);
        }

        [Test]
        public void Peace_NeedsBothOffers_ThenEveryoneIsNotified()
        {
            var b = new Bed(3, war: false);
            var fs = b.S.Factions;
            Assert.AreEqual(FactionReasons.NotAtWar, b.Sim.Check(new OfferPeace(Faction.F0, Faction.F1)).Reason, "no peace without a war");
            b.Do(new DeclareWar(Faction.F0, Faction.F1));
            Assert.IsTrue(b.Do(new OfferPeace(Faction.F0, Faction.F1)).Ok);
            Assert.IsTrue(fs.AtWar(Faction.F0, Faction.F1), "one offer is not peace");
            Assert.IsTrue(fs.HasOfferedPeace(Faction.F0, Faction.F1));
            Assert.AreEqual(FactionReasons.AlreadyOffered, b.Sim.Check(new OfferPeace(Faction.F0, Faction.F1)).Reason);
            Assert.AreEqual(1, b.Events<PeaceOffered>().Count());
            Assert.IsTrue(b.Do(new OfferPeace(Faction.F1, Faction.F0)).Ok);
            Assert.IsFalse(fs.AtWar(Faction.F0, Faction.F1), "both offered: peace");
            Assert.IsFalse(fs.HasOfferedPeace(Faction.F0, Faction.F1));
            var made = b.Events<PeaceMade>().Single();
            Assert.That(new[] { made.A, made.B }, Is.EquivalentTo(new[] { Faction.F0, Faction.F1 }));

            // A fresh war wipes an old one-sided offer.
            b.Do(new DeclareWar(Faction.F1, Faction.F0));
            b.Do(new OfferPeace(Faction.F0, Faction.F1));
            b.Do(new DeclareWar(Faction.F0, Faction.F2));
            Assert.IsTrue(b.Do(new OfferPeace(Faction.F2, Faction.F0)).Ok);
            Assert.IsTrue(fs.AtWar(Faction.F0, Faction.F2));
        }

        [Test]
        public void Units_WalkPastFactionsAtPeace_AndFightOnceAtWar()
        {
            var b = new Bed(3, war: false);
            var (start, dir) = b.LandLine(b.PlayerHq, 4);
            var a = b.Spawn(Faction.F0, UnitKind.Infantry, start);
            var e = b.Spawn(Faction.F1, UnitKind.Infantry, b.Walk(start, dir, 2));
            var n = b.Spawn(Faction.F2, new Kit(Ware.Bow), b.Walk(start, dir, 4));
            // A move order takes F1's soldier right past F0's and on towards F2's archer: nobody stops or shoots.
            b.Do(new DevMoveUnit(e.Id, Pathing.NodeMmX(b.G, start), Pathing.NodeMmZ(b.G, start)));
            b.Step(4);
            Assert.AreEqual(0, b.Events<UnitFired>().Count(), "nobody fires at peace");
            Assert.AreEqual(100, a.Health); Assert.AreEqual(100, e.Health);
            Assert.AreEqual(TargetKind.None, a.TargetKind);
            Assert.AreEqual(TargetKind.None, n.TargetKind);
            Assert.IsTrue(b.RunUntil(100, () => !e.HasGoal), "it walks right past to its goal");
            Assert.AreEqual(0, b.Events<UnitFired>().Count());

            b.Do(new DeclareWar(Faction.F0, Faction.F1));
            b.Step(15);
            var shots = b.Events<UnitFired>().ToList();
            Assert.Greater(shots.Count(x => x.AttackerId == a.Id && x.TargetId == e.Id), 0, "at war they fight");
            Assert.Greater(shots.Count(x => x.AttackerId == e.Id && x.TargetId == a.Id), 0);
            Assert.AreEqual(0, shots.Count(x => x.AttackerId == n.Id || x.TargetId == n.Id), "the third faction is left alone");
            Assert.AreEqual(100, n.Health);
        }

        [Test]
        public void MarkTarget_NeedsWar_AndPeaceDropsTheMarks()
        {
            var b = new Bed(3, war: false);
            var target = b.EnemySmall(0);
            var r = b.Sim.Check(new MarkTarget(Faction.F0, target.Id));
            Assert.IsFalse(r.Ok);
            Assert.AreEqual(MilitaryReasons.NotAtWar, r.Reason);
            b.Do(new DeclareWar(Faction.F0, Faction.F1));
            Assert.IsTrue(b.Do(new MarkTarget(Faction.F0, target.Id)).Ok);
            Assert.IsTrue(b.Do(new MarkTarget(Faction.F0, b.EnemyHq.Id)).Ok);
            Assert.AreEqual(MilitaryReasons.NotAtWar, b.Sim.Check(new MarkTarget(Faction.F0, b.T.HqsOf(Faction.F2)[0].Id)).Reason);
            b.Do(new OfferPeace(Faction.F0, Faction.F1));
            b.Do(new OfferPeace(Faction.F1, Faction.F0));
            Assert.AreEqual(0, b.M.MarkedOf(Faction.F0).Count, "peace drops the marks");
            Assert.AreEqual(2, b.Events<TargetUnmarked>().Count(x => x.By == Faction.F0 && !x.Destroyed));
        }
    }

    /// <summary>Victory points and the Citadel (P7).</summary>
    public class CitadelTests
    {
        [Test]
        public void Citadel_IsDelivered_Upgraded_Worth2Vp_With3000Health()
        {
            var sim = EcoHelp.Quiet(3);
            var s = sim.State; var e = s.Economy; var t = s.Territory; var topo = s.Board.Topology;
            var hq1 = EcoHelp.Hq(sim);
            int corner = topo.Corners.First(c => topo.VertexHexes(c).Count == 3
                && topo.CornerDistance(t.HqOf(hq1.Id).Corner, c) == 2 && sim.Check(new PlaceHq(Faction.F0, c)).Ok);
            int hq2 = new EconomyBot(sim, Faction.F0).PlaceHqAndConnect(corner);
            EcoHelp.Run(sim, 3000, () => e.Find(hq2).Built);
            Assert.IsTrue(e.Find(hq2).Built);
            Assert.AreEqual(2, FactionRules.VictoryPoints(s, Faction.F0));

            // Stage 3 Citadel: 4 brick, 4 mortar, 3 plank, 2 steel. The first HQ lacks steel and some mortar; the second
            // holds them, so carriers must bring them over the road.
            var h1 = e.Find(hq1.Id); var h2 = e.Find(hq2);
            h1.Stock[(int)Ware.Plank] = 3; h1.Stock[(int)Ware.Brick] = 4; h1.Stock[(int)Ware.Mortar] = 1; h1.Stock[(int)Ware.Steel] = 0;
            h2.Stock[(int)Ware.Mortar] = 3; h2.Stock[(int)Ware.Steel] = 2;
            int delivered = e.DeliveredWares;

            Assert.AreEqual(Reasons.NotOwner, sim.Check(new UpgradeToCitadel(Faction.F1, hq1.Id)).Reason);
            Assert.AreEqual(FactionReasons.NotAnHq, sim.Check(new UpgradeToCitadel(Faction.F0, 99999)).Reason);
            Assert.IsTrue(sim.Submit(new UpgradeToCitadel(Faction.F0, hq1.Id)).Ok);
            sim.Step();
            Assert.IsTrue(h1.Upgrading);
            Assert.AreEqual(FactionReasons.AlreadyUpgrading, sim.Check(new UpgradeToCitadel(Faction.F0, hq1.Id)).Reason);
            Assert.AreEqual(1, sim.DrainEvents().OfType<CitadelStarted>().Count());

            long allInAt = -1;
            EcoHelp.Run(sim, 3000, () =>
            {
                if (allInAt < 0 && Enumerable.Range(0, Wares.Count).All(w => h1.In[w] >= EconomyDefs.Citadel.Cost[w])) allInAt = s.Tick;
                return t.HqOf(hq1.Id).Citadel;
            });
            Assert.IsTrue(t.HqOf(hq1.Id).Citadel, "upgraded");
            Assert.Greater(allInAt, 0);
            Assert.That(s.Tick - allInAt, Is.InRange(FactionTuning.CitadelBuildTicks - 1, FactionTuning.CitadelBuildTicks + 1), "30 s of work once the wares are in");
            Assert.AreEqual(0, h2.Stock[(int)Ware.Steel], "the steel was carried over");
            Assert.AreEqual(0, h2.Stock[(int)Ware.Mortar], "and the mortar");
            Assert.GreaterOrEqual(e.DeliveredWares - delivered, 2 + 3, "steel and mortar came by carrier");
            Assert.AreEqual(0, h1.Stock[(int)Ware.Plank] + h1.Stock[(int)Ware.Brick], "its own stock went in first");
            Assert.IsFalse(h1.Upgrading);
            Assert.AreEqual(1, sim.DrainEvents().OfType<CitadelCompleted>().Count());

            Assert.AreEqual(3, FactionRules.VictoryPoints(s, Faction.F0), "Citadel 2 + HQ 1");
            sim.Step();
            var rec = s.Military.BuildingById(hq1.Id);
            Assert.AreEqual(3000, rec.MaxHealth);
            Assert.AreEqual(3000, rec.Health);
            Assert.AreEqual(1500, s.Military.BuildingById(hq2).MaxHealth);
            Assert.AreEqual(FactionReasons.AlreadyCitadel, sim.Check(new UpgradeToCitadel(Faction.F0, hq1.Id)).Reason);
            Assert.IsEmpty(SimInvariants.Check(s));
        }

        [Test]
        public void TheHq_KeepsWorking_WhileItIsUpgraded()
        {
            var sim = EcoHelp.Quiet(3);
            var hq = EcoHelp.Hq(sim);
            // Enough of everything for both: the upgrade takes its share of the HQ's own stock first.
            foreach (var w in new[] { Ware.Plank, Ware.Stone, Ware.Brick }) hq.Stock[(int)w] = 12;
            Assert.IsTrue(sim.Submit(new UpgradeToCitadel(Faction.F0, hq.Id)).Ok);
            sim.Step();
            int site = EcoHelp.Place(sim, BuildingKinds.Sawmill, false);
            EcoHelp.Run(sim, 2000, () => sim.State.Economy.Find(site).Built);
            Assert.IsTrue(sim.State.Economy.Find(site).Built, "its carriers and builders still serve other sites");
            Assert.IsTrue(hq.Upgrading, "no iron yet: the upgrade waits");
        }
    }

    /// <summary>Capture, elimination and the end of the game (P7).</summary>
    public class CaptureTests
    {
        /// <summary>F0 with a small town and walkers on the move; F1 at war with it and next to its HQ.</summary>
        private static (Bed b, int saw, int site) Town()
        {
            var b = new Bed(3);
            Array.Clear(b.S.Board.HexNumber, 0, b.S.Board.HexNumber.Length);
            var bot = new EconomyBot(b.Sim, Faction.F0);
            int saw = bot.PlaceAndConnect(BuildingKinds.Sawmill);
            Assert.AreNotEqual(0, saw, string.Join(";", bot.Log));
            b.Step();
            Assert.IsTrue(EconomyModule.CompleteInstantly(b.S, saw));
            int site = bot.PlaceAndConnect(BuildingKinds.Kiln);
            Assert.AreNotEqual(0, site, string.Join(";", bot.Log));
            return (b, saw, site);
        }

        [Test]
        public void Capture_PassesTheArea_StockAndCarriers_WithoutLeaks_AndEliminates()
        {
            var (b, saw, site) = Town();
            var e = b.S.Economy; var t = b.S.Territory;
            int hqId = b.PlayerHq.Id;

            b.Do(SetDefend.OnBuilding(Faction.F0, hqId, 2));
            b.Do(new MarkTarget(Faction.F1, hqId));
            b.Do(new DeclareWar(Faction.F2, Faction.F0)); b.Do(new MarkTarget(Faction.F2, hqId));
            b.Do(new DeclareWar(Faction.F3, Faction.F0)); b.Do(new DeclareWar(Faction.F3, Faction.F1)); b.Do(new MarkTarget(Faction.F3, hqId));
            var f0Unit = b.Spawn(Faction.F0, UnitKind.Infantry, b.EnemyHq.Node); // far from home: it must disband

            var areaBuildings = t.Buildings.Where(x => x.Faction == Faction.F0 && t.InArea(b.PlayerHq, x.Node)).Select(x => x.Id).ToList();
            CollectionAssert.Contains(areaBuildings, saw); CollectionAssert.Contains(areaBuildings, site);
            var areaRoads = t.Roads.Where(r => r.Faction == Faction.F0 && r.Nodes.All(n => t.InArea(b.PlayerHq, n))).Select(r => r.Id).ToList();
            Assert.Greater(areaRoads.Count, 0);

            // Strike while F0 walkers are out: one axe blow (32 on buildings) brings the HQ down in the tick it arrives.
            Assert.IsTrue(b.RunUntil(400, () => e.Walkers.Count(w => w.Faction == Faction.F0) >= 2), "F0 walkers on the move");
            b.M.BuildingById(hqId).Health = 30;
            b.Sim.Submit(DevSpawnUnit.WithKit(Faction.F1, new Kit(Ware.Axe), b.G.FlagNodeOf(b.PlayerHq.Node)));
            int[] stockBefore = null; int lostBefore = 0, walkersAtCapture = 0;
            Assert.IsTrue(b.RunUntil(400, () =>
            {
                if (b.Events<HqCaptured>().Any()) return true;
                stockBefore = (int[])e.Find(hqId).Stock.Clone(); lostBefore = e.LostWares; walkersAtCapture = e.Walkers.Count(w => w.Faction == Faction.F0);
                return false;
            }), "the HQ falls");

            var cap = b.Events<HqCaptured>().Single();
            Assert.AreEqual(hqId, cap.HqId); Assert.AreEqual(Faction.F0, cap.From); Assert.AreEqual(Faction.F1, cap.To);
            Assert.Greater(cap.WalkersRemoved, 0, "walkers were on the move (" + walkersAtCapture + ")");
            Assert.AreEqual(Faction.F1, t.HqOf(hqId).Faction);
            Assert.AreEqual(750, b.M.BuildingById(hqId).Health, "half of 1500");
            Assert.IsNull(b.M.Rubbles.FirstOrDefault(r => r.Node == b.PlayerHq.Node), "an HQ is captured, not razed");

            // The whole area passed.
            foreach (int id in areaBuildings)
            {
                Assert.AreEqual(Faction.F1, t.BuildingById(id).Faction, "building " + id);
                Assert.AreEqual(Faction.F1, e.Find(id)?.Faction ?? Faction.F1);
                Assert.AreEqual(Faction.F1, b.M.BuildingById(id).Faction);
                CollectionAssert.Contains(cap.BuildingIds, id);
            }
            foreach (int id in areaRoads) Assert.AreEqual(Faction.F1, t.RoadById(id).Faction);
            foreach (var f in t.Flags.Where(f => t.InArea(b.PlayerHq, f.Node))) Assert.AreEqual(Faction.F1, f.Faction);
            Assert.IsFalse(e.Find(site).Built, "a site passes as a site");

            // The stock passed: give or take the few wares carriers moved in the capture tick (a picked-up ware is lost with its carrier).
            var stock = e.Find(hqId).Stock;
            int lost = e.LostWares - lostBefore;
            Assert.AreEqual(stockBefore.Sum(), stock.Sum() + lost, 0 + EconomyTuning.CarriersPerHq, "nothing else went missing");
            Assert.Greater(stock.Sum(), 0);
            Assert.AreEqual(stock.Sum(), Enumerable.Range(0, Wares.Count).Sum(w => e.TotalStock(Faction.F1, (Ware)w)) - e.HqsOf(Faction.F1).Where(h => h.Id != hqId).Sum(h => h.Stock.Sum()));

            // Carriers: all six of the HQ are home and now F1's; no F0 walker is left.
            var rec = e.Find(hqId);
            Assert.AreEqual(EconomyTuning.CarriersPerHq, rec.IdleCarriers + e.CarriersOut(hqId));
            Assert.AreEqual(0, e.Walkers.Count(w => w.Faction == Faction.F0));
            Assert.IsEmpty(SimInvariants.Check(b.S));

            // Marks and defend flags: F1 no longer marks its own HQ, F2 (at peace with F1) drops its mark, F3 (at war with F1) keeps it.
            Assert.IsFalse(b.M.IsMarked(Faction.F1, hqId));
            Assert.IsFalse(b.M.IsMarked(Faction.F2, hqId));
            Assert.IsTrue(b.M.IsMarked(Faction.F3, hqId));
            Assert.IsNull(b.M.DefendAtBuilding(hqId));
            Assert.IsTrue(b.Events<DefendRemoved>().Any(d => d.AnchorLost && d.Faction == Faction.F0));

            // F0 had no other HQ: it is out, its units disband, nothing of it is left.
            var out0 = b.Events<FactionEliminated>().Single();
            Assert.AreEqual(Faction.F0, out0.Faction); Assert.AreEqual(1, out0.By);
            Assert.IsTrue(b.S.Factions.IsEliminated(Faction.F0));
            Assert.IsFalse(b.M.Units.Contains(f0Unit));
            Assert.IsTrue(b.Events<UnitDied>().Any(d => d.UnitId == f0Unit.Id && d.Disbanded));
            Assert.AreEqual(0, t.Buildings.Count(x => x.Faction == Faction.F0));
            Assert.AreEqual(0, t.Roads.Count(x => x.Faction == Faction.F0), "its roads are removed");
            Assert.AreEqual(0, t.Flags.Count(x => x.Faction == Faction.F0));
            Assert.IsFalse(b.S.IsGameOver, "five factions are left");
            Assert.AreEqual(Reasons.NotOwner, b.Sim.Check(new PlaceBuilding(Faction.F0, b.PlayerHq.Node, BuildingSize.Small)).Reason);

            // The new owner runs it: the site is finished by F1's carriers and builders, with no leaks.
            for (int i = 0; i < 3000 && !e.Find(site).Built; i++)
            {
                b.Step();
                if (i % 100 == 0) Assert.IsEmpty(SimInvariants.Check(b.S), "tick " + b.S.Tick);
            }
            Assert.IsTrue(e.Find(site).Built, "F1 finishes the captured site");
            Assert.AreEqual(Faction.F1, e.Find(site).Faction);
            Assert.IsEmpty(SimInvariants.Check(b.S));
        }

        [Test]
        public void Capture_KeepsTheCitadel_AtHalfItsHealth()
        {
            var b = new Bed(3);
            var hq = b.T.HqOf(b.PlayerHq.Id);
            hq.Citadel = true; // test set-up: as if upgraded
            b.Step();
            Assert.AreEqual(3000, b.M.BuildingById(hq.Id).MaxHealth);
            Assert.AreEqual(2, FactionRules.VictoryPoints(b.S, Faction.F0));
            b.M.BuildingById(hq.Id).Health = 30;
            b.Spawn(Faction.F1, new Kit(Ware.Axe), b.G.FlagNodeOf(hq.Node));
            Assert.IsTrue(b.RunUntil(300, () => hq.Faction == Faction.F1));
            Assert.IsTrue(hq.Citadel);
            Assert.AreEqual(1500, b.M.BuildingById(hq.Id).Health);
            Assert.AreEqual(2 + 1, FactionRules.VictoryPoints(b.S, Faction.F1), "F1's own HQ plus the Citadel");
        }

        [Test]
        public void OrdinaryBuildings_StillBurn_ToRubble()
        {
            var b = new Bed(3);
            var small = b.EnemySmall(0);
            b.M.BuildingById(small.Id).Health = 10;
            b.Spawn(Faction.F0, new Kit(Ware.Axe), small.FlagNode);
            Assert.IsTrue(b.RunUntil(100, () => b.T.BuildingById(small.Id) == null));
            Assert.AreEqual(1, b.M.Rubbles.Count);
            Assert.IsFalse(b.Events<HqCaptured>().Any());
        }

        [Test]
        public void LastFactionStanding_WinsAndTheSimStops()
        {
            var b = new Bed(3, factions: 2);
            b.M.BuildingById(b.EnemyHq.Id).Health = 30;
            b.Spawn(Faction.F0, new Kit(Ware.Axe), b.G.FlagNodeOf(b.EnemyHq.Node));
            Assert.IsTrue(b.RunUntil(300, () => b.S.IsGameOver));
            var over = b.Events<GameOver>().Single();
            Assert.AreEqual(0, over.Winner);
            Assert.AreEqual(GameEndReason.LastStanding, over.Reason);
            Assert.AreEqual(Faction.F0, over.Standings[0].Faction);
            Assert.IsTrue(over.Standings[1].Eliminated);
            Assert.AreEqual(GameOutcome.Won, b.S.Factions.OutcomeFor(Faction.F0));
            Assert.AreEqual(GameOutcome.Lost, b.S.Factions.OutcomeFor(Faction.F1));
            Assert.AreEqual(2, FactionRules.VictoryPoints(b.S, Faction.F0));
            long tick = b.S.Tick;
            b.Step(50);
            Assert.AreEqual(tick, b.S.Tick, "the sim stops");
            Assert.IsFalse(b.Sim.Submit(new DeclareWar(Faction.F0, Faction.F1)).Ok);
        }
    }

    public class VictoryTests
    {
        [Test]
        public void TenVictoryPoints_EndTheGame_WithStandings()
        {
            var sim = GodSim.Create(6);
            var s = sim.State; var t = s.Territory; var g = t.Grid; var topo = t.Topology;
            // Test set-up: give F2 four more HQs on free legal corners, then make all five Citadels (10 VP).
            int added = 0;
            foreach (int c in BoardGenerator.LandCorners(s.Board, topo))
            {
                if (added == 4) break;
                int fn = g.FlagNodeOf(g.CornerNode(c));
                if (t.HqNearCorner(c) || !g.IsLand(fn) || t.NodeFlag[fn] != 0 || t.FlagNextTo(fn) || t.NodeRoad[fn] != 0) continue;
                TerritoryRules.AddHq(s, Faction.F2, c, null);
                added++;
            }
            Assert.AreEqual(4, added);
            sim.Step();
            Assert.AreEqual(5, FactionRules.VictoryPoints(s, Faction.F2));
            Assert.IsFalse(s.IsGameOver);
            foreach (var h in t.HqsOf(Faction.F2).Take(4)) h.Citadel = true;
            Assert.AreEqual(9, FactionRules.VictoryPoints(s, Faction.F2));
            sim.Step();
            Assert.IsFalse(s.IsGameOver, "9 VP is not enough");
            t.HqsOf(Faction.F2)[4].Citadel = true;
            sim.Step();
            Assert.IsTrue(s.IsGameOver);
            var over = sim.DrainEvents().OfType<GameOver>().Single();
            Assert.AreEqual(2, over.Winner);
            Assert.AreEqual(GameEndReason.VictoryPoints, over.Reason);
            Assert.AreEqual(6, over.Standings.Count);
            Assert.AreEqual(Faction.F2, over.Standings[0].Faction);
            Assert.AreEqual(10, over.Standings[0].VictoryPoints);
            Assert.AreEqual(1, over.Standings[0].Rank);
            Assert.AreEqual(Enumerable.Range(1, 6).ToArray(), over.Standings.Select(x => x.Rank).ToArray());
            Assert.AreEqual(over.Standings.Count, s.Factions.FinalStandings.Count);
            Assert.IsFalse(sim.Submit(new PlaceHq(Faction.F0, 0)).Ok, "nothing is accepted after the end");
        }
    }

    /// <summary>Six chain bots on one island: determinism and a ten-minute headless run (P7).</summary>
    public class SixBotTests
    {
        private static (GodSim sim, List<ChainBot> bots) Run(ulong seed, int ticks, Action<GodSim> each = null)
        {
            var sim = GodSim.Create(GameSetup.Watch(seed));
            var bots = FactionInfo.All.Select(f => new ChainBot(sim, f)).ToList();
            while (sim.State.Tick < ticks)
            {
                sim.Step();
                if (sim.State.Tick % 20 == 0) foreach (var bot in bots) bot.Think();
                each?.Invoke(sim);
                sim.DrainEvents();
            }
            return (sim, bots);
        }

        [Test]
        public void SameSeedAndCommands_SameHash_SixFactions()
        {
            void War(GodSim sim)
            {
                if (sim.State.Tick == 300) sim.Submit(new DeclareWar(Faction.F1, Faction.F4));
                if (sim.State.Tick == 400) sim.Submit(SetDefend.OnBuilding(Faction.F4, sim.State.Territory.HqsOf(Faction.F4)[0].Id, 2));
            }
            ulong a = Run(11, 1500, War).sim.StateHash();
            ulong b = Run(11, 1500, War).sim.StateHash();
            Assert.AreEqual(a, b);
            Assert.AreNotEqual(a, Run(12, 1500, War).sim.StateHash());
        }

        [Test]
        public void SixChainBots_RunTenMinutes_WithoutExceptionsOrBrokenInvariants()
        {
            int checks = 0;
            var (sim, bots) = Run(20261125, 6000, s =>
            {
                if (s.State.Tick % 100 != 0) return;
                checks++;
                var bad = SimInvariants.Check(s.State);
                Assert.IsEmpty(bad, "tick " + s.State.Tick + ": " + string.Join("; ", bad));
            });
            Assert.GreaterOrEqual(sim.State.Tick, 6000);
            Assert.Greater(checks, 50);
            var e = sim.State.Economy;
            foreach (var f in FactionInfo.All)
                Assert.GreaterOrEqual(e.Buildings.Count(x => x.Faction == f && x.Built && !x.IsHq), 2, FactionInfo.Name(f) + " built its economy: " + string.Join(";", bots[(int)f].Log));
            Assert.Greater(e.Buildings.Count(x => x.Built && !x.IsHq), 20);
            Assert.IsEmpty(SimInvariants.Check(sim.State));
        }
    }
}
