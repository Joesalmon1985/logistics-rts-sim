using God.Sim.Population;

namespace God.Sim.Unified
{
    /// <summary>
    /// UN2 (contracts 23.5, 23.9, EP-01 executor half): TAKE and DROP through the one <c>Goods.Move</c> with <c>Reach.Of(why)</c>.
    /// TAKE sources: a ground lot (<c>PickUp</c>), a lot in any bin, storage, workstation output or container (<c>Withdraw</c>), a
    /// person's held-out lot (<c>HandOver</c>, through <c>UnifiedHooks.HeldOutLookup</c>), and the carry of a person who is asleep,
    /// unconscious or dead (<c>Grab</c>). From an awake person's ordinary carry a TAKE is <see cref="MotorStatus.Resisted"/>: nothing
    /// moves and the holder perceives the attempt. No bin, lot or person is ever asked who it "belongs" to: the only refusals are
    /// reach, room, weight, count and existence.
    /// <para>Targets: <c>Lot</c> (any lot within reach, wherever it lies), <c>Npc</c> (that person's held-out lot for the actor if
    /// any, else the lot named by referent 1, else its first carried lot), <c>Building</c> (the first lot of its workstation output,
    /// else of its storage). <see cref="PrimitiveControl.Quantity"/> 0 takes the whole lot, else that many units.</para>
    /// </summary>
    public static partial class Motor
    {
        private struct TakeFrom
        {
            public Holder From;
            public int Lot, Count, Holder;
            public Ware Ware;
            public MoveReason Why;
            public bool Resisted;
        }

        private static MotorStatus Take(GodState s, EventBuffer ev, Npc npc, UnifiedNpc u, in PrimitiveControl c, ControlOrigin origin, bool apply)
        {
            var st = ResolveTake(s, npc, in c, out var t);
            if (st != MotorStatus.Accepted) return st;
            if (t.Resisted)
            {
                if (apply)
                {
                    var h = s.Unification.PersonOf(t.Holder);
                    if (h != null) { h.ResistedBy = npc.Id; h.ResistedTick = s.Tick; }
                    UnifiedHooks.OnTakeResisted(s, ev, npc.Id, t.Holder, t.Lot, origin);
                }
                return MotorStatus.Resisted;
            }
            st = DryMove(s, npc, t.From, t.Lot, t.Count, Holder.Carry(npc.Id), t.Why, t.Ware);
            if (st != MotorStatus.Accepted || !apply) return st;
            var r = Goods.Move(s, ev, t.From, t.Lot, t.Count, Holder.Carry(npc.Id), t.Why, npc.Id);
            if (!r.Ok) return StatusOf(r.Status);
            if (t.Holder != 0)
            {
                var h = s.Unification.PersonOf(t.Holder);
                if (h != null && h.Equipped == t.Lot && Motor.CarrySlotOf(s.Population.Find(t.Holder), t.Lot) < 0) h.Equipped = 0;
                if (t.Why == MoveReason.Grab) UnifiedHooks.OnGrabbed(s, ev, npc.Id, t.Holder, r.IntoLot, t.Count, origin);
            }
            s.Unification.Index.MarkDirty();
            return Start(s, u, Prim.Take, s.Tuning.Unification.UN2.TakeTicks, c.Target, origin, true);
        }

        private static MotorStatus ResolveTake(GodState s, Npc npc, in PrimitiveControl c, out TakeFrom t)
        {
            t = default;
            var p = s.Population;
            switch (c.Target.Kind)
            {
                case TargetKind.Lot:
                {
                    int lot = c.Target.Id;
                    if (lot <= 0) return MotorStatus.Invalid;
                    if (CarrySlotOf(npc, lot) >= 0) return MotorStatus.Invalid; // already in hand
                    if (FindGround(p, lot, out int node, out var gl)) { t.From = Holder.Ground(node); t.Lot = lot; t.Ware = gl.Ware; t.Why = MoveReason.PickUp; return Count(in c, gl.Count, ref t); }
                    foreach (var b in p.Bins)
                        if (FindInBin(b, lot, out var bl)) { t.From = Holder.Bin(b.Id); t.Lot = lot; t.Ware = bl.Ware; t.Why = MoveReason.Withdraw; return Count(in c, bl.Count, ref t); }
                    // in someone's carry: only bodies within reach can be touched
                    var holder = CarrierNear(s, npc, lot);
                    if (holder == null) return MotorStatus.Invalid;
                    return FromPerson(s, npc, holder, lot, in c, ref t);
                }
                case TargetKind.Npc:
                {
                    var holder = p.Find(c.Target.Id);
                    if (holder == null || holder.Id == npc.Id || !Bodies.IsPerson(holder)) return MotorStatus.Invalid;
                    int lot = 0;
                    if (holder.Carry != null)
                    {
                        foreach (var l in holder.Carry)
                            if (!l.Empty && HeldOut(s, l.Id, out var ho) && (ho.To == 0 || ho.To == npc.Id)) { lot = l.Id; break; }
                        if (lot == 0 && c.R1.Kind == ReferentKind.Lot && CarrySlotOf(holder, c.R1.Id) >= 0) lot = c.R1.Id;
                        if (lot == 0) foreach (var l in holder.Carry) if (!l.Empty) { lot = l.Id; break; }
                    }
                    if (lot == 0) return MotorStatus.NotEnough;
                    return FromPerson(s, npc, holder, lot, in c, ref t);
                }
                case TargetKind.Building:
                {
                    var b = s.Unification.BuildingById(c.Target.Id);
                    if (b == null) return MotorStatus.Invalid;
                    foreach (int binId in new BinPair(b.WorkOut, b.Storage))
                    {
                        var bin = binId != 0 ? Goods.BinById(p, binId) : null;
                        if (bin == null) continue;
                        foreach (var l in bin.Lots)
                            if (!l.Empty) { t.From = Holder.Bin(bin.Id); t.Lot = l.Id; t.Ware = l.Ware; t.Why = MoveReason.Withdraw; return Count(in c, l.Count, ref t); }
                    }
                    return MotorStatus.NotEnough;
                }
                default: return MotorStatus.Invalid;
            }
        }

