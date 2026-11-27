using System.Linq;
using NUnit.Framework;

namespace God.Sim.Tests
{
    public class GodSimTests
    {
        private sealed class Ping : Command { public int N; }

        private sealed class PingHandler : CommandHandler<Ping>
        {
            public int Total;
            protected override CommandResult Validate(GodState s, Ping c) =>
                c.N > 0 ? CommandResult.Accept() : CommandResult.RejectDefault(Reasons.InvalidSite);
            protected override CommandResult Apply(GodState s, Ping c, EventBuffer ev) { Total += c.N; return CommandResult.Accept(); }
        }

        private sealed class CountSystem : ISimSystem
        {
            public int Ticks;
            public void Tick(GodState s, EventBuffer ev) { Ticks++; }
        }

        [Test]
        public void Create_DealsTheIsland_FromTheSeed()
        {
            var sim = GodSim.Create(11);
            Assert.AreEqual(30, sim.State.Board.Topology.HexCount);
            CollectionAssert.AreEqual(BoardGenerator.CreateIslandBoard(11).HexTerrain, sim.State.Board.HexTerrain);
        }

        [Test]
        public void Step_AdvancesTick_AndRunsSystems()
        {
            var sim = GodSim.Create(1);
            var sys = new CountSystem();
            sim.AddSystem(10, sys);
            sim.Step(25);
            Assert.AreEqual(25, sim.State.Tick);
            Assert.AreEqual(25, sys.Ticks);
        }

        [Test]
        public void Commands_AreValidatedNow_AndAppliedAtTheNextTick()
        {
            var sim = GodSim.Create(1);
            var h = new PingHandler();
            sim.RegisterHandler(h);
            Assert.IsFalse(sim.Submit(new Ping { N = 0 }).Ok);
            Assert.IsTrue(sim.Submit(new Ping { N = 3 }).Ok);
            Assert.AreEqual(0, h.Total, "not applied before the tick boundary");
            sim.Step();
            Assert.AreEqual(3, h.Total);
            Assert.IsTrue(sim.DrainEvents().OfType<CommandApplied>().Any());
        }

        [Test]
        public void UnknownCommand_IsRefused()
        {
            var sim = GodSim.Create(1);
            Assert.IsFalse(sim.Submit(new Ping { N = 1 }).Ok);
        }

        [Test]
        public void Systems_RunInOrder_ThenRegistrationOrder()
        {
            var sim = GodSim.Create(1);
            var log = new System.Collections.Generic.List<string>();
            sim.AddSystem(20, new Logger(log, "b"));
            sim.AddSystem(10, new Logger(log, "a"));
            sim.AddSystem(20, new Logger(log, "c"));
            sim.Step();
            CollectionAssert.AreEqual(new[] { "a", "b", "c" }, log);
        }

        private sealed class Logger : ISimSystem
        {
            private readonly System.Collections.Generic.List<string> _log;
            private readonly string _name;
            public Logger(System.Collections.Generic.List<string> log, string name) { _log = log; _name = name; }
            public void Tick(GodState s, EventBuffer ev) { _log.Add(_name); }
        }
    }
}
