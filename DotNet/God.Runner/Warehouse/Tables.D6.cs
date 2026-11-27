namespace God.Runner.Warehouse
{
    /// <summary>Package D6's table statements: its own <c>0014_commandments_religion.sql</c> tables. OWNER: D6.</summary>
    public static partial class Tables
    {
        public static readonly TableDef FactCommandment = new TableDef("fact_commandment", 93, "run_key", "tick", "seq", "date_key", "intervention_id", "commandment_id",
            "god_key", "template_id", "importance", "rule_count", "target_kind_code", "target_id", "target_node", "target_radius", "recipients");
        public static readonly TableDef FactCommandmentRevision = new TableDef("fact_commandment_revision", 94, "run_key", "tick", "seq", "date_key", "intervention_id",
            "commandment_id", "god_key", "kind_code", "revision", "importance", "rule_count", "recipients");
        public static readonly TableDef FactCommandmentKnowledge = new TableDef("fact_commandment_knowledge", 95, "run_key", "tick", "seq", "date_key", "person_key",
            "commandment_id", "kind_code", "revision", "source_code", "from_person_key", "intervention_id", "salience", "adoption", "stance_from_code", "stance_to_code",
            "cause_code", "is_update", "evicted", "variant_mask");
        public static readonly TableDef FactCommandmentChoice = new TableDef("fact_commandment_choice", 96, "run_key", "tick", "seq", "date_key", "person_key",
            "commandment_id", "modality_code", "verb", "target_id", "term");
        public static readonly TableDef FactReligion = new TableDef("fact_religion", 97, "run_key", "tick", "seq", "date_key", "religion_id", "god_key", "parent_religion_id");
        public static readonly TableDef FactAffiliation = new TableDef("fact_affiliation", 98, "run_key", "tick", "seq", "date_key", "person_key", "religion_id", "god_key",
            "opened", "reason_code", "community_code", "citizenship_code");
        public static readonly TableDef FactGodBelief = new TableDef("fact_god_belief", 98, "run_key", "tick", "seq", "date_key", "person_key", "god_key", "cause_code",
            "cause_ref", "d_belief", "d_faith", "d_trust", "d_fear", "d_loyalty", "belief", "faith", "trust", "fear", "loyalty");
    }
}
