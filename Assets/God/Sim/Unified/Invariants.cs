using System.Collections.Generic;
using God.Sim.Population;

namespace God.Sim.Unified
{
    /// <summary>
    /// UN0 (contracts 23.11): the unified-mode invariants, run by <c>SimInvariants.Check</c> only while <c>GodState.Unification</c>
    /// exists. They replace the legacy deceased-owner rule C4 (which <c>PopulationInvariants</c> skips in a unified game); C1, C2 and
    /// C5 of <c>PopulationInvariants</c> keep running in both modes and are U-C1, U-C2 and U-C3 (over all 160 wares and 16 reasons).
    /// <list type="bullet">
    /// <item>U-C1 ledger conservation: <c>PopulationInvariants</c> C1, plus the ledger layout (16 reasons x 160 wares).</item>
    /// <item>U-C2 one holder per unit: C2, plus one origin per live lot.</item>
    /// <item>U-C3 no bin over capacity, no carry over weight: C5.</item>
    /// <item>U-C4 no lot deleted or moved without an event (the event-stream half is <c>GoodsTrace</c>'s): a dead person holds
    /// nothing; every live lot has its origin and no origin outlives its lot; every person has one unified record.</item>
    /// <item>U-C5 every <c>Owner</c> field is <c>None</c> (carry, ground, bins and their lots).</item>
    /// <item>U-C6 site bins exist and are <c>BinKind.Site</c>; reservations name a building, hold units and expire (contents versus
    /// the design's requirement and reservations versus room are added by UN4 and UN3b in their <c>Check</c> slots).</item>
    /// </list>
    /// Read-only.
    /// </summary>
    public static class UnifiedInvariants
    {
        public static void Check(GodState s, List<string> bad)
        {
            var u = s.Unification;
            var p = s.Population;
            if (u == null) return;
            if (p == null) { bad.Add("U: a unified game without a population"); return; }
            // U-C1: the ledger layout
            if (p.Ledger.Produced.Length != GoodsLedger.ProduceReasons * Wares.PersonalCount || p.Ledger.Consumed.Length != GoodsLedger.ConsumeReasons * Wares.PersonalCount)
                bad.Add("U-C1: ledger is not 16 reasons x 160 wares");
            // live lots (C2 checks each is held once)
            var live = new HashSet<int>();
            foreach (var n in p.Npcs)
            {
                foreach (var l in n.Carry)
                {
                    if (l.Empty) continue;
                    live.Add(l.Id);
                    if (!l.Owner.IsNone) bad.Add("U-C5: lot " + l.Id + " carried by " + n.Id + " has an owner (" + l.Owner + ")");
                }
                if (Bodies.IsPerson(n) && n.Alive && u.PersonOf(n.Id) == null) bad.Add("U-C4: person " + n.Id + " has no unified record");
            }
            foreach (var g in p.Lots)
            {
                live.Add(g.Lot.Id);
                if (!g.Lot.Owner.IsNone) bad.Add("U-C5: ground lot " + g.Lot.Id + " has an owner");
            }
            foreach (var b in p.Bins)
            {
                if (!b.Owner.IsNone) bad.Add("U-C5: bin " + b.Id + " has an owner (" + b.Owner + ")");
                foreach (var l in b.Lots)
                {
                    live.Add(l.Id);
                    if (!l.Owner.IsNone) bad.Add("U-C5: lot " + l.Id + " in bin " + b.Id + " has an owner");
                }
            }
            // U-C2 / U-C4: origins follow the lots exactly
            int last = 0;
            var withOrigin = new HashSet<int>();
            foreach (var o in u.Origins)
            {
                if (o.Lot <= last) bad.Add("U-C4: origins not sorted at lot " + o.Lot);
                last = o.Lot;
                withOrigin.Add(o.Lot);
                if (!live.Contains(o.Lot)) bad.Add("U-C4: origin of lot " + o.Lot + " outlives its lot");
            }
            foreach (int id in live) if (!withOrigin.Contains(id)) bad.Add("U-C2: live lot " + id + " has no origin");
            // people records sorted, each a person
            int lastNpc = 0;
            foreach (var r in u.People)
            {
                if (r.Npc <= lastNpc) bad.Add("U-C4: unified people not sorted at " + r.Npc);
                lastNpc = r.Npc;
                if (r.Needs == null || r.Needs.Length != NeedChannels.Extensions) bad.Add("U-C4: person " + r.Npc + " needs array");
                if (p.Find(r.Npc) == null) bad.Add("U-C4: unified record of unknown npc " + r.Npc);
            }
            // U-C6
            foreach (var site in u.Sites)
            {
                var b = Goods.BinById(p, site.Bin);
                if (site.Bin != 0 && (b == null || b.Kind != BinKind.Site)) bad.Add("U-C6: site " + site.Id + " bin " + site.Bin + " is not a site bin");
                else if (b != null && b.WeightCap > 0 && b.Weight > b.WeightCap) bad.Add("U-C6: site " + site.Id + " holds more than its design takes");
                var def = Catalogue.Building(site.Design);
                if (b != null && def != null && site.Level < def.Levels.Count)
                    foreach (var l in b.Lots)
                    {
                        int need = 0;
                        foreach (var m in def.Levels[site.Level].Materials) if (m.Ware == l.Ware) need += m.Count;
                        int have = 0;
                        foreach (var x in b.Lots) if (x.Ware == l.Ware) have += x.Count;
                        if (have > need) { bad.Add("U-C6: site " + site.Id + " holds " + have + " " + Wares.Name(l.Ware) + ", the design needs " + need); break; }
                    }
                if (site.Contributors.Count > ConstructionSite.MaxContributors) bad.Add("U-C6: site " + site.Id + " has too many contributors");
            }
            foreach (var r in u.Reservations)
            {
                if (u.BuildingById(r.Building) == null) bad.Add("U-C6: reservation at unknown building " + r.Building);
                if (r.Units <= 0) bad.Add("U-C6: reservation of " + r.Units + " units");
                if (r.Expires <= 0) bad.Add("U-C6: reservation without expiry");
                else if (r.Expires < s.Tick) bad.Add("U-C6: reservation at building " + r.Building + " for npc " + r.Npc + " expired at " + r.Expires + " and is still open");
            }
            UnifiedHooks.Check(s, bad);
        }
    }
}
