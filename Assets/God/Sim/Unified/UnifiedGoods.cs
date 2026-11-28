using God.Sim.Population;

namespace God.Sim.Unified
{
    /// <summary>
    /// UN0 (contracts 23.4): the unified branch of <see cref="Goods"/>, called only from <c>Goods</c> and only while
    /// <see cref="Unified.On"/>. Goods have no title: every owner is <see cref="Owner.None"/>; lots merge on ware, spoil day and uses
    /// (never a unique ware); the origin table (<see cref="UnifiedState.Origins"/>) records provenance and follows every lot id.
    /// Nothing here pays a learning reward.
    /// </summary>
    public static class UnifiedGoods
    {
        /// <summary>The merge rule of unified mode: same ware, spoil day and uses, and not a unique ware. Owner and provenance never block a merge.</summary>
        public static bool Mergeable(in ItemLot a, in ItemLot b) =>
            !a.Empty && a.Ware == b.Ware && a.SpoilDay == b.SpoilDay && a.Uses == b.Uses && !Wares.IsUnique(b.Ware);

        /// <summary>The origin of a live lot (false when none is recorded).</summary>
        public static bool OriginOf(GodState s, int lot, out LotOrigin o)
        {
            o = default;
            var l = s.Unification?.Origins;
            if (l == null) return false;
            int i = Find(l, lot);
            if (i < 0) return false;
            o = l[i];
            return true;
        }

        /// <summary>
        /// Sets where a just-produced lot came from (the producing package calls this right after <c>Goods.Produce</c>, with the id
        /// it returned). Leaves a merged (<see cref="LotOrigin.Mixed"/>) origin's source as it is.
        /// </summary>
        public static void SetSource(GodState s, int lot, byte sourceKind, int sourceId)
        {
            var l = s.Unification?.Origins;
            if (l == null) return;
            int i = Find(l, lot);
            if (i < 0) return;
            var o = l[i];
            if (o.Mixed) return;
            o.SourceKind = sourceKind; o.SourceId = sourceId;
            l[i] = o;
        }

        // ---------------- called by Goods ----------------

        internal static void Produced(GodState s, int lot, int producer, ProduceReason why, int source)
        {
            Put(s, new LotOrigin { Lot = lot, Producer = producer, Reason = (byte)why, Tick = s.Tick, SourceKind = OriginSource.None, SourceId = source });
        }

        /// <summary>A split: the moved part (<paramref name="child"/>) takes the parent's origin.</summary>
        internal static void Split(GodState s, int parent, int child)
        {
            var l = s.Unification.Origins;
            int i = Find(l, parent);
            var o = i >= 0 ? l[i] : new LotOrigin { Tick = s.Tick };
            o.Lot = child;
            Put(s, o);
        }

        /// <summary>A merge: the surviving (lower) id <paramref name="into"/> keeps the combined origin; <paramref name="ended"/> is gone.</summary>
        internal static void Merged(GodState s, int into, int ended)
        {
            if (ended == 0 || ended == into) return;
            var l = s.Unification.Origins;
            int a = Find(l, into), b = Find(l, ended);
            if (b < 0) return;
            var ob = l[b];
            l.RemoveAt(b);
            if (a < 0) { ob.Lot = into; Put(s, ob); return; }
            a = Find(l, into);
            var oa = l[a];
            bool same = oa.Producer == ob.Producer && oa.Reason == ob.Reason && oa.SourceKind == ob.SourceKind && oa.SourceId == ob.SourceId;
            if (!same || ob.Mixed) oa.Mixed = true;
            if (ob.Tick < oa.Tick) oa.Tick = ob.Tick;
            l[a] = oa;
        }

        /// <summary>A lot ended (consumed in full).</summary>
        internal static void Ended(GodState s, int lot)
        {
            var l = s.Unification.Origins;
            int i = Find(l, lot);
            if (i >= 0) l.RemoveAt(i);
        }

        private static void Put(GodState s, LotOrigin o)
        {
            var l = s.Unification.Origins;
            int lo = 0, hi = l.Count - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                int v = l[mid].Lot;
                if (v == o.Lot) { l[mid] = o; return; }
                if (v < o.Lot) lo = mid + 1; else hi = mid - 1;
            }
            l.Insert(lo, o);
        }

        private static int Find(System.Collections.Generic.List<LotOrigin> l, int lot)
        {
            int lo = 0, hi = l.Count - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                int v = l[mid].Lot;
                if (v == lot) return mid;
                if (v < lot) lo = mid + 1; else hi = mid - 1;
            }
            return -1;
        }
    }
}
