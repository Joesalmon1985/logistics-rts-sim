namespace God.Sim
{
    /// <summary>Tuning constants of land, dice and territory (P1). Change here only.</summary>
    public static class TerritoryTuning
    {
        /// <summary>Sim ticks between dice rolls: 45 ticks at 10 Hz = 4.5 seconds (Q4: was 6 s; the stage 3 chain needs more raws).</summary>
        public const int DiceIntervalTicks = 45;

        /// <summary>Most piles a hex holds before further piles are wasted.</summary>
        public const int PileCap = 3;

        /// <summary>Two flags must be at least this many lattice steps apart.</summary>
        public const int FlagMinGap = 2;

        /// <summary>Kind string stored on a building until P2 brings real building types.</summary>
        public const string PlaceholderKind = "placeholder";

        /// <summary>Kind string of an HQ.</summary>
        public const string HqKind = "hq";
    }
}
