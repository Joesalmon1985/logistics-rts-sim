using System;
using System.Collections.Generic;
using God.Sim.Cognition;
using God.Sim.Population;
using God.Sim.Population.Households;

namespace God.Sim.Civ
{
    /// <summary>
    /// One ownership belief (layer 6, spec 5.7): which asset, where, who the mind believes owns it, how sure it is, and the
    /// use rights it believes it holds itself (bit <c>1 &lt;&lt; UseKind</c>). Formed only from perception: it saw the asset built,
    /// saw someone work it, or was told by the grant it accepted.
    /// </summary>
    public struct OwnBelief
    {
        public int Asset;
        public int Node;
        public int Class;
        public Owner BelievedOwner;
        public byte Confidence;
        public byte MyRights;
        public long Tick;
    }

    /// <summary>C1's part of <c>NpcMind.Civ</c>: at most <see cref="MaxOwn"/> ownership beliefs, ascending asset id.</summary>
    public sealed partial class CivMind
    {
        public const int MaxOwn = 8;
        public List<OwnBelief> Own = new List<OwnBelief>();

        public int IndexOfAsset(int asset)
        {
            for (int i = 0; i < Own.Count; i++) if (Own[i].Asset == asset) return i;
            return -1;
        }

        partial void HashC1(ref StateHasher h)
        {
            h.Add(Own.Count);
            foreach (var b in Own) { h.Add(b.Asset); h.Add(b.Node); h.Add(b.Class); h.Add((int)b.BelievedOwner.Kind); h.Add(b.BelievedOwner.Id); h.Add(b.Confidence); h.Add(b.MyRights); h.Add(b.Tick); }
        }
    }

    /// <summary>
    /// Ownership truth and perception (sim side, layer 1 and the senses). <see cref="Truth"/> and its overloads are the true
    /// permission (spec 5.8): called only by the sim's own classification (<see cref="Classify"/>, the Work act), never by a
    /// policy or a mind hook (a static test). The perception writers (<see cref="Observe"/>, <see cref="Witness"/>) put what a
    /// person saw into its mind; the mind side reads only those beliefs (<see cref="Permits.BelievedAsset"/>).
    /// </summary>
    public static class Ownership
    {
        /// <summary>The true right codes (<see cref="AssetUsed.Right"/>): none, the owner, a household member of the owner, a use right.</summary>
        public const byte RightNone = 0, RightOwner = 1, RightHousehold = 2, RightUse = 3;

        // ---------------- truth (layer 1) ----------------

        /// <summary>Whether <paramref name="actor"/> is the owner, or a member of the owning household (no branch on any other kind: an Org owner's roles are C6's).</summary>
        public static byte OwnerRelation(Npc actor, Owner owner)
        {
            if (actor == null || owner.IsNone) return RightNone;
            if (owner.Same(Owner.Npc(actor.Id))) return RightOwner;
            if (actor.Household != 0 && owner.Same(Owner.Household(actor.Household))) return RightHousehold;
            return RightNone;
        }

        /// <summary>The true right of a person to use an asset in a way (<see cref="RightNone"/> when it has none).</summary>
        public static byte RightOn(GodState s, Npc actor, ProductiveAsset a, UseKind kind)
        {
            byte rel = OwnerRelation(actor, a.Owner);
            if (rel != RightNone) return rel;
            int day = (int)SimCalendar.Of(s).Day(s.Tick);
            foreach (var r in a.Rights)
                if (r.Kind == kind && (r.UntilDay < 0 || r.UntilDay >= day) && OwnerRelation(actor, r.Holder) != RightNone) return RightUse;
            return RightNone;
        }

        /// <summary>The true permission over goods owned by <paramref name="owner"/> (no bin: carried or lying).</summary>
        public static PermissionResult Truth(GodState s, Npc actor, Owner owner)
        {
            if (owner.IsNone) return new PermissionResult(true, PermissionBasis.Ownerless);
            byte rel = OwnerRelation(actor, owner);
            if (rel == RightOwner) return new PermissionResult(true, PermissionBasis.Own);
            if (rel == RightHousehold) return new PermissionResult(true, PermissionBasis.Household);
            return new PermissionResult(false, PermissionBasis.None);
        }

