using System.Collections.Generic;

namespace God.Sim.Unified
{
    /// <summary>
    /// UN0 (contracts 23.12): the unified rows of the event catalogue, codes 1200-1399, domains 80-88. <see cref="Catalog"/> is the
    /// concatenation of the per-package arrays in code order, appended to <c>EventCatalog.All</c>. Each package adds rows only to its
    /// own array, in its own file (<c>UnifiedEvents.UNn.cs</c>), with codes from its block; all are empty at UN0. Every unified action
    /// event carries its <see cref="ControlOrigin"/>. SAC 8 rules apply (codes never reused, a changed meaning is a new code, an added
    /// field bumps the version). OWNER: UN0 (this file frozen); each array to its package.
    /// <list type="table">
    /// <item>1200-1219 UN1 (1206-1207 UN5), domain UnifiedWorld</item><item>1220-1239 UN2, Motor</item>
    /// <item>1240-1259 UN4 (1250-1251 UN3b), Construction</item><item>1260-1279 UN3b, Interaction</item>
    /// <item>1280-1299 UN7, Exchange</item><item>1300-1319 UN8a, Conflict</item><item>1320-1339 UN6 (1320 UN6t), Brain</item>
    /// <item>1340-1359 UB1, Settlement</item><item>1360-1379 UN8b, Groups</item><item>1380-1399 reserved</item>
    /// </list>
    /// </summary>
    public static partial class UnifiedEvents
    {
        public const int First = 1200, Last = 1399;

        public static readonly ReadOnlyArray<EventInfo> Catalog = Concat(UN1, UN5, UN2, UN4, UN3b, UN7, UN8a, UN6, UN6t, UB1, UN8b);

        private static ReadOnlyArray<EventInfo> Concat(params ReadOnlyArray<EventInfo>[] parts)
        {
            var l = new List<EventInfo>();
            foreach (var p in parts) foreach (var e in p) l.Add(e);
            return new ReadOnlyArray<EventInfo>(l);
        }

        /// <summary>The code block a package owns (inclusive), for its registry test.</summary>
        public static (int lo, int hi) BlockOf(string package)
        {
            switch (package)
            {
                case "UN1": return (1200, 1219);
                case "UN5": return (1206, 1207);
                case "UN2": return (1220, 1239);
                case "UN4": return (1240, 1259);
                case "UN3b": return (1250, 1279);
                case "UN7": return (1280, 1299);
                case "UN8a": return (1300, 1319);
                case "UN6": return (1320, 1339);
                case "UN6t": return (1320, 1320);
                case "UB1": return (1340, 1359);
                case "UN8b": return (1360, 1379);
                default: return (0, -1);
            }
        }
    }
}
