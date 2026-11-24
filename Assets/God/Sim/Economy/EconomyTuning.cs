namespace God.Sim
{
    /// <summary>Tuning constants of the economy (P2). Change here only. Times are sim ticks (10 per second).</summary>
    public static class EconomyTuning
    {
        // ---- carriers and builders ----
        /// <summary>Carriers each HQ holds.</summary>
        public const int CarriersPerHq = 6;
        /// <summary>Builders each HQ holds.</summary>
        public const int BuildersPerHq = 2;
        /// <summary>Ticks a carrier takes per lattice step (5 m): 0.5 s.</summary>
        public const int CarrierStepTicks = 5;
        public const int BuilderStepTicks = 6;
        public const int HarvesterStepTicks = 6;

        // ---- buffers ----
        /// <summary>Most units of one input ware a consumer holds or has on the way.</summary>
        public const int InputBuffer = 2;
        /// <summary>Most units of one ware waiting at a building's flag; a producer stops when it would overflow.</summary>
        public const int OutputCap = 6;

        // ---- harvesting ----
        /// <summary>Wares one pile gives (a pile is a cartload).</summary>
        public const int WaresPerPile = 3;
        /// <summary>Ticks a harvester spends at the pile.</summary>
        public const int GatherTicks = 20;

        // ---- construction ----
        public const int BuildTicksSmall = 60;
        public const int BuildTicksMedium = 100;
        public const int BuildTicksHq = 150;

        // ---- production cycles (ticks) ----
        public const int SawmillTicks = 40;
        public const int KilnTicks = 60;
        public const int SmelterTicks = 80;
        public const int BarracksTicks = 80;
        public const int VehicleWorkshopTicks = 80;

        // ---- construction costs ----
        public const int SmallPlank = 2, SmallStone = 1;
        public const int MediumPlank = 3, MediumBrick = 2, MediumStone = 1;
        public const int HqPlank = 4, HqStone = 4;

        // ---- every HQ that exists at the game start ----
        public const int StartPlank = 8, StartStone = 6, StartBrick = 4;
        /// <summary>A few logs and some clay so nothing is stuck before the first dice rolls.</summary>
        public const int StartLog = 8, StartClay = 6;
    }
}
