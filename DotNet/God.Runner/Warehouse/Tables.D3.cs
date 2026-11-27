namespace God.Runner.Warehouse
{
    /// <summary>D3's table statements: its own <c>0012_miracles.sql</c> tables and the rows of F1's <c>0007</c> <c>fact_resource_ledger</c>. OWNER: D3.</summary>
    public static partial class Tables
    {
        public static readonly TableDef FactMiracle = new TableDef("fact_miracle", 90, "run_key", "tick", "seq", "date_key", "intervention_id", "god_key", "kind_code",
            "target_kind_code", "target_id", "target_node", "target_radius", "magnitude", "cost");
        public static readonly TableDef FactMiracleEffect = new TableDef("fact_miracle_effect", 91, "run_key", "tick", "seq", "date_key", "intervention_id", "thing_kind_code",
            "thing_id", "field", "before_value", "after_value", "death", "cause_code");
        public static readonly TableDef FactResourceLedger = new TableDef("fact_resource_ledger", 92, "run_key", "tick", "seq", "date_key", "ledger_id", "intervention_id",
            "god_key", "direction", "ware_key", "quantity", "lot_id", "holder_kind_code", "holder_id", "location_key");
    }
}
