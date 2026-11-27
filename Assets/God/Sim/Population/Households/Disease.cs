using System;

namespace God.Sim.Population.Households
{
    /// <summary>
    /// The fever (POPULATION "Disease", owner Q3: On, Mild or Off from <c>GameSetup.Disease</c>). Once a day per person: a
    /// susceptible person is infected with <c>Base + Contact x infected hut-mates + Near x infected people near + Crowding +
    /// Fouling / 4 + Wound</c> per million, each term a visible condition, and the route that fired is recorded; an infected
    /// person loses <c>Severity</c> (scaled) Health a day, 30% less on a day it was tended, and recovers after 3-6 days (a
    /// tended day counts twice), then is immune for 15 days. Mild halves contact, crowding and sanitation and caps severity.
    /// </summary>
    public static class Disease
    {
        public static void Daily(GodState s, EventBuffer ev, Npc npc, int day)
        {
            var life = npc.Life;
            var dt = s.Tuning.Households.Disease;
            var setting = s.Setup?.Disease ?? DiseaseSetting.On;
            if (life.Illness == 1)
            {
                bool tended = life.TendedDay >= day - 1;
                int loss = life.Severity * dt.SeverityScalePermille / 1000;
                if (tended) loss = loss * s.Tuning.Households.Care.TendSeverityPermille / 1000;
                npc.Health -= loss * 1000;
                life.IllDays++;
                if (npc.Health <= 0)
                {
                    ev.Add(new IllnessEnded { Npc = npc.Id, Days = life.IllDays, Recovered = false });
                    life.Illness = 0;
                    PopulationModule.Kill(s, ev, npc, DeathCause.Disease);
                    return;
                }
                life.IllDaysLeft -= tended ? 2 : 1;
                if (life.IllDaysLeft <= 0)
                {
                    life.Illness = 2;
                    life.ImmuneUntilDay = day + dt.ImmuneDays;
                    ev.Add(new IllnessEnded { Npc = npc.Id, Days = life.IllDays, Recovered = true });
                }
                return;
            }
            if (setting == DiseaseSetting.Off) return;
            if (life.Illness == 2 && day < life.ImmuneUntilDay) return;
            bool mild = setting == DiseaseSetting.Mild;
            long baseP = dt.BasePerMillion, contact = 0, crowding = 0, sanitation = 0, wound = 0;
            int source = 0;
            var p = s.Population;
            var g = s.Territory.Grid;
            foreach (var o in p.Npcs)
            {
                if (!Bodies.IsLivingPerson(o) || o.Id == npc.Id || o.Life == null || o.Life.Illness != 1) continue;
                if (npc.HomeHut != 0 && o.HomeHut == npc.HomeHut) { contact += dt.ContactHutPerMillion; if (source == 0) source = o.Id; }
                else if (g.Distance(o.Node, npc.Node) <= dt.NearNodes) { contact += dt.ContactNearPerMillion; if (source == 0) source = o.Id; }
            }
            if (npc.HomeHut != 0)
            {
                var hut = p.HutById(npc.HomeHut);
                if (hut != null)
                {
                    int used = 0; foreach (int r in hut.Rights) if (r != 0) used++;
                    int lodgers = hut.Lodgers?.Count ?? 0;
                    int over = used + (lodgers + 1) / 2 - hut.Beds;
                    if (over > 0) crowding = (long)over * dt.CrowdingPerBedPerMillion;
                }
            }
            int hex = g.HexOf(npc.Node);
            var f = s.Households.Fouling;
            if (f != null && hex >= 0 && hex < f.Length) sanitation = f[hex] / 4;
            if (npc.Injury > dt.WoundFrom * 1000) wound = dt.WoundInfectPerMillion;
            if (mild) { contact /= 2; crowding /= 2; sanitation /= 2; }
            long total = baseP + contact + crowding + sanitation + wound;
            var rng = s.Rng.Fork("illness", npc.Id, day);
            long u = rng.NextInt(1000000);
            if (u >= total) return;
            byte route;
            if (u < baseP) route = IllnessStarted.Import;
            else if (u < baseP + contact) route = IllnessStarted.Contact;
            else if (u < baseP + contact + crowding) route = IllnessStarted.Crowding;
            else if (u < baseP + contact + crowding + sanitation) route = IllnessStarted.Sanitation;
            else route = IllnessStarted.Wound;
            int severity = 100 + (500 - life.Pheno.Health) / 5 + (500 - npc.NutritionPermille) / 10 + (npc.Stage != LifeStage.Adult ? 30 : 0);
            severity = Math.Max(20, severity);
            if (mild) severity = Math.Min(severity, dt.MildSeverityCap);
            int days = dt.MinDays + rng.NextInt(Math.Max(1, dt.MaxDays - dt.MinDays + 1));
            life.Illness = 1; life.Severity = severity; life.IllDaysLeft = days; life.IllDays = 0;
            s.Households.Stats.Infections++;
            ev.Add(new IllnessStarted { Npc = npc.Id, Route = route, Source = route == IllnessStarted.Contact ? source : 0, Severity = severity, Days = days });
        }
    }
}
