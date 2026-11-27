namespace God.Runner.Warehouse
{
    /// <summary>
    /// D1's table statements: the rows of F1's <c>0007</c> it fills (dim_god, dim_god_history, fact_god_faction) and its own
    /// <c>0008_mythic.sql</c> tables. Dimensions before the facts that point at them. OWNER: D1.
    /// </summary>
    public static partial class Tables
    {
        public static readonly TableDef DimGod = new TableDef("dim_god", 25, "run_key", "god_key", "god_id", "name", "origin_faction_key", "created_tick", "created_date_key");
        public static readonly TableDef DimGodHistory = new TableDef("dim_god_history", 26, "run_key", "god_hist_key", "god_key", "from_tick", "to_tick", "seat_code",
            "policy_name", "mythic_mode_code", "change_reason");
        public static readonly TableDef DimGodHistoryClose = new TableDef("dim_god_history", 27,
            "UPDATE dim_god_history SET to_tick = @p2 WHERE run_key = @p0 AND god_hist_key = @p1", 3);
        public static readonly TableDef FactGodFaction = new TableDef("fact_god_faction", 80, "run_key", "god_key", "faction_key", "from_tick", "to_tick", "kind_code");
        public static readonly TableDef FactMythicLedger = new TableDef("fact_mythic_ledger", 81, "run_key", "tick", "seq", "date_key", "god_key", "event_code",
            "kind_code", "intervention_id", "amount", "nominal", "waived", "capped", "balance_after", "upkeep_ticks", "source_code");
        public static readonly TableDef FactDivineAction = new TableDef("fact_divine_action", 82, "run_key", "tick", "seq", "date_key", "intervention_id", "god_key",
            "kind_code", "act_code", "target_kind_code", "target_id", "target_node", "target_radius", "scope_code", "cost", "charged");
        public static readonly TableDef FactDivineObservation = new TableDef("fact_divine_observation", 83, "run_key", "tick", "seq", "date_key", "god_key",
            "domain_code", "channel_code", "subject_id");
        public static readonly TableDef FactMythicBalanceDay = new TableDef("fact_mythic_balance_day", 84, "run_key", "date_key", "god_key", "balance", "max_balance",
            "credited", "capped", "spent", "waived", "upkeep", "actions", "mythic_mode_code");
    }
}
