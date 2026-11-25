using System.Collections.Generic;

namespace God.Sim
{
    public enum BuildingRole
    {
        Hq = 0,
        /// <summary>Takes piles from its hex and gives a ware.</summary>
        Harvester = 1,
        /// <summary>Turns input wares into output wares (processors and armouries, see <see cref="BuildingDef.Armoury"/>).</summary>
        Workshop = 2,
        /// <summary>Turns input wares into a unit order (the barracks).</summary>
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
        /// <summary>
        /// Inputs per cycle. The barracks lists bread and every piece of gear with 1 here, so each can be asked for and
        /// given a priority; a cycle needs bread and one weapon only (see <see cref="BarracksRules.Assemble"/>).
        /// </summary>
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
        /// <summary>Workshops: the single ware it makes (<see cref="Ware.None"/> for other roles).</summary>
        public Ware Makes;
        /// <summary>Stage 3: a workshop that makes gear (one of <see cref="BuildingKinds.Armouries"/>). Processors are workshops without it.</summary>
        public bool Armoury;

        public int CostTotal { get { int n = 0; foreach (int c in Cost) n += c; return n; } }

        /// <summary>A processor: a workshop that turns two raws into one processed ware.</summary>
        public bool Processor => Role == BuildingRole.Workshop && !Armoury;
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

        /// <summary>Harvester upgrades by the level they lead to: [2] is level 1 to 2, [3] is level 2 to 3 (cost and work time).</summary>
        private static readonly BuildingDef[] HarvesterLevels = new BuildingDef[EconomyTuning.MaxHarvesterLevel + 1];

