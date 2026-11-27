namespace God.Runner.Warehouse
{
    /// <summary>D4's table statements: its own <c>0010_possession.sql</c> table. OWNER: D4.</summary>
    public static partial class Tables
    {
        public static readonly TableDef FactPossession = new TableDef("fact_possession", 93, "run_key", "tick", "seq", "date_key", "intervention_id", "god_key",
            "person_key", "kind_code", "location_key", "superseded_compulsion_id", "superseded_avatar_id", "end_reason_code", "start_tick", "ticks",
            "inputs_applied", "inputs_refused", "actions", "verb", "target_kind_code", "target_id", "target_node", "action_tick", "control_code");
    }
}
