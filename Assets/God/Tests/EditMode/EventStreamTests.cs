using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>M0.14 (DATA_WAREHOUSE 10.1): the domain event stream: catalogue, codes, sequence numbers, snapshots.</summary>
    public class EventStreamTests
    {
        [Test]
        public void EveryEventType_IsADomainEvent_InTheCatalogue_WithAUniqueCodeInItsRange()
        {
            var types = typeof(GodSim).Assembly.GetTypes().Where(t => typeof(SimEvent).IsAssignableFrom(t) && !t.IsAbstract).ToList();
            Assert.Greater(types.Count, 30);
            foreach (var t in types)
            {
                Assert.IsTrue(typeof(DomainEvent).IsAssignableFrom(t), t.Name + " is a DomainEvent");
                Assert.IsTrue(EventCatalog.TryOf(t, out var info), t.Name + " is in the EventCatalog");
                Assert.IsFalse(string.IsNullOrEmpty(info.Table), t.Name + " maps to a table");
                if (info.Table == "analysis-none") Assert.IsFalse(string.IsNullOrEmpty(info.Note), t.Name + ": analysis-none needs a reason");
                Assert.GreaterOrEqual(info.Version, 1);
                // Stage 1-3 events take codes 1-99; the events of stage 4 modules (M3 on) lie in their module's range (DATA_WAREHOUSE 7).
                bool stage3 = t.Namespace == "God.Sim";
                var (lo, hi) = EventCatalog.RangeOf(info.Domain, stage3);
                Assert.That(info.Code, Is.InRange(lo, hi), t.Name);
            }
            Assert.AreEqual(types.Count, EventCatalog.All.Count, "no catalogue row without a type");
            var codes = EventCatalog.All.Select(e => e.Code).ToList();
            Assert.AreEqual(codes.Count, codes.Distinct().Count(), "codes are unique");
            var names = EventCatalog.All.Select(e => e.Name).ToList();
            Assert.AreEqual(names.Count, names.Distinct().Count(), "names are unique");
            Assert.AreEqual(14, new BuildingPlaced().Code);
            Assert.AreEqual("BuildingPlaced", new BuildingPlaced().Name);
        }

        [Test]
        public void EventBufferAdd_StampsTickAndSeq_ResetEveryTick()
        {
            var b = new EventBuffer { CurrentTick = 5 };
            var a1 = new DiceRolled(); var a2 = new PileWasted(); var a3 = new PileAdded();
            b.Add(a1); b.Add(a2); b.Add(a3);
            Assert.AreEqual(new[] { 0, 1, 2 }, new[] { a1.Seq, a2.Seq, a3.Seq });
            Assert.AreEqual(5, a3.Tick);
            b.CurrentTick = 6;
            var b1 = new DiceRolled(); b.Add(b1);
            Assert.AreEqual(0, b1.Seq);
            var into = new List<SimEvent>();
            b.DrainInto(into);
            Assert.AreEqual(4, into.Count);
            Assert.AreEqual(0, b.Count);
        }

        [Test]
        public void ARealGame_GivesContiguousSeqs_CommandPhaseFirst()
        {
            var sim = GodSim.Create(GameSetup.Watch(8));
            AiDirector.Attach(sim, AiDirector.NonHuman(sim.State), AiTuning.Default, new AiTrader());
            var all = new List<SimEvent>();
            for (int i = 0; i < 3000; i++) { sim.Step(); sim.DrainInto(all); }
            Assert.Greater(all.Count, 100);
            foreach (var g in all.GroupBy(e => e.Tick))
            {
                var seqs = g.Select(e => e.Seq).ToList();
                CollectionAssert.AreEqual(Enumerable.Range(0, seqs.Count).ToList(), seqs, "tick " + g.Key);
                // The command phase comes first: each command's own effects (a BuildingPlaced) and then its
                // CommandApplied or CommandRejected marker, in submission order; the systems' events follow.
                bool seenSystem = false;
                foreach (var e in g)
                {
                    bool cmd = e is CommandApplied || e is CommandRejected;
                    if (e is DiceRolled || e is PileAdded || e is WaresProduced || e is BuildingCompleted || e is UnitProduced || e is UnitFired) seenSystem = true;
                    else if (cmd) Assert.IsFalse(seenSystem, "commands come before the systems in tick " + g.Key);
                }
            }
            Assert.IsTrue(all.OfType<CommandApplied>().Any(), "the brains' commands are in the stream");
            Assert.IsTrue(all.OfType<BuildingPlaced>().Any() && all.OfType<BuildingCompleted>().Any(), "buildings placed and finished");
            Assert.IsTrue(all.OfType<WaresProduced>().Any(), "production");
        }

        [Test]
        public void SnapshotBuilders_AreReadOnly()
        {
            // M0.3 in miniature (the runner test runs 5,000 ticks): calling the builder every tick changes nothing.
            ulong Run(bool snap)
            {
                var sim = GodSim.Create(GameSetup.Watch(9));
                AiDirector.Attach(sim, AiDirector.NonHuman(sim.State), AiTuning.Default, new AiTrader());
                for (int i = 0; i < 2000; i++) { sim.Step(); sim.DrainEvents(); if (snap) SnapshotBuilder.FactionDay(sim.State); }
                return sim.StateHash();
            }
            Assert.AreEqual(Run(false), Run(true));
        }

        [Test]
        public void FactionDay_DescribesEachFaction()
        {
            var sim = GodSim.Create(new GameSetup(3UL, GameSetup.NoHuman, 4));
            sim.Step(1200);
            var rows = SnapshotBuilder.FactionDay(sim.State);
            Assert.AreEqual(4, rows.Length);
            Assert.AreEqual(0, rows[0].Day, "the day that just ended");
            foreach (var r in rows)
            {
                Assert.AreEqual(1, r.Hqs); Assert.AreEqual(1, r.VictoryPoints); Assert.IsFalse(r.Eliminated);
                Assert.AreEqual(r.Stock.Sum(), r.StockTotal);
                Assert.Greater(r.StockTotal, 0);
            }
        }
    }
}
