using System.Collections.Generic;
using God.Sim.Learning;

namespace God.Sim.Population
{
    /// <summary>
    /// The population's invariants (ACCEPTANCE G3.1, G3.3, G3.4, G3.7): goods conserved exactly per ware against the one
    /// ledger; no empty or negative lot; no carry over its limit; ids ascending and below their next id; every learning
    /// store, belief store and conjunction pool within its caps and every weight within +-4000; one cause per death.
    /// Read-only.
    /// </summary>
    public static class PopulationInvariants
    {
        public static void Check(GodState s, List<string> bad)
        {
            var p = s.Population;
            if (p == null) return;
            var held = new long[Wares.PersonalCount];
            int lastId = 0, deaths = 0;
            var causes = new int[p.Stats.DeathsByCause.Length];
            foreach (var n in p.Npcs)
            {
                if (n.Id <= lastId) bad.Add("npc ids are not ascending at " + n.Id);
                lastId = n.Id;
                if (n.Id >= p.NextNpcId) bad.Add("npc " + n.Id + " is at or above the next id");
                if (!n.Alive)
                {
                    deaths++;
                    if (n.DeathCause == DeathCause.None) bad.Add("dead npc " + n.Id + " has no cause");
                    else causes[(int)n.DeathCause]++;
                    foreach (var l in n.Carry) if (!l.Empty) bad.Add("dead npc " + n.Id + " still carries goods");
                    continue;
                }
                foreach (var l in n.Carry)
                {
                    if (l.Id == 0) continue;
                    if (l.Count <= 0) bad.Add("npc " + n.Id + " carries an empty lot " + l.Id);
                    if (l.Id >= p.NextLotId) bad.Add("lot " + l.Id + " is at or above the next id");
                    held[(int)l.Ware] += l.Count;
                }
                if (PopulationModule.CarryWeight(n) > s.Tuning.Population.CarryWeight) bad.Add("npc " + n.Id + " carries over its limit");
                for (int d = 0; d < n.Drives.Length; d++) if (n.Drives[d] < 0 || n.Drives[d] > 1000000) bad.Add("npc " + n.Id + " drive " + d + " out of range");
                var m = n.Mind;
                if (m == null) { bad.Add("living npc " + n.Id + " has no mind"); continue; }
                CheckStore(n.Id, m.Store, s.Tuning.Learning, bad);
                if (m.Beliefs.Count > m.Beliefs.Cap) bad.Add("npc " + n.Id + " has too many beliefs");
            }
            foreach (var g in p.Lots)
            {
                if (g.Lot.Count <= 0) bad.Add("empty lot on the ground " + g.Lot.Id);
                held[(int)g.Lot.Ware] += g.Lot.Count;
            }
            for (int w = 0; w < Wares.PersonalCount; w++)
            {
                long balance = p.Ledger.ProducedOf((Ware)w) - p.Ledger.ConsumedOf((Ware)w);
                if (balance != held[w]) bad.Add("ware " + Wares.Name((Ware)w) + " is not conserved: produced - consumed = " + balance + ", held " + held[w]);
            }
            if (deaths != p.Stats.Deaths) bad.Add("deaths " + deaths + " but the counter says " + p.Stats.Deaths);
            for (int c = 0; c < causes.Length; c++) if (causes[c] != p.Stats.DeathsByCause[c]) bad.Add("deaths by cause " + (DeathCause)c + " do not add up");
            foreach (var h in p.Huts)
            {
                if (h.LogsIn > h.LogsNeeded) bad.Add("hut " + h.Id + " has too many logs");
                foreach (int r in h.Rights) if (r != 0) { var o = p.Find(r); if (o == null || !o.Alive || o.HomeHut != h.Id) bad.Add("hut " + h.Id + " right of " + r + " does not match"); }
            }
        }

        public static void CheckStore(int npc, AssociationStore st, LearningTuning tu, List<string> bad)
        {
            if (st.Count > st.Entries.Length || st.Entries.Length != tu.Entries) bad.Add("npc " + npc + " store over its cap");
            if (st.RingCount > st.Ring.Length || st.OpenCount > st.Open.Length || st.DayCount > st.Day.Length) bad.Add("npc " + npc + " traces over their caps");
            if (st.Bytes > 10240 && tu.Entries == 256) bad.Add("npc " + npc + " learning bytes " + st.Bytes + " over 10240");
            for (int i = 0; i < st.Count; i++)
            {
                var e = st.Entries[i];
                if (e.Stw > Fx.WeightCap || e.Stw < -Fx.WeightCap || e.Ltw > Fx.WeightCap || e.Ltw < -Fx.WeightCap) bad.Add("npc " + npc + " weight out of range");
                if (i > 0)
                {
                    var q = st.Entries[i - 1];
                    if (q.Key > e.Key || q.Key == e.Key && q.Verb >= e.Verb) { bad.Add("npc " + npc + " store not sorted"); break; }
                }
            }
        }
    }
}
