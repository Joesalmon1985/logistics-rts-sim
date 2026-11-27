using System.Collections.Generic;

namespace God.Sim.Divinity
{
    /// <summary>
    /// F1.12 (contracts 15.1): the Divinity rows (codes 800-899).
    /// <see cref="Catalog"/> is the concatenation of the per-package arrays in package order, appended to <c>EventCatalog.All</c>.
    /// Each package adds rows only to its own array, in its own file (<c>DivinityEvents.&lt;Pkg&gt;.cs</c>); all are empty in F1.
    /// OWNER: F1 (this file frozen); each array to its package.
    /// </summary>
    public static partial class DivinityEvents
    {
        public static readonly ReadOnlyArray<EventInfo> Catalog = Concat(D1, D2, D3, D4, D5, D6, D7);

        private static ReadOnlyArray<EventInfo> Concat(params ReadOnlyArray<EventInfo>[] parts)
        {
            var l = new List<EventInfo>();
            foreach (var p in parts) foreach (var e in p) l.Add(e);
            return new ReadOnlyArray<EventInfo>(l);
        }
    }
}
