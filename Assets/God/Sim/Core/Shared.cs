using System.Collections.Generic;

namespace God.Sim
{
    /// <summary>
    /// Shared seam between the economy (P2) and the military (P3), agreed before both were built so they can be
    /// written in parallel. Economy fills <see cref="GodState.UnitQueue"/> when a unit factory finishes a cycle;
    /// the military drains it each tick and spawns the unit at that building's flag.
    /// </summary>
    public enum UnitKind { Infantry = 0, Vehicle = 1 }

    public struct UnitOrder
    {
        public Faction Faction;
        /// <summary>Territory building id of the factory that produced it.</summary>
        public int BuildingId;
        public UnitKind Kind;
    }

    /// <summary>Building kind ids stored in <c>Building.Kind</c>. HQs are <c>Hq</c> objects, not buildings.</summary>
    public static class BuildingKinds
    {
        public const string Placeholder = "placeholder";
        public const string Woodcutter = "woodcutter";
        public const string ClayPit = "clay_pit";
        public const string Quarry = "quarry";
        public const string Mine = "mine";
        public const string Farm = "farm";
        public const string Sawmill = "sawmill";
        public const string Kiln = "kiln";
        public const string Smelter = "smelter";
        public const string Barracks = "barracks";
        public const string VehicleWorkshop = "vehicle_workshop";
    }

    /// <summary>System order bands: territory (dice) first, then economy, then military.</summary>
    public static class SystemOrder
    {
        public const int Economy = 200;
        public const int Military = 300;
    }

    public sealed partial class GodState
    {
        /// <summary>Units produced this tick by economy factories, waiting for the military to spawn them.</summary>
        public List<UnitOrder> UnitQueue = new List<UnitOrder>();
    }
}
