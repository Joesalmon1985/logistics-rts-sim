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

        /// <summary>Raw values: log, clay, grain 2; stone and ore 3 (rock piles, shared by the quarry and the mine).</summary>
        public const int LogValue = 2, StoneValue = 3;
        /// <summary>What the armourer's work adds to the value of its two inputs.</summary>
        public const int GearWorkValue = 2;

        private static int[] _values;

        /// <summary>
        /// Trade value of one unit of a ware, for the default judge and the AI trader. Raws by the table above; a processed
        /// ware is worth its two raws (brick 4, plank 5, steel 6...); a piece of gear its two inputs plus
        /// <see cref="GearWorkValue"/> (sling 11, axe 12, sword 13...). Rock and the unused kindling: 2 and 0.
        /// </summary>
        public static int ValueOf(Ware w)
        {
            if (_values == null)
            {
                var v = new int[Wares.Count];
                v[(int)Ware.Log] = v[(int)Ware.Clay] = v[(int)Ware.Grain] = v[(int)Ware.Rock] = LogValue;
                v[(int)Ware.Stone] = v[(int)Ware.Ore] = StoneValue;
                foreach (var p in Wares.Processed) v[(int)p] = InputsValue(v, p);
                foreach (var g in Gear.Attack) v[(int)g] = InputsValue(v, g) + GearWorkValue;
                foreach (var g in Gear.Defence) v[(int)g] = InputsValue(v, g) + GearWorkValue;
                _values = v;
            }
            int i = (int)w;
            return i > 0 && i < _values.Length ? _values[i] : 0;
        }

        private static int InputsValue(int[] v, Ware w)
        {
            var maker = EconomyDefs.MakerOf(w);
            int n = 0;
            if (maker != null) for (int i = 0; i < Wares.Count; i++) n += maker.In[i] * v[i];
            return n;
        }

        /// <summary>True for a ware that can sit in an HQ stock and so be traded (Rock never does).</summary>
        public static bool IsTradable(Ware w) => w != Ware.None && w != Ware.Rock && w != Ware.Kindling && (int)w > 0 && (int)w < Wares.Count;
    }
}
