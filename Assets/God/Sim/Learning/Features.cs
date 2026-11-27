namespace God.Sim.Learning
{
    /// <summary>
    /// Integer helpers of the learning core (NPC_LEARNING_DESIGN 3.5). <see cref="Shr"/> shifts towards zero so positive
    /// and negative weights decay at the same rate (C#'s <c>&gt;&gt;</c> on a negative int rounds towards minus infinity).
    /// </summary>
    public static class Fx
    {
        public const int WeightCap = 4000;

        public static int Shr(int x, int k)
        {
            if (k >= 31) return 0;
            return x >= 0 ? x >> k : -((-x) >> k);
        }

        /// <summary><c>a * b / c</c> with a long intermediate, truncated towards zero.</summary>
        public static int MulDiv(int a, int b, int c) => c == 0 ? 0 : (int)((long)a * b / c);

        public static short Clamp16(int v) => (short)(v > WeightCap ? WeightCap : v < -WeightCap ? -WeightCap : v);

        public static int Clamp(int v, int lo, int hi) => v < lo ? lo : v > hi ? hi : v;

        public static int Abs(int v) => v < 0 ? -v : v;
    }

    /// <summary>The kinds of context feature (NPC_LEARNING_DESIGN 3.1). M3 uses 1-6, 11-13, 15, 16 and Conj.</summary>
    public enum FeatureKind : byte
    {
        None = 0,
        DriveBand = 1,
        DriveActive = 2,
        Self = 3,
        ObjClass = 4,
        ObjCategory = 5,
        ObjState = 6,
        NpcRel = 7,
        NpcGroup = 8,
        NpcRef = 9,
        MsgAct = 10,
        Place = 11,
        PlaceRef = 12,
        Role = 13,
        Posting = 14,
        Situation = 15,
        Time = 16,
        /// <summary>F1 (contracts 14): divine context (D2/D6), values <see cref="DivineFeatureValue"/>.</summary>
        Divine = 17,
        /// <summary>F1 (contracts 14): organisation context (C6). 19-31 reserved.</summary>
        Org = 18,
        Conj = 32,
    }

    /// <summary>The drives of M3 (POPULATION_DESIGN "Drives"), as indices into a drive array.</summary>
    public static class Drive
    {
        public const int Hunger = 0, Fatigue = 1, Cold = 2, Fear = 3, Pain = 4;
        public const int Count = 5;
        public static readonly ReadOnlyArray<string> Names = new ReadOnlyArray<string>("hunger", "fatigue", "cold", "fear", "pain");
    }

    /// <summary><see cref="FeatureKind.Self"/> values.</summary>
    public static class SelfFeature
    {
        public const int CarryingFood = 1, CarryingLog = 2, CarryingTool = 3, Injured = 4, HasBed = 5, HasSite = 6, NoBed = 7, CarryFull = 8;
        // ---- F1 (contracts 14): blocks. Divinity 48-55, Orgs 56-63, Civ 64-71; 72+ reserved ----
        public const int DivinityFirst = 48, DivinityLast = 55, OrgsFirst = 56, OrgsLast = 63, CivFirst = 64, CivLast = 71;
        /// <summary>Divinity values (D3, D4, D2), landed by F1 so the numbers are fixed.</summary>
        public const int Smitten = 48, Healed = 49, Blessed = 50, Possessed = 51, Compelled = 52;
    }

    /// <summary><see cref="FeatureKind.ObjClass"/> values: classes of thing an NPC perceives (never ids).</summary>
    public static class ObjClass
    {
        public const int BerryBush = 1, RootPatch = 2, FishSpot = 3, Nightshade = 4, WoodStand = 5, LogPile = 6, Hut = 7, HutSite = 8,
            Axe = 9, FoodLot = 10, LogLot = 11, Spot = 12, EnemySoldier = 13, Unknown = 14,
            // controlled lab scenarios only (NPC_LEARNING L3)
            LabButton = 30;
        // ---- F1 (contracts 14): blocks. Divinity 80-89, Civ assets 90-99 (C1), Orgs 100-109 (C6); 110+ reserved ----
        public const int DivinityFirst = 80, DivinityLast = 89, CivFirst = 90, CivLast = 99, OrgsFirst = 100, OrgsLast = 109;
        /// <summary>A consecrated place (D3 creates, D6 gives it meaning), perceived through <c>SenseExtra</c>.</summary>
        public const int Shrine = 80;
    }

    /// <summary><see cref="FeatureKind.ObjCategory"/> values (from the item table).</summary>
    public static class ObjCategory
    {
        public const int Food = 1, Raw = 2, Tool = 3, Weapon = 4, Shelter = 5, Workplace = 6, Store = 7, Danger = 8, Ground = 9;
    }

    /// <summary><see cref="FeatureKind.ObjState"/> values.</summary>
    public static class ObjState
    {
        public const int StockHigh = 1, StockLow = 2, StockEmpty = 3, Mine = 4, Others = 5, Unfinished = 6, Full = 7;
    }

    /// <summary><see cref="FeatureKind.Place"/> values: the kind of place the NPC stands in.</summary>
    public static class PlaceKind
    {
        public const int Meadow = 1, Forest = 2, Hills = 3, Fields = 4, Mountain = 5, Pasture = 6, Desert = 7, Coast = 8, Home = 9, Road = 10;
    }

    /// <summary><see cref="FeatureKind.Situation"/> values: Cognition's hand-written situation features.</summary>
    public static class SituationKind
    {
        public const int FoodRemembered = 1, EnemyInSight = 2, NoFoodKnown = 3, Outdoors = 4;
        // ---- F1 (contracts 14): blocks. Divinity 80-95, Orgs 96-111, Civ 112-127; 128+ reserved ----
        public const int DivinityFirst = 80, DivinityLast = 95, OrgsFirst = 96, OrgsLast = 111, CivFirst = 112, CivLast = 127;
        public const int DivineSign = 80, AvatarInSight = 81, ConsecratedPlace = 82, SmiteWitnessed = 83;
    }

    /// <summary><see cref="FeatureKind.NpcGroup"/> values (SHARED_CONTRACTS 6.1): M5 1-3, later 4-5, M4 6-7.</summary>
    public static class NpcGroupValue
    {
        public const int OwnFaction = 1, OtherFaction = 2, EnemyFaction = 3, StateStaff = 4, Soldier = 5, Elder = 6, Child = 7;
        /// <summary>F1 (contracts 14): 8 Clergy (D6), 9 OrgStaff (C6), 10 Avatar (D5); 11+ reserved.</summary>
        public const int Clergy = 8, OrgStaff = 9, Avatar = 10;
    }

    /// <summary>
    /// <see cref="FeatureKind.Role"/> values (F1, contracts 14): 4 and 5 taken by the base, 1-3 reserved; Orgs 6-9, Civ 10; 11+ reserved.
    /// </summary>
    public static class RoleValue
    {
        public const int Priest = 6, Healer = 7, Teacher = 8, Official = 9, Employer = 10;
    }

    /// <summary><see cref="FeatureKind.Divine"/> values (F1, contracts 10.3, 11.3, 13.2).</summary>
    public static class DivineFeatureValue
    {
        public const int Suggested = 1, Commanded = 2, Compelled = 3, Possessed = 4, CommandmentRequired = 5, CommandmentProhibited = 6, CommandmentEncouraged = 7;
    }

    /// <summary><see cref="FeatureKind.Time"/> values.</summary>
    public static class TimeOfDay
    {
        public const int Day = 1, Dusk = 2, Night = 3;
    }

    /// <summary>
    /// <c>FeatureId = (Kind &lt;&lt; 24) | Value</c>; 0 is "none"; sets are sorted ascending (NPC_LEARNING_DESIGN 3.1).
    /// Drive bands: band 0 below comfort emits nothing, 1 mild, 2 high, 3 pain.
    /// </summary>
    public static class Feature
    {
        public static uint Make(FeatureKind k, int value) => ((uint)k << 24) | ((uint)value & 0xFFFFFFu);
        public static FeatureKind KindOf(uint f) => (FeatureKind)(f >> 24);
        public static int ValueOf(uint f) => (int)(f & 0xFFFFFFu);

        public static uint DriveBand(int drive, int band) => Make(FeatureKind.DriveBand, drive * 4 + band);
        public static uint DriveActive(int drive) => Make(FeatureKind.DriveActive, drive);
        public static uint Self(int v) => Make(FeatureKind.Self, v);
        public static uint Class(int v) => Make(FeatureKind.ObjClass, v);
        public static uint Category(int v) => Make(FeatureKind.ObjCategory, v);
        public static uint State(int v) => Make(FeatureKind.ObjState, v);
        public static uint Place(int v) => Make(FeatureKind.Place, v);
        public static uint PlaceRef(int node) => Make(FeatureKind.PlaceRef, node);
        public static uint Situation(int v) => Make(FeatureKind.Situation, v);
        public static uint Time(int v) => Make(FeatureKind.Time, v);
        /// <summary>F0: a person's relationship class (<c>NpcRelClass</c>), group, identity (as recognised) and a message act.</summary>
        public static uint NpcRel(int relClass) => Make(FeatureKind.NpcRel, relClass);
        public static uint NpcGroup(int group) => Make(FeatureKind.NpcGroup, group);
        public static uint NpcRef(int npcAs) => Make(FeatureKind.NpcRef, npcAs);
        public static uint MsgAct(int act) => Make(FeatureKind.MsgAct, act);
        public static uint Conj(int slot, int generation) => Make(FeatureKind.Conj, (slot << 8) | (generation & 0xFF));

        public static bool IsDrive(uint f)
        {
            var k = KindOf(f);
            return k == FeatureKind.DriveBand || k == FeatureKind.DriveActive;
        }

        /// <summary>The drive a drive feature names (-1 for any other feature).</summary>
        public static int DriveOf(uint f)
        {
            var k = KindOf(f);
            if (k == FeatureKind.DriveBand) return ValueOf(f) / 4;
            if (k == FeatureKind.DriveActive) return ValueOf(f);
            return -1;
        }

        /// <summary>True for a feature that belongs to the candidate's target rather than to the NPC's situation.</summary>
        public static bool IsTargetKind(FeatureKind k) =>
            k == FeatureKind.ObjClass || k == FeatureKind.ObjCategory || k == FeatureKind.ObjState || k == FeatureKind.NpcRel
            || k == FeatureKind.NpcGroup || k == FeatureKind.NpcRef || k == FeatureKind.PlaceRef;

        /// <summary>Plain words for a feature, for the inspector's "Habits" panel and the learn-dump (never read by a policy).</summary>
        public static string Describe(uint f)
        {
            int v = ValueOf(f);
            switch (KindOf(f))
            {
                case FeatureKind.DriveBand:
                {
                    int d = v / 4, band = v % 4;
                    string dn = d < Drive.Count ? Drive.Names[d] : "drive" + d;
                    return (band == 1 ? "a little " : band == 2 ? "" : "very ") + AdjOf(d, dn);
                }
                case FeatureKind.DriveActive: return v < Drive.Count ? AdjOf(v, Drive.Names[v]) : "driven";
                case FeatureKind.Self:
                    switch (v)
                    {
                        case SelfFeature.CarryingFood: return "carrying food";
                        case SelfFeature.CarryingLog: return "carrying a log";
                        case SelfFeature.CarryingTool: return "carrying a tool";
                        case SelfFeature.Injured: return "injured";
                        case SelfFeature.HasBed: return "has a bed";
                        case SelfFeature.HasSite: return "building a hut";
                        case SelfFeature.NoBed: return "has no bed";
                        case SelfFeature.CarryFull: return "hands full";
                        default: return "self " + v;
                    }
                case FeatureKind.ObjClass: return ClassName(v);
                case FeatureKind.ObjCategory:
                    switch (v)
                    {
                        case ObjCategory.Food: return "near food";
                        case ObjCategory.Raw: return "near materials";
                        case ObjCategory.Tool: return "near a tool";
                        case ObjCategory.Shelter: return "near shelter";
                        case ObjCategory.Danger: return "in danger";
                        case ObjCategory.Ground: return "right here";
                        default: return "category " + v;
                    }
                case FeatureKind.ObjState:
                    switch (v)
                    {
                        case ObjState.StockHigh: return "plenty there";
                        case ObjState.StockLow: return "little there";
                        case ObjState.StockEmpty: return "nothing there";
                        case ObjState.Mine: return "my own";
                        case ObjState.Others: return "someone else's";
                        case ObjState.Unfinished: return "unfinished";
                        case ObjState.Full: return "full";
                        default: return "state " + v;
                    }
                case FeatureKind.Place:
                    switch (v)
                    {
                        case PlaceKind.Forest: return "in the forest";
                        case PlaceKind.Hills: return "in the hills";
                        case PlaceKind.Fields: return "in the fields";
                        case PlaceKind.Mountain: return "on the mountain";
                        case PlaceKind.Meadow: return "in the meadow";
                        case PlaceKind.Pasture: return "on the pasture";
                        case PlaceKind.Desert: return "in the desert";
                        case PlaceKind.Coast: return "on the coast";
                        case PlaceKind.Home: return "at home";
                        case PlaceKind.Road: return "on a road";
                        default: return "place " + v;
                    }
                case FeatureKind.PlaceRef: return "at place #" + v;
                case FeatureKind.Situation:
                    switch (v)
                    {
                        case SituationKind.FoodRemembered: return "remembering food";
                        case SituationKind.EnemyInSight: return "an enemy in sight";
                        case SituationKind.NoFoodKnown: return "knowing no food";
                        case SituationKind.Outdoors: return "outdoors";
                        default: return "situation " + v;
                    }
                case FeatureKind.Time: return v == TimeOfDay.Night ? "at night" : v == TimeOfDay.Dusk ? "at dusk" : "by day";
                case FeatureKind.Conj: return "combination #" + (v >> 8);
                default: return Cognition.CognitionExtensions.Describe(f) ?? "feature " + f;
            }
        }

        private static string AdjOf(int d, string name)
        {
            switch (d)
            {
                case Drive.Hunger: return "hungry";
                case Drive.Fatigue: return "tired";
                case Drive.Cold: return "cold";
                case Drive.Fear: return "afraid";
                case Drive.Pain: return "in pain";
                default: return name;
            }
        }

        public static string ClassName(int v)
        {
            switch (v)
            {
                case ObjClass.BerryBush: return "berry bush";
                case ObjClass.RootPatch: return "root patch";
                case ObjClass.FishSpot: return "fishing spot";
                case ObjClass.Nightshade: return "nightshade bush";
                case ObjClass.WoodStand: return "wood stand";
                case ObjClass.LogPile: return "log pile";
                case ObjClass.Hut: return "hut";
                case ObjClass.HutSite: return "hut site";
                case ObjClass.Axe: return "axe";
                case ObjClass.FoodLot: return "food on the ground";
                case ObjClass.LogLot: return "log on the ground";
                case ObjClass.Spot: return "this spot";
                case ObjClass.EnemySoldier: return "enemy soldier";
                case ObjClass.LabButton: return "lab button";
                default: return "thing " + v;
            }
        }
    }
}
