namespace God.Sim.Population
{
    /// <summary>
    /// The body, drive, movement and work numbers of the population (POPULATION_DESIGN, ECONOMY_DESIGN 4.1-4.2), on
    /// <c>SimTuning.Population</c>. Rates are per sim day in permille of the drive (0-1000) unless named otherwise; the
    /// sim applies them per tick in micro-units (x1000), so nothing is lost to rounding.
    /// </summary>
    public sealed class PopulationTuning : IOmitWhenDefault
    {
        /// <summary>An NPC thinks when <c>(tick + id) % ThinkEvery == 0</c> (conventions §4).</summary>
        public int ThinkEvery = 10;

        // ---- drives ----
        public int HungerPerDay = 500;
        /// <summary>Extra hunger per day of hard work (gathering, building).</summary>
        public int HungerWorkPerDay = 200;
        public int FatiguePerDay = 450;
        public int FatigueWorkPerDay = 350;
        public int FatigueWalkPerDay = 150;
        /// <summary>Extra fatigue per day while awake at dusk or night (a day/night rhythm: people tire in the dark).</summary>
        public int FatigueNightPerDay = 1300;
        /// <summary>Fatigue relief per day asleep in the open, and in a hut (faster: LS3).</summary>
        public int SleepOpenPerDay = 2000;
        public int SleepHutPerDay = 3400;
        /// <summary>Cold rises at night outdoors and falls by day or in a hut.</summary>
        public int ColdNightPerDay = 1600;
        public int ColdDayFallPerDay = 1600;
        public int ColdHutFallPerDay = 3000;
        public int FearFallPerDay = 2400;
        /// <summary>Fear added per think while an enemy soldier is in sight.</summary>
        public int FearSeenEnemy = 150;
        /// <summary>Pain is the injury level plus a decaying extra (a toxic meal); the extra falls this much a day.</summary>
        public int PainFallPerDay = 600;
        public int PainRestFallPerDay = 1200;
        /// <summary>Comfort levels (reward is only for relief above comfort; drive bands start there).</summary>
        public int ComfortHunger = 300, ComfortFatigue = 300, ComfortCold = 300, ComfortFear = 200, ComfortPain = 100;
        /// <summary>Band 2 (high) and band 3 (pain) thresholds.</summary>
        public int BandHigh = 550, BandPain = 800;
        /// <summary>An NPC this full cannot eat any more (the body refuses: frustration).</summary>
        public int SatedBelow = 150;
        /// <summary>Fatigue at which an NPC falls asleep where it stands (a reflex), and below which it wakes.</summary>
        public int CollapseFatigue = 1000;
        public int WakeFatigue = 80;
        /// <summary>Hunger that wakes a sleeper.</summary>
        public int WakeHunger = 900;
        /// <summary>An NPC cannot fall asleep by choice while fatigue is below this.</summary>
        public int SleepFrom = 400;

        // ---- body ----
        /// <summary>Nutrition per day: falls while hunger is at pain level or high, rises while hunger is low.</summary>
        public int NutritionLossPain = 220;
        public int NutritionLossHigh = 60;
        public int NutritionGainLow = 120;
        /// <summary>Health loss per day at nutrition 0, and below <see cref="NutritionLowFrom"/> (starvation: 5-10 days, POPULATION).</summary>
        public int StarveLossAtZero = 150;
        public int StarveLossLow = 40;
        public int NutritionLowFrom = 150;
        /// <summary>Health recovery per day while nutrition is above 500.</summary>
        public int HealthRecoverPerDay = 15;
        /// <summary>Health loss per day while cold is at pain level outdoors (exposure).</summary>
        public int ExposureLossPerDay = 40;
        public int InjuryHealPerDay = 40;

        // ---- founders ----
        public int FounderHunger = 200, FounderFatigue = 150, FounderNutrition = 800;
        /// <summary>Food units in each founder's pack (roots: they keep 6 days).</summary>
        public int FounderFood = 3;
        /// <summary>Chance per founder (permille) of carrying an axe.</summary>
        public int FounderAxePermille = 300;
        /// <summary>Founders stand 1 to this many hexes' worth of nodes from their HQ, outside its build circle.</summary>
        public int FounderSpreadNodes = 14;

