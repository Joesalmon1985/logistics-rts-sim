using System;
using System.Collections.Generic;
using God.Sim.Population;
using God.Sim.Population.Households;

namespace God.Sim.Estate
{
    /// <summary>The inheritance customs of M4 (MATERIAL 4.3). None: every asset becomes ownerless where it lies.</summary>
    public enum Custom : byte { None = 0, EqualChildren = 1, EldestChild = 2, PartnerFirst = 3, HouseholdKeeps = 4, BackToCommunity = 5, BuriedWithDead = 6 }

    /// <summary>The executor ladder's rungs (MATERIAL 4.2, Q13).</summary>
    public enum ExecutorRung : byte { NamedInWill = 1, Partner = 2, EldestAdultChild = 3, EldestHouseholdAdult = 4, Appointed = 5 }

    /// <summary>
    /// One open estate (MATERIAL 3), keyed by the deceased's NpcId. Its goods sit in an estate bin at the death node (A2); the
    /// split passes ownership in place, heirs collect within reach, and the whole estate settles by <see cref="DueTick"/>.
    /// The heir candidates are fixed at the death from the deceased's own believed kin and household (its mind is released then).
    /// </summary>
    public sealed class EstateRecord
    {
        public int Deceased;
        public Faction Faction;
        public long OpenedTick, SplitTick, DueTick;
        public int Bin, Node, Household;
        /// <summary>The partner(s) believed at the death (live partnerships), and acknowledged children (seniority order: born first, then id).</summary>
        public List<int> Partners = new List<int>();
        public List<int> Children = new List<int>();
        /// <summary>Bins holding split shares (each estate kind, at the same node).</summary>
        public List<int> ShareBins = new List<int>();
        public bool Split;
        public int Executor;
        public byte Custom;
        public int Transfers, Ownerless, Destroyed;

        public void AddTo(ref StateHasher h)
        {
            h.Add(Deceased); h.Add((int)Faction); h.Add(OpenedTick); h.Add(SplitTick); h.Add(DueTick); h.Add(Bin); h.Add(Node); h.Add(Household);
            h.Add(Partners.Count); foreach (int x in Partners) h.Add(x);
            h.Add(Children.Count); foreach (int x in Children) h.Add(x);
            h.Add(ShareBins.Count); foreach (int x in ShareBins) h.Add(x);
            h.Add(Split ? 1 : 0); h.Add(Executor); h.Add(Custom); h.Add(Transfers); h.Add(Ownerless); h.Add(Destroyed);
        }
    }

    /// <summary>
    /// The settlement seam (MATERIAL 4.2, Q13): choose the executor, read its rule, split, transfer. M4 has one
    /// implementation, by custom (<see cref="CustomSettlement"/>); wills, household decisions, communal inheritance, state law
    /// and estate tax are later implementations of the same seam. A split never creates a lot id other than by a split of an
    /// existing lot (<c>Goods.Move</c>), and never duplicates one.
    /// </summary>
    public interface IEstateSettlement
    {
        int ChooseExecutor(GodState s, EstateRecord e);
        Custom RuleOf(GodState s, EstateRecord e, int executor);
        void Split(GodState s, EventBuffer ev, EstateRecord e, int executor, Custom rule);
    }

