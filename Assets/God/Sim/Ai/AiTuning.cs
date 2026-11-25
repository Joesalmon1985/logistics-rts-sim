namespace God.Sim
{
    /// <summary>
    /// Every weight of the faction AI (P9) in one place. Percent values are integers (150 = x1.5). <see cref="Default"/> is
    /// the competent-but-not-perfect setting the game uses; tests may pass a changed copy. Change the game's AI here only.
    /// </summary>
    public sealed class AiTuning
    {
        /// <summary>The setting the game and the smoke run use.</summary>
        public static AiTuning Default => new AiTuning();

        // ---------------- pacing and CPU budget ----------------

        /// <summary>Sim ticks between two thinks of one brain: 20 ticks = 2 s. Brains are staggered over this interval.</summary>
        public int ThinkIntervalTicks = 20;
        /// <summary>Most brains that think in one sim tick (the director's CPU budget; with six brains the stagger keeps it at one).</summary>
        public int MaxThinksPerTick = 2;
        /// <summary>Most placement checks one think may spend (candidate nodes or corners checked against the rules).</summary>
        public int MaxChecksPerThink = 900;
        /// <summary>Most structural actions (a building, an HQ, a Citadel) started in one think.</summary>
        public int BuildsPerThink = 1;
        /// <summary>Best-scored candidate nodes tried for one building before giving up this think.</summary>
        public int PlaceTries = 5;
        /// <summary>Longest new road the AI lays, in lattice steps (also bounds its path search).</summary>
        public int MaxRoadSteps = 40;

        // ---------------- economy ----------------

        /// <summary>Open construction sites allowed at once: this many plus one per two finished HQs.</summary>
        public int MaxOpenSitesBase = 2;
        /// <summary>A ware with fewer free than this is short: its harvesters spread to every hex of its land first.</summary>
        public int ScarceWare = 8;
        /// <summary>Score of a harvester plot per point of the hex's number weight (6 and 8 weigh 5, 2 and 12 weigh 1).</summary>
        public int PipScore = 100;
        /// <summary>Score lost for each harvester of the same pile kind already on the hex (any faction).</summary>
        public int CrowdedHexPenalty = 140;
        /// <summary>Score lost per lattice step of road a new building needs.</summary>
        public int RoadStepPenalty = 6;
        /// <summary>A road at least this long gets a flag in its middle, so later buildings can join it.</summary>
        public int SplitRoadSteps = 6;

        // ---------------- expansion ----------------

        /// <summary>The AI expands up to this many HQs (more only when a needed land is missing).</summary>
        public int TargetHqs = 4;
        /// <summary>A further HQ (beyond the second) waits until there are this many finished buildings per HQ beyond the first.</summary>
        public int BuildingsPerHqBeforeExpand = 5;
        /// <summary>Corner score for each of its hexes whose land the faction has no harvester plot on yet and needs.</summary>
        public int MissingTerrainScore = 120;
        /// <summary>As <see cref="MissingTerrainScore"/> for missing forest or mountain (no planks or stone without them).</summary>
        public int MissingCriticalScore = 400;
        /// <summary>Base value of a hex for a new HQ by land: forest, mountain, hills, fields (desert is worth nothing).</summary>
        public int ForestValue = 30, MountainValue = 34, HillsValue = 20, FieldsValue = 18;
        /// <summary>Corner score per point of number weight of a productive hex.</summary>
        public int CornerPipScore = 8;
        /// <summary>A hex already inside one of the faction's build areas counts this percent.</summary>
        public int CoveredHexPercent = 40;
        /// <summary>Corner score lost per enemy HQ within <see cref="NeighbourCornerDistance"/> corners; doubled when that faction is stronger.</summary>
        public int EnemyNearPenalty = 45;

        // ---------------- VP: Citadels ----------------

        /// <summary>Citadels start once the faction has this many HQs (or no more corners to take).</summary>
        public int CitadelFromHqs = 4;
        /// <summary>A Citadel comes before a further HQ once Citadels lag HQs by more than this.</summary>
        public int CitadelLag = 2;
        /// <summary>No Citadel upgrade starts while the army is below this percent of the army wanted.</summary>
        public int CitadelArmyPercent = 60;

        // ---------------- army ----------------

        /// <summary>Army the faction keeps at peace: this many units plus <see cref="ArmyPerHq"/> per HQ.</summary>
        public int ArmyBase = 6;
        public int ArmyPerHq = 3;
        /// <summary>Percent of the peace army wanted while at war.</summary>
        public int WarArmyPercent = 220;
        /// <summary>Hard cap on units per faction (factories pause above it; keeps the sim fast).</summary>
        public int ArmyCap = 32;
        /// <summary>Strength of one soldier in the AI's army sums (stage 3: every unit is a soldier; Q4 weighs kits).</summary>
        public int InfantryStrength = 2;
        /// <summary>Free weapons in stock above which the axesmith pauses while the army is at its size.</summary>
        public int SpareWeapons = 4;
        /// <summary>While the faction has no harvester of a raw, its processors stop taking that raw below this many free units (kept to build the harvester).</summary>
        public int KeepRawWithoutHarvester = 4;
        /// <summary>A processor pauses while the faction has this many free units of its ware (its raws go elsewhere).</summary>
        public int EnoughProcessed = 10;
        /// <summary>A processor with at least this many free units of its ware stops taking a raw the want list is short of.</summary>
        public int KeepProcessed = 4;

        /// <summary>Guards on an inner HQ, a border HQ, an HQ facing a faction at war, and the most on a threatened one.</summary>
        public int GuardsInner = 1, GuardsBorder = 2, GuardsFront = 4, GuardsThreatMax = 5;
        /// <summary>Enemy units within this many mm of an HQ make it threatened.</summary>
        public int ThreatRadiusMm = 32000;
        /// <summary>Corner distance within which another faction's HQ makes an HQ a border HQ (and makes a neighbour).</summary>
        public int NeighbourCornerDistance = 4;

        // ---------------- attack ----------------

        /// <summary>The free army attacks when its strength is at least this percent of the defence at the target plus <see cref="AttackMinStrength"/>.</summary>
        public int AttackPercent = 160;
        public int AttackMinStrength = 14;
        /// <summary>It calls the attack off when its strength falls below this percent of the defence, or below <see cref="RetreatMinStrength"/>.</summary>
        public int RetreatPercent = 60;
        public int RetreatMinStrength = 4;
        /// <summary>Enemy units within this many mm of a target HQ count as its defence.</summary>
        public int DefenceRadiusMm = 30000;
        /// <summary>Target choice: lattice steps added for a Citadel (harder to take).</summary>
        public int CitadelTargetPenalty = 30;

        // ---------------- war and peace (owner rules) ----------------

        /// <summary>War is declared on a sole leader ahead of the second place by at least this many VP.</summary>
        public int LeaderMargin = 2;
        /// <summary>The war on a leader is kept while it is still the sole leader ahead by at least this many VP.</summary>
        public int LeaderKeepMargin = 1;
        /// <summary>A faction joins a war on a leader (or tied leader) only while the coalition against it has at least this percent of its army.</summary>
        public int CoalitionPercent = 60;
        /// <summary>...and only with an army of at least this strength of its own (a war without one only invites a siege).</summary>
        public int JoinWarMinStrength = 6;
        /// <summary>Tied leaders fight each other only once the tied lead is worth at least this many VP (not at the start).</summary>
        public int TieWarMinVp = 4;
        /// <summary>A weak neighbour is attacked when the own army is at least this percent of its army...</summary>
        public int OpportunistPercent = 200;
        /// <summary>A faction at the top of the VP table starts no opportunistic war.</summary>
        public bool LeaderStaysOut = true;
        /// <summary>...and at least this strong...</summary>
        public int OpportunistMinStrength = 20;
        /// <summary>...and the war is kept while the own army stays at least this percent of the neighbour's (and half that minimum strength).</summary>
        public int OpportunistKeepPercent = 100;
        /// <summary>A faction is losing badly against an enemy whose units at its HQs are at least this percent of its own army plus <see cref="LosingSlack"/>.</summary>
        public int LosingPercent = 200;
        public int LosingSlack = 6;

        // ---------------- trade (P11, AiTrader) ----------------

        /// <summary>A brain sends at most one new offer per this many ticks (300 = 30 s).</summary>
        public int TradeIntervalTicks = 300;
        /// <summary>An offer refused by a partner is not sent to it again (same wares) for this many ticks (1200 = 2 min).</summary>
        public int TradeRefusedMemoryTicks = 1200;
        /// <summary>A deal is fair when what the judge receives is worth at least this percent of what it pays.</summary>
        public int TradeFairPercent = 75;
        /// <summary>Most units of one ware asked for in one offer.</summary>
        public int TradeMaxWant = 4;
        /// <summary>
        /// A ware the faction could make itself is still traded for when it has at least this many free units of surplus
        /// to pay with (trade for target builds and to shed surplus); wares it lacks the land for are traded for at once.
        /// </summary>
        public int TradeShedSurplus = 10;
    }
}
