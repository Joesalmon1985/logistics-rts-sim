namespace God.Sim
{
    /// <summary>Refusal codes of the military commands (kept here so no shared file changes).</summary>
    public static class MilitaryReasons
    {
        public const string GameOver = "game_over";
        public const string NotEnemy = "not_enemy";
        public const string AlreadyMarked = "already_marked";
        public const string NotMarked = "not_marked";
        public const string BadGuards = "bad_guards";
        public const string NotLand = "not_land";
        public const string NoSuchFlag = "no_such_flag";
        public const string NoSuchUnit = "no_such_unit";

        public static CommandResult Refuse(string code, string message) => CommandResult.Reject(code, message);
    }

    /// <summary>God order: mark an enemy building or HQ as a target. Marks are attacked in the order they were made.</summary>
    public sealed class MarkTarget : Command
    {
        public int BuildingId;
        public MarkTarget(int buildingId) { PlayerId = (int)Faction.Player; BuildingId = buildingId; }
    }

    public sealed class UnmarkTarget : Command
    {
        public int BuildingId;
        public UnmarkTarget(int buildingId) { PlayerId = (int)Faction.Player; BuildingId = buildingId; }
    }

    /// <summary>God order: a defend flag on an own building or HQ, or on a land node, with 0..5 guards. A second order on the same anchor changes its guard count.</summary>
    public sealed class SetDefend : Command
    {
        /// <summary>Own building or HQ id, 0 when the anchor is a bare node.</summary>
        public int AnchorBuildingId;
        /// <summary>Anchor node when <see cref="AnchorBuildingId"/> is 0.</summary>
        public int Node = -1;
        public int Guards;

        public SetDefend(int anchorBuildingId, int node, int guards)
        { PlayerId = (int)Faction.Player; AnchorBuildingId = anchorBuildingId; Node = node; Guards = guards; }

        public static SetDefend OnBuilding(int buildingId, int guards) => new SetDefend(buildingId, -1, guards);
        public static SetDefend OnNode(int node, int guards) => new SetDefend(0, node, guards);
    }

    public sealed class RemoveDefend : Command
    {
        public int FlagId;
        public RemoveDefend(int flagId) { PlayerId = (int)Faction.Player; FlagId = flagId; }
    }

    /// <summary>Dev: spawn a unit on a land node, so the military can be tried without the economy.</summary>
    public sealed class DevSpawnUnit : Command
    {
        public Faction Faction;
        public UnitKind Kind;
        public int Node;
        public DevSpawnUnit(Faction faction, UnitKind kind, int node) { PlayerId = (int)faction; Faction = faction; Kind = kind; Node = node; }
    }

    /// <summary>Dev: order a unit to walk to a point (millimetres), around water. It ignores its brain until it arrives.</summary>
    public sealed class DevMoveUnit : Command
    {
        public int UnitId;
        public int X, Z;
        public DevMoveUnit(int unitId, int x, int z) { UnitId = unitId; X = x; Z = z; }
    }

    internal static class MilitaryCheck
    {
        public static CommandResult Playing(GodState s) =>
            s.Military == null || s.Military.IsGameOver ? MilitaryReasons.Refuse(MilitaryReasons.GameOver, "The game is over.") : CommandResult.Accept();

        public static CommandResult PlayerOnly(Command c) =>
            c.PlayerId == (int)Faction.Player ? CommandResult.Accept() : CommandResult.RejectDefault(Reasons.NotOwner);
    }

    public sealed class MarkTargetHandler : CommandHandler<MarkTarget>
    {
        protected override CommandResult Validate(GodState s, MarkTarget c)
        {
            var r = MilitaryCheck.Playing(s); if (!r.Ok) return r;
            r = MilitaryCheck.PlayerOnly(c); if (!r.Ok) return r;
            var b = s.Territory.BuildingById(c.BuildingId);
            if (b == null) return CommandResult.RejectDefault(Reasons.NoSuchThing);
            if (b.Faction != Faction.Enemy) return MilitaryReasons.Refuse(MilitaryReasons.NotEnemy, "Only enemy buildings can be marked.");
            if (s.Military.Marked.Contains(c.BuildingId)) return MilitaryReasons.Refuse(MilitaryReasons.AlreadyMarked, "That target is already marked.");
            return CommandResult.Accept();
        }

        protected override CommandResult Apply(GodState s, MarkTarget c, EventBuffer ev)
        {
            var r = Validate(s, c); if (!r.Ok) return r;
            s.Military.Marked.Add(c.BuildingId);
            ev.Add(new TargetMarked { BuildingId = c.BuildingId, Order = s.Military.Marked.Count });
            return r;
        }
    }

    public sealed class UnmarkTargetHandler : CommandHandler<UnmarkTarget>
    {
        protected override CommandResult Validate(GodState s, UnmarkTarget c)
        {
            var r = MilitaryCheck.Playing(s); if (!r.Ok) return r;
            r = MilitaryCheck.PlayerOnly(c); if (!r.Ok) return r;
            if (!s.Military.Marked.Contains(c.BuildingId)) return MilitaryReasons.Refuse(MilitaryReasons.NotMarked, "That target is not marked.");
            return CommandResult.Accept();
        }

        protected override CommandResult Apply(GodState s, UnmarkTarget c, EventBuffer ev)
        {
            var r = Validate(s, c); if (!r.Ok) return r;
            s.Military.Marked.Remove(c.BuildingId);
            ev.Add(new TargetUnmarked { BuildingId = c.BuildingId, Destroyed = false });
            return r;
        }
    }

    public sealed class SetDefendHandler : CommandHandler<SetDefend>
    {
        protected override CommandResult Validate(GodState s, SetDefend c)
        {
            var r = MilitaryCheck.Playing(s); if (!r.Ok) return r;
            r = MilitaryCheck.PlayerOnly(c); if (!r.Ok) return r;
            if (c.Guards < 0 || c.Guards > MilitaryTuning.MaxGuards)
                return MilitaryReasons.Refuse(MilitaryReasons.BadGuards, "A flag takes 0 to 5 guards.");
            var t = s.Territory;
            if (c.AnchorBuildingId != 0)
            {
                var b = t.BuildingById(c.AnchorBuildingId);
                if (b == null) return CommandResult.RejectDefault(Reasons.NoSuchThing);
                if (b.Faction != Faction.Player) return CommandResult.RejectDefault(Reasons.NotOwner);
                return CommandResult.Accept();
            }
            if (!t.Grid.IsValid(c.Node) || !t.Grid.IsLand(c.Node)) return MilitaryReasons.Refuse(MilitaryReasons.NotLand, "A defend flag goes on land.");
            return CommandResult.Accept();
        }

        protected override CommandResult Apply(GodState s, SetDefend c, EventBuffer ev)
        {
            var r = Validate(s, c); if (!r.Ok) return r;
            var m = s.Military; var g = s.Territory.Grid;
            var flag = c.AnchorBuildingId != 0 ? m.DefendAtBuilding(c.AnchorBuildingId) : m.DefendAtNode(c.Node);
            if (flag == null)
            {
                int node = c.AnchorBuildingId != 0 ? s.Territory.BuildingById(c.AnchorBuildingId).Node : c.Node;
                flag = new DefendFlag
                {
                    Id = m.NextDefendId++, AnchorBuildingId = c.AnchorBuildingId, Node = node,
                    X = Pathing.NodeMmX(g, node), Z = Pathing.NodeMmZ(g, node),
                };
                m.Defends.Add(flag);
            }
            flag.Guards = c.Guards;
            ev.Add(new DefendSet { FlagId = flag.Id, AnchorBuildingId = flag.AnchorBuildingId, Node = flag.Node, Guards = flag.Guards });
            return r;
        }
    }

    public sealed class RemoveDefendHandler : CommandHandler<RemoveDefend>
    {
        protected override CommandResult Validate(GodState s, RemoveDefend c)
        {
            var r = MilitaryCheck.Playing(s); if (!r.Ok) return r;
            r = MilitaryCheck.PlayerOnly(c); if (!r.Ok) return r;
            if (s.Military.DefendById(c.FlagId) == null) return MilitaryReasons.Refuse(MilitaryReasons.NoSuchFlag, "There is no such defend flag.");
            return CommandResult.Accept();
        }

        protected override CommandResult Apply(GodState s, RemoveDefend c, EventBuffer ev)
        {
            var r = Validate(s, c); if (!r.Ok) return r;
            MilitaryRules.DropDefend(s.Military, c.FlagId, false, ev);
            return r;
        }
    }

    public sealed class DevSpawnUnitHandler : CommandHandler<DevSpawnUnit>
    {
        protected override CommandResult Validate(GodState s, DevSpawnUnit c)
        {
            var r = MilitaryCheck.Playing(s); if (!r.Ok) return r;
            var g = s.Territory.Grid;
            if (!g.IsValid(c.Node) || !g.IsLand(c.Node)) return MilitaryReasons.Refuse(MilitaryReasons.NotLand, "Units stand on land.");
            return CommandResult.Accept();
        }

        protected override CommandResult Apply(GodState s, DevSpawnUnit c, EventBuffer ev)
        {
            var r = Validate(s, c); if (!r.Ok) return r;
            var g = s.Territory.Grid;
            MilitaryRules.SpawnUnit(s, c.Faction, c.Kind, Pathing.NodeMmX(g, c.Node), Pathing.NodeMmZ(g, c.Node), 0, ev);
            return r;
        }
    }

    public sealed class DevMoveUnitHandler : CommandHandler<DevMoveUnit>
    {
        protected override CommandResult Validate(GodState s, DevMoveUnit c)
        {
            var r = MilitaryCheck.Playing(s); if (!r.Ok) return r;
            if (s.Military.UnitById(c.UnitId) == null) return MilitaryReasons.Refuse(MilitaryReasons.NoSuchUnit, "There is no such unit.");
            if (!Pathing.IsLandPoint(s.Territory.Grid, c.X, c.Z)) return MilitaryReasons.Refuse(MilitaryReasons.NotLand, "Units stand on land.");
            return CommandResult.Accept();
        }

        protected override CommandResult Apply(GodState s, DevMoveUnit c, EventBuffer ev)
        {
            var r = Validate(s, c); if (!r.Ok) return r;
            var u = s.Military.UnitById(c.UnitId);
            u.GoalX = c.X; u.GoalZ = c.Z; u.HasGoal = true;
            return r;
        }
    }
}
