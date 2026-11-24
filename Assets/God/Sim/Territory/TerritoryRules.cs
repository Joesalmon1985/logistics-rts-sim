using System.Collections.Generic;

namespace God.Sim
{
    /// <summary>
    /// The placement rules of P1: HQ corners, buildings on the node lattice, flags and roads. Each action has a Check
    /// (never mutates) and a Do (re-checks, then mutates and emits events).
    /// </summary>
    public static class TerritoryRules
    {
        private static CommandResult Refuse(string reason, params object[] args) => CommandResult.RejectDefault(reason, args);

        // ---------------- HQ ----------------

        public static CommandResult CheckHq(GodState s, Faction f, int corner)
        {
            var t = s.Territory; var topo = t.Topology; var g = t.Grid;
            if (corner < 0 || corner >= topo.CornerCount || topo.VertexHexes(corner).Count != 3) return Refuse(Reasons.NotLandCorner);
            if (t.HqNearCorner(corner)) return Refuse(Reasons.HqTooClose);
            bool reach = false;
            foreach (var h in t.Hqs) if (h.Faction == f && topo.CornerDistance(h.Corner, corner) == 2) { reach = true; break; }
            if (!reach) return Refuse(Reasons.HqOutOfReach);
            return CheckHqSite(t, g.CornerNode(corner));
        }

        /// <summary>The HQ node and its flag node must be free, and the flag must keep its distance from other flags.</summary>
        private static CommandResult CheckHqSite(TerritoryState t, int node)
        {
            var g = t.Grid;
            int flagNode = g.FlagNodeOf(node);
            if (flagNode < 0 || !g.IsLand(node) || !g.IsLand(flagNode)) return Refuse(Reasons.NotBuildable);
            if (!t.NodeAt(node).IsFree || t.NodeBuilding[flagNode] != 0 || t.NodeFlag[flagNode] != 0 || t.NodeRoad[flagNode] != 0)
                return Refuse(Reasons.Occupied);
            if (t.FlagNextTo(flagNode)) return Refuse(Reasons.FlagTooClose);
            return CommandResult.Accept();
        }

        /// <summary>Puts an HQ and its flag down without the reach rule (game start, tests).</summary>
        public static Hq AddHq(GodState s, Faction f, int corner, EventBuffer ev)
        {
            var t = s.Territory; var g = t.Grid;
            int node = g.CornerNode(corner);
            var b = AddBuilding(t, f, node, BuildingSize.Hq, TerritoryTuning.HqKind, g.FlagNodeOf(node));
            var hq = new Hq { Id = b.Id, Faction = f, Corner = corner, Node = node };
            t.Hqs.Add(hq);
            ev?.Add(new HqPlaced { Faction = f, HqId = hq.Id, Corner = corner });
            return hq;
        }

        public static CommandResult DoPlaceHq(GodState s, Faction f, int corner, EventBuffer ev)
        {
            var r = CheckHq(s, f, corner);
            if (!r.Ok) return r;
            AddHq(s, f, corner, ev);
            return r;
        }

        // ---------------- buildings ----------------

        public static CommandResult CheckBuilding(GodState s, Faction f, int node, BuildingSize size)
        {
            var t = s.Territory; var g = t.Grid;
            if (size != BuildingSize.Small && size != BuildingSize.Medium) return Refuse(Reasons.InvalidSite);
            if (!g.IsValid(node) || !g.IsLand(node) || !t.IsBuildable(f, node)) return Refuse(Reasons.NotBuildable);
            int flagNode = g.FlagNodeOf(node);
            if (flagNode < 0 || !t.IsBuildable(f, flagNode)) return Refuse(Reasons.NotBuildable);
            if (!t.NodeAt(node).IsFree) return Refuse(Reasons.Occupied);

            for (int d = 0; d < 6; d++)
            {
                int nb = g.Neighbour(node, d);
                if (nb >= 0 && t.NodeBuilding[nb] != 0) return Refuse(Reasons.TooCrowded);
            }
            if (size == BuildingSize.Medium)
                for (int d = 0; d < 6; d++)
                {
                    int nb = g.Neighbour(node, d);
                    if (nb < 0 || nb == flagNode) continue;
                    if (t.NodeFlag[nb] != 0 || t.NodeRoad[nb] != 0) return Refuse(Reasons.TooCrowded);
                }

            // The flag: an own spare flag is reused, an own road is split, a free node gets a new flag.
            if (t.NodeFlag[flagNode] != 0)
            {
                var fl = t.FlagById(t.NodeFlag[flagNode]);
                if (fl.Faction != f || fl.BuildingId != 0) return Refuse(Reasons.Occupied);
                return CommandResult.Accept();
            }
            if (t.NodeRoad[flagNode] != 0 && t.RoadById(t.NodeRoad[flagNode]).Faction != f) return Refuse(Reasons.Occupied);
            if (t.FlagNextTo(flagNode)) return Refuse(Reasons.FlagTooClose);
            if (t.InMediumHalo(flagNode)) return Refuse(Reasons.TooCrowded);
            return CommandResult.Accept();
        }

