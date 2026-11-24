namespace God.Sim
{
    public static class EconomyRules
    {
        /// <summary>Placement rule of the typed building kinds: the kind must exist, match the size, and a harvester must stand on the land it takes from.</summary>
        public static CommandResult CheckKind(GodState s, string kind, BuildingSize size, int node)
        {
            if (string.IsNullOrEmpty(kind) || kind == BuildingKinds.Placeholder) return CommandResult.Accept();
            var d = EconomyDefs.Get(kind);
            if (d == null || d.Size != size) return CommandResult.RejectDefault(Reasons.InvalidSite);
            if (d.Role == BuildingRole.Harvester)
            {
                int hex = s.Territory.Grid.HexOf(node);
                if (hex < 0 || RawKinds.OfTerrain(s.Board.HexTerrain[hex]) != d.Harvests) return CommandResult.RejectDefault(Reasons.WrongTerrain);
            }
            return CommandResult.Accept();
        }
    }
}
