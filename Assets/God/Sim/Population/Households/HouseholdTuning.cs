namespace God.Sim.Population.Households
{
    /// <summary>
    /// <c>SimTuning.Households</c> (F0.12): M4's numbers. Written only when changed. OWNER: M4. <see cref="Enabled"/> 0 turns
    /// off ageing, births, households, kin cues, care and estates (SHARED_CONTRACTS 7.3); <see cref="Immigration"/> replaces a
    /// scenario key (12.13). Every other number sits in a nested block that is itself written only when changed, so a game
    /// with <c>Enabled = 0</c> writes exactly the tuning F0 wrote and keeps F0's hash (the M4 neutrality pin).
    /// </summary>
    public sealed class HouseholdTuning : IOmitWhenDefault
    {
        public int Enabled = 1;
        /// <summary>1: the off-island arrival rule runs (POPULATION "Immigration"; default off, conventions 4).</summary>
        public int Immigration;

        public LifeTuning Life = new LifeTuning();
        public CareTuning Care = new CareTuning();
        public PartnerTuning Partner = new PartnerTuning();
        public DiseaseTuning Disease = new DiseaseTuning();
        public FounderTuning Founders = new FounderTuning();
        /// <summary>D4 (unity/demog): individual preferences.</summary>
        public PrefTuning Prefs = new PrefTuning();
    }

    /// <summary>Life stages, growth, fertility, pregnancy and mortality (POPULATION 4). Ages are sim milli-years at LifeScale 100.</summary>
    public sealed class LifeTuning : IOmitWhenDefault
    {
        /// <summary>Natural death at <c>MaxAgeBase + Lifespan phenotype</c> plus a jitter of +-MaxAgeJitter (milli-years).</summary>
        public int MaxAgeBaseMilli = 3500, MaxAgeJitterMilli = 200;
        /// <summary>Fertile window: from adulthood to <c>FertileEndBase + FertileEndSpan x Fertility / 1000</c>.</summary>
        public int FertileFromMilli = 1000, FertileEndBaseMilli = 2400, FertileEndSpanMilli = 400;
        /// <summary>Daily conception chance at best: <c>ConceiveBase + ConceiveSpan x Fertility / 1000</c> permille.</summary>
        public int ConceiveBasePermille = 350, ConceiveSpanPermille = 300;
        /// <summary>The nutrition factor: 0 at or below NutritionNone, 1000 at or above NutritionFull (permille of Nutrition).</summary>
        public int NutritionNone = 300, NutritionFull = 600;
        /// <summary>The age factor falls from 1000 to AgeFactorEnd over the last AgeFactorSpan of the window.</summary>
        public int AgeFactorSpanMilli = 400, AgeFactorEndPermille = 300;
        public int PregnancyMilli = 250;
        /// <summary>Time after a birth before the next conception (so at least Pregnancy + this between births).</summary>
        public int AfterBirthMilli = 100;
        public int MiscarryPermille = 20, MiscarryBelowNutrition = 200;
        /// <summary>How long one "try for a child" decision lasts (ticks): both partners must hold it on the mother's day check.</summary>
        public int TryingTicks = 1200;
        /// <summary>
        /// F2: the conception-coordination rule (<see cref="Conception"/>). 1 (default since the Checkpoint B re-pin, LEAD_RULINGS
        /// R60): at the check, two partners who both hold "try" with each other end a dormant reproductive relationship either
        /// holds with a third adult. 0: the old M4/D4 rule (experiment arm). Written only when not the default, so the default
        /// tuning serialises (and its <c>core</c> hash part stays) exactly as before; an explicit 0 is written. Saves from before
        /// the re-pin (version 5, where an omitted field meant 0) are refused by save version 6.
        /// </summary>
        [System.ComponentModel.DefaultValue(1), Newtonsoft.Json.JsonProperty(DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Ignore)]
        public int ConceptionMode = 1;
        /// <summary>Pregnancy raises the mother's hunger rate by this (permille).</summary>
        public int PregnantHungerPermille = 300;
        /// <summary>Childbirth hazard per 1000 births (mother and newborn), rising to the max when the mother's nutrition is below 300 at the due date.</summary>
        public int BirthRiskPer1000 = 4, BirthRiskMaxPer1000 = 30;
        /// <summary>Newborn state: Nutrition and growth (permille), Health base plus Health phenotype / 5.</summary>
        public int NewbornNutrition = 600, NewbornGrowth = 300, NewbornHealthBase = 700;
        /// <summary>A child eats at ChildHunger permille of the adult rate at birth, rising linearly to 1000 at adulthood.</summary>
        public int ChildHungerPermille = 600;
        /// <summary>Growth per day: <c>(Nutrition - GrowthNutritionFrom) / GrowthDivisor</c> permille, never below 0.</summary>
        public int GrowthNutritionFrom = 300, GrowthDivisor = 40;
        /// <summary>Children and elders take this much of starvation damage (permille).</summary>
        public int FrailStarvePermille = 1500;
        /// <summary>
        /// Dependency (D3 §10): below <see cref="ForageFromMilli"/> a child cannot pick, forage or gather at all; from there its
        /// competence rises linearly from <see cref="CompetenceStartPermille"/> to 1000 at adulthood (each work tick counts that share).
        /// </summary>
        public int ForageFromMilli = 500, CompetenceStartPermille = 200;
        /// <summary>Elders recover Health at this share (permille).</summary>
        public int ElderRecoverPermille = 500;
        /// <summary>
        /// Unattended-child hazard (HOUSEHOLDS "Protection"), per million per day of being alone: under 0.25 years, 0.25-0.5,
        /// and older; "alone" means no adult within <see cref="AttendNodes"/>. Checked every <see cref="AttendCheckTicks"/>.
        /// </summary>
        public int HazardInfantPerMillion = 3333, HazardYoungPerMillion = 1000, HazardChildPerMillion = 200;
        public int AttendNodes = 3, AttendCheckTicks = 10;
        /// <summary>Work hazard per work day (per million): foraging and gathering; an injury of 100-300 (permille).</summary>
        public int ForageHazardPerMillion = 250, GatherHazardPerMillion = 500;
        /// <summary>Injury above this at the day check risks death (permille chance DeathRiskPermille).</summary>
        public int InjuryDeadlyFrom = 700, InjuryDeathPermille = 300;
    }

    /// <summary>Care (HOUSEHOLDS "Child-rearing", "Care of elders", Q12): the reward channel and the no-care control.</summary>
    public sealed class CareTuning : IOmitWhenDefault
    {
        /// <summary>1: no adult is offered Feed, Tend, Adopt or care arrangements (the M4.36 no-care control).</summary>
        public int Off;
        /// <summary>Reward to a feeder per food unit handed over (vp), times (1000 + 2 r̂) / 1000 and the child's visible need.</summary>
        public int FeedRewardVp = 120;
        /// <summary>Food units handed over per Feed.</summary>
        public int FeedUnits = 1;
        /// <summary>Ticks of one Tend; a tended patient's illness is less severe that day (permille kept) and recovers faster.</summary>
        public int TendTicks = 150, TendSeverityPermille = 700;
        public int TendRewardVp = 80;
        /// <summary>Food units taken from the household store at once.</summary>
        public int TakeUnits = 2;
        /// <summary>Begging display length (ticks) and the approach distance (nodes).</summary>
        public int BegTicks = 40, BegNodes = 2;
        /// <summary>A fostered child not told believes its carer a parent after this many days of being fed (the cuckoo cue).</summary>
        public int FosterCueDays = 2;
        /// <summary>The kin term: <c>r̂ x (100 + KinWeight) / 100 x KinTermPermille / 1000</c>.</summary>
        public int KinTermPermille = 600;
        /// <summary>The owed bands of a care arrangement (care ticks given minus returned).</summary>
        public int OwedBand1 = 150, OwedBand2 = 400, OwedBand3 = 900;
        /// <summary>Reward for being accepted into a household (NPC_LEARNING 4.4: +500) and for a partnership formed.</summary>
        public int AcceptedVp = 500, PartneredVp = 300;
        /// <summary>The learned outcome of trying for a child (episode closed at the birth).</summary>
        public int ChildBornVp = 600;
        /// <summary>D4 (M4.44): the forms of care a begging child's display reaches (<see cref="CareStructure"/> bits: 1 parents, 2 other kin, 4 household mates, 8 anyone, 16 adoptive and step parents); 31 all.</summary>
        public int FeedFrom = CareStructure.All;
    }

    /// <summary>Partnerships (HOUSEHOLDS "Partnership and courtship", Q5, Q6).</summary>
    public sealed class PartnerTuning : IOmitWhenDefault
    {
        /// <summary>Live partnerships an NPC may hold at once (Q5: serial in M4; the architecture allows more).</summary>
        public int MaxLivePartnerships = 1;
        /// <summary>A proposal display lasts this long (ticks) and is made within ProposeNodes of the other.</summary>
        public int ProposeTicks = 120, ProposeNodes = 2;
        /// <summary>Adults believed close kin (parent, child, sibling) with confidence above this are not courted.</summary>
        public int KinAvoidConfidence = 400;
        /// <summary>1: conception is disabled (the same-sex equality fixtures, M4.28).</summary>
        public int NoConception;
        /// <summary>
        /// D4 reproductive relationships (<see cref="ReproductiveUnion"/>): how many one adult may hold at once; how long one lasts
        /// with neither trying (milli-years); how long a "wants a child" display is shown (ticks). <see cref="IndependentUnions"/>
        /// 0 is the controlled arm where only romantic partners may agree to try (the M4 rule).
        /// </summary>
        public int MaxLiveUnions = 1, UnionLapseMilli = 500, SeekTicks = 240, IndependentUnions = 1;
        /// <summary>Reward (vp) to both when a reproductive relationship is agreed.</summary>
        public int UnionVp = 200;
    }

    /// <summary>D4 individual preferences (<see cref="Preferences"/>): mean and half-width of each draw (0..1000; 500 is neutral).</summary>
    public sealed class PrefTuning : IOmitWhenDefault
    {
        public int CompanionshipMean = 500, CompanionshipSpread = 400;
        public int ChildDesireMean = 500, ChildDesireSpread = 400;
        public int CommitmentMean = 500, CommitmentSpread = 500;
    }

    /// <summary>Disease (POPULATION "Disease", Q3): one fever. Rates per million per day; Mild halves contact, crowding and sanitation.</summary>
    public sealed class DiseaseTuning : IOmitWhenDefault
    {
        public int BasePerMillion = 250, ContactHutPerMillion = 150000, ContactNearPerMillion = 40000, CrowdingPerBedPerMillion = 667;
        public int NearNodes = 2;
        public int MinDays = 3, MaxDays = 6, ImmuneDays = 15;
        /// <summary>The design's severity is the daily Health loss; scaled so a fed adult loses 100-300 over an illness (M4 calibration).</summary>
        public int SeverityScalePermille = 400;
        public int MildSeverityCap = 150;
        /// <summary>Fouling of a hex: added per person sleeping there, lost each day (permille); the sanitation term is Fouling / 4 per million.</summary>
        public int FoulPerPerson = 40, FoulRegenPermille = 200, FoulMax = 1000;
        /// <summary>Untreated wounds above this (permille) get a daily infection chance (per million).</summary>
        public int WoundFrom = 300, WoundInfectPerMillion = 50000;
    }

    /// <summary>
    /// The founders' ages and household mix (POPULATION "Owner population requirements": scenario data, not code). The default
    /// is the M3 band of adults aged 1.0-2.4 with no families; m4_* scenarios set children, elders, couples and same-sex couples.
    /// </summary>
    public sealed class FounderTuning : IOmitWhenDefault
    {
        public int AgeMinMilli = 1000, AgeMaxMilli = 2400;
        /// <summary>Shares of founders (permille) who are children (aged ChildMin-ChildMax) and elders (3.0-3.3).</summary>
        public int ChildPermille, ElderPermille;
        public int ChildMinMilli = 200, ChildMaxMilli = 900;
        /// <summary>Share of founder adults paired into couples at the start (permille), and of those couples made same-sex.</summary>
        public int CouplePermille, SameSexPermille;
        /// <summary>Founders start with a hut for each founder household with an adult (beds per hut as M3).</summary>
        public int StartHuts;
        /// <summary>Food units in each founder household's store at the start (roots).</summary>
        public int StoreFood;

        /// <summary>
        /// D4 (M4.42): the founder generator. 0: the mix above (the M3 band of unrelated young adults by default). 1: the
        /// established band (<see cref="Founders.Established"/>): elders, mature adults in established households with their
        /// dependent children (biological where a mixed couple's ages allow, adopted otherwise), young adults, birth dates spread
        /// within each class. <see cref="EstablishedBand"/> gives the preferred settings.
        /// </summary>
        public int Generator;
        /// <summary>Established band: share of mature adults (permille, aged MatureMin-MatureMax), elders' ages, and the share of young adults (AgeMin-AgeMax) in couples.</summary>
        public int MaturePermille, MatureMinMilli = 2000, MatureMaxMilli = 2900, ElderMinMilli = 3000, ElderMaxMilli = 3300, YoungCouplePermille;

        /// <summary>The established band (owner D4 §4, the preferred default of population games): 10% elders, 30% mature adults (80% of them in couples), 30% dependent children, 30% young adults (40% in couples); 10% of couples same-sex; a hut and a store of 6 roots per household.</summary>
        public static void EstablishedBand(FounderTuning t)
        {
            t.Generator = 1;
            t.ElderPermille = 100; t.MaturePermille = 300; t.ChildPermille = 300;
            t.ChildMinMilli = 50; t.ChildMaxMilli = 950; t.AgeMinMilli = 1000; t.AgeMaxMilli = 1900;
            t.MatureMinMilli = 2000; t.MatureMaxMilli = 2900; t.ElderMinMilli = 3000; t.ElderMaxMilli = 3300;
            t.CouplePermille = 800; t.YoungCouplePermille = 400; t.SameSexPermille = 100;
            t.StartHuts = 1; t.StoreFood = 6;
        }
    }
}