        public static CommandResult DoPlaceBuilding(GodState s, Faction f, int node, BuildingSize size, string kind, EventBuffer ev)
        {
            var r = CheckBuilding(s, f, node, size);
            if (!r.Ok) return r;
            var b = AddBuilding(s.Territory, f, node, size, string.IsNullOrEmpty(kind) ? TerritoryTuning.PlaceholderKind : kind, s.Territory.Grid.FlagNodeOf(node));
            ev?.Add(new BuildingPlaced { Faction = f, BuildingId = b.Id, Node = node, Size = size });
            return r;
        }

        /// <summary>Creates the building and its flag (reusing a spare flag, splitting a road, or adding a flag). No checks.</summary>
        private static Building AddBuilding(TerritoryState t, Faction f, int node, BuildingSize size, string kind, int flagNode)
        {
            Flag flag;
            if (t.NodeFlag[flagNode] != 0) flag = t.FlagById(t.NodeFlag[flagNode]);
            else if (t.NodeRoad[flagNode] != 0) flag = SplitRoad(t, t.NodeRoad[flagNode], flagNode);
            else flag = AddFlag(t, f, flagNode);
            var b = new Building { Id = t.NextId++, Faction = f, Node = node, Size = size, Kind = kind, FlagId = flag.Id, FlagNode = flagNode };
            flag.BuildingId = b.Id;
            t.Buildings.Add(b);
            t.NodeBuilding[node] = b.Id;
            return b;
        }

        public static CommandResult CheckRemoveBuilding(GodState s, Faction f, int buildingId)
        {
            var b = s.Territory.BuildingById(buildingId);
            if (b == null) return Refuse(Reasons.NoSuchThing);
            if (b.Faction != f) return Refuse(Reasons.NotOwner);
            if (b.Size == BuildingSize.Hq) return Refuse(Reasons.CannotRemove);
            return CommandResult.Accept();
        }

        /// <summary>Removes the building and frees its node. Its flag goes too unless a road still uses it.</summary>
        public static CommandResult DoRemoveBuilding(GodState s, Faction f, int buildingId, EventBuffer ev)
        {
            var r = CheckRemoveBuilding(s, f, buildingId);
            if (!r.Ok) return r;
            var t = s.Territory;
            var b = t.BuildingById(buildingId);
            t.NodeBuilding[b.Node] = 0;
            t.Buildings.Remove(b);
            var flag = t.FlagById(b.FlagId);
            if (flag != null)
            {
                flag.BuildingId = 0;
                DropFlagIfSpare(t, flag);
            }
            ev?.Add(new BuildingRemoved { Faction = f, BuildingId = buildingId, Node = b.Node });
            return r;
        }

        // ---------------- flags ----------------

        private static Flag AddFlag(TerritoryState t, Faction f, int node)
        {
            var fl = new Flag { Id = t.NextId++, Faction = f, Node = node };
            t.Flags.Add(fl);
            t.NodeFlag[node] = fl.Id;
            return fl;
        }

        private static bool FlagHasRoad(TerritoryState t, int flagId)
        {
            foreach (var r in t.Roads) if (r.FlagA == flagId || r.FlagB == flagId) return true;
            return false;
        }

        /// <summary>A flag with no building and no road is removed.</summary>
        private static void DropFlagIfSpare(TerritoryState t, Flag flag)
        {
            if (flag.BuildingId != 0 || FlagHasRoad(t, flag.Id)) return;
            t.NodeFlag[flag.Node] = 0;
            t.Flags.Remove(flag);
        }

        public static CommandResult CheckFlag(GodState s, Faction f, int node)
        {
            var t = s.Territory; var g = t.Grid;
            if (!g.IsValid(node) || !g.IsLand(node)) return Refuse(Reasons.NotOnRoad);
            if (t.NodeRoad[node] == 0) return Refuse(Reasons.NotOnRoad);
            if (t.RoadById(t.NodeRoad[node]).Faction != f) return Refuse(Reasons.NotOwner);
            if (t.FlagNextTo(node)) return Refuse(Reasons.FlagTooClose);
            if (t.InMediumHalo(node)) return Refuse(Reasons.TooCrowded);
            return CommandResult.Accept();
        }

        /// <summary>Places a flag on a road node and splits the road in two.</summary>
        public static CommandResult DoPlaceFlag(GodState s, Faction f, int node, EventBuffer ev)
        {
            var r = CheckFlag(s, f, node);
            if (!r.Ok) return r;
            var flag = SplitRoad(s.Territory, s.Territory.NodeRoad[node], node);
            ev?.Add(new FlagPlaced { Faction = f, FlagId = flag.Id, Node = node });
            return r;
        }

