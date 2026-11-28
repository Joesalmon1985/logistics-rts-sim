namespace God.Sim.Unified
{
    /// <summary>
    /// UN0 (contracts 23.4, 23.7): a catalogue definition id: objects <c>1000 + nnn</c> (OBJ-nnn), buildings <c>2000 + nnn</c>
    /// (BLD-nnn). Immutable once assigned; aliases map names to ids; a definition id, never an instance id.
    /// </summary>
    public static class CatId
    {
        public const ushort None = 0, ObjectBase = 1000, BuildingBase = 2000, ObjectMax = 1999, BuildingMax = 2999;

        public static ushort Object(int nnn) => (ushort)(ObjectBase + nnn);
        public static ushort Building(int nnn) => (ushort)(BuildingBase + nnn);
        public static bool IsObject(ushort id) => id > ObjectBase && id <= ObjectMax;
        public static bool IsBuilding(ushort id) => id > BuildingBase && id <= BuildingMax;
        /// <summary>The workbook row number (OBJ-nnn or BLD-nnn), 0 for none.</summary>
        public static int Row(ushort id) => IsObject(id) ? id - ObjectBase : IsBuilding(id) ? id - BuildingBase : 0;
        /// <summary>"OBJ-044", "BLD-012" (the manifest's form).</summary>
        public static string Code(ushort id) => IsObject(id) ? "OBJ-" + Row(id).ToString("D3") : IsBuilding(id) ? "BLD-" + Row(id).ToString("D3") : "none";
    }

    /// <summary>
    /// UN0 (contracts 23.7): the physics effect kinds a catalogue interaction row may name. Append-only; 21-63 reserved.
    /// Owners: UN1 (1, 2), UN3b (3-11, 19, 20), UN4 (12-14), UN7 (15), UN8a (16-18). Dispatch: <see cref="EffectRegistry"/>.
    /// </summary>
    public enum EffectKind : byte
    {
        None = 0, Forage = 1, DrinkWash = 2, Eat = 3, NeedRelief = 4, CollectRaw = 5, Deposit = 6, Install = 7, Occupy = 8,
        WorkInput = 9, RecipeWork = 10, Fuel = 11, PlaceSite = 12, Contribute = 13, Labour = 14, HoldOut = 15, Strike = 16,
        Equip = 17, Treat = 18, Relieve = 19, Play = 20,
    }

    /// <summary>UN0 (contracts 23.7): what the actor must hold to use an interaction row.</summary>
    public enum SourceReq : byte { EmptyHand = 0, Ware = 1, Class = 2, Tool = 3, Any = 4 }

    /// <summary>UN0 (contracts 23.7): one effect of an interaction row: kind and three data arguments (meaning per kind, owner's doc).</summary>
    public readonly struct EffectDef
    {
        public readonly EffectKind Kind;
        public readonly short A, B, C;

        public EffectDef(EffectKind kind, short a = 0, short b = 0, short c = 0) { Kind = kind; A = a; B = b; C = c; }
    }

    /// <summary>
    /// UN0 (contracts 23.7): one interaction row: <c>USE(source, target, slot)</c> matches it when the target's kind and catalogue
    /// id equal <see cref="TargetKind"/>/<see cref="TargetCat"/> (<see cref="CatId.None"/> = any of that kind), the slot matches (0
    /// = first matching row) and the source meets <see cref="Source"/> (<see cref="SourceArg"/>: the ware, class or tool CatId).
    /// <see cref="Reach"/> in lattice nodes, <see cref="Ticks"/> the duration.
    /// </summary>
    public sealed class InteractionDef
    {
        public readonly TargetKind TargetKind;
        public readonly ushort TargetCat;
        public readonly byte Slot;
        public readonly SourceReq Source;
        public readonly int SourceArg;
        public readonly byte Reach;
        public readonly int Ticks;
        public readonly ReadOnlyArray<EffectDef> Effects;

        public InteractionDef(TargetKind targetKind, ushort targetCat, byte slot, SourceReq source, int sourceArg, byte reach, int ticks, params EffectDef[] effects)
        {
            TargetKind = targetKind; TargetCat = targetCat; Slot = slot; Source = source; SourceArg = sourceArg; Reach = reach; Ticks = ticks;
            Effects = new ReadOnlyArray<EffectDef>(effects ?? new EffectDef[0]);
        }
    }

    /// <summary>A ware and a count (a material, a recipe input or output).</summary>
    public readonly struct WareQty
    {
        public readonly Ware Ware;
        public readonly short Count;

        public WareQty(Ware ware, short count) { Ware = ware; Count = count; }
    }

    /// <summary>A tool's speed multiplier (permille) for one interaction slot kind.</summary>
    public readonly struct ToolMul
    {
        public readonly EffectKind Effect;
        public readonly short Permille;

        public ToolMul(EffectKind effect, short permille) { Effect = effect; Permille = permille; }
    }

    /// <summary>UN0 (contracts 23.7): an object type (OBJ-nnn): its ware, physical numbers and the interactions it offers.</summary>
    public sealed class ObjectDef
    {
        public readonly ushort CatId;
        public readonly Ware Ware;
        public readonly bool Unique;
        public readonly short Weight;
        public readonly short Nutrition;
        public readonly short StrikeAttack;
        public readonly short Defence;
        public readonly ReadOnlyArray<ToolMul> ToolMultiplier;
        public readonly ReadOnlyArray<InteractionDef> Interactions;

        public ObjectDef(ushort catId, Ware ware, bool unique, short weight, short nutrition, short strikeAttack, short defence,
            ToolMul[] toolMultiplier, InteractionDef[] interactions)
        {
            CatId = catId; Ware = ware; Unique = unique; Weight = weight; Nutrition = nutrition; StrikeAttack = strikeAttack; Defence = defence;
            ToolMultiplier = new ReadOnlyArray<ToolMul>(toolMultiplier ?? new ToolMul[0]);
            Interactions = new ReadOnlyArray<InteractionDef>(interactions ?? new InteractionDef[0]);
        }
    }

    /// <summary>UN0 (contracts 23.7): one level of a building: labour, materials and what the finished level offers.</summary>
    public sealed class BuildingLevelDef
    {
        public readonly int Labour;
        public readonly ReadOnlyArray<WareQty> Materials;
        public readonly int Storage;
        public readonly byte SleepSpots;
        public readonly byte FixtureSlots;
        public readonly Ware CollectWare;
        public readonly byte CollectRadius;

        public BuildingLevelDef(int labour, WareQty[] materials, int storage, byte sleepSpots, byte fixtureSlots, Ware collectWare, byte collectRadius)
        {
            Labour = labour; Materials = new ReadOnlyArray<WareQty>(materials ?? new WareQty[0]); Storage = storage; SleepSpots = sleepSpots;
            FixtureSlots = fixtureSlots; CollectWare = collectWare; CollectRadius = collectRadius;
        }
    }

    /// <summary>UN0 (contracts 23.7): where a design may be placed (the row rule; UN4 evaluates it).</summary>
    public enum PlacementRule : byte { Any = 0, Land = 1, Coast = 2, Terrain = 3, InSettlement = 4, Settlement = 5, Road = 6 }

    /// <summary>UN0 (contracts 23.7): what a finished building physically offers (flags).</summary>
    [System.Flags]
    public enum Capabilities : ushort
    {
        None = 0, Storage = 1, Sleep = 2, Fixtures = 4, Workstation = 8, Collect = 16, Hearth = 32, Water = 64, Sanitation = 128, Shelter = 256,
    }

    /// <summary>UN0 (contracts 23.7): a recipe at a station: inputs, outputs, labour ticks and the tool bonus (permille).</summary>
    public sealed class RecipeDef
    {
        public readonly ushort Id;
        public readonly ushort Station;
        public readonly ReadOnlyArray<WareQty> Inputs;
        public readonly ReadOnlyArray<WareQty> Outputs;
        public readonly int LabourTicks;
        public readonly short ToolBonus;

        public RecipeDef(ushort id, ushort station, WareQty[] inputs, WareQty[] outputs, int labourTicks, short toolBonus)
        {
            Id = id; Station = station; Inputs = new ReadOnlyArray<WareQty>(inputs ?? new WareQty[0]); Outputs = new ReadOnlyArray<WareQty>(outputs ?? new WareQty[0]);
            LabourTicks = labourTicks; ToolBonus = toolBonus;
        }
    }

    /// <summary>UN0 (contracts 23.7): a building type (BLD-nnn): levels, placement, capabilities, interactions and recipes.</summary>
    public sealed class BuildingDef
    {
        public readonly ushort CatId;
        public readonly ReadOnlyArray<BuildingLevelDef> Levels;
        public readonly PlacementRule Placement;
        /// <summary>The argument of the placement rule (a terrain kind for <see cref="PlacementRule.Terrain"/>).</summary>
        public readonly byte PlacementArg;
        public readonly Capabilities Capabilities;
        public readonly ReadOnlyArray<InteractionDef> Interactions;
        public readonly ReadOnlyArray<RecipeDef> Recipes;

        public BuildingDef(ushort catId, BuildingLevelDef[] levels, PlacementRule placement, byte placementArg, Capabilities capabilities,
            InteractionDef[] interactions, RecipeDef[] recipes)
        {
            CatId = catId; Levels = new ReadOnlyArray<BuildingLevelDef>(levels ?? new BuildingLevelDef[0]); Placement = placement; PlacementArg = placementArg;
            Capabilities = capabilities; Interactions = new ReadOnlyArray<InteractionDef>(interactions ?? new InteractionDef[0]);
            Recipes = new ReadOnlyArray<RecipeDef>(recipes ?? new RecipeDef[0]);
        }
    }
}
