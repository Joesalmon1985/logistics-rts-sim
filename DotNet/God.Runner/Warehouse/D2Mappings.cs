using God.Sim.Divinity;

namespace God.Runner.Warehouse
{
    /// <summary>
    /// D2's warehouse mappers (OWNER: D2), called by F1's <c>DivinityMappings.Register</c>: suggestions and commands into
    /// <c>fact_divine_command</c>, relays into <c>fact_divine_command_target</c>, perceptions and witnessed signs into
    /// <c>fact_command_perception</c>, the one response per perceiver into <c>fact_command_response</c>, and each compulsion
    /// event into <c>fact_compulsion</c>. The truth flag <c>Mismatch</c> is written at Full only.
    /// </summary>
    public static class D2Mappings
    {
        /// <summary>The grain of each D2 table (written to <c>meta_table_grain</c>).</summary>
        public static readonly (string table, string grain, string events)[] Grains =
        {
            ("fact_divine_command", "one row per accepted suggestion or command (InterventionId)", "DivineSuggestionIssued, DivineCommandIssued"),
            ("fact_divine_command_target", "one row per recipient an organisation-scope instruction was relayed to", "InstructionRelayed"),
            ("fact_command_perception", "one row per perceiving recipient (kind 1) or witness of a command's sign (kind 2)", "CommandPerceived, DivineSignPerceived"),
            ("fact_command_response", "exactly one row per perceiver", "CommandResponded"),
            ("fact_compulsion", "one row per compulsion event: started 1, ended 2, blocked 3", "CompulsionStarted, CompulsionEnded, CompulsionBlocked"),
        };

        private static object P(MapContext c, int npc) => npc > 0 ? (object)c.Person(npc).Key : null;

        public static void Register(EventMapRegistry r)
        {
            r.Register<DivineSuggestionIssued>("fact_divine_command", "divine_command", (c, e) =>
                c.Insert(Tables.FactDivineCommand, c.Tick, c.Seq, c.DateKey(), (long)e.Intervention, c.God(e.God), 1L, (long)e.Act, (long)e.TargetKind, (long)e.TargetId,
                    (long)e.TargetNode, (long)e.TargetRadius, (long)e.Scope, (long)e.Recipients, (long)e.NotReached, e.Expires));
            r.Register<DivineCommandIssued>("fact_divine_command", "divine_command", (c, e) =>
                c.Insert(Tables.FactDivineCommand, c.Tick, c.Seq, c.DateKey(), (long)e.Intervention, c.God(e.God), 2L, (long)e.Act, (long)e.TargetKind, (long)e.TargetId,
                    (long)e.TargetNode, (long)e.TargetRadius, (long)e.Scope, (long)e.Recipients, (long)e.NotReached, e.Expires));
            r.Register<InstructionRelayed>("fact_divine_command_target", "divine_command_target", (c, e) =>
                c.Insert(Tables.FactDivineCommandTarget, c.Tick, c.Seq, c.DateKey(), (long)e.Intervention, (long)e.Org, P(c, e.Npc)));
            r.Register<CommandPerceived>("fact_command_perception", "command_perception", (c, e) =>
            {
                if (!c.RowWanted) return;
                c.Insert(Tables.FactCommandPerception, c.Tick, c.Seq, c.DateKey(), (long)e.Intervention, 1L, P(c, e.Npc), P(c, e.Npc), (long)e.Kind, (long)e.DecodedAct,
                    (long)e.TopicKind, (long)e.TopicId, (long)e.Clarity, e.OnWaking ? 1L : 0L, e.Evicted != 0 ? (object)(long)e.Evicted : null, null,
                    c.Full ? (object)(e.Mismatch ? 1L : 0L) : null);
            });
            r.Register<DivineSignPerceived>("fact_command_perception", "command_perception", (c, e) =>
            {
                if (!c.RowWanted) return;
                c.Insert(Tables.FactCommandPerception, c.Tick, c.Seq, c.DateKey(), (long)e.Intervention, 2L, P(c, e.Witness), P(c, e.Recipient), (long)InterventionKind.Command,
                    null, null, null, null, 0L, null, c.NodeLocation(e.Node), null);
            });
            r.Register<CommandResponded>("fact_command_response", "command_response", (c, e) =>
                c.Insert(Tables.FactCommandResponse, c.Tick, c.Seq, c.DateKey(), (long)e.Intervention, P(c, e.Npc), c.God(e.God), (long)e.Kind, (long)e.Response,
                    e.Verb != 0 ? (object)(long)e.Verb : null, (long)e.Divine));
            r.Register<CompulsionStarted>("fact_compulsion", "compulsion", (c, e) =>
                c.Insert(Tables.FactCompulsion, c.Tick, c.Seq, c.DateKey(), (long)e.Intervention, c.God(e.God), P(c, e.Npc), 1L, (long)e.Act, (long)e.TargetKind, (long)e.TargetId,
                    (long)e.TargetNode, (long)e.TargetRadius, e.Expires, null, null, null));
            r.Register<CompulsionEnded>("fact_compulsion", "compulsion", (c, e) =>
                c.Insert(Tables.FactCompulsion, c.Tick, c.Seq, c.DateKey(), (long)e.Intervention, c.God(e.God), P(c, e.Npc), 2L, null, null, null, null, null, null,
                    (long)e.Reason, e.Started ? 1L : 0L, e.SinceTick));
            r.Register<CompulsionBlocked>("fact_compulsion", "compulsion", (c, e) =>
                c.Insert(Tables.FactCompulsion, c.Tick, c.Seq, c.DateKey(), (long)e.Intervention, c.God(e.God), P(c, e.Npc), 3L, (long)e.Act, null, null, null, null, null,
                    null, null, null));
        }
    }
}
