namespace God.Sim
{
    /// <summary>Tuning constants of factions, victory points, Citadels and capture (P7). Change here only.</summary>
    public static class FactionTuning
    {
        /// <summary>The first faction to reach this many VP wins.</summary>
        public const int VictoryPoints = 10;

        /// <summary>VP of a finished HQ.</summary>
        public const int HqVp = 1;
        /// <summary>VP of a Citadel (an upgraded HQ).</summary>
        public const int CitadelVp = 2;

        // ---- Citadel upgrade ----
        public const int CitadelPlank = 3, CitadelBrick = 4, CitadelStone = 4, CitadelIron = 3;
        /// <summary>Ticks of work once every Citadel ware is in: 30 s.</summary>
        public const int CitadelBuildTicks = 300;

        /// <summary>A captured HQ or Citadel passes to the attacker with this percent of its full health.</summary>
        public const int CaptureHealthPercent = 50;

        /// <summary>System order of the faction system: after the military, so a capture and the elimination it causes land in one tick.</summary>
        public const int SystemOrder = 400;
    }
}
