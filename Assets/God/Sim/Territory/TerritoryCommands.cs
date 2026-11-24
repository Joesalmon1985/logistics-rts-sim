namespace God.Sim
{
    /// <summary>Base of the territory commands: the faction rides in <see cref="Command.PlayerId"/>.</summary>
    public abstract class FactionCommand : Command
    {
        public Faction Faction => (Faction)PlayerId;
        protected FactionCommand(Faction f) { PlayerId = (int)f; }
        public bool ValidFaction => PlayerId == (int)Faction.Player || PlayerId == (int)Faction.Enemy;
    }

    public sealed class PlaceHq : FactionCommand
    {
        public int Corner;
        public PlaceHq(Faction f, int corner) : base(f) { Corner = corner; }
    }

    public sealed class PlaceBuilding : FactionCommand
    {
        public int Node;
        public BuildingSize Size;
        /// <summary>Placeholder kind until P2.</summary>
        public string Kind = TerritoryTuning.PlaceholderKind;
        public PlaceBuilding(Faction f, int node, BuildingSize size) : base(f) { Node = node; Size = size; }
    }

    public sealed class RemoveBuilding : FactionCommand
    {
        public int BuildingId;
        public RemoveBuilding(Faction f, int buildingId) : base(f) { BuildingId = buildingId; }
    }

    public sealed class BuildRoad : FactionCommand
    {
        public int[] Nodes;
        public BuildRoad(Faction f, int[] nodes) : base(f) { Nodes = nodes; }
    }

    public sealed class RemoveRoad : FactionCommand
    {
        public int RoadId;
        public RemoveRoad(Faction f, int roadId) : base(f) { RoadId = roadId; }
    }

    public sealed class PlaceFlag : FactionCommand
    {
        public int Node;
        public PlaceFlag(Faction f, int node) : base(f) { Node = node; }
    }

    internal static class TerritoryHandlers
    {
        public static CommandResult Bad(FactionCommand c) => c.ValidFaction ? CommandResult.Accept() : CommandResult.RejectDefault(Reasons.NotOwner);
    }

    public sealed class PlaceHqHandler : CommandHandler<PlaceHq>
    {
        protected override CommandResult Validate(GodState s, PlaceHq c) =>
            !c.ValidFaction ? TerritoryHandlers.Bad(c) : TerritoryRules.CheckHq(s, c.Faction, c.Corner);
        protected override CommandResult Apply(GodState s, PlaceHq c, EventBuffer ev) =>
            !c.ValidFaction ? TerritoryHandlers.Bad(c) : TerritoryRules.DoPlaceHq(s, c.Faction, c.Corner, ev);
    }

    public sealed class PlaceBuildingHandler : CommandHandler<PlaceBuilding>
    {
        protected override CommandResult Validate(GodState s, PlaceBuilding c) =>
            !c.ValidFaction ? TerritoryHandlers.Bad(c) : TerritoryRules.CheckBuilding(s, c.Faction, c.Node, c.Size);
        protected override CommandResult Apply(GodState s, PlaceBuilding c, EventBuffer ev) =>
            !c.ValidFaction ? TerritoryHandlers.Bad(c) : TerritoryRules.DoPlaceBuilding(s, c.Faction, c.Node, c.Size, c.Kind, ev);
    }

    public sealed class RemoveBuildingHandler : CommandHandler<RemoveBuilding>
    {
        protected override CommandResult Validate(GodState s, RemoveBuilding c) =>
            !c.ValidFaction ? TerritoryHandlers.Bad(c) : TerritoryRules.CheckRemoveBuilding(s, c.Faction, c.BuildingId);
        protected override CommandResult Apply(GodState s, RemoveBuilding c, EventBuffer ev) =>
            !c.ValidFaction ? TerritoryHandlers.Bad(c) : TerritoryRules.DoRemoveBuilding(s, c.Faction, c.BuildingId, ev);
    }

    public sealed class BuildRoadHandler : CommandHandler<BuildRoad>
    {
        protected override CommandResult Validate(GodState s, BuildRoad c) =>
            !c.ValidFaction ? TerritoryHandlers.Bad(c) : TerritoryRules.CheckRoad(s, c.Faction, c.Nodes);
        protected override CommandResult Apply(GodState s, BuildRoad c, EventBuffer ev) =>
            !c.ValidFaction ? TerritoryHandlers.Bad(c) : TerritoryRules.DoBuildRoad(s, c.Faction, c.Nodes, ev);
    }

    public sealed class RemoveRoadHandler : CommandHandler<RemoveRoad>
    {
        protected override CommandResult Validate(GodState s, RemoveRoad c) =>
            !c.ValidFaction ? TerritoryHandlers.Bad(c) : TerritoryRules.CheckRemoveRoad(s, c.Faction, c.RoadId);
        protected override CommandResult Apply(GodState s, RemoveRoad c, EventBuffer ev) =>
            !c.ValidFaction ? TerritoryHandlers.Bad(c) : TerritoryRules.DoRemoveRoad(s, c.Faction, c.RoadId, ev);
    }

    public sealed class PlaceFlagHandler : CommandHandler<PlaceFlag>
    {
        protected override CommandResult Validate(GodState s, PlaceFlag c) =>
            !c.ValidFaction ? TerritoryHandlers.Bad(c) : TerritoryRules.CheckFlag(s, c.Faction, c.Node);
        protected override CommandResult Apply(GodState s, PlaceFlag c, EventBuffer ev) =>
            !c.ValidFaction ? TerritoryHandlers.Bad(c) : TerritoryRules.DoPlaceFlag(s, c.Faction, c.Node, ev);
    }
}