        /// <summary>The true permission to take a lot (owned by <paramref name="lotOwner"/>) out of a bin: the asset's store rights, M4's store access, else the goods' owner.</summary>
        public static PermissionResult Truth(GodState s, Npc actor, LotBin bin, Owner lotOwner)
        {
            if (bin != null && bin.Kind == BinKind.AssetStore)
            {
                var a = Assets.ByStore(s, bin.Id);
                if (a != null)
                {
                    byte r = RightOn(s, actor, a, UseKind.Store);
                    if (r == RightNone) r = RightOn(s, actor, a, UseKind.Work);
                    if (r == RightOwner) return new PermissionResult(true, PermissionBasis.Own);
                    if (r == RightHousehold) return new PermissionResult(true, PermissionBasis.Household);
                    if (r == RightUse) return new PermissionResult(true, PermissionBasis.UseRight);
                }
            }
            if (bin != null && bin.Kind == BinKind.HouseholdStore && actor != null && StoreAccess.MayTake(s, actor, bin)) return new PermissionResult(true, PermissionBasis.Household);
            return Truth(s, actor, lotOwner);
        }

        // ---------------- perception (the senses write beliefs) ----------------

        /// <summary>
        /// A person perceives that <paramref name="believedOwner"/> owns an asset (built it, works it, or it was told). Same
        /// owner: the confidence rises; another owner: replaced only by a surer observation. Full: evict the lowest
        /// confidence, then the oldest, ties by asset id. The mind state is created on the first belief.
        /// </summary>
        public static void Observe(GodState s, Npc npc, ProductiveAsset a, Owner believedOwner, int confidence, byte rights = 0)
        {
            if (npc?.Mind == null || !npc.Alive || a == null) return;
            var cm = npc.Mind.Civ ?? (npc.Mind.Civ = new CivMind());
            var own = cm.Own;
            byte conf = (byte)Math.Max(0, Math.Min(255, confidence));
            int i = cm.IndexOfAsset(a.Id);
            int cls = AssetTable.Get(a.Kind)?.ObjClass ?? 0;
            if (i >= 0)
            {
                var b = own[i];
                if (b.BelievedOwner.Same(believedOwner)) b.Confidence = Math.Max(b.Confidence, conf);
                else if (conf > b.Confidence) { b.BelievedOwner = believedOwner; b.Confidence = conf; b.MyRights = 0; }
                else { b.Tick = s.Tick; own[i] = b; return; }
                b.MyRights |= rights; b.Node = a.Node; b.Class = cls; b.Tick = s.Tick;
                own[i] = b;
                return;
            }
            if (own.Count >= CivMind.MaxOwn)
            {
                int worst = 0;
                for (int k = 1; k < own.Count; k++)
                {
                    var w = own[worst]; var c = own[k];
                    if (c.Confidence < w.Confidence || c.Confidence == w.Confidence && (c.Tick < w.Tick || c.Tick == w.Tick && c.Asset < w.Asset)) worst = k;
                }
                if (own[worst].Confidence > conf) return; // everything held is surer than this
                own.RemoveAt(worst);
            }
            var nb = new OwnBelief { Asset = a.Id, Node = a.Node, Class = cls, BelievedOwner = believedOwner, Confidence = conf, MyRights = rights, Tick = s.Tick };
            int at = 0;
            while (at < own.Count && own[at].Asset < a.Id) at++;
            own.Insert(at, nb);
        }

        /// <summary>The holder of a new right learns it from the grant it accepted (spec 5.6; a person holder only).</summary>
        public static void ToldOfRight(GodState s, ProductiveAsset a, Owner holder, UseKind kind)
        {
            if (holder.Kind != OwnerKind.Npc) return;
            Observe(s, s.Population.Find(holder.Id), a, a.Owner, 255, (byte)(1 << (int)kind));
        }

        /// <summary>
        /// Everyone awake within sight of the asset sees <paramref name="actor"/> build or work it and believes the actor owns
        /// it (what is visible is the person, not the record): a member of the actor's own household knows it is the
        /// household's when the household owns it. The actor itself knows the truth of its own relation.
        /// </summary>
        public static void Witness(GodState s, ProductiveAsset a, Npc actor, int confidence)
        {
            var p = s.Population;
            var g = s.Territory.Grid;
            var tu = s.Tuning.Population;
            int sight = PopulationSystem.TimeOf(SimCalendar.Of(s), s.Tick) == Learning.TimeOfDay.Night ? tu.SightNight : tu.SightDay;
            byte actorRel = OwnerRelation(actor, a.Owner);
            foreach (var n in p.Npcs)
            {
                if (!Bodies.IsLivingPerson(n) || n.Mind == null || n.Action.Asleep) continue;
                if (n.Id == actor.Id)
                {
                    // its own relation: owner or household member knows; a right holder knows the owner from its grant
                    if (actorRel != RightNone) Observe(s, n, a, a.Owner, 255, 0xFF);
                    continue;
                }
                if (g.Distance(n.Node, a.Node) > sight) continue;
                bool kin = actor.Household != 0 && n.Household == actor.Household && actorRel == RightHousehold;
                Observe(s, n, a, kin ? a.Owner : Owner.Npc(actor.Id), confidence);
            }
        }

