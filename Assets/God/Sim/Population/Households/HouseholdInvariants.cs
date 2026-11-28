using System.Collections.Generic;

namespace God.Sim.Population.Households
{
    /// <summary>
    /// M4's invariants (from <c>SimInvariants.Check</c>, every day boundary): household membership (M4.14: one household per
    /// NPC, member lists sorted and agreeing with <c>Npc.Household</c>, at most 12, no dead member), stores (one per household
    /// with a hut, at its node, owned by it), partnerships (living, adult, under the cap), estates (C4: nothing owned by an
    /// estate that is not open; C5: estate bins belong to open estates and every estate settles within its window), the
    /// pedigree (one record per person, ascending), and sleeping rights (a home hut holds the NPC's right). Read-only.
    /// </summary>
    public static class HouseholdInvariants
    {
        public static void Check(GodState s, List<string> bad)
        {
            var hs = s.Households;
            var p = s.Population;
            if (hs == null || p == null) return;
            var member = new Dictionary<int, int>();
            int last = 0;
            foreach (var h in hs.Households)
            {
                if (h.Id <= last) bad.Add("households not ascending at " + h.Id);
                last = h.Id;
                if (h.Id >= hs.NextHouseholdId) bad.Add("household " + h.Id + " at or above the next id");
                if (h.Members.Count == 0) bad.Add("household " + h.Id + " has no members");
                if (h.Members.Count > Household.MaxMembers) bad.Add("household " + h.Id + " has " + h.Members.Count + " members");
                for (int i = 1; i < h.Members.Count; i++) if (h.Members[i] <= h.Members[i - 1]) bad.Add("household " + h.Id + " members not sorted");
                foreach (int m in h.Members)
                {
                    if (member.ContainsKey(m)) bad.Add("npc " + m + " in two households");
                    member[m] = h.Id;
                    var n = p.Find(m);
                    if (n == null || !n.Alive) bad.Add("household " + h.Id + " has a dead or missing member " + m);
                    else if (n.Household != h.Id) bad.Add("npc " + m + " is listed in household " + h.Id + " but records " + n.Household);
                }
                if (h.Bin != 0)
                {
                    var b = Goods.BinById(p, h.Bin);
                    if (b == null) bad.Add("household " + h.Id + " store " + h.Bin + " missing");
                    else
                    {
                        bool ownerOk = s.Unification != null || b.Owner.Kind == OwnerKind.Household && b.Owner.Id == h.Id; // UN0: no owner in a unified game
                        if (b.Kind != BinKind.HouseholdStore || !ownerOk) bad.Add("store " + b.Id + " is not household " + h.Id + "'s");
                    }
                    if (h.Hut == 0) bad.Add("household " + h.Id + " has a store and no hut");
                }
            }
            foreach (var n in p.Npcs)
            {
                if (!Bodies.IsPerson(n)) continue; // F1.8: households are of people
                if (!n.Alive) { if (n.Household != 0) bad.Add("dead npc " + n.Id + " still in household " + n.Household); continue; }
                if (n.Household != 0 && (!member.TryGetValue(n.Id, out int h) || h != n.Household)) bad.Add("npc " + n.Id + " records household " + n.Household + " but is not listed");
                if (n.HomeHut != 0) { var hut = p.HutById(n.HomeHut); if (hut == null || !hut.HasRight(n.Id)) bad.Add("npc " + n.Id + " has home hut " + n.HomeHut + " without a right there"); }
            }
            foreach (var b in p.Bins)
            {
                // bins of M4's own households and estates (a bin a test places for an id that is no household or M4 person is not M4's)
                if (b.Kind == BinKind.HouseholdStore)
                {
                    var h = b.Owner.Kind == OwnerKind.Household ? hs.Find(b.Owner.Id) : null;
                    if (h == null) continue;
                    if (h.Bin != b.Id) bad.Add("store " + b.Id + " is not household " + h.Id + "'s store");
                    else { var hut = p.HutById(h.Hut); if (hut != null && hut.Node != b.Node) bad.Add("store " + b.Id + " is not at its hut"); }
                }
                else if (b.Kind == BinKind.Estate && s.Unification == null) // UN0 (contracts 23.11): estate rules in legacy mode only
                {
                    if (b.Owner.Kind != OwnerKind.Estate || p.Find(b.Owner.Id)?.Life == null) continue;
                    bool open = false;
                    foreach (var e in hs.Estates) if (e.Bin == b.Id || e.ShareBins.Contains(b.Id)) { open = true; break; }
                    if (!open) bad.Add("C5: estate bin " + b.Id + " belongs to no open estate");
                }
            }
            int cap = s.Tuning.Households.Partner.MaxLivePartnerships;
            var count = new SortedDictionary<int, int>();
            foreach (var pr in hs.Partnerships)
            {
                foreach (int x in new[] { pr.A, pr.B })
                {
                    var n = p.Find(x);
                    if (n == null || !n.Alive) bad.Add("partnership " + pr.Id + " names a dead person " + x);
                    count[x] = count.TryGetValue(x, out int k) ? k + 1 : 1;
                }
            }
            foreach (var kv in count) if (kv.Value > cap) bad.Add("npc " + kv.Key + " holds " + kv.Value + " partnerships (cap " + cap + ")");
            // D4: reproductive relationships: two living adults, ascending ids, under the cap, each mind's own record agreeing
            int ucap = System.Math.Max(1, s.Tuning.Households.Partner.MaxLiveUnions);
            var ucount = new SortedDictionary<int, int>();
            int lastU = 0;
            foreach (var u in hs.Unions)
            {
                if (u.Id <= lastU) bad.Add("unions not ascending at " + u.Id);
                lastU = u.Id;
                if (u.A >= u.B) bad.Add("union " + u.Id + " pair not ordered");
                foreach (int x in new[] { u.A, u.B })
                {
                    var n = p.Find(x);
                    if (n == null || !n.Alive || n.Stage == LifeStage.Child) bad.Add("union " + u.Id + " names a dead person or a child " + x);
                    ucount[x] = ucount.TryGetValue(x, out int k) ? k + 1 : 1;
                }
            }
            foreach (var kv in ucount) if (kv.Value > ucap) bad.Add("npc " + kv.Key + " holds " + kv.Value + " reproductive relationships (cap " + ucap + ")");
            if (hs.Economic.Count > 0) bad.Add("economic partnerships exist (a stub type in M4)");
            if (s.Unification == null) // UN0 (contracts 23.11): estate rules in legacy mode only
            foreach (var e in hs.Estates)
                if (s.Tick > e.DueTick) bad.Add("estate " + e.Deceased + " open past its deadline");
            // C4: lots owned by an estate only while it is open
            void Lot(ItemLot l, string where)
            {
                if (s.Unification != null || l.Owner.Kind != OwnerKind.Estate || p.Find(l.Owner.Id)?.Life == null) return; // UN0: legacy only
                bool open = false;
                foreach (var e in hs.Estates) if (e.Deceased == l.Owner.Id) { open = true; break; }
                if (!open) bad.Add("C4: lot " + l.Id + " in " + where + " owned by a settled estate " + l.Owner.Id);
            }
            foreach (var n in p.Npcs) if (n.Alive) foreach (var l in n.Carry) if (!l.Empty) Lot(l, "a carry");
            foreach (var b in p.Bins) foreach (var l in b.Lots) Lot(l, "bin " + b.Id);
            if (p.Pedigree != null)
                for (int i = 1; i < p.Pedigree.Count; i++) if (p.Pedigree[i].Id <= p.Pedigree[i - 1].Id) bad.Add("pedigree not ascending at " + p.Pedigree[i].Id);
        }
    }
}