        // ---- movement and perception ----
        public int StepTicks = 6;
        public int RoadStepTicks = 4;
        /// <summary>Sight radius in nodes by day and at night (5 m a node).</summary>
        public int SightDay = 10, SightNight = 5;
        /// <summary>Items attended per think (goal-driven top K, COMMUNICATION 4.3).</summary>
        public int AttendK = 3;
        /// <summary>At most this many nearest things per think (the perception budget, HEADLESS 4.9).</summary>
        public int PerceiveMax = 16;
        /// <summary>Path search bound (nodes expanded) before falling back to a straight walk.</summary>
        public int PathMaxExpand = 6000;
        /// <summary>Explore walks this many nodes out.</summary>
        public int ExploreNodes = 9;
        /// <summary>Perceived cost of walking one node (vp).</summary>
        public int WalkCostPerNode = 12;
        /// <summary>Perceived cost of abandoning the running intent for another (a switching cost, never the amount already spent).</summary>
        public int SwitchCost = 60;
        /// <summary>Remembered places come in as candidates up to this many nodes away.</summary>
        public int RecallNodes = 40;

        // ---- work ----
        /// <summary>Ticks to forage one unit from a source, and to eat one unit.</summary>
        public int ForageTicks = 30;
        public int EatTicks = 15;
        /// <summary>Ticks of hand work per log (ECONOMY 4.2: 60 by hand), divided by the tool speed.</summary>
        public int GatherTicks = 60;
        /// <summary>An axe's speed for gathering logs (permille: 1600 = +60%). 1000 makes it useless (LS4 control).</summary>
        public int AxeSpeedPermille = 1600;
        public int AxeUses = 200;
        /// <summary>Carry capacity in weight units (food 1, log 4, tools 2: ECONOMY 3.1) and the number of lots carried.</summary>
        public int CarryWeight = 6;
        public int CarrySlots = 4;
        /// <summary>A hut: logs, work ticks and beds (HOUSEHOLDS: a small hut has 3 beds).</summary>
        public int HutLogs = 4;
        public int HutWorkTicks = 150;
        public int HutBeds = 3;
        /// <summary>A hut may not be founded within this many nodes of another.</summary>
        public int HutSpacing = 3;
        /// <summary>Within this many nodes of its hut an NPC is "at home".</summary>
        public int HomeRadius = 3;

        // ---- goods worth (the M3 goods channel, NPC_LEARNING 4.4; price beliefs replace it at M5) ----
        /// <summary>Worth to the NPC of a food unit gained into its carry (vp).</summary>
        public int FoodWorth = 60;
        /// <summary>Worth of a log while the NPC has no bed (it needs a hut), and once it has one.</summary>
        public int LogWorthNeeded = 220;
        public int LogWorthHoused = 20;
        public int ToolWorth = 40;

        // ---- toxic food ----
        /// <summary>Pain a nightshade meal causes (NPC_LEARNING L4: 300).</summary>
        public int NightshadePain = 300;

        /// <summary>NPCs may gather the dice's log piles by hand (ECONOMY 4.2, Q9). Controlled scenarios may turn it off.</summary>
        public bool HandGatherPiles = true;

        /// <summary>Checks on every decision that the target was perceived or believed (acceptance M3.12; costs time, off by default).</summary>
        public bool CheckPerception;
    }

    /// <summary>
    /// The ecological rules of wild resources (ECONOMY 4.8, owner decision Q11), on <c>SimTuning.Ecology</c>: how many
    /// sources each terrain gets, their capacity and logistic regrowth per day. Configurable so experiments can vary how
    /// forgiving nature is.
    /// </summary>
    public sealed class EcologyTuning : IOmitWhenDefault
    {
        public int BerriesPerForest = 3, BerriesPerMeadow = 2, RootsPerHills = 2, RootsPerFields = 2, WoodPerForest = 2, FishPerCoast = 1;
        public int NightshadePerForest = 0;
        public int BerryCapacity = 60, BerryRegrowPermille = 400;
        public int RootCapacity = 50, RootRegrowPermille = 300;
        public int FishCapacity = 60, FishRegrowPermille = 400;
        public int WoodCapacity = 40, WoodRegrowPermille = 150;
        public int NightshadeCapacity = 30, NightshadeRegrowPermille = 300;
        /// <summary>An emptied source regrows only if a neighbour of its kind is above half capacity: one unit every this many days.</summary>
        public int EmptyRecoverDays = 3;
        /// <summary>Radius in nodes within which a source counts as a neighbour for recovery.</summary>
        public int NeighbourNodes = 30;
        /// <summary>Food spoils after this many days (ECONOMY 3.2): berries 2, roots 6, fish 1, nightshade 2.</summary>
        public int BerrySpoilDays = 2, RootSpoilDays = 6, FishSpoilDays = 1, NightshadeSpoilDays = 2;
        /// <summary>Hunger relief per unit (drive points; ECONOMY 4.1: berries 150, roots 200, fish 300).</summary>
        public int BerryNutrition = 150, RootNutrition = 200, FishNutrition = 300, NightshadeNutrition = 100;
    }
}
