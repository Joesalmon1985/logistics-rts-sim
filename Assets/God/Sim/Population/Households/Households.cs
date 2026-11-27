using System;
using System.Collections.Generic;

namespace God.Sim.Population.Households
{
    /// <summary>
    /// Household membership (HOUSEHOLDS 4 "Forming and breaking households"; SHARED_CONTRACTS 5): the only writer of
    /// <c>Npc.Household</c> and <see cref="Household.Members"/>, which always agree (sorted, at most 12, one household per NPC).
    /// Every join and leave emits its event, so membership intervals pair exactly (M4.14).
    /// </summary>
    public static class HouseholdModule
    {
        public static Household Form(GodState s, EventBuffer ev, Npc founder, byte reason)
        {
            var hs = s.Households;
            var h = new Household { Id = hs.NextHouseholdId++, Faction = founder.Faction, FormedTick = s.Tick };
            hs.Households.Add(h);
            ev?.Add(new HouseholdFormed { Household = h.Id, Faction = h.Faction, Founder = founder.Id, Reason = reason });
            Join(s, ev, h, founder, reason, 0);
            return h;
        }

        /// <summary>Adds a person (who must be in no household) to a household. Returns false when it is full.</summary>
        public static bool Join(GodState s, EventBuffer ev, Household h, Npc npc, byte reason, int from)
        {
            if (npc.Household != 0) throw new InvalidOperationException("npc " + npc.Id + " is already in household " + npc.Household);
            if (h.Members.Count >= Household.MaxMembers) return false;
            int at = h.Members.BinarySearch(npc.Id);
            if (at >= 0) throw new InvalidOperationException("duplicate member");
            h.Members.Insert(~at, npc.Id);
            npc.Household = h.Id;
            ev?.Add(new HouseholdJoined { Npc = npc.Id, Household = h.Id, Reason = reason, FromHousehold = from });
            if (h.Hut != 0) GrantHome(s, ev, h, npc);
            return true;
        }

        /// <summary>Removes a person from its household (a household with no members is dissolved).</summary>
        public static void Leave(GodState s, EventBuffer ev, Npc npc, byte reason, int to)
        {
            if (npc.Household == 0) return;
            var h = s.Households.Find(npc.Household);
            npc.Household = 0;
            if (h == null) return;
            h.Members.Remove(npc.Id);
            ev?.Add(new HouseholdLeft { Npc = npc.Id, Household = h.Id, Reason = reason, ToHousehold = to });
            // a child's half bed is the household's, so it goes with the household; an adult's own right stays with it (HOUSEHOLDS "Sleeping rights")
            var hut = h.Hut != 0 ? s.Population.HutById(h.Hut) : null;
            if (hut != null && hut.Lodgers != null && hut.Lodgers.Remove(npc.Id))
            {
                if (npc.HomeHut == hut.Id) npc.HomeHut = 0;
                ev?.Add(new HutRightLost { Hut = hut.Id, Npc = npc.Id, Reason = reason });
            }
            if (h.Members.Count == 0) Dissolve(s, ev, h);
        }

        /// <summary>Moves a person from its household to another (a leave, then a join; refused when the target is full).</summary>
        public static bool Move(GodState s, EventBuffer ev, Npc npc, Household to, byte leaveReason, byte joinReason)
        {
            if (to.Members.Count >= Household.MaxMembers) return false;
            int from = npc.Household;
            Leave(s, ev, npc, leaveReason, to.Id);
            Join(s, ev, to, npc, joinReason, from);
            return true;
        }

        /// <summary>
        /// A household with no members ends (HOUSEHOLDS "Breaking"): what its store holds is set down at its hut, ownerless
        /// (anyone may take it, as any abandoned goods), and the store is removed.
        /// </summary>
        public static void Dissolve(GodState s, EventBuffer ev, Household h)
        {
            var hs = s.Households;
            int dropped = 0;
            if (h.Bin != 0)
            {
                var bin = Goods.BinById(s.Population, h.Bin);
                while (bin != null && bin.Lots.Count > 0)
                {
                    var l = bin.Lots[0];
                    var r = Goods.Move(s, ev, Holder.Bin(bin.Id), l.Id, l.Count, Holder.Ground(bin.Node), MoveReason.Drop, 0, OwnerRule.Set, Owner.None);
                    if (!r.Ok) throw new InvalidOperationException("dissolve: " + r.Status);
                    dropped++;
                }
                Goods.RemoveBin(s, h.Bin);
                h.Bin = 0;
            }
            hs.Households.Remove(h);
            // arrangements with this household end
            for (int i = hs.Care.Count - 1; i >= 0; i--)
            {
                var c = hs.Care[i];
                if (c.AskerHousehold != h.Id && c.CarerHousehold != h.Id) continue;
                hs.Care.RemoveAt(i);
                ev?.Add(new CareArrangementEnded { Arrangement = c.Id, EndedBy = 0, Given = c.Given, Returned = c.Returned, Reason = 3 });
            }
            ev?.Add(new HouseholdDissolved { Household = h.Id, Hut = h.Hut, Faction = h.Faction, LotsDropped = dropped });
        }

