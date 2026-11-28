namespace God.Runner.Warehouse
{
    /// <summary>
    /// UN2's warehouse mappers (OWNER: UN2; UN11a completes), called by UN0's <c>UnifiedMappings.Register</c>. Still a stub: events
    /// 1220-1224 map to the generic <c>fact_event</c> until UN11a lands migration <c>0044_unification_core.sql</c>. Intended table
    /// <c>fact_control</c> (grain: one row per control submission or completion failure): tick, seq, actor, control (code), origin,
    /// status, target_kind, target_id, ware; <c>ActionInterrupted</c> adds reason and ticks_done; <c>Equipped</c>/<c>Unequipped</c>
    /// (UN8a) actor, lot, ware, origin. <c>control_code</c> fills from the event; <c>origin</c> is a column on every unified fact (QA-01).
    /// </summary>
    public static class UN2Mappings
    {
        /// <summary>The grain of each UN2 table (written to <c>meta_table_grain</c>).</summary>
        public static readonly (string table, string grain, string events)[] Grains = { };

        public static void Register(EventMapRegistry r)
        {
        }
    }
}
