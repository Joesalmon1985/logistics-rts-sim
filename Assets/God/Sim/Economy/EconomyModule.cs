using System;

namespace God.Sim
{
    public static class EconomyModule
    {
        /// <summary>Creates the economy state and gives the HQs that exist at the game start their stock and carriers.</summary>
        public static void StartGame(GodState s)
        {
            s.Economy = new EconomyState { PilesReserved = new int[s.Territory.Topology.HexCount] };
            foreach (var tb in s.Territory.Buildings)
            {
                var rec = AddRecord(s, tb, true);
                if (rec != null && rec.IsHq && rec.Built) GiveStartStock(rec);
            }
        }

        private static void GiveStartStock(EcoBuilding hq)
        {
            hq.Stock[(int)Ware.Plank] = EconomyTuning.StartPlank;
            hq.Stock[(int)Ware.Stone] = EconomyTuning.StartStone;
            hq.Stock[(int)Ware.Brick] = EconomyTuning.StartBrick;
            hq.Stock[(int)Ware.Log] = EconomyTuning.StartLog;
            hq.Stock[(int)Ware.Clay] = EconomyTuning.StartClay;
        }

        public static void Install(GodSim sim)
        {
            sim.RegisterHandler(new SetInputPriorityHandler());
            sim.RegisterHandler(new SetPausedHandler());
            sim.AddSystem(SystemOrder.Economy, new EconomySystem());
        }

        /// <summary>
        /// Creates the economy record of a territory building: an HQ (built unless its <see cref="Hq.Site"/> flag is set),
        /// or a construction site of an economy kind. Placeholders get none. Returns null for those.
        /// </summary>
        internal static EcoBuilding AddRecord(GodState s, Building tb, bool atStart)
        {
            var t = s.Territory; var e = s.Economy;
            bool isHq = tb.Size == BuildingSize.Hq;
            if (!isHq && !EconomyDefs.IsEconomyKind(tb.Kind)) return null;
            var rec = new EcoBuilding
            {
                Id = tb.Id, Faction = tb.Faction, Kind = isHq ? TerritoryTuning.HqKind : tb.Kind, IsHq = isHq,
                Node = tb.Node, FlagId = tb.FlagId, FlagNode = tb.FlagNode, Hex = t.Grid.HexOf(tb.Node),
            };
            if (isHq)
            {
                var hq = t.HqOf(tb.Id);
                rec.Built = hq != null && !hq.Site;
                if (rec.Built) { rec.IdleCarriers = EconomyTuning.CarriersPerHq; rec.IdleBuilders = EconomyTuning.BuildersPerHq; }
            }
            int at = e.Buildings.Count;
            while (at > 0 && e.Buildings[at - 1].Id > rec.Id) at--;
            e.Buildings.Insert(at, rec);
            return rec;
        }

        /// <summary>
        /// For scripted set-ups (the enemy base, tests): makes a placed building finished at once, without carrying wares.
        /// Returns false when the building is unknown.
        /// </summary>
        public static bool CompleteInstantly(GodState s, int buildingId)
        {
            var tb = s.Territory.BuildingById(buildingId);
            if (tb == null) return false;
            var rec = s.Economy.Find(buildingId) ?? AddRecord(s, tb, false);
            if (rec == null) return false;
            if (!rec.Built) EconomySystem.Complete(s, s.Economy, s.Territory, rec, null);
            return true;
        }

        // ---------------- state hash ----------------

        /// <summary>FNV-1a over the parts of the state the economy and territory drive. Equal for equal seed and commands.</summary>
        public static ulong StateHash(GodState s)
        {
            ulong h = 14695981039346656037UL;
            void Add(long v) { unchecked { for (int i = 0; i < 8; i++) { h ^= (ulong)((v >> (i * 8)) & 0xFF); h *= 1099511628211UL; } } }
            void AddArr(int[] a) { Add(a.Length); foreach (int x in a) Add(x); }
            Add(s.Tick);
            var t = s.Territory;
            AddArr(t.Piles); Add(t.RollCount); Add(t.WastedPiles); Add(t.NextId);
            foreach (var b in t.Buildings) { Add(b.Id); Add(b.Node); Add(b.FlagId); }
            foreach (var r in t.Roads) { Add(r.Id); AddArr(r.Nodes); }
            foreach (var hq in t.Hqs) { Add(hq.Id); Add(hq.Site ? 1 : 0); }
            var e = s.Economy;
            foreach (var b in e.Buildings)
            {
                Add(b.Id); Add(b.Built ? 1 : 0); Add(b.Paused ? 1 : 0); Add(b.BuildProgress); Add(b.WorkLeft); Add(b.Produced);
                AddArr(b.In); AddArr(b.Incoming); AddArr(b.Out); AddArr(b.OutReserved); AddArr(b.Stock); AddArr(b.StockReserved);
                foreach (var p in b.Prio) Add((int)p);
                Add(b.IdleCarriers); Add(b.IdleBuilders);
            }
            foreach (var w in e.Walkers)
            {
                Add(w.Id); Add((int)w.Kind); Add(w.Home); Add((int)w.Phase); AddArr(w.Path); Add(w.Index); Add(w.Progress);
                Add((int)w.JobWare); Add(w.Loaded ? 1 : 0); Add(w.SrcId); Add(w.DstId);
            }
            foreach (var u in s.UnitQueue) { Add((int)u.Faction); Add(u.BuildingId); Add((int)u.Kind); }
            AddArr(e.PilesReserved); Add(e.LostWares); Add(e.DeliveredWares);
            return h;
        }
    }
}