        /// <summary>
        /// The household takes a hut as its home (a member holds a right there): its store is created at the hut, and every
        /// member gets a bed or, a child, a half bed while there is room (<see cref="HutRights"/>).
        /// </summary>
        public static void AdoptHut(GodState s, EventBuffer ev, Household h, Hut hut)
        {
            h.Hut = hut.Id;
            if (h.Bin == 0) h.Bin = Goods.NewBin(s, BinKind.HouseholdStore, Owner.Household(h.Id), hut.Node, 0);
            foreach (int m in h.Members)
            {
                var n = s.Population.Find(m);
                if (n != null && n.Alive) GrantHome(s, ev, h, n);
            }
        }

        /// <summary>A member's place in its household's hut: its own right if it has one there, else a bed or a child's half bed if free.</summary>
        public static void GrantHome(GodState s, EventBuffer ev, Household h, Npc n)
        {
            var hut = s.Population.HutById(h.Hut);
            if (hut == null || !hut.Built) return;
            if (hut.HasRight(n.Id)) { if (n.HomeHut == 0) n.HomeHut = hut.Id; return; }
            if (n.HomeHut != 0) return; // it keeps its own bed elsewhere
            if (n.Stage == LifeStage.Child) HutRights.GrantLodger(s, ev, hut, n);
            else HutRights.Grant(s, ev, hut, n, HutRights.Household);
        }

        /// <summary>The living adult members (not children) of a household.</summary>
        public static int Adults(GodState s, Household h)
        {
            int k = 0;
            foreach (int m in h.Members) { var n = s.Population.Find(m); if (n != null && n.Alive && n.Stage != LifeStage.Child) k++; }
            return k;
        }
    }

    /// <summary>
    /// Sleeping rights (SHARED_CONTRACTS 5): <c>Hut.Rights</c> stays the single source of truth; M4 grants and revokes through
    /// here and records the source. A child takes a half bed (two children to a free bed, HOUSEHOLDS "Founded").
    /// </summary>
    public static class HutRights
    {
        public const byte Built = 1, Household = 2, Invited = 3;

        public static bool Grant(GodState s, EventBuffer ev, Hut hut, Npc npc, byte source)
        {
            if (!hut.Built || hut.HasRight(npc.Id)) return false;
            if (hut.RightSource == null) { hut.RightSource = new byte[hut.Rights.Length]; for (int i = 0; i < hut.Rights.Length; i++) if (hut.Rights[i] != 0) hut.RightSource[i] = Built; }
            // a free bed, counting two lodging children to a bed
            int lodgerBeds = hut.Lodgers == null ? 0 : (hut.Lodgers.Count + 1) / 2;
            int free = hut.FreeBeds - lodgerBeds;
            if (free <= 0) return false;
            for (int i = 0; i < hut.Rights.Length; i++)
            {
                if (hut.Rights[i] != 0) continue;
                hut.Rights[i] = npc.Id;
                hut.RightSource[i] = source;
                if (npc.HomeHut == 0) npc.HomeHut = hut.Id;
                s.Population.IndexDirty = true;
                ev?.Add(new HutRightGranted { Hut = hut.Id, Npc = npc.Id, Source = source });
                return true;
            }
            return false;
        }

        public static bool GrantLodger(GodState s, EventBuffer ev, Hut hut, Npc child)
        {
            if (!hut.Built || hut.HasRight(child.Id)) return false;
            if (hut.Lodgers == null) hut.Lodgers = new List<int>();
            int beds = hut.FreeBeds * 2 - hut.Lodgers.Count;
            if (beds <= 0) return false;
            int at = hut.Lodgers.BinarySearch(child.Id);
            hut.Lodgers.Insert(~at, child.Id);
            if (child.HomeHut == 0) child.HomeHut = hut.Id;
            s.Population.IndexDirty = true;
            ev?.Add(new HutRightGranted { Hut = hut.Id, Npc = child.Id, Source = Household, Lodger = true });
            return true;
        }

