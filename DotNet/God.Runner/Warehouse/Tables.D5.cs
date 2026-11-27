namespace God.Runner.Warehouse
{
    /// <summary>D5's table statements (OWNER: D4, holding D5's slot): its own <c>0011_avatar.sql</c> table.</summary>
    public static partial class Tables
    {
        public static readonly TableDef FactAvatarManifestation = new TableDef("fact_avatar_manifestation", 94, "run_key", "tick", "seq", "date_key",
            "intervention_id", "god_key", "body_id", "kind_code", "location_key", "health", "created", "superseded_possession_id", "end_reason_code",
            "start_tick", "ticks", "lots_dropped", "actions", "observers", "damage_taken", "by_person_key", "by_intervention_id", "cause_code",
            "cooldown_until", "amount", "verb", "target_kind_code", "target_id", "target_node", "action_tick", "attending", "control_code");
    }
}