    /// <summary>
    /// Estates (MATERIAL_INHERITANCE, SHARED_CONTRACTS 2.5, WORLD_ONTOLOGY 6.5 and A2). At a death the carry goes into an
    /// estate bin at the death node (<c>EstateIn</c>, owner <c>Estate(d)</c>, never merged). After the mourning delay the
    /// executor applies the custom: ownership passes in place (<c>SetOwner</c>); a stackable lot shared by several heirs is
    /// split into a share bin beside it (the moved part takes a new id, as every split does). Heirs within reach collect
    /// their lots (<c>Withdraw</c>); what is left at the deadline goes to the heir's household store (O4 delivery, temporary)
    /// or becomes ownerless on the ground there. Every step is a <see cref="Goods"/> call and an <c>EstateTransfer</c>.
    /// </summary>
    public static class Estates
    {
        /// <summary>Opens the estate of a person who just died (from <c>HouseholdHooks.OnDeath</c>, the tick of <c>NpcDied</c>).</summary>
        public static void Open(GodState s, EventBuffer ev, Npc d)
        {
            var hs = s.Households;
            var et = s.Tuning.Estate;
            var p = s.Population;
            int bin = Goods.NewBin(s, BinKind.Estate, Owner.Estate(d.Id), d.Node, 0);
            var e = new EstateRecord
            {
                Deceased = d.Id, Faction = d.Faction, OpenedTick = s.Tick, SplitTick = s.Tick + et.MournTicks, DueTick = s.Tick + et.SettleWithinTicks,
                Bin = bin, Node = d.Node, Household = d.Household,
            };
            // heir candidates from the deceased's own beliefs (acknowledged kin, MATERIAL 4.3), fixed now
            foreach (var pr in hs.Partnerships) if (pr.A == d.Id || pr.B == d.Id) e.Partners.Add(pr.A == d.Id ? pr.B : pr.A);
            e.Partners.Sort();
            var kin = d.Mind?.Kin;
            if (kin != null)
                foreach (var b in kin.Beliefs)
                {
                    if (b.Confidence <= et.AcknowledgeConfidence) continue;
                    if (b.Role == KinRole.Daughter || b.Role == KinRole.Son || b.Role == KinRole.FosterChild || b.Role == KinRole.StepChild && b.Confidence > 600)
                        e.Children.Add(b.Other);
                }
            e.Children.Sort((a, b) => { var na = p.Find(a); var nb = p.Find(b); long ta = na?.BornTick ?? 0, tb = nb?.BornTick ?? 0; return ta != tb ? ta.CompareTo(tb) : a.CompareTo(b); });
            int lots = 0;
            for (int i = 0; i < d.Carry.Length; i++)
            {
                var l = d.Carry[i];
                if (l.Empty) continue;
                // household goods carried by a member stay the household's; the deceased's own go to the estate
                bool household = l.Owner.Kind == OwnerKind.Household;
                var r = Goods.Move(s, ev, Holder.Carry(d.Id), l.Id, l.Count, Holder.Bin(bin), MoveReason.EstateIn, 0,
                    household ? OwnerRule.Keep : OwnerRule.Set, Owner.Estate(d.Id), MergeRule.Never);
                if (!r.Ok) throw new InvalidOperationException("estate in failed: " + r.Status);
                lots++;
            }
            hs.Estates.Add(e);
            hs.Stats.Estates++;
            // its own goods lying in a household store (an inheritance delivered there) stay with that household (HOUSEHOLDS
            // "Inheritance hooks": the store stays with the household), owned by it in place
            foreach (var b in s.Population.Bins)
            {
                if (b.Kind != BinKind.HouseholdStore || b.Owner.Kind != OwnerKind.Household) continue;
                for (int k = 0; k < b.Lots.Count; k++)
                {
                    var l = b.Lots[k];
                    if (l.Owner.Kind != OwnerKind.Npc || l.Owner.Id != d.Id) continue;
                    if (!Goods.SetOwner(s, ev, Holder.Bin(b.Id), l.Id, b.Owner, OwnershipBasis.Inherited, 0)) throw new InvalidOperationException("store inheritance failed");
                    Transfer(ev, e, l.Id, l.Ware, l.Count, b.Owner, EstateReason.Household);
                }
            }
            ev.Add(new EstateOpened { Estate = d.Id, Bin = bin, Node = d.Node, Heirs = e.Partners.Count + e.Children.Count, Lots = lots, Faction = d.Faction });
        }

        /// <summary>One tick of the estate system (order 255): split the due, let heirs within reach collect, settle at the deadline or when empty.</summary>
        public static void Tick(GodState s, EventBuffer ev, IEstateSettlement rule)
        {
            var hs = s.Households;
            if (hs == null || hs.Estates.Count == 0) return;
            for (int i = 0; i < hs.Estates.Count;)
            {
                var e = hs.Estates[i];
                if (!e.Split && s.Tick >= e.SplitTick)
                {
                    int ex = rule.ChooseExecutor(s, e);
                    var c = rule.RuleOf(s, e, ex);
                    e.Executor = ex; e.Custom = (byte)c; e.Split = true;
                    rule.Split(s, ev, e, ex, c);
                }
                if (e.Split) Collect(s, ev, e);
                if (e.Split && (s.Tick >= e.DueTick || Empty(s, e)))
                {
                    Settle(s, ev, e);
                    hs.Estates.RemoveAt(i);
                    continue;
                }
                i++;
            }
        }