        // ---------------- O7: lost property ----------------

        /// <summary>The true owner of units of a ground lot (Owner.None when nobody's), and how many of its units are owned.</summary>
        public static Owner LostOwner(GodState s, int groundLot, out int units)
        {
            units = 0;
            var o = Assets.StateOf(s);
            if (o == null) return Owner.None;
            Owner who = Owner.None;
            foreach (var l in o.Lost) if (l.Lot == groundLot) { if (who.IsNone) who = l.Owner; units += l.Count; }
            return who;
        }

        /// <summary>
        /// The sim-side classification after the tick's acts (spec 5.9, 5.10): reads the goods moves of the current tick and
        /// keeps the O7 overlay (drops of owned lots, merges on the ground) and emits <see cref="TheftCommitted"/> for a take
        /// of owned goods without the true permission. Nothing reacts to the classification.
        /// </summary>
        public static void Classify(GodState s, EventBuffer ev)
        {
            var o = Assets.StateOf(s);
            var items = ev.Items;
            int from = items.Count;
            while (from > 0 && items[from - 1].Tick == s.Tick) from--;
            int day = -1;
            int n = items.Count; // only the events that were there when classification began
            for (int k = from; k < n; k++)
            {
                if (items[k] is Produced pr)
                {
                    // ECONOMY 12 "Land access": the first to take from a wild source claims it (recorded only)
                    if (pr.Source > 0 && pr.Npc > 0 && (pr.Reason == ProduceReason.Foraged || pr.Reason == ProduceReason.HandGathered))
                        Property.Claim(s, ev, Property.Source, pr.Source, Owner.Npc(pr.Npc), OwnershipBasis.FirstArrival);
                    continue;
                }
                if (!(items[k] is ItemMoved m)) continue;
                if (m.ToKind == HolderKind.Ground)
                {
                    // a merge on the ground: the surviving id carries the record on
                    if (m.EndedLot != 0) for (int i = 0; i < o.Lost.Count; i++) if (o.Lost[i].Lot == m.EndedLot) { var l = o.Lost[i]; l.Lot = m.IntoLot; o.Lost[i] = l; }
                    if (m.Reason == MoveReason.Drop && m.OwnerBeforeKind != OwnerKind.None)
                    {
                        if (day < 0) day = (int)SimCalendar.Of(s).Day(s.Tick);
                        o.Lost.Add(new LostLot { Lot = m.IntoLot, Node = m.ToId, Ware = m.Ware, Count = m.Count, Owner = new Owner(m.OwnerBeforeKind, m.OwnerBeforeId), Day = day });
                    }
                    continue;
                }
                if (m.FromKind == HolderKind.Ground && (m.Reason == MoveReason.PickUp || m.Reason == MoveReason.Claim)) TakeFromGround(s, ev, o, m);
                else if (m.FromKind == HolderKind.Bin && m.Reason == MoveReason.Withdraw) TakeFromBin(s, ev, o, m);
            }
        }

