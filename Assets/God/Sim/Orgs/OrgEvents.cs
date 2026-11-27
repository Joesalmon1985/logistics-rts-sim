using System.Collections.Generic;

namespace God.Sim.Orgs
{
    /// <summary>
    /// F1.12 (contracts 15.1): the Orgs rows (codes 355-374 and 500-599: C6 500-549 under LEAD_RULINGS R23/R36).
    /// <see cref="Catalog"/> is the concatenation of the per-package arrays in package order, appended to <c>EventCatalog.All</c>.
    /// Each package adds rows only to its own array, in its own file (<c>OrgEvents.&lt;Pkg&gt;.cs</c>); all are empty in F1.
    /// OWNER: F1 (this file frozen); each array to its package.
    /// </summary>
    public static partial class OrgEvents
    {
        public static readonly ReadOnlyArray<EventInfo> Catalog = Concat(C6, C7, C8, C9, C10);

        private static ReadOnlyArray<EventInfo> Concat(params ReadOnlyArray<EventInfo>[] parts)
        {
            var l = new List<EventInfo>();
            foreach (var p in parts) foreach (var e in p) l.Add(e);
            return new ReadOnlyArray<EventInfo>(l);
        }
    }
}
