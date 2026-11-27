using God.Sim.Divinity;

namespace God.Runner.Warehouse
{
    /// <summary>
    /// Package D6's warehouse mappers (OWNER: D6), called by F1's <c>DivinityMappings.Register</c>: commandments, revisions and
    /// revocations, people's knowledge events (learned, stance, forgotten), sampled choices, religions, affiliation spells and
    /// sampled belief changes. The truth field <c>VariantMask</c> is written at Full only.
    /// </summary>
    public static class D6Mappings
    {
        /// <summary>The grain of each D6 table (written to <c>meta_table_grain</c>).</summary>
        public static readonly (string table, string grain, string events)[] Grains =
        {
            ("fact_commandment", "one row per revealed commandment (CommandmentId)", "CommandmentIssued"),
            ("fact_commandment_revision", "one row per revision (kind 1) or revocation (kind 2)", "CommandmentRevised, CommandmentRevoked"),
            ("fact_commandment_knowledge", "one row per knowledge event: learned 1, stance changed 2, forgotten 3", "CommandmentLearned, CommandmentStanceChanged, CommandmentForgotten"),
            ("fact_commandment_choice", "one row per sampled choice a held rule weighed", "CommandmentChoiceMade"),
            ("fact_religion", "one row per religion", "ReligionFounded"),
            ("fact_affiliation", "one row per affiliation spell change: opened 1, closed 0", "AffiliationChanged"),
            ("fact_god_belief", "one row per sampled change of a person's beliefs about a god", "BeliefChanged"),
        };

        private static object P(MapContext c, int npc) => npc > 0 ? (object)c.Person(npc).Key : null;

        public static void Register(EventMapRegistry r)
        {
            r.Register<CommandmentIssued>("fact_commandment", "commandment", (c, e) =>
                c.Insert(Tables.FactCommandment, c.Tick, c.Seq, c.DateKey(), (long)e.Intervention, (long)e.Commandment, c.God(e.God), (long)e.Template, (long)e.Importance,
                    (long)e.Rules, (long)e.TargetKind, (long)e.TargetId, (long)e.TargetNode, (long)e.TargetRadius, (long)e.Recipients));
            r.Register<CommandmentRevised>("fact_commandment_revision", "commandment_revision", (c, e) =>
                c.Insert(Tables.FactCommandmentRevision, c.Tick, c.Seq, c.DateKey(), (long)e.Intervention, (long)e.Commandment, c.God(e.God), 1L, (long)e.Revision,
                    (long)e.Importance, (long)e.Rules, (long)e.Recipients));
            r.Register<CommandmentRevoked>("fact_commandment_revision", "commandment_revision", (c, e) =>
                c.Insert(Tables.FactCommandmentRevision, c.Tick, c.Seq, c.DateKey(), (long)e.Intervention, (long)e.Commandment, c.God(e.God), 2L, (long)e.Revision,
                    null, null, (long)e.Recipients));
            r.Register<CommandmentLearned>("fact_commandment_knowledge", "commandment_knowledge", (c, e) =>
                c.Insert(Tables.FactCommandmentKnowledge, c.Tick, c.Seq, c.DateKey(), P(c, e.Npc), (long)e.Commandment, 1L, (long)e.Revision, (long)e.Source,
                    P(c, e.FromNpc), e.Intervention != 0 ? (object)(long)e.Intervention : null, (long)e.Salience, (long)e.Adoption, null, null, null, e.Update ? 1L : 0L, null,
                    c.Full ? (object)(long)e.VariantMask : null));
            r.Register<CommandmentStanceChanged>("fact_commandment_knowledge", "commandment_knowledge", (c, e) =>
                c.Insert(Tables.FactCommandmentKnowledge, c.Tick, c.Seq, c.DateKey(), P(c, e.Npc), (long)e.Commandment, 2L, null, null, null, null, (long)e.Salience,
                    (long)e.Adoption, (long)e.From, (long)e.To, (long)e.Cause, null, null, null));
            r.Register<CommandmentForgotten>("fact_commandment_knowledge", "commandment_knowledge", (c, e) =>
                c.Insert(Tables.FactCommandmentKnowledge, c.Tick, c.Seq, c.DateKey(), P(c, e.Npc), (long)e.Commandment, 3L, (long)e.Revision, null, null, null, null, null,
                    null, null, null, null, e.Evicted ? 1L : 0L, null));
            r.Register<CommandmentChoiceMade>("fact_commandment_choice", "commandment_choice", (c, e) =>
            {
                if (!c.RowWanted) return;
                c.Insert(Tables.FactCommandmentChoice, c.Tick, c.Seq, c.DateKey(), P(c, e.Npc), (long)e.Commandment, (long)e.Modality, (long)e.Verb,
                    e.TargetId != 0 ? (object)(long)e.TargetId : null, (long)e.Term);
            });
            r.Register<ReligionFounded>("fact_religion", "religion", (c, e) =>
                c.Insert(Tables.FactReligion, c.Tick, c.Seq, c.DateKey(), (long)e.Religion, c.God(e.God), e.Parent != 0 ? (object)(long)e.Parent : null));
            r.Register<AffiliationChanged>("fact_affiliation", "affiliation", (c, e) =>
                c.Insert(Tables.FactAffiliation, c.Tick, c.Seq, c.DateKey(), P(c, e.Npc), (long)e.Religion, c.God(e.God), e.Opened ? 1L : 0L, (long)e.Reason,
                    (long)e.Community, (long)e.Citizenship));
            r.Register<BeliefChanged>("fact_god_belief", "god_belief", (c, e) =>
            {
                if (!c.RowWanted) return;
                c.Insert(Tables.FactGodBelief, c.Tick, c.Seq, c.DateKey(), P(c, e.Npc), c.God(e.God), (long)e.Cause, e.CauseRef != 0 ? (object)(long)e.CauseRef : null,
                    (long)e.DBelief, (long)e.DFaith, (long)e.DTrust, (long)e.DFear, (long)e.DLoyalty, (long)e.Belief, (long)e.Faith, (long)e.Trust, (long)e.Fear, (long)e.Loyalty);
            });
        }
    }
}
