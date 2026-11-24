using System.Collections.Generic;

namespace God.Sim
{
    /// <summary>Removing an HQ through territory (P1 has no such function: <c>RemoveBuilding</c> refuses HQs). Uses only the public state.</summary>
    public static class MilitaryTerritory
    {
        /// <summary>Removes any building or HQ: frees its node, drops its flag when nothing else uses it, emits <see cref="BuildingRemoved"/>.</summary>
        public static bool Remove(GodState s, int buildingId, EventBuffer ev)
        {
            var t = s.Territory;
            var b = t.BuildingById(buildingId);
            if (b == null) return false;
            if (b.Size != BuildingSize.Hq)
                return TerritoryRules.DoRemoveBuilding(s, b.Faction, buildingId, ev).Ok;

            var hq = t.HqOf(buildingId);
            if (hq != null) t.Hqs.Remove(hq);
            t.NodeBuilding[b.Node] = 0;
            t.Buildings.Remove(b);
            var flag = t.FlagById(b.FlagId);
            if (flag != null)
            {
                flag.BuildingId = 0;
                bool hasRoad = false;
                foreach (var r in t.Roads) if (r.FlagA == flag.Id || r.FlagB == flag.Id) { hasRoad = true; break; }
                if (!hasRoad) { t.NodeFlag[flag.Node] = 0; t.Flags.Remove(flag); }
            }
            ev?.Add(new BuildingRemoved { Faction = b.Faction, BuildingId = buildingId, Node = b.Node });
            return true;
        }
    }

    /// <summary>Spawning, building health, destruction, rubble, waves and the enemy base (P3 rules).</summary>
    public static class MilitaryRules
    {
        // ---------------- spawning ----------------

        public static MilUnit SpawnUnit(GodState s, Faction f, UnitKind kind, int x, int z, int waveId, EventBuffer ev)
        {
            var m = s.Military;
            var st = MilitaryTuning.Of(kind);
            var u = new MilUnit
            {
                Id = m.NextUnitId++, Faction = f, Kind = kind, X = x, Z = z,
                Health = st.MaxHealth, MaxHealth = st.MaxHealth, WaveId = waveId,
                FaceX = f == Faction.Player ? 1 : -1, FaceZ = 0,
            };
            m.Units.Add(u);
            ev?.Add(new UnitSpawned { UnitId = u.Id, Faction = f, Kind = kind, X = x, Z = z, WaveId = waveId });
            return u;
        }

        /// <summary>Spawns at a node with a small id-based spread so units do not stack.</summary>
        private static MilUnit SpawnAtNode(GodState s, Faction f, UnitKind kind, int node, int waveId, EventBuffer ev)
        {
            var g = s.Territory.Grid;
            int x = Pathing.NodeMmX(g, node), z = Pathing.NodeMmZ(g, node);
            int k = s.Military.NextUnitId % MilitaryTuning.SpawnSpreadX.Length;
            int sx = x + MilitaryTuning.SpawnSpreadX[k], sz = z + MilitaryTuning.SpawnSpreadZ[k];
            if (Pathing.IsLandPoint(g, sx, sz)) { x = sx; z = sz; }
            return SpawnUnit(s, f, kind, x, z, waveId, ev);
        }

        /// <summary>
        /// Drains <see cref="GodState.UnitQueue"/>: each order spawns at its factory's flag node. When the factory is gone the
        /// unit spawns at the faction's HQ nearest to where the factory last stood (its flag node); when the faction has no HQ
        /// either, the order is dropped.
        /// </summary>
        public static void DrainQueue(GodState s, EventBuffer ev)
        {
            if (s.UnitQueue.Count == 0) return;
            var t = s.Territory; var m = s.Military;
            var orders = new List<UnitOrder>(s.UnitQueue);
            s.UnitQueue.Clear();
            foreach (var o in orders)
            {
                var b = t.BuildingById(o.BuildingId);
                if (b != null) { SpawnAtNode(s, o.Faction, o.Kind, b.FlagNode, 0, ev); continue; }

                Hq best = null; long bestD = long.MaxValue;
                int refNode = m.Known.TryGetValue(o.BuildingId, out var k) ? k.node : -1;
                foreach (var h in t.Hqs)
                {
                    if (h.Faction != o.Faction) continue;
                    long d = refNode >= 0 ? t.Grid.Distance(h.Node, refNode) : 0;
                    if (best == null || d < bestD) { best = h; bestD = d; }
                }
                if (best == null) continue;
                SpawnAtNode(s, o.Faction, o.Kind, t.BuildingById(best.Id).FlagNode, 0, ev);
            }
        }

        // ---------------- building health ----------------