        private static void TakeFromGround(GodState s, EventBuffer ev, OwnershipState o, ItemMoved m)
        {
            int ground = m.ParentLot != 0 ? m.ParentLot : m.Lot;
            int recorded = 0;
            foreach (var l in o.Lost) if (l.Lot == ground) recorded += l.Count;
            if (recorded == 0) return;
            var actor = s.Population.Find(m.Actor);
            // what is left on the ground now, so the ownerless units are taken first (benefit of the doubt)
            int left = 0;
            foreach (var g in s.Population.Lots) if (g.Lot.Id == ground) { left = g.Lot.Count; break; }
            int before = left + m.Count;
            int ownerless = Math.Max(0, before - recorded);
            int owned = Math.Max(0, m.Count - ownerless);
            for (int i = 0; i < o.Lost.Count && owned > 0;)
            {
                var l = o.Lost[i];
                if (l.Lot != ground) { i++; continue; }
                int take = Math.Min(owned, l.Count);
                owned -= take;
                l.Count -= take;
                if (!Truth(s, actor, l.Owner).Allowed)
                {
                    o.Thefts++;
                    ev.Add(new TheftCommitted { Kind = TheftCommitted.Goods, Lot = m.IntoLot, Ware = m.Ware, Count = take, OwnerKind = l.Owner.Kind, OwnerId = l.Owner.Id,
                        Taker = m.Actor, Node = m.FromId, FromKind = HolderKind.Ground, FromHolder = m.FromId });
                }
                if (l.Count <= 0) o.Lost.RemoveAt(i); else { o.Lost[i] = l; i++; }
            }
            // a partial take leaves the rest on the ground under the same id (nothing to move); a whole take ends the record above
        }

        private static void TakeFromBin(GodState s, EventBuffer ev, OwnershipState o, ItemMoved m)
        {
            var before = new Owner(m.OwnerBeforeKind, m.OwnerBeforeId);
            if (before.IsNone) return;
            var bin = Goods.BinById(s.Population, m.FromId);
            var actor = s.Population.Find(m.Actor);
            if (Truth(s, actor, bin, before).Allowed) return;
            var a = bin != null ? Assets.ByStore(s, bin.Id) : null;
            o.Thefts++;
            ev.Add(new TheftCommitted { Kind = TheftCommitted.Goods, Lot = m.IntoLot, Asset = a?.Id ?? 0, Ware = m.Ware, Count = m.Count, OwnerKind = before.Kind, OwnerId = before.Id,
                Taker = m.Actor, Node = m.FromNode, FromKind = HolderKind.Bin, FromHolder = m.FromId });
        }

        /// <summary>Day start: lost lots past <c>LostPropertyDays</c> become ownerless (basis Abandoned); records whose lot is gone or smaller are trimmed.</summary>
        public static void DayStart(GodState s, EventBuffer ev, int day)
        {
            var o = Assets.StateOf(s);
            int keep = s.Tuning.Civ.C1.LostPropertyDays;
            for (int i = 0; i < o.Lost.Count;)
            {
                var l = o.Lost[i];
                int have = 0;
                foreach (var g in s.Population.Lots) if (g.Lot.Id == l.Lot) { have = g.Lot.Count; break; }
                if (have <= 0) { o.Lost.RemoveAt(i); continue; }
                if (day - l.Day >= keep)
                {
                    // Goods already holds the lot ownerless (C4); this is a no-op there and the overlay ends: Abandoned
                    Goods.SetOwner(s, ev, Holder.Ground(l.Node), l.Lot, Owner.None, OwnershipBasis.Abandoned, 0);
                    o.Abandoned++;
                    o.Lost.RemoveAt(i);
                    continue;
                }
                if (l.Count > have) { l.Count = have; o.Lost[i] = l; }
                i++;
            }
        }
    }

    /// <summary>Property records of claimable non-assets (sources now; sites and deposits later): spec 5.11 and 22.</summary>
    public static class Property
    {
        public const byte Source = 1;

        /// <summary>Records a claim (one per thing: the first arrival); emits <see cref="SourceClaimed"/> for a source. No exclusion follows.</summary>
        public static bool Claim(GodState s, EventBuffer ev, byte thingKind, int thing, Owner claimant, OwnershipBasis basis)
        {
            var o = Assets.StateOf(s);
            if (o == null || claimant.IsNone) return false;
            int at = 0;
            for (; at < o.Claims.Count; at++)
            {
                var c = o.Claims[at];
                if (c.ThingKind == thingKind && c.Thing == thing) return false;
                if (c.ThingKind > thingKind || c.ThingKind == thingKind && c.Thing > thing) break;
            }
            o.Claims.Insert(at, new SourceClaim { ThingKind = thingKind, Thing = thing, Claimant = claimant, Basis = basis, Tick = s.Tick });
            if (thingKind == Source) ev?.Add(new SourceClaimed { Source = thing, Claimant = claimant.Id, Basis = basis });
            return true;
        }

        public static Owner ClaimantOf(GodState s, byte thingKind, int thing)
        {
            var o = Assets.StateOf(s);
            if (o != null) foreach (var c in o.Claims) if (c.ThingKind == thingKind && c.Thing == thing) return c.Claimant;
            return Owner.None;
        }
    }
}
