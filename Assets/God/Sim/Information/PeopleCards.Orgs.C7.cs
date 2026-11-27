using God.Sim.Civ;
using God.Sim.Orgs;
using God.Sim.Population;

namespace God.Sim.Information
{
    /// <summary>
    /// C7's rows of the organisations tab (the HealthCard of the C7 spec; OWNER: C7; read-only): skills held (truth of the skill
    /// store), treatments given and received (truth: the treated records and the treatment in progress), whom it believes to be
    /// healers and the teaching offer it heard (its own beliefs, layer 6).
    /// </summary>
    public static partial class OrgCards
    {
        /// <summary>Row kinds appended by C7 (after C6's 1-4).</summary>
        public const byte SkillHeld = 5, TreatmentRow = 6, HealerBelieved = 7, TeachingRow = 8;

        static partial void FillC7(GodState s, Npc n, OrgCard c)
        {
            void Row(byte kind, KnowledgeLayer layer, int reference, string text) { c.Rows.Add(new OrgCardRow(kind, layer, reference, text)); c.Lines.Add("[" + layer + "] " + text); }
            var st = s.Civ?.Skills;
            if (st != null)
                foreach (var r in st.Rows) if (r.Npc == n.Id) Row(SkillHeld, KnowledgeLayer.Truth, (int)r.Class, "skill " + r.Class.ToString().ToLowerInvariant() + " " + r.Value);
            var hs = s.Organisations?.Health;
            if (hs != null)
            {
                foreach (var t in hs.Active)
                {
                    if (t.Healer == n.Id) Row(TreatmentRow, KnowledgeLayer.Truth, t.Request, "treating person " + t.Patient + " (" + t.Progress + " ticks so far)");
                    if (t.Patient == n.Id) Row(TreatmentRow, KnowledgeLayer.Truth, t.Request, "being treated by person " + t.Healer);
                }
                int i = hs.IndexOfTreated(n.Id);
                if (i >= 0) Row(TreatmentRow, KnowledgeLayer.Truth, hs.Treated[i].Healer, "treated on day " + hs.Treated[i].Day + " by person " + hs.Treated[i].Healer
                    + " (illness loss x" + hs.Treated[i].SeverityPermille + "/1000, wound healing x" + hs.Treated[i].InjuryPermille + "/1000)");
            }
            var h = n.Mind?.Orgs?.Health;
            if (h == null) return;
            if (h.Healer) Row(HealerBelieved, KnowledgeLayer.Belief, n.Id, "knows how to heal; treatments given " + h.Given + " (" + h.GivenInformal + " informally)");
            for (int k = 0; k < HealthMind.HealerCap; k++) if (h.HealerAt(k) != 0) Row(HealerBelieved, KnowledgeLayer.Belief, h.HealerAt(k), "believes person " + h.HealerAt(k) + " is a healer");
            if (h.OfferFrom != 0) Row(TeachingRow, KnowledgeLayer.Belief, h.OfferFrom, "was offered teaching (" + Education.TopicName(h.OfferTopic) + ") by person " + h.OfferFrom + (h.Attending ? ", attending" : ""));
        }
    }
}
