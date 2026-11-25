namespace God.Sim
{
    /// <summary>Refusal codes of the military commands (kept here so no shared file changes).</summary>
    public static class MilitaryReasons
    {
        public const string GameOver = "game_over";
        public const string NotEnemy = "not_enemy";
        public const string NotAtWar = "not_at_war";
        public const string AlreadyMarked = "already_marked";
        public const string NotMarked = "not_marked";
        public const string BadGuards = "bad_guards";
        public const string NotLand = "not_land";
        public const string NoSuchFlag = "no_such_flag";
        public const string NoSuchUnit = "no_such_unit";

        public static CommandResult Refuse(string code, string message) => CommandResult.Reject(code, message);
    }

    /// <summary>
    /// God order: <c>Faction</c> marks a building or HQ of a faction it is at war with as a target. Its units attack its
    /// marks in the order they were made. Marks against a faction are dropped when peace is made with it, and a mark on a
    /// building that changes hands is dropped unless the new owner is also an enemy.
    /// </summary>
    public sealed class MarkTarget : FactionCommand
    {
        public int BuildingId;
        public MarkTarget(Faction by, int buildingId) : base(by) { BuildingId = buildingId; }
    }

    public sealed class UnmarkTarget : FactionCommand
    {
        public int BuildingId;
        public UnmarkTarget(Faction by, int buildingId) : base(by) { BuildingId = buildingId; }
    }

    /// <summary>God order: a defend flag on an own building or HQ, or on a land node, with 0..5 guards. A second order on the same anchor changes its guard count.</summary>
    public sealed class SetDefend : FactionCommand
    {
        /// <summary>Own building or HQ id, 0 when the anchor is a bare node.</summary>
        public int AnchorBuildingId;
        /// <summary>Anchor node when <see cref="AnchorBuildingId"/> is 0.</summary>
        public int Node = -1;
        public int Guards;

        public SetDefend(Faction by, int anchorBuildingId, int node, int guards) : base(by)
        { AnchorBuildingId = anchorBuildingId; Node = node; Guards = guards; }

        public static SetDefend OnBuilding(Faction by, int buildingId, int guards) => new SetDefend(by, buildingId, -1, guards);
        public static SetDefend OnNode(Faction by, int node, int guards) => new SetDefend(by, 0, node, guards);
    }

    public sealed class RemoveDefend : FactionCommand
    {
        public int FlagId;
        public RemoveDefend(Faction by, int flagId) : base(by) { FlagId = flagId; }
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
            s.Military == null || s.IsGameOver ? MilitaryReasons.Refuse(MilitaryReasons.GameOver, "The game is over.") : CommandResult.Accept();

        public static CommandResult Sender(GodState s, FactionCommand c) =>
            c.ValidIn(s) ? CommandResult.Accept() : CommandResult.RejectDefault(Reasons.NotOwner);
    }

    public sealed class MarkTargetHandler : CommandHandler<MarkTarget>
    {
        protected override CommandResult Validate(GodState s, MarkTarget c)
        {
            var r = MilitaryCheck.Playing(s); if (!r.Ok) return r;
            r = MilitaryCheck.Sender(s, c); if (!r.Ok) return r;
            var b = s.Territory.BuildingById(c.BuildingId);
            if (b == null) return CommandResult.RejectDefault(Reasons.NoSuchThing);
            if (b.Faction == c.Faction) return MilitaryReasons.Refuse(MilitaryReasons.NotEnemy, "That is yours.");
            if (s.Factions == null || !s.Factions.AtWar(c.Faction, b.Faction))
                return MilitaryReasons.Refuse(MilitaryReasons.NotAtWar, "You can only target a faction you are at war with.");
            if (s.Military.IsMarked(c.Faction, c.BuildingId)) return MilitaryReasons.Refuse(MilitaryReasons.AlreadyMarked, "That target is already marked.");
            return CommandResult.Accept();
        }

        protected override CommandResult Apply(GodState s, MarkTarget c, EventBuffer ev)
        {
            var r = Validate(s, c); if (!r.Ok) return r;
            var list = s.Military.MarkedBy[(int)c.Faction];
            list.Add(c.BuildingId);
            ev.Add(new TargetMarked { By = c.Faction, BuildingId = c.BuildingId, Order = list.Count });
            return r;
        }
    }

    public sealed class UnmarkTargetHandler : CommandHandler<UnmarkTarget>
    {
        protected override CommandResult Validate(GodState s, UnmarkTarget c)
        {
            var r = MilitaryCheck.Playing(s); if (!r.Ok) return r;
            r = MilitaryCheck.Sender(s, c); if (!r.Ok) return r;
            if (!s.Military.IsMarked(c.Faction, c.BuildingId)) return MilitaryReasons.Refuse(MilitaryReasons.NotMarked, "That target is not marked.");
            return CommandResult.Accept();
        }

        protected override CommandResult Apply(GodState s, UnmarkTarget c, EventBuffer ev)
        {
            var r = Validate(s, c); if (!r.Ok) return r;
            s.Military.MarkedBy[(int)c.Faction].Remove(c.BuildingId);
            ev.Add(new TargetUnmarked { By = c.Faction, BuildingId = c.BuildingId, Destroyed = false });
            return r;
        }
    }

    public sealed class SetDefendHandler : CommandHandler<SetDefend>
    {
        protected override CommandResult Validate(GodState s, SetDefend c)
        {
            var r = MilitaryCheck.Playing(s); if (!r.Ok) return r;
            r = MilitaryCheck.Sender(s, c); if (!r.Ok) return r;
            if (c.Guards < 0 || c.Guards > MilitaryTuning.MaxGuards)
                return MilitaryReasons.Refuse(MilitaryReasons.BadGuards, "A flag takes 0 to 5 guards.");
            var t = s.Territory;
            if (c.AnchorBuildingId != 0)
            {
                var b = t.BuildingById(c.AnchorBuildingId);
                if (b == null) return CommandResult.RejectDefault(Reasons.NoSuchThing);
                if (b.Faction != c.Faction) return CommandResult.RejectDefault(Reasons.NotOwner);
                return CommandResult.Accept();
            }
            if (!t.Grid.IsValid(c.Node) || !t.Grid.IsLand(c.Node)) return MilitaryReasons.Refuse(MilitaryReasons.NotLand, "A defend flag goes on land.");
            return CommandResult.Accept();
        }

        protected override CommandResult Apply(GodState s, SetDefend c, EventBuffer ev)
        {
            var r = Validate(s, c); if (!r.Ok) return r;
            var m = s.Military; var g = s.Territory.Grid;
            var flag = c.AnchorBuildingId != 0 ? m.DefendAtBuilding(c.AnchorBuildingId) : m.DefendAtNode(c.Faction, c.Node);
            if (flag == null)
            {
                int node = c.AnchorBuildingId != 0 ? s.Territory.BuildingById(c.AnchorBuildingId).Node : c.Node;
                flag = new DefendFlag
                {
                    Id = m.NextDefendId++, Faction = c.Faction, AnchorBuildingId = c.AnchorBuildingId, Node = node,
                    X = Pathing.NodeMmX(g, node), Z = Pathing.NodeMmZ(g, node),
                };
                m.Defends.Add(flag);
            }
            flag.Guards = c.Guards;
            ev.Add(new DefendSet { Faction = c.Faction, FlagId = flag.Id, AnchorBuildingId = flag.AnchorBuildingId, Node = flag.Node, Guards = flag.Guards });
            return r;
        }
    }

    public sealed class RemoveDefendHandler : CommandHandler<RemoveDefend>
    {
        protected override CommandResult Validate(GodState s, RemoveDefend c)
        {
            var r = MilitaryCheck.Playing(s); if (!r.Ok) return r;
            r = MilitaryCheck.Sender(s, c); if (!r.Ok) return r;
            var f = s.Military.DefendById(c.FlagId);
            if (f == null || f.Faction != c.Faction) return MilitaryReasons.Refuse(MilitaryReasons.NoSuchFlag, "There is no such defend flag.");
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
            if (!s.Setup.InGame((int)c.Faction) || s.Factions.IsEliminated(c.Faction)) return CommandResult.RejectDefault(Reasons.NotOwner);
            var g = s.Territory.Grid;
            if (!g.IsValid(c.Node) || !g.IsLand(c.Node)) return MilitaryReasons.Refuse(MilitaryReasons.NotLand, "Units stand on land.");
            return CommandResult.Accept();
        }

        protected override CommandResult Apply(GodState s, DevSpawnUnit c, EventBuffer ev)
        {
            var r = Validate(s, c); if (!r.Ok) return r;
            var g = s.Territory.Grid;
            MilitaryRules.SpawnUnit(s, c.Faction, c.Kind, Pathing.NodeMmX(g, c.Node), Pathing.NodeMmZ(g, c.Node), ev);
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
