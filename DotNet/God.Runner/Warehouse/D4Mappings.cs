using God.Sim.Divinity;

namespace God.Runner.Warehouse
{
    /// <summary>
    /// D4's warehouse mappers (OWNER: D4), called by F1's <c>DivinityMappings.Register</c>: every possession event into
    /// <c>fact_possession</c> (started 1, ended 2, act 3), with <c>control_code</c> 2 (possessed) from the event family.
    /// </summary>
    public static class D4Mappings
    {
        /// <summary>The grain of each D4 table (written to <c>meta_table_grain</c>).</summary>
        public static readonly (string table, string grain, string events)[] Grains =
        {
            ("fact_possession", "one row per possession event: started 1, ended 2, act started 3 (high volume)", "PossessionStarted, PossessionEnded, PossessedActionStarted"),
        };

        private static object P(MapContext c, int npc) => npc > 0 ? (object)c.Person(npc).Key : null;
        private static object Id(int v) => v != 0 ? (object)(long)v : null;
        private const long Possessed = (long)ControlKind.Possessed;

        public static void Register(EventMapRegistry r)
        {
            r.Register<PossessionStarted>("fact_possession", "possession", (c, e) =>
                c.Insert(Tables.FactPossession, c.Tick, c.Seq, c.DateKey(), (long)e.Intervention, c.God(e.God), P(c, e.Npc), 1L, c.NodeLocation(e.Node),
                    Id(e.SupersededCompulsion), Id(e.SupersededAvatar), null, null, null, null, null, null, null, null, null, null, null, Possessed));
            r.Register<PossessionEnded>("fact_possession", "possession", (c, e) =>
                c.Insert(Tables.FactPossession, c.Tick, c.Seq, c.DateKey(), (long)e.Intervention, c.God(e.God), P(c, e.Npc), 2L, c.NodeLocation(e.Node),
                    null, null, (long)e.Reason, e.StartTick, e.Ticks, (long)e.InputsApplied, (long)e.InputsRefused, (long)e.Actions, null, null, null, null, null, Possessed));
            r.Register<PossessedActionStarted>("fact_possession", "possession", (c, e) =>
            {
                if (!c.RowWanted) return;
                c.Insert(Tables.FactPossession, c.Tick, c.Seq, c.DateKey(), (long)e.Intervention, null, P(c, e.Npc), 3L, null,
                    null, null, null, null, null, null, null, null, (long)e.Verb, (long)e.TargetKind, (long)e.TargetId, (long)e.TargetNode, e.ActionTick, Possessed);
            });
        }
    }
}
