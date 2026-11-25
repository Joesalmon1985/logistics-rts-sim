namespace God.Sim
{
    /// <summary>Tuning constants of the economy (P2, stage 3). Change here only. Times are sim ticks (10 per second).</summary>
    public static class EconomyTuning
    {
        // ---- carriers and builders ----
        /// <summary>Carriers each HQ holds.</summary>
        public const int CarriersPerHq = 6;
        /// <summary>Builders each HQ holds.</summary>
        public const int BuildersPerHq = 2;
        /// <summary>Ticks a carrier takes per lattice step (5 m): 0.3 s.</summary>
        public const int CarrierStepTicks = 3;
        public const int BuilderStepTicks = 6;
        /// <summary>Ticks a level 1 harvester takes per lattice step (see <see cref="HarvesterStepTicks"/> for the levels).</summary>
        public const int HarvesterStepTicksL1 = 6;

        // ---- buffers ----
        /// <summary>Most units of one input ware a consumer holds or has on the way.</summary>
        public const int InputBuffer = 2;
        /// <summary>Most units of one ware waiting at a building's flag; a producer stops when it would overflow.</summary>
        public const int OutputCap = 6;
        /// <summary>As <see cref="OutputCap"/> for harvesters: room for two level 3 cartloads, so a carrier's late pickup does not stop the next trip.</summary>
        public const int HarvesterOutputCap = 12;

        // ---- harvesting and harvester levels ----
        /// <summary>Wares one pile gives at level 1 (a pile is a cartload). See <see cref="WaresPerPileAt"/>.</summary>
        public const int WaresPerPile = 4;
        /// <summary>Ticks a level 1 harvester spends at the pile. See <see cref="GatherTicksAt"/>.</summary>
        public const int GatherTicks = 20;
        /// <summary>Highest harvester level (level 1 is the built harvester).</summary>
        public const int MaxHarvesterLevel = 3;
        /// <summary>Ticks of work at the harvester once every upgrade ware is in (5 s).</summary>
        public const int UpgradeWorkTicks = 50;
        /// <summary>A level 3 harvester whose hex has no pile makes one ware on its own every this many ticks (40 s).</summary>
        public const int TrickleTicks = 400;

        /// <summary>Wares per pile by level: 4, 5, 6.</summary>
        public static int WaresPerPileAt(int level) => level >= 3 ? 6 : level == 2 ? 5 : WaresPerPile;

        /// <summary>Ticks at the pile by level: level 2 a third faster (x3/4), level 3 twice as fast (x1/2).</summary>
        public static int GatherTicksAt(int level) => level >= 3 ? GatherTicks / 2 : level == 2 ? GatherTicks * 3 / 4 : GatherTicks;

        /// <summary>
        /// Ticks of one lattice step of a harvester walker by level: 6 at level 1; at level 2 steps alternate 5 and 4 ticks
        /// (4.5 on average: a third faster); 3 at level 3 (twice as fast).
        /// </summary>
        public static int HarvesterStepTicks(int level, int stepIndex) =>
            level >= 3 ? HarvesterStepTicksL1 / 2 : level == 2 ? ((stepIndex & 1) == 0 ? 5 : 4) : HarvesterStepTicksL1;

        // ---- construction ----
        public const int BuildTicksSmall = 60;
        public const int BuildTicksMedium = 100;
        public const int BuildTicksHq = 150;

        // ---- production cycles (ticks) ----
        public const int SawmillTicks = 40;
        public const int KilnTicks = 50;
        public const int SmelterTicks = 60;
        public const int BakeryTicks = 40;
        public const int MortarMillTicks = 50;
        public const int FoundryTicks = 60;
        public const int PotteryTicks = 50;
        public const int ForgeTicks = 70;
        public const int TanneryTicks = 50;
        public const int WeaverTicks = 50;
        /// <summary>Every armoury makes one piece of gear per this many ticks.</summary>
        public const int ArmouryTicks = 80;
        public const int BarracksTicks = 80;

        // ---- construction costs (stage 3) ----
        /// <summary>Harvesters: 2 log, 1 stone (raw goods only, so any start can begin).</summary>
        public const int SmallLog = 2, SmallStone = 1;
        /// <summary>Processors: 2 plank, 1 brick, 1 stone.</summary>
        public const int ProcessorPlank = 2, ProcessorBrick = 1, ProcessorStone = 1;
        /// <summary>Armouries and the barracks: 2 plank, 2 brick, 1 mortar.</summary>
        public const int ArmouryPlank = 2, ArmouryBrick = 2, ArmouryMortar = 1;
        /// <summary>HQ: 3 plank, 2 brick, 2 mortar, 2 stone.</summary>
        public const int HqPlank = 3, HqBrick = 2, HqMortar = 2, HqStone = 2;

        // ---- harvester upgrades ----
        /// <summary>Level 1 to 2: 2 plank, 2 brick.</summary>
        public const int Level2Plank = 2, Level2Brick = 2;
        /// <summary>Level 2 to 3: 2 mortar, 2 iron, 2 bread.</summary>
        public const int Level3Mortar = 2, Level3Iron = 2, Level3Bread = 2;

        // ---- every HQ that exists at the game start ----
        public const int StartLog = 8, StartStone = 6, StartClay = 6, StartGrain = 4, StartPlank = 6, StartBrick = 4, StartMortar = 2;
    }
}
