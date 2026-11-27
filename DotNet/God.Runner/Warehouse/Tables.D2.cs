namespace God.Runner.Warehouse
{
    /// <summary>D2's table statements: its own <c>0009_commands.sql</c> tables. OWNER: D2.</summary>
    public static partial class Tables
    {
        public static readonly TableDef FactDivineCommand = new TableDef("fact_divine_command", 85, "run_key", "tick", "seq", "date_key", "intervention_id", "god_key",
            "kind_code", "act_code", "target_kind_code", "target_id", "target_node", "target_radius", "scope_code", "recipients", "not_reached", "expires_tick");
        public static readonly TableDef FactDivineCommandTarget = new TableDef("fact_divine_command_target", 86, "run_key", "tick", "seq", "date_key", "intervention_id",
            "org_id", "person_key");
        public static readonly TableDef FactCommandPerception = new TableDef("fact_command_perception", 87, "run_key", "tick", "seq", "date_key", "intervention_id",
            "kind_code", "person_key", "recipient_person_key", "instruction_kind_code", "decoded_act_code", "topic_kind_code", "topic_id", "clarity", "on_waking",
            "evicted_intervention_id", "location_key", "mismatch");
        public static readonly TableDef FactCommandResponse = new TableDef("fact_command_response", 88, "run_key", "tick", "seq", "date_key", "intervention_id",
            "person_key", "god_key", "instruction_kind_code", "response_code", "verb", "divine_term");
        public static readonly TableDef FactCompulsion = new TableDef("fact_compulsion", 89, "run_key", "tick", "seq", "date_key", "intervention_id", "god_key",
            "person_key", "kind_code", "act_code", "target_kind_code", "target_id", "target_node", "target_radius", "expires_tick", "end_reason_code", "started", "since_tick");
    }
}
