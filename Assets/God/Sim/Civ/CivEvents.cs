using System.Collections.Generic;

namespace God.Sim.Civ
{
    /// <summary>
    /// F1.12 (contracts 15.1): the Civ rows (codes 250-299, 320-329 and 400-429, plus C2's lending 540-549 in domain Lending).
    /// <see cref="Catalog"/> is the concatenation of the per-package arrays in package order, appended to <c>EventCatalog.All</c>.
    /// Each package adds rows only to its own array, in its own file (<c>CivEvents.&lt;Pkg&gt;.cs</c>); all are empty in F1.
    /// OWNER: F1 (this file frozen); each array to its package.
    /// </summary>
    public static partial class CivEvents
    {
        public static readonly ReadOnlyArray<EventInfo> Catalog = Concat(C1, C2, C3, C4, C5);

        private static ReadOnlyArray<EventInfo> Concat(params ReadOnlyArray<EventInfo>[] parts)
        {
            var l = new List<EventInfo>();
            foreach (var p in parts) foreach (var e in p) l.Add(e);
            return new ReadOnlyArray<EventInfo>(l);
        }
    }
}
