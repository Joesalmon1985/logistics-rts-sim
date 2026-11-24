using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>Shared helpers of the military tests.</summary>
    internal sealed class Bed
    {
        public readonly GodSim Sim;
        public readonly GodState S;
        public readonly TerritoryState T;
        public readonly NodeGrid G;
        public readonly MilitaryState M;
        public readonly List<SimEvent> Log = new List<SimEvent>();
        public readonly Hq PlayerHq, EnemyHq;

        public Bed(ulong seed = 3, bool waves = false)
        {
            Sim = GodSim.Create(seed);
            S = Sim.State; T = S.Territory; G = T.Grid; M = S.Military;
            PlayerHq = T.HqsOf(Faction.Player).Single();
            EnemyHq = T.HqsOf(Faction.Enemy).Single();
            M.WavesEnabled = waves;
        }

        public void Step(int n = 1)
        {
            for (int i = 0; i < n; i++) { Sim.Step(); Log.AddRange(Sim.DrainEvents()); }
        }

        /// <summary>Steps until the condition holds or the limit is hit. Returns true when it held.</summary>
        public bool RunUntil(int maxTicks, Func<bool> done)
        {
            for (int i = 0; i < maxTicks; i++)
            {
                if (done()) return true;
                Step();
            }
            return done();
        }

        public CommandResult Do(Command c)
        {
            var r = Sim.Submit(c);
            if (r.Ok) Step();
            return r;
        }

        public IEnumerable<T2> Events<T2>() where T2 : SimEvent => Log.OfType<T2>();

        public MilUnit Spawn(Faction f, UnitKind k, int node)
        {
            int before = M.NextUnitId;
            Assert.IsTrue(Do(new DevSpawnUnit(f, k, node)).Ok);
            return M.UnitById(before);
        }

        public int Node(Hq hq, int di, int dj) => G.Id(G.I(hq.Node) + di, G.J(hq.Node) + dj);

        public Building EnemyKind(string kind) => T.Buildings.First(b => b.Faction == Faction.Enemy && b.Kind == kind);

        /// <summary>A run of land nodes in one lattice direction from some node inside a circle, as a start node and direction.</summary>
        public (int start, int dir) LandLine(Hq hq, int steps)
        {
            foreach (int n in G.CircleNodes(hq.Node))
                for (int d = 0; d < 6; d++)
                {
                    int cur = n; bool ok = G.IsLand(cur);
                    for (int k = 0; k < steps && ok; k++) { cur = G.Neighbour(cur, d); ok = cur >= 0 && G.IsLand(cur); }
                    if (ok) return (n, d);
                }
            throw new InvalidOperationException("no land line");
        }

        public int Walk(int node, int dir, int steps)
        {
            for (int k = 0; k < steps; k++) node = G.Neighbour(node, dir);
            return node;
        }

        public int DistMm(MilUnit a, MilUnit b) => Pathing.Dist(a.X, a.Z, b.X, b.Z);
    }

    public class MilitaryBaseTests
    {
        [Test]
        public void EnemyBase_IsPlacedAtStart_ForEverySeed()
        {
            for (ulong seed = 1; seed <= 60; seed++)
            {
                var b = new Bed(seed);
                var circle = b.T.CircleOf(b.EnemyHq);
                var theirs = b.T.Buildings.Where(x => x.Faction == Faction.Enemy && x.Size != BuildingSize.Hq).ToList();
                Assert.AreEqual(5, theirs.Count, "seed " + seed);
                foreach (string kind in new[] { BuildingKinds.Barracks, BuildingKinds.VehicleWorkshop, BuildingKinds.Woodcutter, BuildingKinds.Sawmill, BuildingKinds.Quarry })
                    Assert.AreEqual(1, theirs.Count(x => x.Kind == kind), kind + " seed " + seed);
                foreach (var x in theirs)
                {
                    Assert.IsTrue(b.G.InCircle(b.EnemyHq.Node, x.Node), "inside the build circle");
                    Assert.IsNotNull(b.T.FlagAtNode(x.FlagNode), "has a flag");
                }
                Assert.AreEqual(0, b.T.Buildings.Count(x => x.Faction == Faction.Player && x.Size != BuildingSize.Hq));
            }
        }

        [Test]
        public void BuildingHealth_StartsFull_ByHqAndSize_AndNewBuildingsAreRegistered()
        {
            var b = new Bed();
            Assert.AreEqual(1500, b.M.BuildingHealth(b.EnemyHq.Id));
            Assert.AreEqual(1500, b.M.BuildingHealth(b.PlayerHq.Id));
            foreach (var x in b.T.Buildings.Where(x => x.Size == BuildingSize.Medium)) Assert.AreEqual(700, b.M.BuildingHealth(x.Id));
            foreach (var x in b.T.Buildings.Where(x => x.Size == BuildingSize.Small)) Assert.AreEqual(400, b.M.BuildingHealth(x.Id));

            int node = b.Node(b.PlayerHq, -3, 0);
            Assert.IsTrue(b.Do(new PlaceBuilding(Faction.Player, node, BuildingSize.Small)).Ok);
            var nb = b.T.BuildingAtNode(node);
            Assert.AreEqual(400, b.M.BuildingHealth(nb.Id));
            Assert.AreEqual(1f, b.M.BuildingHealthFraction(nb.Id));
            Assert.IsTrue(b.Do(new RemoveBuilding(Faction.Player, nb.Id)).Ok);
            b.Step();
            Assert.AreEqual(-1, b.M.BuildingHealth(nb.Id), "record dropped with the building");
        }
    }

    public class MilitaryMovementTests
    {
        [Test]
        public void Unit_WalksToAPoint_AtItsSpeed_AndStopsThere()
        {
            var b = new Bed();
            var (start, dir) = b.LandLine(b.PlayerHq, 8);
            var u = b.Spawn(Faction.Player, UnitKind.Infantry, start);
            int gx = Pathing.NodeMmX(b.G, b.Walk(start, dir, 8)), gz = Pathing.NodeMmZ(b.G, b.Walk(start, dir, 8));
            int startX = u.X, startZ = u.Z;
            int dist = Pathing.Dist(u.X, u.Z, gx, gz);
            Assert.IsTrue(b.Do(new DevMoveUnit(u.Id, gx, gz)).Ok);
            b.Step(10);
            int moved = Pathing.Dist(startX, startZ, u.X, u.Z);
            Assert.That(moved, Is.InRange(2800, 3300), "about 3 m in the first second, got " + moved);
            Assert.IsTrue(b.RunUntil(dist / 300 + 20, () => !u.HasGoal));
            Assert.AreEqual(gx, u.X); Assert.AreEqual(gz, u.Z);
            Assert.AreEqual(UnitActivity.Idle, u.Activity);
        }

        [Test]
        public void Vehicle_IsFasterThanInfantry()
        {
            var b = new Bed();
            var (start, dir) = b.LandLine(b.PlayerHq, 8);
            var a = b.Spawn(Faction.Player, UnitKind.Infantry, start);
            var v = b.Spawn(Faction.Player, UnitKind.Vehicle, start);
            int gx = Pathing.NodeMmX(b.G, b.Walk(start, dir, 8)), gz = Pathing.NodeMmZ(b.G, b.Walk(start, dir, 8));
            b.Do(new DevMoveUnit(a.Id, gx, gz)); b.Do(new DevMoveUnit(v.Id, gx, gz));
            b.Step(20);
            Assert.Greater(Pathing.Dist(v.X, v.Z, gx, gz) * -1, Pathing.Dist(a.X, a.Z, gx, gz) * -1, "the vehicle is closer to the goal");
        }

        [Test]
        public void Unit_GoesAroundWater_NeverStepsOffLand()
        {
            var b = new Bed();
            var coast = new List<int>();
            for (int n = 0; n < b.G.Count; n++)
            {
                if (!b.G.IsLand(n)) continue;
                for (int d = 0; d < 6; d++) { int nb = b.G.Neighbour(n, d); if (nb >= 0 && !b.G.IsLand(nb)) { coast.Add(n); break; } }
            }
            int a = -1, c = -1;
            for (int i = 0; i < coast.Count && a < 0; i++)
                for (int j = i + 1; j < coast.Count; j++)
                {
                    int steps = b.G.Distance(coast[i], coast[j]);
                    if (steps < 8 || steps > 40) continue;
                    if (Pathing.LineClear(b.G, Pathing.NodeMmX(b.G, coast[i]), Pathing.NodeMmZ(b.G, coast[i]), Pathing.NodeMmX(b.G, coast[j]), Pathing.NodeMmZ(b.G, coast[j]))) continue;
                    a = coast[i]; c = coast[j]; break;
                }
            Assert.GreaterOrEqual(a, 0, "the coast has a notch the straight line cannot cross");
            var u = b.Spawn(Faction.Player, UnitKind.Vehicle, a);
            int gx = Pathing.NodeMmX(b.G, c), gz = Pathing.NodeMmZ(b.G, c);
            int straight = Pathing.Dist(u.X, u.Z, gx, gz);
            Assert.IsTrue(b.Do(new DevMoveUnit(u.Id, gx, gz)).Ok);
            long travelled = 0; int px = u.X, pz = u.Z;
            for (int i = 0; i < 2000 && u.HasGoal; i++)
            {
                b.Step();
                Assert.IsTrue(Pathing.IsLandPoint(b.G, u.X, u.Z), "on land at tick " + i);
                travelled += Pathing.Dist(px, pz, u.X, u.Z); px = u.X; pz = u.Z;
            }
            Assert.IsFalse(u.HasGoal, "arrived");
            Assert.AreEqual(gx, u.X); Assert.AreEqual(gz, u.Z);
            Assert.Greater(travelled, straight, "the way round is longer than the straight line");
        }

        [Test]
        public void AStar_FindsAPath_OnLandOnly()
        {
            var b = new Bed();
            var land = Enumerable.Range(0, b.G.Count).Where(b.G.IsLand).ToList();
            var path = Pathing.FindNodePath(b.G, land.First(), land.Last());
            Assert.NotNull(path);
            Assert.AreEqual(land.First(), path[0]); Assert.AreEqual(land.Last(), path[path.Length - 1]);
            foreach (int n in path) Assert.IsTrue(b.G.IsLand(n));
            for (int i = 1; i < path.Length; i++) Assert.IsTrue(b.G.Adjacent(path[i - 1], path[i]));
            Assert.IsNull(Pathing.FindNodePath(b.G, land.First(), 0), "node 0 is water");
        }
    }

    public class MilitaryCombatTests
    {
        [Test]
        public void DamageMultipliers_FollowTheTable()
        {
            Assert.AreEqual(12, MilitaryTuning.ShotDamage(UnitKind.Infantry, TargetClass.Infantry));
            Assert.AreEqual(4, MilitaryTuning.ShotDamage(UnitKind.Infantry, TargetClass.Vehicle));
            Assert.AreEqual(4, MilitaryTuning.ShotDamage(UnitKind.Infantry, TargetClass.Building));
            Assert.AreEqual(20, MilitaryTuning.ShotDamage(UnitKind.Vehicle, TargetClass.Infantry));
            Assert.AreEqual(60, MilitaryTuning.ShotDamage(UnitKind.Vehicle, TargetClass.Vehicle));
            Assert.AreEqual(60, MilitaryTuning.ShotDamage(UnitKind.Vehicle, TargetClass.Building));
            Assert.AreEqual(100, MilitaryTuning.Infantry.MaxHealth);
            Assert.AreEqual(220, MilitaryTuning.Vehicle.MaxHealth);
            Assert.AreEqual(12000, MilitaryTuning.Infantry.RangeMm);
            Assert.AreEqual(16000, MilitaryTuning.Vehicle.RangeMm);
            Assert.AreEqual(20000, MilitaryTuning.SightMm);
            Assert.AreEqual(6, MilitaryTuning.Infantry.CooldownTicks);
            Assert.AreEqual(20, MilitaryTuning.Vehicle.CooldownTicks);
        }

        [Test]
        public void Fight_FiresOnCooldown_WithTheTableDamage_AndEndsInDeath()
        {
            var b = new Bed();
            var (start, dir) = b.LandLine(b.PlayerHq, 3);
            var inf = b.Spawn(Faction.Enemy, UnitKind.Infantry, start);
            var veh = b.Spawn(Faction.Player, UnitKind.Vehicle, b.Walk(start, dir, 3)); // 15 m apart: vehicle in range, infantry not
            Assert.IsTrue(b.RunUntil(400, () => !b.M.Units.Contains(inf)), "the infantry dies");

            var vehShots = b.Events<UnitFired>().Where(e => e.AttackerId == veh.Id).ToList();
            Assert.AreEqual(5, vehShots.Count, "5 shots of 20 kill 100 hp");
            foreach (var e in vehShots) { Assert.AreEqual(20, e.Damage); Assert.AreEqual(inf.Id, e.TargetId); Assert.AreEqual(TargetKind.Unit, e.TargetKind); }
            for (int i = 1; i < vehShots.Count; i++) Assert.AreEqual(20, vehShots[i].Tick - vehShots[i - 1].Tick, "2 s between vehicle shots");

            var infShots = b.Events<UnitFired>().Where(e => e.AttackerId == inf.Id).ToList();
            Assert.Greater(infShots.Count, 0, "the infantry closed to 12 m and answered");
            foreach (var e in infShots) Assert.AreEqual(4, e.Damage, "x0.5 against vehicles");
            for (int i = 1; i < infShots.Count; i++) Assert.AreEqual(6, infShots[i].Tick - infShots[i - 1].Tick, "0.6 s between infantry shots");
            Assert.AreEqual(220 - 4 * infShots.Count, veh.Health);
            Assert.AreEqual(1, b.Events<UnitDied>().Count(e => e.UnitId == inf.Id));
        }

        [Test]
        public void Infantry_Beats_Infantry_With_x1_5()
        {
            var b = new Bed();
            var (start, dir) = b.LandLine(b.PlayerHq, 2);
            var a = b.Spawn(Faction.Player, UnitKind.Infantry, start);
            var e = b.Spawn(Faction.Enemy, UnitKind.Infantry, b.Walk(start, dir, 2)); // 10 m, in range of both
            b.Step(7);
            Assert.AreEqual(100 - 12 - 12, a.Health, "two volleys of 12 within 7 ticks");
            Assert.AreEqual(100 - 12 - 12, e.Health);
        }

        [Test]
        public void Units_PreferEnemyUnits_ToBuildings()
        {
            var b = new Bed();
            var eb = b.EnemyKind(BuildingKinds.Woodcutter);
            int ebX = Pathing.NodeMmX(b.G, eb.Node);
            var rec = b.M.BuildingById(eb.Id);
            var veh = b.Spawn(Faction.Player, UnitKind.Vehicle, b.Node(b.EnemyHq, 0, 0));
            var inf = b.Spawn(Faction.Enemy, UnitKind.Infantry, b.Node(b.EnemyHq, 0, 0));
            b.Step();
            Assert.AreEqual(TargetKind.Unit, veh.TargetKind, "enemy unit before any building");
            Assert.AreEqual(inf.Id, veh.TargetId);
            Assert.AreEqual(ebX, rec.X);
        }

        [Test]
        public void Building_IsDestroyed_LeavesRubble30s_AndTheNodeCanBeBuiltOnAgain()
        {
            var b = new Bed();
            var target = b.EnemyKind(BuildingKinds.Woodcutter);
            int node = target.Node;
            b.Spawn(Faction.Player, UnitKind.Vehicle, b.G.FlagNodeOf(node));
            b.Spawn(Faction.Player, UnitKind.Vehicle, b.G.FlagNodeOf(node));
            // They take whatever enemy building is nearest; hold the others up so only the woodcutter dies first.
            foreach (var r in b.M.Buildings) if (r.Id != target.Id) r.Health = r.MaxHealth = 1000000;
            Assert.IsTrue(b.RunUntil(600, () => b.T.BuildingById(target.Id) == null), "the woodcutter falls");

            var d = b.Events<BuildingDestroyed>().Single();
            Assert.AreEqual(target.Id, d.BuildingId); Assert.AreEqual(node, d.Node); Assert.IsFalse(d.IsHq);
            Assert.AreEqual(1, b.M.Rubbles.Count);
            var rub = b.M.Rubbles[0];
            Assert.AreEqual(node, rub.Node); Assert.AreEqual(BuildingKinds.Woodcutter, rub.Kind);
            Assert.That(rub.TicksLeft, Is.InRange(290, 300));
            Assert.AreEqual(0, b.T.NodeBuilding[node], "the node is free");
            Assert.IsTrue(b.Sim.Check(new PlaceBuilding(Faction.Enemy, node, BuildingSize.Small)).Ok, "and can be built on");
            Assert.AreEqual(-1, b.M.BuildingHealth(target.Id));

            b.Step(300);
            Assert.AreEqual(0, b.M.Rubbles.Count, "rubble gone after 30 s");
            Assert.AreEqual(1, b.Events<RubbleExpired>().Count());
        }

        [Test]
        public void LosingTheLastHq_LeavesHqRubble_AndEndsTheGame()
        {
            var b = new Bed();
            var rec = b.M.BuildingById(b.PlayerHq.Id);
            rec.Health = 1;
            // Enemy infantry on top of the player HQ.
            b.Spawn(Faction.Enemy, UnitKind.Vehicle, b.Node(b.PlayerHq, 0, 0));
            Assert.IsTrue(b.RunUntil(100, () => b.M.IsGameOver));
            Assert.AreEqual(GameOutcome.Lost, b.M.Outcome);
            Assert.AreEqual(1, b.M.Rubbles.Count);
            Assert.IsTrue(b.M.Rubbles[0].Size == BuildingSize.Hq);
            Assert.AreEqual(0, b.T.Hqs.Count(h => h.Faction == Faction.Player));
            Assert.AreEqual(0, b.T.NodeBuilding[b.PlayerHq.Node]);
        }
    }

    public class MilitarySpawnTests
    {
        [Test]
        public void UnitQueue_IsDrained_AndTheUnitAppearsAtTheFactoryFlag()
        {
            var b = new Bed();
            int node = b.Node(b.PlayerHq, -3, 0);
            Assert.IsTrue(b.Do(new PlaceBuilding(Faction.Player, node, BuildingSize.Medium) { Kind = BuildingKinds.Barracks }).Ok);
            var fac = b.T.BuildingAtNode(node);
            b.S.UnitQueue.Add(new UnitOrder { Faction = Faction.Player, BuildingId = fac.Id, Kind = UnitKind.Infantry });
            b.S.UnitQueue.Add(new UnitOrder { Faction = Faction.Player, BuildingId = fac.Id, Kind = UnitKind.Vehicle });
            b.Step();
            Assert.AreEqual(0, b.S.UnitQueue.Count);
            Assert.AreEqual(2, b.M.Units.Count);
            Assert.AreEqual(UnitKind.Infantry, b.M.Units[0].Kind); Assert.AreEqual(UnitKind.Vehicle, b.M.Units[1].Kind);
            int fx = Pathing.NodeMmX(b.G, fac.FlagNode), fz = Pathing.NodeMmZ(b.G, fac.FlagNode);
            foreach (var u in b.M.Units) Assert.Less(Pathing.Dist(u.X, u.Z, fx, fz), 3500, "spawned at the factory flag");
            Assert.AreEqual(2, b.Events<UnitSpawned>().Count());
        }

        [Test]
        public void UnitQueue_WhenTheFactoryIsGone_SpawnsAtTheNearestHq()
        {
            var b = new Bed();
            int node = b.Node(b.PlayerHq, -3, 0);
            b.Do(new PlaceBuilding(Faction.Player, node, BuildingSize.Medium) { Kind = BuildingKinds.Barracks });
            var fac = b.T.BuildingAtNode(node);
            b.Step();
            Assert.IsTrue(b.Do(new RemoveBuilding(Faction.Player, fac.Id)).Ok);
            b.S.UnitQueue.Add(new UnitOrder { Faction = Faction.Player, BuildingId = fac.Id, Kind = UnitKind.Infantry });
            b.Step();
            Assert.AreEqual(1, b.M.Units.Count);
            var hqFlag = b.T.BuildingById(b.PlayerHq.Id).FlagNode;
            Assert.Less(Pathing.Dist(b.M.Units[0].X, b.M.Units[0].Z, Pathing.NodeMmX(b.G, hqFlag), Pathing.NodeMmZ(b.G, hqFlag)), 3500);
        }

        [Test]
        public void UnitQueue_WhenTheFactoryIsGoneAndTheFactionHasNoHq_IsDropped()
        {
            var b = new Bed();
            b.S.UnitQueue.Add(new UnitOrder { Faction = Faction.Player, BuildingId = 9999, Kind = UnitKind.Infantry });
            MilitaryTerritory.Remove(b.S, b.PlayerHq.Id, null);
            b.Step();
            Assert.AreEqual(0, b.M.Units.Count);
            Assert.AreEqual(0, b.S.UnitQueue.Count);
        }

        [Test]
        public void DevSpawn_NeedsLand()
        {
            var b = new Bed();
            Assert.IsFalse(b.Sim.Submit(new DevSpawnUnit(Faction.Player, UnitKind.Infantry, 0)).Ok);
            Assert.IsTrue(b.Sim.Submit(new DevSpawnUnit(Faction.Player, UnitKind.Infantry, b.PlayerHq.Node)).Ok);
        }
    }

    public class MilitaryOrdersTests
    {
        [Test]
        public void MarkTarget_KeepsMarkOrder_AndValidates()
        {
            var b = new Bed();
            var w = b.EnemyKind(BuildingKinds.Woodcutter); var q = b.EnemyKind(BuildingKinds.Quarry);
            Assert.IsFalse(b.Sim.Submit(new MarkTarget(b.PlayerHq.Id)).Ok, "not an enemy");
            Assert.IsFalse(b.Sim.Submit(new MarkTarget(99999)).Ok);
            Assert.IsTrue(b.Do(new MarkTarget(q.Id)).Ok);
            Assert.IsTrue(b.Do(new MarkTarget(w.Id)).Ok);
            Assert.IsFalse(b.Sim.Submit(new MarkTarget(q.Id)).Ok, "twice");
            Assert.IsTrue(b.Do(new MarkTarget(b.EnemyHq.Id)).Ok);
            CollectionAssert.AreEqual(new[] { q.Id, w.Id, b.EnemyHq.Id }, b.M.MarkedTargets);
            Assert.AreEqual(2, b.M.MarkOrder(w.Id));
            Assert.IsTrue(b.Do(new UnmarkTarget(w.Id)).Ok);
            CollectionAssert.AreEqual(new[] { q.Id, b.EnemyHq.Id }, b.M.MarkedTargets);
            Assert.IsFalse(b.Sim.Submit(new UnmarkTarget(w.Id)).Ok);
        }

        [Test]
        public void Targets_AreAttackedInTheOrderMarked()
        {
            var b = new Bed();
            var first = b.EnemyKind(BuildingKinds.Quarry);
            var second = b.EnemyKind(BuildingKinds.Woodcutter);
            b.Do(new MarkTarget(first.Id)); b.Do(new MarkTarget(second.Id));
            for (int i = 0; i < 4; i++) b.Spawn(Faction.Player, UnitKind.Vehicle, b.EnemyHq.Node);
            bool secondTouched = false;
            bool firstDone = false;
            for (int i = 0; i < 1500 && b.M.BuildingById(second.Id) != null; i++)
            {
                b.Step();
                if (b.M.BuildingById(first.Id) != null && b.M.BuildingById(second.Id)?.Damaged == true) secondTouched = true;
                firstDone = b.M.BuildingById(first.Id) == null;
            }
            Assert.IsTrue(firstDone, "target 1 fell");
            Assert.IsFalse(secondTouched, "target 2 untouched while target 1 stood");
            Assert.IsNull(b.M.BuildingById(second.Id), "then target 2 fell");
            var order = b.Events<BuildingDestroyed>().Select(e => e.BuildingId).ToList();
            Assert.Less(order.IndexOf(first.Id), order.IndexOf(second.Id));
            Assert.AreEqual(0, b.M.Marked.Count, "destroyed targets leave the list");
        }

        [Test]
        public void Defend_Validates_AndUpdatesAnExistingFlag()
        {
            var b = new Bed();
            Assert.IsFalse(b.Sim.Submit(SetDefend.OnBuilding(b.PlayerHq.Id, 6)).Ok, "0 to 5 guards");
            Assert.IsFalse(b.Sim.Submit(SetDefend.OnBuilding(b.PlayerHq.Id, -1)).Ok);
            Assert.IsFalse(b.Sim.Submit(SetDefend.OnBuilding(b.EnemyHq.Id, 1)).Ok, "own buildings only");
            Assert.IsFalse(b.Sim.Submit(SetDefend.OnNode(0, 1)).Ok, "land only");
            Assert.IsTrue(b.Do(SetDefend.OnBuilding(b.PlayerHq.Id, 2)).Ok);
            Assert.IsTrue(b.Do(SetDefend.OnNode(b.Node(b.PlayerHq, -3, 0), 1)).Ok);
            Assert.AreEqual(2, b.M.Defends.Count);
            Assert.IsTrue(b.Do(SetDefend.OnBuilding(b.PlayerHq.Id, 4)).Ok);
            Assert.AreEqual(2, b.M.Defends.Count, "same anchor, same flag");
            Assert.AreEqual(4, b.M.DefendAtBuilding(b.PlayerHq.Id).Guards);
            var flag = b.M.DefendAtNode(b.Node(b.PlayerHq, -3, 0));
            Assert.IsTrue(b.Do(new RemoveDefend(flag.Id)).Ok);
            Assert.IsFalse(b.Sim.Submit(new RemoveDefend(flag.Id)).Ok);
            Assert.AreEqual(1, b.M.Defends.Count);
        }

        [Test]
        public void DefendFlag_GoesWhenItsBuildingDoes()
        {
            var b = new Bed();
            int node = b.Node(b.PlayerHq, -3, 0);
            b.Do(new PlaceBuilding(Faction.Player, node, BuildingSize.Small));
            var bl = b.T.BuildingAtNode(node);
            b.Do(SetDefend.OnBuilding(bl.Id, 1));
            Assert.AreEqual(1, b.M.Defends.Count);
            b.Do(new RemoveBuilding(Faction.Player, bl.Id));
            b.Step();
            Assert.AreEqual(0, b.M.Defends.Count);
            Assert.AreEqual(1, b.Events<DefendRemoved>().Count(e => e.AnchorLost));
        }

        [Test]
        public void GuardCounts_AreFilledBeforeAnythingAttacks()
        {
            var b = new Bed();
            b.Do(new MarkTarget(b.EnemyKind(BuildingKinds.Quarry).Id));
            var u1 = b.Spawn(Faction.Player, UnitKind.Infantry, b.Node(b.PlayerHq, 0, 0));
            var u2 = b.Spawn(Faction.Player, UnitKind.Infantry, b.Node(b.PlayerHq, 0, 1));
            b.Do(SetDefend.OnBuilding(b.PlayerHq.Id, 2));
            b.Step(3);
            var flag = b.M.DefendAtBuilding(b.PlayerHq.Id);
            Assert.AreEqual(2, b.M.GuardCount(flag.Id), "both units guard");
            Assert.AreNotEqual(UnitActivity.Marching, u1.Activity);
            Assert.AreNotEqual(UnitActivity.Marching, u2.Activity);

            var u3 = b.Spawn(Faction.Player, UnitKind.Infantry, b.Node(b.PlayerHq, 1, 0));
            b.Step(3);
            Assert.AreEqual(2, b.M.GuardCount(flag.Id));
            Assert.AreEqual(0, u3.GuardFlagId);
            Assert.AreEqual(UnitActivity.Marching, u3.Activity, "the rest march on the marked target");

            b.Do(SetDefend.OnBuilding(b.PlayerHq.Id, 3));
            b.Step(3);
            Assert.AreEqual(3, b.M.GuardCount(flag.Id), "a free unit joins a flag that grew");
            Assert.AreNotEqual(UnitActivity.Marching, u3.Activity);
            b.Do(SetDefend.OnBuilding(b.PlayerHq.Id, 1));
            b.Step(3);
            Assert.AreEqual(1, b.M.GuardCount(flag.Id), "extra guards are released");
        }

        [Test]
        public void OldestFlagIsFilledFirst_ByTheNearestFreeUnit()
        {
            var b = new Bed();
            var far = b.Spawn(Faction.Player, UnitKind.Infantry, b.Node(b.PlayerHq, 0, 0));
            int n1 = b.Node(b.PlayerHq, -4, 0);
            b.Do(SetDefend.OnNode(n1, 1));           // oldest
            b.Do(SetDefend.OnBuilding(b.PlayerHq.Id, 1));
            b.Step(2);
            Assert.AreEqual(b.M.DefendAtNode(n1).Id, far.GuardFlagId, "the only unit goes to the oldest flag");
            var near = b.Spawn(Faction.Player, UnitKind.Infantry, b.Node(b.PlayerHq, 0, 0));
            b.Step(2);
            Assert.AreEqual(b.M.DefendAtBuilding(b.PlayerHq.Id).Id, near.GuardFlagId, "the next unit fills the next flag");
        }

        [Test]
        public void WithNoTarget_FreeUnitsWaitNearTheirHq_AndStayPut()
        {
            var b = new Bed();
            var u = b.Spawn(Faction.Player, UnitKind.Infantry, b.Node(b.PlayerHq, 0, 0));
            b.Step(200);
            var hq = b.M.BuildingById(b.PlayerHq.Id);
            Assert.Less(Pathing.Dist(u.X, u.Z, hq.X, hq.Z), 14000);
            int x = u.X, z = u.Z;
            b.Step(100);
            Assert.AreEqual(x, u.X); Assert.AreEqual(z, u.Z);
            Assert.AreEqual(UnitActivity.Waiting, u.Activity);
        }

        [Test]
        public void Guard_ReturnsToItsAnchor_AfterBeingPulledAway()
        {
            var b = new Bed();
            var g1 = b.Spawn(Faction.Player, UnitKind.Infantry, b.PlayerHq.Node);
            b.Do(SetDefend.OnBuilding(b.PlayerHq.Id, 1));
            b.Step(5);
            Assert.AreEqual(1, b.M.GuardCount(b.M.Defends[0].Id));
            // Walk it 12 m away with a dev order, then let the brain take over again.
            int tx = 0, tz = 0; bool found = false;
            for (int d = 0; d < 6 && !found; d++)
            {
                int n = b.Walk(b.PlayerHq.Node, d, 0); int m = n;
                bool ok = true;
                for (int k = 0; k < 3; k++) { m = b.G.Neighbour(m, d); if (m < 0 || !b.G.IsLand(m)) { ok = false; break; } }
                if (ok) { tx = Pathing.NodeMmX(b.G, m); tz = Pathing.NodeMmZ(b.G, m); found = true; }
            }
            Assert.IsTrue(found);
            b.Do(new DevMoveUnit(g1.Id, tx, tz));
            Assert.IsTrue(b.RunUntil(200, () => !g1.HasGoal));
            var anchor = b.M.Defends[0];
            Assert.Greater(Pathing.Dist(g1.X, g1.Z, anchor.X, anchor.Z), MilitaryTuning.GuardReturnMm, "pulled out past the leash");
            Assert.IsTrue(b.RunUntil(300, () => Pathing.Dist(g1.X, g1.Z, anchor.X, anchor.Z) <= MilitaryTuning.GuardStayMm));
            b.Step(100);
            Assert.LessOrEqual(Pathing.Dist(g1.X, g1.Z, anchor.X, anchor.Z), MilitaryTuning.GuardStayMm);
            Assert.AreEqual(UnitActivity.Guarding, g1.Activity);
        }

        [Test]
        public void Guards_ChaseAnAttacker_ThenReturn()
        {
            var b = new Bed();
            var g1 = b.Spawn(Faction.Player, UnitKind.Infantry, b.PlayerHq.Node);
            var g2 = b.Spawn(Faction.Player, UnitKind.Infantry, b.PlayerHq.Node);
            b.Do(SetDefend.OnBuilding(b.PlayerHq.Id, 2));
            b.Step(60);
            var anchor = b.M.Defends[0];
            // An enemy infantry about 14 m from the anchor, beyond a guard's 12 m range.
            int en = -1;
            for (int d = 0; d < 6 && en < 0; d++)
            {
                int m = b.PlayerHq.Node; bool ok = true;
                for (int k = 0; k < 3; k++) { m = b.G.Neighbour(m, d); if (m < 0 || !b.G.IsLand(m)) { ok = false; break; } }
                if (ok) { en = m; }
            }
            Assert.GreaterOrEqual(en, 0);
            var enemy = b.Spawn(Faction.Enemy, UnitKind.Infantry, en);
            int maxOut = 0;
            Assert.IsTrue(b.RunUntil(400, () =>
            {
                foreach (var g in new[] { g1, g2 }) maxOut = Math.Max(maxOut, Pathing.Dist(g.X, g.Z, anchor.X, anchor.Z));
                return !b.M.Units.Contains(enemy);
            }), "the attacker dies");
            b.Step(300);
            foreach (var g in new[] { g1, g2 })
            {
                if (!b.M.Units.Contains(g)) continue;
                Assert.LessOrEqual(Pathing.Dist(g.X, g.Z, anchor.X, anchor.Z), MilitaryTuning.GuardStayMm, "back at the anchor");
                Assert.AreEqual(UnitActivity.Guarding, g.Activity);
            }
            Assert.IsTrue(b.M.Units.Contains(g1) || b.M.Units.Contains(g2), "two against one: a guard survived");
            Assert.LessOrEqual(maxOut, MilitaryTuning.GuardChaseMm + 2000, "never chased far past 15 m");
        }
    }

    public class MilitaryWaveTests
    {
        private static Bed Fortified()
        {
            var b = new Bed(3, waves: true);
            var hq = b.M.BuildingById(b.PlayerHq.Id);
            hq.Health = hq.MaxHealth = 100000000;
            return b;
        }

        [Test]
        public void Waves_ComeOnTheirClock_WithTheRightComposition()
        {
            var b = Fortified();
            b.Step(MilitaryTuning.FirstWaveTicks - 1);
            Assert.AreEqual(0, b.M.WaveNumber);
            Assert.AreEqual(1, b.M.TicksToNextWave(b.S.Tick));
            b.Step(1);
            Assert.AreEqual(1, b.M.WaveNumber);
            b.Step(MilitaryTuning.WaveIntervalTicks * 3);
            var waves = b.Events<WaveSpawned>().ToList();
            Assert.AreEqual(4, waves.Count);
            CollectionAssert.AreEqual(new long[] { MilitaryTuning.FirstWaveTicks, MilitaryTuning.FirstWaveTicks + MilitaryTuning.WaveIntervalTicks, MilitaryTuning.FirstWaveTicks + 2 * MilitaryTuning.WaveIntervalTicks, MilitaryTuning.FirstWaveTicks + 3 * MilitaryTuning.WaveIntervalTicks }, waves.Select(w => w.Tick).ToArray());
            CollectionAssert.AreEqual(new[] { 2, 3, 4, 5 }, waves.Select(w => w.Infantry).ToArray());
            CollectionAssert.AreEqual(new[] { 0, 0, 1, 2 }, waves.Select(w => w.Vehicles).ToArray());

            var spawned = b.Events<UnitSpawned>().Where(e => e.Faction == Faction.Enemy).ToList();
            Assert.AreEqual(2 + 3 + 5 + 7, spawned.Count);
            foreach (var e in spawned.Where(e => e.Tick == MilitaryTuning.FirstWaveTicks)) Assert.AreEqual(UnitKind.Infantry, e.Kind);
            Assert.AreEqual(1, spawned.Count(e => e.Tick == MilitaryTuning.FirstWaveTicks + 2 * MilitaryTuning.WaveIntervalTicks && e.Kind == UnitKind.Vehicle));
            var barracks = b.T.Buildings.First(x => x.Kind == BuildingKinds.Barracks);
            var shop = b.T.Buildings.First(x => x.Kind == BuildingKinds.VehicleWorkshop);
            foreach (var e in spawned)
            {
                int node = e.Kind == UnitKind.Infantry ? barracks.FlagNode : shop.FlagNode;
                Assert.Less(Pathing.Dist(e.X, e.Z, Pathing.NodeMmX(b.G, node), Pathing.NodeMmZ(b.G, node)), 3500);
            }
        }

        [Test]
        public void Wave_TargetsThePlayerBuildingNearestTheEnemyHq()
        {
            var b = Fortified();
            Assert.IsTrue(b.Do(new PlaceBuilding(Faction.Player, b.Node(b.PlayerHq, -3, 0), BuildingSize.Small)).Ok);
            b.Step(MilitaryTuning.FirstWaveTicks);
            var grp = b.M.WaveGroups.Single(g => g.Id != 0);
            var home = b.M.BuildingById(b.EnemyHq.Id);
            long best = long.MaxValue; int bestId = 0;
            foreach (var r in b.M.Buildings.Where(r => r.Faction == Faction.Player))
            {
                long d = Pathing.Dist2(home.X, home.Z, r.X, r.Z);
                if (d < best) { best = d; bestId = r.Id; }
            }
            Assert.AreEqual(TargetKind.Building, grp.TargetKind);
            Assert.AreEqual(bestId, grp.TargetId);
            var wave = b.M.Units.Where(u => u.WaveId == grp.Id).ToList();
            Assert.AreEqual(2, wave.Count);
            b.Step(50);
            foreach (var u in wave) Assert.AreEqual(UnitActivity.Marching, u.Activity);
        }

        [Test]
        public void Waves_Stop_WhenTheEnemyFactoriesAreDestroyed()
        {
            var b = new Bed(3, waves: true);
            b.Do(new RemoveBuilding(Faction.Enemy, b.EnemyKind(BuildingKinds.Barracks).Id));
            b.Do(new RemoveBuilding(Faction.Enemy, b.EnemyKind(BuildingKinds.VehicleWorkshop).Id));
            b.Step(MilitaryTuning.FirstWaveTicks);
            Assert.IsTrue(b.M.WavesStopped);
            Assert.AreEqual(0, b.M.WaveNumber);
            Assert.AreEqual(-1, b.M.TicksToNextWave(b.S.Tick));
            b.Step(2 * MilitaryTuning.WaveIntervalTicks);
            Assert.AreEqual(0, b.M.Units.Count);
            Assert.AreEqual(1, b.Events<WavesStopped>().Count());
        }

        [Test]
        public void OneFactoryLeft_StillSendsItsOwnKind()
        {
            var b = new Bed(3, waves: true);
            b.Do(new RemoveBuilding(Faction.Enemy, b.EnemyKind(BuildingKinds.VehicleWorkshop).Id));
            b.M.BuildingById(b.PlayerHq.Id).Health = b.M.BuildingById(b.PlayerHq.Id).MaxHealth = 100000000;
            b.Step(MilitaryTuning.FirstWaveTicks + 2 * MilitaryTuning.WaveIntervalTicks);
            Assert.AreEqual(3, b.M.WaveNumber);
            Assert.AreEqual(0, b.Events<UnitSpawned>().Count(e => e.Kind == UnitKind.Vehicle));
        }
    }

    public class MilitaryGameTests
    {
        [Test]
        public void Battle_SixVehiclesMarkedOnTheEnemyHq_WinTheGame()
        {
            var b = new Bed(3, waves: true);
            for (int i = 0; i < 6; i++) b.Spawn(Faction.Player, UnitKind.Vehicle, b.PlayerHq.Node);
            Assert.IsTrue(b.Do(new MarkTarget(b.EnemyHq.Id)).Ok);
            Assert.IsTrue(b.RunUntil(9000, () => b.M.IsGameOver), "the game ends");
            Assert.AreEqual(GameOutcome.Won, b.M.Outcome);
            var over = b.Events<GameOver>().Single();
            Assert.IsTrue(over.Won);
            Assert.AreEqual(0, b.T.Hqs.Count(h => h.Faction == Faction.Enemy));
            Assert.AreEqual(1, b.Events<BuildingDestroyed>().Count(e => e.IsHq && e.Faction == Faction.Enemy));
            Assert.Greater(b.M.GameOverTick, 0);

            // Over: waves, orders and units stop.
            int wave = b.M.WaveNumber; int units = b.M.Units.Count;
            b.Step(2000);
            Assert.AreEqual(wave, b.M.WaveNumber);
            Assert.AreEqual(units, b.M.Units.Count);
            Assert.IsFalse(b.Sim.Submit(new MarkTarget(1)).Ok);
            Assert.IsFalse(b.Sim.Submit(SetDefend.OnBuilding(b.PlayerHq.Id, 1)).Ok);
            Assert.IsFalse(b.Sim.Submit(new DevSpawnUnit(Faction.Player, UnitKind.Infantry, b.PlayerHq.Node)).Ok);
            Assert.AreEqual(1, b.Events<GameOver>().Count());
        }

        [Test]
        public void Wave_DestroysAnUndefendedPlayerHq_AndTheGameIsLost()
        {
            var b = new Bed(3, waves: true);
            Assert.IsTrue(b.RunUntil(20000, () => b.M.IsGameOver), "the player loses");
            Assert.AreEqual(GameOutcome.Lost, b.M.Outcome);
            Assert.IsFalse(b.Events<GameOver>().Single().Won);
            Assert.AreEqual(0, b.T.Hqs.Count(h => h.Faction == Faction.Player));
            Assert.IsTrue(b.M.Rubbles.Any(r => r.Size == BuildingSize.Hq));
            Assert.Greater(b.M.WaveNumber, 0);
            int n = b.M.WaveNumber;
            b.Step(2000);
            Assert.AreEqual(n, b.M.WaveNumber, "waves stop at game over");
        }

        private static ulong Play(ulong seed, int ticks)
        {
            var b = new Bed(seed, waves: true);
            for (int i = 0; i < 3; i++) b.Spawn(Faction.Player, UnitKind.Infantry, b.PlayerHq.Node);
            for (int i = 0; i < 2; i++) b.Spawn(Faction.Player, UnitKind.Vehicle, b.PlayerHq.Node);
            b.Do(SetDefend.OnBuilding(b.PlayerHq.Id, 2));
            b.Do(new MarkTarget(b.EnemyKind(BuildingKinds.Barracks).Id));
            b.Do(new MarkTarget(b.EnemyHq.Id));
            b.Step(ticks);
            return b.M.Hash(b.S);
        }

        [Test]
        public void SameSeed_SameHash_AndOtherSeedsDiffer()
        {
            Assert.AreEqual(Play(7, 2500), Play(7, 2500));
            Assert.AreEqual(Play(21, 1500), Play(21, 1500));
            Assert.AreNotEqual(Play(7, 2500), Play(8, 2500));
            Assert.AreNotEqual(Play(7, 1000), Play(7, 1001));
        }
    }
}
