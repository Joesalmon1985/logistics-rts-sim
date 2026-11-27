using System;
using System.Collections.Generic;
using God.Sim.Population.Households;

namespace God.Sim.Population
{
    /// <summary>
    /// The population-side functions D3's miracles need (LEAD_RULINGS R3: a new static class, because
    /// <c>PopulationModule</c> is not partial and F1 did not make it so). Public API only; no existing behaviour changes.
    /// Nothing here moves or creates goods: <see cref="HutStoreBins"/> names the bins a hut's removal must first empty
    /// (the caller does that through <see cref="Goods"/>), and <see cref="RemoveHut"/> then removes the hut, its rights and
    /// its link to a household. OWNER: D3.
    /// </summary>
    public static class PopulationMiracles
    {
        /// <summary><c>HutRightLost.Reason</c> of a right that ended because the hut was destroyed (M4's <c>LeaveReason</c> uses 1-9).</summary>
        public const byte HutDestroyed = 20;

        /// <summary>A source's stock is set (a miracle's change; wild stock is not in the goods ledger). Returns the stock before.</summary>
        public static int SetSourceStock(GodState s, ResourceSource src, int stock)
        {
            int before = src.Stock;
            src.Stock = Math.Max(0, Math.Min(src.Capacity, stock));
            src.EmptyDays = 0;
            return before;
        }

        /// <summary>The bins that belong to a hut: its household's store (ascending bin id).</summary>
        public static List<LotBin> HutStoreBins(GodState s, Hut hut)
        {
            var l = new List<LotBin>();
            if (s.Households == null) return l;
            foreach (var h in s.Households.Households)
            {
                if (h.Hut != hut.Id || h.Bin == 0) continue;
                var b = Goods.BinById(s.Population, h.Bin);
                if (b != null) l.Add(b);
            }
            l.Sort((a, b) => a.Id.CompareTo(b.Id));
            return l;
        }

        /// <summary>
        /// Removes a hut (or a site): every sleeping right and lodging ends (<see cref="HutRightLost"/>, home cleared), people
        /// asleep in it are left asleep in the open, a household living there is left without a home and without a store, and
        /// the hut leaves the list. Its store bins must already be empty (<see cref="HutStoreBins"/>); throws otherwise.
        /// Returns the number of people who lost a bed or a place.
        /// </summary>
        public static int RemoveHut(GodState s, EventBuffer ev, Hut hut)
        {
            var p = s.Population;
            foreach (var b in HutStoreBins(s, hut)) if (b.Lots.Count > 0) throw new InvalidOperationException("RemoveHut: store bin " + b.Id + " is not empty");
            int lost = 0;
            var holders = new List<int>();
            foreach (int r in hut.Rights) if (r != 0) holders.Add(r);
            if (hut.Lodgers != null) foreach (int r in hut.Lodgers) holders.Add(r);
            foreach (int id in holders)
            {
                var n = p.Find(id);
                if (n == null || !Bodies.IsPerson(n)) continue;
                HutRights.Revoke(s, ev, hut, n, HutDestroyed);
                lost++;
            }
            foreach (var n in p.Npcs)
            {
                if (!Bodies.IsPerson(n)) continue;
                if (n.HomeHut == hut.Id) n.HomeHut = 0;
                if (n.SiteHut == hut.Id) n.SiteHut = 0;
                if (n.Alive && n.Action.SleepHut == hut.Id) { n.Action.SleepHut = 0; n.Action.TargetKind = Policy.IntentTarget.Node; }
            }
            if (s.Households != null)
                foreach (var h in s.Households.Households)
                {
                    if (h.Hut != hut.Id) continue;
                    if (h.Bin != 0) { Goods.RemoveBin(s, h.Bin); h.Bin = 0; }
                    h.Hut = 0;
                }
            p.Huts.Remove(hut);
            p.IndexDirty = true;
            return lost;
        }
    }
}
