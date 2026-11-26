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

    /// <summary>
    /// Static data of one building kind. Ware arrays are indexed by <c>(int)Ware</c>. Frozen: every field is readonly and
    /// the ware amounts are <see cref="ReadOnlyArray{T}"/>, so the shared table cannot be edited (M0 static audit).
    /// Built through <see cref="Draft"/> in <see cref="EconomyDefs"/>'s static constructor.
    /// </summary>
    public sealed class BuildingDef
    {
        public readonly string Kind;
        public readonly string Label;
        public readonly BuildingSize Size;
        public readonly BuildingRole Role;
        public readonly ReadOnlyArray<int> Cost;
        /// <summary>
        /// Inputs per cycle. The barracks lists bread and every piece of gear with 1 here, so each can be asked for and
        /// given a priority; a cycle needs bread and one weapon only (see <see cref="BarracksRules.Assemble"/>).
        /// </summary>
        public readonly ReadOnlyArray<int> In;
        /// <summary>Outputs per cycle.</summary>
        public readonly ReadOnlyArray<int> Out;
        public readonly int CycleTicks;
        public readonly int BuildTicks;
        /// <summary>Harvesters: the pile kind taken.</summary>
        public readonly RawKind Harvests;
        /// <summary>Harvesters: the ware that results.</summary>
        public readonly Ware HarvestWare;
        /// <summary>Factories: the unit made.</summary>
        public readonly UnitKind Unit;
        /// <summary>Workshops: the single ware it makes (<see cref="Ware.None"/> for other roles).</summary>
        public readonly Ware Makes;
        /// <summary>Stage 3: a workshop that makes gear (one of <see cref="BuildingKinds.Armouries"/>). Processors are workshops without it.</summary>
        public readonly bool Armoury;
        /// <summary>Sum of <see cref="Cost"/>.</summary>
        public readonly int CostTotal;

        private BuildingDef(Draft d)
        {
            Kind = d.Kind; Label = d.Label; Size = d.Size; Role = d.Role;
            Cost = new ReadOnlyArray<int>(d.Cost); In = new ReadOnlyArray<int>(d.In); Out = new ReadOnlyArray<int>(d.Out);
            CycleTicks = d.CycleTicks; BuildTicks = d.BuildTicks; Harvests = d.Harvests; HarvestWare = d.HarvestWare;
            Unit = d.Unit; Makes = d.Makes; Armoury = d.Armoury;
            int n = 0; foreach (int c in d.Cost) n += c; CostTotal = n;
        }

        /// <summary>A processor: a workshop that turns two raws into one processed ware.</summary>
        public bool Processor => Role == BuildingRole.Workshop && !Armoury;

        /// <summary>The editable form of a def, used only while the table is built.</summary>
        internal sealed class Draft
        {
            public string Kind;
            public string Label;
            public BuildingSize Size;
            public BuildingRole Role;
            public int[] Cost = new int[Wares.Count];
            public int[] In = new int[Wares.Count];
            public int[] Out = new int[Wares.Count];
            public int CycleTicks;
            public int BuildTicks;
            public RawKind Harvests;
            public Ware HarvestWare;
            public UnitKind Unit;
            public Ware Makes;
            public bool Armoury;

            public BuildingDef Freeze() => new BuildingDef(this);
        }
    }

    public static class EconomyDefs
    {
        /// <summary>Every economy building kind, in build-bar order. Frozen.</summary>
        private static readonly ReadOnlyArray<BuildingDef> All;
        /// <summary>Lookup by kind over <see cref="All"/>: filled once in the static constructor, never written after (allow-listed in the static audit).</summary>
        private static readonly Dictionary<string, BuildingDef> ByKind = new Dictionary<string, BuildingDef>();

        public static IReadOnlyList<BuildingDef> Defs => All;

        /// <summary>The def used for an HQ site (cost only).</summary>
        public static readonly BuildingDef Hq;

        /// <summary>The Citadel upgrade of a finished HQ: its cost and work time (see <see cref="UpgradeToCitadel"/>).</summary>
        public static readonly BuildingDef Citadel;

        /// <summary>Kind string of a Citadel upgrade (never a territory building kind: the building stays an HQ).</summary>
        public const string CitadelKind = "citadel";

        /// <summary>Harvester upgrades by the level they lead to: [2] is level 1 to 2, [3] is level 2 to 3 (cost and work time).</summary>
        private static readonly ReadOnlyArray<BuildingDef> HarvesterLevels;

        static EconomyDefs()
        {
            var hq = new BuildingDef.Draft { Kind = TerritoryTuning.HqKind, Label = "HQ", Role = BuildingRole.Hq, Size = BuildingSize.Hq, BuildTicks = EconomyTuning.BuildTicksHq };
            hq.Cost[(int)Ware.Plank] = EconomyTuning.HqPlank; hq.Cost[(int)Ware.Brick] = EconomyTuning.HqBrick;
            hq.Cost[(int)Ware.Mortar] = EconomyTuning.HqMortar; hq.Cost[(int)Ware.Stone] = EconomyTuning.HqStone;
            Hq = hq.Freeze();
            var citadel = new BuildingDef.Draft { Kind = CitadelKind, Label = "Citadel", Role = BuildingRole.Hq, Size = BuildingSize.Hq, BuildTicks = FactionTuning.CitadelBuildTicks };
            citadel.Cost[(int)Ware.Brick] = FactionTuning.CitadelBrick; citadel.Cost[(int)Ware.Mortar] = FactionTuning.CitadelMortar;
            citadel.Cost[(int)Ware.Plank] = FactionTuning.CitadelPlank; citadel.Cost[(int)Ware.Steel] = FactionTuning.CitadelSteel;
            Citadel = citadel.Freeze();

            var l2 = new BuildingDef.Draft { Kind = "harvester_level_2", Label = "Level 2", Role = BuildingRole.Harvester, BuildTicks = EconomyTuning.UpgradeWorkTicks };
            l2.Cost[(int)Ware.Plank] = EconomyTuning.Level2Plank; l2.Cost[(int)Ware.Brick] = EconomyTuning.Level2Brick;
            var l3 = new BuildingDef.Draft { Kind = "harvester_level_3", Label = "Level 3", Role = BuildingRole.Harvester, BuildTicks = EconomyTuning.UpgradeWorkTicks };
            l3.Cost[(int)Ware.Mortar] = EconomyTuning.Level3Mortar; l3.Cost[(int)Ware.Iron] = EconomyTuning.Level3Iron; l3.Cost[(int)Ware.Bread] = EconomyTuning.Level3Bread;
            HarvesterLevels = new ReadOnlyArray<BuildingDef>(null, null, l2.Freeze(), l3.Freeze());

            var all = new List<BuildingDef.Draft>();
            // ---- harvesters (the farm keeps its medium plot; all cost 2 log, 1 stone) ----
            Harvester(all, BuildingKinds.Woodcutter, "Woodcutter", RawKind.Log, Ware.Log, BuildingSize.Small);
            Harvester(all, BuildingKinds.ClayPit, "Clay pit", RawKind.Clay, Ware.Clay, BuildingSize.Small);
            Harvester(all, BuildingKinds.Farm, "Farm", RawKind.Grain, Ware.Grain, BuildingSize.Medium);
            Harvester(all, BuildingKinds.Quarry, "Quarry", RawKind.Rock, Ware.Stone, BuildingSize.Small);
            Harvester(all, BuildingKinds.Mine, "Mine", RawKind.Rock, Ware.Ore, BuildingSize.Small);

            // ---- processors: one per pair of raws ----
            Processor(all, BuildingKinds.Sawmill, "Sawmill", Ware.Log, Ware.Stone, Ware.Plank, EconomyTuning.SawmillTicks).Out[(int)Ware.Plank] = EconomyTuning.SawmillPlanks;
            Processor(all, BuildingKinds.Kiln, "Brick kiln", Ware.Log, Ware.Clay, Ware.Brick, EconomyTuning.KilnTicks);
            Processor(all, BuildingKinds.Smelter, "Smelter", Ware.Log, Ware.Ore, Ware.Iron, EconomyTuning.SmelterTicks);
            Processor(all, BuildingKinds.Bakery, "Bakery", Ware.Log, Ware.Grain, Ware.Bread, EconomyTuning.BakeryTicks);
            Processor(all, BuildingKinds.MortarMill, "Mortar mill", Ware.Clay, Ware.Stone, Ware.Mortar, EconomyTuning.MortarMillTicks);
            Processor(all, BuildingKinds.Foundry, "Bronze foundry", Ware.Clay, Ware.Ore, Ware.Bronze, EconomyTuning.FoundryTicks);
            Processor(all, BuildingKinds.Pottery, "Pottery", Ware.Clay, Ware.Grain, Ware.Pottery, EconomyTuning.PotteryTicks);
            Processor(all, BuildingKinds.Forge, "Steel forge", Ware.Stone, Ware.Ore, Ware.Steel, EconomyTuning.ForgeTicks);
            Processor(all, BuildingKinds.Tannery, "Tannery", Ware.Grain, Ware.Stone, Ware.Leather, EconomyTuning.TanneryTicks);
            Processor(all, BuildingKinds.Weaver, "Weaver", Ware.Grain, Ware.Ore, Ware.Cloth, EconomyTuning.WeaverTicks);

            // ---- armouries: one per piece of gear ----
            ArmouryDef(all, BuildingKinds.Swordsmith, "Swordsmith", Ware.Steel, Ware.Leather, Ware.Sword);
            ArmouryDef(all, BuildingKinds.Spearmaker, "Spearmaker", Ware.Plank, Ware.Bronze, Ware.Spear);
            ArmouryDef(all, BuildingKinds.Axesmith, "Axesmith", Ware.Iron, Ware.Plank, Ware.Axe);
            ArmouryDef(all, BuildingKinds.Bowyer, "Bowyer", Ware.Plank, Ware.Cloth, Ware.Bow);
            ArmouryDef(all, BuildingKinds.Slingmaker, "Slingmaker", Ware.Leather, Ware.Pottery, Ware.Sling);
            ArmouryDef(all, BuildingKinds.Shieldwright, "Shieldwright", Ware.Plank, Ware.Leather, Ware.BigShield);
            ArmouryDef(all, BuildingKinds.Helmsmith, "Helmsmith", Ware.Iron, Ware.Cloth, Ware.Helmet);
            ArmouryDef(all, BuildingKinds.Bucklersmith, "Bucklersmith", Ware.Bronze, Ware.Leather, Ware.SmallShield);
            ArmouryDef(all, BuildingKinds.Armourer, "Armourer", Ware.Steel, Ware.Cloth, Ware.Breastplate);
            ArmouryDef(all, BuildingKinds.Leatherworker, "Leatherworker", Ware.Leather, Ware.Cloth, Ware.LightArmour);

            // ---- the barracks: bread and a weapon per soldier, defence items when it has them ----
            var barracks = Add(all, BuildingKinds.Barracks, "Barracks", BuildingRole.Factory, EconomyTuning.BarracksTicks, BuildingSize.Medium);
            barracks.In[(int)Ware.Bread] = 1;
            foreach (var g in Gear.Attack) barracks.In[(int)g] = 1;
            foreach (var g in Gear.Defence) barracks.In[(int)g] = 1;
            barracks.Unit = UnitKind.Infantry;
            ArmouryCost(barracks);

            var frozen = new List<BuildingDef>();
            foreach (var d in all) { var f = d.Freeze(); frozen.Add(f); ByKind[f.Kind] = f; }
            All = new ReadOnlyArray<BuildingDef>(frozen);
        }

        private static BuildingDef.Draft Add(List<BuildingDef.Draft> all, string kind, string label, BuildingRole role, int cycle, BuildingSize size)
        {
            var d = new BuildingDef.Draft { Kind = kind, Label = label, Role = role, CycleTicks = cycle, Size = size };
            d.BuildTicks = size == BuildingSize.Small ? EconomyTuning.BuildTicksSmall : EconomyTuning.BuildTicksMedium;
            all.Add(d);
            return d;
        }

        private static void Harvester(List<BuildingDef.Draft> all, string kind, string label, RawKind raw, Ware give, BuildingSize size)
        {
            var d = Add(all, kind, label, BuildingRole.Harvester, 0, size);
            d.Harvests = raw; d.HarvestWare = give;
            d.Cost[(int)Ware.Log] = EconomyTuning.SmallLog; d.Cost[(int)Ware.Stone] = EconomyTuning.SmallStone;
        }

        private static BuildingDef.Draft Processor(List<BuildingDef.Draft> all, string kind, string label, Ware a, Ware b, Ware make, int cycle)
        {
            var d = Add(all, kind, label, BuildingRole.Workshop, cycle, BuildingSize.Medium);
            d.In[(int)a] = 1; d.In[(int)b] = 1; d.Out[(int)make] = 1; d.Makes = make;
            d.Cost[(int)Ware.Plank] = EconomyTuning.ProcessorPlank;
            d.Cost[(int)Ware.Brick] = EconomyTuning.ProcessorBrick;
            d.Cost[(int)Ware.Stone] = EconomyTuning.ProcessorStone;
            return d;
        }

        private static void ArmouryDef(List<BuildingDef.Draft> all, string kind, string label, Ware a, Ware b, Ware make)
        {
            var d = Add(all, kind, label, BuildingRole.Workshop, EconomyTuning.ArmouryTicks, BuildingSize.Medium);
            d.In[(int)a] = 1; d.In[(int)b] = 1; d.Out[(int)make] = 1; d.Makes = make; d.Armoury = true;
            ArmouryCost(d);
        }

        private static void ArmouryCost(BuildingDef.Draft d)
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