        /// <summary>Keeps one health record per building and HQ: new ones start full, vanished ones are dropped with their marks and flags.</summary>
        public static void SyncBuildings(GodState s, EventBuffer ev)
        {
            var t = s.Territory; var m = s.Military; var g = t.Grid;
            var old = new Dictionary<int, BuildingRecord>();
            foreach (var r in m.Buildings) old[r.Id] = r;
            var list = new List<BuildingRecord>(t.Buildings.Count);
            foreach (var b in t.Buildings)
            {
                if (!old.TryGetValue(b.Id, out var rec))
                {
                    int hp = MilitaryTuning.HealthOf(b.Size);
                    rec = new BuildingRecord
                    {
                        Id = b.Id, Faction = b.Faction, Node = b.Node, Size = b.Size, Kind = b.Kind, Health = hp, MaxHealth = hp,
                        X = Pathing.NodeMmX(g, b.Node), Z = Pathing.NodeMmZ(g, b.Node),
                    };
                    m.Known[b.Id] = (b.Faction, b.FlagNode);
                }
                list.Add(rec);
            }
            m.Buildings = list;
            m.IndexBuildings();

            for (int i = m.Marked.Count - 1; i >= 0; i--)
                if (m.BuildingById(m.Marked[i]) == null)
                {
                    ev.Add(new TargetUnmarked { BuildingId = m.Marked[i], Destroyed = true });
                    m.Marked.RemoveAt(i);
                }
            for (int i = m.Defends.Count - 1; i >= 0; i--)
                if (m.Defends[i].AnchorBuildingId != 0 && m.BuildingById(m.Defends[i].AnchorBuildingId) == null)
                    DropDefend(m, m.Defends[i].Id, true, ev);
        }

        public static void DropDefend(MilitaryState m, int flagId, bool anchorLost, EventBuffer ev)
        {
            var f = m.DefendById(flagId);
            if (f == null) return;
            m.Defends.Remove(f);
            foreach (var u in m.Units) if (u.GuardFlagId == flagId) { u.GuardFlagId = 0; u.Returning = false; }
            ev?.Add(new DefendRemoved { FlagId = flagId, AnchorLost = anchorLost });
        }

        /// <summary>
        /// Destroys a building or HQ: removes it through territory (the node is free at once), leaves rubble for
        /// <see cref="MilitaryTuning.RubbleTicks"/>, emits <see cref="BuildingDestroyed"/>.
        /// </summary>
        public static void Destroy(GodState s, BuildingRecord rec, EventBuffer ev)
        {
            var m = s.Military;
            MilitaryTerritory.Remove(s, rec.Id, ev);
            var rub = new Rubble
            {
                Id = m.NextRubbleId++, Faction = rec.Faction, Node = rec.Node, Size = rec.Size, Kind = rec.Kind,
                X = rec.X, Z = rec.Z, TicksLeft = MilitaryTuning.RubbleTicks,
            };
            m.Rubbles.Add(rub);
            ev.Add(new BuildingDestroyed
            {
                BuildingId = rec.Id, Faction = rec.Faction, Node = rec.Node, Size = rec.Size, Kind = rec.Kind, IsHq = rec.IsHq, RubbleId = rub.Id,
            });
            m.Buildings.Remove(rec);
            m.IndexBuildings();
            for (int i = m.Marked.Count - 1; i >= 0; i--)
                if (m.Marked[i] == rec.Id) { m.Marked.RemoveAt(i); ev.Add(new TargetUnmarked { BuildingId = rec.Id, Destroyed = true }); }
            for (int i = m.Defends.Count - 1; i >= 0; i--)
                if (m.Defends[i].AnchorBuildingId == rec.Id) DropDefend(m, m.Defends[i].Id, true, ev);
        }

        public static void TickRubble(GodState s, EventBuffer ev)
        {
            var m = s.Military; var t = s.Territory;
            for (int i = m.Rubbles.Count - 1; i >= 0; i--)
            {
                var r = m.Rubbles[i];
                r.TicksLeft--;
                // Something built on the plot clears its rubble at once.
                if (r.TicksLeft <= 0 || t.NodeBuilding[r.Node] != 0)
                {
                    m.Rubbles.RemoveAt(i);
                    ev.Add(new RubbleExpired { RubbleId = r.Id, Node = r.Node });
                }
            }
        }

        // ---------------- enemy base and waves ----------------

        private static readonly (string kind, BuildingSize size)[] EnemyBase =
        {
            (BuildingKinds.Barracks, BuildingSize.Medium),
            (BuildingKinds.VehicleWorkshop, BuildingSize.Medium),
            (BuildingKinds.Woodcutter, BuildingSize.Small),
            (BuildingKinds.Sawmill, BuildingSize.Medium),
            (BuildingKinds.Quarry, BuildingSize.Small),
        };

