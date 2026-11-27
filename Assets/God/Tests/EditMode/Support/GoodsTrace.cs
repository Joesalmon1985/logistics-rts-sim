using System.Collections.Generic;
using System.Linq;
using God.Sim;
using God.Sim.Population;

namespace God.Sim.Tests
{
    /// <summary>
    /// Follows item lot ids through the event stream (F0.16; SHARED_CONTRACTS 2.4 C3, A1 C6; frozen): <see cref="Produced"/>
    /// creates an id (and may merge it on arrival), <see cref="ItemMoved"/> splits (a new id from <c>ParentLot</c>) and merges
    /// (<c>EndedLot</c> ends), <see cref="Consumed"/> uses goods up (the id ends at count 0). Lot 0 (goods used at once, A9)
    /// is exempt. Start it from a state (the ids already held, e.g. founder packs), then feed it every event in order.
    /// </summary>
    public sealed class GoodsTrace
    {
        /// <summary>How an id ended (or None while it lives).</summary>
        public enum End : byte { None = 0, Consumed = 1, Merged = 2 }

        public sealed class LotLife
        {
            public int Id, Count, Parent, MergedInto;
            public Ware Ware;
            public End Ended;
            public long BornTick = -1, EndTick = -1;
        }

        public readonly SortedDictionary<int, LotLife> Lots = new SortedDictionary<int, LotLife>();
        public readonly List<SimEvent> Events = new List<SimEvent>();
        /// <summary>Breaks of C3 seen while feeding (an event naming an id that does not live, counts that go negative).</summary>
        public readonly List<string> Problems = new List<string>();

        /// <summary>A trace that knows the lots already held in <paramref name="s"/> (carries, ground, bins).</summary>
        public static GoodsTrace Start(GodState s)
        {
            var t = new GoodsTrace();
            foreach (var (id, ware, count) in Held(s)) t.Lots[id] = new LotLife { Id = id, Ware = ware, Count = count };
            return t;
        }

        /// <summary>Every lot held now: (id, ware, count), carries, then the ground, then bins.</summary>
        public static List<(int id, Ware ware, int count)> Held(GodState s)
        {
            var l = new List<(int, Ware, int)>();
            var p = s.Population;
            if (p == null) return l;
            foreach (var n in p.Npcs) foreach (var c in n.Carry) if (!c.Empty) l.Add((c.Id, c.Ware, c.Count));
            foreach (var g in p.Lots) l.Add((g.Lot.Id, g.Lot.Ware, g.Lot.Count));
            foreach (var b in p.Bins) foreach (var c in b.Lots) l.Add((c.Id, c.Ware, c.Count));
            return l;
        }

        /// <summary>The ids (with their counts) the given holders hold now, sorted: the multiset of I-BS.7.</summary>
        public static List<(int id, int count)> IdMultiset(GodState s, IEnumerable<Holder> holders)
        {
            var l = new List<(int, int)>();
            var p = s.Population;
            foreach (var h in holders)
                switch (h.Kind)
                {
                    case HolderKind.Carry: { var n = p.Find(h.Id); if (n != null) foreach (var c in n.Carry) if (!c.Empty) l.Add((c.Id, c.Count)); break; }
                    case HolderKind.Ground: foreach (var g in p.Lots) if (g.Node == h.Id) l.Add((g.Lot.Id, g.Lot.Count)); break;
                    case HolderKind.Bin: { var b = Goods.BinById(p, h.Id); if (b != null) foreach (var c in b.Lots) l.Add((c.Id, c.Count)); break; }
                }
            l.Sort();
            return l;
        }

        public void AddRange(IEnumerable<SimEvent> events) { foreach (var e in events) Add(e); }

        public void Add(SimEvent e)
        {
            switch (e)
            {
                case Produced pr:
                    Events.Add(e);
                    if (pr.Lot == 0) return; // used at once (A9)
                    Born(pr.Lot, pr.Ware, pr.Count, 0, e.Tick);
                    if (pr.IntoLot != pr.Lot) Merge(pr.Lot, pr.IntoLot, e.Tick);
                    return;
                case ItemMoved m:
                    Events.Add(e);
                    if (m.ParentLot != 0)
                    {
                        Take(m.ParentLot, m.Count, "split");
                        Born(m.Lot, m.Ware, m.Count, m.ParentLot, e.Tick);
                    }
                    else Need(m.Lot, "move");
                    if (m.EndedLot != 0)
                    {
                        if (m.EndedLot == m.Lot) Merge(m.Lot, m.IntoLot, e.Tick);       // the moved goods joined an older lot
                        else Merge(m.EndedLot, m.Lot, e.Tick);                       // an equal, newer lot joined the moved one
                    }
                    return;
                case Consumed c:
                    Events.Add(e);
                    if (c.Lot == 0) return;
                    if (Take(c.Lot, c.Count, "consume") == 0) { var l = Lots[c.Lot]; l.Ended = End.Consumed; l.EndTick = e.Tick; }
                    return;
                case OwnershipChanged _:
                    Events.Add(e);
                    return;
            }
        }

