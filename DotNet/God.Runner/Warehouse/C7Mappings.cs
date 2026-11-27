using God.Sim.Orgs;

namespace God.Runner.Warehouse
{
    /// <summary>
    /// C7's warehouse mappers (OWNER: C7; migration <c>0019_health_education.sql</c>, tables in <c>Tables.C7.cs</c>): health
    /// encounters (TreatmentStarted inserts, TreatmentEnded fills the end; truth before/after at Full only), teaching
    /// enrolments and assessments (knowledge transferred, skill changes). Every row is written whatever the sampling.
    /// </summary>
    public static class C7Mappings
    {
        /// <summary>The grain of each C7 table (written to <c>meta_table_grain</c>).</summary>
        public static readonly (string table, string grain, string events)[] Grains =
        {
            ("fact_health_encounter", "one row per treatment (request_id joins fact_service_request)", "TreatmentStarted, TreatmentEnded"),
            ("fact_education_enrolment", "one row per learner per teaching session", "TeachingSession"),
            ("fact_education_assessment", "one row per measured change on one person (knowledge transferred, skill changed)", "KnowledgeTransferred, HealerSkillChanged"),
        };

        public static void Register(EventMapRegistry r)
        {
            object P(MapContext c, int npc) => npc > 0 ? (object)c.Person(npc).Key : null;
            r.Register<TreatmentStarted>("fact_health_encounter", "health_encounter", (c, e) =>
                c.Insert(Tables.FactHealthEncounter, (long)e.Request, e.Org > 0 ? (object)(long)e.Org : null, P(c, e.Healer), P(c, e.Patient), (long)e.Skill,
                    e.RemedyLot != 0 ? (object)(long)e.RemedyLot : null, (long)e.SeenNeed, (long)e.Control, c.Tick, c.DateKey()));
            r.Register<TreatmentEnded>("fact_health_encounter", "health_encounter", (c, e) =>
                c.Insert(Tables.FactHealthEncounterEnd, (long)e.Request, c.Tick, (long)e.Ticks, (long)e.Outcome, (long)e.SeverityPermille, (long)e.InjuryPermille, e.Remedy ? 1L : 0L,
                    c.Full ? (object)(long)e.SeverityBefore : null, c.Full ? (object)(long)e.SeverityAfter : null, c.Full ? (object)(long)e.InjuryBefore : null,
                    c.Full ? (object)(long)e.InjuryAfter : null, c.Full ? (object)(long)e.HealthBefore : null, c.Full ? (object)(long)e.HealthAfter : null));
            r.Register<TeachingSession>("fact_education_enrolment", "education_enrolment", (c, e) =>
                c.Insert(Tables.FactEducationEnrolment, c.Tick, c.Seq, c.DateKey(), P(c, e.Teacher), P(c, e.Learner), (long)e.Topic, (long)e.Kind, (long)e.Ticks,
                    e.Org > 0 ? (object)(long)e.Org : null, (long)e.Control));
            r.Register<KnowledgeTransferred>("fact_education_assessment", "education_assessment", (c, e) =>
                c.Insert(Tables.FactEducationAssessment, c.Tick, c.Seq, c.DateKey(), 1L, P(c, e.Learner), P(c, e.Teacher), (long)e.Topic, (long)e.EntriesCopied, (long)e.Fidelity,
                    (long)e.Kind, (long)e.SkillBefore, (long)e.SkillAfter));
            r.Register<HealerSkillChanged>("fact_education_assessment", "education_assessment", (c, e) =>
                c.Insert(Tables.FactEducationAssessment, c.Tick, c.Seq, c.DateKey(), 2L, P(c, e.Npc), null, (long)e.Skill, null, null, (long)e.Cause, (long)e.From, (long)e.To));
        }
    }
}