        static EconomyDefs()
        {
            Hq = new BuildingDef { Kind = TerritoryTuning.HqKind, Label = "HQ", Role = BuildingRole.Hq, Size = BuildingSize.Hq, BuildTicks = EconomyTuning.BuildTicksHq };
            Hq.Cost[(int)Ware.Plank] = EconomyTuning.HqPlank; Hq.Cost[(int)Ware.Brick] = EconomyTuning.HqBrick;
            Hq.Cost[(int)Ware.Mortar] = EconomyTuning.HqMortar; Hq.Cost[(int)Ware.Stone] = EconomyTuning.HqStone;
            Citadel = new BuildingDef { Kind = CitadelKind, Label = "Citadel", Role = BuildingRole.Hq, Size = BuildingSize.Hq, BuildTicks = FactionTuning.CitadelBuildTicks };
            Citadel.Cost[(int)Ware.Brick] = FactionTuning.CitadelBrick; Citadel.Cost[(int)Ware.Mortar] = FactionTuning.CitadelMortar;
            Citadel.Cost[(int)Ware.Plank] = FactionTuning.CitadelPlank; Citadel.Cost[(int)Ware.Steel] = FactionTuning.CitadelSteel;

            var l2 = new BuildingDef { Kind = "harvester_level_2", Label = "Level 2", Role = BuildingRole.Harvester, BuildTicks = EconomyTuning.UpgradeWorkTicks };
            l2.Cost[(int)Ware.Plank] = EconomyTuning.Level2Plank; l2.Cost[(int)Ware.Brick] = EconomyTuning.Level2Brick;
            var l3 = new BuildingDef { Kind = "harvester_level_3", Label = "Level 3", Role = BuildingRole.Harvester, BuildTicks = EconomyTuning.UpgradeWorkTicks };
            l3.Cost[(int)Ware.Mortar] = EconomyTuning.Level3Mortar; l3.Cost[(int)Ware.Iron] = EconomyTuning.Level3Iron; l3.Cost[(int)Ware.Bread] = EconomyTuning.Level3Bread;
            HarvesterLevels[2] = l2; HarvesterLevels[3] = l3;

            // ---- harvesters (the farm keeps its medium plot; all cost 2 log, 1 stone) ----
            Harvester(BuildingKinds.Woodcutter, "Woodcutter", RawKind.Log, Ware.Log, BuildingSize.Small);
            Harvester(BuildingKinds.ClayPit, "Clay pit", RawKind.Clay, Ware.Clay, BuildingSize.Small);
            Harvester(BuildingKinds.Farm, "Farm", RawKind.Grain, Ware.Grain, BuildingSize.Medium);
            Harvester(BuildingKinds.Quarry, "Quarry", RawKind.Rock, Ware.Stone, BuildingSize.Small);
            Harvester(BuildingKinds.Mine, "Mine", RawKind.Rock, Ware.Ore, BuildingSize.Small);

            // ---- processors: one per pair of raws ----
            Processor(BuildingKinds.Sawmill, "Sawmill", Ware.Log, Ware.Stone, Ware.Plank, EconomyTuning.SawmillTicks).Out[(int)Ware.Plank] = EconomyTuning.SawmillPlanks;
            Processor(BuildingKinds.Kiln, "Brick kiln", Ware.Log, Ware.Clay, Ware.Brick, EconomyTuning.KilnTicks);
            Processor(BuildingKinds.Smelter, "Smelter", Ware.Log, Ware.Ore, Ware.Iron, EconomyTuning.SmelterTicks);
            Processor(BuildingKinds.Bakery, "Bakery", Ware.Log, Ware.Grain, Ware.Bread, EconomyTuning.BakeryTicks);
            Processor(BuildingKinds.MortarMill, "Mortar mill", Ware.Clay, Ware.Stone, Ware.Mortar, EconomyTuning.MortarMillTicks);
            Processor(BuildingKinds.Foundry, "Bronze foundry", Ware.Clay, Ware.Ore, Ware.Bronze, EconomyTuning.FoundryTicks);
            Processor(BuildingKinds.Pottery, "Pottery", Ware.Clay, Ware.Grain, Ware.Pottery, EconomyTuning.PotteryTicks);
            Processor(BuildingKinds.Forge, "Steel forge", Ware.Stone, Ware.Ore, Ware.Steel, EconomyTuning.ForgeTicks);
            Processor(BuildingKinds.Tannery, "Tannery", Ware.Grain, Ware.Stone, Ware.Leather, EconomyTuning.TanneryTicks);
            Processor(BuildingKinds.Weaver, "Weaver", Ware.Grain, Ware.Ore, Ware.Cloth, EconomyTuning.WeaverTicks);

            // ---- armouries: one per piece of gear ----
            ArmouryDef(BuildingKinds.Swordsmith, "Swordsmith", Ware.Steel, Ware.Leather, Ware.Sword);
            ArmouryDef(BuildingKinds.Spearmaker, "Spearmaker", Ware.Plank, Ware.Bronze, Ware.Spear);
            ArmouryDef(BuildingKinds.Axesmith, "Axesmith", Ware.Iron, Ware.Plank, Ware.Axe);
            ArmouryDef(BuildingKinds.Bowyer, "Bowyer", Ware.Plank, Ware.Cloth, Ware.Bow);
            ArmouryDef(BuildingKinds.Slingmaker, "Slingmaker", Ware.Leather, Ware.Pottery, Ware.Sling);
            ArmouryDef(BuildingKinds.Shieldwright, "Shieldwright", Ware.Plank, Ware.Leather, Ware.BigShield);
            ArmouryDef(BuildingKinds.Helmsmith, "Helmsmith", Ware.Iron, Ware.Cloth, Ware.Helmet);
            ArmouryDef(BuildingKinds.Bucklersmith, "Bucklersmith", Ware.Bronze, Ware.Leather, Ware.SmallShield);
            ArmouryDef(BuildingKinds.Armourer, "Armourer", Ware.Steel, Ware.Cloth, Ware.Breastplate);
            ArmouryDef(BuildingKinds.Leatherworker, "Leatherworker", Ware.Leather, Ware.Cloth, Ware.LightArmour);

            // ---- the barracks: bread and a weapon per soldier, defence items when it has them ----
            var barracks = Add(BuildingKinds.Barracks, "Barracks", BuildingRole.Factory, EconomyTuning.BarracksTicks, BuildingSize.Medium);
            barracks.In[(int)Ware.Bread] = 1;
            foreach (var g in Gear.Attack) barracks.In[(int)g] = 1;
            foreach (var g in Gear.Defence) barracks.In[(int)g] = 1;
            barracks.Unit = UnitKind.Infantry;
            ArmouryCost(barracks);
        }