        /// <summary>Replaces a road by two roads that meet at a new flag on one of its interior nodes.</summary>
        private static Flag SplitRoad(TerritoryState t, int roadId, int node)
        {
            var road = t.RoadById(roadId);
            int k = System.Array.IndexOf(road.Nodes, node);
            var flag = AddFlag(t, road.Faction, node);
            t.Roads.Remove(road);
            t.NodeRoad[node] = 0;
            AddRoad(t, road.Faction, road.FlagA, flag.Id, Slice(road.Nodes, 0, k));
            AddRoad(t, road.Faction, flag.Id, road.FlagB, Slice(road.Nodes, k, road.Nodes.Length - 1));
            return flag;
        }

        private static int[] Slice(int[] a, int from, int to)
        {
            var r = new int[to - from + 1];
            System.Array.Copy(a, from, r, 0, r.Length);
            return r;
        }

        private static Road AddRoad(TerritoryState t, Faction f, int flagA, int flagB, int[] nodes)
        {
            var road = new Road { Id = t.NextId++, Faction = f, FlagA = flagA, FlagB = flagB, Nodes = nodes };
            t.Roads.Add(road);
            for (int i = 1; i < nodes.Length - 1; i++) t.NodeRoad[nodes[i]] = road.Id;
            return road;
        }

        // ---------------- roads ----------------

        public static CommandResult CheckRoad(GodState s, Faction f, int[] nodes)
        {
            var t = s.Territory; var g = t.Grid;
            if (nodes == null || nodes.Length < 2) return Refuse(Reasons.BadRoad, "A road needs at least two nodes.");
            var seen = new HashSet<int>();
            for (int i = 0; i < nodes.Length; i++)
            {
                if (!g.IsValid(nodes[i]) || !g.IsLand(nodes[i])) return Refuse(Reasons.BadRoad, "A road runs on land.");
                if (!seen.Add(nodes[i])) return Refuse(Reasons.BadRoad, "A road cannot cross itself.");
                if (i > 0 && !g.Adjacent(nodes[i - 1], nodes[i])) return Refuse(Reasons.BadRoad, "A road is a chain of adjacent nodes.");
            }
            var first = t.FlagAtNode(nodes[0]);
            if (first == null || first.Faction != f) return Refuse(Reasons.BadRoad, "A road starts at one of your flags.");

            for (int i = 1; i < nodes.Length - 1; i++)
            {
                if (!t.NodeAt(nodes[i]).IsFree) return Refuse(Reasons.BadRoad, "A road cannot pass through buildings, flags or other roads.");
                if (t.InMediumHalo(nodes[i])) return Refuse(Reasons.TooCrowded);
            }

            int last = nodes[nodes.Length - 1];
            var end = t.FlagAtNode(last);
            if (end != null)
            {
                if (end.Faction != f) return Refuse(Reasons.BadRoad, "A road ends at one of your flags.");
                return CommandResult.Accept();
            }
            if (!t.NodeAt(last).IsFree) return Refuse(Reasons.BadRoad, "A road cannot end on a building or another road.");
            if (t.FlagNextTo(last)) return Refuse(Reasons.FlagTooClose);
            if (t.InMediumHalo(last)) return Refuse(Reasons.TooCrowded);
            return CommandResult.Accept();
        }

        public static CommandResult DoBuildRoad(GodState s, Faction f, int[] nodes, EventBuffer ev)
        {
            var r = CheckRoad(s, f, nodes);
            if (!r.Ok) return r;
            var t = s.Territory;
            int last = nodes[nodes.Length - 1];
            var end = t.FlagAtNode(last) ?? AddFlag(t, f, last);
            var start = t.FlagAtNode(nodes[0]);
            var road = AddRoad(t, f, start.Id, end.Id, (int[])nodes.Clone());
            ev?.Add(new RoadBuilt { Faction = f, RoadId = road.Id });
            return r;
        }

        public static CommandResult CheckRemoveRoad(GodState s, Faction f, int roadId)
        {
            var road = s.Territory.RoadById(roadId);
            if (road == null) return Refuse(Reasons.NoSuchThing);
            if (road.Faction != f) return Refuse(Reasons.NotOwner);
            return CommandResult.Accept();
        }

        /// <summary>Removes a road and frees its interior nodes. Its end flags stay unless nothing uses them any more.</summary>
        public static CommandResult DoRemoveRoad(GodState s, Faction f, int roadId, EventBuffer ev)
        {
            var r = CheckRemoveRoad(s, f, roadId);
            if (!r.Ok) return r;
            var t = s.Territory;
            var road = t.RoadById(roadId);
            for (int i = 1; i < road.Nodes.Length - 1; i++) t.NodeRoad[road.Nodes[i]] = 0;
            t.Roads.Remove(road);
            var a = t.FlagById(road.FlagA); var b = t.FlagById(road.FlagB);
            if (a != null) DropFlagIfSpare(t, a);
            if (b != null && b != a) DropFlagIfSpare(t, b);
            ev?.Add(new RoadRemoved { Faction = f, RoadId = roadId });
            return r;
        }
    }
}
