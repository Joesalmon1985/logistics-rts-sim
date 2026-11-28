namespace God.Sim.Unified
{
    /// <summary>
    /// <c>Unification.UN1</c>: OWNER UN1 (world, forage, water, raw materialisation, need rates). Fields only in this file; written
    /// only when changed. Every number is configuration (D10): nothing is sized by tribes, board or population.
    /// </summary>
    public sealed partial class UN1Tuning : IOmitWhenDefault
    {
        // ---- geography (the land pool; the sum should equal the land hex count, else it is scaled) ----
        /// <summary>Land hexes per type in the default 30-hex island: 5 Forest, 5 Mountain, 5 Wetland, 6 Pastures, 6 Fields, 3 Desert.</summary>
        public int Forests = 5, Mountains = 5, Wetlands = 5, PasturesHexes = 6, FieldsHexes = 6, Deserts = 3;

        // ---- forage places (design 3.2; one food per hex type) ----
        public int ForagePlacesPerHex = 4;
        /// <summary>Minimum lattice distance between two places of one hex, and from a start corner (hall site).</summary>
        public int ForageSpacing = 2, StartClearance = 2;
        /// <summary>Per food (Mushroom, Roots, WildEgg, Rabbit, WildVeg, CactusFruit, Seaweed, Herbs): collect ticks.</summary>
        public int[] ForageTicks = { 30, 45, 40, 90, 30, 40, 35, 30 };
        /// <summary>Per food: nutrition (hunger removed), as the design table (read by UN3b's Eat through the catalogue).</summary>
        public int[] ForageNutrition = { 120, 200, 180, 350, 160, 100, 90, 0 };
        /// <summary>Per food: days until spoiled (read by UN3a rows; recorded here as the design default).</summary>
        public int[] ForageSpoilDays = { 2, 6, 4, 1, 3, 3, 2, 10 };

        // ---- water (20 ticks per drink or wash) ----
        public int DrinkTicks = 20, WashTicks = 20;
        /// <summary>Thirst removed by one drink, Hygiene removed by one wash (pressure points, 0-1000).</summary>
        public int DrinkRelief = 400, WashRelief = 400;

        // ---- dice clock and gradual materialisation (design 4) ----
        public int AllowancePerRoll = 6, SheepAllowancePerRoll = 2, AllowanceCap = 30;
        public int MaterialiseIntervalTicks = 30, MaterialiseUnits = 1;
        public int NodesPerHex = 4, RawNodeSpacing = 4, RawNodeCap = 12;

        // ---- reach audit ----
        public int StartReachMaxEdges = 30;
        /// <summary>Herb places (Herbs 32, an infinite forage-like place, G5) per Forest and Mountain hex.</summary>
        public int HerbPlacesPerHex = 1;

        // ---- need rates (G15): the seven channels Thirst, Hygiene, Bladder, Social, Fun, Comfort, Room; pressure points (0-1000) per day ----
        public int[] NeedRatesPerDay = { 800, 250, 600, 200, 250, 150, 100 };
        /// <summary>Per-person rate variation bound (permille of the rate: hash and temperament halves) and the age factors (permille added for children and elders, Bladder and Thirst).</summary>
        public int NeedVariationPermille = 200, ChildExtraPermille = 300, ElderExtraPermille = 200;
        /// <summary>Rates while asleep, permille of the awake rate (Comfort and Room run at full rate).</summary>
        public int NeedSleepPermille = 500;
        /// <summary>Thirst health coupling (D9.4 d): from this pressure, health falls by this many micro per day (health is 1,000,000).</summary>
        public int ThirstDamageAt = 900, ThirstLossPerDay = 500000;
    }
}