        private static BuildingDef Add(string kind, string label, BuildingRole role, int cycle, BuildingSize size)
        {
            var d = new BuildingDef { Kind = kind, Label = label, Role = role, CycleTicks = cycle, Size = size };
            d.BuildTicks = size == BuildingSize.Small ? EconomyTuning.BuildTicksSmall : EconomyTuning.BuildTicksMedium;
            All.Add(d); ByKind[kind] = d;
            return d;
        }

        private static void Harvester(string kind, string label, RawKind raw, Ware give, BuildingSize size)
        {
            var d = Add(kind, label, BuildingRole.Harvester, 0, size);
            d.Harvests = raw; d.HarvestWare = give;
            d.Cost[(int)Ware.Log] = EconomyTuning.SmallLog; d.Cost[(int)Ware.Stone] = EconomyTuning.SmallStone;
        }

        private static BuildingDef Processor(string kind, string label, Ware a, Ware b, Ware make, int cycle)
        {
            var d = Add(kind, label, BuildingRole.Workshop, cycle, BuildingSize.Medium);
            d.In[(int)a] = 1; d.In[(int)b] = 1; d.Out[(int)make] = 1; d.Makes = make;
            d.Cost[(int)Ware.Plank] = EconomyTuning.ProcessorPlank;
            d.Cost[(int)Ware.Brick] = EconomyTuning.ProcessorBrick;
            d.Cost[(int)Ware.Stone] = EconomyTuning.ProcessorStone;
            return d;
        }

        private static void ArmouryDef(string kind, string label, Ware a, Ware b, Ware make)
        {
            var d = Add(kind, label, BuildingRole.Workshop, EconomyTuning.ArmouryTicks, BuildingSize.Medium);
            d.In[(int)a] = 1; d.In[(int)b] = 1; d.Out[(int)make] = 1; d.Makes = make; d.Armoury = true;
            ArmouryCost(d);
        }

        private static void ArmouryCost(BuildingDef d)
        {
            d.Cost[(int)Ware.Plank] = EconomyTuning.ArmouryPlank;
            d.Cost[(int)Ware.Brick] = EconomyTuning.ArmouryBrick;
            d.Cost[(int)Ware.Mortar] = EconomyTuning.ArmouryMortar;
        }

        /// <summary>Def of a building kind, or null for placeholders, HQs and unknown kinds.</summary>
        public static BuildingDef Get(string kind) => kind != null && ByKind.TryGetValue(kind, out var d) ? d : null;

        public static bool IsEconomyKind(string kind) => Get(kind) != null;

        /// <summary>Def of an economy building record: the HQ def for HQs.</summary>
        public static BuildingDef DefOf(string kind) => kind == TerritoryTuning.HqKind ? Hq : Get(kind);

        /// <summary>The harvester upgrade that leads to <paramref name="toLevel"/> (2 or 3): its cost and work time. Null otherwise.</summary>
        public static BuildingDef HarvesterUpgrade(int toLevel) => toLevel >= 2 && toLevel <= EconomyTuning.MaxHarvesterLevel ? HarvesterLevels[toLevel] : null;

        /// <summary>The workshop (processor or armoury) that makes a ware, or null.</summary>
        public static BuildingDef MakerOf(Ware w)
        {
            foreach (var d in All) if (d.Role == BuildingRole.Workshop && d.Makes == w) return d;
            return null;
        }
    }
}
