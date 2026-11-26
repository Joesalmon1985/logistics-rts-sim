using System.Collections.Generic;
using System.Linq;
using God.Sim;
using God.Sim.Comms;
using God.Sim.Population;
using God.Sim.Social;
using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>M5 on the owner's island (6 factions x 20 founders): what the social layer does in a few days, and its invariants.</summary>
    public class SocialIslandTests
    {
        private static GameSetup Setup(ulong seed, int perFaction = 20, int factions = 6) =>
            new GameSetup(seed, GameSetup.NoHuman, factions) { Population = true, StartNpcsPerFaction = perFaction, Victory = VictorySpec.SandboxOnly() };

        [Test]
        public void Island_ThreeDays_SocialEventsHappen_InvariantsHold()
        {
            var sim = GodSim.Create(Setup(3));
            var probe = new EventProbe(sim);
            for (int d = 0; d < 3; d++)
            {
                probe.Step(1200);
                Assert.IsEmpty(SimInvariants.Check(sim.State), "day " + d);
            }
            var counts = probe.Events.GroupBy(e => e.GetType().Name).ToDictionary(g => g.Key, g => g.Count());
            foreach (var kv in counts.OrderBy(k => k.Key)) TestContext.WriteLine(kv.Key + " " + kv.Value);
            var acts = probe.Events.OfType<GestureMade>().GroupBy(g => g.Act).Select(g => g.Key + "=" + g.Count());
            TestContext.WriteLine("acts: " + string.Join(", ", acts));
            var outc = probe.Events.OfType<ExchangeClosed>().GroupBy(g => g.Outcome).Select(g => g.Key + "=" + g.Count());
            TestContext.WriteLine("outcomes: " + string.Join(", ", outc));
            Assert.Greater(counts.TryGetValue("GestureMade", out var n) ? n : 0, 0);
        }
    }
}