        private void Born(int id, Ware w, int count, int parent, long tick)
        {
            if (Lots.ContainsKey(id)) { Problems.Add("lot " + id + " born twice (tick " + tick + ")"); return; }
            Lots[id] = new LotLife { Id = id, Ware = w, Count = count, Parent = parent, BornTick = tick };
        }

        private LotLife Need(int id, string what)
        {
            if (!Lots.TryGetValue(id, out var l)) { Problems.Add(what + " of lot " + id + ", which never existed"); return null; }
            if (l.Ended != End.None) Problems.Add(what + " of lot " + id + ", which already ended (" + l.Ended + ")");
            return l;
        }

        private int Take(int id, int count, string what)
        {
            var l = Need(id, what);
            if (l == null) return -1;
            l.Count -= count;
            if (l.Count < 0) Problems.Add(what + " takes more of lot " + id + " than it held");
            return l.Count;
        }

        private void Merge(int ended, int into, long tick)
        {
            var a = Need(ended, "merge"); var b = Need(into, "merge into");
            if (a == null || b == null) return;
            b.Count += a.Count; a.Count = 0; a.Ended = End.Merged; a.MergedInto = into; a.EndTick = tick;
        }

        /// <summary>
        /// C3: every id that ever existed is live (held now with the traced count), consumed, or merged; and every held id is
        /// one the trace knows. Returns one line per break (empty: sound).
        /// </summary>
        public List<string> CheckC3(GodState s)
        {
            var bad = new List<string>(Problems);
            var held = new Dictionary<int, int>();
            foreach (var (id, _, count) in Held(s)) held[id] = count;
            foreach (var l in Lots.Values)
            {
                bool live = held.TryGetValue(l.Id, out int n);
                if (l.Ended == End.None && !live) bad.Add("C3: lot " + l.Id + " vanished (not held, not consumed, not merged)");
                if (l.Ended != End.None && live) bad.Add("C3: lot " + l.Id + " ended (" + l.Ended + ") but is still held");
                if (live && l.Ended == End.None && n != l.Count) bad.Add("C3: lot " + l.Id + " holds " + n + ", the events say " + l.Count);
            }
            foreach (var id in held.Keys) if (!Lots.ContainsKey(id)) bad.Add("C3: lot " + id + " is held but no event created it");
            return bad;
        }

        /// <summary>
        /// C6 (A1): every person-initiated <see cref="ItemMoved"/> (all but estate in and out, and death spills) had its actor
        /// within the move's reach of both holders' nodes. Needs the grid for distances.
        /// </summary>
        public List<string> CheckC6(NodeGrid g)
        {
            var bad = new List<string>();
            foreach (var m in Events.OfType<ItemMoved>())
            {
                if (m.Reason == MoveReason.EstateIn || m.Reason == MoveReason.EstateOut || m.Reason == MoveReason.DeathSpill || m.Actor <= 0) continue;
                int r = Reach.Of(m.Reason);
                if (r < 0) continue;
                if (m.ActorNode < 0) { bad.Add("C6: move of lot " + m.Lot + " at tick " + m.Tick + " has no actor node"); continue; }
                bool ownFrom = m.FromKind == HolderKind.Carry && m.FromId == m.Actor, ownTo = m.ToKind == HolderKind.Carry && m.ToId == m.Actor;
                if (!ownFrom && g.Distance(m.ActorNode, m.FromNode) > r) bad.Add("C6: " + m.Reason + " of lot " + m.Lot + " at tick " + m.Tick + " from " + g.Distance(m.ActorNode, m.FromNode) + " nodes (reach " + r + ")");
                if (!ownTo && g.Distance(m.ActorNode, m.ToNode) > r) bad.Add("C6: " + m.Reason + " of lot " + m.Lot + " at tick " + m.Tick + " to " + g.Distance(m.ActorNode, m.ToNode) + " nodes (reach " + r + ")");
            }
            return bad;
        }

        /// <summary>
        /// The events that touched one lot, in order, following it through splits (its children), merges (the lot it joined)
        /// and on to its consumption: <c>Produced(lot) -> ItemMoved* -> Consumed</c>.
        /// </summary>
        public List<SimEvent> Follow(int lotId) => Follow(Events, lotId);

        public static List<SimEvent> Follow(IEnumerable<SimEvent> events, int lotId)
        {
            var ids = new HashSet<int> { lotId };
            var l = new List<SimEvent>();
            foreach (var e in events)
                switch (e)
                {
                    case Produced p when p.Lot != 0 && (ids.Contains(p.Lot) || ids.Contains(p.IntoLot)):
                        l.Add(e); if (ids.Contains(p.Lot)) ids.Add(p.IntoLot); break;
                    case ItemMoved m when ids.Contains(m.Lot) || ids.Contains(m.ParentLot) || ids.Contains(m.IntoLot) || ids.Contains(m.EndedLot):
                        l.Add(e); if (ids.Contains(m.ParentLot)) ids.Add(m.Lot); if (ids.Contains(m.Lot)) ids.Add(m.IntoLot); break;
                    case Consumed c when c.Lot != 0 && ids.Contains(c.Lot):
                        l.Add(e); break;
                    case OwnershipChanged o when ids.Contains(o.Lot):
                        l.Add(e); break;
                }
            return l;
        }
    }
}