        /// <summary>
        /// Pre-places the enemy base round the enemy HQ with the P1 placement rules: nodes of the build circle at 3 to 6
        /// steps from the HQ, nearest to a 4-step ring first, each at least 3 steps from the others. A medium building that
        /// finds no room is tried as a small one.
        /// </summary>
        public static void PlaceEnemyBase(GodState s)
        {
            var t = s.Territory; var g = t.Grid;
            Hq hq = null;
            foreach (var h in t.Hqs) if (h.Faction == Faction.Enemy) { hq = h; break; }
            if (hq == null) return;
            var cand = new List<int>();
            foreach (int n in g.CircleNodes(hq.Node)) if (g.IsLand(n) && g.Distance(n, hq.Node) >= 3) cand.Add(n);
            cand.Sort((a, b) =>
            {
                int da = System.Math.Abs(g.Distance(a, hq.Node) - 4), db = System.Math.Abs(g.Distance(b, hq.Node) - 4);
                return da != db ? da.CompareTo(db) : a.CompareTo(b);
            });
            var placed = new List<int>();
            foreach (var (kind, size) in EnemyBase)
            {
                bool done = false;
                foreach (int gap in new[] { 3, 2 })
                {
                    foreach (var sz in size == BuildingSize.Medium ? new[] { BuildingSize.Medium, BuildingSize.Small } : new[] { BuildingSize.Small })
                    {
                        foreach (int n in cand)
                        {
                            bool far = true;
                            foreach (int p in placed) if (g.Distance(n, p) < gap) { far = false; break; }
                            if (!far || !TerritoryRules.CheckBuilding(s, Faction.Enemy, n, sz).Ok) continue;
                            TerritoryRules.DoPlaceBuilding(s, Faction.Enemy, n, sz, kind, null);
                            placed.Add(n); done = true; break;
                        }
                        if (done) break;
                    }
                    if (done) break;
                }
            }
        }

        public static List<Building> EnemyFactories(TerritoryState t, string kind)
        {
            var l = new List<Building>();
            foreach (var b in t.Buildings) if (b.Faction == Faction.Enemy && b.Kind == kind) l.Add(b);
            return l;
        }

        /// <summary>Infantry in wave n (1-based): 2, then one more each wave.</summary>
        public static int WaveInfantry(int n) => MilitaryTuning.FirstWaveInfantry + (n - 1);

        /// <summary>Vehicles in wave n: none before wave 3, then 1 in wave 3 and one more each wave.</summary>
        public static int WaveVehicles(int n) => n >= MilitaryTuning.FirstVehicleWave ? n - MilitaryTuning.FirstVehicleWave + 1 : 0;

        public static void TickWaves(GodState s, EventBuffer ev)
        {
            var m = s.Military; var t = s.Territory;
            if (m.WavesStopped || !m.WavesEnabled || s.Tick < m.NextWaveTick) return;
            var barracks = EnemyFactories(t, BuildingKinds.Barracks);
            var shops = EnemyFactories(t, BuildingKinds.VehicleWorkshop);
            if (barracks.Count == 0 && shops.Count == 0)
            {
                m.WavesStopped = true;
                ev.Add(new WavesStopped());
                return;
            }
            m.NextWaveTick += MilitaryTuning.WaveIntervalTicks;
            m.WaveNumber++;
            int n = m.WaveNumber;
            int inf = barracks.Count > 0 ? WaveInfantry(n) : 0;
            int veh = shops.Count > 0 ? WaveVehicles(n) : 0;

            var grp = new WaveGroup { Id = m.NextWaveGroupId++ };
            PickWaveTarget(s, grp, true);
            m.WaveGroups.Add(grp);
            for (int i = 0; i < inf; i++) SpawnAtNode(s, Faction.Enemy, UnitKind.Infantry, barracks[i % barracks.Count].FlagNode, grp.Id, ev);
            for (int i = 0; i < veh; i++) SpawnAtNode(s, Faction.Enemy, UnitKind.Vehicle, shops[i % shops.Count].FlagNode, grp.Id, ev);
            ev.Add(new WaveSpawned { Number = n, Infantry = inf, Vehicles = veh });
        }

        /// <summary>Picks the wave target: the player building, HQ or (first pick only) unit nearest to the enemy HQ. Ties: buildings first, then lowest id.</summary>
        public static void PickWaveTarget(GodState s, WaveGroup grp, bool includeUnits)
        {
            var m = s.Military;
            BuildingRecord home = null;
            foreach (var r in m.Buildings) if (r.IsHq && r.Faction == Faction.Enemy) { home = r; break; }
            grp.TargetKind = TargetKind.None; grp.TargetId = 0;
            if (home == null) return;
            long best = long.MaxValue;
            foreach (var r in m.Buildings)
            {
                if (r.Faction != Faction.Player) continue;
                long d = Pathing.Dist2(home.X, home.Z, r.X, r.Z);
                if (d < best) { best = d; grp.TargetKind = TargetKind.Building; grp.TargetId = r.Id; }
            }
            if (!includeUnits) return;
            foreach (var u in m.Units)
            {
                if (u.Faction != Faction.Player) continue;
                long d = Pathing.Dist2(home.X, home.Z, u.X, u.Z);
                if (d < best) { best = d; grp.TargetKind = TargetKind.Unit; grp.TargetId = u.Id; }
            }
        }

        /// <summary>Re-picks the target of every wave group whose target is gone: the nearest player building.</summary>
        public static void ResolveWaveTargets(GodState s)
        {
            var m = s.Military;
            foreach (var grp in m.WaveGroups)
            {
                bool ok = grp.TargetKind == TargetKind.Building ? m.BuildingById(grp.TargetId) != null
                        : grp.TargetKind == TargetKind.Unit && m.UnitById(grp.TargetId) != null;
                if (!ok) PickWaveTarget(s, grp, false);
            }
        }
    }
}
