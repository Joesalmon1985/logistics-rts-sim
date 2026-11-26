using System.Linq;
using God.Sim;
using God.Sim.Population;
using God.Sim.Tests;
using NUnit.Framework;

namespace God.Runner.Tests
{
    /// <summary>
    /// F0 gate item 4 (SHARED_CONTRACTS 2.4, A1): on the owner's island (band_6x20, six factions of 20 founders), goods are
    /// conserved for 30 days: C1, C2, C4 and C5 at every day boundary (the state invariants), C3 (every lot id accounted
    /// for by the events) and C6 (every person-initiated move within reach) at the end; and they still hold after a save
    /// and load on day 15, where the loaded game goes on exactly as the unsaved one.
    /// </summary>
    public class F0GoodsConservationTests
    {
        [Test]
        public void Band6x20_ThirtyDays_C1ToC6_HoldEveryDay_AndAfterSaveAndLoad()
        {
            var setup = new GameSetup(1, GameSetup.NoHuman, 6) { Population = true, StartNpcsPerFaction = 20, Victory = VictorySpec.SandboxOnly() };
            var twin = SimBuilder.Create(setup).Sim;
            var built = SimBuilder.Create(setup);
            var probe = new EventProbe(built.Sim, traceGoods: true);
            built.Sim.TraceDecisions = false; // goods only: keeps the event list small (the trace changes no hash either way)
            int tpd = setup.TicksPerDay;
            for (int d = 1; d <= 30; d++)
            {
                probe.Step(tpd);
                twin.Step(tpd); twin.DrainEvents();
                probe.Events.RemoveAll(e => !(e is Produced || e is Consumed || e is ItemMoved || e is OwnershipChanged));
                var bad = SimInvariants.Check(probe.Sim.State);
                Assert.IsEmpty(bad, "day " + d + ": " + string.Join("; ", bad));
                if (d == 15)
                {
                    // save and load mid-run: the trace carries on over the loaded state
                    var json = SaveFile.From(probe.Sim).ToJson();
                    var loaded = SaveFile.FromJson(json);
                    Assert.AreEqual(SaveFile.CurrentVersion, loaded.Version);
                    var sim2 = SimBuilder.FromSave(loaded).Sim;
                    Assert.AreEqual(probe.Sim.StateHash(), sim2.StateHash(), "the loaded game hashes as the saved one");
                    Assert.IsEmpty(SimInvariants.Check(sim2.State), "invariants after load");
                    var trace = probe.Trace;
                    probe = new EventProbe(sim2) { Trace = trace };
                    sim2.TraceDecisions = false;
                    Assert.IsEmpty(trace.CheckC3(sim2.State), "C3 at the load");
                }
            }
            Assert.AreEqual(twin.StateHash(), probe.Sim.StateHash(), "after save and load the game goes on exactly as the unsaved twin");
            var c3 = probe.Trace.CheckC3(probe.Sim.State);
            Assert.IsEmpty(c3, string.Join("\n", c3.Take(20)));
            var c6 = probe.Trace.CheckC6(probe.Sim.State.Territory.Grid);
            Assert.IsEmpty(c6, string.Join("\n", c6.Take(20)));
            var p = probe.Sim.State.Population;
            TestContext.WriteLine("lots traced " + probe.Trace.Lots.Count + ", consumed " + probe.Trace.Lots.Values.Count(l => l.Ended == GoodsTrace.End.Consumed)
                + ", merged " + probe.Trace.Lots.Values.Count(l => l.Ended == GoodsTrace.End.Merged) + ", live " + probe.Trace.Lots.Values.Count(l => l.Ended == GoodsTrace.End.None)
                + "; moves " + probe.Trace.Events.OfType<ItemMoved>().Count() + "; deaths " + p.Stats.Deaths + "; next lot id " + p.NextLotId);
        }
    }
}