        private static bool Empty(GodState s, EstateRecord e)
        {
            var p = s.Population;
            if (Goods.BinById(p, e.Bin)?.Lots.Count > 0) return false;
            foreach (int b in e.ShareBins) if (Goods.BinById(p, b)?.Lots.Count > 0) return false;
            return true;
        }

        /// <summary>
        /// Heirs standing within <see cref="Reach.Store"/> of the estate take their own lots (and household members their
        /// household's) into their carry, as room allows: a <c>Withdraw</c> by the heir, ownership kept (A2). This is a
        /// reflex of an heir at the place, not a decision: the mind has no seam to perceive a bin yet (contract amendment).
        /// </summary>
        public static void Collect(GodState s, EventBuffer ev, EstateRecord e)
        {
            var p = s.Population;
            var g = s.Territory.Grid;
            CollectFrom(s, ev, e.Bin, e, g);
            foreach (int b in e.ShareBins) CollectFrom(s, ev, b, e, g);
        }

        private static void CollectFrom(GodState s, EventBuffer ev, int binId, EstateRecord e, NodeGrid g)
        {
            var p = s.Population;
            var bin = Goods.BinById(p, binId);
            if (bin == null) return;
            for (int k = 0; k < bin.Lots.Count;)
            {
                var l = bin.Lots[k];
                Npc taker = null;
                if (l.Owner.Kind == OwnerKind.Npc) { var h = p.Find(l.Owner.Id); if (h != null && h.Alive && g.Distance(h.Node, bin.Node) <= Reach.Store) taker = h; }
                else if (l.Owner.Kind == OwnerKind.Household)
                {
                    var hh = s.Households.Find(l.Owner.Id);
                    if (hh != null) foreach (int m in hh.Members) { var h = p.Find(m); if (h != null && h.Alive && h.Stage != LifeStage.Child && g.Distance(h.Node, bin.Node) <= Reach.Store) { taker = h; break; } }
                }
                if (taker != null)
                {
                    int room = PopulationModule.Room(s, taker, l.Ware);
                    int n = Math.Min(room, l.Count);
                    if (n > 0)
                    {
                        var r = Goods.Move(s, ev, Holder.Bin(binId), l.Id, n, Holder.Carry(taker.Id), MoveReason.Withdraw, taker.Id, OwnerRule.Keep);
                        if (r.Ok) { if (n == l.Count) continue; }
                    }
                }
                k++;
            }
        }

        /// <summary>
        /// The deadline (MATERIAL 4.1.6): lots still owned by a living heir go to its household store (O4 delivery) when it has
        /// one and they fit; the rest become ownerless on the ground at the death node. Then every estate bin is removed.
        /// </summary>
        public static void Settle(GodState s, EventBuffer ev, EstateRecord e)
        {
            var hs = s.Households;
            var all = new List<int> { e.Bin };
            all.AddRange(e.ShareBins);
            foreach (int b in all) Clear(s, ev, e, b);
            foreach (int b in all) Goods.RemoveBin(s, b);
            hs.Stats.EstatesSettled++;
            ev.Add(new EstateSettled
            {
                Estate = e.Deceased, Ticks = (int)(s.Tick - e.OpenedTick), Transfers = e.Transfers, Ownerless = e.Ownerless, Destroyed = e.Destroyed,
                Executor = e.Executor, Custom = e.Custom,
            });
        }