        /// <summary>Two bin ids in order without allocating (workstation output, then storage).</summary>
        private struct BinPair
        {
            private readonly int _a, _b; private int _i;
            public BinPair(int a, int b) { _a = a; _b = b; _i = -1; }
            public BinPair GetEnumerator() => this;
            public int Current => _i == 0 ? _a : _b;
            public bool MoveNext() => ++_i < 2;
        }

        private static MotorStatus FromPerson(GodState s, Npc npc, Npc holder, int lot, in PrimitiveControl c, ref TakeFrom t)
        {
            int slot = CarrySlotOf(holder, lot);
            if (slot < 0) return MotorStatus.Invalid;
            var l = holder.Carry[slot];
            t.From = Holder.Carry(holder.Id); t.Lot = lot; t.Ware = l.Ware; t.Holder = holder.Id;
            if (HeldOut(s, lot, out var ho) && ho.Npc == holder.Id && (ho.To == 0 || ho.To == npc.Id)) t.Why = MoveReason.HandOver;
            else if (Incapacitated(s, holder)) t.Why = MoveReason.Grab;
            else
            {
                // an awake person keeps hold: perceived only when the taker could actually touch the carry
                if (s.Territory.Grid.Distance(npc.Node, holder.Node) > Reach.Touch) return MotorStatus.OutOfReach;
                t.Resisted = true;
                return Count(in c, l.Count, ref t);
            }
            return Count(in c, l.Count, ref t);
        }

        /// <summary>
        /// The hold-out of a lot: UN7's <c>UnifiedHooks.HeldOutLookup</c> when it answers; otherwise the live (unexpired) record in
        /// <see cref="UnifiedState.HoldOuts"/> (the UN0 shape UN7 writes; fixtures write it directly until UN7 lands).
        /// </summary>
        public static bool HeldOut(GodState s, int lot, out HoldOut h)
        {
            if (UnifiedHooks.HeldOutLookup(s, lot, out h)) return true;
            foreach (var x in s.Unification.HoldOuts) if (x.Lot == lot && x.Expires >= s.Tick) { h = x; return true; }
            h = default; return false;
        }

        /// <summary>Asleep, unconscious (health at or below <c>UN2.UnconsciousHealthPermille</c>) or dead: the body cannot resist.</summary>
        public static bool Incapacitated(GodState s, Npc n) =>
            !n.Alive || n.Action.Asleep || n.HealthPermille <= s.Tuning.Unification.UN2.UnconsciousHealthPermille;

        private static MotorStatus Count(in PrimitiveControl c, int have, ref TakeFrom t)
        {
            int want = c.Quantity > 0 ? c.Quantity : have;
            if (want > have || want <= 0) return MotorStatus.NotEnough;
            t.Count = want;
            return MotorStatus.Accepted;
        }

        private static bool FindGround(PopulationState p, int lot, out int node, out ItemLot l)
        {
            var a = p.Lots;
            int lo = 0, hi = a.Count - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                int v = a[mid].Lot.Id;
                if (v == lot) { node = a[mid].Node; l = a[mid].Lot; return !l.Empty; }
                if (v < lot) lo = mid + 1; else hi = mid - 1;
            }
            node = -1; l = default; return false;
        }

