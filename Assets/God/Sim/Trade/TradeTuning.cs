namespace God.Sim
{
    /// <summary>Tuning constants of trade envoys (P8). Change here only. Times are ticks (10 per second), lengths millimetres.</summary>
    public static class TradeTuning
    {
        /// <summary>Most wares (counted in units) in one direction of an offer.</summary>
        public const int MaxWaresPerSide = 8;
        /// <summary>Most envoys one faction may have walking at once.</summary>
        public const int MaxEnvoysPerFaction = 4;
        /// <summary>Hit points of an envoy.</summary>
        public const int EnvoyHealth = 40;
        /// <summary>Millimetres per tick: 10 m/s overland, a little under a carrier (5 m in 3 ticks) so units have a chance to hit it.</summary>
        public const int EnvoySpeedMmPerTick = 1000;
        /// <summary>An envoy has arrived when it is this close to an HQ node.</summary>
        public const int ArriveMm = 2500;
        /// <summary>The human answers within this many ticks of the envoy arriving (60 s) or the offer is refused.</summary>
        public const int AnswerTicks = 600;
        /// <summary>An envoy that cannot move for this many ticks gives up (refused, or its goods lost when homeward).</summary>
        public const int StuckTicks = 50;
        /// <summary>Completed trades kept in the history.</summary>
        public const int HistorySize = 64;
        /// <summary>System order: after the military (units shoot envoys), before the faction system.</summary>
        public const int SystemOrder = 350;
        /// <summary>The default judge accepts when the target gets at least this percent of the value it pays.</summary>
        public const int FairPercent = 75;

        /// <summary>Trade value of one unit of a ware, for the default judge.</summary>
        public static int ValueOf(Ware w)
        {
            switch (w)
            {
                case Ware.Log: case Ware.Clay: case Ware.Grain: case Ware.Rock: return 2;
                case Ware.Stone: case Ware.Ore: case Ware.Plank: case Ware.Kindling: return 3;
                case Ware.Brick: return 4;
                case Ware.Iron: return 6;
                default: return 0;
            }
        }

        /// <summary>True for a ware that can sit in an HQ stock and so be traded (Rock never does).</summary>
        public static bool IsTradable(Ware w) => w != Ware.None && w != Ware.Rock && (int)w > 0 && (int)w < Wares.Count;
    }
}
