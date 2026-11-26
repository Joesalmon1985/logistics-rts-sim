using System.Collections.Generic;

namespace God.Sim
{
    /// <summary>One faction at the end of one sim day (DATA_WAREHOUSE_DESIGN 3.2, the daily faction snapshot). Integers only.</summary>
    public struct FactionDayRow
    {
        /// <summary>The sim day that just ended (day 0 first) and the tick it ended on.</summary>
        public long Day;
        public long Tick;
        public Faction Faction;
        public bool Eliminated;
        public int VictoryPoints;
        public int Hqs;
        public int Citadels;
        /// <summary>Territory buildings other than HQs (finished or not), and how many of those are finished.</summary>
        public int Buildings;
        public int BuildingsBuilt;
        public int Units;
        public int Walkers;
        /// <summary>Wares in the faction's HQ stores (all wares), and the same per ware indexed by (int)Ware.</summary>
        public int StockTotal;
        public int[] Stock;
        public int WarsActive;
        public int EnvoysOut;
    }

    /// <summary>
    /// Read-only builders of the snapshot rows (DATA_WAREHOUSE_DESIGN 3.2). Pure functions over <see cref="GodState"/>: they
    /// allocate fresh rows and write nothing, so calling them or not cannot change a run (test <c>SnapshotBuilders_AreReadOnly</c>).
    /// The host calls them after the last tick of each day (<see cref="SimCalendar.IsDayEnd"/>); the sim never does.
    /// </summary>
    public static class SnapshotBuilder
    {
        /// <summary>One row per faction of the game, ascending id.</summary>
        public static FactionDayRow[] FactionDay(GodState s)
        {
            var cal = SimCalendar.Of(s);
            long day = cal.Day(s.Tick) - (cal.IsDayEnd(s.Tick) ? 1 : 0);
            var rows = new FactionDayRow[s.FactionCount];
            for (int i = 0; i < rows.Length; i++)
            {
                var f = (Faction)i;
                var r = new FactionDayRow { Day = day < 0 ? 0 : day, Tick = s.Tick, Faction = f, Stock = new int[Wares.Count] };
                r.Eliminated = s.Factions != null && s.Factions.IsEliminated(f);
                r.VictoryPoints = FactionRules.VictoryPoints(s, f);
                foreach (var h in s.Territory.Hqs) if (h.Faction == f && !h.Site) { r.Hqs++; if (h.Citadel) r.Citadels++; }
                foreach (var b in s.Territory.Buildings)
                {
                    if (b.Faction != f || b.Size == BuildingSize.Hq) continue;
                    r.Buildings++;
                    var rec = s.Economy?.Find(b.Id);
                    if (rec != null && rec.Built) r.BuildingsBuilt++;
                }
                if (s.Military != null) r.Units = s.Military.CountUnits(f);
                if (s.Economy != null)
                {
                    foreach (var w in s.Economy.Walkers) if (w.Faction == f) r.Walkers++;
                    foreach (var b in s.Economy.Buildings)
                        if (b.Faction == f && b.IsHq) for (int w = 0; w < Wares.Count; w++) r.Stock[w] += b.Stock[w];
                    for (int w = 0; w < Wares.Count; w++) r.StockTotal += r.Stock[w];
                }
                if (s.Factions != null) r.WarsActive = s.Factions.Enemies(f).Count;
                if (s.Trade != null) r.EnvoysOut = s.Trade.EnvoyCountOf(f);
                rows[i] = r;
            }
            return rows;
        }
    }
}
