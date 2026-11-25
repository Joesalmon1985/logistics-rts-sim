namespace God.Sim
{
    public sealed class SetInputPriority : FactionCommand
    {
        public int BuildingId;
        public Ware Ware;
        public Priority Priority;
        public SetInputPriority(Faction f, int buildingId, Ware ware, Priority priority) : base(f)
        { BuildingId = buildingId; Ware = ware; Priority = priority; }
    }

    public sealed class SetPaused : FactionCommand
    {
        public int BuildingId;
        public bool Paused;
        public SetPaused(Faction f, int buildingId, bool paused) : base(f) { BuildingId = buildingId; Paused = paused; }
    }

    public sealed class SetInputPriorityHandler : CommandHandler<SetInputPriority>
    {
        private static CommandResult Check(GodState s, SetInputPriority c)
        {
            if (!c.ValidIn(s)) return CommandResult.RejectDefault(Reasons.NotOwner);
            var b = s.Economy?.Find(c.BuildingId);
            if (b == null || b.IsHq) return CommandResult.RejectDefault(Reasons.NoSuchThing);
            if (b.Faction != c.Faction) return CommandResult.RejectDefault(Reasons.NotOwner);
            if (c.Ware == Ware.None || (int)c.Ware >= Wares.Count || (b.Def.Cost[(int)c.Ware] <= 0 && b.Def.In[(int)c.Ware] <= 0)) return CommandResult.RejectDefault(Reasons.NotAnInput);
            return CommandResult.Accept();
        }
        protected override CommandResult Validate(GodState s, SetInputPriority c) => Check(s, c);
        protected override CommandResult Apply(GodState s, SetInputPriority c, EventBuffer ev)
        {
            var r = Check(s, c);
            if (r.Ok) s.Economy.Find(c.BuildingId).Prio[(int)c.Ware] = c.Priority;
            return r;
        }
    }

    public sealed class SetPausedHandler : CommandHandler<SetPaused>
    {
        private static CommandResult Check(GodState s, SetPaused c)
        {
            if (!c.ValidIn(s)) return CommandResult.RejectDefault(Reasons.NotOwner);
            var b = s.Economy?.Find(c.BuildingId);
            if (b == null || b.IsHq) return CommandResult.RejectDefault(Reasons.NoSuchThing);
            if (b.Faction != c.Faction) return CommandResult.RejectDefault(Reasons.NotOwner);
            return CommandResult.Accept();
        }
        protected override CommandResult Validate(GodState s, SetPaused c) => Check(s, c);
        protected override CommandResult Apply(GodState s, SetPaused c, EventBuffer ev)
        {
            var r = Check(s, c);
            if (r.Ok) s.Economy.Find(c.BuildingId).Paused = c.Paused;
            return r;
        }
    }
}

namespace God.Sim
{
    /// <summary>
    /// Stage 3 seam: upgrade a finished harvester one level (1 to 2, 2 to 3). The cost wares are carried to it like a
    /// site's; it keeps working meanwhile. The handler belongs to the economy package.
    /// </summary>
    public sealed class UpgradeBuilding : FactionCommand
    {
        public int BuildingId;
        public UpgradeBuilding(Faction f, int buildingId) : base(f) { BuildingId = buildingId; }
    }
}

namespace God.Sim
{
    /// <summary>A harvester upgrade began: its wares are now carried to it.</summary>
    public sealed class BuildingUpgradeStarted : SimEvent { public Faction Faction; public int BuildingId; public string Kind; public int ToLevel; }
    /// <summary>A harvester reached a new level (the view makes it bigger).</summary>
    public sealed class BuildingUpgraded : SimEvent { public Faction Faction; public int BuildingId; public string Kind; public int Level; }

    public sealed class UpgradeBuildingHandler : CommandHandler<UpgradeBuilding>
    {
        public static CommandResult Check(GodState s, UpgradeBuilding c)
        {
            if (!c.ValidIn(s)) return CommandResult.RejectDefault(Reasons.NotOwner);
            var b = s.Economy?.Find(c.BuildingId);
            if (b == null) return CommandResult.RejectDefault(Reasons.NoSuchThing);
            if (b.Faction != c.Faction) return CommandResult.RejectDefault(Reasons.NotOwner);
            if (b.IsHq || b.Def == null || b.Def.Role != BuildingRole.Harvester) return CommandResult.RejectDefault(Reasons.NotAHarvester);
            if (!b.Built) return CommandResult.RejectDefault(Reasons.NotFinished);
            if (b.Upgrading) return CommandResult.RejectDefault(Reasons.AlreadyUpgrading);
            if (b.Level >= EconomyTuning.MaxHarvesterLevel) return CommandResult.RejectDefault(Reasons.MaxLevel);
            return CommandResult.Accept();
        }

        protected override CommandResult Validate(GodState s, UpgradeBuilding c) => Check(s, c);

        protected override CommandResult Apply(GodState s, UpgradeBuilding c, EventBuffer ev)
        {
            var r = Check(s, c);
            if (!r.Ok) return r;
            var b = s.Economy.Find(c.BuildingId);
            EconomySystem.StartUpgrade(b);
            ev.Add(new BuildingUpgradeStarted { Faction = b.Faction, BuildingId = b.Id, Kind = b.Kind, ToLevel = b.Level + 1 });
            return r;
        }
    }
}
