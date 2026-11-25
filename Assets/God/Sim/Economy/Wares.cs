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
        public const int Count = 28;

        /// <summary>The five raws that carriers haul (rock is never hauled).</summary>
        public static readonly Ware[] Raws = { Ware.Log, Ware.Clay, Ware.Grain, Ware.Stone, Ware.Ore };

        /// <summary>The ten processed wares of stage 3, one per pair of raws.</summary>
        public static readonly Ware[] Processed = { Ware.Plank, Ware.Brick, Ware.Iron, Ware.Bread, Ware.Mortar, Ware.Bronze, Ware.Pottery, Ware.Steel, Ware.Leather, Ware.Cloth };

        public static Ware OfRaw(RawKind k) => (Ware)(int)k;

        public static bool IsRaw(Ware w) => w == Ware.Log || w == Ware.Clay || w == Ware.Grain || w == Ware.Stone || w == Ware.Ore;
        public static bool IsProcessed(Ware w) => System.Array.IndexOf(Processed, w) >= 0;
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
                default: return "none";
            }
        }
    }

    /// <summary>Where a piece of gear is worn. A soldier carries one weapon and at most one item per other slot.</summary>
    public enum GearSlot { None = 0, Weapon = 1, Shield = 2, Head = 3, Body = 4 }

    /// <summary>Gear rules shared by the economy (barracks) and the military (combat). Stats live in the military tuning.</summary>
    public static class Gear
    {
        public static readonly Ware[] Attack = { Ware.Sword, Ware.Spear, Ware.Axe, Ware.Bow, Ware.Sling };
        public static readonly Ware[] Defence = { Ware.BigShield, Ware.Helmet, Ware.SmallShield, Ware.Breastplate, Ware.LightArmour };

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
