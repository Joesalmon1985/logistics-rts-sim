using System.Collections.Generic;

namespace God.Sim.Unified
{
    /// <summary>
    /// UN0 (contracts 23.4, 23.8, 23.14): <c>GodState.Unification</c>, the unified world. Exists only in a unified game (so every
    /// older game keeps its JSON and hash); hashed as the <c>unification</c> part after <c>divine</c>. Every list is sorted by its id
    /// (<see cref="People"/> by NpcId, <see cref="Origins"/> by lot, <see cref="Reservations"/> by (building, npc), <see cref="HoldOuts"/>
    /// by (npc, lot), <see cref="MessageExt"/> by message); ids come from the counters below and are never reused.
    /// <para>PARTIAL: a package adds its own state in <c>UnifiedState.UNn.cs</c> and hashes it in its <c>AddToUNn</c> slot (called in
    /// the fixed order below). The lists here are declared by UN0 so their hash order is frozen; their behaviour belongs to the
    /// owners of contracts 23.8.</para>
    /// </summary>
    public sealed partial class UnifiedState
    {
        /// <summary>The layout version of this record (1 at UN0); a package changing a shape it owns bumps nothing here (it adds fields).</summary>
        public int Layout = 1;

        public int NextSiteId = 1, NextBuildingId = 1, NextRawNodeId = 1, NextForageId = 1, NextWaterId = 1;
        /// <summary>The next exchange correlation id (contracts 23.9): allocated by the first gesture's speaker; analytics only.</summary>
        public uint NextCorr = 1;

        /// <summary>Provenance of every live lot (contracts 23.4), sorted by lot id. Written only by <c>Goods</c>.</summary>
        public List<LotOrigin> Origins = new List<LotOrigin>();
        /// <summary>The unified extension of every person (contracts 23.6), sorted by NpcId.</summary>
        public List<UnifiedNpc> People = new List<UnifiedNpc>();

        // ---- contracts 23.8 (owners: Sites UN4; Buildings, Reservations UN3b; RawNodes, ForagePlaces, WaterPoints UN1) ----
        public List<ConstructionSite> Sites = new List<ConstructionSite>();
        public List<Building> Buildings = new List<Building>();
        public List<Reservation> Reservations = new List<Reservation>();
        public List<RawNode> RawNodes = new List<RawNode>();
        public List<ForagePlace> ForagePlaces = new List<ForagePlace>();
        public List<WaterPoint> WaterPoints = new List<WaterPoint>();

        // ---- contracts 23.9 (owner UN7) ----
        public List<MsgExt> MessageExt = new List<MsgExt>();
        public List<HoldOut> HoldOuts = new List<HoldOut>();

        /// <summary>The person's extension, or null (binary search over <see cref="People"/>).</summary>
        public UnifiedNpc PersonOf(int npc)
        {
            int lo = 0, hi = People.Count - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                int v = People[mid].Npc;
                if (v == npc) return People[mid];
                if (v < npc) lo = mid + 1; else hi = mid - 1;
            }
            return null;
        }

        /// <summary>The person's extension, created (in sorted place) when missing.</summary>
        public UnifiedNpc PersonOrNew(int npc)
        {
            int lo = 0, hi = People.Count - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                int v = People[mid].Npc;
                if (v == npc) return People[mid];
                if (v < npc) lo = mid + 1; else hi = mid - 1;
            }
            var p = new UnifiedNpc { Npc = npc };
            People.Insert(lo, p);
            return p;
        }

        public ConstructionSite SiteById(int id) { foreach (var x in Sites) if (x.Id == id) return x; return null; }
        public Building BuildingById(int id) { foreach (var x in Buildings) if (x.Id == id) return x; return null; }

        /// <summary>The <c>unification</c> hash part (contracts 23.14): every field in declaration order, then the package slots.</summary>
        public void AddTo(ref StateHasher h)
        {
            h.Add(Layout);
            h.Add(NextSiteId); h.Add(NextBuildingId); h.Add(NextRawNodeId); h.Add(NextForageId); h.Add(NextWaterId); h.Add(NextCorr);
            h.Add(Origins.Count); foreach (var o in Origins) o.AddTo(ref h);
            h.Add(People.Count); foreach (var p in People) p.AddTo(ref h);
            h.Add(Sites.Count); foreach (var x in Sites) x.AddTo(ref h);
            h.Add(Buildings.Count); foreach (var x in Buildings) x.AddTo(ref h);
            h.Add(Reservations.Count); foreach (var x in Reservations) x.AddTo(ref h);
            h.Add(RawNodes.Count); foreach (var x in RawNodes) x.AddTo(ref h);
            h.Add(ForagePlaces.Count); foreach (var x in ForagePlaces) x.AddTo(ref h);
            h.Add(WaterPoints.Count); foreach (var x in WaterPoints) x.AddTo(ref h);
            h.Add(MessageExt.Count); foreach (var x in MessageExt) x.AddTo(ref h);
            h.Add(HoldOuts.Count); foreach (var x in HoldOuts) x.AddTo(ref h);
            AddToUN1(ref h); AddToUN2(ref h); AddToUN3(ref h); AddToUN4(ref h); AddToUN5(ref h);
            AddToUN6(ref h); AddToUN7(ref h); AddToUN8(ref h); AddToUN9(ref h); AddToUB(ref h);
        }

        partial void AddToUN1(ref StateHasher h);
        partial void AddToUN2(ref StateHasher h);
        partial void AddToUN3(ref StateHasher h);
        partial void AddToUN4(ref StateHasher h);
        partial void AddToUN5(ref StateHasher h);
        partial void AddToUN6(ref StateHasher h);
        partial void AddToUN7(ref StateHasher h);
        partial void AddToUN8(ref StateHasher h);
        partial void AddToUN9(ref StateHasher h);
        partial void AddToUB(ref StateHasher h);
    }
}
