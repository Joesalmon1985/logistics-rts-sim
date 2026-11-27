using System.Collections.Generic;

namespace God.Sim
{
    /// <summary>
    /// Consistency checks over the whole state, for tests and headless runs (bots included). Read-only. Returns one line
    /// per broken rule; an empty list means the state is sound.
    /// </summary>
    public static class SimInvariants
    {
        public static List<string> Check(GodState s)
        {
            var bad = new List<string>();
            var t = s.Territory; var e = s.Economy; var m = s.Military;

            foreach (var b in t.Buildings)
            {
                var f = t.FlagById(b.FlagId);
                if (f == null) bad.Add("building " + b.Id + " has no flag");
                else if (f.Faction != b.Faction) bad.Add("building " + b.Id + " and its flag belong to different factions");
                if (t.NodeBuilding[b.Node] != b.Id) bad.Add("building " + b.Id + " is not on its node");
                var rec = e?.Find(b.Id);
                if (rec != null && rec.Faction != b.Faction) bad.Add("economy record " + b.Id + " has the wrong faction");
                var mr = m?.BuildingById(b.Id);
                if (mr != null && mr.Faction != b.Faction) bad.Add("health record " + b.Id + " has the wrong faction");
            }
            foreach (var h in t.Hqs)
            {
                var b = t.BuildingById(h.Id);
                if (b == null || b.Faction != h.Faction) bad.Add("hq " + h.Id + " and its building disagree");
            }
            if (s.Factions != null)
                foreach (var b in t.Buildings) if (s.Factions.IsEliminated(b.Faction)) bad.Add("eliminated faction still owns building " + b.Id);

            if (e != null)
            {
                foreach (var b in e.Buildings)
                {
                    for (int w = 0; w < Wares.Count; w++)
                    {
                        if (b.In[w] < 0 || b.Incoming[w] < 0 || b.Stock[w] < 0 || b.Out[w] < 0) bad.Add("negative count at " + b.Id);
                        if (b.OutReserved[w] > b.Out[w] || b.StockReserved[w] > b.Stock[w]) bad.Add("over-reserved at " + b.Id);
                    }
                    if (b.IsHq && b.Built)
                    {
                        int carriers = b.IdleCarriers + e.CarriersOut(b.Id);
                        if (carriers != EconomyTuning.CarriersPerHq) bad.Add("hq " + b.Id + " has " + carriers + " carriers");
                        int builders = b.IdleBuilders + e.BuildersOut(b.Id);
                        if (builders != EconomyTuning.BuildersPerHq) bad.Add("hq " + b.Id + " has " + builders + " builders");
                    }
                }
                foreach (var w in e.Walkers)
                {
                    var home = e.Find(w.Home);
                    if (home == null) continue; // swept next tick
                    if (home.Faction != w.Faction) bad.Add("walker " + w.Id + " works for another faction's building");
                }
            }
            if (m != null && s.Factions != null)
                foreach (var u in m.Units) if (s.Factions.IsEliminated(u.Faction)) bad.Add("eliminated faction still has unit " + u.Id);
            CheckTrade(s, bad);
            Population.PopulationInvariants.Check(s, bad);
            if (s.Population != null) Population.PopulationExtensions.Check(s, bad); // F0.13: M4's and M5's invariants
            if (s.Civ != null) Civ.CivModule.Check(s, bad);                         // F1.5: Civ, Orgs, Divinity
            if (s.Organisations != null) Orgs.OrgModule.Check(s, bad);
            if (s.Divinity != null) Divinity.DivinityModule.Check(s, bad);
            return bad;
        }

        /// <summary>Trade: ledger (goods in transit are exactly what left stocks and has not arrived or been lost), offers match envoys, no dead owners.</summary>
        private static void CheckTrade(GodState s, List<string> bad)
        {
            var tr = s.Trade;
            if (tr == null) return;
            for (int w = 1; w < Wares.Count; w++)
            {
                int transit = 0;
                foreach (var env in tr.Envoys) { if (env.Cargo[w] < 0) bad.Add("envoy " + env.Id + " has negative cargo"); transit += env.Cargo[w]; }
                if (tr.Departed[w] - tr.Arrived[w] - tr.Lost[w] != transit) bad.Add("ware " + Wares.Name((Ware)w) + " is not conserved: left " + tr.Departed[w] + ", arrived " + tr.Arrived[w] + ", lost " + tr.Lost[w] + ", in packs " + transit);
            }
            int lastId = 0;
            foreach (var env in tr.Envoys)
            {
                if (env.Id <= lastId) bad.Add("envoy ids are not ascending at " + env.Id);
                lastId = env.Id;
                if (env.Cargo.Length != Wares.Count || env.CargoTotal > TradeTuning.MaxWaresPerSide) bad.Add("envoy " + env.Id + " carries " + env.CargoTotal);
                if (env.Health <= 0) bad.Add("dead envoy " + env.Id + " is still walking");
                if (s.Factions != null && s.Factions.IsEliminated(env.Faction)) bad.Add("eliminated faction still has envoy " + env.Id);
                var o = tr.OfferById(env.Id);
                bool unresolved = env.Stage != EnvoyStage.Homeward;
                if (unresolved != (o != null)) bad.Add("envoy " + env.Id + " and its offer disagree");
                if (o != null && (o.Stage == OfferStage.Pending) != (env.Stage == EnvoyStage.Waiting)) bad.Add("offer " + o.Id + " stage does not match its envoy");
                if (o != null && (o.From != env.Faction || o.To != env.Target)) bad.Add("offer " + o.Id + " parties do not match its envoy");
            }
            foreach (var o in tr.Offers) if (tr.EnvoyById(o.Id) == null) bad.Add("offer " + o.Id + " has no envoy");
        }
    }
}
