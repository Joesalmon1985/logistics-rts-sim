using System.Collections.Generic;

namespace God.Sim
{
    /// <summary>
    /// Shared seam between the economy (P2) and the military (P3), agreed before both were built so they can be
    /// written in parallel. Economy fills <see cref="GodState.UnitQueue"/> when a unit factory finishes a cycle;
    /// the military drains it each tick and spawns the unit at that building's flag.
    /// </summary>
    /// <summary>Stage 3: every unit is a soldier (infantry); what it fights with is its <see cref="Kit"/>. Vehicles are gone.</summary>
    public enum UnitKind { Infantry = 0 }

    public struct UnitOrder
    {
        public Faction Faction;
        /// <summary>Territory building id of the factory that produced it.</summary>
        public int BuildingId;
        public UnitKind Kind;
        /// <summary>Stage 3: the gear the barracks put on this soldier (Weapon is None for stage 2 units).</summary>
        public Kit Kit;
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
        /// <summary>Stage 2 only: no building def since stage 3 (no vehicles are made). Kept for old views until they drop it.</summary>
        public const string VehicleWorkshop = "vehicle_workshop";

        // ---- stage 3 processors (one per pair of raws; sawmill, kiln and smelter above are reused) ----
        public const string Bakery = "bakery";
        public const string MortarMill = "mortar_mill";
        public const string Foundry = "foundry";
        public const string Pottery = "pottery";
        public const string Forge = "forge";
        public const string Tannery = "tannery";
        public const string Weaver = "weaver";

        // ---- stage 3 armouries (one per piece of gear) ----
        public const string Swordsmith = "swordsmith";
        public const string Spearmaker = "spearmaker";
        public const string Axesmith = "axesmith";
        public const string Bowyer = "bowyer";
        public const string Slingmaker = "slingmaker";
        public const string Shieldwright = "shieldwright";
        public const string Helmsmith = "helmsmith";
        public const string Bucklersmith = "bucklersmith";
        public const string Armourer = "armourer";
        public const string Leatherworker = "leatherworker";

        /// <summary>Stage 3 groups (for the build bar and the AI).</summary>
        public static readonly string[] Harvesters = { Woodcutter, ClayPit, Farm, Quarry, Mine };
        public static readonly string[] Processors = { Sawmill, Kiln, Smelter, Bakery, MortarMill, Foundry, Pottery, Forge, Tannery, Weaver };
        public static readonly string[] Armouries = { Swordsmith, Spearmaker, Axesmith, Bowyer, Slingmaker, Shieldwright, Helmsmith, Bucklersmith, Armourer, Leatherworker };
    }

    /// <summary>System order bands: territory (dice) first, then economy, then military.</summary>
    public static class SystemOrder
    {
        public const int Economy = 200;
        public const int Military = 300;
        /// <summary>The faction system (eliminations, game end), see <see cref="FactionTuning.SystemOrder"/>.</summary>
        public const int Factions = FactionTuning.SystemOrder;
    }

    public sealed partial class GodState
    {
        /// <summary>Units produced this tick by economy factories, waiting for the military to spawn them.</summary>
        public List<UnitOrder> UnitQueue = new List<UnitOrder>();
    }
}
