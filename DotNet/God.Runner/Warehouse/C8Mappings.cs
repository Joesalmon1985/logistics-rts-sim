using God.Sim.Orgs;

namespace God.Runner.Warehouse
{
    /// <summary>
    /// C8's warehouse mappers, Tier 2 welfare taken by C6 (the package index section 0; LEAD_RULINGS R49: the table lives in C6's
    /// <c>0018_organisations.sql</c>, 0020 stays reserved for banking). One row per <see cref="WelfareGranted"/>.
    /// </summary>
    public static class C8Mappings
    {
        /// <summary>The grain of each C8 table (written to <c>meta_table_grain</c>).</summary>
        public static readonly (string table, string grain, string events)[] Grains =
        {
            ("fact_welfare", "one row per welfare grant (a keeper's hand-over of rationed goods)", "WelfareGranted"),
        };

        public static void Register(EventMapRegistry r)
        {
            r.Register<WelfareGranted>("fact_welfare", "welfare", (c, e) =>
                c.Insert(Tables.FactWelfare, c.Tick, c.Seq, c.DateKey(), (long)e.Org, e.Keeper > 0 ? (object)c.Person(e.Keeper).Key : null,
                    e.Recipient > 0 ? (object)c.Person(e.Recipient).Key : null, e.Ware != God.Sim.Ware.None ? (object)(long)(int)e.Ware : null, (long)e.Count,
                    e.Request != 0 ? (object)(long)e.Request : null, (long)e.Control));
        }
    }
}
