using System.Collections.Generic;
using System.Linq;

namespace God.Sim.Tests
{
    /// <summary>
    /// F1.16: collects the divine events a sim emits (every catalogued event of the domains 60-66: Divine, Possession, Avatar,
    /// Miracle, Ledger, Commandment, Religion), on top of an <see cref="EventProbe"/>, and answers the counting questions the
    /// D packages' tests ask (spells paired, effects per intervention). OWNER: F1 (frozen).
    /// </summary>
    public sealed class DivineProbe
    {
        public readonly EventProbe Probe;

        public DivineProbe(GodSim sim) { Probe = new EventProbe(sim); }

        public void Step(int ticks = 1) => Probe.Step(ticks);

        public static bool IsDivine(SimEvent e) =>
            EventCatalog.TryOf(e.GetType(), out var info) && (int)info.Domain >= (int)TelemetryDomain.Divine && (int)info.Domain <= (int)TelemetryDomain.Religion;

        /// <summary>Every divine event so far, in emission order.</summary>
        public List<SimEvent> Events => Probe.Events.Where(IsDivine).ToList();

        public List<T> Of<T>() where T : SimEvent => Probe.Events.OfType<T>().ToList();

        public int Count(string eventName) => Events.Count(e => e.GetType().Name == eventName);

        /// <summary>The divine events carrying a public int field <c>Intervention</c> equal to <paramref name="intervention"/>.</summary>
        public List<SimEvent> OfIntervention(int intervention) =>
            Events.Where(e => e.GetType().GetField("Intervention") is System.Reflection.FieldInfo f && f.FieldType == typeof(int) && (int)f.GetValue(e) == intervention).ToList();
    }
}
