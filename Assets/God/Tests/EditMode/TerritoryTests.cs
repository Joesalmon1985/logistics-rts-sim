using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace God.Sim.Tests
{
    public class StartAndDiceTests
    {
        [Test]
        public void StartCorners_AreDeterministic_Legal_AndAsFarApartAsPossible()
        {
            for (ulong seed = 1; seed <= 30; seed++)
            {
                var a = GodSim.Create(seed).State;
                var b = GodSim.Create(seed).State;
                var topo = a.Board.Topology;
                var p = a.Territory.HqsOf(Faction.Player).Single();
                var e = a.Territory.HqsOf(Faction.Enemy).Single();
                Assert.AreEqual(p.Corner, b.Territory.HqsOf(Faction.Player).Single().Corner, "deterministic");
                Assert.AreEqual(e.Corner, b.Territory.HqsOf(Faction.Enemy).Single().Corner, "deterministic");
                var land = BoardGenerator.LandCorners(a.Board, topo);
                CollectionAssert.Contains(land, p.Corner);
                CollectionAssert.Contains(land, e.Corner);
                int max = 0;
                foreach (int x in land) foreach (int y in land) max = Math.Max(max, topo.CornerDistance(x, y));
                Assert.AreEqual(max, topo.CornerDistance(p.Corner, e.Corner), "as far apart as possible");
                Assert.GreaterOrEqual(max, 4);
            }
        }

        [Test]
        public void StartCorners_VaryWithTheSeed()
        {
            var set = new HashSet<(int, int)>();
            for (ulong seed = 1; seed <= 30; seed++)
            {
                var t = GodSim.Create(seed).State.Territory;
                set.Add((t.HqsOf(Faction.Player).Single().Corner, t.HqsOf(Faction.Enemy).Single().Corner));
            }
            Assert.Greater(set.Count, 1);
        }

        [Test]
        public void Dice_AreDeterministic_AndRollEverySixSeconds()
        {
            var a = GodSim.Create(5); var b = GodSim.Create(5);
            a.Step(59);
            Assert.AreEqual(0, a.State.Territory.RollCount);
            a.Step(1);
            Assert.AreEqual(1, a.State.Territory.RollCount);
            Assert.AreEqual(60, a.State.Territory.LastRollTick);
            Assert.AreEqual(1, a.DrainEvents().OfType<DiceRolled>().Count());
            a.Step(6000); b.Step(6060);
            Assert.AreEqual(b.State.Territory.LastTotal, a.State.Territory.LastTotal);
            CollectionAssert.AreEqual(b.State.Territory.Piles, a.State.Territory.Piles);
            Assert.AreEqual(101, a.State.Territory.RollCount);
        }

        [Test]
        public void Dice_FollowThe2d6Distribution()
        {
            var sim = GodSim.Create(77);
            const int n = 3600;
            var counts = new int[13];
            for (int i = 0; i < n; i++)
            {
                sim.Step(TerritoryTuning.DiceIntervalTicks);
                foreach (var d in sim.DrainEvents().OfType<DiceRolled>())
                {
                    Assert.AreEqual(d.Die1 + d.Die2, d.Total);
                    Assert.That(d.Die1, Is.InRange(1, 6)); Assert.That(d.Die2, Is.InRange(1, 6));
                    counts[d.Total]++;
                }
            }
            Assert.AreEqual(n, counts.Sum());
            for (int total = 2; total <= 12; total++)
            {
                double p = (6 - Math.Abs(7 - total)) / 36.0;
                double sigma = Math.Sqrt(n * p * (1 - p));
                Assert.That(Math.Abs(counts[total] - n * p), Is.LessThan(5 * sigma), "total " + total);
            }
        }

        [Test]
        public void Piles_OnlyMatchingHexes_Cap3_NeverOnDesert_SevenDoesNothing()
        {
            var sim = GodSim.Create(31);
            var s = sim.State; var t = s.Territory;
            int sevens = 0, wasted = 0, added = 0;
            for (int i = 0; i < 2000; i++)
            {
                var before = (int[])t.Piles.Clone();
                sim.Step(TerritoryTuning.DiceIntervalTicks);
                var ev = sim.DrainEvents();
                int total = ev.OfType<DiceRolled>().Single().Total;
                int matching = 0;
                for (int h = 0; h < s.Board.HexNumber.Length; h++) if (s.Board.HexNumber[h] == total) matching++;
                var adds = ev.OfType<PileAdded>().ToList(); var wastes = ev.OfType<PileWasted>().ToList();
                Assert.AreEqual(matching, adds.Count + wastes.Count, "every matching hex gets a pile or wastes it");
                if (total == 7) { sevens++; Assert.AreEqual(0, adds.Count + wastes.Count); CollectionAssert.AreEqual(before, t.Piles); }
                foreach (var a in adds)
                {
                    Assert.AreEqual(total, s.Board.HexNumber[a.Hex]);
                    Assert.AreNotEqual(Terrain.Desert, s.Board.HexTerrain[a.Hex]);
                    Assert.AreEqual(RawKinds.OfTerrain(s.Board.HexTerrain[a.Hex]), a.Kind);
                    Assert.AreEqual(before[a.Hex] + 1, t.Piles[a.Hex]);
                    added++;
                }
                foreach (var w in wastes) { Assert.AreEqual(TerritoryTuning.PileCap, before[w.Hex]); wasted++; }
                foreach (int c in t.Piles) Assert.That(c, Is.InRange(0, TerritoryTuning.PileCap));
            }
            Assert.Greater(sevens, 0); Assert.Greater(wasted, 0); Assert.Greater(added, 0);
            for (int h = 0; h < s.Board.HexTerrain.Length; h++)
                if (s.Board.HexTerrain[h] == Terrain.Desert) Assert.AreEqual(0, t.Piles[h]);
            Assert.AreEqual(wasted, t.WastedPiles);
        }

        [Test]
        public void Piles_KindFollowsTerrain()
        {
            Assert.AreEqual(RawKind.Log, RawKinds.OfTerrain(Terrain.Forest));
            Assert.AreEqual(RawKind.Clay, RawKinds.OfTerrain(Terrain.Hills));
            Assert.AreEqual(RawKind.Grain, RawKinds.OfTerrain(Terrain.Fields));
            Assert.AreEqual(RawKind.Rock, RawKinds.OfTerrain(Terrain.Mountain));
            Assert.AreEqual(RawKind.None, RawKinds.OfTerrain(Terrain.Desert));
        }
    }

    public class NodeGridTests
    {
        private static readonly NodeGrid G = NodeGrid.Get(BoardGenerator.IslandRadius);

        [Test]
        public void Spacing_IsATwelfthOfTheEdge_WithSixNodesAcrossTheCircleRadius()
        {
            Assert.AreEqual(12, G.NodesPerHexEdge);
            Assert.AreEqual(6, G.CircleRadiusSteps);
            Assert.AreEqual(30.0, G.CircleRadius, 1e-9);
        }

        [Test]
        public void Corners_SitExactlyOnNodes()
        {
            var t = G.Topology;
            for (int c = 0; c < t.CornerCount; c++)
            {
                int n = G.CornerNode(c);
                Assert.That(n, Is.GreaterThanOrEqualTo(0));
                Assert.AreEqual(t.VertexX(c), G.X(n), 1e-6);
                Assert.AreEqual(t.VertexZ(c), G.Z(n), 1e-6);
            }
        }

        [Test]
        public void NodeToHex_Mapping()
        {
            var t = G.Topology;
            int land = 0;
            for (int n = 0; n < G.Count; n++)
            {
                int h = G.HexOf(n);
                if (h < 0) { Assert.IsFalse(G.IsLand(n)); continue; }
                land++;
                double d = Math.Sqrt(Math.Pow(G.X(n) - t.HexX(h), 2) + Math.Pow(G.Z(n) - t.HexZ(h), 2));
                Assert.LessOrEqual(d, BoardTopology.HexEdgeLength + 1e-6, "inside its hex");
                for (int o = 0; o < t.HexCount; o++)
                {
                    double d2 = Math.Sqrt(Math.Pow(G.X(n) - t.HexX(o), 2) + Math.Pow(G.Z(n) - t.HexZ(o), 2));
                    // The hex's own centre is as near as any other centre (equal only on shared borders).
                    Assert.LessOrEqual(d, d2 + 1e-6, "node " + n + " hex " + h + " vs " + o);
                }
            }
            Assert.Greater(land, 19 * 100);
            for (int h = 0; h < t.HexCount; h++) Assert.AreEqual(h, G.HexOf(G.HexCentreNode(h)));
            // Far out at sea.
            Assert.AreEqual(-1, G.HexOf(0));
        }

        [Test]
        public void SouthEastNeighbour_IsRightAndDown()
        {
            int n = G.CornerNode(G.Topology.Corners[0]);
            int se = G.FlagNodeOf(n);
            Assert.Greater(G.X(se), G.X(n));
            Assert.Less(G.Z(se), G.Z(n));
            Assert.AreEqual(1, G.Distance(n, se));
        }

        [Test]
        public void EverySixNeighboursIsOneStep()
        {
            int n = G.CornerNode(G.Topology.Corners[3]);
            for (int d = 0; d < 6; d++) Assert.AreEqual(1, G.Distance(n, G.Neighbour(n, d)));
        }
    }

    public class HqPlacementTests
    {
        private static int[] CornersAtDistance(GodState s, int from, int dist, bool landOnly = true)
        {
            var topo = s.Board.Topology;
            return topo.Corners.Where(c => topo.CornerDistance(from, c) == dist && (!landOnly || topo.VertexHexes(c).Count == 3)).ToArray();
        }

        private static string Why(GodSim sim, Command c) => sim.Check(c).Reason;

        [Test]
        public void LegalHq_AtTwoCornerSteps_BecomesASiteAtTheNextTick_AndAnHqWhenCompleted()
        {
            var sim = GodSim.Create(3); var s = sim.State;
            var p = s.Territory.HqsOf(Faction.Player).Single();
            var c = CornersAtDistance(s, p.Corner, 2).First(x => !s.Territory.HqNearCorner(x) && sim.Check(new PlaceHq(Faction.Player, x)).Ok);
            Assert.IsTrue(sim.Submit(new PlaceHq(Faction.Player, c)).Ok);
            Assert.AreEqual(1, s.Territory.HqsOf(Faction.Player).Count);
            sim.Step();
            // P2: the HQ is now a construction site (no circle, no reach) until the economy carries its wares.
            Assert.AreEqual(2, s.Territory.Hqs.Count(h => h.Faction == Faction.Player));
            Assert.AreEqual(1, s.Territory.HqsOf(Faction.Player).Count, "a site is not a finished HQ");
            Assert.IsTrue(s.Territory.Hqs.Single(h => h.Corner == c).Site);
            Assert.IsTrue(sim.DrainEvents().OfType<BuildingPlaced>().Any(e => e.Size == BuildingSize.Hq));
            TerritoryRules.CompleteHq(s, s.Territory.Hqs.Single(h => h.Corner == c).Id, null);
            Assert.AreEqual(2, s.Territory.HqsOf(Faction.Player).Count);
            Assert.AreEqual(1, s.Territory.Buildings.Count(b => b.Node == s.Territory.Grid.CornerNode(c)));
            Assert.AreNotEqual(0, s.Territory.NodeFlag[s.Territory.Grid.FlagNodeOf(s.Territory.Grid.CornerNode(c))], "the new HQ has a flag");
        }

        [Test]
        public void Refusals_WaterCorner_Distance_Reach_Occupied()
        {
            var sim = GodSim.Create(3); var s = sim.State; var topo = s.Board.Topology;
            var p = s.Territory.HqsOf(Faction.Player).Single();
            int water = topo.Corners.First(c => topo.VertexHexes(c).Count < 3);
            Assert.AreEqual(Reasons.NotLandCorner, Why(sim, new PlaceHq(Faction.Player, water)));
            Assert.AreEqual(Reasons.NotLandCorner, Why(sim, new PlaceHq(Faction.Player, -5)));
            Assert.AreEqual(Reasons.HqTooClose, Why(sim, new PlaceHq(Faction.Player, p.Corner)));
            foreach (int n in topo.CornerNeighbours(p.Corner))
                if (topo.VertexHexes(n).Count == 3) Assert.AreEqual(Reasons.HqTooClose, Why(sim, new PlaceHq(Faction.Player, n)));
            foreach (int far in topo.Corners.Where(c => topo.VertexHexes(c).Count == 3 && topo.CornerDistance(p.Corner, c) >= 3))
                if (!s.Territory.HqNearCorner(far)) Assert.AreEqual(Reasons.HqOutOfReach, Why(sim, new PlaceHq(Faction.Player, far)), "corner " + far);
            // Next to the enemy HQ is too close for the player as well.
            var e = s.Territory.HqsOf(Faction.Enemy).Single();
            foreach (int n in topo.CornerNeighbours(e.Corner))
                if (topo.VertexHexes(n).Count == 3) Assert.AreEqual(Reasons.HqTooClose, Why(sim, new PlaceHq(Faction.Player, n)));
        }

        [Test]
        public void Reach_IsPerFaction()
        {
            var sim = GodSim.Create(3); var s = sim.State;
            var e = s.Territory.HqsOf(Faction.Enemy).Single();
            var pc = s.Territory.HqsOf(Faction.Player).Single().Corner;
            var nearEnemy = CornersAtDistance(s, e.Corner, 2).First(x => sim.Check(new PlaceHq(Faction.Enemy, x)).Ok && s.Board.Topology.CornerDistance(pc, x) != 2);
            Assert.AreEqual(Reasons.HqOutOfReach, Why(sim, new PlaceHq(Faction.Player, nearEnemy)));
            Assert.IsTrue(sim.Submit(new PlaceHq(Faction.Enemy, nearEnemy)).Ok);
        }

        [Test]
        public void FlagOnTheHqFlagSite_RefusesTheHq()
        {
            var sim = GodSim.Create(3); var s = sim.State; var t = s.Territory; var g = t.Grid;
            var p = t.HqsOf(Faction.Player).Single();
            int target = CornersAtDistance(s, p.Corner, 2).First(x => sim.Check(new PlaceHq(Faction.Player, x)).Ok);
            int flagSite = g.FlagNodeOf(g.CornerNode(target));
            var path = t.FindRoadPath(t.FlagAtNode(g.FlagNodeOf(p.Node)).Node, flagSite);
            Assert.NotNull(path);
            Assert.IsTrue(sim.Submit(new BuildRoad(Faction.Player, path)).Ok);
            sim.Step();
            Assert.AreEqual(Reasons.Occupied, sim.Check(new PlaceHq(Faction.Player, target)).Reason);
        }

        [Test]
        public void Circles_NeverOverlap_ForAnyLegalPairOfHqs()
        {
            var board = BoardGenerator.CreateIslandBoard(9);
            var topo = board.Topology; var g = NodeGrid.Get(board.Radius);
            var land = BoardGenerator.LandCorners(board, topo);
            int pairs = 0;
            foreach (int a in land)
                foreach (int b in land)
                {
                    if (a >= b) continue;
                    // Legal pair: not the same corner, not adjacent.
                    if (topo.CornerDistance(a, b) < 2) continue;
                    pairs++;
                    double dx = topo.VertexX(a) - topo.VertexX(b), dz = topo.VertexZ(a) - topo.VertexZ(b);
                    Assert.Greater(Math.Sqrt(dx * dx + dz * dz), 2 * g.CircleRadius, "circle centres");
                    var na = new HashSet<int>(g.CircleNodes(g.CornerNode(a)));
                    foreach (int n in g.CircleNodes(g.CornerNode(b))) Assert.IsFalse(na.Contains(n), "shared node");
                }
            Assert.Greater(pairs, 100);
        }

        [Test]
        public void Circles_AreExposed_WithRadiusHalfAnEdge()
        {
            var t = GodSim.Create(3).State.Territory;
            foreach (var c in t.Circles()) Assert.AreEqual(30.0, c.Radius, 1e-9);
            Assert.AreEqual(2, t.Circles().Count);
        }
    }

    public class PlacementTests
    {
        private GodSim _sim;
        private GodState _s;
        private TerritoryState _t;
        private NodeGrid _g;
        private Hq _hq;

        [SetUp]
        public void Setup()
        {
            _sim = GodSim.Create(3);
            _s = _sim.State; _t = _s.Territory; _g = _t.Grid;
            _hq = _t.HqsOf(Faction.Player).Single();
        }

        private int N(int di, int dj) => _g.Id(_g.I(_hq.Node) + di, _g.J(_hq.Node) + dj);

        private CommandResult Do(Command c)
        {
            var r = _sim.Submit(c);
            if (r.Ok) _sim.Step();
            return r;
        }

        [Test]
        public void HqHasAFlagOnItsSouthEastNode()
        {
            var f = _t.FlagAtNode(N(1, -1));
            Assert.NotNull(f);
            Assert.AreEqual(_hq.Id, f.BuildingId);
            Assert.AreEqual(_g.FlagNodeOf(_hq.Node), f.Node);
        }

        [Test]
        public void Buildable_IsLandInsideTheFactionsCircles()
        {
            Assert.IsTrue(_t.IsBuildable(Faction.Player, N(0, 0)));
            Assert.IsTrue(_t.IsBuildable(Faction.Player, N(6, 0)));
            Assert.IsFalse(_t.IsBuildable(Faction.Player, N(7, 0)));
            Assert.IsFalse(_t.IsBuildable(Faction.Enemy, N(0, 0)));
            var list = _t.BuildableNodes(Faction.Player);
            Assert.AreEqual(_g.CircleNodes(_hq.Node).Count(_g.IsLand), list.Count);
            Assert.Greater(list.Count, 100);
        }

        [Test]
        public void Small_PlacesWithAnAutoFlagOnTheSouthEast()
        {
            Assert.IsTrue(Do(new PlaceBuilding(Faction.Player, N(-3, 0), BuildingSize.Small)).Ok);
            var b = _t.BuildingAtNode(N(-3, 0));
            Assert.NotNull(b);
            Assert.AreEqual(BuildingSize.Small, b.Size);
            Assert.AreEqual(TerritoryTuning.PlaceholderKind, b.Kind);
            Assert.AreEqual(N(-2, -1), b.FlagNode);
            Assert.AreEqual(b.Id, _t.FlagAtNode(N(-2, -1)).BuildingId);
        }

        [Test]
        public void Small_Refusals()
        {
            Assert.AreEqual(Reasons.NotBuildable, _sim.Check(new PlaceBuilding(Faction.Player, N(8, 0), BuildingSize.Small)).Reason);
            Assert.AreEqual(Reasons.NotBuildable, _sim.Check(new PlaceBuilding(Faction.Player, N(6, 0), BuildingSize.Small)).Reason, "its flag node is outside the circle");
            Assert.AreEqual(Reasons.NotBuildable, _sim.Check(new PlaceBuilding(Faction.Enemy, N(-3, 0), BuildingSize.Small)).Reason);
            Assert.AreEqual(Reasons.Occupied, _sim.Check(new PlaceBuilding(Faction.Player, N(0, 0), BuildingSize.Small)).Reason);
            Assert.AreEqual(Reasons.TooCrowded, _sim.Check(new PlaceBuilding(Faction.Player, N(-1, 0), BuildingSize.Small)).Reason, "next to the HQ");
            Assert.AreEqual(Reasons.InvalidSite, _sim.Check(new PlaceBuilding(Faction.Player, N(-3, 0), BuildingSize.Hq)).Reason);
            Assert.IsTrue(Do(new PlaceBuilding(Faction.Player, N(-3, 0), BuildingSize.Small)).Ok);
            Assert.AreEqual(Reasons.Occupied, _sim.Check(new PlaceBuilding(Faction.Player, N(-3, 0), BuildingSize.Small)).Reason);
            Assert.AreEqual(Reasons.TooCrowded, _sim.Check(new PlaceBuilding(Faction.Player, N(-4, 0), BuildingSize.Small)).Reason, "neighbouring building");
        }

        [Test]
        public void Small_FlagMustKeepItsDistanceFromOtherFlags()
        {
            // A plain road flag at (3,-1); a building at (2,1) would put its flag at (3,0), right next to it.
            Assert.IsTrue(Do(new BuildRoad(Faction.Player, new[] { N(1, -1), N(2, -1), N(3, -1) })).Ok);
            Assert.AreEqual(Reasons.FlagTooClose, _sim.Check(new PlaceBuilding(Faction.Player, N(2, 1), BuildingSize.Small)).Reason);
        }

        [Test]
        public void Medium_NeedsSixClearNeighbours_ExceptItsOwnFlag()
        {
            Assert.IsTrue(Do(new PlaceBuilding(Faction.Player, N(0, 4), BuildingSize.Medium)).Ok);
            var b = _t.BuildingAtNode(N(0, 4));
            Assert.AreEqual(BuildingSize.Medium, b.Size);
            Assert.AreEqual(N(1, 3), b.FlagNode);
            Assert.NotNull(_t.FlagAtNode(N(1, 3)));
        }

        [Test]
        public void Medium_RefusedNextToARoadOrFlag()
        {
            var f = _t.FlagAtNode(N(1, -1));
            var road = new[] { N(1, -1), N(2, -1), N(3, -1), N(4, -1), N(5, -1), N(6, -1) };
            Assert.IsTrue(Do(new BuildRoad(Faction.Player, road)).Ok);
            // (3,0): its neighbour (3,-1) is a road node. (4,-1) too.
            Assert.AreEqual(Reasons.TooCrowded, _sim.Check(new PlaceBuilding(Faction.Player, N(3, 0), BuildingSize.Medium)).Reason);
            // The same spot is fine for a Small (its flag node (4,-1) is a road node, split needs a free gap: (4,-1) is 3 from the ends).
            Assert.IsTrue(_sim.Check(new PlaceBuilding(Faction.Player, N(3, 0), BuildingSize.Small)).Ok);
            Assert.NotNull(f);
        }

        [Test]
        public void Small_OnARoad_SplitsItAtItsFlag()
        {
            var road = new[] { N(1, -1), N(2, -1), N(3, -1), N(4, -1), N(5, -1), N(6, -1) };
            Assert.IsTrue(Do(new BuildRoad(Faction.Player, road)).Ok);
            Assert.IsTrue(Do(new PlaceBuilding(Faction.Player, N(3, 0), BuildingSize.Small)).Ok);
            Assert.AreEqual(2, _t.Roads.Count);
            Assert.AreEqual(0, _t.NodeRoad[N(4, -1)]);
            Assert.NotNull(_t.FlagAtNode(N(4, -1)));
        }

        [Test]
        public void RoadsAndFlags_KeepClearOfAMediumBuilding()
        {
            Assert.IsTrue(Do(new PlaceBuilding(Faction.Player, N(0, 4), BuildingSize.Medium)).Ok); // flag (1,3)
            var flag = _t.FlagAtNode(N(1, 3));
            // A road from the medium's flag that brushes the building's neighbour (0,3) is refused; going away is fine.
            Assert.AreEqual(Reasons.TooCrowded, _sim.Check(new BuildRoad(Faction.Player, new[] { N(1, 3), N(1, 2), N(0, 3), N(-1, 3) })).Reason);
            Assert.IsTrue(_sim.Check(new BuildRoad(Faction.Player, new[] { N(1, 3), N(2, 2), N(3, 1) })).Ok);
            Assert.NotNull(flag);
        }

        [Test]
        public void RemoveBuilding_FreesTheNodes_AndTheSpareFlag()
        {
            Assert.IsTrue(Do(new PlaceBuilding(Faction.Player, N(-3, 0), BuildingSize.Small)).Ok);
            int id = _t.BuildingAtNode(N(-3, 0)).Id;
            Assert.AreEqual(Reasons.NotOwner, _sim.Check(new RemoveBuilding(Faction.Enemy, id)).Reason);
            Assert.AreEqual(Reasons.CannotRemove, _sim.Check(new RemoveBuilding(Faction.Player, _hq.Id)).Reason);
            Assert.AreEqual(Reasons.NoSuchThing, _sim.Check(new RemoveBuilding(Faction.Player, 9999)).Reason);
            Assert.IsTrue(Do(new RemoveBuilding(Faction.Player, id)).Ok);
            Assert.AreEqual(0, _t.NodeBuilding[N(-3, 0)]);
            Assert.AreEqual(0, _t.NodeFlag[N(-2, -1)]);
            Assert.IsTrue(_sim.Check(new PlaceBuilding(Faction.Player, N(-3, 0), BuildingSize.Small)).Ok);
        }

        [Test]
        public void RemoveBuilding_KeepsAFlagThatARoadStillUses()
        {
            Assert.IsTrue(Do(new PlaceBuilding(Faction.Player, N(-3, 0), BuildingSize.Small)).Ok); // flag (-2,-1)
            Assert.IsTrue(Do(new BuildRoad(Faction.Player, new[] { N(-2, -1), N(-2, -2), N(-2, -3), N(-2, -4) })).Ok);
            Assert.IsTrue(Do(new RemoveBuilding(Faction.Player, _t.BuildingAtNode(N(-3, 0)).Id)).Ok);
            var f = _t.FlagAtNode(N(-2, -1));
            Assert.NotNull(f);
            Assert.AreEqual(0, f.BuildingId);
        }
    }

    public class RoadTests
    {
        private GodSim _sim;
        private GodState _s;
        private TerritoryState _t;
        private NodeGrid _g;
        private Hq _hq;

        [SetUp]
        public void Setup()
        {
            _sim = GodSim.Create(3);
            _s = _sim.State; _t = _s.Territory; _g = _t.Grid;
            _hq = _t.HqsOf(Faction.Player).Single();
        }

        private int N(int di, int dj) => _g.Id(_g.I(_hq.Node) + di, _g.J(_hq.Node) + dj);
        private int[] P(params (int, int)[] cells) => cells.Select(c => N(c.Item1, c.Item2)).ToArray();

        private CommandResult Do(Command c)
        {
            var r = _sim.Submit(c);
            if (r.Ok) _sim.Step();
            return r;
        }

        private string Why(Command c) => _sim.Check(c).Reason;

        [Test]
        public void Road_MustBeAdjacentNodes_FromAFlag_OnLand()
        {
            Assert.AreEqual(Reasons.BadRoad, Why(new BuildRoad(Faction.Player, P((1, -1), (3, -1), (4, -1)))), "gap");
            Assert.AreEqual(Reasons.BadRoad, Why(new BuildRoad(Faction.Player, P((2, -1), (3, -1), (4, -1)))), "not from a flag");
            Assert.AreEqual(Reasons.BadRoad, Why(new BuildRoad(Faction.Player, P((1, -1)))), "too short");
            Assert.AreEqual(Reasons.BadRoad, Why(new BuildRoad(Faction.Player, null)));
            Assert.AreEqual(Reasons.BadRoad, Why(new BuildRoad(Faction.Enemy, P((1, -1), (2, -1), (3, -1)))), "someone else's flag");
            Assert.AreEqual(Reasons.BadRoad, Why(new BuildRoad(Faction.Player, new[] { N(1, -1), N(2, -1), N(1, -1) })), "loop back");
            Assert.AreEqual(Reasons.BadRoad, Why(new BuildRoad(Faction.Player, new[] { N(1, -1), -4 })), "bad id");
            Assert.IsTrue(_sim.Check(new BuildRoad(Faction.Player, P((1, -1), (2, -1), (3, -1)))).Ok);
        }

        [Test]
        public void Road_MayNotPassThroughABuilding()
        {
            // Around the HQ node: (1,-1) -> (1,0) -> (0,... ) cannot step onto (0,0).
            Assert.AreEqual(Reasons.BadRoad, Why(new BuildRoad(Faction.Player, P((1, -1), (1, 0), (0, 0), (-1, 0), (-1, 1)))));
        }

        [Test]
        public void Road_NewEndFlagKeepsItsDistance_AndRoadsMayEndOnAnExistingFlag()
        {
            Assert.AreEqual(Reasons.FlagTooClose, Why(new BuildRoad(Faction.Player, P((1, -1), (2, -1)))));
            Assert.IsTrue(Do(new BuildRoad(Faction.Player, P((1, -1), (2, -1), (3, -1)))).Ok);
            var end = _t.FlagAtNode(N(3, -1));
            Assert.NotNull(end);
            Assert.AreEqual(0, end.BuildingId);
            Assert.AreEqual(1, _t.Roads.Count);
            Assert.AreEqual(_t.FlagAtNode(N(1, -1)).Id, _t.Roads[0].FlagA);
            // End on an existing flag: a second road round the HQ flag's other side.
            Assert.IsTrue(Do(new BuildRoad(Faction.Player, P((3, -1), (3, 0), (2, 0), (1, 0), (1, -1)))).Ok);
            Assert.AreEqual(2, _t.Roads.Count);
            Assert.AreEqual(2, _t.Flags.Count(f => f.Faction == Faction.Player));
        }

        [Test]
        public void Road_CannotCrossAnotherRoadExceptAtFlags()
        {
            Assert.IsTrue(Do(new BuildRoad(Faction.Player, P((1, -1), (2, -1), (3, -1), (4, -1), (5, -1), (6, -1)))).Ok);
            Assert.IsTrue(Do(new PlaceFlag(Faction.Player, N(4, -1))).Ok);
            Assert.IsTrue(Do(new BuildRoad(Faction.Player, P((4, -1), (4, 0), (4, 1), (4, 2)))).Ok);
            // A road from (4,2) that runs into the first road at (5,-1).
            Assert.AreEqual(Reasons.BadRoad, Why(new BuildRoad(Faction.Player, P((4, 2), (5, 1), (5, 0), (5, -1), (5, -2)))));
            Assert.AreEqual(Reasons.BadRoad, Why(new BuildRoad(Faction.Player, P((4, 2), (4, 1)))), "through its own road");
            // Ending on the other road's interior node is refused too (put a flag there first).
            Assert.AreEqual(Reasons.BadRoad, Why(new BuildRoad(Faction.Player, P((4, 2), (5, 1), (5, 0), (5, -1)))));
        }

        [Test]
        public void PlaceFlag_SplitsTheRoad_AndFlagsStayTwoApart()
        {
            var nodes = P((1, -1), (2, -1), (3, -1), (4, -1), (5, -1), (6, -1), (7, -1));
            Assert.IsTrue(Do(new BuildRoad(Faction.Player, nodes)).Ok);
            Assert.AreEqual(Reasons.FlagTooClose, Why(new PlaceFlag(Faction.Player, N(2, -1))), "next to the first flag");
            Assert.AreEqual(Reasons.FlagTooClose, Why(new PlaceFlag(Faction.Player, N(6, -1))), "next to the last flag");
            Assert.AreEqual(Reasons.NotOnRoad, Why(new PlaceFlag(Faction.Player, N(2, 3))));
            Assert.AreEqual(Reasons.NotOwner, Why(new PlaceFlag(Faction.Enemy, N(4, -1))));
            int flags = _t.Flags.Count;
            Assert.IsTrue(Do(new PlaceFlag(Faction.Player, N(4, -1))).Ok);
            Assert.AreEqual(flags + 1, _t.Flags.Count);
            Assert.AreEqual(2, _t.Roads.Count);
            Assert.AreEqual(3, _t.Roads[0].Length);
            Assert.AreEqual(3, _t.Roads[1].Length);
            Assert.AreEqual(0, _t.NodeRoad[N(4, -1)]);
            Assert.AreEqual(_t.Roads[0].FlagB, _t.Roads[1].FlagA);
            Assert.AreEqual(Reasons.FlagTooClose, Why(new PlaceFlag(Faction.Player, N(5, -1))), "next to the new flag");
            // Flags exactly two apart are fine.
            Assert.IsTrue(Do(new BuildRoad(Faction.Player, P((4, -1), (4, 0), (4, 1)))).Ok);
        }

        [Test]
        public void RemoveRoad_FreesItsNodes_AndDropsSpareFlags()
        {
            Assert.IsTrue(Do(new BuildRoad(Faction.Player, P((1, -1), (2, -1), (3, -1)))).Ok);
            int id = _t.Roads[0].Id;
            Assert.AreEqual(Reasons.NotOwner, Why(new RemoveRoad(Faction.Enemy, id)));
            Assert.AreEqual(Reasons.NoSuchThing, Why(new RemoveRoad(Faction.Player, 4242)));
            Assert.IsTrue(Do(new RemoveRoad(Faction.Player, id)).Ok);
            Assert.AreEqual(0, _t.Roads.Count);
            Assert.AreEqual(0, _t.NodeRoad[N(2, -1)]);
            Assert.IsNull(_t.FlagAtNode(N(3, -1)), "the spare end flag is gone");
            Assert.NotNull(_t.FlagAtNode(N(1, -1)), "the HQ flag stays");
        }

        [Test]
        public void FindRoadPath_IsAdjacent_Land_AndDeterministic()
        {
            var a = _t.FlagAtNode(N(1, -1)).Node;
            var p1 = _t.FindRoadPath(a, N(5, 3));
            var p2 = _t.FindRoadPath(a, N(5, 3));
            CollectionAssert.AreEqual(p1, p2);
            for (int i = 1; i < p1.Length; i++) Assert.IsTrue(_g.Adjacent(p1[i - 1], p1[i]));
            Assert.AreEqual(_g.Distance(a, N(5, 3)), p1.Length - 1, "no obstacles: a straight shortest way");
        }

        [Test]
        public void FindRoadPath_WithAvoid_KeepsClearOfThoseNodes_AndLeavesStateAlone()
        {
            var a = _t.FlagAtNode(N(1, -1)).Node;
            var straight = _t.FindRoadPath(a, N(5, 3));
            var avoid = new HashSet<int>(straight.Skip(1).Take(straight.Length - 2));
            var around = _t.FindRoadPath(a, N(5, 3), avoid);
            Assert.NotNull(around);
            Assert.IsFalse(around.Skip(1).Take(around.Length - 2).Any(avoid.Contains), "none of the avoided nodes is used");
            for (int i = 1; i < around.Length; i++) Assert.IsTrue(_g.Adjacent(around[i - 1], around[i]));
            CollectionAssert.AreEqual(straight, _t.FindRoadPath(a, N(5, 3), null), "null avoids nothing");
            Assert.IsNull(_t.FindRoadPath(a, N(5, 3), new HashSet<int>(_g.CircleNodes(N(5, 3)).Where(n => n != N(5, 3)))), "a fully fenced target has no way");
        }

        [Test]
        public void RoadJoinsTwoHqCircles_AndRoutesBetweenTheirFlags()
        {
            var topo = _s.Board.Topology;
            int target = topo.Corners.First(c => topo.VertexHexes(c).Count == 3 && topo.CornerDistance(_hq.Corner, c) == 2 && _sim.Check(new PlaceHq(Faction.Player, c)).Ok);
            Assert.IsTrue(Do(new PlaceHq(Faction.Player, target)).Ok);
            var hq2 = _t.Hqs.Single(h => h.Faction == Faction.Player && h.Corner == target);
            var a = _t.FlagAtNode(_g.FlagNodeOf(_hq.Node));
            var b = _t.FlagAtNode(_g.FlagNodeOf(hq2.Node));
            var path = _t.FindRoadPath(a.Node, b.Node);
            Assert.NotNull(path);
            Assert.IsTrue(path.Any(n => !_g.InCircle(_hq.Node, n) && !_g.InCircle(hq2.Node, n)), "the road leaves the circles");
            Assert.IsTrue(_sim.Check(new BuildRoad(Faction.Player, path)).Ok);
            // Flags on the way every 3 nodes would be needed for carriers; one road is enough for the route.
            Assert.IsTrue(Do(new BuildRoad(Faction.Player, path)).Ok);
            var route = _t.RouteBetweenFlags(Faction.Player, a.Id, b.Id);
            Assert.NotNull(route);
            CollectionAssert.AreEqual(new[] { a.Id, b.Id }, route.Flags);
            CollectionAssert.AreEqual(path, route.Nodes);
            var back = _t.RouteBetweenFlags(Faction.Player, b.Id, a.Id);
            CollectionAssert.AreEqual(path.AsEnumerable().Reverse().ToArray(), back.Nodes);
            Assert.IsNull(_t.RouteBetweenFlags(Faction.Enemy, a.Id, b.Id));
        }

        [Test]
        public void Routing_PicksTheShorterWay_WithDeterministicTies()
        {
            // Flags A=(1,-1), B=(4,-1) (after a split), C=(7,-1) in a line; a longer detour A->C also exists.
            Assert.IsTrue(Do(new BuildRoad(Faction.Player, P((1, -1), (2, -1), (3, -1), (4, -1), (5, -1), (6, -1), (7, -1)))).Ok);
            Assert.IsTrue(Do(new PlaceFlag(Faction.Player, N(4, -1))).Ok);
            int a = _t.FlagAtNode(N(1, -1)).Id, b = _t.FlagAtNode(N(4, -1)).Id, c = _t.FlagAtNode(N(7, -1)).Id;
            var r = _t.RouteBetweenFlags(Faction.Player, a, c);
            CollectionAssert.AreEqual(new[] { a, b, c }, r.Flags);
            Assert.AreEqual(6, r.Length);
            Assert.AreEqual(7, r.Nodes.Length);
            // A detour A -> C over the north side is longer, so it is not chosen.
            var detour = P((1, -1), (1, 0), (1, 1), (2, 1), (3, 1), (4, 1), (5, 1), (6, 0), (7, -1));
            Assert.IsTrue(Do(new BuildRoad(Faction.Player, detour)).Ok);
            Assert.AreEqual(3, _t.Roads.Count);
            CollectionAssert.AreEqual(new[] { a, b, c }, _t.RouteBetweenFlags(Faction.Player, a, c).Flags);
            // Remove the short way: the detour is used.
            foreach (var road in _t.Roads.Where(x => x.Length == 3).ToList()) Assert.IsTrue(Do(new RemoveRoad(Faction.Player, road.Id)).Ok);
            var viaDetour = _t.RouteBetweenFlags(Faction.Player, a, c);
            Assert.AreEqual(8, viaDetour.Length);
            // Same flag, and an unconnected flag.
            Assert.AreEqual(0, _t.RouteBetweenFlags(Faction.Player, a, a).Length);
            Assert.IsNull(_t.RouteBetweenFlags(Faction.Player, a, 99999));
        }

        private static int ViaOf(bool yFirst, out int x, out int y)
        {
            var sim = GodSim.Create(3);
            var t = sim.State.Territory; var g = t.Grid; var hq = t.HqsOf(Faction.Player).Single();
            Func<int, int, int> n = (di, dj) => g.Id(g.I(hq.Node) + di, g.J(hq.Node) + dj);
            Action<int[]> road = nodes => { Assert.IsTrue(sim.Submit(new BuildRoad(Faction.Player, nodes)).Ok); sim.Step(); };
            var ax = new[] { n(1, -1), n(1, 0), n(1, 1) };      // A -> X
            var ay = new[] { n(1, -1), n(2, -1), n(3, -1) };    // A -> Y
            if (yFirst) { road(ay); road(ax); } else { road(ax); road(ay); }
            road(new[] { n(1, 1), n(2, 1), n(3, 1) });          // X -> D
            road(new[] { n(3, -1), n(3, 0), n(3, 1) });         // Y -> D
            int a = t.FlagAtNode(n(1, -1)).Id, d = t.FlagAtNode(n(3, 1)).Id;
            x = t.FlagAtNode(n(1, 1)).Id; y = t.FlagAtNode(n(3, -1)).Id;
            var r = t.RouteBetweenFlags(Faction.Player, a, d);
            Assert.AreEqual(4, r.Length);
            Assert.AreEqual(3, r.Flags.Length);
            return r.Flags[1];
        }

        [Test]
        public void Routing_EqualRoutes_GoViaTheLowerFlagId()
        {
            int via = ViaOf(true, out int x, out int y);
            Assert.Less(y, x);
            Assert.AreEqual(y, via);
            via = ViaOf(false, out x, out y);
            Assert.Less(x, y);
            Assert.AreEqual(x, via);
        }
    }
}