        private static void Clear(GodState s, EventBuffer ev, EstateRecord e, int binId)
        {
            var p = s.Population;
            var et = s.Tuning.Estate;
            var bin = Goods.BinById(p, binId);
            if (bin == null) return;
            while (bin.Lots.Count > 0)
            {
                var l = bin.Lots[0];
                int store = 0;
                if (et.Delivery != 0)
                {
                    if (l.Owner.Kind == OwnerKind.Npc)
                    {
                        var h = p.Find(l.Owner.Id);
                        if (h != null && h.Alive && h.Household != 0) store = s.Households.Find(h.Household)?.Bin ?? 0;
                    }
                    else if (l.Owner.Kind == OwnerKind.Household) store = s.Households.Find(l.Owner.Id)?.Bin ?? 0;
                }
                if (store != 0)
                {
                    var r = Goods.Move(s, ev, Holder.Bin(binId), l.Id, l.Count, Holder.Bin(store), MoveReason.EstateOut, 0, OwnerRule.Keep);
                    if (r.Ok) { Transfer(ev, e, l.Id, l.Ware, l.Count, l.Owner, EstateReason.Delivered); continue; }
                }
                var g = Goods.Move(s, ev, Holder.Bin(binId), l.Id, l.Count, Holder.Ground(bin.Node), MoveReason.EstateOut, 0, OwnerRule.Set, Owner.None,
                    MergeRule.Never, 0, OwnershipBasis.Escheat);
                if (!g.Ok) throw new InvalidOperationException("estate to ground failed: " + g.Status);
                e.Ownerless += l.Count;
                Transfer(ev, e, l.Id, l.Ware, l.Count, Owner.None, EstateReason.Ownerless);
            }
        }

        internal static void Transfer(EventBuffer ev, EstateRecord e, int lot, Ware w, int count, Owner to, byte reason)
        {
            e.Transfers++;
            ev.Add(new EstateTransfer { Estate = e.Deceased, AssetKind = 1, AssetId = lot, Ware = w, Count = count, ToKind = to.Kind, ToId = to.Id, Reason = reason, Executor = e.Executor });
        }
    }

    /// <summary>
    /// Settlement by custom (MATERIAL 4.2-4.7, the one M4 implementation of <see cref="IEstateSettlement"/>). The executor is
    /// found by the configured ladder; the custom is the executor's faction's (a fixed per-faction default before M11). The
    /// split reads no reference table: stackables by units (quotient, remainder by seniority), indivisible tools by the
    /// executor's own worth estimates. No skimming (Q14): the executor gets exactly what the custom gives it as an heir.
    /// </summary>
    public sealed class CustomSettlement : IEstateSettlement
    {
        public int ChooseExecutor(GodState s, EstateRecord e)
        {
            var p = s.Population;
            var cal = SimCalendar.Of(s);
            int ladder = s.Tuning.Estate.Ladder;
            var digits = new List<int>();
            for (int v = ladder; v > 0; v /= 10) digits.Insert(0, v % 10);
            foreach (int rung in digits)
            {
                switch ((ExecutorRung)rung)
                {
                    case ExecutorRung.NamedInWill: break; // no wills in M4 (MATERIAL 8: M8)
                    case ExecutorRung.Partner:
                        foreach (int x in e.Partners) { var n = p.Find(x); if (n != null && n.Alive && n.Stage != LifeStage.Child) return x; }
                        break;
                    case ExecutorRung.EldestAdultChild:
                        foreach (int x in e.Children) { var n = p.Find(x); if (n != null && n.Alive && n.Stage != LifeStage.Child) return x; }
                        break;
                    case ExecutorRung.EldestHouseholdAdult:
                    {
                        var hh = s.Households.Find(e.Household);
                        if (hh == null) break;
                        int best = 0; long born = long.MaxValue;
                        foreach (int m in hh.Members) { var n = p.Find(m); if (n != null && n.Alive && n.Stage != LifeStage.Child && n.BornTick < born) { born = n.BornTick; best = m; } }
                        if (best != 0) return best;
                        break;
                    }
                    case ExecutorRung.Appointed: break; // elders and institutions appoint from M12
                }
            }
            return 0;
        }

        public Custom RuleOf(GodState s, EstateRecord e, int executor)
        {
            if (executor == 0) return Custom.None;
            var ex = s.Population.Find(executor);
            return s.Tuning.Estate.CustomOf(ex != null ? ex.Faction : e.Faction);
        }

