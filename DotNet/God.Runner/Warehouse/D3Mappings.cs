using God.Sim.Divinity;

namespace God.Runner.Warehouse
{
    /// <summary>
    /// D3's warehouse mappers (OWNER: D3), called by F1's <c>DivinityMappings.Register</c>. <c>fact_miracle</c> one row per
    /// <c>MiracleCast</c>; <c>fact_miracle_effect</c> one per <c>MiracleEffectApplied</c> (before and after, the divine attribution
    /// of a death); <c>fact_resource_ledger</c> (F1's 0007) one per <c>ResourceCreated</c> (direction 1) or <c>ResourceDestroyed</c> (-1),
    /// so per ware the sums equal the goods ledger's miracle slots (C7, scenario 17). <c>LocationConsecrated</c> stays generic.
    /// </summary>
    public static class D3Mappings
    {
        /// <summary>The grain of each D3 table (written to <c>meta_table_grain</c>).</summary>
        public static readonly (string table, string grain, string events)[] Grains =
        {
            ("fact_miracle", "one row per accepted miracle (InterventionId)", "MiracleCast"),
            ("fact_miracle_effect", "one row per thing a miracle changed, with before and after", "MiracleEffectApplied"),
            ("fact_resource_ledger", "one row per miracle creation (direction 1) or destruction (-1) of goods", "ResourceCreated, ResourceDestroyed"),
        };

        public static void Register(EventMapRegistry r)
        {
            r.Register<MiracleCast>("fact_miracle", "miracle", (c, e) =>
                c.Insert(Tables.FactMiracle, c.Tick, c.Seq, c.DateKey(), (long)e.Intervention, c.God(e.God), (long)e.Kind, (long)e.TargetKind, (long)e.TargetId,
                    (long)e.TargetNode, (long)e.TargetRadius, (long)e.Magnitude, e.Cost));
            r.Register<MiracleEffectApplied>("fact_miracle_effect", "miracle_effect", (c, e) =>
                c.Insert(Tables.FactMiracleEffect, c.Tick, c.Seq, c.DateKey(), (long)e.Intervention, (long)e.ThingKind, (long)e.ThingId, e.Field, e.Before, e.After,
                    e.Death ? 1L : 0L, e.Death ? (object)(long)(int)e.Cause : null));
            r.Register<ResourceCreated>("fact_resource_ledger", "resource_ledger", (c, e) =>
                c.Insert(Tables.FactResourceLedger, c.Tick, c.Seq, c.DateKey(), (long)e.LedgerId, (long)e.Intervention, c.God(e.God), 1L, (long)(int)e.Ware, (long)e.Count,
                    (long)e.Lot, (long)(int)e.HolderKind, (long)e.HolderId, e.Node >= 0 ? (object)c.NodeLocation(e.Node) : null));
            r.Register<ResourceDestroyed>("fact_resource_ledger", "resource_ledger", (c, e) =>
                c.Insert(Tables.FactResourceLedger, c.Tick, c.Seq, c.DateKey(), (long)e.LedgerId, (long)e.Intervention, c.God(e.God), -1L, (long)(int)e.Ware, (long)e.Count,
                    (long)e.Lot, (long)(int)e.HolderKind, (long)e.HolderId, e.Node >= 0 ? (object)c.NodeLocation(e.Node) : null));
        }
    }
}
