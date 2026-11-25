using System.Collections.Generic;

namespace God.Sim
{
    public enum BuildingRole
    {
        Hq = 0,
        /// <summary>Takes piles from its hex and gives a ware.</summary>
        Harvester = 1,
        /// <summary>Turns input wares into output wares.</summary>
        Workshop = 2,
        /// <summary>Turns input wares into a unit order.</summary>
        Factory = 3,
    }

    /// <summary>Static data of one building kind. Ware arrays are indexed by <c>(int)Ware</c>.</summary>
    public sealed class BuildingDef
    {
        public string Kind;
        public string Label;
        public BuildingSize Size;
        public BuildingRole Role;
        public int[] Cost = new int[Wares.Count];
        /// <summary>Inputs per cycle.</summary>
        public int[] In = new int[Wares.Count];
        /// <summary>Outputs per cycle.</summary>
        public int[] Out = new int[Wares.Count];
        public int CycleTicks;
        public int BuildTicks;
        /// <summary>Harvesters: the pile kind taken.</summary>
        public RawKind Harvests;
        /// <summary>Harvesters: the ware that results.</summary>
        public Ware HarvestWare;
        /// <summary>Factories: the unit made.</summary>
        public UnitKind Unit;

        public int CostTotal { get { int n = 0; foreach (int c in Cost) n += c; return n; } }
    }

    public static class EconomyDefs
    {
        private static readonly List<BuildingDef> All = new List<BuildingDef>();
        private static readonly Dictionary<string, BuildingDef> ByKind = new Dictionary<string, BuildingDef>();

        public static IReadOnlyList<BuildingDef> Defs => All;

        /// <summary>The def used for an HQ site (cost only).</summary>
        public static readonly BuildingDef Hq;

        /// <summary>The Citadel upgrade of a finished HQ: its cost and work time (see <see cref="UpgradeToCitadel"/>).</summary>
        public static readonly BuildingDef Citadel;

        /// <summary>Kind string of a Citadel upgrade (never a territory building kind: the building stays an HQ).</summary>
        public const string CitadelKind = "citadel";

        static EconomyDefs()
        {
            Hq = new BuildingDef { Kind = TerritoryTuning.HqKind, Label = "HQ", Role = BuildingRole.Hq, Size = BuildingSize.Hq, BuildTicks = EconomyTuning.BuildTicksHq };
            Hq.Cost[(int)Ware.Plank] = EconomyTuning.HqPlank; Hq.Cost[(int)Ware.Stone] = EconomyTuning.HqStone;
            Citadel = new BuildingDef { Kind = CitadelKind, Label = "Citadel", Role = BuildingRole.Hq, Size = BuildingSize.Hq, BuildTicks = FactionTuning.CitadelBuildTicks };
            Citadel.Cost[(int)Ware.Plank] = FactionTuning.CitadelPlank; Citadel.Cost[(int)Ware.Brick] = FactionTuning.CitadelBrick;
            Citadel.Cost[(int)Ware.Stone] = FactionTuning.CitadelStone; Citadel.Cost[(int)Ware.Iron] = FactionTuning.CitadelIron;

            Harvester(BuildingKinds.Woodcutter, "Woodcutter", RawKind.Log, Ware.Log);
            Harvester(BuildingKinds.ClayPit, "Clay pit", RawKind.Clay, Ware.Clay);
            Harvester(BuildingKinds.Quarry, "Quarry", RawKind.Rock, Ware.Stone);
            Harvester(BuildingKinds.Mine, "Mine", RawKind.Rock, Ware.Ore);
            Medium(Harvester(BuildingKinds.Farm, "Farm", RawKind.Grain, Ware.Grain));

            var saw = Add(BuildingKinds.Sawmill, "Sawmill", BuildingRole.Workshop, EconomyTuning.SawmillTicks);
            saw.In[(int)Ware.Log] = 1; saw.Out[(int)Ware.Plank] = 1; saw.Out[(int)Ware.Kindling] = 1; Medium(saw);

            var kiln = Add(BuildingKinds.Kiln, "Kiln", BuildingRole.Workshop, EconomyTuning.KilnTicks);
            kiln.In[(int)Ware.Clay] = 1; kiln.In[(int)Ware.Kindling] = 1; kiln.Out[(int)Ware.Brick] = 1; Medium(kiln);

            var smelter = Add(BuildingKinds.Smelter, "Smelter", BuildingRole.Workshop, EconomyTuning.SmelterTicks);
            smelter.In[(int)Ware.Ore] = 1; smelter.In[(int)Ware.Kindling] = 1; smelter.Out[(int)Ware.Iron] = 1; Medium(smelter);

            var barracks = Add(BuildingKinds.Barracks, "Barracks", BuildingRole.Factory, EconomyTuning.BarracksTicks);
            barracks.In[(int)Ware.Grain] = 1; barracks.In[(int)Ware.Iron] = 1; barracks.Unit = UnitKind.Infantry; Medium(barracks);

            var vw = Add(BuildingKinds.VehicleWorkshop, "Vehicle workshop", BuildingRole.Factory, EconomyTuning.VehicleWorkshopTicks);
            vw.In[(int)Ware.Iron] = 2; vw.In[(int)Ware.Plank] = 1; vw.Unit = UnitKind.Infantry; /* Q2: vehicles removed; Q1 drops this building */ Medium(vw);
        }

        private static BuildingDef Add(string kind, string label, BuildingRole role, int cycle)
        {
            var d = new BuildingDef { Kind = kind, Label = label, Role = role, CycleTicks = cycle, Size = BuildingSize.Medium };
            All.Add(d); ByKind[kind] = d;
            return d;
        }

        private static BuildingDef Harvester(string kind, string label, RawKind raw, Ware give)
        {
            var d = Add(kind, label, BuildingRole.Harvester, 0);
            d.Harvests = raw; d.HarvestWare = give;
            d.Size = BuildingSize.Small;
            d.Cost[(int)Ware.Plank] = EconomyTuning.SmallPlank; d.Cost[(int)Ware.Stone] = EconomyTuning.SmallStone;
            d.BuildTicks = EconomyTuning.BuildTicksSmall;
            return d;
        }

        private static void Medium(BuildingDef d)
        {
            d.Size = BuildingSize.Medium;
            d.Cost[(int)Ware.Plank] = EconomyTuning.MediumPlank;
            d.Cost[(int)Ware.Brick] = EconomyTuning.MediumBrick;
            d.Cost[(int)Ware.Stone] = EconomyTuning.MediumStone;
            d.BuildTicks = EconomyTuning.BuildTicksMedium;
        }

        /// <summary>Def of a building kind, or null for placeholders, HQs and unknown kinds.</summary>
        public static BuildingDef Get(string kind) => kind != null && ByKind.TryGetValue(kind, out var d) ? d : null;

        public static bool IsEconomyKind(string kind) => Get(kind) != null;

        /// <summary>Def of an economy building record: the HQ def for HQs.</summary>
        public static BuildingDef DefOf(string kind) => kind == TerritoryTuning.HqKind ? Hq : Get(kind);
    }
}