        /// <summary>Ends a right (an adult's bed or a child's half bed) in a hut.</summary>
        public static void Revoke(GodState s, EventBuffer ev, Hut hut, Npc npc, byte reason)
        {
            bool had = false;
            for (int i = 0; i < hut.Rights.Length; i++)
                if (hut.Rights[i] == npc.Id) { hut.Rights[i] = 0; if (hut.RightSource != null) hut.RightSource[i] = 0; had = true; }
            if (hut.Lodgers != null && hut.Lodgers.Remove(npc.Id)) had = true;
            if (!had) return;
            if (npc.HomeHut == hut.Id) npc.HomeHut = 0;
            s.Population.IndexDirty = true;
            ev?.Add(new HutRightLost { Hut = hut.Id, Npc = npc.Id, Reason = reason });
        }

        /// <summary>A grown child's half bed becomes a bed when one is free; otherwise it keeps lodging (it may build or find one).</summary>
        public static void Mature(GodState s, EventBuffer ev, Hut hut, Npc npc)
        {
            if (hut.Lodgers == null || !hut.Lodgers.Contains(npc.Id)) return;
            hut.Lodgers.Remove(npc.Id);
            if (!Grant(s, ev, hut, npc, Household)) hut.Lodgers.Insert(~hut.Lodgers.BinarySearch(npc.Id), npc.Id);
        }
    }

    /// <summary>
    /// Who may take from a household store (SHARED_CONTRACTS 5, A4): a permission query used when building candidates and
    /// repeated by the actor on arrival; never a refusal inside <see cref="Goods"/>. Norm 0 adults only, 1 all members.
    /// </summary>
    public static class StoreAccess
    {
        public static bool MayTake(GodState s, Npc npc, LotBin bin)
        {
            if (bin == null || bin.Kind != BinKind.HouseholdStore || bin.Owner.Kind != OwnerKind.Household) return false;
            if (npc.Household != bin.Owner.Id) return false;
            var h = s.Households?.Find(npc.Household);
            if (h == null) return false;
            return h.StoreAccess != 0 || npc.Stage != LifeStage.Child;
        }
    }

    /// <summary>
    /// Partnerships (HOUSEHOLDS "Partnership", Q5, Q6): pair records with their own ids; code that asks for "the partner" uses
    /// the list (<see cref="PartnersOf"/>). Formed and ended only by the NPCs' own decisions (propose, accept, leave) or a death.
    /// Nothing here reads either partner's sex.
    /// </summary>
    public static class Partnerships
    {
        public static List<int> PartnersOf(GodState s, int npc)
        {
            var l = new List<int>();
            foreach (var p in s.Households.Partnerships) if (p.A == npc) l.Add(p.B); else if (p.B == npc) l.Add(p.A);
            return l;
        }

        public static int Count(GodState s, int npc)
        {
            int k = 0;
            foreach (var p in s.Households.Partnerships) if (p.A == npc || p.B == npc) k++;
            return k;
        }

        public static Partnership Between(GodState s, int a, int b)
        {
            foreach (var p in s.Households.Partnerships) if (p.A == a && p.B == b || p.A == b && p.B == a) return p;
            return null;
        }

        public static Partnership Form(GodState s, EventBuffer ev, int a, int b, int initiator, int household)
        {
            var hs = s.Households;
            var p = new Partnership { Id = hs.NextPartnershipId++, A = Math.Min(a, b), B = Math.Max(a, b), StartTick = s.Tick, Initiator = initiator };
            hs.Partnerships.Add(p);
            hs.Stats.Partnerships++;
            ev?.Add(new PartnershipFormed { Partnership = p.Id, A = p.A, B = p.B, Initiator = initiator, Household = household });
            return p;
        }

        public static void End(GodState s, EventBuffer ev, Partnership p, byte reason, int initiator)
        {
            var hs = s.Households;
            hs.Partnerships.Remove(p);
            hs.Stats.PartnershipsEnded++;
            ev?.Add(new PartnershipEnded { Partnership = p.Id, A = p.A, B = p.B, Reason = reason, Initiator = initiator });
        }
    }
}
