using System;
using System.Collections.Generic;
using System.Reflection;

namespace God.Sim.Tests
{
    /// <summary>
    /// F1.16 (contracts 5, invariant M1): reconstructs every god's Mythic balance from the event stream alone:
    /// <c>initial + Σ credited − Σ spent − Σ upkeep charged = balance</c>. It reads the D1 events by name
    /// (<c>MythicCredited</c>, <c>MythicSpent</c>, <c>MythicUpkeepCharged</c>), each with public fields <c>int God</c> and
    /// <c>long Amount</c> (milli-Mythic); D1 names its fields so (or amends this helper). Nothing in the sim reads it.
    /// OWNER: F1 (frozen).
    /// </summary>
    public sealed class MythicTrace
    {
        public const string Credited = "MythicCredited", Spent = "MythicSpent", Upkeep = "MythicUpkeepCharged";

        private readonly SortedDictionary<int, long> _balance = new SortedDictionary<int, long>();
        public long Credits, Spends, Upkeeps;

        /// <summary>Starts every god at its initial balance (milli-Mythic).</summary>
        public static MythicTrace Start(IEnumerable<(int god, long initial)> gods)
        {
            var t = new MythicTrace();
            foreach (var (g, v) in gods) t._balance[g] = v;
            return t;
        }

        public void AddRange(IEnumerable<SimEvent> events) { foreach (var e in events) Add(e); }

        public void Add(SimEvent e)
        {
            string n = e.GetType().Name;
            int sign = n == Credited ? 1 : n == Spent || n == Upkeep ? -1 : 0;
            if (sign == 0) return;
            int god = (int)Field(e, "God");
            long amount = Convert.ToInt64(Field(e, "Amount"));
            _balance.TryGetValue(god, out long b);
            _balance[god] = b + sign * amount;
            if (n == Credited) Credits += amount; else if (n == Spent) Spends += amount; else Upkeeps += amount;
        }

        /// <summary>The balance the events imply for a god.</summary>
        public long Balance(int god) => _balance.TryGetValue(god, out long b) ? b : 0;

        public IEnumerable<int> Gods => _balance.Keys;

        private static object Field(SimEvent e, string name)
        {
            var f = e.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance) ?? throw new InvalidOperationException(e.GetType().Name + " has no field " + name);
            return f.GetValue(e);
        }
    }
}