        public void Split(GodState s, EventBuffer ev, EstateRecord e, int executor, Custom rule)
        {
            var p = s.Population;
            var bin = Goods.BinById(p, e.Bin);
            if (bin == null) return;
            var exNpc = p.Find(executor);
            var hh = s.Households.Find(e.Household);
            bool householdAlive = hh != null && hh.Members.Count > 0;
            // household goods the deceased carried stay the household's (or go ownerless with no household)
            var ids = new List<int>();
            foreach (var l in bin.Lots) ids.Add(l.Id);
            var estateLots = new List<int>();
            foreach (int id in ids)
            {
                var l = Lot(bin, id);
                if (l.Owner.Kind == OwnerKind.Estate) { estateLots.Add(id); continue; }
                Estates.Transfer(ev, e, id, l.Ware, l.Count, l.Owner, EstateReason.Household);
            }
            if (rule == Custom.BuriedWithDead)
            {
                var et = s.Tuning.Estate;
                foreach (int id in estateLots)
                {
                    var l = Lot(bin, id);
                    int bury = l.Count * et.BuriedPermille / 1000;
                    if (bury <= 0) continue;
                    if (!Goods.Consume(s, ev, Holder.Bin(e.Bin), id, bury, ConsumeReason.Buried, 0, e.Deceased)) throw new InvalidOperationException("burial failed");
                    e.Destroyed += bury;
                    Estates.Transfer(ev, e, id, l.Ware, bury, Owner.None, EstateReason.Burial);
                }
                estateLots.RemoveAll(id => Lot(bin, id).Id == 0);
                rule = Custom.HouseholdKeeps; // the second variant (MATERIAL 4.3): none in M4, so the household keeps the rest
            }
            if (rule == Custom.PartnerFirst)
            {
                int partner = 0;
                foreach (int x in e.Partners) { var n = p.Find(x); if (n != null && n.Alive) { partner = x; break; } }
                if (partner != 0) { foreach (int id in estateLots) Give(s, ev, e, bin, id, Owner.Npc(partner), EstateReason.Custom); return; }
                rule = Custom.EqualChildren;
            }
            if (rule == Custom.HouseholdKeeps)
            {
                if (householdAlive) { foreach (int id in estateLots) Give(s, ev, e, bin, id, Owner.Household(hh.Id), EstateReason.Household); return; }
                rule = Custom.BackToCommunity;
            }
            if (rule == Custom.BackToCommunity)
            {
                foreach (int id in estateLots) Give(s, ev, e, bin, id, Owner.Group((int)e.Faction), EstateReason.Communal);
                return;
            }
            if (rule == Custom.EqualChildren || rule == Custom.EldestChild)
            {
                var heirs = new List<int>();
                foreach (int x in e.Children) { var n = p.Find(x); if (n != null && n.Alive) heirs.Add(x); }
                if (heirs.Count == 0) { foreach (int id in estateLots) ToGround(s, ev, e, bin, id); return; }
                var awarded = new long[heirs.Count];
                foreach (int id in estateLots)
                {
                    var l = Lot(bin, id);
                    bool indivisible = l.Ware == Ware.Axe || Wares.IsGear(l.Ware) || l.Count == 1 && heirs.Count == 1;
                    if (l.Ware == Ware.Axe || Wares.IsGear(l.Ware))
                    {
                        int to = 0;
                        if (rule == Custom.EqualChildren)
                            for (int h = 1; h < heirs.Count; h++) if (awarded[h] < awarded[to]) to = h; // ties to the eldest
                        awarded[to] += Worth(s, exNpc, l.Ware) * l.Count;
                        Give(s, ev, e, bin, id, Owner.Npc(heirs[to]), EstateReason.Custom);
                        continue;
                    }
                    var shares = SplitUnits(l.Count, heirs.Count);
                    int last = -1;
                    for (int h = 0; h < shares.Length; h++) if (shares[h] > 0) last = h;
                    for (int h = 0; h < shares.Length; h++)
                    {
                        if (shares[h] <= 0) continue;
                        awarded[h] += Worth(s, exNpc, l.Ware) * shares[h];
                        if (h == last) { Give(s, ev, e, bin, id, Owner.Npc(heirs[h]), EstateReason.Custom); continue; }
                        int share = ShareBin(s, e);
                        var r = Goods.Move(s, ev, Holder.Bin(e.Bin), id, shares[h], Holder.Bin(share), MoveReason.EstateOut, 0, OwnerRule.Set, Owner.Npc(heirs[h]),
                            MergeRule.Never, 0, OwnershipBasis.Inherited);
                        if (!r.Ok) throw new InvalidOperationException("estate split failed: " + r.Status);
                        Estates.Transfer(ev, e, r.Lot, l.Ware, shares[h], Owner.Npc(heirs[h]), EstateReason.Custom);
                    }
                }
                Disputes(s, ev, e, heirs, awarded, rule);
                return;
            }
            // no custom (no executor): everything ownerless where it lies
            foreach (int id in estateLots) ToGround(s, ev, e, bin, id);
        }

