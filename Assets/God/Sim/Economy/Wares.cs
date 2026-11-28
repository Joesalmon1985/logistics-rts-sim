namespace God.Sim
{
    /// <summary>
    /// Every ware. The first four share their numbers with <see cref="RawKind"/> (what a pile holds). Rock is a raw
    /// pile kind that is never hauled: the quarry turns it into stone and the mine into ore on the spot.
    /// Stage 3: the five hauled raws are log, clay, grain, stone and ore. Each pair of raws makes one processed ware
    /// (ten in all, see <see cref="Wares.Processed"/>), and processed wares make the ten pieces of gear (five attack,
    /// five defence, see <see cref="Gear"/>). <see cref="Kindling"/> is from stage 2: no building makes or takes it any more
    /// (the value stays so ware numbers and arrays keep their size).
    /// </summary>
    public enum Ware
    {
        None = 0,
        // ---- raw ----
        Log = 1,
        Clay = 2,
        Grain = 3,
        Rock = 4,
        Stone = 5,
        Ore = 6,
        // ---- processed (one per pair of raws) ----
        /// <summary>Log + stone (sawmill).</summary>
        Plank = 7,
        /// <summary>Stage 2 only: unused since stage 3 (nothing makes or takes it). Kept so the numbers of the other wares stay.</summary>
        Kindling = 8,
        /// <summary>Log + clay (brick kiln).</summary>
        Brick = 9,
        /// <summary>Log + ore (smelter).</summary>
        Iron = 10,
        /// <summary>Log + grain (bakery).</summary>
        Bread = 11,
        /// <summary>Clay + stone (mortar mill).</summary>
        Mortar = 12,
        /// <summary>Clay + ore (bronze foundry).</summary>
        Bronze = 13,
        /// <summary>Clay + grain (pottery).</summary>
        Pottery = 14,
        /// <summary>Stone + ore (steel forge).</summary>
        Steel = 15,
        /// <summary>Grain + stone (tannery).</summary>
        Leather = 16,
        /// <summary>Grain + ore (weaver).</summary>
        Cloth = 17,
        // ---- attack gear ----
        Sword = 18,
        Spear = 19,
        Axe = 20,
        Bow = 21,
        Sling = 22,
        // ---- defence gear ----
        BigShield = 23,
        Helmet = 24,
        SmallShield = 25,
        Breastplate = 26,
        LightArmour = 27,
        // ---- stage 4 M3: foraged food (ECONOMY_DESIGN 3.2). Personal goods of NPCs only: beyond Wares.Count, so the stage 3
        //      economy's per-ware arrays (and every stage 3 hash) are unchanged. ----
        Berries = 28,
        Roots = 29,
        Fish = 30,
        /// <summary>A bitter, mildly toxic berry: food that hurts (NPC_LEARNING L4). Only test scenarios place it by default.</summary>
        Nightshade = 31,
        // ---- F1.17 (contracts 7.1): the one new personal ware of this run (sources and remedy use: C7). 33-39 free for later
        //      appends without another ledger layout change. ----
        Herbs = 32,
        // ---- 40-71 reserved for industry (contracts 22.3). ----
        // ---- UN0 (contracts 23.4): the unification block 72-159. UA wares fixed now (catalogue OBJ-nnn in the comments; "u" =
        //      unique, never merges); 94-159 are appended by UN3a/UB in manifest order. They exist only in unified games. ----
        Mushroom = 72,        // OBJ-044
        WildEgg = 73,         // OBJ-046
        Rabbit = 74,          // OBJ-047
        WildVeg = 75,         // OBJ-048
        CactusFruit = 76,     // OBJ-049
        Seaweed = 77,         // OBJ-050
        Hay = 78,             // OBJ-043
        Sheep = 79,           // OBJ-042 (u)
        Water = 80,           // OBJ-039 (container-only, UB)
        CookedFish = 81,      // OBJ-054
        Bedding = 82,         // OBJ-084
        Bed = 83,             // OBJ-092 (u)
        Stool = 84,           // OBJ-097 (u)
        Bench = 85,           // OBJ-127 (u)
        Toy = 86,             // OBJ-132 (u)
        CarvedOrnament = 87,  // OBJ-108 (u)
        StorageChest = 88,    // OBJ-107 (u)
        Cradle = 89,          // OBJ-103 (u)
        Latrine = 90,         // OBJ-136 (u)
        Waste = 91,           // OBJ-114
        StoneHammer = 92,     // OBJ-068 (u)
        SleepingMat = 93,     // OBJ-093 (u; founder endowment in UA)
    }

    /// <summary>How urgently a consumer wants one input ware. Stop means it asks for none.</summary>
    public enum Priority
    {
        Stop = 0,
        Low = 1,
        Normal = 2,
        High = 3,
    }

    public static class Wares
    {
        /// <summary>The stage 3 wares (the economy's per-ware arrays). The personal food wares of M3 lie beyond it.</summary>
        public const int Count = 28;

        /// <summary>
        /// Every ware including the personal goods of the population (M3): the population ledger's array length. 40 since F1.17
        /// (32 before): the population hash keeps the old 32-ware order and adds wares 32-39 only when any of them is non-zero.
        /// <para>UN0 (contracts 23.4): 160, so the unification block 72-159 fits (40-71 stay reserved for industry). The population
        /// part hashes wares 40-159 (and ledger reasons 8-15) only when any of them is non-zero, so every older game hashes as before.</para>
        /// </summary>
        public const int PersonalCount = 160;

        /// <summary>The ledger width the population hash was laid out for before F1.17 (wares 0-31).</summary>
        public const int LegacyPersonalCount = 32;

        /// <summary>
        /// UN0: the personal-ware width of F1 (wares 0-39): the hash layout of the HERB block and the bound the legacy divine
        /// targets keep (a Give naming ware 40+ is refused exactly as before).
        /// </summary>
        public const int F1PersonalCount = 40;

        /// <summary>UN0 (contracts 23.4): the first and last ware of the unification block.</summary>
        public const int UnifiedFirst = 72, UnifiedLast = 159;

        /// <summary>
        /// UN0 (contracts 23.4): a durable object that never merges (count 1 per lot): the axe and every "u" ware of the
        /// unification block. The legacy merge rule keeps its own test (axe only), so this changes nothing in a legacy game.
        /// </summary>
        public static bool IsUnique(Ware w)
        {
            switch (w)
            {
                case Ware.Axe: case Ware.Sheep: case Ware.Bed: case Ware.Stool: case Ware.Bench: case Ware.Toy: case Ware.CarvedOrnament:
                case Ware.StorageChest: case Ware.Cradle: case Ware.Latrine: case Ware.StoneHammer: case Ware.SleepingMat:
                    return true;
                default: return false;
            }
        }

        public static bool IsFood(Ware w) => w == Ware.Berries || w == Ware.Roots || w == Ware.Fish || w == Ware.Nightshade;

        /// <summary>The five raws that carriers haul (rock is never hauled).</summary>
        public static readonly ReadOnlyArray<Ware> Raws = new ReadOnlyArray<Ware>(Ware.Log, Ware.Clay, Ware.Grain, Ware.Stone, Ware.Ore);

        /// <summary>The ten processed wares of stage 3, one per pair of raws.</summary>
        public static readonly ReadOnlyArray<Ware> Processed = new ReadOnlyArray<Ware>(Ware.Plank, Ware.Brick, Ware.Iron, Ware.Bread, Ware.Mortar, Ware.Bronze, Ware.Pottery, Ware.Steel, Ware.Leather, Ware.Cloth);

        public static Ware OfRaw(RawKind k) => (Ware)(int)k;

        public static bool IsRaw(Ware w) => w == Ware.Log || w == Ware.Clay || w == Ware.Grain || w == Ware.Stone || w == Ware.Ore;
        public static bool IsProcessed(Ware w) => Processed.IndexOf(w) >= 0;
        public static bool IsGear(Ware w) => w >= Ware.Sword && w <= Ware.LightArmour;

        public static string Name(Ware w)
        {
            switch (w)
            {
                case Ware.Log: return "log";
                case Ware.Clay: return "clay";
                case Ware.Grain: return "grain";
                case Ware.Rock: return "rock";
                case Ware.Stone: return "stone";
                case Ware.Ore: return "ore";
                case Ware.Plank: return "plank";
                case Ware.Kindling: return "kindling";
                case Ware.Brick: return "brick";
                case Ware.Iron: return "iron";
                case Ware.Bread: return "bread";
                case Ware.Mortar: return "mortar";
                case Ware.Bronze: return "bronze";
                case Ware.Pottery: return "pottery";
                case Ware.Steel: return "steel";
                case Ware.Leather: return "leather";
                case Ware.Cloth: return "cloth";
                case Ware.Sword: return "sword";
                case Ware.Spear: return "spear";
                case Ware.Axe: return "axe";
                case Ware.Bow: return "bow";
                case Ware.Sling: return "sling";
                case Ware.BigShield: return "big shield";
                case Ware.Helmet: return "helmet";
                case Ware.SmallShield: return "small shield";
                case Ware.Breastplate: return "breastplate";
                case Ware.LightArmour: return "light armour";
                case Ware.Berries: return "berries";
                case Ware.Roots: return "roots";
                case Ware.Fish: return "fish";
                case Ware.Nightshade: return "nightshade";
                case Ware.Herbs: return "herbs";
                // UN0: the unification block (names for reports; display names come from the catalogue)
                case Ware.Mushroom: return "mushroom";
                case Ware.WildEgg: return "wild egg";
                case Ware.Rabbit: return "rabbit";
                case Ware.WildVeg: return "wild vegetables";
                case Ware.CactusFruit: return "cactus fruit";
                case Ware.Seaweed: return "seaweed";
                case Ware.Hay: return "hay";
                case Ware.Sheep: return "sheep";
                case Ware.Water: return "water";
                case Ware.CookedFish: return "cooked fish";
                case Ware.Bedding: return "bedding";
                case Ware.Bed: return "bed";
                case Ware.Stool: return "stool";
                case Ware.Bench: return "bench";
                case Ware.Toy: return "toy";
                case Ware.CarvedOrnament: return "carved ornament";
                case Ware.StorageChest: return "storage chest";
                case Ware.Cradle: return "cradle";
                case Ware.Latrine: return "latrine";
                case Ware.Waste: return "waste";
                case Ware.StoneHammer: return "stone hammer";
                case Ware.SleepingMat: return "sleeping mat";
                default: return "none";
            }
        }
    }

    /// <summary>Where a piece of gear is worn. A soldier carries one weapon and at most one item per other slot.</summary>
    public enum GearSlot { None = 0, Weapon = 1, Shield = 2, Head = 3, Body = 4 }

    /// <summary>Gear rules shared by the economy (barracks) and the military (combat). Stats live in the military tuning.</summary>
    public static class Gear
    {
        public static readonly ReadOnlyArray<Ware> Attack = new ReadOnlyArray<Ware>(Ware.Sword, Ware.Spear, Ware.Axe, Ware.Bow, Ware.Sling);
        public static readonly ReadOnlyArray<Ware> Defence = new ReadOnlyArray<Ware>(Ware.BigShield, Ware.Helmet, Ware.SmallShield, Ware.Breastplate, Ware.LightArmour);

        public static GearSlot SlotOf(Ware w)
        {
            switch (w)
            {
                case Ware.Sword: case Ware.Spear: case Ware.Axe: case Ware.Bow: case Ware.Sling: return GearSlot.Weapon;
                case Ware.BigShield: case Ware.SmallShield: return GearSlot.Shield;
                case Ware.Helmet: return GearSlot.Head;
                case Ware.Breastplate: case Ware.LightArmour: return GearSlot.Body;
                default: return GearSlot.None;
            }
        }

        /// <summary>A bow needs both hands: no shield with it. Every other weapon takes a shield.</summary>
        public static bool AllowsShield(Ware weapon) => weapon != Ware.Bow;

        public static bool IsRanged(Ware weapon) => weapon == Ware.Bow || weapon == Ware.Sling;
    }

    /// <summary>
    /// What one soldier carries. Weapon is always set (a soldier needs one). Shield, Head and Body are
    /// <see cref="Ware.None"/> when the barracks had none of that slot in stock.
    /// </summary>
    public struct Kit
    {
        public Ware Weapon;
        public Ware Shield;
        public Ware Head;
        public Ware Body;

        public Kit(Ware weapon, Ware shield = Ware.None, Ware head = Ware.None, Ware body = Ware.None)
        { Weapon = weapon; Shield = shield; Head = head; Body = body; }

        public bool IsArmed => Gear.SlotOf(Weapon) == GearSlot.Weapon;

        public override string ToString()
        {
            var s = Wares.Name(Weapon);
            if (Shield != Ware.None) s += " + " + Wares.Name(Shield);
            if (Head != Ware.None) s += " + " + Wares.Name(Head);
            if (Body != Ware.None) s += " + " + Wares.Name(Body);
            return s;
        }
    }
}
