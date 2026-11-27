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

    /// <summary>Spawning, building health, destruction and rubble (P3 rules; P7 removed the enemy base and its waves).</summary>
    public static class MilitaryRules
    {
        // ---------------- spawning ----------------

        /// <summary>Spawns a soldier with a kit (normalised: no weapon fights as a sword with no defence).</summary>
        public static MilUnit SpawnUnit(GodState s, Faction f, UnitKind kind, Kit kit, int x, int z, EventBuffer ev)
        {
            var m = s.Military;
            var k = MilitaryTuning.Normalize(kit);
            var st = s.Tuning.Military.Of(k);
            var u = new MilUnit
            {
                Id = m.NextUnitId++, Faction = f, Kind = kind, Kit = k, X = x, Z = z,
                Health = st.MaxHealth, MaxHealth = st.MaxHealth,
                FaceX = 1, FaceZ = 0,
            };
            m.Units.Add(u);
            ev?.Add(new UnitSpawned { UnitId = u.Id, Faction = f, Kind = kind, Kit = k, X = x, Z = z });
            return u;
        }

        /// <summary>Spawns a soldier without gear (a sword, no defence).</summary>
        public static MilUnit SpawnUnit(GodState s, Faction f, UnitKind kind, int x, int z, EventBuffer ev) => SpawnUnit(s, f, kind, default, x, z, ev);

        /// <summary>Spawns at a node with a small id-based spread so units do not stack.</summary>
        private static MilUnit SpawnAtNode(GodState s, Faction f, UnitKind kind, Kit kit, int node, EventBuffer ev)
        {
            var g = s.Territory.Grid;
            int x = Pathing.NodeMmX(g, node), z = Pathing.NodeMmZ(g, node);
            int k = s.Military.NextUnitId % MilitaryTuning.SpawnSpreadX.Length;
            int sx = x + MilitaryTuning.SpawnSpreadX[k], sz = z + MilitaryTuning.SpawnSpreadZ[k];
            if (Pathing.IsLandPoint(g, sx, sz)) { x = sx; z = sz; }
            return SpawnUnit(s, f, kind, kit, x, z, ev);
        }

        /// <summary>
        /// Drains <see cref="GodState.UnitQueue"/>: each order spawns a soldier wearing <c>order.Kit</c> at its factory's flag node. When the factory is gone (or
        /// has changed hands) the unit spawns at the faction's HQ nearest to where the factory last stood (its flag node);
        /// when the faction has no HQ either, the order is dropped.
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
                if (b != null && b.Faction == o.Faction) { SpawnAtNode(s, o.Faction, o.Kind, o.Kit, b.FlagNode, ev); continue; }

                Hq best = null; long bestD = long.MaxValue;
                int refNode = m.Known.TryGetValue(o.BuildingId, out var k) ? k.node : -1;
                foreach (var h in t.Hqs)
                {
                    if (h.Faction != o.Faction || h.Site) continue;
                    long d = refNode >= 0 ? t.Grid.Distance(h.Node, refNode) : 0;
                    if (best == null || d < bestD) { best = h; bestD = d; }
                }
                if (best == null) continue;
                SpawnAtNode(s, o.Faction, o.Kind, o.Kit, t.BuildingById(best.Id).FlagNode, ev);
            }
        }

        // ---------------- building health ----------------

        /// <summary>
        /// Keeps one health record per building and HQ: new ones start full, vanished ones are dropped with their marks and
        /// flags. An HQ that became a Citadel gains the extra health (its damage stays).
        /// </summary>
        public static void SyncBuildings(GodState s, EventBuffer ev)
        {
            var t = s.Territory; var m = s.Military; var g = t.Grid;
            var old = new Dictionary<int, BuildingRecord>();
            foreach (var r in m.Buildings) old[r.Id] = r;
            var list = new List<BuildingRecord>(t.Buildings.Count);
            foreach (var b in t.Buildings)
            {
                int max = MaxHealthOf(t, b);
                if (!old.TryGetValue(b.Id, out var rec))
                {
                    rec = new BuildingRecord
                    {
                        Id = b.Id, Faction = b.Faction, Node = b.Node, Size = b.Size, Kind = b.Kind, Health = max, MaxHealth = max,
                        X = Pathing.NodeMmX(g, b.Node), Z = Pathing.NodeMmZ(g, b.Node),
                    };
                }
                else if (rec.MaxHealth != max)
                {
                    rec.Health = System.Math.Max(1, rec.Health + max - rec.MaxHealth);
                    rec.MaxHealth = max;
                }
                rec.Faction = b.Faction;
                m.Known[b.Id] = (b.Faction, b.FlagNode);
                list.Add(rec);
            }
            m.Buildings = list;
            m.IndexBuildings();

            foreach (var f in FactionInfo.All)
            {
                var marks = m.MarkedBy[(int)f];
                for (int i = marks.Count - 1; i >= 0; i--)
                    if (m.BuildingById(marks[i]) == null)
                    {
                        ev.Add(new TargetUnmarked { By = f, BuildingId = marks[i], Destroyed = true });
                        marks.RemoveAt(i);
                    }
            }
            for (int i = m.Defends.Count - 1; i >= 0; i--)
                if (m.Defends[i].AnchorBuildingId != 0 && m.BuildingById(m.Defends[i].AnchorBuildingId) == null)
                    DropDefend(m, m.Defends[i].Id, true, ev);
        }

        /// <summary>Full health of a territory building: by size, and for an HQ by whether it is a Citadel.</summary>
        public static int MaxHealthOf(TerritoryState t, Building b)
        {
            if (b.Size == BuildingSize.Hq) { var hq = t.HqOf(b.Id); return MilitaryTuning.HqMaxHealth(hq != null && hq.Citadel); }
            return MilitaryTuning.HealthOf(b.Size);
        }

        public static void DropDefend(MilitaryState m, int flagId, bool anchorLost, EventBuffer ev)
        {
            var f = m.DefendById(flagId);
            if (f == null) return;
            m.Defends.Remove(f);
            foreach (var u in m.Units) if (u.GuardFlagId == flagId) { u.GuardFlagId = 0; u.Returning = false; }
            ev?.Add(new DefendRemoved { Faction = f.Faction, FlagId = flagId, AnchorLost = anchorLost });
        }

        /// <summary>Removes one faction's mark on a building, if it has one, with a <see cref="TargetUnmarked"/>.</summary>
        public static void Unmark(MilitaryState m, Faction by, int buildingId, bool destroyed, EventBuffer ev)
        {
            if (m.MarkedBy[(int)by].Remove(buildingId)) ev?.Add(new TargetUnmarked { By = by, BuildingId = buildingId, Destroyed = destroyed });
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
            foreach (var f in FactionInfo.All) Unmark(m, f, rec.Id, true, ev);
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
    }
}
