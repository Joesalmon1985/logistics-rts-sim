namespace God.Runner.Warehouse
{
    /// <summary>C7's table statements (OWNER: C7): migration <c>0019_health_education.sql</c>. Orders 200-209 (C7's block).</summary>
    public static partial class Tables
    {
        public static readonly TableDef FactHealthEncounter = new TableDef("fact_health_encounter", 200, "run_key", "request_id", "org_key", "healer_person_key", "patient_person_key",
            "skill", "remedy_lot", "seen_need", "control_code", "start_tick", "start_date_key");
        public static readonly TableDef FactHealthEncounterEnd = new TableDef("fact_health_encounter", 201,
            "UPDATE fact_health_encounter SET end_tick = @p2, ticks = @p3, outcome_code = @p4, severity_permille = @p5, injury_permille = @p6, remedy_used = @p7, "
            + "severity_before = @p8, severity_after = @p9, injury_before = @p10, injury_after = @p11, health_before = @p12, health_after = @p13 "
            + "WHERE run_key = @p0 AND request_id = @p1 AND end_tick IS NULL", 14);
        public static readonly TableDef FactEducationEnrolment = new TableDef("fact_education_enrolment", 202, "run_key", "tick", "seq", "date_key", "teacher_person_key",
            "learner_person_key", "topic_code", "kind_code", "ticks", "org_key", "control_code");
        public static readonly TableDef FactEducationAssessment = new TableDef("fact_education_assessment", 203, "run_key", "tick", "seq", "date_key", "assessment_kind",
            "person_key", "teacher_person_key", "topic_code", "entries_copied", "fidelity", "cause_code", "skill_before", "skill_after");
    }
}
