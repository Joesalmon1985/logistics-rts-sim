using System.Collections.Generic;
using System.Linq;
using God.Sim;

namespace God.Sim.Tests
{
    /// <summary>
    /// Collects every event a sim emits, as Full recording would see them (F0.16, frozen): the decision trace is switched
    /// on (a host setting; it changes no hash). Step the sim through the probe, or drain it yourself and call
    /// <see cref="Collect"/>. Optionally feeds a <see cref="GoodsTrace"/> as it goes.
    /// </summary>
    public sealed class EventProbe
    {
        public readonly GodSim Sim;
        public readonly List<SimEvent> Events = new List<SimEvent>();
        public GoodsTrace Trace;
        private readonly List<SimEvent> _buf = new List<SimEvent>();

        public EventProbe(GodSim sim, bool traceGoods = false)
        {
            Sim = sim;
            sim.TraceDecisions = true;
            if (traceGoods) Trace = GoodsTrace.Start(sim.State);
        }

        /// <summary>Steps the sim <paramref name="ticks"/> times, collecting every event.</summary>
        public void Step(int ticks = 1)
        {
            for (int i = 0; i < ticks; i++) { Sim.Step(); Collect(); }
        }

        /// <summary>Drains the sim's events into the probe.</summary>
        public void Collect()
        {
            _buf.Clear();
            Sim.DrainInto(_buf);
            Events.AddRange(_buf);
            Trace?.AddRange(_buf);
        }

        public IEnumerable<T> Of<T>() where T : SimEvent => Events.OfType<T>();
        public void Clear() => Events.Clear();
    }
}
