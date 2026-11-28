namespace God.Runner.Warehouse
{
    /// <summary>
    /// UN6's warehouse mappers (OWNER: UN6; UN11a completes), called by UN0's <c>UnifiedMappings.Register</c>. Still a stub: events
    /// 1320-1323 map to the generic <c>fact_event</c> until UN11a lands migration <c>0046_unification_brain.sql</c>. Intended tables:
    /// <c>dim_brain_artifact</c> (one row per game: kind, artifact_version, weights_sha256, label; from <c>BrainAttached</c>) and
    /// <c>fact_knowledge</c> (grain: one row per acquisition or observed demonstration: tick, npc / observer, actor_from, what
    /// (design, recipe, interaction), index, source (own, observed, taught, start), distance, chance_permille, learned, taught, origin;
    /// <c>TeachingGesture</c> adds act and until). These are analytics of beliefs and knowledge, never world facts (QA-02).
    /// </summary>
    public static class UN6Mappings
    {
        /// <summary>The grain of each UN6 table (written to <c>meta_table_grain</c>).</summary>
        public static readonly (string table, string grain, string events)[] Grains = { };

        public static void Register(EventMapRegistry r)
        {
        }
    }
}