        private static bool FindInBin(LotBin b, int lot, out ItemLot l)
        {
            var a = b.Lots;
            int lo = 0, hi = a.Count - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                int v = a[mid].Id;
                if (v == lot) { l = a[mid]; return !l.Empty; }
                if (v < lot) lo = mid + 1; else hi = mid - 1;
            }
            l = default; return false;
        }

        /// <summary>The person within touch reach of the actor (spatial index) whose carry holds <paramref name="lot"/>, or null.</summary>
        private static Npc CarrierNear(GodState s, Npc npc, int lot)
        {
            var ix = s.Unification.Index;
            var g = s.Territory.Grid;
            var p = s.Population;
            for (int dir = -1; dir < 6; dir++)
            {
                int node = dir < 0 ? npc.Node : g.Neighbour(npc.Node, dir);
                if (node < 0) continue;
                int n = ix.At(s, node, SpatialKind.Npc, s.Unification.Scratch);
                for (int i = 0; i < n; i++)
                {
                    var h = p.Find(s.Unification.Scratch[i].Id);
                    if (h != null && h.Id != npc.Id && h.Node == node && CarrySlotOf(h, lot) >= 0) return h;
                }
            }
            // the dead are not entities of the index (living persons only): a dead holder on the touched nodes
            foreach (var h in p.Npcs)
                if (h.Id != npc.Id && Bodies.IsPerson(h) && !Bodies.IsLivingPerson(h) && g.Distance(h.Node, npc.Node) <= Reach.Touch && CarrySlotOf(h, lot) >= 0) return h;
            return null;
        }

        // ---------------- DROP ----------------

        /// <summary>DROP the source lot (or <see cref="PrimitiveControl.Quantity"/> units of it) on the ground at the actor's node.</summary>
        private static MotorStatus Drop(GodState s, EventBuffer ev, Npc npc, UnifiedNpc u, in PrimitiveControl c, ControlOrigin origin, bool apply)
        {
            int slot = CarrySlotOf(npc, c.SourceLot);
            if (slot < 0) return MotorStatus.Invalid;
            var l = npc.Carry[slot];
            int count = c.Quantity > 0 ? c.Quantity : l.Count;
            if (count > l.Count) return MotorStatus.NotEnough;
            var to = Holder.Ground(npc.Node);
            var st = DryMove(s, npc, Holder.Carry(npc.Id), c.SourceLot, count, to, MoveReason.Drop, l.Ware);
            if (st != MotorStatus.Accepted || !apply) return st;
            var r = Goods.Move(s, ev, Holder.Carry(npc.Id), c.SourceLot, count, to, MoveReason.Drop, npc.Id);
            if (!r.Ok) return StatusOf(r.Status);
            if (u.Equipped == c.SourceLot && CarrySlotOf(npc, c.SourceLot) < 0) u.Equipped = 0;
            s.Unification.Index.MarkDirty();
            return Start(s, u, Prim.Drop, s.Tuning.Unification.UN2.DropTicks, new TargetRef(TargetKind.Node, npc.Node), origin, true, c.SourceLot);
        }

        // ---------------- the dry run of Goods.Move (no effect) ----------------

        /// <summary>The refusal <c>Goods.Move</c> would give (reach of <c>Reach.Of(why)</c>, room and weight), without moving anything.</summary>
        private static MotorStatus DryMove(GodState s, Npc npc, Holder from, int lot, int count, Holder to, MoveReason why, Ware w)
        {
            int reach = Reach.Of(why);
            if (reach >= 0 && (!Goods.InReach(s, npc.Id, from, reach) || !Goods.InReach(s, npc.Id, to, reach))) return MotorStatus.OutOfReach;
            switch (to.Kind)
            {
                case HolderKind.Carry:
                {
                    var t = s.Population.Find(to.Id);
                    if (t == null || !t.Alive) return MotorStatus.Invalid;
                    int free = s.Tuning.Population.CarryWeight - PopulationModule.CarryWeight(t);
                    if (free / System.Math.Max(1, PopulationModule.Weight(w)) < count) return MotorStatus.NoRoom;
                    var probe = ProbeOf(s, from, lot);
                    foreach (var l in t.Carry) if (l.Empty || UnifiedGoods.Mergeable(l, probe)) return MotorStatus.Accepted;
                    return MotorStatus.NoRoom;
                }
                case HolderKind.Ground: return to.Id >= 0 && to.Id < s.Territory.Grid.Count ? MotorStatus.Accepted : MotorStatus.Invalid;
                case HolderKind.Bin:
                {
                    var b = Goods.BinById(s.Population, to.Id);
                    if (b == null) return MotorStatus.Invalid;
                    return b.WeightCap == 0 || b.Weight + PopulationModule.Weight(w) * count <= b.WeightCap ? MotorStatus.Accepted : MotorStatus.Full;
                }
            }
            return MotorStatus.Invalid;
        }

        private static ItemLot ProbeOf(GodState s, Holder from, int lot)
        {
            var p = s.Population;
            switch (from.Kind)
            {
                case HolderKind.Ground: return FindGround(p, lot, out _, out var g) ? g : default;
                case HolderKind.Bin: { var b = Goods.BinById(p, from.Id); return b != null && FindInBin(b, lot, out var l) ? l : default; }
                case HolderKind.Carry: { var n = p.Find(from.Id); int i = n != null ? CarrySlotOf(n, lot) : -1; return i >= 0 ? n.Carry[i] : default; }
            }
            return default;
        }

        private static MotorStatus StatusOf(MoveStatus m)
        {
            switch (m)
            {
                case MoveStatus.Ok: return MotorStatus.Accepted;
                case MoveStatus.NotEnough: return MotorStatus.NotEnough;
                case MoveStatus.NoRoom: return MotorStatus.NoRoom;
                case MoveStatus.OutOfReach: return MotorStatus.OutOfReach;
                default: return MotorStatus.Invalid;
            }
        }
    }
}