        /// <summary>Units divided by quotient; the remainder one unit at a time to the heirs in seniority order (MATERIAL 4.4).</summary>
        public static int[] SplitUnits(int count, int heirs)
        {
            var a = new int[heirs];
            int q = count / heirs, r = count % heirs;
            for (int h = 0; h < heirs; h++) a[h] = q + (h < r ? 1 : 0);
            return a;
        }

        private static int ShareBin(GodState s, EstateRecord e)
        {
            int b = Goods.NewBin(s, BinKind.Estate, Owner.Estate(e.Deceased), e.Node, 0);
            e.ShareBins.Add(b);
            return b;
        }

        private static ItemLot Lot(LotBin bin, int id)
        {
            foreach (var l in bin.Lots) if (l.Id == id) return l;
            return default;
        }

        private static void Give(GodState s, EventBuffer ev, EstateRecord e, LotBin bin, int id, Owner to, byte reason)
        {
            var l = Lot(bin, id);
            if (!Goods.SetOwner(s, ev, Holder.Bin(bin.Id), id, to, OwnershipBasis.Inherited, 0)) throw new InvalidOperationException("estate set owner failed");
            Estates.Transfer(ev, e, id, l.Ware, l.Count, to, reason);
        }

        private static void ToGround(GodState s, EventBuffer ev, EstateRecord e, LotBin bin, int id)
        {
            var l = Lot(bin, id);
            var r = Goods.Move(s, ev, Holder.Bin(bin.Id), id, l.Count, Holder.Ground(bin.Node), MoveReason.EstateOut, 0, OwnerRule.Set, Owner.None, MergeRule.Never, 0, OwnershipBasis.Escheat);
            if (!r.Ok) throw new InvalidOperationException("estate to ground failed: " + r.Status);
            e.Ownerless += l.Count;
            Estates.Transfer(ev, e, id, l.Ware, l.Count, Owner.None, EstateReason.Ownerless);
        }

        /// <summary>The executor's own worth of a ware (its estimate, ECONOMY 4.5; the M3 goods worth), never a reference table.</summary>
        private static long Worth(GodState s, Npc executor, Ware w) => executor != null ? GoodsWorth.Of(s, executor, w) : 1;

        /// <summary>
        /// MATERIAL 4.7: an heir whose own custom (its faction's) would have given it at least 25% more, valued by its own
        /// worth, raises a dispute; the grudge it holds against the executor is <c>min(255, shortfall / 2) x 3</c> (M5 applies it).
        /// </summary>
        private static void Disputes(GodState s, EventBuffer ev, EstateRecord e, List<int> heirs, long[] awarded, Custom applied)
        {
            if (e.Executor == 0) return;
            var p = s.Population;
            var et = s.Tuning.Estate;
            for (int h = 0; h < heirs.Count; h++)
            {
                var heir = p.Find(heirs[h]);
                if (heir == null) continue;
                var own = et.CustomOf(heir.Faction);
                if (own == applied) continue;
                if (own != Custom.EqualChildren && own != Custom.EldestChild) continue;
                long total = 0; foreach (long a in awarded) total += a;
                long would = own == Custom.EqualChildren ? total / heirs.Count : (h == 0 ? total : 0);
                long got = awarded[h];
                if (would <= 0 || got >= would) continue;
                long shortfall = would - got;
                int permille = (int)(shortfall * 1000 / would);
                if (permille < et.DisputeFromPermille) continue;
                int grudge = (int)Math.Min(255, shortfall / 2) * 3;
                ev.Add(new EstateDisputed { Estate = e.Deceased, Claimant = heir.Id, Executor = e.Executor, ShortfallPermille = permille, Grudge = grudge });
            }
        }
    }

    /// <summary>The estate system (order 255, SHARED_CONTRACTS 9.2): settles the estates due this tick.</summary>
    public sealed class EstateSystem : ISimSystem
    {
        private readonly CustomSettlement _rule = new CustomSettlement();

        public void Tick(GodState s, EventBuffer ev)
        {
            if (s.Households == null || s.Population == null) return;
            Estates.Tick(s, ev, _rule);
        }
    }
}
