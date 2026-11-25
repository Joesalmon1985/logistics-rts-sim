using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>
    /// Shared helpers of the military tests. F0 plays the old player and F1 the old enemy; unless <c>war</c> is false they
    /// declare war on each other first (one tick). The other factions stay at peace with both.
    /// </summary>
    internal sealed class Bed
    {
        public readonly GodSim Sim;
        public readonly GodState S;
        public readonly TerritoryState T;
        public readonly NodeGrid G;
        public readonly MilitaryState M;
        public readonly List<SimEvent> Log = new List<SimEvent>();
        public readonly Hq PlayerHq, EnemyHq;

        public Bed(ulong seed = 3, bool war = true, int factions = FactionInfo.Max)
        {
            Sim = GodSim.Create(new GameSetup(seed, 0, factions));
            S = Sim.State; T = S.Territory; G = T.Grid; M = S.Military;
            PlayerHq = T.HqsOf(Faction.F0).Single();
            EnemyHq = T.HqsOf(Faction.F1).Single();
            if (war) Assert.IsTrue(Do(new DeclareWar(Faction.F0, Faction.F1)).Ok);
        }

        private readonly List<Building> _enemySmall = new List<Building>();

        /// <summary>
        /// The i-th small finished F1 building beside the F1 HQ (placed on first use, at least 3 steps from the others), so
        /// tests have enemy buildings besides the HQ now that there is no pre-placed enemy base.
        /// </summary>
        public Building EnemySmall(int i)
        {
            while (_enemySmall.Count <= i)
            {
                int pick = -1;
                foreach (int n in T.BuildableNodes(Faction.F1))
                {
                    if (G.Distance(n, EnemyHq.Node) < 3 || _enemySmall.Any(x => G.Distance(x.Node, n) < 3)) continue;
                    if (!Sim.Check(new PlaceBuilding(Faction.F1, n, BuildingSize.Small)).Ok) continue;
                    pick = n; break;
                }
                Assert.GreaterOrEqual(pick, 0, "room for an enemy building");
                Assert.IsTrue(Do(new PlaceBuilding(Faction.F1, pick, BuildingSize.Small)).Ok);
                _enemySmall.Add(T.BuildingAtNode(pick));
            }
            return _enemySmall[i];
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

        /// <summary>Spawns a soldier wearing a kit.</summary>
        public MilUnit Spawn(Faction f, Kit kit, int node)
        {
            int before = M.NextUnitId;
            Assert.IsTrue(Do(DevSpawnUnit.WithKit(f, kit, node)).Ok);
            return M.UnitById(before);
        }

        public int Node(Hq hq, int di, int dj) => G.Id(G.I(hq.Node) + di, G.J(hq.Node) + dj);

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
        public void BuildingHealth_StartsFull_ByHqAndSize_AndNewBuildingsAreRegistered()
        {
            var b = new Bed();
            Assert.AreEqual(3000, b.M.BuildingHealth(b.EnemyHq.Id));
            Assert.AreEqual(3000, b.M.BuildingHealth(b.PlayerHq.Id));
            foreach (var h in b.T.Hqs) Assert.AreEqual(3000, b.M.BuildingHealth(h.Id), "every faction HQ");
            Assert.AreEqual(6, b.T.Buildings.Count, "only the six HQs at the start");

            int node = b.Node(b.PlayerHq, -3, 0);
            Assert.IsTrue(b.Do(new PlaceBuilding(Faction.F0, node, BuildingSize.Small)).Ok);
            var nb = b.T.BuildingAtNode(node);
            Assert.AreEqual(400, b.M.BuildingHealth(nb.Id));
            Assert.AreEqual(1f, b.M.BuildingHealthFraction(nb.Id));
            Assert.IsTrue(b.Do(new RemoveBuilding(Faction.F0, nb.Id)).Ok);
            b.Step();
            Assert.AreEqual(-1, b.M.BuildingHealth(nb.Id), "record dropped with the building");
            foreach (int n in b.T.BuildableNodes(Faction.F0))
                if (b.Sim.Check(new PlaceBuilding(Faction.F0, n, BuildingSize.Medium)).Ok)
                {
                    Assert.IsTrue(b.Do(new PlaceBuilding(Faction.F0, n, BuildingSize.Medium)).Ok);
                    Assert.AreEqual(700, b.M.BuildingHealth(b.T.BuildingAtNode(n).Id));
                    break;
                }
        }
    }

    public class MilitaryMovementTests
    {
        [Test]
        public void Unit_WalksToAPoint_AtItsSpeed_AndStopsThere()
        {
            var b = new Bed();
            var (start, dir) = b.LandLine(b.PlayerHq, 8);
            var u = b.Spawn(Faction.F0, UnitKind.Infantry, start);
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
        public void HeavyKit_IsSlower_ThanABareSoldier()
        {
            var b = new Bed();
            var (start, dir) = b.LandLine(b.PlayerHq, 8);
            var bare = b.Spawn(Faction.F0, new Kit(Ware.Sword), start);
            var heavy = b.Spawn(Faction.F0, new Kit(Ware.Sword, Ware.BigShield, Ware.None, Ware.Breastplate), start);
            Assert.AreEqual(300, MilitaryTuning.Of(bare.Kit).SpeedMmPerTick);
            Assert.AreEqual(225, MilitaryTuning.Of(heavy.Kit).SpeedMmPerTick, "big shield -10%, breastplate -15%");
            int gx = Pathing.NodeMmX(b.G, b.Walk(start, dir, 8)), gz = Pathing.NodeMmZ(b.G, b.Walk(start, dir, 8));
            b.Do(new DevMoveUnit(bare.Id, gx, gz)); b.Do(new DevMoveUnit(heavy.Id, gx, gz));
            b.Step(20);
            Assert.Less(Pathing.Dist(bare.X, bare.Z, gx, gz), Pathing.Dist(heavy.X, heavy.Z, gx, gz), "the bare soldier is closer to the goal");
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
            var u = b.Spawn(Faction.F0, UnitKind.Infantry, a);
            int gx = Pathing.NodeMmX(b.G, c), gz = Pathing.NodeMmZ(b.G, c);
            int straight = Pathing.Dist(u.X, u.Z, gx, gz);
            Assert.IsTrue(b.Do(new DevMoveUnit(u.Id, gx, gz)).Ok);
            long travelled = 0; int px = u.X, pz = u.Z;
            for (int i = 0; i < 4000 && u.HasGoal; i++)
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
        public void Fight_StrikesOnCooldown_DamageMatchesTheOutcome_AndEndsInDeath()
        {
            var b = new Bed();
            var (start, dir) = b.LandLine(b.PlayerHq, 3);
            var a = b.Spawn(Faction.F1, new Kit(Ware.Sword), start);
            var c = b.Spawn(Faction.F0, new Kit(Ware.Sword), b.Walk(start, dir, 3));
            Assert.IsTrue(b.RunUntil(1500, () => !b.M.Units.Contains(a) || !b.M.Units.Contains(c)), "one of them dies");

            var shots = b.Events<UnitFired>().ToList();
            Assert.Greater(shots.Count, 8);
            foreach (var e in shots)
            {
                Assert.AreEqual(Ware.Sword, e.Weapon); Assert.AreEqual(TargetKind.Unit, e.TargetKind); Assert.IsFalse(e.Ranged);
                switch (e.Outcome)
                {
                    case AttackOutcome.Miss: case AttackOutcome.Blocked: Assert.AreEqual(0, e.Damage, "a sword blocked or missed does nothing"); break;
                    case AttackOutcome.Hit: Assert.AreEqual(11, e.Damage); break;
                    case AttackOutcome.Crit: Assert.AreEqual(22, e.Damage); break;
                    default: Assert.Fail("outcome " + e.Outcome); break;
                }
                Assert.LessOrEqual(Pathing.Dist(e.FromX, e.FromZ, e.ToX, e.ToZ), 2000, "melee: closed to 2 m");
            }
            foreach (var who in new[] { a, c })
            {
                var mine = shots.Where(e => e.AttackerId == who.Id).ToList();
                for (int i = 1; i < mine.Count; i++) Assert.AreEqual(9, mine[i].Tick - mine[i - 1].Tick, "9 ticks between sword blows");
            }
            Assert.IsTrue(shots.Any(e => e.Outcome == AttackOutcome.Blocked), "a sword without a shield parries now and then");
            var dead = b.Events<UnitDied>().Single();
            var alive = dead.UnitId == a.Id ? c : a;
            Assert.AreEqual(alive.Id, dead.KillerId);
            Assert.AreEqual(100 - shots.Where(e => e.TargetId == alive.Id).Sum(e => e.Damage), alive.Health);
        }

        [Test]
        public void TurnOrder_LongerReachFirst_ThenLowerId_AndTheDeadDoNotStrike()
        {
            for (ulong seed = 1; seed <= 6; seed++)
            {
                var b = new Bed(seed);
                var (start, _) = b.LandLine(b.PlayerHq, 1);
                int id0 = b.M.NextUnitId;
                // All four arrive in one tick: sword (lowest id, shortest reach), sling (longest reach), two spears.
                Assert.IsTrue(b.Sim.Submit(DevSpawnUnit.WithKit(Faction.F0, new Kit(Ware.Sword), start)).Ok);
                Assert.IsTrue(b.Sim.Submit(DevSpawnUnit.WithKit(Faction.F1, new Kit(Ware.Sling), start)).Ok);
                Assert.IsTrue(b.Sim.Submit(DevSpawnUnit.WithKit(Faction.F0, new Kit(Ware.Spear), start)).Ok);
                Assert.IsTrue(b.Sim.Submit(DevSpawnUnit.WithKit(Faction.F1, new Kit(Ware.Spear), start)).Ok);
                b.Log.Clear();
                b.Step();
                var sword = b.M.UnitById(id0); var sling = b.M.UnitById(id0 + 1); var spear = b.M.UnitById(id0 + 2); var spear2 = b.M.UnitById(id0 + 3);
                Assert.AreEqual(Ware.Sling, sling.Kit.Weapon); Assert.AreEqual(Ware.Spear, spear2.Kit.Weapon);
                var order = b.Events<UnitFired>().Select(e => e.AttackerId).ToList();
                CollectionAssert.AreEqual(new[] { sling.Id, spear.Id, spear2.Id, sword.Id }, order, "seed " + seed + ": sling 18 m, spears 3.5 m (lower id first), sword 2 m");
            }
            for (ulong seed = 1; seed <= 12; seed++)
            {
                var b = new Bed(seed);
                var (start, _) = b.LandLine(b.PlayerHq, 1);
                var x = b.Spawn(Faction.F0, new Kit(Ware.Axe), start);
                var y = b.Spawn(Faction.F1, new Kit(Ware.Axe), start);
                x.Health = 1; y.Health = 1;
                Assert.IsTrue(b.RunUntil(300, () => b.Events<UnitDied>().Any()));
                Assert.AreEqual(1, b.Events<UnitDied>().Count(), "seed " + seed + ": never both in one tick");
            }
        }

        [Test]
        public void Units_PreferEnemyUnits_ToBuildings()
        {
            var b = new Bed();
            var eb = b.EnemySmall(0);
            int ebX = Pathing.NodeMmX(b.G, eb.Node);
            var rec = b.M.BuildingById(eb.Id);
            var mine = b.Spawn(Faction.F0, new Kit(Ware.Bow), b.Node(b.EnemyHq, 0, 0));
            var inf = b.Spawn(Faction.F1, UnitKind.Infantry, b.Node(b.EnemyHq, 0, 0));
            b.Step();
            Assert.AreEqual(TargetKind.Unit, mine.TargetKind, "enemy unit before any building");
            Assert.AreEqual(inf.Id, mine.TargetId);
            Assert.AreEqual(ebX, rec.X);
        }

        [Test]
        public void Building_IsDestroyed_LeavesRubble30s_AndTheNodeCanBeBuiltOnAgain()
        {
            var b = new Bed();
            var target = b.EnemySmall(0);
            int node = target.Node;
            b.Spawn(Faction.F0, new Kit(Ware.Axe), b.G.FlagNodeOf(node));
            b.Spawn(Faction.F0, new Kit(Ware.Axe), b.G.FlagNodeOf(node));
            // They take whatever enemy building is nearest; hold the others up so only the woodcutter dies first.
            foreach (var r in b.M.Buildings) if (r.Id != target.Id) r.Health = r.MaxHealth = 1000000;
            Assert.IsTrue(b.RunUntil(600, () => b.T.BuildingById(target.Id) == null), "the woodcutter falls");
            foreach (var e in b.Events<UnitFired>()) { Assert.AreEqual(AttackOutcome.Building, e.Outcome); Assert.AreEqual(38, e.Damage, "axe: 19 x 200% on buildings"); }

            var d = b.Events<BuildingDestroyed>().Single();
            Assert.AreEqual(target.Id, d.BuildingId); Assert.AreEqual(node, d.Node); Assert.IsFalse(d.IsHq);
            Assert.AreEqual(1, b.M.Rubbles.Count);
            var rub = b.M.Rubbles[0];
            Assert.AreEqual(node, rub.Node); Assert.AreEqual(target.Kind, rub.Kind);
            Assert.That(rub.TicksLeft, Is.InRange(290, 300));
            Assert.AreEqual(0, b.T.NodeBuilding[node], "the node is free");
            Assert.IsTrue(b.Sim.Check(new PlaceBuilding(Faction.F1, node, BuildingSize.Small)).Ok, "and can be built on");
            Assert.AreEqual(-1, b.M.BuildingHealth(target.Id));

            b.Step(300);
            Assert.AreEqual(0, b.M.Rubbles.Count, "rubble gone after 30 s");
            Assert.AreEqual(1, b.Events<RubbleExpired>().Count());
        }
    }

    public class MilitarySpawnTests
    {
        [Test]
        public void UnitQueue_IsDrained_AndTheSoldierWearsItsKit()
        {
            var b = new Bed();
            int node = b.Node(b.PlayerHq, -3, 0);
            Assert.IsTrue(b.Do(new PlaceBuilding(Faction.F0, node, BuildingSize.Medium) { Kind = BuildingKinds.Barracks }).Ok);
            var fac = b.T.BuildingAtNode(node);
            var kit = new Kit(Ware.Spear, Ware.SmallShield, Ware.Helmet, Ware.LightArmour);
            b.S.UnitQueue.Add(new UnitOrder { Faction = Faction.F0, BuildingId = fac.Id, Kind = UnitKind.Infantry, Kit = kit });
            b.S.UnitQueue.Add(new UnitOrder { Faction = Faction.F0, BuildingId = fac.Id, Kind = UnitKind.Infantry });
            b.S.UnitQueue.Add(new UnitOrder { Faction = Faction.F0, BuildingId = fac.Id, Kind = UnitKind.Infantry, Kit = new Kit(Ware.Bow, Ware.BigShield, Ware.Helmet) });
            b.Step();
            Assert.AreEqual(0, b.S.UnitQueue.Count);
            Assert.AreEqual(3, b.M.Units.Count);
            Assert.AreEqual(kit, b.M.Units[0].Kit);
            Assert.AreEqual(new Kit(Ware.Sword), b.M.Units[1].Kit, "a stage 2 order (no weapon) fights as a sword with no defence");
            Assert.AreEqual(new Kit(Ware.Bow, Ware.None, Ware.Helmet), b.M.Units[2].Kit, "a bow takes no shield");
            foreach (var u in b.M.Units) Assert.AreEqual(100, u.Health);
            int fx = Pathing.NodeMmX(b.G, fac.FlagNode), fz = Pathing.NodeMmZ(b.G, fac.FlagNode);
            foreach (var u in b.M.Units) Assert.Less(Pathing.Dist(u.X, u.Z, fx, fz), 3500, "spawned at the factory flag");
            var spawned = b.Events<UnitSpawned>().ToList();
            Assert.AreEqual(3, spawned.Count);
            Assert.AreEqual(kit, spawned[0].Kit);
        }

        [Test]
        public void UnitQueue_WhenTheFactoryIsGone_SpawnsAtTheNearestHq()
        {
            var b = new Bed();
            int node = b.Node(b.PlayerHq, -3, 0);
            b.Do(new PlaceBuilding(Faction.F0, node, BuildingSize.Medium) { Kind = BuildingKinds.Barracks });
            var fac = b.T.BuildingAtNode(node);
            b.Step();
            Assert.IsTrue(b.Do(new RemoveBuilding(Faction.F0, fac.Id)).Ok);
            b.S.UnitQueue.Add(new UnitOrder { Faction = Faction.F0, BuildingId = fac.Id, Kind = UnitKind.Infantry });
            b.Step();
            Assert.AreEqual(1, b.M.Units.Count);
            var hqFlag = b.T.BuildingById(b.PlayerHq.Id).FlagNode;
            Assert.Less(Pathing.Dist(b.M.Units[0].X, b.M.Units[0].Z, Pathing.NodeMmX(b.G, hqFlag), Pathing.NodeMmZ(b.G, hqFlag)), 3500);
        }

        [Test]
        public void UnitQueue_WhenTheFactoryIsGoneAndTheFactionHasNoHq_IsDropped()
        {
            var b = new Bed();
            b.S.UnitQueue.Add(new UnitOrder { Faction = Faction.F0, BuildingId = 9999, Kind = UnitKind.Infantry });
            MilitaryTerritory.Remove(b.S, b.PlayerHq.Id, null);
            b.Step();
            Assert.AreEqual(0, b.M.Units.Count);
            Assert.AreEqual(0, b.S.UnitQueue.Count);
        }

        [Test]
        public void DevSpawn_NeedsLand()
        {
            var b = new Bed();
            Assert.IsFalse(b.Sim.Submit(new DevSpawnUnit(Faction.F0, UnitKind.Infantry, 0)).Ok);
            Assert.IsTrue(b.Sim.Submit(new DevSpawnUnit(Faction.F0, UnitKind.Infantry, b.PlayerHq.Node)).Ok);
        }
    }

    public class MilitaryOrdersTests
    {
        [Test]
        public void MarkTarget_KeepsMarkOrder_AndValidates()
        {
            var b = new Bed();
            var w = b.EnemySmall(0); var q = b.EnemySmall(1);
            var F0 = Faction.F0;
            Assert.IsFalse(b.Sim.Submit(new MarkTarget(F0, b.PlayerHq.Id)).Ok, "not an enemy");
            Assert.IsFalse(b.Sim.Submit(new MarkTarget(F0, 99999)).Ok);
            Assert.IsTrue(b.Do(new MarkTarget(F0, q.Id)).Ok);
            Assert.IsTrue(b.Do(new MarkTarget(F0, w.Id)).Ok);
            Assert.IsFalse(b.Sim.Submit(new MarkTarget(F0, q.Id)).Ok, "twice");
            Assert.IsTrue(b.Do(new MarkTarget(F0, b.EnemyHq.Id)).Ok);
            CollectionAssert.AreEqual(new[] { q.Id, w.Id, b.EnemyHq.Id }, b.M.MarkedOf(F0));
            Assert.AreEqual(2, b.M.MarkOrder(F0, w.Id));
            Assert.IsTrue(b.Do(new UnmarkTarget(F0, w.Id)).Ok);
            CollectionAssert.AreEqual(new[] { q.Id, b.EnemyHq.Id }, b.M.MarkedOf(F0));
            Assert.IsFalse(b.Sim.Submit(new UnmarkTarget(F0, w.Id)).Ok);
            Assert.AreEqual(0, b.M.MarkedOf(Faction.F1).Count, "marks are per faction");
        }

        [Test]
        public void Targets_AreAttackedInTheOrderMarked()
        {
            var b = new Bed();
            var first = b.EnemySmall(1);
            var second = b.EnemySmall(0);
            b.Do(new MarkTarget(Faction.F0, first.Id)); b.Do(new MarkTarget(Faction.F0, second.Id));
            // Next to target 1, so it is in sight from the start (an unseen mark would let them shoot the HQ instead).
            for (int i = 0; i < 4; i++) b.Spawn(Faction.F0, new Kit(Ware.Axe), first.FlagNode);
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
            Assert.AreEqual(0, b.M.MarkedOf(Faction.F0).Count, "destroyed targets leave the list");
        }

        [Test]
        public void Defend_Validates_AndUpdatesAnExistingFlag()
        {
            var b = new Bed();
            Assert.IsFalse(b.Sim.Submit(SetDefend.OnBuilding(Faction.F0, b.PlayerHq.Id, 6)).Ok, "0 to 5 guards");
            Assert.IsFalse(b.Sim.Submit(SetDefend.OnBuilding(Faction.F0, b.PlayerHq.Id, -1)).Ok);
            Assert.IsFalse(b.Sim.Submit(SetDefend.OnBuilding(Faction.F0, b.EnemyHq.Id, 1)).Ok, "own buildings only");
            Assert.IsFalse(b.Sim.Submit(SetDefend.OnNode(Faction.F0, 0, 1)).Ok, "land only");
            Assert.IsTrue(b.Do(SetDefend.OnBuilding(Faction.F0, b.PlayerHq.Id, 2)).Ok);
            Assert.IsTrue(b.Do(SetDefend.OnNode(Faction.F0, b.Node(b.PlayerHq, -3, 0), 1)).Ok);
            Assert.AreEqual(2, b.M.Defends.Count);
            Assert.IsTrue(b.Do(SetDefend.OnBuilding(Faction.F0, b.PlayerHq.Id, 4)).Ok);
            Assert.AreEqual(2, b.M.Defends.Count, "same anchor, same flag");
            Assert.AreEqual(4, b.M.DefendAtBuilding(b.PlayerHq.Id).Guards);
            var flag = b.M.DefendAtNode(Faction.F0, b.Node(b.PlayerHq, -3, 0));
            Assert.IsTrue(b.Do(new RemoveDefend(Faction.F0, flag.Id)).Ok);
            Assert.IsFalse(b.Sim.Submit(new RemoveDefend(Faction.F0, flag.Id)).Ok);
            Assert.AreEqual(1, b.M.Defends.Count);
        }

        [Test]
        public void DefendFlag_GoesWhenItsBuildingDoes()
        {
            var b = new Bed();
            int node = b.Node(b.PlayerHq, -3, 0);
            b.Do(new PlaceBuilding(Faction.F0, node, BuildingSize.Small));
            var bl = b.T.BuildingAtNode(node);
            b.Do(SetDefend.OnBuilding(Faction.F0, bl.Id, 1));
            Assert.AreEqual(1, b.M.Defends.Count);
            b.Do(new RemoveBuilding(Faction.F0, bl.Id));
            b.Step();
            Assert.AreEqual(0, b.M.Defends.Count);
            Assert.AreEqual(1, b.Events<DefendRemoved>().Count(e => e.AnchorLost));
        }

        [Test]
        public void GuardCounts_AreFilledBeforeAnythingAttacks()
        {
            var b = new Bed();
            b.Do(new MarkTarget(Faction.F0, b.EnemySmall(0).Id));
            var u1 = b.Spawn(Faction.F0, UnitKind.Infantry, b.Node(b.PlayerHq, 0, 0));
            var u2 = b.Spawn(Faction.F0, UnitKind.Infantry, b.Node(b.PlayerHq, 0, 1));
            b.Do(SetDefend.OnBuilding(Faction.F0, b.PlayerHq.Id, 2));
            b.Step(3);
            var flag = b.M.DefendAtBuilding(b.PlayerHq.Id);
            Assert.AreEqual(2, b.M.GuardCount(flag.Id), "both units guard");
            Assert.AreNotEqual(UnitActivity.Marching, u1.Activity);
            Assert.AreNotEqual(UnitActivity.Marching, u2.Activity);

            var u3 = b.Spawn(Faction.F0, UnitKind.Infantry, b.Node(b.PlayerHq, 1, 0));
            b.Step(3);
            Assert.AreEqual(2, b.M.GuardCount(flag.Id));
            Assert.AreEqual(0, u3.GuardFlagId);
            Assert.AreEqual(UnitActivity.Marching, u3.Activity, "the rest march on the marked target");

            b.Do(SetDefend.OnBuilding(Faction.F0, b.PlayerHq.Id, 3));
            b.Step(3);
            Assert.AreEqual(3, b.M.GuardCount(flag.Id), "a free unit joins a flag that grew");
            Assert.AreNotEqual(UnitActivity.Marching, u3.Activity);
            b.Do(SetDefend.OnBuilding(Faction.F0, b.PlayerHq.Id, 1));
            b.Step(3);
            Assert.AreEqual(1, b.M.GuardCount(flag.Id), "extra guards are released");
        }

        [Test]
        public void OldestFlagIsFilledFirst_ByTheNearestFreeUnit()
        {
            var b = new Bed();
            var far = b.Spawn(Faction.F0, UnitKind.Infantry, b.Node(b.PlayerHq, 0, 0));
            int n1 = b.Node(b.PlayerHq, -4, 0);
            b.Do(SetDefend.OnNode(Faction.F0, n1, 1));           // oldest
            b.Do(SetDefend.OnBuilding(Faction.F0, b.PlayerHq.Id, 1));
            b.Step(2);
            Assert.AreEqual(b.M.DefendAtNode(Faction.F0, n1).Id, far.GuardFlagId, "the only unit goes to the oldest flag");
            var near = b.Spawn(Faction.F0, UnitKind.Infantry, b.Node(b.PlayerHq, 0, 0));
            b.Step(2);
            Assert.AreEqual(b.M.DefendAtBuilding(b.PlayerHq.Id).Id, near.GuardFlagId, "the next unit fills the next flag");
        }

        [Test]
        public void WithNoTarget_FreeUnitsWaitNearTheirHq_AndStayPut()
        {
            var b = new Bed();
            var u = b.Spawn(Faction.F0, UnitKind.Infantry, b.Node(b.PlayerHq, 0, 0));
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
            var g1 = b.Spawn(Faction.F0, UnitKind.Infantry, b.PlayerHq.Node);
            b.Do(SetDefend.OnBuilding(Faction.F0, b.PlayerHq.Id, 1));
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
            var g1 = b.Spawn(Faction.F0, UnitKind.Infantry, b.PlayerHq.Node);
            var g2 = b.Spawn(Faction.F0, UnitKind.Infantry, b.PlayerHq.Node);
            b.Do(SetDefend.OnBuilding(Faction.F0, b.PlayerHq.Id, 2));
            b.Step(60);
            var anchor = b.M.Defends[0];
            // An enemy soldier about 14 m from the anchor, beyond a guard's reach.
            int en = -1;
            for (int d = 0; d < 6 && en < 0; d++)
            {
                int m = b.PlayerHq.Node; bool ok = true;
                for (int k = 0; k < 3; k++) { m = b.G.Neighbour(m, d); if (m < 0 || !b.G.IsLand(m)) { ok = false; break; } }
                if (ok) { en = m; }
            }
            Assert.GreaterOrEqual(en, 0);
            var enemy = b.Spawn(Faction.F1, UnitKind.Infantry, en);
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

    public class MilitaryGameTests
    {
        private static ulong Play(ulong seed, int ticks)
        {
            var b = new Bed(seed);
            for (int i = 0; i < 3; i++) b.Spawn(Faction.F0, UnitKind.Infantry, b.PlayerHq.Node);
            b.Spawn(Faction.F0, new Kit(Ware.Axe, Ware.SmallShield, Ware.Helmet), b.PlayerHq.Node);
            b.Spawn(Faction.F0, new Kit(Ware.Bow, Ware.None, Ware.None, Ware.LightArmour), b.PlayerHq.Node);
            b.Spawn(Faction.F1, new Kit(Ware.Spear, Ware.BigShield), b.EnemyHq.Node);
            b.Spawn(Faction.F1, new Kit(Ware.Sling), b.EnemyHq.Node);
            b.Do(SetDefend.OnBuilding(Faction.F0, b.PlayerHq.Id, 2));
            b.Do(new MarkTarget(Faction.F0, b.EnemySmall(0).Id));
            b.Do(new MarkTarget(Faction.F0, b.EnemyHq.Id));
            b.Step(ticks);
            return b.Sim.StateHash();
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
