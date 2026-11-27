using God.Sim.Divinity;

namespace God.Runner.Warehouse
{
    /// <summary>
    /// D5's warehouse mappers (OWNER: D4, holding D5's slot), called by F1's <c>DivinityMappings.Register</c>: every avatar event
    /// into <c>fact_avatar_manifestation</c> (manifested 1, dismissed 2, destroyed 3, damaged 4, act 5) with <c>control_code</c>
    /// 3 (avatar). The avatar body never gets a <c>dim_person</c> row: its id is written as <c>body_id</c>.
    /// </summary>
    public static class D5Mappings
    {
        /// <summary>The grain of each D5 table (written to <c>meta_table_grain</c>).</summary>
        public static readonly (string table, string grain, string events)[] Grains =
        {
            ("fact_avatar_manifestation", "one row per avatar event: manifested 1, dismissed 2, destroyed 3, damaged 4, act started 5 (high volume)",
                "AvatarManifested, AvatarDismissed, AvatarDestroyed, AvatarDamaged, AvatarActionStarted"),
        };

        private static object P(MapContext c, int npc) => npc > 0 && !DivinityIds.IsAvatarBodyId(npc) ? (object)c.Person(npc).Key : null;
        private static object Id(int v) => v != 0 ? (object)(long)v : null;
        private const long Avatar = (long)ControlKind.AvatarBody;

        public static void Register(EventMapRegistry r)
        {
            r.Register<AvatarManifested>("fact_avatar_manifestation", "avatar", (c, e) =>
                c.Insert(Tables.FactAvatarManifestation, c.Tick, c.Seq, c.DateKey(), (long)e.Intervention, c.God(e.God), (long)e.Body, 1L, c.NodeLocation(e.Node),
                    (long)e.Health, e.Created ? 1L : 0L, Id(e.SupersededPossession), null, null, null, null, null, null, null, null, null, null, null, null,
                    null, null, null, null, null, null, Avatar));
            r.Register<AvatarDismissed>("fact_avatar_manifestation", "avatar", (c, e) =>
                c.Insert(Tables.FactAvatarManifestation, c.Tick, c.Seq, c.DateKey(), (long)e.Intervention, c.God(e.God), (long)e.Body, 2L, c.NodeLocation(e.Node),
                    null, null, null, (long)e.Reason, e.StartTick, e.Ticks, (long)e.LotsDropped, (long)e.Actions, (long)e.Observers, (long)e.DamageTaken,
                    null, null, null, null, null, null, null, null, null, null, null, Avatar));
            r.Register<AvatarDestroyed>("fact_avatar_manifestation", "avatar", (c, e) =>
                c.Insert(Tables.FactAvatarManifestation, c.Tick, c.Seq, c.DateKey(), (long)e.Intervention, c.God(e.God), (long)e.Body, 3L, c.NodeLocation(e.Node),
                    0L, null, null, (long)ControlEndReason.Destroyed, e.StartTick, e.Ticks, (long)e.LotsDropped, (long)e.Actions, (long)e.Observers, (long)e.DamageTaken,
                    P(c, e.By), Id(e.ByIntervention), (long)e.Cause, e.CooldownUntil, null, null, null, null, null, null, null, Avatar));
            r.Register<AvatarDamaged>("fact_avatar_manifestation", "avatar", (c, e) =>
                c.Insert(Tables.FactAvatarManifestation, c.Tick, c.Seq, c.DateKey(), (long)e.Intervention, c.God(e.God), (long)e.Body, 4L, null,
                    (long)e.HealthAfter, null, null, null, null, null, null, null, null, null,
                    P(c, e.By), Id(e.ByIntervention), (long)e.Cause, null, (long)e.Amount, null, null, null, null, null, null, Avatar));
            r.Register<AvatarActionStarted>("fact_avatar_manifestation", "avatar", (c, e) =>
            {
                if (!c.RowWanted) return;
                c.Insert(Tables.FactAvatarManifestation, c.Tick, c.Seq, c.DateKey(), (long)e.Intervention, null, (long)e.Body, 5L, null,
                    null, null, null, null, null, null, null, null, (long)e.Observers, null, null, null, null, null, null,
                    (long)e.Verb, (long)e.TargetKind, (long)e.TargetId, (long)e.TargetNode, e.ActionTick, (long)e.Attending, Avatar);
            });
        }
    }
}
